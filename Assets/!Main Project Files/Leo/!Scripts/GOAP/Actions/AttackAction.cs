using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Main_Project_Files.Leo._Scripts.GOAP.Status_Systems;

namespace _Main_Project_Files.Leo._Scripts.GOAP.Actions
{
    [RequireComponent(typeof(AgentHealthSystem))]
    [RequireComponent(typeof(GladiatorAgent))]
    [RequireComponent(typeof(AgentWorldState))]
    public class AttackAction : GoapAction
    {
        [Header("- Attack Settings")]
        [SerializeField] private float attackRange = 2f;

        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private int defaultDamage = 10;

        private AgentHealthSystem _healthSystem;
        private GladiatorAgent _gladiatorAgent;
        private float _lastAttackTime = -999f;
        private bool _isAttackLoopRunning = false;

        protected override void Awake() {
            base.Awake();
            _healthSystem = GetComponent<AgentHealthSystem>();
            _gladiatorAgent = GetComponent<GladiatorAgent>();
        }

        protected override void SetUpPreRequisites() {
            AddPrerequisite(WorldStateKey.EnemyDetected, true);
            AddPrerequisite(WorldStateKey.HasEnemyTarget, true);
            AddPrerequisite(WorldStateKey.IsInAttackRange, true);
        }

        protected override void SetUpEffects() {
            AddEffect(WorldStateKey.IsInCombat, true);
        }

        public override bool CheckProceduralPreconditions() {
            // Don't start a new attack action if one is already running.
            if (_isAttackLoopRunning)
                return false;

            GladiatorAgent currentTarget = _gladiatorAgent.CurrentTargetEnemy;
            bool targetIsValid = currentTarget != null && !currentTarget.GetComponent<AgentHealthSystem>().IsDead;

            AgentWorldState.SetState(WorldStateKey.HasEnemyTarget, targetIsValid);

            bool inRange = false;
            if (targetIsValid) {
                inRange = Vector3.Distance(transform.position, currentTarget.transform.position) <= attackRange;
            }

            AgentWorldState.SetState(WorldStateKey.IsInAttackRange, inRange);
            return targetIsValid && inRange;
        }

        public override IEnumerator PerformAction() {
            // Start the attack loop.
            _isAttackLoopRunning = true;

            Debug.Log($"[Attack Action] {gameObject.name}: Starting attack loop");

            // Continue attacking until something stops us.
            while (_isAttackLoopRunning) {
                // Verify we still have a valid target before each attack.
                if (!CanContinueAttacking()) {
                    Debug.Log(
                        $"[Attack Action] {gameObject.name}: Attack conditions no longer valid, stopping attack loop");
                    break;
                }

                // Wait for cooldown if needed.
                float timeUntilCanAttack = (_lastAttackTime + attackCooldown) - Time.time;
                if (timeUntilCanAttack > 0) {
                    yield return new WaitForSeconds(timeUntilCanAttack);
                }

                // Perform the actual attack.
                yield return StartCoroutine(ExecuteSingleAttack());
            }

            // Clean up when the loop ends.
            _isAttackLoopRunning = false;
            Debug.Log($"[Attack Action] {gameObject.name}: Attack loop completed");
        }

        /// <summary>
        /// Check if we can continue attacking.
        /// </summary>
        /// <returns></returns>
        private bool CanContinueAttacking() {
            GladiatorAgent targetEnemy = _gladiatorAgent.CurrentTargetEnemy;

            // No target to attack.
            if (targetEnemy == null) {
                AgentWorldState.SetState(WorldStateKey.HasEnemyTarget, false);
                return false;
            }

            // Target health check.
            AgentHealthSystem targetHealth = targetEnemy.GetComponent<AgentHealthSystem>();
            if (targetHealth == null || targetHealth.IsDead) {
                Debug.Log(
                    $"[Attack Action] {gameObject.name}: Target {targetEnemy.name} is dead or has no health component");
                _gladiatorAgent.CurrentTargetEnemy = null;
                AgentWorldState.SetState(WorldStateKey.HasEnemyTarget, false);
                return false;
            }

            // Range check.
            float distanceToTarget = Vector3.Distance(transform.position, targetEnemy.transform.position);
            if (distanceToTarget > attackRange) {
                Debug.Log(
                    $"[Attack Action] {gameObject.name}: Target {targetEnemy.name} moved out of range ({distanceToTarget:F2} > {attackRange:F2})");

                // ! Important: Update the world state to reflect we're not in attack range.
                AgentWorldState.SetState(WorldStateKey.IsInAttackRange, false);

                // Force immediate replanning by aborting the current plan.
                _gladiatorAgent.AbortCurrentPlan();

                // Make sure the enemy is still a valid target.
                if (!targetHealth.IsDead) {
                    // Maintain that we have a target, but need to get back in range.
                    AgentWorldState.SetState(WorldStateKey.HasEnemyTarget, true);
                    AgentWorldState.SetState(WorldStateKey.EnemyDetected, true);

                    // Directly trigger MoveToEnemy to follow target instead of waiting for planning.
                    StartCoroutine(TriggerMoveToEnemyAction());
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Helper method to immediately trigger a move action when out of range.
        /// </summary>
        /// <returns></returns>
        private IEnumerator TriggerMoveToEnemyAction() {
            // Give a small delay to allow AbortCurrentPlan to complete.
            yield return new WaitForSeconds(0.1f);

            // Find the MoveToEnemyAction component.
            MoveToEnemyAction moveAction = GetComponent<MoveToEnemyAction>();

            if (moveAction != null && moveAction.CheckProceduralPreconditions()) {
                Debug.Log($"[Attack Action] {gameObject.name}: Directly triggering MoveToEnemyAction to follow target");

                // Manually start the move action.
                yield return StartCoroutine(moveAction.PerformAction());

                // After moving is complete, check if attacking is now possible.
                if (CheckProceduralPreconditions()) {
                    // Resume attacking.
                    _isAttackLoopRunning = true;
                    StartCoroutine(PerformAction());
                }
            }
            else {
                Debug.LogWarning($"[Attack Action] {gameObject.name}: Could not find or validate MoveToEnemyAction");
            }
        }

        // Executes a single attack in the loop.
        private IEnumerator ExecuteSingleAttack() {
            GladiatorAgent targetEnemy = _gladiatorAgent.CurrentTargetEnemy;
            AgentHealthSystem targetHealth = targetEnemy.GetComponent<AgentHealthSystem>();

            Debug.Log($"[Attack Action] {gameObject.name} attacking {targetEnemy.name}");
            float attackPower = _healthSystem != null ? _healthSystem.GetAttackPower() : defaultDamage;
            targetHealth.TakeDamage(attackPower, gameObject);
            _lastAttackTime = Time.time;

            // Check if the target died from this attack.
            if (targetHealth.IsDead) {
                Debug.Log($"[Attack Action] {gameObject.name} defeated {targetEnemy.name}");
                _gladiatorAgent.CurrentTargetEnemy = null;
                AgentWorldState.SetState(WorldStateKey.HasEnemyTarget, false);
                AgentWorldState.SetState(WorldStateKey.IsInAttackRange, false);
                _isAttackLoopRunning = false;
            }
            else {
                Debug.Log(
                    $"[Attack Action] {gameObject.name} will continue attacking {targetEnemy.name}, current health: {targetHealth.CurrentHealth:F1}");
            }

            // Small delay for better debugging and to avoid tight loops.
            yield return new WaitForSeconds(0.1f);
        }
    }
}