using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Main_Project_Files.Leo._Scripts.GOAP.Status_Systems;

namespace _Main_Project_Files.Leo._Scripts.GOAP.Actions
{
    /// <summary>
    /// Action for attacking enemy targets within a specified range.
    /// Handles attack execution, cooldown management and coordination with other actions.
    /// </summary>
    [RequireComponent(typeof(AgentHealthSystem))]
    [RequireComponent(typeof(GladiatorAgent))]
    [RequireComponent(typeof(AgentWorldState))]
    public class AttackAction : GoapAction
    {
        #region Settings
        
        [Header("- Attack Settings")]
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private int defaultDamage = 10;
        
        #endregion

        #region Private Variables
        
        private AgentHealthSystem _healthSystem;
        private GladiatorAgent _gladiatorAgent;
        private float _lastAttackTime = -999f;
        private bool _isAttackLoopRunning = false;
        
        #endregion

        #region Initialization
        
        /// <summary>
        /// Initializes required components.
        /// </summary>
        protected override void Awake() 
        {
            base.Awake();
            _healthSystem = GetComponent<AgentHealthSystem>();
            _gladiatorAgent = GetComponent<GladiatorAgent>();
        }
        
        #endregion

        #region GOAP Configuration
        
        /// <summary>
        /// Sets up the prerequisites for this action.
        /// </summary>
        protected override void SetUpPreRequisites() 
        {
            AddPrerequisite(WorldStateKey.EnemyDetected, true);
            AddPrerequisite(WorldStateKey.HasEnemyTarget, true);
            AddPrerequisite(WorldStateKey.IsInAttackRange, true);
        }

        /// <summary>
        /// Sets up the effects of this action.
        /// </summary>
        protected override void SetUpEffects() 
        {
            AddEffect(WorldStateKey.IsInCombat, true);
        }

        /// <summary>
        /// Checks procedural preconditions immediately before execution.
        /// </summary>
        /// <returns>True if the action can proceed, false otherwise</returns>
        public override bool CheckProceduralPreconditions() 
        {
            // Don't start a new attack action if one is already running.
            if (_isAttackLoopRunning)
                return false;

            // Validate target existence and health.
            GladiatorAgent currentTarget = _gladiatorAgent.CurrentTargetEnemy;
            bool targetIsValid = IsTargetValid(currentTarget);

            AgentWorldState.SetState(WorldStateKey.HasEnemyTarget, targetIsValid);

            // Check if target is in range.
            bool inRange = false;
            if (targetIsValid) 
            {
                inRange = IsTargetInAttackRange(currentTarget);
            }

            AgentWorldState.SetState(WorldStateKey.IsInAttackRange, inRange);
            return targetIsValid && inRange;
        }
        
        #endregion

        #region Action Execution
        
        /// <summary>
        /// Performs the attack action - main execution coroutine.
        /// </summary>
        /// <returns>IEnumerator for coroutine</returns>
        public override IEnumerator PerformAction() 
        {
            // Start the attack loop.
            _isAttackLoopRunning = true;
            Debug.Log($"[Attack Action] {gameObject.name}: Starting attack loop");

            // Continue attacking until something stops us.
            while (_isAttackLoopRunning) 
            {
                // Verify we still have a valid target before each attack.
                if (!CanContinueAttacking()) 
                {
                    Debug.Log($"[Attack Action] {gameObject.name}: Attack conditions no longer valid, stopping attack loop");
                    break;
                }

                // Wait for cooldown if needed
                float timeUntilCanAttack = (_lastAttackTime + attackCooldown) - Time.time;
                if (timeUntilCanAttack > 0) 
                {
                    yield return new WaitForSeconds(timeUntilCanAttack);
                }

                // Re-check target validity right before attacking.
                // This handles the case where an ally killed the enemy during cooldown.
                if (!CanContinueAttacking()) 
                {
                    Debug.Log($"[Attack Action] {gameObject.name}: Target invalidated during cooldown, stopping attack");
                    break;
                }

                // Perform the actual attack.
                yield return StartCoroutine(ExecuteSingleAttack());
            }

            // Clean up when the loop ends.
            _isAttackLoopRunning = false;
            Debug.Log($"[Attack Action] {gameObject.name}: Attack loop completed");
        }

        /// <summary>
        /// Executes a single attack in the attack loop.
        /// </summary>
        /// <returns>IEnumerator for coroutine</returns>
        private IEnumerator ExecuteSingleAttack() 
        {
            GladiatorAgent targetEnemy = _gladiatorAgent.CurrentTargetEnemy;
            
            // Final null check before damage.
            if (targetEnemy == null) 
            {
                AbortAttackLoop();
                yield break;
            }
            
            AgentHealthSystem targetHealth = targetEnemy.GetComponent<AgentHealthSystem>();
            if (targetHealth == null) 
            {
                AbortAttackLoop();
                yield break;
            }

            Debug.Log($"[Attack Action] {gameObject.name} attacking {targetEnemy.name}");
            float attackPower = _healthSystem != null ? _healthSystem.GetAttackPower() : defaultDamage;
            targetHealth.TakeDamage(attackPower, gameObject);
            _lastAttackTime = Time.time;

            // Check if the target died from this attack.
            if (targetHealth.IsDead) 
            {
                Debug.Log($"[Attack Action] {gameObject.name} defeated {targetEnemy.name}");
                UpdateWorldStateAfterEnemyDeath();
            }
            else 
            {
                Debug.Log($"[Attack Action] {gameObject.name} will continue attacking {targetEnemy.name}, current health: {targetHealth.CurrentHealth:F1}");
            }

            // Small delay for better debugging and to avoid tight loops.
            yield return new WaitForSeconds(0.1f);
        }
        
        #endregion

        #region Helper Methods
        
