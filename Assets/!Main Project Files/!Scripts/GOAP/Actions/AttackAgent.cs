using System.Collections;
using System.Collections.Generic;
using _Main_Project_Files._Scripts.Agents;
using _Main_Project_Files._Scripts.Pathfinding;
using UnityEngine;

namespace _Main_Project_Files._Scripts.GOAP.Actions
{
    public class AttackAgent : Action
    {
        [Header("- Attack Settings")]
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private string enemyEliminatedStateName = "EnemyEliminated";
        [SerializeField] private LayerMask agentLayer;
        [SerializeField] private float detectionRadius = 20f;

        [SerializeField] private GameObject attackEffectPrefab;
        [SerializeField] private float effectDuration = 0.5f;

        private TeamAgent targetAgent;
        private bool canAttack = true;

        protected override void Awake()
        {
            base.Awake();

            actionName = "AttackAgent";
            isActionAchivable = true;
            
            if (preRequisites.Count == 0)
            {
                // Agent must be alive.
                AddPreRequisite("IsDead", false);
            }

            if (effects.Count == 0)
            {
                // This action eliminates an enemy.
                AddEffect(enemyEliminatedStateName, true);
            }
        }

        protected override IEnumerator PerformAction()
        {
            Debug.Log($"[ACTION] AttackAgent.cs: {gameObject.name} is looking for enemies to attack.");

            // Check if an enemy is around or pick from the game manager.
            targetAgent = FindClosestEnemyAgent();

            if (targetAgent == null || targetAgent.IsDead)
            {
                Debug.Log("[ACTION] AttackAgent.cs: No valid enemy found to attack.");
                yield break;
            }

            // Set agent state.
            teamAgent.SetAgentState(AgentState.Attacking);

            // A while loop to chase and attempt to kill the target.
            int maxIterations = 300;
            int currentIteration = 0;

            while (currentIteration < maxIterations && targetAgent != null && !targetAgent.IsDead)
            {
                Vector3 enemyPosition = targetAgent.transform.position;
                Agent pathfindingAgent = GetPathAgent();

                // Update path every so often to reduce jitter.
                if (pathfindingAgent != null && currentIteration % 30 == 0)
                {
                    pathfindingAgent.FollowPath(enemyPosition);
                    Debug.Log($"[ACTION] AttackAgent.cs: Chasing enemy at {enemyPosition}");
                }

                float distanceToEnemy = Vector3.Distance(transform.position, enemyPosition);

                if (distanceToEnemy <= attackRange && canAttack)
                {
                    yield return StartCoroutine(PerformAttack());
                }

                currentIteration++;
                yield return null;

                if (targetAgent == null || targetAgent.IsDead)
                {
                    Debug.Log("[ACTION] AttackAgent.cs: Target has been eliminated or is no longer valid.");
                    break;
                }
            }

            Debug.Log("[ACTION] AttackAgent.cs: Attack action completed.");
            teamAgent.SetAgentState(AgentState.Patrolling);
        }

        private IEnumerator PerformAttack()
        {
            canAttack = false;

            Debug.Log($"[ACTION] AttackAgent.cs: Attacking {targetAgent.gameObject.name}!");

            if (attackEffectPrefab != null)
            {
                GameObject effect = Instantiate(attackEffectPrefab,
                    targetAgent.transform.position + Vector3.up,
                    Quaternion.identity);

                Destroy(effect, effectDuration);
            }

            targetAgent.Die();
            if (CheckAllEnemiesDead(targetAgent.TeamColor))
            {
                teamAgent.WorldState.SetState("EnemiesDefeated", true);
            }
            teamAgent.WorldState.SetState("HasEnergy", true);

            if (gameManager != null)
            {
                gameManager.OnAgentKilled(targetAgent, teamAgent);
            }

            yield return new WaitForSeconds(attackCooldown);
            canAttack = true;
        }

        private TeamAgent FindClosestEnemyAgent()
        {
            if (teamAgent == null) return null;

            Collider[] agentColliders = Physics.OverlapSphere(transform.position, detectionRadius, agentLayer);
            TeamAgent closestEnemy = null;
            float closestDistance = float.MaxValue;

            foreach (Collider col in agentColliders)
            {
                TeamAgent potentialTarget = col.GetComponentInParent<TeamAgent>();

                if (potentialTarget != null &&
                    potentialTarget.TeamColor != teamAgent.TeamColor &&
                    !potentialTarget.IsDead)
                {
                    float distance = Vector3.Distance(transform.position, potentialTarget.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = potentialTarget;
                    }
                }
            }

            // If no enemies in immediate range, try to get one from the game manager.
            if (closestEnemy == null && gameManager != null)
            {
                float minDistance = float.MaxValue;

                foreach (TeamColor color in System.Enum.GetValues(typeof(TeamColor)))
                {
                    if (color == teamAgent.TeamColor) continue;

                    List<TeamAgent> enemyTeam = gameManager.GetTeamAgents(color);

                    foreach (TeamAgent enemy in enemyTeam)
                    {
                        if (enemy != null && !enemy.IsDead)
                        {
                            float distance = Vector3.Distance(transform.position, enemy.transform.position);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                closestEnemy = enemy;
                            }
                        }
                    }
                }
            }

            return closestEnemy;
        }

        private bool CheckAllEnemiesDead(TeamColor color)
        {
            if (gameManager == null) return false;
            List<TeamAgent> enemyTeam = gameManager.GetTeamAgents(color);
            foreach (TeamAgent agent in enemyTeam)
            {
                if (!agent.IsDead)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
