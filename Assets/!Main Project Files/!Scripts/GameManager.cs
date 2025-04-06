using System.Collections.Generic;
using _Main_Project_Files._Scripts.Agents;
using _Main_Project_Files._Scripts.Pathfinding;
using UnityEngine;

namespace _Main_Project_Files._Scripts
{
    public class GameManager : MonoBehaviour
    {
        [Header("- Game Settings")]
        [SerializeField] private bool autoStartSimulation = true;
        [SerializeField] private bool debugMode = true;
        
        [Header("- Team Setup")]
        [SerializeField] private GameObject agentPrefab;
        [SerializeField] private int agentsPerTeam = 3;
        [SerializeField] private Transform[] redTeamSpawnPoints;
        [SerializeField] private Transform[] blueTeamSpawnPoints;
        [SerializeField] private Transform[] greenTeamSpawnPoints;
        [SerializeField] private Transform[] yellowTeamSpawnPoints;
        
        [Header("- Grid Setup")]
        [SerializeField] private GridManager gridManager;
        
        private Dictionary<TeamColor, List<TeamAgent>> teamAgents = new Dictionary<TeamColor, List<TeamAgent>>();
        private Dictionary<TeamColor, Territory> teamTerritories = new Dictionary<TeamColor, Territory>();
        private Dictionary<TeamColor, Flag> teamFlags = new Dictionary<TeamColor, Flag>();
        private bool simulationRunning = false;
        
        private void Awake()
        {
            // Initialize dictionaries.
            foreach (TeamColor color in System.Enum.GetValues(typeof(TeamColor)))
            {
                teamAgents[color] = new List<TeamAgent>();
                teamTerritories[color] = null;
                teamFlags[color] = null;
            }
            
            if (gridManager == null)
                gridManager = FindObjectOfType<GridManager>();
        }
        
        private void Start()
        {
            if (autoStartSimulation)
                StartSimulation();
        }

        public void StartSimulation()
        {
            if (simulationRunning) return;
            
            Debug.Log("[GAME MANAGER] Starting capture the flag simulation!");
            
            // Initialize teams if they don't exist.
            if (CountAllAgents() == 0)
                SpawnTeams();
                
            simulationRunning = true;
        }

        public void StopSimulation()
        {
            if (!simulationRunning) return;
            
            Debug.Log("[GAME MANAGER] Stopping simulation!!!!!!!!!!!!!!!!!!!!!!!!!!! RAAAAAAAAAAAAAAAAAAAH");
            simulationRunning = false;
            
            // TODO: Could add code to pause agents here.
        }
        
        private void SpawnTeams()
        {
            SpawnTeam(TeamColor.Red, redTeamSpawnPoints);
            SpawnTeam(TeamColor.Blue, blueTeamSpawnPoints);
            SpawnTeam(TeamColor.Green, greenTeamSpawnPoints);
            SpawnTeam(TeamColor.Yellow, yellowTeamSpawnPoints);
        }

        private void SpawnTeam(TeamColor teamColor, Transform[] spawnPoints)
        {
            if (agentPrefab == null)
            {
                Debug.LogError("[GAME MANAGER] Cannot spawn teams: Agent prefab is not set!");
                return;
            }
            
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning($"[GAME MANAGER] No spawn points set for {teamColor} team!");
                return;
            }
            
            int spawnCount = Mathf.Min(agentsPerTeam, spawnPoints.Length);
            
            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 spawnPos = spawnPoints[i].position;
                GameObject newAgent = Instantiate(agentPrefab, spawnPos, Quaternion.identity);
                
                newAgent.name = $"{teamColor}Agent_{i}";
                newAgent.GetComponent<TeamAgent>().SetTeamColor(teamColor);
                
                // Set home base/territory.
                if (newAgent.GetComponent<TeamAgent>().HomeBase == null)
                    newAgent.transform.Find("HomeBase").transform.position = spawnPos;
                
                RegisterAgent(newAgent.GetComponent<TeamAgent>());
                
                if (debugMode)
                    Debug.Log($"[GAME MANAGER] Spawned {teamColor} agent at {spawnPos}");
            }
        }
        
        public void RegisterAgent(TeamAgent agent)
        {
            if (agent == null) return;
            
            TeamColor team = agent.TeamColor;
            
            if (!teamAgents.ContainsKey(team))
                teamAgents[team] = new List<TeamAgent>();
                
            if (!teamAgents[team].Contains(agent))
                teamAgents[team].Add(agent);
        }

        public void RegisterTerritory(Territory territory)
        {
            if (territory == null) return;
            
            TeamColor team = territory.OwnerTeam;
            teamTerritories[team] = territory;
        }

        public void RegisterFlag(Flag flag)
        {
            if (flag == null) return;
            
            TeamColor team = flag.OwnerTeam;
            teamFlags[team] = flag;
        }
        
        public void OnFlagCaptured(Flag flag, TeamColor newOwner)
        {
            TeamColor previousOwner = flag.OwnerTeam;
            
            // Remove from previous owner.
            if (teamFlags.ContainsKey(previousOwner) && teamFlags[previousOwner] == flag)
                teamFlags[previousOwner] = null;
                
            // Add to new owner.
            teamFlags[newOwner] = flag;
            Debug.Log($"[GAME MANAGER] {newOwner} team captured {previousOwner} team's flag. L.");
        }
        
        public void OnTerritoryChanged(Territory territory, TeamColor newOwner)
        {
            TeamColor previousOwner = territory.OwnerTeam;
            
            // Remove from previous owner.
            if (teamTerritories.ContainsKey(previousOwner) && teamTerritories[previousOwner] == territory)
                teamTerritories[previousOwner] = null;
                
            // Add to new owner.
            teamTerritories[newOwner] = territory;
            
            // TODO: Could add victory condition checks here.
        }
        
        public void OnAgentKilled(TeamAgent victim, TeamAgent killer)
        {
            if (victim == null) return;
            
            Debug.Log($"[GAME MANAGER] {victim.gameObject.name} ({victim.TeamColor}) was killed by {killer?.gameObject.name} ({killer?.TeamColor})!");
            
            // Respawn is handled by the agent itself.
        }

        private int CountAllAgents()
        {
            int count = 0;
            foreach (var team in teamAgents.Values)
            {
                count += team.Count;
            }
            return count;
        }
        
        /// <summary>
        /// Returns all agents of a specific team.
        /// </summary>
        /// <param name="team">Team.</param>
        /// <returns></returns>
        public List<TeamAgent> GetTeamAgents(TeamColor team)
        {
            if (teamAgents.TryGetValue(team, out List<TeamAgent> agents))
                return new List<TeamAgent>(agents);
                
            return new List<TeamAgent>();
        }

        /// <summary>
        /// Returns the territory for a team.
        /// </summary>
        /// <param name="team">Team.</param>
        /// <returns></returns>
        public Territory GetTeamTerritory(TeamColor team)
        {
            if (teamTerritories.TryGetValue(team, out Territory territory))
                return territory;
                
            return null;
        }

        /// <summary>
        /// Returns the flag for a team.
        /// </summary>
        /// <param name="team">Team.</param>
        /// <returns></returns>
        public Flag GetTeamFlag(TeamColor team)
        {
            if (teamFlags.TryGetValue(team, out Flag flag))
                return flag;
                
            return null;
        }
        
    }
}