        /// <summary>
        /// Checks if a target is valid for attacking.
        /// </summary>
        /// <param name="targetEnemy">The potential target enemy</param>
        /// <returns>True if target is valid, false otherwise</returns>
        private bool IsTargetValid(GladiatorAgent targetEnemy) 
        {
            if (targetEnemy == null)
                return false;
                
            var targetHealth = targetEnemy.GetComponent<AgentHealthSystem>();
            return targetHealth != null && !targetHealth.IsDead;
        }

        /// <summary>
        /// Checks if a target is within attack range.
        /// </summary>
        /// <param name="targetEnemy">The enemy to check range for</param>
        /// <returns>True if target is in range, false otherwise</returns>
        private bool IsTargetInAttackRange(GladiatorAgent targetEnemy) 
        {
            if (targetEnemy == null)
                return false;
                
            return Vector3.Distance(transform.position, targetEnemy.transform.position) <= attackRange;
        }

        /// <summary>
        /// Check if the agent can continue attacking.
        /// </summary>
        /// <returns>True if attacking can continue, false otherwise</returns>
        private bool CanContinueAttacking() 
        {
            GladiatorAgent targetEnemy = _gladiatorAgent.CurrentTargetEnemy;

            // No target to attack.
            if (targetEnemy == null) 
            {
                Debug.Log($"[Attack Action] {gameObject.name}: Target is null, possibly killed by ally");
                UpdateWorldStateAfterEnemyDeath();
                return false;
            }

            // Target health check.
            AgentHealthSystem targetHealth = targetEnemy.GetComponent<AgentHealthSystem>();
            if (targetHealth == null || targetHealth.IsDead) 
            {
                Debug.Log($"[Attack Action] {gameObject.name}: Target {targetEnemy.name} is dead or has no health component");
                UpdateWorldStateAfterEnemyDeath();
                return false;
            }

            // Range check.
            float distanceToTarget = Vector3.Distance(transform.position, targetEnemy.transform.position);
            if (distanceToTarget > attackRange) 
            {
                Debug.Log($"[Attack Action] {gameObject.name}: Target {targetEnemy.name} moved out of range ({distanceToTarget:F2} > {attackRange:F2})");
                HandleTargetOutOfRange();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Updates world state and cleans up after an enemy dies.
        /// </summary>
        private void UpdateWorldStateAfterEnemyDeath() 
        {
            // Clear current target.
            _gladiatorAgent.CurrentTargetEnemy = null;
            
            // Update world state.
            AgentWorldState.SetState(WorldStateKey.HasEnemyTarget, false);
            AgentWorldState.SetState(WorldStateKey.IsInAttackRange, false);
            
            // Stop attack loop.
            _isAttackLoopRunning = false;
            
            // Force replanning.
            _gladiatorAgent.AbortCurrentPlan();
            
            // Check if there are any other enemies to engage.
            bool moreEnemies = _gladiatorAgent.knownEnemies.Count > 0;
            AgentWorldState.SetState(WorldStateKey.EnemyDetected, moreEnemies);
            
            if (!moreEnemies) 
            {
                Debug.Log($"[Attack Action] {gameObject.name}: No more enemies detected, returning to non-combat state");
                AgentWorldState.SetState(WorldStateKey.IsInCombat, false);
            }
        }
        
        /// <summary>
        /// Handles logic when target moves out of attack range.
        /// </summary>
        private void HandleTargetOutOfRange() 
        {
            // Update world state to reflect not in attack range.
            AgentWorldState.SetState(WorldStateKey.IsInAttackRange, false);

            // Force immediate replanning
            _gladiatorAgent.AbortCurrentPlan();

            // If target is still valid, try to move back in range.
            GladiatorAgent targetEnemy = _gladiatorAgent.CurrentTargetEnemy;
            if (targetEnemy != null) 
            {
                var targetHealth = targetEnemy.GetComponent<AgentHealthSystem>();
                if (targetHealth != null && !targetHealth.IsDead) 
                {
                    // Maintain that we have a target, but need to get back in range.
                    AgentWorldState.SetState(WorldStateKey.HasEnemyTarget, true);
                    AgentWorldState.SetState(WorldStateKey.EnemyDetected, true);

                    // Directly trigger MoveToEnemy to follow target instead of waiting for planning.
                    StartCoroutine(TriggerMoveToEnemyAction());
                }
            }
        }

        /// <summary>
        /// Aborts the attack loop and cleans up state.
        /// </summary>
        private void AbortAttackLoop() 
        {
            _isAttackLoopRunning = false;
            UpdateWorldStateAfterEnemyDeath();
        }

        /// <summary>
        /// Helper method to immediately trigger a move action when out of range.
        /// </summary>
        /// <returns>IEnumerator for coroutine</returns>
        private IEnumerator TriggerMoveToEnemyAction() 
        {
            // Give a small delay to allow AbortCurrentPlan to complete.
            yield return new WaitForSeconds(0.1f);

            // Find the MoveToEnemyAction component.
            MoveToEnemyAction moveAction = GetComponent<MoveToEnemyAction>();

            if (moveAction != null && moveAction.CheckProceduralPreconditions()) 
            {
                Debug.Log($"[Attack Action] {gameObject.name}: Directly triggering MoveToEnemyAction to follow target");

                // Manually start the move action.
                yield return StartCoroutine(moveAction.PerformAction());

                // After moving is complete, check if attacking is now possible.
                if (CheckProceduralPreconditions()) 
                {
                    // Resume attacking.
                    _isAttackLoopRunning = true;
                    StartCoroutine(PerformAction());
                }
            }
            else 
            {
                Debug.LogWarning($"[Attack Action] {gameObject.name}: Could not find or validate MoveToEnemyAction");
            }
        }
        
        #endregion
    }
}