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
        private AgentWorldState _agentWorldState;
        private GladiatorAgent _targetEnemy;
        private float _lastAttackTime = -999f;

        protected override void Awake()
        {
            base.Awake();
            _healthSystem = GetComponent<AgentHealthSystem>();
            _gladiatorAgent = GetComponent<GladiatorAgent>();
            _agentWorldState = GetComponent<AgentWorldState>();
        }

        protected override void SetUpPreRequisites()
        {
            AddPrerequisite(WorldStateKey.EnemyDetected, true);
            AddPrerequisite(WorldStateKey.IsInAttackRange, true);
        }

        protected override void SetUpEffects()
        {
            AddEffect(WorldStateKey.IsInCombat, true);
        }

        public override bool CheckProceduralPreconditions()
        {
            _targetEnemy = FindBestEnemyTarget();
            if (_targetEnemy == null)
            {
                _agentWorldState.SetState(WorldStateKey.HasEnemyTarget, false);
                _agentWorldState.SetState(WorldStateKey.IsInAttackRange, false);
                return false;
            }
            
            // ! There is a target.
            _agentWorldState.SetState(WorldStateKey.HasEnemyTarget, true);

            // Range check.
            bool inRange = Vector3.Distance(transform.position, _targetEnemy.transform.position) <= attackRange;
            _agentWorldState.SetState(WorldStateKey.IsInAttackRange, inRange); 

            AgentHealthSystem targetHealth = _targetEnemy.GetComponent<AgentHealthSystem>();
            bool targetAlive = targetHealth != null && !targetHealth.IsDead;

            return inRange && targetAlive;
        }

        public override IEnumerator PerformAction()
        {
            if (_targetEnemy == null || Time.time < _lastAttackTime + attackCooldown)
            {
                yield break;
            }

            AgentHealthSystem targetHealth = _targetEnemy.GetComponent<AgentHealthSystem>();
            if (targetHealth != null && !targetHealth.IsDead)
            {
                Debug.Log($"[Attack Action] {gameObject.name} attacking {_targetEnemy.name}");
                float attackPower = _healthSystem != null ? _healthSystem.GetAttackPower() : defaultDamage;
                targetHealth.TakeDamage(attackPower, gameObject);
                _lastAttackTime = Time.time;

                // Check if target died after attack.
                if (targetHealth.IsDead)
                {
                     Debug.Log($"[Attack Action] {gameObject.name} defeated {_targetEnemy.name}");
                     // ? I'm not sure if to update world state immediately, or let individual perceptions handle it.
                     // _agentWorldState.SetState(WorldStateKey.EnemyDetected, false);
                     // _agentWorldState.SetState(WorldStateKey.HasEnemyTarget, false);
                     // _agentWorldState.SetState(WorldStateKey.IsInAttackRange, false);
                     _targetEnemy = null;
                }
            }
            else
            {
                 // ? Target might have died before attack landed by other agent.
                 _targetEnemy = null;
                 _agentWorldState.SetState(WorldStateKey.HasEnemyTarget, false);
                 _agentWorldState.SetState(WorldStateKey.IsInAttackRange, false);
            }

            yield return new WaitForSeconds(0.1f);
        }

        private GladiatorAgent FindBestEnemyTarget()
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
         }
    }
}