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
        
        [Header("- Combat Stats")]
        [SerializeField] private float attackPower = 10f;

        [SerializeField] private float defensePower = 5f;
        [SerializeField] private float attackSpeed = 1f;
        [SerializeField] private float moveSpeed = 5f;

        private float attackPowerModifier = 1f;
        private float defensePowerModifier = 1f;
        private float attackSpeedModifier = 1f;
        private float moveSpeedModifier = 1f;

        [Header("- State Settings")]
        [SerializeField] private bool invulnerable = false;

        [SerializeField] private bool isDead = false;

        private AgentWorldState _agentWorldState;
        private Pathfinding.PathFindingAgent _pathfindingAgent;
        
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
        
        private float CalculateModifiedDamage(float rawDamage) {
            // ! Apply defense modifier: damage reduction based on defense power.
            float effectiveDefense = defensePower * defensePowerModifier;

            // ! damage reduction scales with defense
            // Higher defense = less damage taken (min 10% of original damage).
            float damageReduction = Mathf.Clamp01(effectiveDefense / (effectiveDefense + 50f));
            float finalDamage = rawDamage * (1f - damageReduction * 0.9f); // ? Max reduction is 90%.

            return Mathf.Max(1f, finalDamage); // ? Minimum damage is 1.
        }

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

        private void ProcessHealthRegeneration() {
            if (!regenerateHealth || isDead || currentHealth <= 0 || currentHealth >= maxHealth) return;

            if (Time.time < lastDamageTime + healthRegenDelay) return;

            float healAmount = healthRegenRate * Time.deltaTime;
            if (healAmount > 0) {
                Heal(healAmount);
            }
        }

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
        
        public void Resurrect(float healthPercent = 1.0f) {
            if (!isDead) return;

            isDead = false;

            float healthToRestore = maxHealth * Mathf.Clamp01(healthPercent);
            currentHealth = healthToRestore;

            if (_agentWorldState != null) {
                // agentWorldState.SetState(GOAP.WorldStateKey.IsDead, false);
            }

            if (_pathfindingAgent != null) {
                _pathfindingAgent.enabled = true;
            }

            OnHealthChanged.Invoke(currentHealth, maxHealth);
        }
        
        public float GetAttackPower() {
            return attackPower * attackPowerModifier;
        }

        public float GetDefensePower() {
            return defensePower * defensePowerModifier;
        }

        public float GetAttackSpeed() {
            return attackSpeed * attackSpeedModifier;
        }

        public float GetMoveSpeed() {
            return moveSpeed * moveSpeedModifier;
        }
        
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
                    if (_pathfindingAgent != null) {
                        // Todo:
                        // pathfindingAgent.moveSpeed = GetMoveSpeed();
                    }

                    break;
            }

            if (duration > 0) {
                StartCoroutine(RemoveStatModifierAfterDelay(stat, duration));
            }
        }
        
        private System.Collections.IEnumerator RemoveStatModifierAfterDelay(AgentStat stat, float delay) {
            yield return new WaitForSeconds(delay);

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

        public float CurrentHealth => currentHealth;


        public float MaxHealth => maxHealth;


        public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0;


        public bool IsDead => isDead;


        public void SetInvulnerable(bool value) {
            invulnerable = value;
        }

    }


    public enum AgentStat
    {
        AttackPower,
        DefensePower,
        AttackSpeed,
        MoveSpeed
    }
}