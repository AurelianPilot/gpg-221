using System;
using System.Net.Cache;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Main_Project_Files._Scripts.Pathfinding
{
    /// <summary>
    /// This is a single node in the pathfinding grid, stores data and handles debug visuals.
    /// </summary>
    public class Node : MonoBehaviour, IComparable
    {
        [Header("- Node Properties")] [SerializeField]
        private bool isWalkable = true;
        
        public Vector3 Position { get; set; }
        public Node Parent { get; set; }
        public float GCost { get; set; } // The distance from the current position to the start (the distance already walked in).
        public float HCost { get; set; } // The distance from the end/objective.
        public float FCost => GCost + HCost;
        public bool Walkable { get; set; }
        public int Index { get; set; }

        [Header(" - Visual References")] 
        [SerializeField] private TextMeshProUGUI gCostText;
        [SerializeField] private TextMeshProUGUI hCostText;
        [SerializeField] private TextMeshProUGUI fCostText;
        [SerializeField] private MeshRenderer _meshRenderer;

        [SerializeField] private Color defaultColor;
        [SerializeField] private Color openColor;
        [SerializeField] private Color closedColor;
        [SerializeField] private Color pathColor;
        [SerializeField] private Color unwalkableColor;

        private void Awake()
        {
            if (_meshRenderer == null)
            {
                _meshRenderer = GetComponent<MeshRenderer>();
            }
            Position = transform.position;
            Walkable = isWalkable;
        }

        public void SetWalkable(bool walkable)
        {
            isWalkable = walkable;
            UpdateVisuals(isWalkable ? NodeState.Default : NodeState.Unwalkable);
        }
        
        public void UpdateVisuals(NodeState state)
        {
            if (_meshRenderer == null) return;

            if (!isWalkable)
            {
                _meshRenderer.material.color = unwalkableColor;
                return;
            }
            
            Color newColor = state switch
            {
                NodeState.Default => defaultColor,
                NodeState.Open => openColor,
                NodeState.Closed => closedColor,
                NodeState.Path => pathColor,
                NodeState.Unwalkable => unwalkableColor,
                _ => defaultColor
            };

            _meshRenderer.material.color = newColor;

            gCostText.text = $"G: {GCost:F1}";
            hCostText.text = $"H: {HCost:F1}";
            fCostText.text = $"F: {FCost:F1}";
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            if (obj is Node otherNode)
            {
                int comparison = FCost.CompareTo(otherNode.FCost);

                if (comparison == 0)
                {
                    comparison = HCost.CompareTo(otherNode.HCost);
                }
                
                return comparison;
            }
            else
            {
                // Argument exception and not debug log error because it lets me not return anything.
                throw new ArgumentException("Object is not a Node");
            }
        }
    }
}
