using System;
using NUnit.Framework.Internal.Execution;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace _Main_Project_Files._Scripts.Pathfinding
{
    /// <summary>
    /// Manages the grid of nodes used for pathfinding. Creates the grid with nodes inside.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [Header("- Grid Settings")] [SerializeField]
        private Node nodePrefab;

        public Node[] Nodes => grid;
        public int Width => width;
        public int Height => height;
        public float NodeSize => nodeSize;
        public float NodeSpacing => nodeSpacing;
        
        [SerializeField] private int width = 10;
        [SerializeField] private int height = 10;
        
        [SerializeField] private float nodeSize = 1f;
        [SerializeField] private float nodeSpacing = 0.1f;

        [Header("- Obstacle Detection")]
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private float raycastHeight = 10f;
        [SerializeField] private float raycastRadius = 0.45f;
        [SerializeField] private bool detectObstacles = true;
        
        [Header("- Grid Debug")] [SerializeField]
        private bool isDebug = true;

        private Node[] grid;

        private void Start()
        {
            CreateGrid();
        }

        private void CreateGrid()
        {
            grid = new Node[width * height];

            float actualNodeSize = nodeSize + nodeSpacing;
            float startX = transform.position.x - (width * actualNodeSize / 2);
            float startZ = transform.position.z + (height * actualNodeSize / 2);

            GameObject nodesParent = new GameObject("Nodes");
            nodesParent.transform.parent = transform;

            for (int i = 0; i < grid.Length; i++)
            {
                int x = i % width;
                int z = i / width;

                Vector3 nodePosition = new Vector3(
                    startX + (x * actualNodeSize),
                    transform.position.y,
                    // Setting this to a substraction so the instantiation starts at the top left (personal preference).
                    startZ - (z * actualNodeSize)
                );

                Node newNode = CreateNode(nodePosition, nodesParent.transform);
                newNode.Index = i;
                grid[i] = newNode;

                newNode.gameObject.name = $"Node_{x}_{z}";
                
                // Look for obstacles if asked to.
                CheckForObstacle(newNode);
            }
        }

        private void CheckForObstacle(Node node)
        {
            Vector3 raycastStar = node.Position + Vector3.up * raycastHeight;
            bool hasObstacle = Physics.CheckSphere(node.Position, raycastRadius * nodeSize, obstacleMask);

            if (hasObstacle)
            {
                node.Walkable = false;
                node.SetWalkable(false);

                if (isDebug)
                {
                    Debug.Log($"Obstacle detected at {node.Position}.");
                }
            }
        }

        private Node CreateNode(Vector3 nodePosition, Transform nodesParentTransform)
        {
            Node node = Instantiate(nodePrefab, nodePosition, quaternion.identity, nodesParentTransform);

            node.transform.localScale = Vector3.one * nodeSize;

            return node;
        }

        public Node GetNodeIndex(Vector3 worldPosition)
        {
   
            float actualNodeSize = nodeSize + nodeSpacing;

            // This is to sync the actual world position and make it relative to the grid's origin.
            float relativeX = worldPosition.x - transform.position.x + (width * actualNodeSize / 2);
            float relativeZ = transform.position.z + (height * actualNodeSize / 2) - worldPosition.z;

            // This is to convert to grid coordinates:
            int x = Mathf.RoundToInt(relativeX / actualNodeSize);
            int z = Mathf.RoundToInt(relativeZ / actualNodeSize);

            Debug.Log($"Calculated grid coordinates: x={x}, z={z}");

            // Check if the coordinates are within the grid bounds.
            if (x < 0 || x >= width || z < 0 || z >= height)
            {
                Debug.LogWarning($"GridManager.cs in {gameObject.name}: Position {worldPosition} is outside the grid bounds.");
                return null;
            }

            int index = x + (z * width);

            // Check if the grid array exists
            if (grid == null)
            {
                Debug.LogError("Grid array is null. Make sure CreateGrid has been called.");
                return null;
            }

            // Check if the index is valid:
            if (index < 0 || index >= grid.Length)
            {
                Debug.LogWarning($"GridManager.cs in {gameObject.name}: The calculated index '{index}' is out of bounds.");
                return null;
            }

            return grid[index];
        }
    }
}
