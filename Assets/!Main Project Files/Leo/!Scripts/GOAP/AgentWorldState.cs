using System.Collections.Generic;
using UnityEngine;

namespace _Main_Project_Files.Leo._Scripts.GOAP
{
    /// <summary>
    /// Stores the knowledge of an agent about the world.
    /// This script will be used by the GOAP planner to make decisions.
    /// </summary>
    public class AgentWorldState : MonoBehaviour
    {
        private Dictionary<WorldStateKey, bool> states = new();

        /// <summary>
        /// Checks if a specific state exists in the world state and its value.
        /// </summary>
        /// <param name="key">The WorldStateKey enum representing the state to check.</param>
        /// <returns></returns>
        public bool GetState(WorldStateKey key) {
            if (states.TryGetValue(key, out var value)) {
                return value;
            }
            
            return false;
        }

        public void SetState(WorldStateKey key, bool value) {
            if (states.ContainsKey(key)) {
                states[key] = value;
            }
            else {
                states.Add(key, value);
            }
        }

        public Dictionary<WorldStateKey, bool> GetAllStates() {
            return new Dictionary<WorldStateKey, bool>(states);
        }
    }
}