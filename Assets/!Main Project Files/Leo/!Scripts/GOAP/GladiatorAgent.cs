using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Main_Project_Files.Leo._Scripts.GOAP.Actions;
using UnityEngine;

namespace _Main_Project_Files.Leo._Scripts.GOAP
{
    /// <summary>
    /// The main GOAP agent component. It manages the agent's state,
    /// goals, available actions, and orchestrates the planning and
    /// execution process.
    /// </summary>
    [RequireComponent(typeof(AgentWorldState))]
    public class GladiatorAgent : MonoBehaviour
    {
        #region Variables

        [Header("- GOAP Core")]
        [SerializeField] private AgentWorldState agentWorldState;
        [SerializeField] private List<GoapAction> availableActions;
        private GoapGoal _currentGoal;
        private GoapPlanner _planner;

        [Header("- State")]
        private Queue<GoapAction> _currentPlan = new();
        private bool _isExecutingPlan;
        private GoapAction _currentExecutingAction = null;

        [Header("- Debugging")]
        [SerializeField] private bool logPlan;
        [SerializeField] private bool logExecution;
        
        public enum AgentRole { Warrior, Healer }
        public enum TeamID { TeamA, TeamB }

        [Header("- Team & Role")]
        public TeamID teamID;
        public AgentRole agentRole;
        public List<GladiatorAgent> knownAllies = new();
        public List<GladiatorAgent> knownEnemies = new();

        #endregion

        #region Unity Lifecycle

        private void Awake() {
            InitializeComponents();
        }

