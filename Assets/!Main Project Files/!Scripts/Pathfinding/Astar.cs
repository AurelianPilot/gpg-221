using System;
using System.Collections.Generic;
using _Main_Project_Files._Scripts.Pathfinding;
using UnityEngine;
using UnityEngine.UI;
using Vector3 = UnityEngine.Vector3;

public class Astar : MonoBehaviour
{
    // These lists are to track the nodes during the pathfinding process.
    private readonly List<Node> openList = new();
    private readonly List<Node> closeList = new();
    
    // In here I'm storing the current path for external access (for the agent).
    public List<Node> CurrentPath { get; private set; } = new List<Node>();

    [Header("- Path Settings")] [SerializeField]
    private Vector3 startPosition;

    [Header("- Input Settings")] [SerializeField]
    private KeyCode runPathfindingKeyCode = KeyCode.Space;

    [SerializeField] private Button pathfindingButton;

    [SerializeField] private Vector3 goalPosition;

    private GridManager _grid;
    private Node startNode;
    private Node goalNode;
    private Node currentNode;

    private void Start()
    {
        _grid = GetComponent<GridManager>();

        // UI initialization/dummy prevention.
        if (pathfindingButton != null) pathfindingButton.onClick.AddListener(RunPathfinding);
    }

    private void Update()
    {
        if (Input.GetKeyDown(runPathfindingKeyCode))
        {
            RunPathfinding();
        }
    }

    private void RunPathfinding()
    {
        ResetVisualization();
        FindPath(startPosition, goalPosition);
    }

    private void ResetVisualization()
    {
        var allNodes = FindObjectsOfType<Node>();

        // Resets the nodes.
        foreach (var node in allNodes)
        {
            // Don't set the costs to 0 because it messes the script when running the pathfinding again.
            node.GCost = float.MaxValue;
            node.HCost = float.MaxValue;
            node.Parent = null;
            node.UpdateVisuals(NodeState.Default);
        }

        openList.Clear();
        closeList.Clear();
        CurrentPath.Clear();
    }

    // Calculate the Manhattan distance between two nodes.
    private float CalculateDistance(Node nodeA, Node nodeB)
    {
        return Mathf.Abs(nodeA.Position.x - nodeB.Position.x) +
               Mathf.Abs(nodeA.Position.z - nodeB.Position.z);
    }

    // Calculate the distance from a node to the goal.
    private float CalculateHCost(Node node, Node goalNode)
    {
        return CalculateDistance(node, goalNode);
    }

    public void FindPath(Vector3 start, Vector3 goal)
    {
        // Get the indexes from the position of our nodes.
        startNode = _grid.GetNodeIndex(start);
        goalNode = _grid.GetNodeIndex(goal);

        if (startNode == null || goalNode == null)
        {
            Debug.LogError($"Astar.cs in {gameObject.name}: Start or goal position is outside the grid.");
            return;
        }

        // Clear to start fresh every time this method runs.
        openList.Clear();
        closeList.Clear();
        CurrentPath.Clear();

        // Initializes the start node.
        startNode.GCost = 0;
        startNode.HCost = CalculateHCost(startNode, goalNode);

        // Adds the start node to the open list to begin processing.
        startNode.GCost = 0;
        startNode.HCost = CalculateHCost(startNode, goalNode);

        openList.Add(startNode);

        // Debug visualizer.
        startNode.UpdateVisuals(NodeState.Open);
        goalNode.UpdateVisuals(NodeState.Closed);

        // Pathfinding loop:
        while (openList.Count > 0)
        {
            currentNode = FindLowestFCostNode(openList);

            if (currentNode == goalNode)
            {
                RetracePath(startNode, goalNode);
                return;
            }

            // Move current node from the open to the closed list.
            openList.Remove(currentNode);
            closeList.Add(currentNode);

            currentNode.UpdateVisuals(NodeState.Closed);

            var neighbors = GetNeighbors(currentNode);

            foreach (var neighbor in neighbors)
            {
                // Skip if the neighbor is not walkable or was already checked.
                if (!neighbor.Walkable || closeList.Contains(neighbor)) continue;

                var tentativeGCost = currentNode.GCost + CalculateDistance(currentNode, neighbor);

                if (tentativeGCost < neighbor.GCost || !openList.Contains(neighbor))
                {
                    neighbor.GCost = tentativeGCost;
                    neighbor.HCost = CalculateHCost(neighbor, goalNode);
                    neighbor.Parent = currentNode;

                    if (!openList.Contains(neighbor))
                    {
                        openList.Add(neighbor);
                        neighbor.UpdateVisuals(NodeState.Open);
                    }
                }
            }
        }

        Debug.LogWarning($"Astar.cs in {gameObject.name}: No path found.");
    }


    // Helping methods.
    private void RetracePath(Node node, Node goalNode)
    {
        CurrentPath = new List<Node>();
        
        // List that stores the current path.
        var path = new List<Node>();
        var currentNode = goalNode;

        // This time it starts from the goal and goes until it reaches the start.
        while (currentNode != startNode)
        {
            CurrentPath.Add(currentNode);
            path.Add(currentNode);
            // This is the previous node (aka parent).
            currentNode = currentNode.Parent;
        }

        // This reverses the order:
        path.Reverse();

        foreach (var _node in path) node.UpdateVisuals(NodeState.Path);

        Debug.Log($"Path found, {path.Count} nodes in path.");
    }

    private Node FindLowestFCostNode(List<Node> nodeList)
    {
        // Default to prevent null.
        var lowestFCostNode = nodeList[0];

        // Start at 1 for better understanding, and look through all nodes.
        for (var i = 1; i < nodeList.Count; i++)
            // If the node's F cost is lower than the lowest F cost, it becomes the best choice.
            if (nodeList[i].FCost < lowestFCostNode.FCost)
                lowestFCostNode = nodeList[i];
            // If the F costs are the same then grab the one with the lower H cost (closer to the end).
            else if (nodeList[i].FCost == lowestFCostNode.FCost && nodeList[i].HCost < lowestFCostNode.HCost)
                lowestFCostNode = nodeList[i];
        return lowestFCostNode;
    }

    private List<Node> GetNeighbors(Node node)
    {
        var neighbors = new List<Node>();

        var x = node.Index % _grid.Width;
        var z = node.Index / _grid.Width;

        // Right neighbor:
        if (x < _grid.Width - 1) neighbors.Add(_grid.Nodes[node.Index + 1]);

        // Left neighbor:
        if (x > 0) neighbors.Add(_grid.Nodes[node.Index - 1]);

        // Down neighbor:
        if (z < _grid.Height - 1) neighbors.Add(_grid.Nodes[node.Index + _grid.Width]);

        // Up neighbor:
        if (z > 0) neighbors.Add(_grid.Nodes[node.Index - _grid.Width]);

        return neighbors;
    }
}