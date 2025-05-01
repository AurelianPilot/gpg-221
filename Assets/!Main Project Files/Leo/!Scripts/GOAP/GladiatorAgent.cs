using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Main_Project_Files.Leo._Scripts.GOAP.Actions;
using _Main_Project_Files.Leo._Scripts.GOAP.Status_Systems;
using UnityEngine;

namespace _Main_Project_Files.Leo._Scripts.GOAP
{
    /// <summary>
    /// The main GOAP agent component that manages the agent's state,
    /// goals, available actions, and orchestrates the planning and
    /// execution process.
    /// </summary>
    [RequireComponent(typeof(AgentWorldState))]
    public class GladiatorAgent : MonoBehaviour
    {
        #region Enums
        
        /// <summary>
        /// Defines the role of the agent in the simulation.
        /// </summary>
        public enum AgentRole
        {
            Warrior,
            Healer
        }

        /// <summary>
        /// Defines the team affiliation of the agent.
        /// </summary>
        public enum TeamID
        {
            TeamA,
            TeamB
        }
        
        #endregion

        #region Serialized Fields
        
        [Header("- GOAP Core")]
        [SerializeField] private AgentWorldState agentWorldState;
        [SerializeField] private List<GoapAction> availableActions;

        [Header("- Debugging")]
        [SerializeField] private bool logPlan;
        [SerializeField] private bool logExecution;

        [Header("- Team & Role")]
        public TeamID teamID;
        public AgentRole agentRole;
        
        #endregion

        #region Private Fields
        
        private GoapGoal _currentGoal;
        private GoapPlanner _planner;
        private Queue<GoapAction> _currentPlan = new();
        private bool _isExecutingPlan;
        private GoapAction _currentExecutingAction = null;
        
        #endregion

        #region Public Properties
        
        /// <summary>
        /// Gets or sets the current enemy target for this agent.
        /// </summary>
        public GladiatorAgent CurrentTargetEnemy { get; set; }
        
        #endregion

        #region Public Fields
        
        /// <summary>
        /// List of allies known to this agent.
        /// </summary>
        public List<GladiatorAgent> knownAllies = new();
        
        /// <summary>
        /// List of enemies known to this agent.
        /// </summary>
        public List<GladiatorAgent> knownEnemies = new();
        
        #endregion

        #region Unity Lifecycle
        
        /// <summary>
        /// Initializes components and references on awake.
        /// </summary>
        private void Awake() 
        {
            InitializeComponents();
        }

        /// <summary>
        /// Sets initial goal and starts the GOAP decision loop.
        /// </summary>
        private void Start() 
        {
            SetInitialGoal();
            StartCoroutine(RunGoapLoop());
        }
        
        #endregion

        #region Initialization Methods
        
        /// <summary>
        /// Initializes required components and references.
        /// </summary>
        private void InitializeComponents() 
        {
            if (agentWorldState == null) 
            {
                agentWorldState = GetComponent<AgentWorldState>();
            }

            availableActions = GetComponents<GoapAction>().ToList();
            Debug.Log($"Found {availableActions.Count} actions for {gameObject.name}");

            _planner = new GoapPlanner();
        }

        /// <summary>
        /// Sets the initial goal for the agent depending on its role.
        /// </summary>
        private void SetInitialGoal() 
        {
            if (agentWorldState == null) agentWorldState = GetComponent<AgentWorldState>();

            InitializeRoleBasedWorldState();
            AssignInitialGoalBasedOnRole();
            
            Debug.Log($"{gameObject.name} starting with goal: {_currentGoal.GoalName}");
        }

        /// <summary>
        /// Initializes world state values based on agent role.
        /// </summary>
        private void InitializeRoleBasedWorldState()
        {
            agentWorldState.SetState(WorldStateKey.IsWarriorRole, agentRole == AgentRole.Warrior);
            agentWorldState.SetState(WorldStateKey.IsHealerRole, agentRole == AgentRole.Healer);
            agentWorldState.SetState(WorldStateKey.IsWandering, false);
            agentWorldState.SetState(WorldStateKey.IsInCombat, false);
        }

        /// <summary>
        /// Assigns the initial goal based on agent role.
        /// </summary>
        private void AssignInitialGoalBasedOnRole()
        {
            if (agentRole == AgentRole.Warrior) 
            {
                _currentGoal = new GoapGoal("WanderGoal", WorldStateKey.IsWandering, true, 1);
            }
            else if (agentRole == AgentRole.Healer) 
            {
                _currentGoal = new GoapGoal("KeepAlliesHealthyGoal", WorldStateKey.AllyNeedsHealing, false, 10);
            }
            else 
            {
                _currentGoal = new GoapGoal("WanderGoal", WorldStateKey.IsWandering, true, 1);
            }
        }
        
