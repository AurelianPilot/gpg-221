using System.Collections.Generic;
using _Main_Project_Files._Scripts;
using _Main_Project_Files.Leo._Scripts.Agents;
using _Main_Project_Files.Leo._Scripts.Pathfinding;
using UnityEditor;
using UnityEngine;

namespace _Main_Project_Files.Leo._Scripts
{
    public class SimpleAgentSpawner : MonoBehaviour
    {
        [Header("- Spawn Settings")]
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private GameObject agentPrefab;
        [SerializeField] private float spawnOffsetY = 1f;
        
        [Header("- Team Configuration")]
        [SerializeField] private int agentsPerTeam = 2;
        [SerializeField] private float spawnAreaSize = 5f;
        
        [Header("- Team Base Positions")]
        [SerializeField] private Transform redTeamBase;
        [SerializeField] private Transform blueTeamBase;
        [SerializeField] private Transform greenTeamBase;
        [SerializeField] private Transform yellowTeamBase;
        
        [Header("- Team Flags")]
        [SerializeField] private GameObject flagPrefab;
        [SerializeField] private float flagOffset = 1f;
        
        private GridManager gridManager;
        private GameManager gameManager;
        private Dictionary<TeamColor, GameObject> spawnedFlags = new Dictionary<TeamColor, GameObject>();

