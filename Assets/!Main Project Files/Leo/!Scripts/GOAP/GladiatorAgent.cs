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
        [Header("- GOAP Core")]
        [SerializeField] private AgentWorldState agentWorldState;

        [SerializeField] private List<GoapAction> availableActions;
        private GoapGoal currentGoal;

        private GoapPlanner planner;

        [Header("- State")]
        private Queue<GoapAction> currentPlan = new();

        private bool isExecutingPlan;

        [Header("- Debugging")]
        [SerializeField] private bool logPlan = true;

        [SerializeField] private bool logExecution = true;

        #region MyRegion

        private void Awake() {
            if (agentWorldState == null) {
                agentWorldState = GetComponent<AgentWorldState>();
            }

            // Discover all GoapAction components attached to this GameObject.
            availableActions = GetComponents<GoapAction>().ToList();
            Debug.Log($"Found {availableActions.Count} actions for {gameObject.name}");

            planner = new GoapPlanner();        }

        private void Start() {
            // TODO: Replace this placeholder with a proper Goal definition
            currentGoal = new GoapGoal("WanderGoal", WorldStateKey.IsWandering, true);

            // Start the main decision-making loop
            StartCoroutine(RunGoapLoop());
        }

        /// <summary>
        /// The main Goap Loop happening for this agent.
        /// </summary>
        /// <returns></returns>
        private IEnumerator RunGoapLoop() {
            while (true) {
                // If we are not currently busy executing a plan.
                if (!isExecutingPlan) {
                    // Check if the current goal is already met.
                    if (IsGoalMet(currentGoal)) {
                        if (logExecution)
                            Debug.Log(
                                $"GladiatorAgent.cs: Goal '{currentGoal.GoalName}' already met. Idling or finding new goal...");

                        // TODO: Implement logic to find a new goal if current one is met.

                        yield return
                            new WaitForSeconds(
                                1f); // Wait before checking again (maybe let's add a variable for idling time?).
                        continue;
                    }

                    Debug.Log("GladiatorAgent.cs: Finding a plan...");

                    // Get current state and goal for the planner.
                    Dictionary<WorldStateKey, bool> currentState = agentWorldState.GetAllStates();
                    Dictionary<WorldStateKey, bool>
                        goalState = currentGoal.GetGoalState(); // Need to implement this in GoapGoal

                    planner = new GoapPlanner();
                    List<GoapAction> planList = planner.CreatePlan(this, availableActions, currentState, goalState);


                    if (planList != null && planList.Count > 0) {
                        // Plan found, store it and start execution.
                        currentPlan = new Queue<GoapAction>(planList);
                        if (logPlan) LogPlan(currentPlan);
                        isExecutingPlan = true;
                        StartCoroutine(ExecutePlan());
                    }
                    else {
                        // No plan found.
                        Debug.LogWarning(
                            $"GladiatorAgent.cs: Could not find a plan to achieve goal '{currentGoal.GoalName}'. Waiting...");
                        yield return new WaitForSeconds(1f);
                    }
                }

                yield return null;
            }
        }

        /// <summary>
        /// Executes the actions in the current plan queue.
        /// </summary>
        /// <returns></returns>
        private IEnumerator ExecutePlan() {
            if (logExecution) Debug.Log("GladiatorAgent.cs: Starting plan execution...");

            while (currentPlan.Count > 0) {
                GoapAction currentAction = currentPlan.Dequeue();

                if (logExecution) Debug.Log($"GladiatorAgent.cs: Executing Action: {currentAction.GetType().Name}");

                // Check procedural preconditions right before running.
                if (!currentAction.CheckProceduralPreconditions()) {
                    Debug.LogWarning(
                        $"GladiatorAgent.cs: Procedural precondition failed for action {currentAction.GetType().Name}. Aborting plan.");
                    isExecutingPlan = false;
                    currentPlan.Clear();
                    yield break;
                }

                yield return StartCoroutine(currentAction.PerformAction());
                currentAction.ApplyEffectsToWorldState();

                // TODO: Small delay between actions?
                // yield return new WaitForSeconds(0.1f);

                // if (IsGoalMet(currentGoal)) {
                //     Debug.Log("GladiatorAgent.cs: oal met mid-plan.");
                //     break;
                // }
            }

            if (logExecution) Debug.Log("GladiatorAgent.cs: Plan execution finished.");
            isExecutingPlan = false;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Checks if the goal conditions are currently met in the world state
        /// </summary>
        /// <param name="goal">GoapGoal class reference (to the goal to look if its met).</param>
        /// <returns></returns>
        private bool IsGoalMet(GoapGoal goal) {
            // TODO: Implement GoapGoal class properly
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
        /// <param name="plan">Reference to the planner.</param>
        /// TODO: Add the actual planner as a reference.
        private void LogPlan(Queue<GoapAction> plan) {
            string planStr = "GladiatorAgent.cs: Found Plan: ";
            foreach (GoapAction action in plan) {
                planStr += action.GetType().Name + " -> ";
            }

            planStr += "GOAL.";
            Debug.Log(planStr);
        }

        #endregion
    }
}