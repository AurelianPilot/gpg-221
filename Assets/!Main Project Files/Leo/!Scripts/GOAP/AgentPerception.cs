using UnityEngine;
using System.Collections.Generic;

namespace _Main_Project_Files.Leo._Scripts.GOAP
{
    /// <summary>
    /// Handles agent perception by detecting entities within a trigger collider 
    /// and updating behavior based on what is perceived.
    /// </summary>
    [RequireComponent(typeof(GladiatorAgent))]
    [RequireComponent(typeof(AgentWorldState))]
    [RequireComponent(typeof(SphereCollider))]
    public class AgentPerception : MonoBehaviour
    {
        #region Fields

        private GladiatorAgent _localAgent;
        private AgentWorldState _localWorldState;
        private SphereCollider _triggerCollider;
        private float _lastBehaviorCheck;
        private readonly float _behaviorCheckInterval = 2f;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes required components and validates setup.
        /// </summary>
        private void Awake() {
            _localAgent = GetComponent<GladiatorAgent>();
            _localWorldState = GetComponent<AgentWorldState>();
            _triggerCollider = GetComponent<SphereCollider>();

            ValidateRequiredComponents();
        }

        /// <summary>
        /// Validates that all required components are present and properly configured.
        /// </summary>
        private void ValidateRequiredComponents() {
            if (_localAgent == null || _localWorldState == null) {
                Debug.LogError(
                    $"AgentPerception on {gameObject.name}: Missing GladiatorAgent or AgentWorldState component!");
                enabled = false;
                return;
            }

            if (_triggerCollider == null || !_triggerCollider.isTrigger) {
                Debug.LogError(
                    $"AgentPerception on {gameObject.name}: Missing SphereCollider or it's not set to 'Is Trigger'!");
                enabled = false;
            }
        }

        #endregion

        #region Update Logic

        /// <summary>
        /// Periodically updates agent behavior based on perception.
        /// </summary>
        private void Update() {
            if (Time.time > _lastBehaviorCheck + _behaviorCheckInterval) {
                _lastBehaviorCheck = Time.time;
                UpdateBehaviorBasedOnPerception();
            }
        }

        /// <summary>
        /// Updates the agent's behavior based on what it currently perceives.
        /// </summary>
        private void UpdateBehaviorBasedOnPerception() {
            CleanupEnemiesList();

            bool hasValidEnemies = HasValidEnemies();

            _localWorldState.SetState(WorldStateKey.EnemyDetected, hasValidEnemies);

            GoapGoal currentGoal = _localAgent.GetCurrentGoal();
            if (currentGoal == null) return;

            UpdateBehaviorForRole(hasValidEnemies, currentGoal);
        }

        /// <summary>
        /// Updates behavior based on agent role, perception, and current goal.
        /// </summary>
        /// <param name="hasValidEnemies">Whether valid enemies are detected</param>
        /// <param name="currentGoal">The agent's current goal</param>
        private void UpdateBehaviorForRole(bool hasValidEnemies, GoapGoal currentGoal) {
            if (_localAgent.agentRole == GladiatorAgent.AgentRole.Warrior) {
                if (hasValidEnemies) {
                    if (currentGoal.GoalName == "WanderGoal") {
                        Debug.Log($"{_localAgent.name}: Detected enemies while wandering, switching to combat");
                        SwitchToCombatGoal();
                    }
                }
                else {
                    if (currentGoal.GoalName == "EngageInCombatGoal") {
                        Debug.Log($"{_localAgent.name}: No enemies detected, switching to wandering");
                        SwitchToWanderingGoal();
                    }
                }
            }
        }

        #endregion

        #region Trigger Detection

        /// <summary>
        /// Detects when entities enter the agent's perception radius.
        /// </summary>
        /// <param name="other">The collider that entered the trigger</param>
        private void OnTriggerEnter(Collider other) {
            if (other.gameObject == gameObject) return;

            GladiatorAgent detectedAgent = other.GetComponent<GladiatorAgent>();
            if (detectedAgent != null) {
                if (detectedAgent.teamID != _localAgent.teamID) {
                    HandleEnemyDetected(detectedAgent);
                }
                else {
                    HandleAllyDetected(detectedAgent);
                }
            }
        }

        /// <summary>
        /// Adds newly detected enemy to known enemies list and updates state.
        /// </summary>
        /// <param name="detectedAgent">The enemy agent that was detected</param>
        private void HandleEnemyDetected(GladiatorAgent detectedAgent) {
            if (!_localAgent.knownEnemies.Contains(detectedAgent)) {
                _localAgent.knownEnemies.Add(detectedAgent);
                UpdateWorldStateEnemyDetected();
                Debug.Log($"{_localAgent.name} detected ENEMY: {detectedAgent.name}");

                UpdateBehaviorBasedOnPerception();
            }
        }

