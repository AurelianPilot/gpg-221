using System.Collections;
using System.Collections.Generic;
using _Main_Project_Files.Leo._Scripts.Pathfinding;
using UnityEngine;

namespace _Main_Project_Files.Leo._Scripts.GOAP.Actions
{
    /// <summary>
    /// This script MUST be in the Agent that can perform the action.
    /// </summary>
    [RequireComponent(typeof(PathFindingAgent))]
    public sealed class WanderAction : GoapAction
    {
        [Header("- Wander Settings")]
        [SerializeField] private float wanderRadius = 10f;
        [SerializeField] private float minWaitTime = 1f;
        [SerializeField] private float maxWaitTime = 3f;
        [Tooltip("How close to the target counts as arrived.")]
        [SerializeField] private float arrivalDistance = 0.5f;

        private PathFindingAgent pathfindingAgent;
        private GridManager gridManager;
        private bool actionIsRunning;
        private Vector3 currentWanderTarget = Vector3.zero;

        protected override void Awake() {
            base.Awake();

            pathfindingAgent = GetComponent<PathFindingAgent>();
            if (pathfindingAgent == null) {
                Debug.LogError($"WanderAction.cs: ({gameObject.name}): Agent component not found!");
            }

            // The Aster component should also be in the inspector.
            if (pathfindingAgent != null && pathfindingAgent.astar == null) {
                Debug.LogError($"WanderAction.cs: ({gameObject.name}): The Agent component's Astar reference is NOT set!");
            }

            // Get GridManager in scene.
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null) {
                Debug.LogError($"WanderAction.cs: ({gameObject.name}): GridManager not found in scene!");
            }
        }

        // Define the prerequisites for this action
        protected override void SetUpPreRequisites() {
            // No prerequisites for wandering (at least for now?) ----------------------------------------------------------------------------------------------------------------------
        }

        protected override void SetUpEffects() {
            AddEffect(WorldStateKey.IsWandering, true);
        }

        /// <summary>
        /// Checks if the action can run in this instant. (comes in handy when the action's requisites are dynamic).
        /// </summary>
        public override bool CheckProceduralPreconditions() {
            // Can it pathfind? 
            return pathfindingAgent != null &&
                   // Astar and agent link exists?
                   pathfindingAgent.astar != null &&
                   // Grid exists?       
                   gridManager != null &&
                   //Not already running?
                   !actionIsRunning;
        }


        public override IEnumerator PerformAction() {
            if (!CheckProceduralPreconditions()) {
                Debug.LogWarning($"WanderAction.cs: Procedural precondition failed.");
                yield break;
            }

            actionIsRunning = true;
            // Find a random walkable node nearby.
            Node targetNode = GetRandomWalkableNodeNearby(transform.position, wanderRadius);

            // Failed to find a target.
            if (targetNode == null) {
                Debug.LogWarning("WanderAction.cs: Could not find suitable random walkable node nearby.");
                actionIsRunning = false;
                yield break;
            }

            currentWanderTarget = targetNode.Position;
            //Debug.Log($"WanderAction.cs: target node found at: {currentWanderTarget}");


            // Move to the point.
            pathfindingAgent.FollowPath(currentWanderTarget);

            // Wait until the agent reaches the destination (or gets close).
            // Monitor the agent's position relative to the target.
            float timeStartedMoving = Time.time;
            // Max time to wait for movement.
            float moveTimeout = 15f;

            while (Vector3.Distance(transform.position, currentWanderTarget) > arrivalDistance) {
                // Check if pathfinding failed or timed out
                if (!pathfindingAgent.IsFollowingPath() &&
                    Vector3.Distance(transform.position, currentWanderTarget) > arrivalDistance) {
                    // If not following path but still not close, agent probably got stuck (or something else).
                    yield return new WaitForSeconds(0.1f);

                    if (!pathfindingAgent.IsFollowingPath() &&
                        Vector3.Distance(transform.position, currentWanderTarget) > arrivalDistance) {
                        Debug.LogWarning(
                            $"WanderAction.cs: Agent stopped following path but hasn't reached target {currentWanderTarget}. Current Pos: {transform.position}. Aborting move.");
                        actionIsRunning = false;
                        yield break;
                    }
                }

                // Exit if taking too long.
                if (Time.time > timeStartedMoving + moveTimeout) {
                    Debug.LogWarning($"WanderAction: Movement to {currentWanderTarget} timed out!");
                    actionIsRunning = false;
                    yield break;
                }

                // wait for the next frame.
                yield return null;
            }

            Debug.Log("WanderAction.cs: Reached destination. Waiting.");

            // Wait for a bit (?).
            /*float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);*/

            // Action finished
            Debug.Log("WanderAction.cs: Finished.");
            currentWanderTarget = Vector3.zero;
            actionIsRunning = false;

            // GoapAgent.cs will call ApplyEffectsToWorldState after this coroutine finishes.
        }

        /// <summary>
        /// Helper method to find a random Walkable Node using my own Astar (Leo) from project 1.
        /// </summary>
        /// <param name="origin">Origin point from where Agent wants to find a random walkable node.</param>
        /// <param name="radius">Area from which the agent wants a walkable node.</param>
        /// <returns></returns>
        private Node GetRandomWalkableNodeNearby(Vector3 origin, float radius) {
            if (gridManager == null) return null;

            List<Node> nearbyWalkableNodes = new List<Node>();
            Node originNode = gridManager.GetNodeIndex(origin);

            if (originNode == null) {
                Debug.LogWarning("WanderAction.cs: Could not find node at agent's current position.");
                // Fallback: Check all nodes? Might be slow. Return null for now.
                return null;
            }

            // Simple approach: Iterate through grid nodes and check distance + walkability
            // This isn't the most efficient way for large grids, but okay for start.
            foreach (Node node in gridManager.Nodes) {
                if (node.Walkable) {
                    if (Vector3.Distance(origin, node.Position) <= radius) {
                        if (node.Index != originNode.Index) {
                            nearbyWalkableNodes.Add(node);
                        }
                    }
                }
            }

            if (nearbyWalkableNodes.Count > 0) {
                // Pick a random one from the list.
                int randomIndex = Random.Range(0, nearbyWalkableNodes.Count);
                return nearbyWalkableNodes[randomIndex];
            }

            return null;
        }
    }
}















