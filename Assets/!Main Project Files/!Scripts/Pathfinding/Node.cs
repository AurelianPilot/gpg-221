using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Main_Project_Files._Scripts.Pathfinding
{
    public class Node : MonoBehaviour
    {
        [Header("- Node Properties")]
        public Vector3 Position { get; set; }
        public Node Parent { get; set; }
        public float GCost { get; set; } // The distance from the current position to the start (the distance already walked in).
        public float HCost { get; set; } // The distance from the end/objective.
        public float FCost => GCost + HCost;
        public bool Walkable { get; set; }

        [Header(" - Visual References")] 
        [SerializeField] private TextMeshProUGUI gCostText;
        [SerializeField] private TextMeshProUGUI hCostText;
        [SerializeField] private TextMeshProUGUI fCostText;
        [SerializeField] private MeshRenderer _meshRenderer;

        [SerializeField] private Color defaultColor;
        [SerializeField] private Color openColor;
        [SerializeField] private Color closedColor;
        [SerializeField] private Color pathColor;

        private void Awake()
        {
            Position = transform.position;
        }

        public void UpdateVisuals(NodeState state)
        {
            Color newColor = state switch
            {
                NodeState.Default => defaultColor,
                NodeState.Open => openColor,
                NodeState.Closed => closedColor,
                NodeState.Path => pathColor,
                _ => defaultColor
            };

            _meshRenderer.material.color = newColor;

            gCostText.text = $"G: {GCost:F1}";
            gCostText.text = $"H: {HCost:F1}";
            gCostText.text = $"F: {FCost:F1}";
        }

        public Node(Vector3 pos, bool walkable = true)
        {
            Position = pos;
            Walkable = walkable;
            GCost = float.MaxValue;
            HCost = 0;
        }
    }
}
