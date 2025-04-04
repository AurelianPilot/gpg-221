using System;
using System.Collections;
using System.Collections.Generic;
using _Main_Project_Files._Scripts.Pathfinding;
using UnityEngine;

namespace _Main_Project_Files._Scripts.GOAP
{
    public class GoapAgent : MonoBehaviour
    {
        [Header("- Agent Settings")] [SerializeField]
        private string agentName = "GOAP Agent";

        [Header("- Goal Settings")] [SerializeField]
        private string activeGoalState = "";

        [SerializeField] private bool activeGoalValue = true;

        [Header("- Debug")] [SerializeField] private bool debugMode = true;

        private WorldState worldState;
        private List<Action> availableActions = new List<Action>();
        private Queue<Action> currentPlan = new Queue<Action>();
        private Agent pathfindingAgent;
        private bool isExecutingPlan = false;

        public WorldState WorldState => worldState;
        public string ActiveGoalState => activeGoalState;
        public bool ActiveGoalValue => activeGoalValue;

        private void Awake()
        {
            worldState = GetComponent<WorldState>();
            if (worldState == null)
            {
                worldState = gameObject.AddComponent<WorldState>();
            }

            pathfindingAgent = GetComponent<Agent>();
            if (pathfindingAgent == null)
            {
                pathfindingAgent = gameObject.AddComponent<Agent>();
            }

            InitializeAvailableActions();
        }

        private void Start()
        {
            // start planning here later.
        }

        private void InitializeAvailableActions()
        {
            availableActions.Clear();
            Action[] actions = GetComponents<Action>();
            foreach (var action in actions)
            {
                availableActions.Add(action);
                if (debugMode)
                {
                    Debug.Log($"GoapAgent.cs: Added action '{action.ActionName}'");
                }
            }
        }

        private IEnumerator PlanningRoutine()
        {
            while (true)
            {
                if (!isExecutingPlan && !string.IsNullOrEmpty(activeGoalState))
                {
                    if ()
                }
            }
        }

        private bool CreatePlan()
        {
            currentPlan.Clear();

            if (worldState.GetState(activeGoalState) == activeGoalValue)
            {
                if (debugMode)
                {
                    Debug.Log($"GoapAgent.cs: Goal {activeGoalState} = {activeGoalValue} is already achieved.");
                }
            }
            return false;
        }
        
        
    }
}