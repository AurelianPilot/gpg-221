using System.Collections;
using System.Collections.Generic;
using _Main_Project_Files.Leo._Scripts.Pathfinding;
using UnityEngine;

namespace _Main_Project_Files.Leo._Scripts.GOAP.Actions
{
    /// <summary>
    /// WanderAction allows an agent to wander around by moving to random walkable points.
    /// This action requires the agent to have a PathFindingAgent component for navigation.
    /// 
    /// Execution flow:
    /// 1. Find a random walkable point within a radius.
    /// 2. Navigate to that point.
    /// 3. Wait at the destination.
    /// 4. Mark the action as complete.
    /// </summary>
    [RequireComponent(typeof(PathFindingAgent))]
    public sealed class WanderAction : GoapAction
    {
        #region Settings

        [Header("- Wander Settings")]
        [SerializeField] private float wanderRadius = 10f;

        [SerializeField] private float minWaitTime = 1f;
        [SerializeField] private float maxWaitTime = 3f;

        [Tooltip("How close to the target counts as arrived.")] [SerializeField]
        private float arrivalDistance = 0.5f;

        [SerializeField] private float moveTimeout = 15f;

        #endregion

        #region Private Variables

        private PathFindingAgent _pathfindingAgent;
        private GridManager _gridManager;
        private bool _actionIsRunning;
        private Vector3 _currentWanderTarget = Vector3.zero;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the action with required components.
        /// </summary>
        protected override void Awake() {
            base.Awake();
            InitializeComponents();
        }

        /// <summary>
        /// Get and validate all required components.
        /// </summary>
        private void InitializeComponents() {
            InitializePathfindingAgent();
            InitializeGridManager();
        }

        /// <summary>
        /// Get and validate the pathfinding agent.
        /// </summary>
        private void InitializePathfindingAgent() {
            _pathfindingAgent = GetComponent<PathFindingAgent>();
            if (_pathfindingAgent == null) {
                Debug.LogError($"WanderAction.cs: ({gameObject.name}): Agent component not found!");
            }

            // ! The Astar component should also be in the inspector.
            if (_pathfindingAgent != null && _pathfindingAgent.astar == null) {
                Debug.LogError(
                    $"WanderAction.cs: ({gameObject.name}): The Agent component's Astar reference is NOT set!");
            }
        }

        /// <summary>
        /// Get and validate the grid manager.
        /// </summary>
        private void InitializeGridManager() {
            _gridManager = FindObjectOfType<GridManager>();
            if (_gridManager == null) {
                Debug.LogError($"WanderAction.cs: ({gameObject.name}): GridManager not found in scene!");
            }
        }

        #endregion

        #region GOAP Configuration

        /// <summary>
        /// Define the prerequisites for this action.
        /// </summary>
        protected override void SetUpPreRequisites() {
            // ? No prerequisites for wandering.
        }

        /// <summary>
        /// Define the effects this action will have on the world state.
        /// </summary>
        protected override void SetUpEffects() {
            AddEffect(WorldStateKey.IsWandering, true);
        }

        /// <summary>
        /// Checks if the action can run in this instant.
        /// Verifies dynamic conditions just before execution.
        /// </summary>
        public override bool CheckProceduralPreconditions() {
            return HasValidPathfindingSetup() && !_actionIsRunning;
        }

        /// <summary>
        /// Checks if all pathfinding components are properly set up.
        /// </summary>
        private bool HasValidPathfindingSetup() {
            // Can it pathfind?.
            return _pathfindingAgent != null &&
                   // Astar and agent link exists.
                   _pathfindingAgent.astar != null &&
                   // Grid exists?.
                   _gridManager != null;
        }

        #endregion

        #region Action Execution

        /// <summary>
        /// Performs the wander action:
        /// 1. Finds a random walkable location.
        /// 2. Moves the agent to that location.
        /// 3. ? Optionally waits at the destination.
        /// </summary>
        public override IEnumerator PerformAction() {
            if (!CheckProceduralPreconditions()) {
                Debug.LogWarning($"WanderAction.cs: Procedural precondition failed.");
                yield break;
            }

            _actionIsRunning = true;

            // Find a random walkable node nearby.
            if (!TryFindRandomWalkableTarget()) {
                _actionIsRunning = false;
                yield break;
            }

            // Move to the target.
            bool moveSuccessful = false;
            yield return StartCoroutine(MoveToTarget(moveSuccessful));

            if (moveSuccessful) {
                yield return StartCoroutine(FinishWandering());
            }

            // Reset state when complete.
            ResetActionState();
        }

        /// <summary>
        /// Attempts to find a random walkable target point.
        /// </summary>
        /// <returns>True if a valid target was found, false otherwise.</returns>
        private bool TryFindRandomWalkableTarget() {
            // Find a random walkable node nearby.
            Node targetNode = GetRandomWalkableNodeNearby(transform.position, wanderRadius);

            // Failed to find a target
            if (targetNode == null) {
                Debug.LogWarning("WanderAction.cs: Could not find suitable random walkable node nearby.");
                return false;
            }

            _currentWanderTarget = targetNode.Position;
            return true;
        }

