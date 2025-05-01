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
            GladiatorAgent targetEnemy = _gladiatorAgent.CurrentTargetEnemy;

            if (targetEnemy == null || Time.time < _lastAttackTime + attackCooldown) {
                if (targetEnemy == null) AgentWorldState.SetState(WorldStateKey.HasEnemyTarget, false);
                yield break;
            }

            AgentHealthSystem targetHealth = targetEnemy.GetComponent<AgentHealthSystem>();
            if (targetHealth != null && !targetHealth.IsDead) {
                Debug.Log($"[Attack Action] {gameObject.name} attacking {targetEnemy.name}");
                float attackPower = _healthSystem != null ? _healthSystem.GetAttackPower() : defaultDamage;
                targetHealth.TakeDamage(attackPower, gameObject);
                _lastAttackTime = Time.time;

                if (targetHealth.IsDead) {
                    Debug.Log($"[Attack Action] {gameObject.name} defeated {targetEnemy.name}");
                    _gladiatorAgent.CurrentTargetEnemy = null;
                    AgentWorldState.SetState(WorldStateKey.HasEnemyTarget, false);
                    AgentWorldState.SetState(WorldStateKey.IsInAttackRange, false);
                }
            }
            else {
                if (targetEnemy != null)
                    Debug.LogWarning(
                        $"[Attack Action] {gameObject.name} target {targetEnemy.name} was dead or invalid before attack.");
                _gladiatorAgent.CurrentTargetEnemy = null;
                AgentWorldState.SetState(WorldStateKey.HasEnemyTarget, false);
                AgentWorldState.SetState(WorldStateKey.IsInAttackRange, false);
            }

            yield return new WaitForSeconds(0.1f);
        }

        /*private GladiatorAgent FindBestEnemyTarget()
        {
             if (_gladiatorAgent == null || _gladiatorAgent.knownEnemies.Count == 0) return null;

             GladiatorAgent closestAliveEnemy = null;
             float minDistance = float.MaxValue;

             List<GladiatorAgent> currentKnownEnemies = new List<GladiatorAgent>(_gladiatorAgent.knownEnemies);

             foreach(var enemy in currentKnownEnemies)
             {
                 if (enemy == null) continue;

                 AgentHealthSystem enemyHealth = enemy.GetComponent<AgentHealthSystem>();
                 if (enemyHealth == null || enemyHealth.IsDead) continue;

                 float dist = Vector3.Distance(transform.position, enemy.transform.position);
                 if (dist < minDistance)
                 {
                     minDistance = dist;
                     closestAliveEnemy = enemy;
                 }
             }
             return closestAliveEnemy;
         }*/
    }
}