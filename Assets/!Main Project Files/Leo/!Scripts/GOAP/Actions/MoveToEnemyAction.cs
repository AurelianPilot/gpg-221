using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Main_Project_Files.Leo._Scripts.Pathfinding;
using _Main_Project_Files.Leo._Scripts.GOAP.Status_Systems;

namespace _Main_Project_Files.Leo._Scripts.GOAP.Actions
{
    [RequireComponent(typeof(GladiatorAgent))]
    [RequireComponent(typeof(AgentWorldState))]
    [RequireComponent(typeof(PathFindingAgent))]
    public class MoveToEnemyAction : GoapAction
    {
        [Header("- Movement Settings")]
        [Tooltip("How close the agent needs to be to the target to be considered 'in range'. Should match or be slightly less than AttackAction's range.")]
        [SerializeField]
        private float targetRange = 1.8f;

        [Tooltip("How close the agent must be to the final path point to count as arrived.")] [SerializeField]
        private float arrivalDistance = 0.5f;

        [Tooltip("Maximum time allowed for the movement attempt.")] [SerializeField]
        private float moveTimeout = 20f;

        private GladiatorAgent _gladiatorAgent;
        private PathFindingAgent _pathfindingAgent;
        private AgentWorldState _agentWorldState;

        private GladiatorAgent _currentTargetEnemy;
        private bool _actionIsRunning;

        /// <summary>
        /// Gets component references.
        /// </summary>
        protected override void Awake() {
            base.Awake();
            _gladiatorAgent = GetComponent<GladiatorAgent>();
            _agentWorldState = GetComponent<AgentWorldState>();
            _pathfindingAgent = GetComponent<PathFindingAgent>();

            if (_pathfindingAgent.astar == null) {
                Debug.LogError(
                    $"[MoveToEnemyAction]    ({gameObject.name}): PathFindingAgent's Astar reference is NOT set!");
            }
        }

        /// <summary>
        /// Sets the static prerequisites for this action.
        /// </summary>
        protected override void SetUpPreRequisites() {
            AddPrerequisite(WorldStateKey.EnemyDetected, true);
        }

        /// <summary>
        /// Sets the effects this action has on the world state upon successful completion.
        /// </summary>
        protected override void SetUpEffects() {
            AddEffect(WorldStateKey.IsInAttackRange, true);
        }

        /// <summary>
        /// Checks dynamic conditions immediately before execution. Finds and validates the target enemy.
        /// </summary>
        public override bool CheckProceduralPreconditions() {
            if (_actionIsRunning) return false;

            _currentTargetEnemy = FindBestEnemyTarget();
            if (_currentTargetEnemy == null) {
                _agentWorldState.SetState(WorldStateKey.HasEnemyTarget, false);
                return false;
            }

            _agentWorldState.SetState(WorldStateKey.HasEnemyTarget, true);

            bool canPathfind = _pathfindingAgent != null && _pathfindingAgent.astar != null;
            if (!canPathfind) Debug.LogError($"MoveToEnemyAction on {gameObject.name}: Pathfinding setup invalid.");

            return _currentTargetEnemy != null && canPathfind;
        }

        /// <summary>
        /// Executes the movement logic towards the target enemy.
        /// </summary>
        public override IEnumerator PerformAction() {
            if (_currentTargetEnemy == null) {
                Debug.LogWarning(
                    $"MoveToEnemyAction ({gameObject.name}): Target became null before PerformAction started.");
                yield break;
            }

            _actionIsRunning = true;
            Debug.Log($"MoveToEnemyAction ({gameObject.name}): Starting movement towards {_currentTargetEnemy.name}");

            float timeStartedMoving = Time.time;
            Vector3 lastTargetPosition = _currentTargetEnemy.transform.position;

            _pathfindingAgent.FollowPath(lastTargetPosition);

            while (_actionIsRunning) {
                if (_currentTargetEnemy == null || _currentTargetEnemy.GetComponent<AgentHealthSystem>().IsDead) {
                    Debug.Log(
                        $"MoveToEnemyAction ({gameObject.name}): Target {_currentTargetEnemy?.name ?? "NULL"} is dead or lost. Aborting move.");
                    _actionIsRunning = false;
                    _agentWorldState.SetState(WorldStateKey.HasEnemyTarget, false);
                    _agentWorldState.SetState(WorldStateKey.IsInAttackRange, false);
                    yield break;
                }

                Vector3 currentTargetPos = _currentTargetEnemy.transform.position;
                float distanceToTarget = Vector3.Distance(transform.position, currentTargetPos);

                if (distanceToTarget <= targetRange) {
                    Debug.Log(
                        $"MoveToEnemyAction ({gameObject.name}): Reached target range ({distanceToTarget:F2}m <= {targetRange}m). Stopping.");
                    _actionIsRunning = false;
                    yield break;
                }

                if (!_pathfindingAgent.IsFollowingPath() &&
                    Vector3.Distance(transform.position, _pathfindingAgent.GetTargetPosition()) > arrivalDistance) {
                    yield return new WaitForSeconds(0.1f);
                    if (!_pathfindingAgent.IsFollowingPath() &&
                        Vector3.Distance(transform.position, _pathfindingAgent.GetTargetPosition()) > arrivalDistance) {
                        Debug.LogWarning(
                            $"MoveToEnemyAction ({gameObject.name}): Pathfollowing stopped unexpectedly far from waypoint. Aborting move.");
                        _actionIsRunning = false;
                        _agentWorldState.SetState(WorldStateKey.IsInAttackRange, false);
                        yield break;
                    }
                }

                if (Time.time > timeStartedMoving + moveTimeout) {
                    Debug.LogWarning(
                        $"MoveToEnemyAction ({gameObject.name}): Movement towards {_currentTargetEnemy.name} timed out!");
                    _actionIsRunning = false;
                    _agentWorldState.SetState(WorldStateKey.IsInAttackRange, false);
                    yield break;
                }

                if (Vector3.Distance(currentTargetPos, lastTargetPosition) > 1.0f) {
                    _pathfindingAgent.FollowPath(currentTargetPos);
                    lastTargetPosition = currentTargetPos;
                }

                yield return null;
            }

            _actionIsRunning = false;
            float finalDist = Vector3.Distance(transform.position,
                _currentTargetEnemy != null ? _currentTargetEnemy.transform.position : Vector3.positiveInfinity);
            _agentWorldState.SetState(WorldStateKey.IsInAttackRange, finalDist <= targetRange);
            Debug.Log($"MoveToEnemyAction ({gameObject.name}): Finished. Final distance: {finalDist:F2}m");
        }

        /// <summary>
        /// Finds the closest, living enemy from the known enemies list.
        /// </summary>
        private GladiatorAgent FindBestEnemyTarget() {
            if (_gladiatorAgent == null || _gladiatorAgent.knownEnemies.Count == 0) return null;

            GladiatorAgent closestAliveEnemy = null;
            float minDistance = float.MaxValue;

            List<GladiatorAgent> currentKnownEnemies = new List<GladiatorAgent>(_gladiatorAgent.knownEnemies);

            foreach (var enemy in currentKnownEnemies) {
                if (enemy == null) continue;
                AgentHealthSystem enemyHealth = enemy.GetComponent<AgentHealthSystem>();
                if (enemyHealth == null || enemyHealth.IsDead) continue;

                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < minDistance) {
                    minDistance = dist;
                    closestAliveEnemy = enemy;
                }
            }

            return closestAliveEnemy;
        }
    }
}