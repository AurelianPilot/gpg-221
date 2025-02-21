using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Astar : MonoBehaviour
{
    private List<Node> openList = new List<Node>();
    private List<Node> closeList = new List<Node>();

    [SerializeField] private Vector3 startPosition;
    [SerializeField] private Vector3 goalPosition;

    private Node startNode;
    private Node goalNode;

    private void Start()
    {
        grid = GetComponent<Grid>();

        startNode = grid.GetNode(startPosition);
    }
}
