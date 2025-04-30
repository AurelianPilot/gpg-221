using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vector3 = UnityEngine.Vector3;

namespace _Main_Project_Files.Leo._Scripts.Pathfinding
{
    /// <summary>
    /// Implements the Astar pathfinding algorithm to find the shortest path between two points in the grid.
    /// </summary>
    public class Astar : MonoBehaviour
    {
        // These lists are to track the nodes during the pathfinding process.
        private readonly List<Node> _openList = new();
        private readonly List<Node> _closeList = new();

        // public List<Node> CurrentPath { get; private set; } = new List<Node>();

        [Header("- Path Settings")] [SerializeField]
        private Vector3 startPosition;

        [SerializeField] private Vector3 goalPosition;

        private GridManager _grid;
        private Node _startNode;
        private Node _goalNode;
        private Node _currentNode;

        private void Start()
        {
            _grid = GetComponent<GridManager>();
        }

        #region Public Methods

        public List<Node> FindPath(Vector3 start, Vector3 goal)
        {
            // Get the indexes from the position of our nodes.
            _startNode = _grid.GetNodeIndex(start);
            _goalNode = _grid.GetNodeIndex(goal);

            if (_startNode == null || _goalNode == null)
            {
                Debug.LogError($"Astar.cs in {gameObject.name}: Start or goal position is outside the grid.");
                return new List<Node>(); 
            }

            // Clear to start fresh every time this method runs.
            _openList.Clear();
            _closeList.Clear();
            
            // Initializes the start node.
            _startNode.GCost = 0;
            _startNode.HCost = CalculateHCost(_startNode, _goalNode);

            // Adds the start node to the open list to begin processing.
            _startNode.GCost = 0;
            _startNode.HCost = CalculateHCost(_startNode, _goalNode);

            _openList.Add(_startNode);

            // Debug visualizer.
            //startNode.UpdateVisuals(NodeState.Open);
            //goalNode.UpdateVisuals(NodeState.Closed);

            // Pathfinding loop:
            while (_openList.Count > 0)
            {
                _currentNode = FindLowestFCostNode(_openList);

                if (_currentNode == _goalNode)
                {
                    // CHANGED: Return path instead of storing it
                    return RetracePath(_startNode, _goalNode);
                }

                // Move current node from the open to the closed list.
                _openList.Remove(_currentNode);
                _closeList.Add(_currentNode);

                _currentNode.UpdateVisuals(NodeState.Closed);

                var neighbors = GetNeighbors(_currentNode);

                foreach (var neighbor in neighbors)
                {
                    // Skip if the neighbor is not walkable or was already checked.
                    if (!neighbor.Walkable || _closeList.Contains(neighbor)) continue;

                    var tentativeGCost = _currentNode.GCost + CalculateDistance(_currentNode, neighbor);

                    if (tentativeGCost < neighbor.GCost || !_openList.Contains(neighbor))
                    {
                        neighbor.GCost = tentativeGCost;
                        neighbor.HCost = CalculateHCost(neighbor, _goalNode);
                        neighbor.Parent = _currentNode;

                        if (!_openList.Contains(neighbor))
                        {
                            _openList.Add(neighbor);
                            neighbor.UpdateVisuals(NodeState.Open);
                        }
                    }
                }
            }

            Debug.LogWarning($"Astar.cs in {gameObject.name}: No path found.");
            return new List<Node>();
        }

        #endregion

        #region Private Methods

        private void RunPathfinding()
        {
            ResetVisualization();
            
            List<Node> path = FindPath(startPosition, goalPosition);
            
            /*if (path.Count > 0)
            {
                foreach (var node in path)
                {
                    node.UpdateVisuals(NodeState.Path);
                }
            }*/
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

            _openList.Clear();
            _closeList.Clear();
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

        // Helping methods.
        private List<Node> RetracePath(Node node, Node goalNode)
        {
            var path = new List<Node>();
            var currentNode = goalNode;

            while (currentNode != _startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.Parent;
            }

            // This reverses the order:
            path.Reverse();

            // CurrentPath = new List<Node>(path);

            //foreach (var _node in path) _node.UpdateVisuals(NodeState.Path);

            Debug.Log($"Path found, {path.Count} nodes in path.");
            
            return path; // CHANGED: Return the path
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

        #endregion
    }
}