        #endregion

        #region GOAP Goal Management
        
        /// <summary>
        /// Sets a new goal for the agent.
        /// </summary>
        /// <param name="newGoal">The new goal to pursue.</param>
        public void SetGoal(GoapGoal newGoal) 
        {
            if (newGoal != null) 
            {
                _currentGoal = newGoal;
                Debug.Log($"{gameObject.name} assigned new goal: {_currentGoal.GoalName}");
            }
        }

        /// <summary>
        /// Switches the agent to a wandering goal
        /// </summary>
        private void SwitchToWanderingGoal() 
        {
            Debug.Log($"{gameObject.name} has nothing to do, switching to wandering goal");

            // If already wandering, don't switch
            if (_currentGoal.GoalName == "WanderGoal")
                return;

            // Stop current plan if any
            AbortCurrentPlan();

            // Set wandering goal
            _currentGoal = new GoapGoal("WanderGoal", WorldStateKey.IsWandering, true, 1);

            // Reset relevant world states
            agentWorldState.SetState(WorldStateKey.IsWandering, false);

            Debug.Log($"{gameObject.name} switching to goal: {_currentGoal.GoalName}");
        }

        /// <summary>
        /// Checks if the goal conditions are currently met in the world state.
        /// </summary>
        /// <param name="goal">Goal to check</param>
        /// <returns>True if all goal conditions are met, false otherwise</returns>
        private bool IsGoalMet(GoapGoal goal) 
        {
            if (goal == null) return true;

            Dictionary<WorldStateKey, bool> goalState = goal.GetGoalState();

            foreach (var condition in goalState) 
            {
                if (agentWorldState.GetState(condition.Key) != condition.Value) 
                {
                    return false;
                }
            }

            return true;
        }
        
        #endregion

        #region GOAP Main Loop
        
        /// <summary>
        /// The main GOAP decision loop for this agent.
        /// </summary>
        /// <returns>IEnumerator for coroutine</returns>
        private IEnumerator RunGoapLoop() 
        {
            while (true) 
            {
                if (!_isExecutingPlan) 
                {
                    yield return StartCoroutine(HandleGoalProcessing());
                }

                yield return null;
            }
        }

        /// <summary>
        /// Handles goal processing and planning when no plan is being executed.
        /// </summary>
        /// <returns>IEnumerator for coroutine</returns>
        private IEnumerator HandleGoalProcessing() 
        {
            if (IsGoalMet(_currentGoal)) 
            {
                yield return StartCoroutine(HandleCompletedGoal());
            }
            else 
            {
                yield return StartCoroutine(CreateAndExecutePlan());
            }
        }

        /// <summary>
        /// Handles logic when a goal is already completed.
        /// </summary>
        /// <returns>IEnumerator for coroutine</returns>
        private IEnumerator HandleCompletedGoal() 
        {
            if (logExecution) 
            {
                Debug.Log($"GladiatorAgent.cs: Goal '{_currentGoal.GoalName}' already met. Checking for new goals or wandering...");
            }

            if (IsAgentIdle() && _currentGoal.GoalName != "WanderGoal") 
            {
                SwitchToWanderingGoal();
            }
            else 
            {
                yield return new WaitForSeconds(0.5f);
            }

            yield return null;
        }

        /// <summary>
        /// Creates a plan and executes it if valid.
        /// </summary>
        /// <returns>IEnumerator for coroutine</returns>
        private IEnumerator CreateAndExecutePlan() 
        {
            Debug.Log($"GladiatorAgent.cs: Finding a plan for {transform.name}...");

            List<GoapAction> planList = GeneratePlan();

            if (planList != null && planList.Count > 0) 
            {
                yield return StartCoroutine(InitiateAndExecutePlan(planList));
            }
            else 
            {
                Debug.LogWarning($"GladiatorAgent.cs: Could not find a plan to achieve goal '{_currentGoal.GoalName}'. Waiting...");
                yield return new WaitForSeconds(1f);
            }
        }
        
        #endregion

        #region GOAP Planning
        
