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
        [SerializeField] private Astar astar;

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
            if (isPathFindingInProgress)
            {
                Debug.Log($"Agent.cs: Pathfinding already in progress to {targetPosition}, ignoring new request.");
            }
            targetPosition = newTargetPosition;
            StopPathFollowing();
            
            Vector3 startPosition = transform.position;
            
            astar.FindPath(startPosition, newTargetPosition);   

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
            
            float startTime = Time.time;
            while (Time.time < startTime + pathfindingTimeout)
            {
                currentPath = astar.CurrentPath;
                
                if (currentPath != null && currentPath.Count > 0)
                {
                    break;
                }
                
                yield return null;
            }

            isPathFindingInProgress = false;

            if (currentPath != null && currentPath.Count > 0)
            {
                followPathCoroutine = StartCoroutine(FollowPathCoroutine());

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
            }
        }

        private IEnumerator FollowPathCoroutine()
        {
            isFollowingPath = true;
            currentWaypointIndex = 0;
            
            float fixedHeight = transform.position.y;

            while (isFollowingPath && currentPath != null && currentWaypointIndex < currentPath.Count)
            {
                Node targetNode = currentPath[currentWaypointIndex];
                Vector3 nodePosition = targetNode.Position;
                
                nodePosition.y = fixedHeight;
                
                Vector3 directionToTarget = (nodePosition - transform.position).normalized;
                
                float distanceToTarget = Vector3.Distance(targetNode.Position, nodePosition);

                if (distanceToTarget < waypointReachedDistance)
                {
                    currentWaypointIndex++;

                    if (currentWaypointIndex >= currentPath.Count)
                    {
                        isFollowingPath = false;
                        Debug.Log($"Agent.cs: Path completed. Reached final destination near {targetPosition}.");
                        break;
                    }
                }

                if (directionToTarget != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
                
                transform.position += transform.forward * moveSpeed * Time.deltaTime;
                yield return null;
            }

            isFollowingPath = false;

            float finalDistance = Vector3.Distance(transform.position, targetPosition);

            if (finalDistance <= waypointReachedDistance * 2)
            {
                Debug.Log($"Agent.cs: Path completed. Reached {targetPosition}.");
            }
            else
            {
                Debug.LogWarning($"Agent.cs Path completed but agent is {finalDistance:F2} from the actual target.");
            }
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
        
        #endregion
    }
}
