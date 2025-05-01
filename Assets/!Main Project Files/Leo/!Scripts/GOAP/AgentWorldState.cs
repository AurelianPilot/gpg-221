using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Main_Project_Files.Leo._Scripts.GOAP
{
    /// <summary>
    /// Stores the knowledge of an agent about the world.
    /// This component maintains a dictionary of world state values that
    /// are used by the GOAP planner to make decisions.
    /// 
    /// Execution Flow:
    /// 1. Actions modify world state through SetState.
    /// 2. Planner reads world state to determine valid actions.
    /// 3. Goal conditions are compared against world state.
    /// </summary>
    public class AgentWorldState : MonoBehaviour
    {
        #region Variables

        /// <summary>
        /// Dictionary of all world state values, keyed by WorldStateKey enum.
        /// </summary>
        private readonly Dictionary<WorldStateKey, bool> _states = new();

        #endregion

        private void Awake()
        {
            InitializeWorldState();
        }
        
        /// <summary>
        /// Ensures all possible world state keys exist in the dictionary, defaulting to false.
        /// </summary>
        private void InitializeWorldState()
        {
            Debug.Log($"Initializing World State for {gameObject.name}...");
            // Get all possible keys defined in the WorldStateKey enum.
            foreach (WorldStateKey key in Enum.GetValues(typeof(WorldStateKey)))
            {
                // Add the key with a default value of 'false' if it doesn't already exist.
                if (!_states.ContainsKey(key))
                {
                    _states.Add(key, false);
                }
            }
        }
        
        #region State Accessors

        /// <summary>
        /// Retrieves the value of a specific state from the world state dictionary.
        /// If the state doesn't exist, returns false by default.
        /// </summary>
        /// <param name="key">The WorldStateKey enum representing the state to check.</param>
        /// <returns>The boolean value of the state, or false if not found.</returns>
        public bool GetState(WorldStateKey key) {
            if (_states.TryGetValue(key, out var value)) {
                return value;
            }

            return false;
        }

        /// <summary>
        /// Sets or updates a world state value with the specified key and value.
        /// If the key already exists, updates its value; otherwise, adds a new entry.
        /// </summary>
        /// <param name="key">The WorldStateKey enum representing the state to set.</param>
        /// <param name="value">The boolean value to assign to the state.</param>
        public void SetState(WorldStateKey key, bool value) {
            if (_states.ContainsKey(key)) {
                _states[key] = value;
            }
            else {
                _states.Add(key, value);
            }
        }

        /// <summary>
        /// Returns a copy of the entire world state dictionary.
        /// This is used by the planner to work with the current state without modifying it.
        /// </summary>
        /// <returns>A new dictionary containing all current world state key-value pairs.</returns>
        public Dictionary<WorldStateKey, bool> GetAllStates() {
            return new Dictionary<WorldStateKey, bool>(_states);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Toggles the value of a specific state (true becomes false, false becomes true).
        /// </summary>
        /// <param name="key">The WorldStateKey enum representing the state to toggle.</param>
        public void ToggleState(WorldStateKey key) {
            bool currentValue = GetState(key);
            SetState(key, !currentValue);
        }

        /// <summary>
        /// Checks if a specific state exists in the world state dictionary.
        /// </summary>
        /// <param name="key">The WorldStateKey enum to check for existence.</param>
        /// <returns>True if the state exists, false otherwise.</returns>
        public bool HasState(WorldStateKey key) {
            return _states.ContainsKey(key);
        }

        /// <summary>
        /// Removes a state from the world state dictionary if it exists.
        /// </summary>
        /// <param name="key">The WorldStateKey enum to remove.</param>
        /// <returns>True if the state was removed, false if it didn't exist.</returns>
        public bool RemoveState(WorldStateKey key) {
            return _states.Remove(key);
        }

        /// <summary>
        /// Clears all states from the world state dictionary.
        /// </summary>
        public void ClearAllStates() {
            _states.Clear();
        }

        #endregion

        #region Debug Methods

        /// <summary>
        /// Logs the current world state to the console for debugging.
        /// </summary>
        public void LogWorldState() {
            string stateLog = "Current World State:\n";

            foreach (var state in _states) {
                stateLog += $"- {state.Key}: {state.Value}\n";
            }

            Debug.Log(stateLog);
        }

        #endregion
    }
}