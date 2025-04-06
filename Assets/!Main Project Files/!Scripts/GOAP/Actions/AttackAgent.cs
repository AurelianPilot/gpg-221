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
        
        private TeamAgent teamAgent;
        private TeamAgent targetAgent;
        private GameManager gameManager;
        private bool canAttack = true;
        
        private void Awake()
        {
            actionName = "AttackAgent";
            isActionAchivable = true;
            
            if (preRequisites.Count == 0)
            {
                AddPreRequisite("IsDead", false);
            }
            
            if (effects.Count == 0)
            {
                AddEffect(enemyEliminatedStateName, true);
            }
            teamAgent = GetComponent<TeamAgent>();
            gameManager = FindObjectOfType<GameManager>();
        }

        protected override IEnumerator PerformAction()
        {
            Debug.Log($"[ACTION] AttackAgent.cs: {gameObject.name} is looking for enemies to attack.");
            
            // Find closest enemy agent.
            targetAgent = FindClosestEnemyAgent();
            
            if (targetAgent == null || targetAgent.IsDead)
            {
                Debug.Log("[ACTION] AttackAgent.cs: No valid enemy found to attack.");
                yield break;
            }
            
            // Set agent state.
            teamAgent.SetAgentState(AgentState.Attacking);
            
            // Chase and attack the enemy until they're eliminated or max iterations reached.
            int maxIterations = 300;
            int currentIteration = 0;
            
            while (currentIteration < maxIterations && targetAgent != null && !targetAgent.IsDead)
            {
                // Move to the enemy position
                Vector3 enemyPosition = targetAgent.transform.position;
                Agent pathfindingAgent = teamAgent?.GetPathfindingAgent();
                
                if (pathfindingAgent != null)
                {
                    // Only update path periodically to prevent jittering.
                    if (currentIteration % 30 == 0)
                    {
                        pathfindingAgent.FollowPath(enemyPosition);
                        Debug.Log($"[ACTION] AttackAgent.cs: Chasing enemy at {enemyPosition}");
                    }
                }
                
                // Check if agent is close enough to attack.
                float distanceToEnemy = Vector3.Distance(transform.position, enemyPosition);
                
                if (distanceToEnemy <= attackRange && canAttack)
                {
                    yield return StartCoroutine(PerformAttack());
                }
                
                currentIteration++;
                yield return null;
                
                // Re-check if target is still valid (in case multiple of them attack at once).
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
            
            // Kill target (yes they're all one hit kills because of time restraints).
            targetAgent.Die();
            
            if (gameManager != null)
            {
                gameManager.OnAgentKilled(targetAgent, teamAgent);
            }
            
            // Attack cooldown? (just in case).
            yield return new WaitForSeconds(attackCooldown);
            
            canAttack = true;
        }
        
        
        private TeamAgent FindClosestEnemyAgent()
        {
            if (teamAgent == null) return null;
            
            // Check for agents in range.
            Collider[] agentColliders = Physics.OverlapSphere(transform.position, detectionRadius, agentLayer);
            TeamAgent closestEnemy = null;
            float closestDistance = float.MaxValue;
            
            foreach (Collider col in agentColliders)
            {
                TeamAgent potentialTarget = col.GetComponentInParent<TeamAgent>();
                
                // Check if it's a valid enemy (different team and not dead).
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
            
            // If no enemies in immediate range, try to get one from the game managr as in trying to force an attack because its funny.
            if (closestEnemy == null && gameManager != null)
            {
                float minDistance = float.MaxValue;
                
                // Check all teams.
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
        
    }
}