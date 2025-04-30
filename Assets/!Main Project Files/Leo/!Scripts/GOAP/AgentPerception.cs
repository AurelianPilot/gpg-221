using UnityEngine;
using System.Collections.Generic; // Required for Lists

namespace _Main_Project_Files.Leo._Scripts.GOAP
{
    [RequireComponent(typeof(GladiatorAgent))]
    [RequireComponent(typeof(AgentWorldState))]
    [RequireComponent(typeof(SphereCollider))]
    public class AgentPerception : MonoBehaviour
    {
        private GladiatorAgent _localAgent;
        private AgentWorldState _localWorldState;
        private SphereCollider _triggerCollider;

        private void Awake()
        {
            _localAgent = GetComponent<GladiatorAgent>();
            _localWorldState = GetComponent<AgentWorldState>();
            _triggerCollider = GetComponent<SphereCollider>();

            if (_localAgent == null || _localWorldState == null)
            {
                Debug.LogError($"AgentPerception on {gameObject.name}: Missing GladiatorAgent or AgentWorldState component!");
                enabled = false;
                return;
            }

            if (_triggerCollider == null || !_triggerCollider.isTrigger)
            {
                Debug.LogError($"AgentPerception on {gameObject.name}: Missing SphereCollider or it's not set to 'Is Trigger'!");
                enabled = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == gameObject) return;

            GladiatorAgent detectedAgent = other.GetComponent<GladiatorAgent>();
            if (detectedAgent != null)
            {
                // Check Team ID.
                if (detectedAgent.teamID != _localAgent.teamID)
                {
                    // ! It's an Enemy.
                    if (!_localAgent.knownEnemies.Contains(detectedAgent))
                    {
                        _localAgent.knownEnemies.Add(detectedAgent);
                        UpdateWorldStateEnemyDetected();
                         Debug.Log($"{_localAgent.name} detected ENEMY: {detectedAgent.name}");
                    }
                }
                else
                {
                    // * It's an Ally.
                    if (!_localAgent.knownAllies.Contains(detectedAgent))
                    {
                        _localAgent.knownAllies.Add(detectedAgent);
                        UpdateWorldStateAllyDetected();
                         Debug.Log($"{_localAgent.name} detected ALLY: {detectedAgent.name}");
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
             if (other.gameObject == gameObject) return; // Ignore self.

            GladiatorAgent exitedAgent = other.GetComponent<GladiatorAgent>();
            if (exitedAgent != null)
            {
                 // Check Team ID.
                 if (exitedAgent.teamID != _localAgent.teamID)
                 {
                     // Enemy left range.
                     if (_localAgent.knownEnemies.Remove(exitedAgent))
                     {
                         UpdateWorldStateEnemyDetected(); // Update state based on remaining enemies.
                         Debug.Log($"{_localAgent.name} lost sight of ENEMY: {exitedAgent.name}");
                     }
                 }
                 else
                 {
                     // Ally left range.
                     if (_localAgent.knownAllies.Remove(exitedAgent))
                     {
                         UpdateWorldStateAllyDetected(); // Update state based on remaining allies.
                         Debug.Log($"{_localAgent.name} lost sight of ALLY: {exitedAgent.name}");
                     }
                 }
             }
        }

        // Helper method to update world state based on known enemies list.
        private void UpdateWorldStateEnemyDetected()
        {
            // Clean up null references (e.g., if an enemy was destroyed).
            _localAgent.knownEnemies.RemoveAll(item => item == null);
            _localWorldState.SetState(WorldStateKey.EnemyDetected, _localAgent.knownEnemies.Count > 0);
        }

        // Helper method to update world state based on known allies list.
        private void UpdateWorldStateAllyDetected()
        {
             // Clean up null references
             _localAgent.knownAllies.RemoveAll(item => item == null);
            _localWorldState.SetState(WorldStateKey.AllyDetected, _localAgent.knownAllies.Count > 0);
            // TODO: In a later step, you could check ally health here and set AllyNeedsHealing.
        }

         /*Optional: Periodically clean lists in Update just in case OnTriggerExit doesn't fire reliably
         void Update() {
            if (Time.frameCount % 30 == 0) // Every 30 frames approx
            {
               UpdateWorldStateEnemyDetected();
               UpdateWorldStateAllyDetected();
            }
         }*/
    }
}