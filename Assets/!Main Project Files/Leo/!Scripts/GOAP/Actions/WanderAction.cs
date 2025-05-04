using System.Collections;
using System.Collections.Generic;
using _Main_Project_Files.Leo._Scripts.Pathfinding;
using UnityEngine;

namespace _Main_Project_Files.Leo._Scripts.GOAP.Actions
{
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
        
        [Header("- Cooldown Settings")]
        [Tooltip("Base cooldown between wander actions in seconds")] [SerializeField]
        private float baseCooldownTime = 5f;
        [Tooltip("Random variation added to cooldown (-randomVariation to +randomVariation)")] [SerializeField]
        private float randomVariation = 2f;

        #endregion

        #region Private Variables

        private PathFindingAgent _pathfindingAgent;
        private GladiatorAgent _gladiatorAgent;
        private GridManager _gridManager;
        private bool _actionIsRunning;
        private bool _isWanderingLoopRunning;
        private Vector3 _currentWanderTarget = Vector3.zero;
        private bool _moveWasSuccessful;

        #endregion

        #region Initialization

        protected override void Awake() {
            base.Awake();
            InitializeComponents();
        }

        private void InitializeComponents() {
            InitializePathfindingAgent();
            InitializeGridManager();
            _gladiatorAgent = GetComponent<GladiatorAgent>();
        }

        private void InitializePathfindingAgent() {
            _pathfindingAgent = GetComponent<PathFindingAgent>();
            if (_pathfindingAgent == null) {
                Debug.LogError($"WanderAction.cs: ({gameObject.name}): Agent component not found!");
            }

            if (_pathfindingAgent != null && _pathfindingAgent.astar == null) {
                Debug.LogError(
                    $"WanderAction.cs: ({gameObject.name}): The Agent component's Astar reference is NOT set!");
            }
        }

        private void InitializeGridManager() {
            _gridManager = FindObjectOfType<GridManager>();
            if (_gridManager == null) {
                Debug.LogError($"WanderAction.cs: ({gameObject.name}): GridManager not found in scene!");
            }
        }

        #endregion

        #region GOAP Configuration

        protected override void SetUpPreRequisites() {
        }

        protected override void SetUpEffects() {
            AddEffect(WorldStateKey.IsWandering, true);
        }

        public override bool CheckProceduralPreconditions() {
            if (_isWanderingLoopRunning)
                return false;
                
            return HasValidPathfindingSetup() && !_actionIsRunning;
        }

        private bool HasValidPathfindingSetup() {
            return _pathfindingAgent != null &&
                   _pathfindingAgent.astar != null &&
                   _gridManager != null;
        }

        #endregion

        #region Action Execution

        public override IEnumerator PerformAction() {
            if (!CheckProceduralPreconditions()) {
                Debug.LogWarning($"WanderAction.cs: Procedural precondition failed.");
                yield break;
            }

            _isWanderingLoopRunning = true;
            Debug.Log($"[WanderAction] {gameObject.name}: Starting wandering loop");
            
            while (_isWanderingLoopRunning) {
                if (!CanContinueWandering()) {
                    Debug.Log($"[WanderAction] {gameObject.name}: Wandering conditions no longer valid");
                    break;
                }
                
                if (!TryFindRandomWalkableTarget()) {
                    Debug.LogWarning($"[WanderAction] {gameObject.name}: Could not find walkable target, waiting before retry");
                    yield return new WaitForSeconds(1f);
                    continue;
                }
                
                _actionIsRunning = true;
                _moveWasSuccessful = false;
                yield return StartCoroutine(MoveToTarget());
                
                if (_moveWasSuccessful) {
                    Debug.Log($"[WanderAction] {gameObject.name}: Reached destination, waiting");
                    float waitTime = Random.Range(minWaitTime, maxWaitTime);
                    yield return new WaitForSeconds(waitTime);
                    
                    float randomOffset = Random.Range(-randomVariation, randomVariation);
                    float actualCooldown = baseCooldownTime + randomOffset;
                    actualCooldown = Mathf.Max(0.5f, actualCooldown);
                    
                    Debug.Log($"[WanderAction] {gameObject.name}: Cooldown for {actualCooldown:F1} seconds before next wander");
                    yield return new WaitForSeconds(actualCooldown);
                }
                
                _actionIsRunning = false;
            }
            
            _isWanderingLoopRunning = false;
            _actionIsRunning = false;
            Debug.Log($"[WanderAction] {gameObject.name}: Wandering loop completed");
        }

