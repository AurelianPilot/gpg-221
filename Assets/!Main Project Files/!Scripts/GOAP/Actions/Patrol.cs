using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using _Main_Project_Files._Scripts.Pathfinding;
using UnityEngine;

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
                            
                        }
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