        /// <summary>
        /// Moves the agent to the current wander target.
        /// </summary>
        /// <param name="success">Whether the move completed successfully.</param>
        private IEnumerator MoveToTarget(bool success) {
            success = false;

            // Start movement.
            _pathfindingAgent.FollowPath(_currentWanderTarget);
            float timeStartedMoving = Time.time;

            // Wait until reaching destination or timeout.
            while (Vector3.Distance(transform.position, _currentWanderTarget) > arrivalDistance) {
                // Check if movement failed or agent is stuck.
                if (HasMovementFailed()) {
                    yield return new WaitForSeconds(0.1f);

                    // Double-check to avoid false positives.
                    if (HasMovementFailed()) {
                        Debug.LogWarning(
                            $"WanderAction.cs: Agent stopped following path but hasn't reached target {_currentWanderTarget}. Current Pos: {transform.position}. Aborting move.");
                        yield break;
                    }
                }

                // Exit if taking too long.
                if (HasMovementTimedOut(timeStartedMoving)) {
                    Debug.LogWarning($"WanderAction: Movement to {_currentWanderTarget} timed out!");
                    yield break;
                }

                // Wait for the next frame.
                yield return null;
            }

            // Successfully reached destination.
            Debug.Log("WanderAction.cs: Reached destination.");
            success = true;
        }

        /// <summary>
        /// Checks if the agent has failed to follow the path.
        /// </summary>
        private bool HasMovementFailed() {
            return !_pathfindingAgent.IsFollowingPath() &&
                   Vector3.Distance(transform.position, _currentWanderTarget) > arrivalDistance;
        }

        /// <summary>
        /// Checks if the movement has exceeded the time limit.
        /// </summary>
        private bool HasMovementTimedOut(float startTime) {
            return Time.time > startTime + moveTimeout;
        }

        /// <summary>
        /// Finishes the wandering action with an optional wait period.
        /// </summary>
        private IEnumerator FinishWandering() {
            Debug.Log("WanderAction.cs: Waiting at destination.");

            // ? Wait at the destination.

            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);

            Debug.Log("WanderAction.cs: Finished.");
            yield return null;
        }

        /// <summary>
        /// Resets the action state after completion
        /// </summary>
        private void ResetActionState() {
            _currentWanderTarget = Vector3.zero;
            _actionIsRunning = false;
            // GoapAgent.cs will call ApplyEffectsToWorldState after this coroutine finishes.
        }

        #endregion

        #region Pathfinding Helpers

        /// <summary>
        /// Finds a random walkable node within a given radius.
        /// </summary>
        /// <param name="origin">Origin point to search from.</param>
        /// <param name="radius">Search radius.</param>
        /// <returns>A random walkable node or null if none found.</returns>
        private Node GetRandomWalkableNodeNearby(Vector3 origin, float radius) {
            if (_gridManager == null) return null;

            // Get the origin node.
            Node originNode = GetOriginNode(origin);
            if (originNode == null) return null;

            // Find nearby walkable nodes.
            List<Node> nearbyWalkableNodes = FindNearbyWalkableNodes(origin, radius, originNode);

            // Select a random node from the list.
            return SelectRandomNodeFromList(nearbyWalkableNodes);
        }

        /// <summary>
        /// Gets the node at the agent's current position.
        /// </summary>
        private Node GetOriginNode(Vector3 origin) {
            Node originNode = _gridManager.GetNodeIndex(origin);

            if (originNode == null) {
                Debug.LogWarning("WanderAction.cs: Could not find node at agent's current position.");
            }

            return originNode;
        }

        /// <summary>
        /// Finds all walkable nodes within the given radius.
        /// </summary>
        private List<Node> FindNearbyWalkableNodes(Vector3 origin, float radius, Node originNode) {
            List<Node> nearbyWalkableNodes = new List<Node>();

            // Iterate through grid nodes and check distance + walkability.
            foreach (Node node in _gridManager.Nodes) {
                if (IsNodeValidForWandering(node, origin, radius, originNode)) {
                    nearbyWalkableNodes.Add(node);
                }
            }

            return nearbyWalkableNodes;
        }

        /// <summary>
        /// Checks if a node is valid for wandering (walkable, within radius, not current position).
        /// </summary>
        private bool IsNodeValidForWandering(Node node, Vector3 origin, float radius, Node originNode) {
            return node.Walkable &&
                   Vector3.Distance(origin, node.Position) <= radius &&
                   node.Index != originNode.Index;
        }

        /// <summary>
        /// Selects a random node from the list of walkable nodes.
        /// </summary>
        private Node SelectRandomNodeFromList(List<Node> nodes) {
            if (nodes.Count > 0) {
                int randomIndex = Random.Range(0, nodes.Count);
                return nodes[randomIndex];
            }

            return null;
        }

        #endregion
    }
}