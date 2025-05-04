using UnityEngine;

namespace _Main_Project_Files.Leo._Scripts.Pathfinding
{
    public class PathfindingAgentController : MonoBehaviour
    {
        [Header("- Agent Settings")]
        [SerializeField] private GameObject agentPrefab;
        [SerializeField] private Vector3 startPosition;
        [SerializeField] private float agentYOffset = 0.5f;
        /// <summary>
        /// Don't activate this unless its to debug Astar, it will break the flag sim.
        /// </summary>
        [SerializeField] private bool spawnSingleAgent;
        
        [Header("- References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private Astar astar;

        private GameObject agentInstance;
        private PathFindingAgent pathFindingAgentComponent;
        private Camera mainCamera;

        private void Start()
        {
            if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
            if (astar == null) astar = FindObjectOfType<Astar>();
            mainCamera = Camera.main;

            if (spawnSingleAgent)
            {
                CreateAgent();
            }
        }

        private void CreateAgent()
        {
            Node startNode = gridManager.GetNodeIndex(startPosition);
            if (startNode != null)
            {
                Vector3 spawnPosition = startNode.Position;
                spawnPosition.y += agentYOffset;
                
                agentInstance = Instantiate(agentPrefab, spawnPosition, Quaternion.identity);
                agentInstance.name = "Astar following agent";
                
                pathFindingAgentComponent = agentInstance.GetComponent<PathFindingAgent>();
                // Dummy proofing.
                if (pathFindingAgentComponent == null)
                {
                    pathFindingAgentComponent = agentInstance.AddComponent<PathFindingAgent>();
                }
            }
            else
            {
                Debug.LogError("AgentController.cs: Start position is outside of grid.");
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) && pathFindingAgentComponent != null)
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    Node targetNode = hit.collider.GetComponent<Node>();
                    if (targetNode != null && targetNode.Walkable)
                    {
                        pathFindingAgentComponent.FollowPath(targetNode.Position);
                    }
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    Node targetNode = hit.collider.GetComponent<Node>();
                    if (targetNode != null)
                    {
                        targetNode.Walkable = !targetNode.Walkable;
                        targetNode.UpdateVisuals(targetNode.Walkable ? NodeState.Default : NodeState.Unwalkable);
                    }
                }
            }
        }
    }
}