        /// <summary>
        /// Adds newly detected ally to known allies list and updates state.
        /// </summary>
        /// <param name="detectedAgent">The ally agent that was detected</param>
        private void HandleAllyDetected(GladiatorAgent detectedAgent) {
            if (!_localAgent.knownAllies.Contains(detectedAgent)) {
                _localAgent.knownAllies.Add(detectedAgent);
                UpdateWorldStateAllyDetected();
                Debug.Log($"{_localAgent.name} detected ALLY: {detectedAgent.name}");
            }
        }

        /// <summary>
        /// Detects when entities exit the agent's perception radius.
        /// </summary>
        /// <param name="other">The collider that exited the trigger</param>
        private void OnTriggerExit(Collider other) {
            if (other.gameObject == gameObject) return;

            GladiatorAgent exitedAgent = other.GetComponent<GladiatorAgent>();
            if (exitedAgent != null) {
                if (exitedAgent.teamID != _localAgent.teamID) {
                    HandleEnemyLost(exitedAgent);
                }
                else {
                    HandleAllyLost(exitedAgent);
                }
            }
        }

        /// <summary>
        /// Removes lost enemy from known enemies list and updates state.
        /// </summary>
        /// <param name="exitedAgent">The enemy agent that was lost</param>
        private void HandleEnemyLost(GladiatorAgent exitedAgent) {
            if (_localAgent.knownEnemies.Remove(exitedAgent)) {
                UpdateWorldStateEnemyDetected();
                Debug.Log($"{_localAgent.name} lost sight of ENEMY: {exitedAgent.name}");

                UpdateBehaviorBasedOnPerception();
            }
        }

        /// <summary>
        /// Removes lost ally from known allies list and updates state.
        /// </summary>
        /// <param name="exitedAgent">The ally agent that was lost</param>
        private void HandleAllyLost(GladiatorAgent exitedAgent) {
            if (_localAgent.knownAllies.Remove(exitedAgent)) {
                UpdateWorldStateAllyDetected();
                Debug.Log($"{_localAgent.name} lost sight of ALLY: {exitedAgent.name}");
            }
        }

        #endregion

        #region World State Management

        /// <summary>
        /// Updates world state based on enemy detection.
        /// </summary>
        private void UpdateWorldStateEnemyDetected() {
            CleanupEnemiesList();
            _localWorldState.SetState(WorldStateKey.EnemyDetected, _localAgent.knownEnemies.Count > 0);
        }

        /// <summary>
        /// Updates world state based on ally detection.
        /// </summary>
        private void UpdateWorldStateAllyDetected() {
            _localAgent.knownAllies.RemoveAll(item => item == null);
            _localWorldState.SetState(WorldStateKey.AllyDetected, _localAgent.knownAllies.Count > 0);
        }

        #endregion

        #region Goal Management

        /// <summary>
        /// Switches the agent to combat goal when enemies are detected.
        /// </summary>
        private void SwitchToCombatGoal() {
            _localAgent.AbortCurrentPlan();

            GoapGoal combatGoal = new GoapGoal("EngageInCombatGoal", WorldStateKey.IsInCombat, true, 5);
            _localAgent.SetGoal(combatGoal);

            _localWorldState.SetState(WorldStateKey.IsWandering, false);
        }

        /// <summary>
        /// Switches the agent to wandering goal when no enemies are present.
        /// </summary>
        private void SwitchToWanderingGoal() {
            _localAgent.AbortCurrentPlan();

            GoapGoal wanderGoal = new GoapGoal("WanderGoal", WorldStateKey.IsWandering, true, 1);
            _localAgent.SetGoal(wanderGoal);

            _localWorldState.SetState(WorldStateKey.IsInCombat, false);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Checks if there are any valid (alive) enemies in the known enemies list.
        /// </summary>
        /// <returns>True if there are valid enemies, false otherwise</returns>
        private bool HasValidEnemies() {
            foreach (var enemy in _localAgent.knownEnemies) {
                if (enemy != null) {
                    var enemyHealth = enemy.GetComponent<Status_Systems.AgentHealthSystem>();
                    if (enemyHealth != null && !enemyHealth.IsDead) {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Cleans up the enemies list by removing null or dead enemies.
        /// </summary>
        private void CleanupEnemiesList() {
            _localAgent.knownEnemies.RemoveAll(item => item == null);

            for (int i = _localAgent.knownEnemies.Count - 1; i >= 0; i--) {
                var enemy = _localAgent.knownEnemies[i];
                var enemyHealth = enemy.GetComponent<Status_Systems.AgentHealthSystem>();
                if (enemyHealth != null && enemyHealth.IsDead) {
                    _localAgent.knownEnemies.RemoveAt(i);
                }
            }
        }

        #endregion
    }
}