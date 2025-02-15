using System;
using NUnit.Framework.Internal.Execution;
using Unity.Android.Gradle;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace _Main_Project_Files._Scripts.Pathfinding
{
    public class GridManager : MonoBehaviour
    {
        [Header("- Grid Settings")] 
        [SerializeField] private Node nodePrefab;
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
    }
}
