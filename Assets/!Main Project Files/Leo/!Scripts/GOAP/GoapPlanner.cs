using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Main_Project_Files.Leo._Scripts.GOAP
{
    /// <summary>
    /// Planner that helps AI agents decide what actions to take.
    /// 
    /// How it works:
    /// 1. Takes a list of possible actions and a goal state.
    /// 2. Uses A* search to find the best sequence of actions.
    /// 3. Returns a plan (list of actions) that will achieve the goal.
    /// 
    /// Example:
    /// If an agent needs to "attack enemy", the planner might create a plan like:
    /// 1. Move to enemy.
    /// 2. Draw weapon.
    /// 3. Attack.
    /// </summary>
    public class GoapPlanner
    {
        [Header("- Debugging")]
        [Tooltip("Enable this to see detailed planning information in the console")] [SerializeField]
        private readonly bool _showDebugLogs = true;

        [Tooltip("Stop planning if we can't find a solution after this many tries")] [SerializeField]
        private readonly int _maxSearchIterations = 1000;

        /// <summary>
        /// Creates a plan to achieve the goal state from the current world state.
        /// </summary>
        /// <param name="agent">The AI agent that will execute the plan.</param>
        /// <param name="availableActions">All actions the agent can possibly do.</param>
        /// <param name="currentState">What the world looks like right now.</param>
        /// <param name="goalState">What we want the world to look like.</param>
        /// <returns>A list of actions to perform, or null if no plan found.</returns>
        public List<GoapAction> CreatePlan(GladiatorAgent agent, List<GoapAction> availableActions,
            Dictionary<WorldStateKey, bool> currentState, Dictionary<WorldStateKey, bool> goalState) {
            if (_showDebugLogs)
                Debug.Log("GoapPlanner.cs: Starting to create a plan...");

            // Step 1: Get only the actions that can be performed right now.
            var possibleActions = FilterPossibleActions(availableActions);
            if (possibleActions.Count == 0) {
                Debug.LogWarning("GoapPlanner.cs: No possible actions available!");
                return null;
            }

            // Step 2: Set up our search.
            var statesToExplore = new List<ActionPlanNode>(); // States we haven't looked at yet.
            var exploredStates = new List<ActionPlanNode>(); // States we've already checked.

            // Step 3: Create our starting point
            var startState = new ActionPlanNode {
                Parent = null,
                Action = null,
                State = new Dictionary<WorldStateKey, bool>(currentState),
                CostSoFar = 0
            };
            startState.EstimatedCostToGoal = CalculateHeuristic(startState.State, goalState);
            statesToExplore.Add(startState);

            // Step 4: Main planning loop
            int iterations = 0;
            while (statesToExplore.Count > 0 && iterations < _maxSearchIterations) {
                iterations++;

                // Get the most promising state to look at next.
                var currentNode = GetMostPromisingState(statesToExplore);
                statesToExplore.Remove(currentNode);
                exploredStates.Add(currentNode);

                // Check if we've reached our goal
                if (HasReachedGoal(currentNode.State, goalState)) {
                    if (_showDebugLogs)
                        Debug.Log($"GoapPlanner.cs: Found a plan after {iterations} iterations!");
                    return BuildPlanFromNode(currentNode);
                }

                // Try each possible action from this state.
                foreach (var action in possibleActions) {
                    // Skip if we can't do this action right now.
                    if (!CanPerformAction(action, currentNode.State))
                        continue;

                    // Create a new state by doing this action.
                    var newState = ApplyActionEffects(currentNode.State, action);

                    // Skip if we've already seen this state
                    if (IsStateInList(newState, exploredStates))
                        continue;

                    // Calculate how good this new state is
                    float costToReachThisState = currentNode.CostSoFar + action.GetCost();
                    float estimatedCostToGoal = CalculateHeuristic(newState, goalState);

                    // Create a new node for this state
                    var newStateNode = new ActionPlanNode {
                        Parent = currentNode,
                        Action = action,
                        State = newState,
                        CostSoFar = costToReachThisState,
                        EstimatedCostToGoal = estimatedCostToGoal
                    };

                    // Add to our list of states to explore
                    AddOrUpdateState(newStateNode, statesToExplore);
                }
            }

            if (_showDebugLogs)
                Debug.LogWarning($"GoapPlanner.cs: Failed to find a plan after an iteration!");
            return null;
        }

        #region Helper Methods

        /// <summary>
        /// Filters out actions that can't be performed right now.
        /// </summary>
        private List<GoapAction> FilterPossibleActions(List<GoapAction> actions) {
            return actions.Where(action => action.CheckProceduralPreconditions()).ToList();
        }

        /// <summary>
        /// Gets the most promising state to explore next.
        /// We pick the state that has the lowest total cost (cost so far + estimated cost to goal).
        /// </summary>
        private ActionPlanNode GetMostPromisingState(List<ActionPlanNode> statesToExplore) {
            return statesToExplore.OrderBy(n => n.TotalCost).First();
        }

        /// <summary>
        /// Checks if the current state matches our goal state.
        /// </summary>
        private bool HasReachedGoal(Dictionary<WorldStateKey, bool> currentState,
            Dictionary<WorldStateKey, bool> goalState) {
            foreach (var goal in goalState) {
                if (!currentState.ContainsKey(goal.Key) || currentState[goal.Key] != goal.Value)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Estimates how far we are from the goal state.
        /// The more goal conditions that aren't met, the higher the number.
        /// </summary>
        private float CalculateHeuristic(Dictionary<WorldStateKey, bool> currentState,
            Dictionary<WorldStateKey, bool> goalState) {
            float distance = 0;
            foreach (var goal in goalState) {
                if (!currentState.ContainsKey(goal.Key) || currentState[goal.Key] != goal.Value)
                    distance++;
            }

            return distance;
        }

        /// <summary>
        /// Checks if an action can be performed in the current state.
        /// Example: Can't "attack" if we don't have a weapon.
        /// </summary>
        private bool CanPerformAction(GoapAction action, Dictionary<WorldStateKey, bool> currentState) {
            foreach (var prereq in action.GetPrerequisites()) {
                if (!currentState.ContainsKey(prereq.Key) || currentState[prereq.Key] != prereq.Value)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Applies an action's effects to the current state.
        /// Example: After "pickupWeapon" action, the state changes to "hasWeapon = true"
        /// </summary>
        private Dictionary<WorldStateKey, bool> ApplyActionEffects(Dictionary<WorldStateKey, bool> currentState,
            GoapAction action) {
            var newState = new Dictionary<WorldStateKey, bool>(currentState);
            foreach (var effect in action.GetEffects()) {
                newState[effect.Key] = effect.Value;
            }

            return newState;
        }

        /// <summary>
        /// Checks if we've already seen this state before.
        /// This helps us avoid going in circles.
        /// </summary>
        private bool IsStateInList(Dictionary<WorldStateKey, bool> state, List<ActionPlanNode> states) {
            return states.Any(node => AreStatesEqual(node.State, state));
        }

        /// <summary>
        /// Compares two states to see if they're exactly the same.
        /// </summary>
        private bool AreStatesEqual(Dictionary<WorldStateKey, bool> state1, Dictionary<WorldStateKey, bool> state2) {
            if (state1.Count != state2.Count) return false;
            return state1.All(kvp => state2.ContainsKey(kvp.Key) && state2[kvp.Key] == kvp.Value);
        }

        /// <summary>
        /// Adds a new state to explore, or updates an existing one if we found a better path.
        /// </summary>
        private void AddOrUpdateState(ActionPlanNode newState, List<ActionPlanNode> statesToExplore) {
            var existingState = statesToExplore.FirstOrDefault(n => AreStatesEqual(n.State, newState.State));
            if (existingState == null) {
                statesToExplore.Add(newState);
            }
            else if (newState.TotalCost < existingState.TotalCost) {
                statesToExplore.Remove(existingState);
                statesToExplore.Add(newState);
            }
        }

        /// <summary>
        /// Builds the final plan by following parent nodes back to the start.
        /// Example: If we found a path like: Start -> Move -> Attack,
        /// this will return [Move, Attack]
        /// </summary>
        private List<GoapAction> BuildPlanFromNode(ActionPlanNode goalNode) {
            var plan = new List<GoapAction>();
            var currentNode = goalNode;

            while (currentNode.Parent != null) {
                if (currentNode.Action != null)
                    plan.Add(currentNode.Action);
                currentNode = currentNode.Parent;
            }

            plan.Reverse(); // Put actions in the correct order
            return plan;
        }

        #endregion
    }

    /// <summary>
    /// Represents a single step in our planning process.
    /// Each node contains:
    /// - The world state at this point.
    /// - The action that got us here.
    /// - How much it cost to get here.
    /// - How far we think we are from the goal.
    /// </summary>
    public class ActionPlanNode
    {
        public ActionPlanNode Parent { get; set; }
        public GoapAction Action { get; set; }
        public Dictionary<WorldStateKey, bool> State { get; set; }
        public float CostSoFar { get; set; }
        public float EstimatedCostToGoal { get; set; }
        public float TotalCost => CostSoFar + EstimatedCostToGoal;
    }
}