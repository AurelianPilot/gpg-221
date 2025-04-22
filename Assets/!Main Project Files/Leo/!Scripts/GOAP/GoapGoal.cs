using System.Collections.Generic;

namespace _Main_Project_Files.Leo._Scripts.GOAP
{
    public class GoapGoal
    {
        public string GoalName { get; private set; }
        private Dictionary<WorldStateKey, bool> goalState = new();

        public GoapGoal(string name, WorldStateKey key, bool value) {
            GoalName = name;
            goalState[key] = value;
        }
        
        public Dictionary<WorldStateKey, bool> GetGoalState() {
            return goalState;
        }
    }
}