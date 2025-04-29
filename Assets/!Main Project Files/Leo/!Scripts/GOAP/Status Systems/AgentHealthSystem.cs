using System;
using UnityEngine;
using UnityEngine.Events;

namespace _Main_Project_Files.Leo._Scripts.GOAP.Status_Systems
{
    /// <summary>
    /// Manages health and basic stats for a gladiator agent.
    /// Handles damage, healing, death, and stat modifiers.
    /// </summary>
    public class AgentHealthSystem : MonoBehaviour
    {
        #region Events

        [Serializable] // Current, Max.
        public class HealthChangedEvent : UnityEvent<float, float>
        {
        }

        [Serializable]
        public class AgentDiedEvent : UnityEvent
        {
        }

        [Serializable] // Amount, Source.
        public class AgentDamagedEvent : UnityEvent<float, GameObject>
        {
        }

        [Serializable] // Amount.
        public class AgentHealedEvent : UnityEvent<float>
        {
        }

        public HealthChangedEvent OnHealthChanged = new();
        public AgentDiedEvent OnAgentDied = new();
        public AgentDamagedEvent OnAgentDamaged = new();
        public AgentHealedEvent OnAgentHealed = new();

        #endregion

        #region Health Properties

        [Header("- Health Settings")]
        [SerializeField] private float maxHealth = 100f;

        [SerializeField] private float currentHealth;

        [Tooltip("Health regeneration per second when not in combat")] [SerializeField]
        private float healthRegenRate = 0f;

        [Tooltip("Delay in seconds after taking damage before health regen starts")] [SerializeField]
        private float healthRegenDelay = 5f;

        [Tooltip("If true, agent will regenerate health over time when not in combat")] [SerializeField]
        private bool regenerateHealth = false;

        private float lastDamageTime = -999f;

        #endregion

        #region Stats Properties

        [Header("- Combat Stats")]
        [SerializeField] private float attackPower = 10f;

        [SerializeField] private float defensePower = 5f;
        [SerializeField] private float attackSpeed = 1f;
        [SerializeField] private float moveSpeed = 5f;

        private float attackPowerModifier = 1f;
        private float defensePowerModifier = 1f;
        private float attackSpeedModifier = 1f;
        private float moveSpeedModifier = 1f;

        #endregion

        #region State Properties

        [Header("- State Settings")]
        [SerializeField] private bool invulnerable = false;

        [SerializeField] private bool isDead = false;

        private AgentWorldState _agentWorldState;
        private Pathfinding.PathFindingAgent _pathfindingAgent;

        #endregion

        #region Unity Lifecycle

        private void Awake() {
            currentHealth = maxHealth;

            _agentWorldState = GetComponent<AgentWorldState>();
            _pathfindingAgent = GetComponent<Pathfinding.PathFindingAgent>();
        }

        private void Start() {
            // ! Trigger initial health event.
            OnHealthChanged.Invoke(currentHealth, maxHealth);
        }

        private void Update() {
            ProcessHealthRegeneration();
        }

        #endregion

        #region Health Methods
        /// <summary>
        /// Apply damage to the agent.
        /// </summary>
        /// <param name="damageAmount">Amount of damage to apply.</param>
        /// <param name="damageSource">Optional source of the damage.</param>
        /// <returns>The actual amount of damage applied after modifiers.</returns>
        public float TakeDamage(float damageAmount, GameObject damageSource = null) {
            if (isDead || invulnerable || damageAmount <= 0) return 0f;

            lastDamageTime = Time.time;

            float modifiedDamage = CalculateModifiedDamage(damageAmount);

            currentHealth -= modifiedDamage;

            currentHealth = Mathf.Max(0, currentHealth);

            OnAgentDamaged.Invoke(modifiedDamage, damageSource);
            OnHealthChanged.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0 && !isDead) {
                Die();
            }

            return modifiedDamage;
        }
        
        /// <summary>
        /// Calculate damage after applying defensive modifiers.
        /// </summary>
        /// <param name="rawDamage">The initial damage amount.</param>
        /// <returns>Modified damage amount.</returns>
        private float CalculateModifiedDamage(float rawDamage) {
            // ! Apply defense modifier: damage reduction based on defense power.
            float effectiveDefense = defensePower * defensePowerModifier;

            // ! damage reduction scales with defense
            // Higher defense = less damage taken (min 10% of original damage).
            float damageReduction = Mathf.Clamp01(effectiveDefense / (effectiveDefense + 50f));
            float finalDamage = rawDamage * (1f - damageReduction * 0.9f); // ? Max reduction is 90%.

            return Mathf.Max(1f, finalDamage); // ? Minimum damage is 1.
        }

        /// <summary>
        /// Heal the agent for the specified amount.
        /// </summary>
        /// <param name="healAmount">Amount to heal.</param>
        /// <returns>The actual amount healed.</returns>
        public float Heal(float healAmount) {
            if (isDead || healAmount <= 0) return 0f;

            float oldHealth = currentHealth;

            currentHealth += healAmount;

            currentHealth = Mathf.Min(currentHealth, maxHealth);

            float actualHealAmount = currentHealth - oldHealth;

            if (actualHealAmount > 0) {
                OnAgentHealed.Invoke(actualHealAmount);
                OnHealthChanged.Invoke(currentHealth, maxHealth);
            }

            return actualHealAmount;
        }

