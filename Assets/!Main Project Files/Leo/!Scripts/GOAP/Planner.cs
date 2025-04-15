using System.Collections.Generic;
using UnityEngine;

namespace _Main_Project_Files.Leo._Scripts.GOAP
{
    public class Planner
    {
        private class PlanNode
        {
            public Action Action { get; }
            public PlanNode Parent { get; }
            public float RunningCost { get; }
            public Dictionary<string, bool> State { get; }

            public PlanNode(Action action, PlanNode parent, Dictionary<string, bool> state, float runningCost)
            {
                Action = action;
                Parent = parent;
                State = new Dictionary<string, bool>(state);
                RunningCost = runningCost;
            }
        }

        public List<Action> CreatePlan(WorldState worldState, List<Action> availableActions, string goalState,
            bool goalValue)
        {
            // Get copy of current world state.
            Dictionary<string, bool> currentState = worldState.GetStates();

            // Check if goal state is already achieved.
            if (currentState.ContainsKey(goalState) && currentState[goalState] == goalValue)
            {
                Debug.Log($"[GOAP PLANNER] Planner.cs: Goal {goalState} = {goalValue} is already active.");
                return new List<Action>();
            }

            // Store the final plan in a list.
            List<Action> plan = new List<Action>();

            // Lists for Astar search.
            List<PlanNode> openNodes = new List<PlanNode>();
            HashSet<PlanNode> closedNodes = new HashSet<PlanNode>();

            // Create a start node with no action and current state.
            PlanNode start = new PlanNode(null, null, currentState, 0);
            openNodes.Add(start);

            while (openNodes.Count > 0)
            {
                // Get the node with the lowest running cost.
                PlanNode currentNode = FindNodeWithLowestcost(openNodes);
                openNodes.Remove(currentNode);
                closedNodes.Add(currentNode);

                // Check if the current node satistfies the goal.
                if (currentNode.State.ContainsKey(goalState) && currentNode.State[goalState] == goalValue)
                {
                    // If a plan is found then trace it back through the parents.
                    plan = BuildPlan(currentNode);
                    return plan;
                }

                // Try all actions available.
                foreach (var action in availableActions)
                {
                    // Skip actions that can't be performed.
                    if (!CanPerformAction(action, currentNode.State)) continue;

                    // Apply the effects of the actions to create a new state.
                    Dictionary<string, bool> newState =
                        ApplyActionEffects(action, new Dictionary<string, bool>(currentNode.State));
                    // Calculate the new cost.
                    float newCost = currentNode.RunningCost + action.ActionCost;
                    // Create a new node.
                    PlanNode newNode = new PlanNode(action, currentNode, newState, newCost);
                    // Skip if the state was already ecxplored with a lower cost.
                    if ((IsInClosedList(closedNodes, newNode)) || IsInOpenListWithLowerCost(openNodes, newNode))
                        continue;
                    openNodes.Add(newNode);
                }
            }

            // no plan found
            Debug.LogWarning($"[GOAP PLANNER] Planner.cs: No plan found for goal {goalState}={goalValue}");
            return null;
        }

        private PlanNode FindNodeWithLowestcost(List<PlanNode> nodes)
        {
            PlanNode lowestCostNode = nodes[0];
            for (int i = 1; i < nodes.Count; i++)
            {
                if (nodes[i].RunningCost < lowestCostNode.RunningCost)
                {
                    lowestCostNode = nodes[i];
                }
            }

            return lowestCostNode;
        }

        private List<Action> BuildPlan(PlanNode goalNode)
        {
            // Empty plan.
            List<Action> plan = new List<Action>();
            
            // Work backwards from the goal node.
            PlanNode currentNode = goalNode;
            
            // Skip the first node (cause it never has any actions).
            while (currentNode.Parent != null)
            {
                plan.Insert(0, currentNode.Action);
                currentNode = currentNode.Parent;
                ;
            }

            return plan;
        }

        private bool CanPerformAction(Action action, Dictionary<string, bool> state)
        {
            // Check all prerequisites to see if can be performed.
            foreach (var preRequisite in action.PreRequisites)
            {
                // If the state doens't have the key or its value doesn't match:
                if (!state.ContainsKey(preRequisite.PreRequisiteName) ||
                    state[preRequisite.PreRequisiteName] != preRequisite.IsAchivable)
                {
                    return false;
                }
            }

            return true;
        }

        private Dictionary<string, bool> ApplyActionEffects(Action action, Dictionary<string, bool> state)
        {
            // Apply all effects.
            foreach (var effect in action.Effects)
            {
                state[effect.EffectName] = effect.IsEffectActive;
            }

            return state;
        }

        private bool IsInClosedList(HashSet<PlanNode> closedList, PlanNode node)
        {
            // Check if the node's state is already in the closed list.
            foreach (var closedNode in closedList)
            {
                if (AreStatesEqual(closedNode.State, node.State))
                    return true;
            }

            return false;
        }

        private bool IsInOpenListWithLowerCost(List<PlanNode> openList, PlanNode node)
        {
            // Check if the node's state is in the open list with a lower cost.
            foreach (var openNode in openList)
            {
                if (AreStatesEqual(openNode.State, node.State) && openNode.RunningCost < node.RunningCost)
                    return true;
            }

            return false;
        }

        private bool AreStatesEqual(Dictionary<string, bool> state1, Dictionary<string, bool> state2)
        {
            // States are equal if they have the same keys with the same values.
            if (state1.Count != state2.Count)
                return false;

            foreach (var kvp in state1)
            {
                if (!state2.ContainsKey(kvp.Key) || state2[kvp.Key] != kvp.Value)
                    return false;
            }

            return true;
        }
    }
}