        private bool CanContinueWandering() {
            if (_gladiatorAgent != null && 
                _gladiatorAgent.agentRole == GladiatorAgent.AgentRole.Warrior && 
                _gladiatorAgent.knownEnemies.Count > 0) {
                
                bool hasAliveEnemy = false;
                foreach (var enemy in _gladiatorAgent.knownEnemies) {
                    if (enemy != null) {
                        var enemyHealth = enemy.GetComponent<Status_Systems.AgentHealthSystem>();
                        if (enemyHealth != null && !enemyHealth.IsDead) {
                            hasAliveEnemy = true;
                            break;
                        }
                    }
                }
                
                if (hasAliveEnemy) {
                    Debug.Log($"[WanderAction] {gameObject.name}: Detected enemies while wandering, stopping wander");
                    return false;
                }
            }
            
            if (!HasValidPathfindingSetup()) {
                Debug.LogWarning($"[WanderAction] {gameObject.name}: Pathfinding setup became invalid");
                return false;
            }
            
            if (_gladiatorAgent != null && _gladiatorAgent.GetCurrentGoal()?.GoalName != "WanderGoal") {
                Debug.Log($"[WanderAction] {gameObject.name}: Goal has changed, stopping wander");
                return false;
            }
            
            return true;
        }

        private bool TryFindRandomWalkableTarget() {
            Node targetNode = GetRandomWalkableNodeNearby(transform.position, wanderRadius);

            if (targetNode == null) {
                Debug.LogWarning($"[WanderAction] {gameObject.name}: Could not find suitable walkable node");
                return false;
            }

            _currentWanderTarget = targetNode.Position;
            return true;
        }

        private IEnumerator MoveToTarget() {
            _moveWasSuccessful = false;
            _pathfindingAgent.FollowPath(_currentWanderTarget);
            float timeStartedMoving = Time.time;
            Debug.Log($"[WanderAction] {gameObject.name}: Moving to {_currentWanderTarget}");

            while (Vector3.Distance(transform.position, _currentWanderTarget) > arrivalDistance) {
                if (!CanContinueWandering()) {
                    _isWanderingLoopRunning = false;
                    yield break;
                }

                if (HasMovementFailed()) {
                    yield return new WaitForSeconds(0.1f);
                    if (HasMovementFailed()) {
                        Debug.LogWarning($"[WanderAction] {gameObject.name}: Movement failed, path following stopped unexpectedly");
                        yield break;
                    }
                }

                if (HasMovementTimedOut(timeStartedMoving)) {
                    Debug.LogWarning($"[WanderAction] {gameObject.name}: Movement timed out after {moveTimeout} seconds");
                    yield break;
                }

                yield return null;
            }

            Debug.Log($"[WanderAction] {gameObject.name}: Successfully reached destination");
            _moveWasSuccessful = true;
        }

        private bool HasMovementFailed() {
            return !_pathfindingAgent.IsFollowingPath() &&
                   Vector3.Distance(transform.position, _currentWanderTarget) > arrivalDistance;
        }

        private bool HasMovementTimedOut(float startTime) {
            return Time.time > startTime + moveTimeout;
        }

        #endregion

        #region Pathfinding Helpers

        private Node GetRandomWalkableNodeNearby(Vector3 origin, float radius) {
            if (_gridManager == null) return null;

            Node originNode = GetOriginNode(origin);
            if (originNode == null) return null;

            List<Node> nearbyWalkableNodes = FindNearbyWalkableNodes(origin, radius, originNode);
            return SelectRandomNodeFromList(nearbyWalkableNodes);
        }

        private Node GetOriginNode(Vector3 origin) {
            Node originNode = _gridManager.GetNodeIndex(origin);
            if (originNode == null) {
                Debug.LogWarning($"[WanderAction] {gameObject.name}: Could not find node at current position");
            }
            return originNode;
        }

        private List<Node> FindNearbyWalkableNodes(Vector3 origin, float radius, Node originNode) {
            List<Node> nearbyWalkableNodes = new List<Node>();
            foreach (Node node in _gridManager.Nodes) {
                if (IsNodeValidForWandering(node, origin, radius, originNode)) {
                    nearbyWalkableNodes.Add(node);
                }
            }
            return nearbyWalkableNodes;
        }

        private bool IsNodeValidForWandering(Node node, Vector3 origin, float radius, Node originNode) {
            return node.Walkable &&
                   Vector3.Distance(origin, node.Position) <= radius &&
                   node.Index != originNode.Index;
        }

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