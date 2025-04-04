using System;
using System.Collections;
using System.Collections.Generic;
using _Main_Project_Files._Scripts.Pathfinding;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace _Main_Project_Files._Scripts.GOAP.Actions
{
    public class Patrol : Action
    {
        [SerializeField] private List<Transform> patrolPoints = new List<Transform>();
        [SerializeField] private float patrolPointStopTime = 1.0f;
        [SerializeField] private float arrivalDistance = 0.5f;
        [SerializeField] private string isPatrollingStateName = "IsPatrolling";
        [SerializeField] private string areaPatrolledStateName = "AreaPatrolled";

        private Agent pathfindingAgent;

        private void Awake()
        {
            actionName = "Patrol";
            isActionAchivable = true;

            if (effects.Count == 0)
            {
                AddEffect(isPatrollingStateName, true);
                AddEffect(areaPatrolledStateName, true);
            }
        }

        private void Start()
        {
            GameObject[] waypointObjects = GameObject.FindGameObjectsWithTag("PatrolPoint");

            if (waypointObjects.Length > 0)
            {
                Transform[] waypoints = new Transform[waypointObjects.Length];
                for (int i = 0; i < waypointObjects.Length; i++)
                {
                    waypoints[i] = waypointObjects[i].transform;
                }
                
                SetWaypoints(waypoints);
            }
            else
            {
                Debug.LogWarning("[ACTION] Patrol.cs: No waypoints found in scene.");
            }
        }

        protected override IEnumerator PerformAction()
        {
            Debug.Log($"[ACTION] Patrol.cs: {gameObject.name} is patrolling.");

            if (patrolPoints.Count == 0)
            {
                Debug.LogError("[ACTION] Patrol.cs: No patrol points set.");
                yield break;
            }

            if (pathfindingAgent == null)
            {
                pathfindingAgent = gameObject.GetComponent<Agent>();
                if (pathfindingAgent == null)
                {
                    pathfindingAgent = owner.GetPathfindingAgent();
                    if (pathfindingAgent == null)
                    {
                        Debug.LogError("[ACTION] Patrol.cs: No pathfinding agent found.");
                        yield break;
                    }
                }
            }

            // Visit each waypoint.
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                Transform patrolPoint = patrolPoints[i];
                Debug.Log($"[ACTION] Patrol.cs: Moving to waypoint {i + 1} of {patrolPoints.Count}.");
                pathfindingAgent.FollowPath(patrolPoint.position);

                bool reachedWaypoint = false;
                while (!reachedWaypoint)
                {
                    float distance = Vector3.Distance(transform.position, patrolPoint.position);
                    if (distance <= arrivalDistance)
                    {
                        reachedWaypoint = true;
                    }

                    yield return null;
                }

                yield return new WaitForSeconds(patrolPointStopTime);
            }

            Debug.Log($"[ACTION] Patrol.cs: {gameObject.name} has finished patrolling.");
        }

        public void SetWaypoints(Transform[] waypoints)
        {
            patrolPoints.Clear();

            if (waypoints == null || waypoints.Length == 0)
            {
                Debug.LogWarning($"[ACTION] Patrol.cs: No waypoints set for {gameObject.name}.");
                return;
            }

            foreach (var waypoint in waypoints)
            {
                if (waypoint != null)
                {
                    patrolPoints.Add(waypoint);
                }
            }

            Debug.Log($"[ACTION] Patrol.cs: Added {patrolPoints.Count} waypoints for patrolling.");
        }
    }
}