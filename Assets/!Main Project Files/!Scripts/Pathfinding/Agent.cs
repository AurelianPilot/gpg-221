using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Main_Project_Files._Scripts.Pathfinding
{
    public class Agent : MonoBehaviour
    {
        [Header("- Agent Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 100f;
        [SerializeField] private float waypointReachedDistance = 0.1f;
    
        [Header("- References")]
        [SerializeField] private Astar astar;

        private List<Node> currentPath;
        private int currentWaypointIndex;
        private bool isFollowingPath;
    
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

        public void FollowPath(Vector3 targetPosition)
        {
            Vector3 startPosition = transform.position;
        
            astar.FindPath(startPosition, targetPosition);

            StartCoroutine(WaitForPathAndFollow());
        }

        private IEnumerator WaitForPathAndFollow()
        {
            yield return null;
        
            // Getting the path from the Astar script.
            currentPath = astar.CurrentPath;

            if (currentPath != null && currentPath.Count > 0)
            {
                // Stop existing path follow.
                StopPathFollowing();
                // Start the trajectory.
                followPathCoroutine = StartCoroutine(FollowPathCoroutine());
            }
            else
            {
                Debug.LogError("Agent.cs: No path to follow.");
            }
        }

        private IEnumerator FollowPathCoroutine()
        {
            isFollowingPath = true;
            currentWaypointIndex = 0;

            while (isFollowingPath && currentPath != null && currentWaypointIndex < currentPath.Count)
            {
                // Get current target.
                Node targetNode = currentPath[currentWaypointIndex];
                Vector3 targetPosition = targetNode.transform.position;
            
                // Keep the agent Y position relative to the node (a little bit upwards),
                targetPosition.y = transform.position.y + 1;
            
                // Get direction to target.
                Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            
                // Get the distance.
                float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

                if (distanceToTarget < waypointReachedDistance)
                {
                    currentWaypointIndex++;
                
                    // If the end was reached:
                    if (currentWaypointIndex >= currentPath.Count)
                    {
                        isFollowingPath = false;
                        Debug.Log("Agent.cs: No path to follow.");
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
            
                // Move towards target.
                transform.position += transform.forward * moveSpeed * Time.deltaTime;
                yield return null;
            }
            isFollowingPath = false;
        }

        private void StopPathFollowing()
        {
            isFollowingPath = false;
            if (followPathCoroutine != null)
            {
                StopCoroutine(followPathCoroutine);
                followPathCoroutine = null;
            }
        }

        private void OnDrawGizmos()
        {
            if (!isFollowingPath || currentPath == null || currentPath.Count == 0) return;
        
            Gizmos.color = Color.red;

            for (int i = currentWaypointIndex; i < currentPath.Count - 1; i++)
            {
                Vector3 startPosition = currentPath[i].Position;
                Vector3 endPosition = currentPath[i + 1].Position;
            
                // Y above ground for visibility.
                startPosition.y += .2f;
                endPosition.y += .2f; 
            
                Gizmos.DrawLine(startPosition, endPosition);
            }

            if (currentWaypointIndex < currentPath.Count)
            {
                Vector3 currentTarget = currentPath[currentWaypointIndex].Position;
                currentTarget.y += .2f;
                Gizmos.DrawSphere(currentTarget, 0.2f);
            }
        
        }
    }
}