        private void Awake()
        {
            gridManager = FindObjectOfType<GridManager>();
            gameManager = FindObjectOfType<GameManager>();
        }

        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnTeams();
                SpawnFlags();
                InitializeTerritories();
            }
        }
        
        [ContextMenu("Spawn Teams")]
        public void SpawnTeams()
        {
            if (agentPrefab == null)
            {
                Debug.LogError("[SPAWNER] Agent prefab not set.");
                return;
            }
            
            SpawnTeamAgents(TeamColor.Red, redTeamBase);
            SpawnTeamAgents(TeamColor.Blue, blueTeamBase);
            SpawnTeamAgents(TeamColor.Green, greenTeamBase);
            SpawnTeamAgents(TeamColor.Yellow, yellowTeamBase);
            
            Debug.Log("[SPAWNER] All teams spawned successfully!");
        }

        [ContextMenu("Spawn Flags")]
        public void SpawnFlags()
        {
            if (flagPrefab == null)
            {
                Debug.LogWarning("[SPAWNER] Flag prefab not set, skipping flag spawning.");
                return;
            }
            
            SpawnTeamFlag(TeamColor.Red, redTeamBase);
            SpawnTeamFlag(TeamColor.Blue, blueTeamBase);
            SpawnTeamFlag(TeamColor.Green, greenTeamBase);
            SpawnTeamFlag(TeamColor.Yellow, yellowTeamBase);
            
            Debug.Log("[SPAWNER] All flags spawned successfully!");
        }
        
        private void SpawnTeamAgents(TeamColor teamColor, Transform baseTransform)
        {
            if (baseTransform == null)
            {
                Debug.LogWarning($"[SPAWNER] No base position set for {teamColor} team!");
                return;
            }
            
            Vector3 basePosition = baseTransform.position;
            
            for (int i = 0; i < agentsPerTeam; i++)
            {
                // Calculate spawn position within team area.
                Vector3 spawnOffset = new Vector3(
                    Random.Range(-spawnAreaSize/2, spawnAreaSize/2),
                    0,
                    Random.Range(-spawnAreaSize/2, spawnAreaSize/2)
                );
                
                Vector3 spawnPosition = basePosition + spawnOffset;
                
                // Adjust height to match grid.
                if (gridManager != null)
                {
                    Node node = gridManager.GetNodeIndex(spawnPosition);
                    if (node != null)
                    {
                        spawnPosition = node.Position;
                        spawnPosition.y += spawnOffsetY;    
                    }
                }
                
                // Spawn agent.
                GameObject agent = Instantiate(agentPrefab, spawnPosition, Quaternion.identity);
                agent.name = $"{teamColor}Agent_{i}";
                agent.GetComponent<TeamAgent>().SetTeamColor(teamColor);
                
                Agent pathAgent = agent.GetComponent<Agent>();
                if (pathAgent == null)
                {
                    pathAgent = agent.AddComponent<Agent>();
                }
                Astar astar = FindObjectOfType<Astar>();
                if (astar != null)
                {
                    SerializedObject serializedObject = new SerializedObject(pathAgent);
                    serializedObject.FindProperty("astar").objectReferenceValue = astar;
                    serializedObject.ApplyModifiedProperties();
                }
                
                // FORCE THE Y POSITION (bug fix, idk why the previous one isn't working)
                agent.transform.position = new Vector3(
                    agent.transform.position.x,
                    spawnOffsetY,
                    agent.transform.position.z
                );
                
                // Set home base reference.
                GameObject homeBaseObj = new GameObject($"{teamColor}HomeBase_{i}");
                homeBaseObj.transform.position = spawnPosition;
                homeBaseObj.transform.parent = agent.transform;
                
                Debug.Log($"[SPAWNER] Spawned {teamColor} agent at {spawnPosition}");
            }
        }

        private void SpawnTeamFlag(TeamColor teamColor, Transform baseTransform)
        {
            if (baseTransform == null) return;
            
            Vector3 flagPosition = baseTransform.position + Vector3.up * flagOffset;
            
            GameObject flagObject = Instantiate(flagPrefab, flagPosition, Quaternion.identity);
            flagObject.name = $"{teamColor}Flag";
            
            Flag flagComponent = flagObject.GetComponent<Flag>();
            if (flagComponent == null)
                flagComponent = flagObject.AddComponent<Flag>();
                
            flagComponent.SetOwner(teamColor);
            
            // Store reference
            spawnedFlags[teamColor] = flagObject;
            
            Debug.Log($"[SPAWNER] Spawned {teamColor} flag at {flagPosition}");
        }

        private void InitializeTerritories()
        {
            // Create territories for each team base.
            InitializeTeamTerritory(TeamColor.Red, redTeamBase);
            InitializeTeamTerritory(TeamColor.Blue, blueTeamBase);
            InitializeTeamTerritory(TeamColor.Green, greenTeamBase);
            InitializeTeamTerritory(TeamColor.Yellow, yellowTeamBase);
        }
        
        private void InitializeTeamTerritory(TeamColor teamColor, Transform baseTransform)
        {
            if (baseTransform == null || gridManager == null) return;
            
            // Create territory object.
            GameObject territoryObj = new GameObject($"{teamColor}Territory");
            Territory territory = territoryObj.AddComponent<Territory>();
            territory.SetOwner(teamColor);
            
            // Find nodes near the base to claim as initial territory.
            Vector3 basePosition = baseTransform.position;
            
            // TODO: add this to the inspector somewhere.
            int territorySize = 4;
            
            Node baseNode = gridManager.GetNodeIndex(basePosition);
            if (baseNode == null) return;
            
            int baseIndex = baseNode.Index;
            int width = gridManager.Width;
            Node[] allNodes = gridManager.Nodes;
            
            // Claim nodes in a square around the base.
            for (int x = -territorySize; x <= territorySize; x++)
            {
                for (int z = -territorySize; z <= territorySize; z++)
                {
                    int targetIndex = baseIndex + x + (z * width);
                    
                    if (targetIndex >= 0 && targetIndex < allNodes.Length)
                    {
                        Node node = allNodes[targetIndex];
                        if (node != null && node.Walkable)
                        {
                            territory.AddNode(node);
                        }
                    }
                }
            }
            if (gameManager != null)
            {
                gameManager.RegisterTerritory(territory);
            }
            Debug.Log($"[SPAWNER] Initialized {teamColor} territory with {territory.TerritoryNodes.Count} nodes.");
        }
    }
}
