using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Main_Project_Files.Leo._Scripts.GOAP.Status_Systems;

namespace _Main_Project_Files.Leo._Scripts.GOAP.Actions
{
    [RequireComponent(typeof(GladiatorAgent))]
    [RequireComponent(typeof(AgentWorldState))]
    public class FindEnemyAction : GoapAction
    {
        private GladiatorAgent _gladiatorAgent;

        /// <summary>
        /// Gets component references.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            _gladiatorAgent = GetComponent<GladiatorAgent>();
            GetComponent<AgentWorldState>();
        }

        /// <summary>
        /// Sets the static prerequisites for this action.
        /// </summary>
        protected override void SetUpPreRequisites() {
            ; // ! Must know enemies are around.
            AddPrerequisite(WorldStateKey.EnemyDetected, true);
            // ! Only run if there is no target YET.
            AddPrerequisite(WorldStateKey.HasEnemyTarget, false);
        }

        /// <summary>
        /// Sets the effects this action has on the world state upon successful completion.
        /// </summary>
        protected override void SetUpEffects()
        {
            AddEffect(WorldStateKey.HasEnemyTarget, true);
        }

        /// <summary>
        /// Checks dynamic conditions immediately before execution. Ensures valid enemies are available.
        /// </summary>
        public override bool CheckProceduralPreconditions()
        {
            // Check if there are any known enemies and if at least one is alive.
            GladiatorAgent potentialTarget = FindBestEnemyTarget();
            
            // Can only run if there is a valid target.
            return potentialTarget != null; 
        }

        /// <summary>
        /// Executes the logic to find and assign the best enemy target.
        /// </summary>
        public override IEnumerator PerformAction()
        {
            GladiatorAgent bestTarget = FindBestEnemyTarget();

            if (bestTarget != null)
            {
                Debug.Log($"[FindEnemyAction] ({gameObject.name}): Found target {bestTarget.name}");
                _gladiatorAgent.CurrentTargetEnemy = bestTarget;
                // ! The effect HasEnemyTarget=true will be applied automatically by GladiatorAgent after this completes.
            }
            else
            {
                Debug.LogWarning($"[FindEnemyAction] ({gameObject.name}): Could not find a valid enemy target during PerformAction.");
                // ? If no target found, should probably invalidate the state this action aimed for.
                // ? Setting HasEnemyTarget to false here would deny the effect.
                // TODO: It might be better to let the prerequisites handle re-evaluation.
                // * For now, just clear the target reference if one existed - CHANGE LATER.
                _gladiatorAgent.CurrentTargetEnemy = null;

                // TODO: REMINDER!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! AAAAAAAAAAFIJHLDSIUFHJAOJUHSPA;IHSJFIUOAHS
                // ! We need a way to signal failure to the GOAP system if PerformAction fails its objective.
                // ! This might involve returning false or having the Agent abort the plan.
                // ! For now, we'll let the next action's CheckProceduralPreconditions fail if the target is null.
            }

            yield return null;
        }

        /// <summary>
        /// Finds the closest, living enemy from the known enemies list.
        /// </summary>
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