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
        [Tooltip(
            "How close the agent needs to be to the target to be considered 'in range'. Should match or be slightly less than AttackAction's range.")]
        [SerializeField]
        private float targetRange = 1.8f;

        [Tooltip("How close the agent must be to the final path point to count as arrived.")] [SerializeField]
        private float arrivalDistance = 0.5f;

        [Tooltip("Maximum time allowed for the movement attempt.")] [SerializeField]
        private float moveTimeout = 20f;

        private GladiatorAgent _gladiatorAgent;
        private PathFindingAgent _pathfindingAgent;
        //private AgentWorldState _agentWorldState;

        private GladiatorAgent _currentTargetEnemy;
        private bool _actionIsRunning;

        /// <summary>
        /// Gets component references.
        /// </summary>
        protected override void Awake() {
            base.Awake();
            _gladiatorAgent = GetComponent<GladiatorAgent>();
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
            AddPrerequisite(WorldStateKey.HasEnemyTarget, true);
        }

        /// <summary>
        /// Sets the effects this action has on the world state upon successful completion.
        /// </summary>
        protected override void SetUpEffects() {
            AddEffect(WorldStateKey.IsInAttackRange, true);
        }

        /// <summary>
        /// Checks dynamic conditions immediately before execution. Validates the assigned target enemy.
        /// </summary>
        public override bool CheckProceduralPreconditions() {
            if (_actionIsRunning) return false;

            GladiatorAgent currentTarget = _gladiatorAgent.CurrentTargetEnemy;
            bool targetIsValid = currentTarget != null && !currentTarget.GetComponent<AgentHealthSystem>().IsDead;

            AgentWorldState.SetState(WorldStateKey.HasEnemyTarget, targetIsValid);

            bool canPathfind = _pathfindingAgent != null && _pathfindingAgent.astar != null;
            if (!canPathfind && targetIsValid)
                Debug.LogError(
                    $"MoveToEnemyAction on {gameObject.name}: Pathfinding setup invalid but has valid target!");

            return targetIsValid && canPathfind;
        }

        /// <summary>
        /// Executes the movement logic towards the target enemy.
        /// </summary>
        public override IEnumerator PerformAction() {
            // Use the target assigned by FindEnemyAction
            GladiatorAgent targetEnemy = _gladiatorAgent.CurrentTargetEnemy;

            if (targetEnemy == null) {
                Debug.LogWarning($"MoveToEnemyAction ({gameObject.name}): Target was null when PerformAction started.");
                _actionIsRunning = false;
                AgentWorldState.SetState(WorldStateKey.IsInAttackRange, false);
                yield break;
            }

            _actionIsRunning = true;
            Debug.Log($"MoveToEnemyAction ({gameObject.name}): Starting movement towards {targetEnemy.name}");

            float timeStartedMoving = Time.time;
            Vector3 lastTargetPosition = targetEnemy.transform.position;

            // Initial path calculation.
            _pathfindingAgent.FollowPath(lastTargetPosition);

            while (_actionIsRunning) {
                targetEnemy = _gladiatorAgent.CurrentTargetEnemy;
                if (targetEnemy == null || targetEnemy.GetComponent<AgentHealthSystem>().IsDead) {
                    Debug.Log(
                        $"MoveToEnemyAction ({gameObject.name}): Target {targetEnemy?.name ?? "NULL"} is dead or lost. Aborting move.");
                    _actionIsRunning = false;
                    AgentWorldState.SetState(WorldStateKey.HasEnemyTarget, false);
                    AgentWorldState.SetState(WorldStateKey.IsInAttackRange, false);
                    yield break;
                }

                Vector3 currentTargetPos = targetEnemy.transform.position;
                float distanceToTarget = Vector3.Distance(transform.position, currentTargetPos);

                // If we're in range, stop moving and mark success.
                if (distanceToTarget <= targetRange) {
                    Debug.Log(
                        $"MoveToEnemyAction ({gameObject.name}): Reached target range ({distanceToTarget:F2}m <= {targetRange}m). Stopping.");
                    _actionIsRunning = false;
                    yield break;
                }

                // Check if movement failed.
                if (!_pathfindingAgent.IsFollowingPath() &&
                    Vector3.Distance(transform.position, _pathfindingAgent.GetTargetPosition()) > arrivalDistance) {
                    yield return new WaitForSeconds(0.1f);
                    if (!_pathfindingAgent.IsFollowingPath() &&
                        Vector3.Distance(transform.position, _pathfindingAgent.GetTargetPosition()) > arrivalDistance) {
                        Debug.LogWarning(
                            $"MoveToEnemyAction ({gameObject.name}): Pathfollowing stopped unexpectedly far from waypoint. Aborting move.");
                        _actionIsRunning = false;
                        AgentWorldState.SetState(WorldStateKey.IsInAttackRange, false);
                        yield break;
                    }
                }

                // Check for timeout.
                if (Time.time > timeStartedMoving + moveTimeout) {
                    Debug.LogWarning(
                        $"MoveToEnemyAction ({gameObject.name}): Movement towards {targetEnemy.name} timed out!");
                    _actionIsRunning = false;
                    AgentWorldState.SetState(WorldStateKey.IsInAttackRange, false);
                    yield break;
                }

                // Constantly updates the path to follow the moving target (this fixes some issues guys don't remove it, in any case it might need optimization).
                // ? Only recalculate if target has moved significantly () to avoid performance issues.
                if (Vector3.Distance(currentTargetPos, lastTargetPosition) > 0.01f) {
                    Debug.Log(
                        $"MoveToEnemyAction ({gameObject.name}): Target moved significantly, recalculating path.");
                    _pathfindingAgent.FollowPath(currentTargetPos);
                    lastTargetPosition = currentTargetPos;
                }

                yield return null;
            }

            _actionIsRunning = false;
            targetEnemy = _gladiatorAgent.CurrentTargetEnemy;
            float finalDist = Vector3.Distance(transform.position,
                targetEnemy != null ? targetEnemy.transform.position : Vector3.positiveInfinity);
            AgentWorldState.SetState(WorldStateKey.IsInAttackRange, finalDist <= targetRange);
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