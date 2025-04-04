using System;
using System.Collections;
using _Main_Project_Files._Scripts.Pathfinding;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;

namespace _Main_Project_Files._Scripts.GOAP.Actions
{
    public class MoveToLocation : Action
    {
        [SerializeField] private Transform targetLocation;
        [SerializeField] private float arrivaDistance = 0.5f;
        [SerializeField] private string atLocationStateName = "AtLocation";

        private Agent pathfindingAgent;
        private bool hasReachedTarget = false;

        private void Awake()
        {
            actionName = "MoveToLocation";
            isActionAchivable = true;

            if (effects.Count == 0)
            {
                AddEffect(atLocationStateName, true);
            }
        }

        public void SetTargetLocation(Transform target)
        {
            this.targetLocation = target;
        }

        protected override IEnumerator PerformAction()
        {
            Debug.Log($"[ACTION] MoveToLocation.cs: {gameObject.name} is moving to {(targetLocation ? targetLocation.name : "null")}");
            if (targetLocation == null)
            {
                Debug.LogError("[ACTION] MoveToLocation.cs: No target location set.");
                yield break;   
            }
            
            if (pathfindingAgent == null)
            {
                pathfindingAgent = gameObject.GetComponent<Agent>();
            }
            
            if (pathfindingAgent == null)
            {
                pathfindingAgent = owner.GetPathfindingAgent();
                if (pathfindingAgent == null)
                {
                    Debug.LogError("[ACTION] MoveToLocation.cs: No pathfinding agent found.");
                    yield break;
                }
            }
            
            hasReachedTarget = false;
            pathfindingAgent.FollowPath(targetLocation.position);

            while (!hasReachedTarget)
            {
                float distanceToTarget = Vector3.Distance(transform.position, targetLocation.position);
                if (distanceToTarget <= arrivaDistance)
                {
                    hasReachedTarget = true;
                    Debug.Log($"[ACTION] MoveToLocation.cs: {gameObject.name} has reached the target location.");
                    break;
                }
                yield return null;
            }
        }
    }
}
