using System;
using System.Collections;
using System.Collections.Generic;
using _Main_Project_Files._Scripts.Pathfinding;
using NUnit.Framework.Internal.Execution;
using UnityEngine;
using UnityEngine.Analytics;

namespace _Main_Project_Files._Scripts.GOAP
{
    public class GoapAgent : MonoBehaviour
    {
        [Header("- Agent Settings")] [SerializeField]
        private string agentName = "GOAP Agent";
        [SerializeField] private float planningInterval = 1f;

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
            StartCoroutine(PlanningRoutine());
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

        public void SetGoal(string goalState, bool goalValue)
        {
            activeGoalState = goalState;
            activeGoalValue = goalValue;

            if (debugMode)
            {
                Debug.Log($"GoadAgent.cs: {agentName}: Goal set to {goalState} = {goalValue}");
            }

            AbortPlan();
        }

        private void AbortPlan()
        {
            if (isExecutingPlan)
            {
                StopAllCoroutines();
                isExecutingPlan = false;
                currentPlan.Clear();

                // Restart.
                StartCoroutine(PlanningRoutine());
            }
        }

        private IEnumerator PlanningRoutine()
        {
            while (true)
            {
                if (!isExecutingPlan && !string.IsNullOrEmpty(activeGoalState))
                {
                    if (CreatePlan())
                    {
                        StartCoroutine(ExecutePlan());
                    }
                    else if (debugMode)
                    {
                        Debug.Log($"GoapAgent.cs: {agentName} failed to create plan for goal {activeGoalState} = {activeGoalValue}");
                    }
                }
                yield return new WaitForSeconds(planningInterval);
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

            Planner planner = new Planner();
            List<Action> plan = planner.CreatePlan(worldState, availableActions, activeGoalState, activeGoalValue);

            if (plan != null && plan.Count > 0)
            {
                foreach (var action in plan)
                {
                    currentPlan.Enqueue(action);
                }

                // THIS IS FOR BETTER VISUALS WHEN DEBUGGING.
                if (debugMode)
                {
                    Debug.Log($"GoapAgent.cs: {agentName} Created plan with {plan.Count} actions.");
                    string planDesc = "Plan: ";
                    foreach (var actions in plan)
                    {
                        planDesc += actions.ActionName + " -> ";
                    }

                    planDesc += "GOAL";
                    Debug.Log(planDesc);
                }

                return true;
            }

            return false;
        }

        private IEnumerator ExecutePlan()
        {
            isExecutingPlan = true;

            while (currentPlan.Count > 0)
            {
                Action currentAction = currentPlan.Dequeue();

                if (debugMode)
                {
                    Debug.Log($"GoapAgent.cs: {agentName} is executing action '{currentAction.ActionName}'");
                }
                
                yield return PerformAction(currentAction);

                if (worldState.GetState(activeGoalState) == activeGoalValue)
                {
                    if (debugMode)
                    {
                        Debug.Log($"GoapAgent.cs: Goal {activeGoalState} = {activeGoalValue} is achieved.");
                    }
                    break;
                }
            }
            isExecutingPlan = false;
        }

        private IEnumerator PerformAction(Action action)
        {
            action.ApplyEffects(worldState);
            yield return new WaitForSeconds(1f);
            
            // TODO: Add actual real action execution, this is a 'placeholder' function for now.
        }

        public Agent GetPathfindingAgent()
        {
            return pathfindingAgent;
        }
        
        
    }
}