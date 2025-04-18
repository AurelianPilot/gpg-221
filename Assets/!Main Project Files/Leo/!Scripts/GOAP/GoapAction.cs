using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Main_Project_Files.Leo._Scripts.GOAP
{
    /// <summary>
    /// This is the base class for all actions in the GOAP system.
    /// The actions MUST have pre-requisites, effects and cost, along the execution logic.
    /// </summary>
    [RequireComponent(typeof(AgentWorldState))] // Actions need access to the agents' world state.
    public abstract class GoapAction : MonoBehaviour
    {
        [Header("- Action Cost")]
        [SerializeField] protected float cost = 1f; // I'm setting 1 as the default one.
        
        protected Dictionary<WorldStateKey, bool> preRequisites = new();
        protected Dictionary<WorldStateKey, bool> effects = new();
        
        protected AgentWorldState agentWorldState;

        private void Awake() {
            agentWorldState = GetComponent<AgentWorldState>();
            if (agentWorldState == null) {
                Debug.LogError($"GoapAction.cs: ({gameObject.name}: No AgentWorldState component referenced.");
                
                SetUpPreRequisites();
                SetUpEffects();
            }
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
        protected abstract IEnumerator PerformAction();

        /// <summary>
        /// This is to check for conditions that must be true right before the action executes,
        /// but might be too dynamic/complex to be part of the static WorldState.
        /// </summary>
        /// <returns>True if the conditions are met, false if not.</returns>
        public abstract bool CheckProceduralPreconditions();
        
        #endregion

        #region Helper Methods for Setup

        protected void AddPrerequisite(WorldStateKey key, bool value) {
            preRequisites[key] = value;
        }

        protected void AddEffect(WorldStateKey key, bool value) {
            effects[key] = value;
        }
        
        #endregion

        #region Public Accesors

        public float GetCost() {
            return cost;
        }

        public Dictionary<WorldStateKey, bool> GetPrerequisites() {
            return preRequisites;
        }

        public Dictionary<WorldStateKey, bool> GetEffects() {
            return effects;
        }

        public void ApplyEffectsToWorldState() {
            foreach (var effect in effects) {
                agentWorldState.SetState(effect.Key, effect.Value);
            }
        }

        #endregion
    }
}
