using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

namespace _Main_Project_Files._Scripts.Pathfinding
{
    public class Agent : MonoBehaviour
    {
        /// <summary>
        /// Controls the movement of an agent in a path calculated by the Astar algorithm.
        /// </summary>
        [Header("- Agent Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 100f;
        [SerializeField] private float waypointReachedDistance = 0.1f;
        [SerializeField] private float pathfindingTimeout = 2f;
        [SerializeField] private bool logging = true;
    
        [Header("- References")]
        [SerializeField] public Astar astar;

        private List<Node> currentPath;
        private int currentWaypointIndex;
        private bool isFollowingPath;
        private bool isPathFindingInProgress;
        private Vector3 targetPosition;
    
        private Coroutine followPathCoroutine;
    
        void Awake()
        {
            // Dummy proofing.
            if (astar == null)
            {
                astar = FindObjectOfType<Astar>();
                if (astar == null)
                {
                    Debug.LogError("Agent.cs: No Astar found in scene.");
                }
            }
        }

        #region Public Methods
        
        public void FollowPath(Vector3 newTargetPosition)
        {
            /*if (isPathFindingInProgress)
            {
                Debug.Log($"Agent.cs: Pathfinding already in progress to {targetPosition}, ignoring new request to {newTargetPosition}");
                return;
            }*/
    
            targetPosition = newTargetPosition;
            StopPathFollowing();
    
            Vector3 startPosition = transform.position;
            Debug.Log($"Agent.cs: Finding path from {startPosition} to {targetPosition}");
    
            astar.FindPath(startPosition, targetPosition);

            StartCoroutine(WaitForPathAndFollow());
        }

        public bool IsFollowingPath()
        {
            return isFollowingPath;
        }

        public Vector3 GetTargetPosition()
        {
            return targetPosition;
        }
        
        #endregion

        #region Private Methods
        private IEnumerator WaitForPathAndFollow()
        {
            isPathFindingInProgress = true;
    
            // CHANGED: Get path directly from FindPath call
            Vector3 startPosition = transform.position;
            Debug.Log($"Agent.cs: Finding path from {startPosition} to {targetPosition}");
    
            // Call FindPath and store the result directly
            currentPath = astar.FindPath(startPosition, targetPosition);
    
            isPathFindingInProgress = false;

            if (currentPath != null && currentPath.Count > 0)
            {
                followPathCoroutine = StartCoroutine(FollowPathCoroutine());
                Debug.Log($"Agent.cs: Path found with {currentPath.Count} nodes. Following path to {targetPosition}");

                if (logging)
                {
                    for (int i = 0; i < currentPath.Count; i++)
                    {
                        Debug.Log($"Agent.cs: Path node {i}: {currentPath[i].Position}");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Agent.cs: No path found or pathfinding timed out when trying to reach {targetPosition}");
                // Add direct movement fallback
                StartCoroutine(DirectMovementFallback());
            }

            yield return null;
        }
        
        private IEnumerator FollowPathCoroutine()
        {
            isFollowingPath = true;
            currentWaypointIndex = 0;
            
            float fixedHeight = transform.position.y;
            
            if (logging)
            {
                Debug.Log($"Agent.cs: Started following path with {currentPath.Count} nodes");
            }

            while (isFollowingPath && currentPath != null && currentWaypointIndex < currentPath.Count)
            {
                if ((currentPath == null || currentPath.Count == 0) && targetPosition != Vector3.zero)
                {
                    // Use direct movement if no path was found.
                    StartCoroutine(DirectMovementFallback());
                }
                
                Node targetNode = currentPath[currentWaypointIndex];
                Vector3 nodePosition = targetNode.Position;
                
                nodePosition.y = fixedHeight;
                
                // Debug log current status every .5 seconds.(debug)
                if (logging && Time.frameCount % 30 == 0) 
                {
                    float distanceToNode = Vector3.Distance(transform.position, nodePosition);
                    Debug.Log($"Agent.cs: Moving to node {currentWaypointIndex}/{currentPath.Count-1}, distance: {distanceToNode:F2}");
                }
                
                Vector3 directionToTarget = (nodePosition - transform.position).normalized;
                
                float distanceToTarget = Vector3.Distance(transform.position, nodePosition);

                if (distanceToTarget < waypointReachedDistance)
                {
                    if (logging)
                    {
                        Debug.Log($"Agent.cs: Reached node {currentWaypointIndex} at position {nodePosition}");
                    }
                    
                    currentWaypointIndex++;
                
                    // If the end was reached:
                    if (currentWaypointIndex >= currentPath.Count)
                    {
                        isFollowingPath = false;
                        Debug.Log($"Agent.cs: Path completed. Reached final destination near {targetPosition}");
                        break;
                    }
                    continue;
                }

                // Rotate agent towards target.
                if (directionToTarget != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
                
                transform.position += transform.forward * moveSpeed * Time.deltaTime;
                Debug.Log($"Agent.cs: Moving agent {gameObject.name}, current pos: {transform.position}");
                yield return null;
            }

            isFollowingPath = false;

            // Calculate final distance to target to check if it actually arrived.
            float finalDistance = Vector3.Distance(transform.position, targetPosition);
            Debug.Log($"Agent.cs: Path following complete. Final distance to target: {finalDistance:F2}");
            
            // Check if the agent is in a certain threshold distance from the target.
            if (finalDistance <= waypointReachedDistance * 2)
            {
                Debug.Log("Agent.cs: Successfully reached target!");
            }
            else
            {
                Debug.LogWarning($"Agent.cs: Path completed but agent is still {finalDistance:F2} units away from the target.");
            }
        }

        private IEnumerator DirectMovementFallback()
        {
            Debug.Log($"Agent.cs: Using direct movement to {targetPosition}");
    
            while (Vector3.Distance(transform.position, targetPosition) > waypointReachedDistance)
            {
                Vector3 direction = (targetPosition - transform.position).normalized;
                transform.position += direction * moveSpeed * Time.deltaTime;
                transform.rotation = Quaternion.LookRotation(direction);
        
                yield return null;
            }
    
            Debug.Log($"Agent.cs: Reached target {targetPosition} using direct movement");
        }
        
        private void StopPathFollowing()
        {
            if (isFollowingPath)
            {
                Debug.Log($"Agent.cs: Stopping current path following.");
            }
            
            isFollowingPath = false;
            if (followPathCoroutine != null)
            {
                StopCoroutine(followPathCoroutine);
                followPathCoroutine = null;
            }
        }
        
        public Astar GetAstar()
        {
            return astar;
        }
        
        #endregion
    }
}