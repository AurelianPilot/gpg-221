using System.Collections;
using System.Collections.Generic;
using _Main_Project_Files.Leo._Scripts.Pathfinding;
using UnityEngine;
using Action = _Main_Project_Files.Leo._Scripts.GOAP.Action;
using Random = UnityEngine.Random;

namespace _Main_Project_Files.Leo._Scripts.GOAP
{
    public class GoapAgent : MonoBehaviour
    {
        [Header("- Agent Settings")] 
        [SerializeField] private string agentName = "GOAP Agent";
        [SerializeField] private float planningInterval = 1f;
        [SerializeField] private float actionExecutionCooldown = 0.5f;

        [Header("- Goal Settings")] 
        [SerializeField] private string activeGoalState = "";
        [SerializeField] private bool activeGoalValue = true;

        [Header("- Random Goals Settings")]
        [SerializeField] private float minGoalChangeSeconds = 2f;
        [SerializeField] private float maxGoalChangeSeconds = 5f;
        
        [Header("- Debug")] 
        [SerializeField] private bool debugMode = true;
        
        private WorldState worldState;
        private List<Action> availableActions = new List<Action>();
        private Queue<Action> currentPlan = new Queue<Action>();
        private Agent pathfindingAgent;
        private bool isExecutingPlan = false;
        private Action currentAction = null;
        private Coroutine planningCoroutine;
        private Coroutine executionCoroutine;

        public WorldState WorldState => worldState;
        public string ActiveGoalState => activeGoalState;
        public bool ActiveGoalValue => activeGoalValue;

        protected virtual void Awake()
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
            StartCoroutine(RandomGoalSelection());
        }

        private void OnDisable()
        {
            if (planningCoroutine != null)
            {
                StopCoroutine(planningCoroutine);
            }

            if (executionCoroutine != null)
            {
                StopCoroutine(executionCoroutine);
            }
        }

        private void InitializeAvailableActions()
        {
            availableActions.Clear();
            Action[] actions = GetComponents<Action>();
            
            foreach (var action in actions)
            {
                availableActions.Add(action);
                action.SetOwner(this);
                
                if (debugMode)
                {
                    Debug.Log($"[GOAP SYS] GoapAgent.cs: {agentName} added action '{action.ActionName}'");
                }
            }
        }

        public void SetGoal(string goalState, bool goalValue)
        {
            if (string.IsNullOrEmpty(goalState))
            {
                Debug.LogError($"[GOAP] GoapAgent.cs: {agentName} Can't set emtpy goal state.");
            }
            
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
                if (executionCoroutine != null)
                {
                    StopCoroutine(executionCoroutine);
                    executionCoroutine = null;
                }
                isExecutingPlan = false;
                currentPlan.Clear();
                currentAction = null;

                if (planningCoroutine != null)
                {
                    StopCoroutine(planningCoroutine);
                }
                
                planningCoroutine = StartCoroutine(PlanningRoutine());
                
                Debug.Log($"[GOAP SYS] GoapAgent.cs: {agentName} aborted current plan.");
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
                        executionCoroutine = StartCoroutine(ExecutePlan());
                    }
                    else if (debugMode)
                    {
                        Debug.Log($"GoapAgent.cs: {agentName} failed to create plan for goal {activeGoalState} = {activeGoalValue}");
                    }
                }
                yield return new WaitForSeconds(planningInterval);
            }
        }

        private IEnumerator RandomGoalSelection()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minGoalChangeSeconds, maxGoalChangeSeconds));
                
                string[] possibleGoals = {"IsPatrolling", "FlagCaptured", "TerritoryExpanded", "EnemyEliminated"};
                string randomGoal = possibleGoals[Random.Range(0, possibleGoals.Length)];
                
                Debug.Log($"[GOAP SYS] {agentName} changing goal to {randomGoal}");
                SetGoal(randomGoal, true);
                
                if (!isExecutingPlan)
                {
                    AbortPlan();
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

                return false;
            }

            Planner planner = new Planner();
            List<Action> plan = planner.CreatePlan(worldState, availableActions, activeGoalState, activeGoalValue);

            if (plan != null && plan.Count > 0)
            {
                foreach (var action in plan)
                {
                    currentPlan.Enqueue(action);
                }

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
                currentAction = currentPlan.Dequeue();

                if (debugMode)
                {
                    Debug.Log($"GoapAgent.cs: {agentName} is executing action '{currentAction.ActionName}'");
                }

                yield return StartCoroutine(currentAction.Execute());

                currentAction.ApplyEffects(worldState);
                    
                yield return new WaitForSeconds(actionExecutionCooldown);

                if (worldState.GetState(activeGoalState) == activeGoalValue)
                {
                    if (debugMode)
                    {
                        Debug.Log($"GoapAgent.cs: Goal {activeGoalState} = {activeGoalValue} is achieved.");
                    }
                    break;
                }
            }
            
            currentAction = null;
            isExecutingPlan = false;
        }

        public Agent GetPathfindingAgent()
        {
            return pathfindingAgent;
        }
    }
}