using System;
using _Main_Project_Files._Scripts.Pathfinding;
using UnityEngine;

namespace _Main_Project_Files._Scripts.GOAP
{
    [Serializable]
    public class PreRequisite
    {
        private bool isAchivable;
        private string preRequisiteName;
        
        public bool IsAchivable => isAchivable;
        public string PreRequisiteName => preRequisiteName;

        public PreRequisite(bool isAchivable, string preRequisiteName)
        {
            this.isAchivable = isAchivable;
            this.preRequisiteName = preRequisiteName;
        }

        /// <summary>
        /// This function checks if the pre-requisite is fulfilled by a given world state.
        /// </summary>
        /// <param name="worldState">Reference to the world state</param>
        /// <returns></returns>
        public bool IsSatisfied(WorldState worldState)
        {
            return worldState.GetState(preRequisiteName) == isAchivable;
        }
    }
}
