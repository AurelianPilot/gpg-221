using System;
using NUnit.Framework.Internal.Execution;
using Unity.Android.Gradle;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace _Main_Project_Files._Scripts.Pathfinding
{
    public class GridManager : MonoBehaviour
    {
        [Header("- Grid Settings")] [SerializeField]
        private Node nodePrefab;

        [SerializeField] private int width = 10;
        [SerializeField] private int height = 10;
        [SerializeField] private float nodeSize = 1f;
        [SerializeField] private float nodeSpacing = 0.1f;

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
            // Setting this to a sum so the instantiation starts at the top left (personal preference).
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

                grid[i] = newNode;

                newNode.gameObject.name = $"Node_{x}_{z}";
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

            // For the X, subtract the grid's position from the world's position to get the relative position.
            float relativeX = worldPosition.x - transform.position.x + (width * actualNodeSize / 2);
            // For the Z, start from the grid's Z position, add half the grid's height to reach the top edge and subtract the world Z position. 
            float relativeZ = transform.position.z + (height * actualNodeSize / 2) - worldPosition.z;

            // This is to convert to grid coordinates:
            int x = Mathf.RoundToInt(relativeX / actualNodeSize);
            int z = Mathf.RoundToInt(relativeZ / actualNodeSize);

            // Check if the coordinates are within the grid bounds.
            if (x < 0 || x >= width || z < 0 || z >= height)
            {
                Debug.LogWarning($"Position {worldPosition} is outside the grid bounds.");
                return null;
            }

            int index = x + (z * width);

            // Check if the index is valid:
            if (index < 0 || index >= grid.Length)
            {
                Debug.LogWarning($"The calculated index '{index}' is out of bounds.");
                return null;
            }

            return grid[index];
        }
}
}
