using System;
using System.Xml.Serialization;
using UnityEngine;

namespace _Main_Project_Files._Scripts.Pathfinding
{
    public class AgentController : MonoBehaviour
    {
        [Header("- Agent Settings")]
        [SerializeField] private GameObject agentPrefab;
        [SerializeField] private Vector3 startPosition;
        [SerializeField] private float agentYOffset = 0.5f;
        
        [Header("- References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private Astar astar;

        private GameObject agentInstance;
        private Agent agentComponent;
        private Camera mainCamera;

        private void Start()
        {
            if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
            if (astar == null) astar = FindObjectOfType<Astar>();
            mainCamera = Camera.main;

            CreateAgent();
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
                
                agentComponent = agentInstance.GetComponent<Agent>();
                // Dummy proofing.
                if (agentComponent == null)
                {
                    agentComponent = agentInstance.AddComponent<Agent>();
                }
            }
            else
            {
                Debug.LogError("AgentController.cs: Start position is outside of grid.");
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) && agentComponent != null)
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    Node targetNode = hit.collider.GetComponent<Node>();
                    if (targetNode != null && targetNode.Walkable)
                    {
                        agentComponent.FollowPath(targetNode.Position);
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