        private void Start() {
            // TODO: Replace this placeholder with a proper Goal definition.
            SetInitialGoal();

            // Start the main decision-making loop. If GOAP was a car engine this would start it lol.
            StartCoroutine(RunGoapLoop());
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize required components and references.
        /// </summary>
        private void InitializeComponents() {
            if (agentWorldState == null) {
                agentWorldState = GetComponent<AgentWorldState>();
            }

            // * This automatically populates the list with GoapActions by looking in the Gmae Object.
            availableActions = GetComponents<GoapAction>().ToList();
            Debug.Log($"Found {availableActions.Count} actions for {gameObject.name}");

            // Initialize the GOAP planner.
            _planner = new GoapPlanner();
        }

        /// <summary>
        /// Sets the initial goal for the agent depending on its role.
        /// </summary>
        private void SetInitialGoal()
        {
            if (agentWorldState == null) agentWorldState = GetComponent<AgentWorldState>();

            agentWorldState.SetState(WorldStateKey.IsWarriorRole, agentRole == AgentRole.Warrior);
            agentWorldState.SetState(WorldStateKey.IsHealerRole, agentRole == AgentRole.Healer);

            if (agentRole == AgentRole.Warrior)
            {
                //_currentGoal = new GoapGoal("AttackEnemiesGoal", WorldStateKey.EnemyDetected, false, 5);
                _currentGoal = new GoapGoal("EngageInCombatGoal", WorldStateKey.IsInCombat, true, 5);
            }
            else if (agentRole == AgentRole.Healer)
            {
                _currentGoal = new GoapGoal("KeepAlliesHealthyGoal", WorldStateKey.AllyNeedsHealing, false, 10);
            }
            else
            {
                _currentGoal = new GoapGoal("WanderGoal", WorldStateKey.IsWandering, true, 1);
            }

            Debug.Log($"{gameObject.name} starting with goal: {_currentGoal.GoalName}");
        }

        #endregion

        #region GOAP Core Logic

        /// <summary>
        /// The main GOAP decision loop for this agent.
        /// 
        /// Execution flow:
        /// 1. Check if a plan is already being executed.
        /// 2. If not, check if the current goal is already met.
        /// 3. If the goal is not met, create a new plan.
        /// 4. Execute the plan if one is found.
        /// 5. Repeat.
        /// </summary>
        private IEnumerator RunGoapLoop() {
            while (true) {
                if (!_isExecutingPlan) {
                    yield return StartCoroutine(HandleGoalProcessing());
                }

                yield return null;
            }
            // ReSharper disable once IteratorNeverReturns
        }

        /// <summary>
        /// Handles goal processing and planning when no plan is being executed.
        /// </summary>
        private IEnumerator HandleGoalProcessing() {
            // Check if the current goal is already met.
            if (IsGoalMet(_currentGoal)) {
                yield return StartCoroutine(HandleCompletedGoal());
            }
            else {
                yield return StartCoroutine(CreateAndExecutePlan());
            }
        }

        /// <summary>
        /// Handles logic when a goal is already completed.
        /// </summary>
        private IEnumerator HandleCompletedGoal() {
            if (logExecution) {
                Debug.Log(
                    $"GladiatorAgent.cs: Goal '{_currentGoal.GoalName}' already met. Idling or finding new goal...");
            }

            // TODO: Implement logic to find a new goal if current one is met.

            // ! I currently commented this because I wanna add an "afterActionCompletedCooldown" in the main GoapAction.cs class,
            // so each action has a cooldown before the next action can be executed.
            // yield return new WaitForSeconds(1f);
            yield return null;
        }

        /// <summary>
        /// Creates a plan and executes it if valid.
        /// </summary>
        private IEnumerator CreateAndExecutePlan() {
            Debug.Log($"GladiatorAgent.cs: Finding a plan for {transform.name}...");

            // Create a new plan.
            List<GoapAction> planList = GeneratePlan();

            if (planList != null && planList.Count > 0) {
                yield return StartCoroutine(InitiateAndExecutePlan(planList));
            }
            else {
                // No plan found.
                Debug.LogWarning(
                    $"GladiatorAgent.cs: Could not find a plan to achieve goal '{_currentGoal.GoalName}'. Waiting...");
                yield return new WaitForSeconds(1f);
            }
        }

        /// <summary>
        /// Generates a plan using the GOAP planner.
        /// </summary>
        private List<GoapAction> GeneratePlan() {
            // Get current state and goal for the planner.
            Dictionary<WorldStateKey, bool> currentState = agentWorldState.GetAllStates();
            Dictionary<WorldStateKey, bool> goalState = _currentGoal.GetGoalState();

            // Create a fresh planner instance and plan.
            _planner = new GoapPlanner();
            return _planner.CreatePlan(this, availableActions, currentState, goalState);
        }

        /// <summary>
        /// Sets up and executes a generated plan
        /// </summary>
        private IEnumerator InitiateAndExecutePlan(List<GoapAction> planList) {
            // Plan found, store it and start execution.
            _currentPlan = new Queue<GoapAction>(planList);

            if (logPlan) {
                LogPlan(_currentPlan);
            }

            _isExecutingPlan = true;
            yield return StartCoroutine(ExecutePlan());
        }

        /// <summary>
        /// Executes the actions in the current plan queue.
        /// 
        /// Execution flow:
        /// 1. For each action in the plan:
        ///    a) Check if procedural preconditions are met. If not, abort plan.
        ///    b) Perform the action.
        ///    c) Apply effects to world state.
        /// 2. Mark plan as completed.
        /// </summary>
        private IEnumerator ExecutePlan() {
            if (logExecution) Debug.Log("GladiatorAgent.cs: Starting plan execution...");

            while (_currentPlan.Count > 0) {
                GoapAction currentAction = _currentPlan.Dequeue();
                _currentExecutingAction = currentAction;

                if (!TryExecuteAction(currentAction)) {
                    _currentExecutingAction = null;
                    yield break;
                }

                yield return StartCoroutine(currentAction.PerformAction());
                currentAction.ApplyEffectsToWorldState();

                // TODO: Small delay between actions? ----------------------------again, I wanna add the variable afterActionCompletedCooldown in GoapAction.cs.
                // yield return new WaitForSeconds(0.1f);

                /*? Check if goal is met mid-plan
                if (IsGoalMet(currentGoal)) {
                    Debug.Log("GladiatorAgent.cs: Goal met mid-plan.");
                    break;
                }*/
                _currentExecutingAction = null;
            }

            CompletePlanExecution();
        }

        /// <summary>
        /// Attempts to execute a single action, checking its procedural preconditions.
        /// </summary>
        /// <returns>True if the action can be executed, false otherwise.</returns>
        private bool TryExecuteAction(GoapAction currentAction) {
            if (logExecution) Debug.Log($"GladiatorAgent.cs: Executing Action: {currentAction.GetType().Name}");

            // Check procedural preconditions right before running.
            if (!currentAction.CheckProceduralPreconditions()) {
                Debug.LogWarning(
                    $"GladiatorAgent.cs: Procedural precondition failed for action {currentAction.GetType().Name}. Aborting plan.");
                _isExecutingPlan = false;
                _currentPlan.Clear();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Completes the plan execution and resets execution state.
        /// </summary>
        private void CompletePlanExecution() {
            if (logExecution) Debug.Log("GladiatorAgent.cs: Plan execution finished.");
            _isExecutingPlan = false;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Checks if the goal conditions are currently met in the world state.
        /// </summary>
        /// <param name="goal">GoapGoal reference to check if it's met.</param>
        /// <returns>True if all goal conditions are met, false otherwise.</returns>
        private bool IsGoalMet(GoapGoal goal) {
            if (goal == null) return true;

            Dictionary<WorldStateKey, bool> goalState = goal.GetGoalState();

            // Look for a goal condition that wasn't met.
            foreach (var condition in goalState) {
                if (agentWorldState.GetState(condition.Key) != condition.Value) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Logs the calculated plan to the console
        /// </summary>
        /// <param name="plan">Queue of actions that form the plan</param>
        private void LogPlan(Queue<GoapAction> plan) {
            string planStr = "GladiatorAgent.cs: Found Plan: ";
            foreach (GoapAction action in plan) {
                planStr += action.GetType().Name + " -> ";
            }

            planStr += "GOAL.";
            Debug.Log(planStr);
        }

        #endregion
        
        #region Debug Methods

        /// <summary>
        /// Gets the agent's current goal for debugging purposes.
        /// </summary>
        /// <returns>The current goal or null if no goal is set.</returns>
        public GoapGoal GetCurrentGoal()
        {
            return _currentGoal;
        }

        /// <summary>
        /// Gets the agent's current executing action for debugging purposes.
        /// </summary>
        /// <returns>The current action being executed or null if no action is running.</returns>
        /// <summary>
        /// Gets the agent's current executing action for debugging purposes.
        /// </summary>
        /// <returns>The current action being executed or null if no action is running.</returns>
        public GoapAction GetCurrentAction()
        {
            return _currentExecutingAction;
        }

        /// <summary>
        /// Gets the agent's current plan as a list.
        /// </summary>
        /// <returns>A list of actions in the current plan or empty list if no plan exists.</returns>
        public List<GoapAction> GetCurrentPlan()
        {
            if (_currentPlan != null && _currentPlan.Count > 0)
            {
                return new List<GoapAction>(_currentPlan);
            }
    
            return new List<GoapAction>();
        }

        /// <summary>
        /// Gets the execution status of the agent.
        /// </summary>
        /// <returns>True if the agent is currently executing a plan, false otherwise.</returns>
        public bool IsExecutingPlan()
        {
            return _isExecutingPlan;
        }

        #endregion
    }
}