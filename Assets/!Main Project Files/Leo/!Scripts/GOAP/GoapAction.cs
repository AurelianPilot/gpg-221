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
                
                // TODO: Call setup methods here later.
            }
        }

        #region Abstract Methods (that should be implemented by actions inheriting from this).

        protected abstract void SetUpPreRequisites();
        
        protected abstract void SetUpEffects();
        
        protected abstract IEnumerator PerformAction();

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
