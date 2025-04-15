using System.Collections.Generic;
using _Main_Project_Files._Scripts.Agents;
using _Main_Project_Files._Scripts.Pathfinding;
using UnityEngine;

namespace _Main_Project_Files._Scripts
{
    public class GameManager : MonoBehaviour
    {
        [Header("- Game Settings")] [SerializeField]
        private bool autoStartSimulation = true;

        [SerializeField] private bool debugMode = true;

        [Header("- Team Setup")] [SerializeField]
        private GameObject agentPrefab;

        [SerializeField] private int agentsPerTeam = 3;
        [SerializeField] private Transform[] redTeamSpawnPoints;
        [SerializeField] private Transform[] blueTeamSpawnPoints;
        [SerializeField] private Transform[] greenTeamSpawnPoints;
        [SerializeField] private Transform[] yellowTeamSpawnPoints;

        [Header("- Grid Setup")] [SerializeField]
        private GridManager gridManager;

        private Dictionary<TeamColor, List<TeamAgent>> teamAgents = new Dictionary<TeamColor, List<TeamAgent>>();
        private Dictionary<TeamColor, Territory> teamTerritories = new Dictionary<TeamColor, Territory>();
        private Dictionary<TeamColor, Flag> teamFlags = new Dictionary<TeamColor, Flag>();
        private bool simulationRunning = false;

        private void Awake()
        {
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
            if (autoStartSimulation) StartSimulation();
        }

        private void Update()
        {
            if (Time.frameCount % 300 == 0)
            {
                TeamColor randomTeam = (TeamColor)Random.Range(0, 4);
                List<TeamAgent> agents = GetTeamAgents(randomTeam);
                if (agents.Count > 0)
                {
                    int randomAgentIndex = Random.Range(0, agents.Count);
                    if (randomAgentIndex < agents.Count)
                    {
                        TeamAgent agent = agents[randomAgentIndex];
                        if (agent != null && !agent.IsDead)
                        {
                            agent.SetGoal("TerritoryExpanded", true);
                            Debug.Log($"[GAME MANAGER] Prompting {agent.gameObject.name} to expand territory");
                        }
                    }
                }
            }
        }

        public void StartSimulation()
        {
            if (simulationRunning) return;
            Debug.Log("[GAME MANAGER] Starting capture the flag simulation!");
            if (CountAllAgents() == 0) SpawnTeams();
            simulationRunning = true;
        }

        public void StopSimulation()
        {
            if (!simulationRunning) return;
            Debug.Log("[GAME MANAGER] Stopping simulation!!!!!!!!!!!!!!!!!!!!!!!!!!! RAAAAAAAAAAAAAAAAAAAH");
            simulationRunning = false;
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
            if (!teamAgents.ContainsKey(team)) teamAgents[team] = new List<TeamAgent>();
            if (!teamAgents[team].Contains(agent)) teamAgents[team].Add(agent);
        }

        public void RemoveAgent(TeamAgent agent)
        {
            if (agent == null) return;
            TeamColor team = agent.TeamColor;
            if (!teamAgents.ContainsKey(team)) return;
            teamAgents[team].Remove(agent);
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
            if (teamFlags.ContainsKey(previousOwner) && teamFlags[previousOwner] == flag)
                teamFlags[previousOwner] = null;
            teamFlags[newOwner] = flag;
            Debug.Log($"[GAME MANAGER] {newOwner} team captured {previousOwner} team's flag. L.");
        }

        public void OnTerritoryChanged(Territory territory, TeamColor newOwner)
        {
            TeamColor previousOwner = territory.OwnerTeam;
            if (teamTerritories.ContainsKey(previousOwner) && teamTerritories[previousOwner] == territory)
                teamTerritories[previousOwner] = null;
            teamTerritories[newOwner] = territory;
        }

        public void OnAgentKilled(TeamAgent victim, TeamAgent killer)
        {
            if (victim == null) return;
            Debug.Log($"[GAME MANAGER] {victim.gameObject.name} ({victim.TeamColor}) was killed by {killer?.gameObject.name} ({killer?.TeamColor})!");
            RemoveAgent(victim);
            if (OnlyOneTeamRemains())
            {
                TeamColor survivingTeam = GetSurvivingTeam();
                CaptureAllFlagsForTeam(survivingTeam);
            }
        }

        private bool OnlyOneTeamRemains()
        {
            int teamsWithAgents = 0;
            foreach (var kvp in teamAgents)
            {
                if (kvp.Value.Count > 0) teamsWithAgents++;
            }
            return (teamsWithAgents == 1);
        }

        private TeamColor GetSurvivingTeam()
        {
            foreach (var kvp in teamAgents)
            {
                if (kvp.Value.Count > 0) return kvp.Key;
            }
            return TeamColor.Red;
        }

        private void CaptureAllFlagsForTeam(TeamColor winner)
        {
            foreach (var kvp in teamFlags)
            {
                Flag f = kvp.Value;
                if (f == null) continue;
                if (f.OwnerTeam != winner)
                {
                    f.SetOwner(winner);
                }
            }
        }

        private int CountAllAgents()
        {
            int count = 0;
            foreach (var team in teamAgents.Values) count += team.Count;
            return count;
        }

        public List<TeamAgent> GetTeamAgents(TeamColor team)
        {
            if (teamAgents.TryGetValue(team, out List<TeamAgent> agents))
                return new List<TeamAgent>(agents);
            return new List<TeamAgent>();
        }

        public Territory GetTeamTerritory(TeamColor team)
        {
            if (teamTerritories.TryGetValue(team, out Territory territory))
                return territory;
            return null;
        }

        public Flag GetTeamFlag(TeamColor team)
        {
            if (teamFlags.TryGetValue(team, out Flag flag))
                return flag;
            return null;
        }
    }
}
