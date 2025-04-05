using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using _Main_Project_Files._Scripts.Pathfinding;
using UnityEditor.AssetImporters;
using UnityEngine;
using Color = System.Drawing.Color;

namespace _Main_Project_Files._Scripts.GOAP.Actions
{
    public class Patrol : Action
    {
        [SerializeField] private List<Transform> patrolPoints = new List<Transform>();
        [SerializeField] private float patrolPointStopTime = 1.0f;
        [SerializeField] private float arrivalDistance = 0.5f;
        [SerializeField] private string isPatrollingStateName = "IsPatrolling";
        [SerializeField] private string areaPatrolledStateName = "AreaPatrolled";
        [SerializeField] private int numberOfGeneratedPoints = 3;

        [Header("- Debug")] 
        [SerializeField] private bool showDebugPoints = true;
        [SerializeField] private bool logging = true;
        
        private Agent pathfindingAgent;
        private GridManager gridManager;
        private bool isPatrolling;
        private int currentWaypointIndex;

        private void Awake()
        {
            actionName = "Patrol";
            isActionAchivable = true;

            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("[ACTION] Patrol.cs: No grid manager found.");
            }
            
            if (effects.Count == 0)
            {
                AddEffect(isPatrollingStateName, true);
                AddEffect(areaPatrolledStateName, true);
            }
        }

        private void Start()
        {
            if (patrolPoints.Count > 0)
            {
                Debug.Log($"[ACTION] Patrol.cs: Using {patrolPoints.Count} pre-assigned points.");
                return;
            }

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

            if (gridManager != null)
            {
                if (gridManager == null) return;
                Debug.Log("[ACTION] Patrol.cs: Generating patrol points randomly on grid.");

                for (int i = 0; i < numberOfGeneratedPoints; i++)
                {
                    Node randomNode = GetRandomWalkableNode();
                    if (randomNode != null)
                    {
                        GameObject patrolPointObject = new GameObject($"PatrolPoint{i + 1}");
                        patrolPointObject.transform.position = randomNode.Position;
                        
                        patrolPoints.Add(patrolPointObject.transform);

                        if (showDebugPoints)
                        {
                            GameObject visualMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            visualMarker.transform.position = randomNode.Position;
                            visualMarker.transform.localScale = Vector3.one * 0.3f;
                            visualMarker.transform.parent = patrolPointObject.transform;
                        }
                    }
                }
                
                Debug.Log($"[ACTION] Patrol.cs: Generated {patrolPoints.Count} randomly on grid.");

                if (logging)
                {
                    for (int i = 0; i < patrolPoints.Count; i++)
                    {
                        Debug.Log($"[ACTION] Patrol.cs: patrol point {i + 1} position {patrolPoints[i].position}");
                    }
                }
            }
            
            else
            {
                Debug.LogWarning("[ACTION] Patrol.cs: No waypoints found in scene.");
            }
        }

        private Node GetRandomWalkableNode()
        {
            if (gridManager.Nodes == null || gridManager.Nodes.Length == 0) return null;

            // The magic number is to try only 50 times so it doens't go forever (workaround for a found problem).
            for (int i = 0; i < 50; i++)
            {
                int randomIndex = Random.Range(0, gridManager.Nodes.Length);
                Node node = gridManager.Nodes[randomIndex];

                if (node != null && node.Walkable && !IsTooCloseToOtherPoints(node.Position))
                {
                    return node;
                }
            }

            return null;
        }

        private bool IsTooCloseToOtherPoints(Vector3 position)
        {
            float minDistance = gridManager.NodeSize * 3; // putting some distances between points TODO: fix magic numbers later.
            foreach (var point in patrolPoints)
            {
                if (Vector3.Distance(position, point.position) < minDistance)
                {
                    return true;
                }
            }

            return false;
        }
        
