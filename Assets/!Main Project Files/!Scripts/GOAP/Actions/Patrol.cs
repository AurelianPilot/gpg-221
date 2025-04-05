using System.Collections;
using System.Collections.Generic;
using _Main_Project_Files._Scripts.Pathfinding;
using UnityEngine;
using Random = UnityEngine.Random;

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

        [Header("- Debug")] [SerializeField] private bool showDebugPoints = true;
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
            // ALWAYS force generating random patrol points for testing TODO: DELETE THIS LATER!11!!!.
            ForceGenerateRandomPoints();
        }

        public void ForceGenerateRandomPoints()
        {
            patrolPoints.Clear();
            Debug.Log("[ACTION] Patrol.cs: Forcing generation of random patrol points.");
            GenerateRandomPatrolPoints();
        }

        private void GenerateRandomPatrolPoints()
        {
            if (gridManager == null)
            {
                gridManager = FindObjectOfType<GridManager>();
                if (gridManager == null)
                {
                    Debug.LogError("[ACTION] Patrol.cs: Still no grid manager found.");
                    return;
                }
            }

            Debug.Log("[ACTION] Patrol.cs: Generating random patrol points from grid.");

            // Debug all available nodes in the grid.
            Debug.Log($"[ACTION] Patrol.cs: Grid has {gridManager.Nodes.Length} total nodes.");
            int walkableCount = 0;
            foreach (var node in gridManager.Nodes)
            {
                if (node != null && node.Walkable)
                    walkableCount++;
            }

            Debug.Log($"[ACTION] Patrol.cs: Found {walkableCount} walkable nodes in grid.");

            // Create empty GameObjects to serve as patrol points.
            for (int i = 0; i < numberOfGeneratedPoints; i++)
            {
                // Get random walkable nodes from the grid.
                Node randomNode = GetRandomWalkableNode();
                if (randomNode != null)
                {
                    GameObject patrolPointObj = new GameObject($"RandomPatrolPoint_{i}");
                    patrolPointObj.transform.position = randomNode.Position;
                    
                    patrolPoints.Add(patrolPointObj.transform);
                    
                    if (showDebugPoints)
                    {
                        GameObject visualMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        visualMarker.transform.position = randomNode.Position;
                        visualMarker.transform.localScale = Vector3.one * 0.3f;
                        visualMarker.transform.parent = patrolPointObj.transform;

                        // Add a distinctive color to better visualize.
                        Renderer renderer = visualMarker.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            renderer.material.color = Color.green;
                        }
                    }

                    Debug.Log($"[ACTION] Patrol.cs: Added random patrol point {i} at position {randomNode.Position}.");
                }
                else
                {
                    Debug.LogError($"[ACTION] Patrol.cs: Failed to find a walkable node for point {i}.");
                }
            }

            Debug.Log($"[ACTION] Patrol.cs: Generated {patrolPoints.Count} random patrol points.");

            // Log positions of all generated patrol points.
            if (logging)
            {
                for (int i = 0; i < patrolPoints.Count; i++)
                {
                    Debug.Log($"[ACTION] Patrol.cs: RandomPatrolPoint_{i} position: {patrolPoints[i].position}.");
                }
            }
        }

        private Node GetRandomWalkableNode()
        {
            if (gridManager.Nodes == null || gridManager.Nodes.Length == 0)
            {
                Debug.LogError("[ACTION] Patrol.cs: Grid has no nodes!");
                return null;
            }

            // Create a list of all walkable nodes first.
            List<Node> walkableNodes = new List<Node>();
            foreach (var node in gridManager.Nodes)
            {
                if (node != null && node.Walkable)
                {
                    walkableNodes.Add(node);
                }
            }

            if (walkableNodes.Count == 0)
            {
                Debug.LogError("[ACTION] Patrol.cs: No walkable nodes found in the grid!");
                return null;
            }

            Debug.Log($"[ACTION] Patrol.cs: Found {walkableNodes.Count} walkable nodes to choose from.");

            // Try up to 50 times to find a suitable node.
            for (int i = 0; i < 50; i++)
            {
                if (walkableNodes.Count == 0) break;

                int randomIndex = Random.Range(0, walkableNodes.Count);
                Node node = walkableNodes[randomIndex];

                if (!IsTooCloseToOtherPoints(node.Position))
                {
                    Debug.Log($"[ACTION] Patrol.cs: Selected node at position {node.Position} for patrol point.");
                    return node;
                }

                // Remove this node  for next attempts.
                walkableNodes.RemoveAt(randomIndex);
            }

            // If no node was found with proper spacing, just pick any walkable node.
            if (walkableNodes.Count > 0)
            {
                Node fallbackNode = walkableNodes[Random.Range(0, walkableNodes.Count)];
                Debug.LogWarning(
                    "[ACTION] Patrol.cs: Using fallback node because couldn't find one with proper spacing.");
                return fallbackNode;
            }

            Debug.LogError("[ACTION] Patrol.cs: Could not find any suitable random walkable node.");
            return null;
        }

        private bool IsTooCloseToOtherPoints(Vector3 position)
        {
            float minDistance = gridManager.NodeSize * 3; // putting some distances between points TODO: explain magic numbers lol.
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
                Debug.LogError("[ACTION] Patrol.cs: No patrol points set. Generating random ones now.");
                GenerateRandomPatrolPoints();

                if (patrolPoints.Count == 0)
                {
                    Debug.LogError("[ACTION] Patrol.cs: Still no patrol points after generation attempt. Aborting.");
                    isPatrolling = false;
                    yield break;
                }
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
                        Debug.Log($"[ACTION] Patrol.cs: Point {i}: {patrolPoints[i].position}.");
                    }
                    else
                    {
                        Debug.LogError($"[ACTION] Patrol.cs: Point {i} is null.");
                    }
                }
            }

            while (waypointVisitCount < maxIterations && isPatrolling)
            {
                if (patrolPoints.Count == 0)
                {
                    Debug.LogError("[ACTION] Patrol.cs: List became empty during patrol?");
                    isPatrolling = false;
                    yield break;
                }

                if (currentWaypointIndex >= patrolPoints.Count)
                {
                    Debug.LogWarning(
                        $"[ACTION] Patrol.cs: Waypoint index {currentWaypointIndex} out of bounds, resetting waypoint index.");
                    currentWaypointIndex = 0;
                }

                Transform patrolPoint = patrolPoints[currentWaypointIndex];
                if (patrolPoint == null)
                {
                    Debug.LogWarning(
                        $"[ACTION] Patrol.cs: Patrol point at index {currentWaypointIndex} not found, skipping.");
                    currentWaypointIndex = (currentWaypointIndex + 1) % patrolPoints.Count;
                    
                    // Important: skip to next iteration if this point is null (bug fix).
                    continue;
                }

                Debug.Log(
                    $"[ACTION] Patrol.cs: Moving to waypoint {currentWaypointIndex + 1} of {patrolPoints.Count} at {patrolPoint.position}.");

                // Check if already at this checkpoint.
                float initialDistance = Vector3.Distance(transform.position, patrolPoint.position);
                if (initialDistance <= arrivalDistance)
                {
                    Debug.Log(
                        $"[ACTION] Patrol.cs: Already at waypoint {currentWaypointIndex + 1}, moving to next one.");
                    currentWaypointIndex = (currentWaypointIndex + 1) % patrolPoints.Count;
                    waypointVisitCount++;
                    // Move to next point.
                    continue;
                }

                // Move to the waypoint.
                pathfindingAgent.FollowPath(patrolPoint.position);

                // Wait til reaching waypoint or then show a timeout error.
                float startWaitTime = Time.time;
                float timeOut = 10f;
                bool reachedWaypoint = false;

                while (!reachedWaypoint && Time.time < startWaitTime + timeOut && isPatrolling)
                {
                    float distance = Vector3.Distance(transform.position, patrolPoint.position);

                    // Log distance around every second.
                    if (logging && Time.frameCount % 60 == 0)
                    {
                        Debug.Log(
                            $"[ACTION] Patrol.cs: Distance to waypoint {currentWaypointIndex + 1}: {distance:F2} (arrival threshold: {arrivalDistance}).");
                    }

                    if (distance <= arrivalDistance)
                    {
                        reachedWaypoint = true;
                        Debug.Log(
                            $"[ACTION] Patrol.cs: Reached waypoint {currentWaypointIndex + 1} at {Time.time - startWaitTime:F1} seconds.");
                    }

                    yield return null;
                }

                if (!reachedWaypoint)
                {
                    Debug.LogWarning(
                        $"[ACTION] Patrol.cs: Timed out trying to reach waypoint {currentWaypointIndex + 1}, moving to next one.");
                }
                else
                {
                    Debug.Log(
                        $"[ACTION] Patrol.cs: Waiting at waypoint {currentWaypointIndex + 1} for {patrolPointStopTime} seconds.");
                    yield return new WaitForSeconds(patrolPointStopTime);
                    Debug.Log(
                        $"[ACTION] Patrol.cs: Done waiting at waypoint {currentWaypointIndex + 1}. Moving to next one.");
                }

                currentWaypointIndex = (currentWaypointIndex + 1) % patrolPoints.Count;
                waypointVisitCount++;

                if (waypointVisitCount > 0 && waypointVisitCount % patrolPoints.Count == 0)
                {
                    Debug.Log($"[ACTION] Patrol.cs: Completed circuit!! Starting again.");
                }
            }

            Debug.Log(
                $"[ACTION] Patrol.cs: {gameObject.name} has finished patrolling after {waypointVisitCount} waypoints.");
            isPatrolling = false;
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

        private void OnDisable()
        {
            isPatrolling = false;
        }
    }
}