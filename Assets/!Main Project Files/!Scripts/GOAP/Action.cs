using System.Collections.Generic;
using _Main_Project_Files._Scripts.Pathfinding;
using UnityEngine;

namespace _Main_Project_Files._Scripts.GOAP
{
    /// <summary>
    /// An action that is performed by AI (as in, an agent).
    /// </summary>
    public class Action : MonoBehaviour
    {
        [SerializeField] protected string actionName = "Unnamed Action";
        [SerializeField] protected float actionCost = 1f;
        [SerializeField] protected bool isActionAchivable = false;
        
        /// <summary>
        /// Effect caused by this action being performed.
        /// </summary>
        [SerializeField] protected List<Effect> effects = new List<Effect>();
        
        /// <summary>
        /// Pre-requisites for this action to be performed.
        /// </summary>
        [SerializeField] protected List<PreRequisite> preRequisites = new List<PreRequisite>();

        // Getters.
        public string ActionName => actionName;
        public float ActionCost => actionCost;
        public bool IsActionAchivable => isActionAchivable;
        public List<PreRequisite> PreRequisites => preRequisites;
        public List<Effect> Effects => effects;
        
        #region Script Specific

        /// <summary>
        ///  Checks the world state lists if the preRequisites are met to perform an action.
        /// </summary>
        /// <param name="worldState">Reference to the world state.</param>
        /// <returns></returns>
        public bool ArePreRequisitesSatisfied(WorldState worldState)
        {
            if (!isActionAchivable) return false;
            
            foreach (var preRequisite in preRequisites)
            {
                if (!preRequisite.IsSatisfied(worldState)) return false;
            }

            return true;
        }

        /// <summary>
        /// Apply all effects to the world state.
        /// </summary>
        /// <param name="worldState">Reference to the world state.</param>
        public void ApplyEffects(WorldState worldState)
        {
            foreach (var effect in effects)
            {
                effect.ApplyEffect(worldState);
            }
        }
        
        /// <summary>
        /// Get cost of than action.
        /// </summary>
        /// <returns>The cost of the action.</returns>
        int GetCost()
        {
            
            // TODO: Make sure to return the actual value later in development.
            return 0;
        }
        
        #endregion

    }
}