// 5) ExpandTerritory.cs
using System.Collections;
using System.Collections.Generic;
using _Main_Project_Files._Scripts.Agents;
using _Main_Project_Files._Scripts.Pathfinding;
using UnityEngine;

namespace _Main_Project_Files._Scripts.GOAP.Actions
{
    public class ExpandTerritory : Action
    {
        [Header("- Expansion Settings")]
        [SerializeField] private float expansionRange = 1.5f;
        [SerializeField] private string territoryExpandedStateName = "TerritoryExpanded";
        [SerializeField] private float nodeConversionTime = 2f;
        [SerializeField] private int maxExpansionsPerAction = 3;

        [SerializeField] private GameObject conversionEffectPrefab;
        [SerializeField] private float effectDuration = 1f;
        
        private GridManager gridManager;

        protected override void Awake()
        {
            actionName = "ExpandTerritory";
            isActionAchivable = true;
            if (preRequisites.Count == 0)
            {
                AddPreRequisite("IsDead", false);
                AddPreRequisite("HasEnergy", true);
            }
            if (effects.Count == 0)
            {
                AddEffect(territoryExpandedStateName, true);
            }
            teamAgent = GetComponent<TeamAgent>();
            gameManager = FindObjectOfType<GameManager>();
            gridManager = FindObjectOfType<GridManager>();
        }

        protected override IEnumerator PerformAction()
        {
            Debug.Log($"[ACTION] ExpandTerritory.cs: {gameObject.name} is looking for neutral nodes to claim.");
            teamAgent.SetAgentState(AgentState.ExpandingTerritory);
            Territory ourTerritory = null;
            if (gameManager != null && teamAgent != null)
            {
                ourTerritory = gameManager.GetTeamTerritory(teamAgent.TeamColor);
            }
            if (ourTerritory == null)
            {
                Debug.LogWarning("[ACTION] ExpandTerritory.cs: No territory found for our team.");
                yield break;
            }
            List<Node> expandableNodes = FindExpandableNodes(ourTerritory);
            if (expandableNodes.Count == 0)
            {
                Debug.Log("[ACTION] ExpandTerritory.cs: No expandable nodes found nearby.");
                yield break;
            }
            int expansionCount = 0;
            foreach (Node targetNode in expandableNodes)
            {
                if (expansionCount >= maxExpansionsPerAction) break;
                Vector3 nodePosition = targetNode.Position;
                Agent pathfindingAgent = teamAgent?.GetPathfindingAgent();
                if (pathfindingAgent != null)
                {
                    pathfindingAgent.FollowPath(nodePosition);
                    Debug.Log($"[ACTION] ExpandTerritory.cs: Moving to expandable node at {nodePosition}");
                }
                int maxIterations = 100;
                int currentIteration = 0;
                bool reachedNode = false;
                while (currentIteration < maxIterations && !reachedNode)
                {
                    float distanceToNode = Vector3.Distance(transform.position, nodePosition);
                    if (distanceToNode <= expansionRange)
                    {
                        reachedNode = true;
                        Debug.Log($"[ACTION] ExpandTerritory.cs: Reached expandable node, distance: {distanceToNode}");
                    }
                    currentIteration++;
                    yield return null;
                }
                if (!reachedNode)
                {
                    Debug.LogWarning("[ACTION] ExpandTerritory.cs: Failed to reach node, moving to next one.");
                    continue;
                }
                yield return StartCoroutine(ConvertNode(targetNode, ourTerritory));
                expansionCount++;
            }
            Debug.Log($"[ACTION] ExpandTerritory.cs: Territory expansion completed. Converted {expansionCount} nodes.");
            teamAgent.SetAgentState(AgentState.Patrolling);
        }

        private IEnumerator ConvertNode(Node node, Territory ourTerritory)
        {
            if (conversionEffectPrefab != null)
            {
                GameObject effect = Instantiate(conversionEffectPrefab,
                    node.Position + Vector3.up * 0.5f,
                    Quaternion.identity);
                Destroy(effect, effectDuration);
            }
            Debug.Log($"[ACTION] ExpandTerritory.cs: Converting node at {node.Position}...");
            yield return new WaitForSeconds(nodeConversionTime);
            bool success = ourTerritory.TryExpandTerritory(node, teamAgent.TeamColor);
            if (success)
            {
                Debug.Log($"[ACTION] ExpandTerritory.cs: Successfully claimed node at {node.Position}");
            }
            else
            {
                Debug.LogWarning($"[ACTION] ExpandTerritory.cs: Failed to claim node at {node.Position}");
            }
        }

        private List<Node> FindExpandableNodes(Territory teamTerritory)
        {
            List<Node> expandableNodes = new List<Node>();
            if (gridManager == null || teamTerritory == null) return expandableNodes;
            List<Node> territoryNodes = teamTerritory.TerritoryNodes;
            HashSet<Node> checkedNodes = new HashSet<Node>();
            foreach (Node territoryNode in territoryNodes)
            {
                List<Node> adjacentNodes = GetAdjacentNodes(territoryNode);
                foreach (Node adjacentNode in adjacentNodes)
                {
                    if (adjacentNode != null &&
                        adjacentNode.Walkable &&
                        !checkedNodes.Contains(adjacentNode) &&
                        !territoryNodes.Contains(adjacentNode))
                    {
                        checkedNodes.Add(adjacentNode);
                        bool belongsToTerritory = false;
                        foreach (TeamColor color in System.Enum.GetValues(typeof(TeamColor)))
                        {
                            Territory territory = gameManager.GetTeamTerritory(color);
                            if (territory != null && territory.TerritoryNodes.Contains(adjacentNode))
                            {
                                belongsToTerritory = true;
                                break;
                            }
                        }
                        if (!belongsToTerritory)
                        {
                            expandableNodes.Add(adjacentNode);
                        }
                    }
                }
            }
            expandableNodes.Sort((a, b) =>
                Vector3.Distance(transform.position, a.Position)
                    .CompareTo(Vector3.Distance(transform.position, b.Position)));
            return expandableNodes;
        }

        private List<Node> GetAdjacentNodes(Node node)
        {
            List<Node> adjacentNodes = new List<Node>();
            if (node == null || gridManager == null) return adjacentNodes;
            int nodeIndex = node.Index;
            int width = gridManager.Width;
            Node[] allNodes = gridManager.Nodes;
            if ((nodeIndex + 1) % width != 0 && nodeIndex + 1 < allNodes.Length) adjacentNodes.Add(allNodes[nodeIndex + 1]);
            if (nodeIndex % width != 0 && nodeIndex - 1 >= 0) adjacentNodes.Add(allNodes[nodeIndex - 1]);
            if (nodeIndex + width < allNodes.Length) adjacentNodes.Add(allNodes[nodeIndex + width]);
            if (nodeIndex - width >= 0) adjacentNodes.Add(allNodes[nodeIndex - width]);
            return adjacentNodes;
        }
    }
}
