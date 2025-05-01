using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Main_Project_Files.Leo._Scripts.GOAP.Actions
{
    /// <summary>
    /// This is the base class for all actions in the GOAP system.
    /// The actions MUST have pre-requisites, effects and cost, along the execution logic.
    /// </summary>
    [RequireComponent(typeof(AgentWorldState))] // Actions need access to the agents' world state.
    public abstract class GoapAction : MonoBehaviour
    {
        [Header("- Action Cost")]
        [SerializeField] protected float cost = 1f; // I'm setting 1 as the default cost for everything.

        private readonly Dictionary<WorldStateKey, bool> _preRequisites = new();
        private readonly Dictionary<WorldStateKey, bool> _effects = new();

        protected AgentWorldState AgentWorldState;

        protected virtual void Awake() {
            AgentWorldState = GetComponent<AgentWorldState>();
            if (AgentWorldState == null) {
                Debug.LogError(
                    $"GoapAction.cs: ({gameObject.name}): AgentWorldState component not found! Action cannot function.");
            }

            SetUpPreRequisites();
            SetUpEffects();
        }

        #region Abstract Methods (that should be implemented by actions inheriting from this).

        /// <summary>
        /// Actions MUST implement this to define their prerequisites using the method AddPrerequisite(key, value).
        /// </summary>
        protected abstract void SetUpPreRequisites();

        /// <summary>
        /// Actions should also define their effects using the method AddEffect(key, value).
        /// </summary>
        protected abstract void SetUpEffects();

        /// <summary>
        /// The core logic of the action (what the agent actually does). 
        /// </summary>
        /// <returns>True if the action was successfully done, false if failed.</returns>
        public abstract IEnumerator PerformAction();

        /// <summary>
        /// This is to check for conditions that must be true right before the action executes,
        /// but might be too dynamic/complex to be part of the static WorldState.
        /// </summary>
        /// <returns>True if the conditions are met, false if not.</returns>
        public abstract bool CheckProceduralPreconditions();

        #endregion

        #region Helper Methods for Setup

        /// <summary>
        /// Add PreRequisite to action.
        /// </summary>
        /// <param name="key">WorldStateKey (enum).</param>
        /// <param name="value">Should be true or false.</param>
        protected void AddPrerequisite(WorldStateKey key, bool value) {
            _preRequisites[key] = value;
        }

        /// <summary>
        /// Add Effect from action.
        /// </summary>
        /// <param name="key">WorldStateKey (enum).</param>
        /// <param name="value">Set it to be true or false.</param>
        protected void AddEffect(WorldStateKey key, bool value) {
            _effects[key] = value;
        }

        #endregion

        #region Public Accesors

        /// <summary>
        /// Get the action's cost.
        /// </summary>
        /// <returns>The action's cost.</returns>
        public float GetCost() {
            return cost;
        }

        /// <summary>
        /// Get the action's pre-requisites.
        /// </summary>
        /// <returns>The action's pre-requisistes.</returns>
        public Dictionary<WorldStateKey, bool> GetPrerequisites() {
            return _preRequisites;
        }

        /// <summary>
        /// Get the action's effects.
        /// </summary>
        /// <returns>The action's effects.</returns>
        public Dictionary<WorldStateKey, bool> GetEffects() {
            return _effects;
        }

        /// <summary>
        /// Apply effects to the agent's world state.
        /// </summary>
        public void ApplyEffectsToWorldState() {
            foreach (var effect in _effects) {
                AgentWorldState.SetState(effect.Key, effect.Value);
            }
        }

        #endregion
    }
}