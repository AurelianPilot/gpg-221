using System.Collections.Generic;
using UnityEngine;

namespace _Main_Project_Files._Scripts.GOAP
{
    /// <summary>
    /// An action that is performed by AI.
    /// </summary>
    public class Action : MonoBehaviour, IActionable
    {
        /// <summary>
        /// Effect caused by this action being performed.
        /// </summary>
        List<Effect> effects = new List<Effect>();
        
        /// <summary>
        /// Pre-requisites for this action to be performed.
        /// </summary>
        List<PreRequisites> preRequisites = new List<PreRequisites>();

        /// <summary>
        /// Get cost of than action.
        /// </summary>
        /// <returns>The cost of the action.</returns>
        int GetCost()
        {
            
            // TODO: Make sure to return the actual value later in development.
            return 0;
        }

        /// <summary>
        /// Perform the action.
        /// </summary>
        public void PerformAction()
        {
            throw new System.NotImplementedException();
        }
    }
}
