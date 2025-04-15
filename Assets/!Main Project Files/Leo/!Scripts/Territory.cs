using System.Collections.Generic;
using _Main_Project_Files.Leo._Scripts.Agents;
using _Main_Project_Files.Leo._Scripts.Pathfinding;
using UnityEngine;

namespace _Main_Project_Files.Leo._Scripts
{
    public class Territory : MonoBehaviour
    {
        [Header("- Territory Settings")]
        [SerializeField] private TeamColor ownerTeam = TeamColor.Red;
        [SerializeField] private List<Node> territoryNodes = new List<Node>();
        [SerializeField] private Color neutralColor = Color.gray;
        
        private GameManager gameManager;

        public TeamColor OwnerTeam => ownerTeam;
        public List<Node> TerritoryNodes => territoryNodes;

        private void Awake()
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        private void Start()
        {
            // Initialize node colors.
            UpdateNodeColors();
            
            // Register with game manager.
            if (gameManager != null)
                gameManager.RegisterTerritory(this);
        }
        
        public void SetOwner(TeamColor newOwner)
        {
            if (ownerTeam == newOwner) return;
            
            ownerTeam = newOwner;
            UpdateNodeColors();
            
            Debug.Log($"[TERRITORY] Territory at {transform.position} is now owned by {ownerTeam} team!");
            
            // Notify game manager of territory change.
            if (gameManager != null)
                gameManager.OnTerritoryChanged(this, newOwner);
        }

        public void AddNode(Node node)
        {
            if (!territoryNodes.Contains(node))
            {
                territoryNodes.Add(node);
                UpdateNodeColor(node);
            }
        }

        public void RemoveNode(Node node)
        {
            if (territoryNodes.Contains(node))
            {
                territoryNodes.Remove(node);
                // Reset node to neutral color.
                if (node != null)
                {
                    node.UpdateVisuals(NodeState.Default);
                }
            }
        }

        private void UpdateNodeColors()
        {
            foreach (Node node in territoryNodes)
            {
                UpdateNodeColor(node);
            }
        }
        
        private void UpdateNodeColor(Node node)
        {
            if (node == null) return;
            
            // Get team color.
            Color teamColor = ownerTeam switch
            {
                TeamColor.Red => new Color(0.9f, 0.3f, 0.3f), 
                TeamColor.Blue => new Color(0.3f, 0.3f, 0.9f),
                TeamColor.Green => new Color(0.3f, 0.9f, 0.3f),
                TeamColor.Yellow => new Color(0.9f, 0.9f, 0.3f),
                _ => neutralColor
            };
            
            // Set node color.
            MeshRenderer nodeRenderer = node.GetComponent<MeshRenderer>();
            if (nodeRenderer != null)
            {
                nodeRenderer.material.color = teamColor;
            }
        }
        
        
        // Called by agents when they're expanding territory.
        public bool TryExpandTerritory(Node node, TeamColor expandingTeam)
        {
            // Can only expand from owned territory.
            if (ownerTeam != expandingTeam) return false;
            
            // Check if node is adjacent to any territory node.
            bool isAdjacent = IsNodeAdjacentToTerritory(node);
            
            if (isAdjacent && node.Walkable)
            {
                AddNode(node);
                return true;
            }
            
            return false;
        }
        
        private bool IsNodeAdjacentToTerritory(Node targetNode)
        {
            GridManager gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null) return false;
            
            int targetIndex = targetNode.Index;
            int gridWidth = gridManager.Width;
            
            foreach (Node territoryNode in territoryNodes)
            {
                int nodeIndex = territoryNode.Index;
                
                bool isAdjacent = 
                    nodeIndex + 1 == targetIndex || // right
                    nodeIndex - 1 == targetIndex || // left
                    nodeIndex + gridWidth == targetIndex || // down
                    nodeIndex - gridWidth == targetIndex;   // up
                
                // Yes im gonna use this till the day i die #optimization (in a million quotes)
                
                if (isAdjacent)
                    return true;
            }
            return false;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Notify agent they entered this territory.
            TeamAgent agent = other.GetComponentInParent<TeamAgent>();
            if (agent != null)
            {
                agent.OnEnterTerritory(ownerTeam);
            }
        }
    }
}
