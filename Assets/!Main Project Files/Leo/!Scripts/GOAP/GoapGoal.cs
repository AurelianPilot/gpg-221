using System.Collections.Generic;
using UnityEngine;

namespace _Main_Project_Files.Leo._Scripts.GOAP
{
    /// <summary>
    /// Represents a goal that a GOAP agent can pursue.
    /// Goals define a world state that the agent will try to achieve
    /// through planning and action execution.
    /// 
    /// Usage flow:
    /// 1. Create a goal with a name and initial condition.
    /// 2. Optionally add more conditions using AddCondition.
    /// 3. Assign the goal to an agent.
    /// 4. Agent will plan actions to satisfy all goal conditions.
    /// </summary>
    public class GoapGoal
    {
        #region Properties

        /// <summary>
        /// Readable name for debugging.
        /// </summary>
        public string GoalName { get; private set; }

        /// <summary>
        /// Priority of this goal (higher values = more important).
        /// </summary>
        public int Priority { get; private set; }

        /// <summary>
        /// The desired world state that this goal aims to achieve.
        /// </summary>
        private Dictionary<WorldStateKey, bool> goalState = new();

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new goal with a single condition.
        /// </summary>
        /// <param name="name">Human-readable name for debugging.</param>
        /// <param name="key">The world state key to target.</param>
        /// <param name="value">The desired value for the key.</param>
        /// <param name="priority">Priority level (higher = more important).</param>
        public GoapGoal(string name, WorldStateKey key, bool value, int priority = 1) {
            GoalName = name;
            Priority = priority;
            goalState[key] = value;
        }

        /// <summary>
        /// Creates a new goal with multiple conditions from an existing dictionary.
        /// </summary>
        /// <param name="name">Human-readable name for debugging.</param>
        /// <param name="conditions">Dictionary of conditions to satisfy.</param>
        /// <param name="priority">Priority level (higher = more important).</param>
        public GoapGoal(string name, Dictionary<WorldStateKey, bool> conditions, int priority = 1) {
            GoalName = name;
            Priority = priority;

            foreach (var condition in conditions) {
                goalState[condition.Key] = condition.Value;
            }
        }

        #endregion

        #region Goal Management

        /// <summary>
        /// Gets the full dictionary of goal conditions.
        /// </summary>
        /// <returns>Dictionary of world state conditions that define this goal.</returns>
        public Dictionary<WorldStateKey, bool> GetGoalState() {
            return new Dictionary<WorldStateKey, bool>(goalState);
        }

        /// <summary>
        /// Adds a new condition to this goal.
        /// </summary>
        /// <param name="key">World state key.</param>
        /// <param name="value">Desired value.</param>
        public void AddCondition(WorldStateKey key, bool value) {
            goalState[key] = value;
        }

        /// <summary>
        /// Removes a condition from this goal if it exists.
        /// </summary>
        /// <param name="key">World state key to remove.</param>
        /// <returns>True if condition was removed, false if it didn't exist.</returns>
        public bool RemoveCondition(WorldStateKey key) {
            return goalState.Remove(key);
        }

        /// <summary>
        /// Checks if this goal has a specific condition.
        /// </summary>
        /// <param name="key">World state key to check.</param>
        /// <returns>True if the goal includes this condition, false otherwise.</returns>
        public bool HasCondition(WorldStateKey key) {
            return goalState.ContainsKey(key);
        }

        /// <summary>
        /// Gets the desired value for a specific condition.
        /// </summary>
        /// <param name="key">World state key to check.</param>
        /// <returns>The desired value, or false if the condition isn't part of this goal.</returns>
        public bool GetConditionValue(WorldStateKey key) {
            if (goalState.TryGetValue(key, out bool value)) {
                return value;
            }

            return false;
        }

        /// <summary>
        /// Changes the priority of this goal.
        /// </summary>
        /// <param name="newPriority">New priority value.</param>
        public void SetPriority(int newPriority) {
            Priority = newPriority;
        }

        /// <summary>
        /// Returns a string representation of this goal for debugging.
        /// </summary>
        public override string ToString() {
            string result = $"Goal: {GoalName} (Priority: {Priority})\nConditions:";

            foreach (var condition in goalState) {
                result += $"\n - {condition.Key}: {condition.Value}";
            }

            return result;
        }

        #endregion
    }
}