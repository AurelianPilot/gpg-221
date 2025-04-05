using System.Collections;
using _Main_Project_Files._Scripts.Agents;
using _Main_Project_Files._Scripts.Pathfinding;
using UnityEngine;

namespace _Main_Project_Files._Scripts.GOAP.Actions
{
    public class ExpandTerritory : Action
    {
        [Header("- Expansion Settings")]
        [SerializeField] private string territoryExpandedStateName = "TerritoryExpanded";
        
        
        private TeamAgent teamAgent;
        private GameManager gameManager;
        private GridManager gridManager;

        
        private void Awake()
        {
            actionName = "ExpandTerritory";
            isActionAchivable = true;
            
            if (preRequisites.Count == 0)
            {
                AddPreRequisite("IsDead", false);
            }
            
            if (effects.Count == 0)
            {
                AddEffect(territoryExpandedStateName, true);
            }
            
            teamAgent = GetComponent<TeamAgent>();
            gameManager = FindObjectOfType<GameManager>();
            gridManager = FindObjectOfType<GridManager>();
        }

        protected override IEnumerator PerformAction()
        {
            throw new System.NotImplementedException();
        }
    }
}