        protected override IEnumerator PerformAction()
        {
            Debug.Log($"[ACTION] Patrol.cs: {gameObject.name} is patrolling.");
            isPatrolling = true;
            currentWaypointIndex = 0;
            
            if (patrolPoints.Count == 0)
            {
                Debug.LogError("[ACTION] Patrol.cs: No patrol points set.");
                isPatrolling = false;
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
                        isPatrolling = false;
                        yield break;
                    }
                }
            }

            // Visit each waypoint.
            int waypointVisitCount = 0;
            int maxIterations = 100;

            if (logging)
            {
                Debug.Log($"[ACTION] Patrol.cs: {gameObject.name} is patrolling with {patrolPoints.Count} waypoints.");
                for (int i = 0; i < patrolPoints.Count; i++)
                {
                    if (patrolPoints[i] != null)
                    {
                        Debug.Log($"[ACTION] Patrol.cs: Point {i}: {patrolPoints[i].position}");
                    }
                    else
                    {
                        Debug.LogError($"[ACTION] Patrol.cs: Point {i}is null");
                    }
                }
            }

            while (waypointVisitCount < maxIterations && isPatrolling)
            {
                if (patrolPoints.Count == 0)
                {
                    Debug.LogError("[ACTION] Patrol.cs: list became empty during patrol?");
                    isPatrolling = false;
                    yield break;
                }

                if (currentWaypointIndex >= patrolPoints.Count)
                {
                    Debug.LogWarning($"[ACTION] Patrol.cs: Waypoint index {currentWaypointIndex} out of bounds, resetting waypoint index.");
                    currentWaypointIndex = 0;
                }
            }
            
            Transform patrolPoint = patrolPoints[currentWaypointIndex];
            if (patrolPoint == null)
            {
                Debug.LogWarning($"[ACTION] Patrol.cs: atrol point at index {currentWaypointIndex} not found, skipping.");
                currentWaypointIndex = (currentWaypointIndex + 1) % patrolPoints.Count;
            }
            
            Debug.Log($"[ACTION] Patrol.cs: Moving to waypoint {currentWaypointIndex + 1} of {patrolPoints.Count}");
            
            float initialDistance = Vector3.Distance(transform.position, patrolPoint.position);
            if (initialDistance <= arrivalDistance)
            {
                Debug.Log($"[ACTION] Patrol.cs: Reached waypoint {currentWaypointIndex + 1}");
                currentWaypointIndex = (currentWaypointIndex + 1) % patrolPoints.Count;
                waypointVisitCount++;
            }
            
            pathfindingAgent.FollowPath(patrolPoint.position);

            float startWaitTime = Time.time;
            float timeOut = 10f;
            bool reachedWaypoint = false;

            while (!reachedWaypoint && Time.time < startWaitTime + timeOut && isPatrolling)
            {
                float distance = Vector3.Distance(transform.position, patrolPoint.position);

                if (distance <= arrivalDistance)
                {
                    reachedWaypoint = true;
                    Debug.Log($"[ACTION] Patrol.cs: Reached waypoint {currentWaypointIndex + 1} at {Time.time - startWaitTime:F1} seconds");
                }

                yield return null;
            }

            if (!reachedWaypoint)
            {
                Debug.LogWarning($"[ACTION] Patrol.cs: Timed out trying to reach waypoint {currentWaypointIndex + 1}, moving to next one.");
            }
            else
            {
                Debug.Log($"[ACTION] Patrol.cs: Waiting at waypoint {currentWaypointIndex + 1} for {patrolPointStopTime} seconds");
                yield return new WaitForSeconds(patrolPointStopTime);
                Debug.Log($"[ACTION] Patrol.cs: Done waiting at waypoint {currentWaypointIndex + 1}. Moving to next one.");
            }

            currentWaypointIndex = (currentWaypointIndex + 1) % patrolPoints.Count;
            waypointVisitCount++;

            if (waypointVisitCount > 0 && waypointVisitCount % patrolPoints.Count == 0)
            {
                Debug.Log($"[ACTION] Patrol.cs: Completed circuit!! Starting again.");
            }
            
            /*
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

            Debug.Log($"[ACTION] Patrol.cs: {gameObject.name} has finished patrolling.");*/
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