        /// <summary>
        /// Process health regeneration over time when enabled.
        /// </summary>
        private void ProcessHealthRegeneration() {
            if (!regenerateHealth || isDead || currentHealth <= 0 || currentHealth >= maxHealth) return;

            if (Time.time < lastDamageTime + healthRegenDelay) return;

            float healAmount = healthRegenRate * Time.deltaTime;
            if (healAmount > 0) {
                Heal(healAmount);
            }
        }

        /// <summary>
        /// Handle the agent's death.
        /// </summary>
        private void Die() {
            if (isDead) return;

            isDead = true;

            if (_agentWorldState != null) {
                // TODO: WorldStateKey enum.
                // agentWorldState.SetState(GOAP.WorldStateKey.IsDead, true);
            }

            if (_pathfindingAgent != null) {
                _pathfindingAgent.enabled = false;
            }

            OnAgentDied.Invoke();
        }

        /// <summary>
        /// Resurrect the agent with the specified health percentage.
        /// </summary>
        /// <param name="healthPercent">Percentage of max health to restore (0.0-1.0).</param>
        public void Resurrect(float healthPercent = 1.0f) {
            if (!isDead) return;

            isDead = false;

            // Calculate health to restore.
            float healthToRestore = maxHealth * Mathf.Clamp01(healthPercent);
            currentHealth = healthToRestore;

            // Update world state if available.
            if (_agentWorldState != null) {
                // agentWorldState.SetState(GOAP.WorldStateKey.IsDead, false);
            }

            // Enable pathfinding.
            if (_pathfindingAgent != null) {
                _pathfindingAgent.enabled = true;
            }

            // Trigger health changed event.
            OnHealthChanged.Invoke(currentHealth, maxHealth);
        }

        #endregion

        #region Stats Methods

        /// <summary>
        /// Get the current attack power including modifiers.
        /// </summary>
        public float GetAttackPower() {
            return attackPower * attackPowerModifier;
        }

        /// <summary>
        /// Get the current defense power including modifiers.
        /// </summary>
        public float GetDefensePower() {
            return defensePower * defensePowerModifier;
        }

        /// <summary>
        /// Get the current attack speed including modifiers.
        /// </summary>
        public float GetAttackSpeed() {
            return attackSpeed * attackSpeedModifier;
        }

        /// <summary>
        /// Get the current move speed including modifiers.
        /// </summary>
        public float GetMoveSpeed() {
            return moveSpeed * moveSpeedModifier;
        }

        /// <summary>
        /// Apply a temporary modifier to a stat.
        /// </summary>
        /// <param name="stat">The stat to modify.</param>
        /// <param name="modifierMultiplier">The multiplier to apply.</param>
        /// <param name="duration">Duration in seconds, or -1 for permanent.</param>
        public void ApplyStatModifier(AgentStat stat, float modifierMultiplier, float duration = -1f) {
            switch (stat) {
                case AgentStat.AttackPower:
                    attackPowerModifier = modifierMultiplier;
                    break;
                case AgentStat.DefensePower:
                    defensePowerModifier = modifierMultiplier;
                    break;
                case AgentStat.AttackSpeed:
                    attackSpeedModifier = modifierMultiplier;
                    break;
                case AgentStat.MoveSpeed:
                    moveSpeedModifier = modifierMultiplier;
                    // Update pathfinding agent speed if available
                    if (_pathfindingAgent != null) {
                        // Assuming your PathFindingAgent has a public moveSpeed field
                        // pathfindingAgent.moveSpeed = GetMoveSpeed();
                    }

                    break;
            }

            // If temporary, start coroutine to remove after duration
            if (duration > 0) {
                StartCoroutine(RemoveStatModifierAfterDelay(stat, duration));
            }
        }

        /// <summary>
        /// Coroutine to remove a stat modifier after a delay.
        /// </summary>
        private System.Collections.IEnumerator RemoveStatModifierAfterDelay(AgentStat stat, float delay) {
            yield return new WaitForSeconds(delay);

            // Reset the modifier.
            switch (stat) {
                case AgentStat.AttackPower:
                    attackPowerModifier = 1f;
                    break;
                case AgentStat.DefensePower:
                    defensePowerModifier = 1f;
                    break;
                case AgentStat.AttackSpeed:
                    attackSpeedModifier = 1f;
                    break;
                case AgentStat.MoveSpeed:
                    moveSpeedModifier = 1f;
                    // TODO: Update pathfinding agent speed.
                    if (_pathfindingAgent != null) {
                        // pathfindingAgent.moveSpeed = GetMoveSpeed();
                    }

                    break;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Get current health.
        /// </summary>
        public float CurrentHealth => currentHealth;

        /// <summary>
        /// Get maximum health.
        /// </summary>
        public float MaxHealth => maxHealth;

        /// <summary>
        /// Get current health percentage (0-1).
        /// </summary>
        public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0;

        /// <summary>
        /// Check if agent is dead.
        /// </summary>
        public bool IsDead => isDead;

        /// <summary>
        /// Set invulnerability status.
        /// </summary>
        public void SetInvulnerable(bool value) {
            invulnerable = value;
        }

        #endregion
    }

    /// <summary>
    /// Enum for agent stats that can be modified.
    /// </summary>
    public enum AgentStat
    {
        AttackPower,
        DefensePower,
        AttackSpeed,
        MoveSpeed
    }
}