        /// <summary>
        /// Generates a plan using the GOAP planner.
        /// </summary>
        /// <returns>List of GoapActions that form the plan, or null if no plan is found</returns>
        private List<GoapAction> GeneratePlan() 
        {
            Dictionary<WorldStateKey, bool> currentState = agentWorldState.GetAllStates();
            Dictionary<WorldStateKey, bool> goalState = _currentGoal.GetGoalState();

            _planner = new GoapPlanner();
            return _planner.CreatePlan(this, availableActions, currentState, goalState);
        }

        /// <summary>
        /// Sets up and executes a generated plan
        /// </summary>
        /// <param name="planList">List of actions that form the plan</param>
        /// <returns>IEnumerator for coroutine</returns>
        private IEnumerator InitiateAndExecutePlan(List<GoapAction> planList) 
        {
            _currentPlan = new Queue<GoapAction>(planList);

            if (logPlan) 
            {
                LogPlan(_currentPlan);
            }

            _isExecutingPlan = true;
            yield return StartCoroutine(ExecutePlan());
        }
        
        #endregion

        #region GOAP Execution
        
        /// <summary>
        /// Executes the actions in the current plan queue.
        /// </summary>
        /// <returns>IEnumerator for coroutine</returns>
        private IEnumerator ExecutePlan() 
        {
            if (logExecution) Debug.Log("GladiatorAgent.cs: Starting plan execution...");

            while (_currentPlan.Count > 0) 
            {
                GoapAction currentAction = _currentPlan.Dequeue();
                _currentExecutingAction = currentAction;

                if (!TryExecuteAction(currentAction)) 
                {
                    _currentExecutingAction = null;
                    yield break;
                }

                yield return StartCoroutine(currentAction.PerformAction());
                currentAction.ApplyEffectsToWorldState();

                _currentExecutingAction = null;
            }

            CompletePlanExecution();
        }

        /// <summary>
        /// Attempts to execute a single action, checking its procedural preconditions.
        /// </summary>
        /// <param name="currentAction">The action to execute</param>
        /// <returns>True if the action can be executed, false otherwise</returns>
        private bool TryExecuteAction(GoapAction currentAction) 
        {
            if (logExecution) Debug.Log($"GladiatorAgent.cs: Executing Action: {currentAction.GetType().Name}");

            if (!currentAction.CheckProceduralPreconditions()) 
            {
                Debug.LogWarning($"GladiatorAgent.cs: Procedural precondition failed for action {currentAction.GetType().Name}. Aborting plan.");
                _isExecutingPlan = false;
                _currentPlan.Clear();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Aborts the current plan execution.
        /// </summary>
        public void AbortCurrentPlan() 
        {
            if (_isExecutingPlan) 
            {
                Debug.Log($"GladiatorAgent.cs: Aborting current plan for {gameObject.name}");
                _isExecutingPlan = false;
                _currentPlan.Clear();
                _currentExecutingAction = null;
            }
        }

        /// <summary>
        /// Completes the plan execution and resets execution state.
        /// </summary>
        private void CompletePlanExecution() 
        {
            if (logExecution) Debug.Log("GladiatorAgent.cs: Plan execution finished.");
            _isExecutingPlan = false;
        }
        
        #endregion

        #region Helper Methods
        
        /// <summary>
        /// Checks if the agent is currently idle (no combat targets and no other priority goals)
        /// </summary>
        /// <returns>True if the agent is idle, false otherwise</returns>
        private bool IsAgentIdle() 
        {
            if (agentRole == AgentRole.Warrior) 
            {
                return !HasValidEnemies();
            }

            if (agentRole == AgentRole.Healer) 
            {
                return !agentWorldState.GetState(WorldStateKey.AllyNeedsHealing);
            }

            return true;
        }

        /// <summary>
        /// Checks if there are any valid (alive) enemies.
        /// </summary>
        /// <returns>True if there are valid enemies, false otherwise</returns>
        private bool HasValidEnemies() 
        {
            foreach (var enemy in knownEnemies) 
            {
                if (enemy != null) 
                {
                    var enemyHealth = enemy.GetComponent<AgentHealthSystem>();
                    if (enemyHealth != null && !enemyHealth.IsDead) 
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Logs the calculated plan to the console
        /// </summary>
        /// <param name="plan">Queue of actions that form the plan</param>
        private void LogPlan(Queue<GoapAction> plan) 
        {
            string planStr = "GladiatorAgent.cs: Found Plan: ";
            foreach (GoapAction action in plan) 
            {
                planStr += action.GetType().Name + " -> ";
            }

            planStr += "GOAL.";
            Debug.Log(planStr);
        }
        
        #endregion

        #region Debug/Public Getters
        
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