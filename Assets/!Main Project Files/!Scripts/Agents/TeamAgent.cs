using System.Collections;
using _Main_Project_Files._Scripts.GOAP;
using _Main_Project_Files._Scripts.GOAP.Actions;
using _Main_Project_Files._Scripts.Pathfinding;
using UnityEditor.Build.Content;
using UnityEngine;

namespace _Main_Project_Files._Scripts.Agents
{
    public enum TeamColor
    {
        Red,
        Blue,
        Green,
        Yellow
    }

    public enum AgentState
    {
        Patrolling,
        Attacking,
        CapturingFlag,
        ExpandingTerritory,
        Respawning,
        Dead
    }

    public class TeamAgent : GoapAgent
    {
        [Header("- Team Settings")]
        [SerializeField] private TeamColor teamColor = TeamColor.Red;
        [SerializeField] private float respawnTime = 5f;
        [SerializeField] private Transform homeBase;
        [SerializeField] private MeshRenderer meshRenderer;
        
        [Header("- Agent State")]
        [SerializeField] private AgentState currentState = AgentState.Patrolling;

        private bool isDead = false;
        private GameManager gameManager;
        private Color teamColorValue;

        public TeamColor TeamColor => teamColor;
        public bool IsDead => isDead;
        public Transform HomeBase => homeBase;

        protected override void Awake()
        {
            base.Awake();
            
            if (meshRenderer == null)
                meshRenderer = GetComponentInChildren<MeshRenderer>();
                
            gameManager = FindObjectOfType<GameManager>();
            
            // Calling this to initialize team colors.
            UpdateTeamColorVisual();
        }
        
        private void Start()
        {
            Agent pathAgent = GetPathfindingAgent();
            if (pathAgent != null)
            {
                // Forcing a valid reference to Astar\
                if (pathAgent.astar == null)
                {
                    pathAgent.astar = FindObjectOfType<Astar>();
                    Debug.Log($"[TEAM AGENT] Set missing Astar reference for {gameObject.name}");
                }
            }
            
            // Create homeBase if it doesn't exist.
            if (homeBase == null)
            {
                homeBase = new GameObject($"{teamColor}HomeBase_{gameObject.name}").transform;
                homeBase.position = transform.position;
                homeBase.parent = transform;
                Debug.Log($"[TEAM AGENT] Created home base for {gameObject.name} ({teamColor}) at {transform.position}");
            }
    
            // Register the agent with game manager.
            if (gameManager != null)
                gameManager.RegisterAgent(this);
        
            // Initialize world state.
            WorldState.SetState("IsDead", false);
            WorldState.SetState("HasFlag", false);
            WorldState.SetState("IsInHomeTerritory", true);
    
            // Make sure the agent has a patrol action.
            Patrol patrolAction = GetComponent<Patrol>();
            if (patrolAction == null)
            {
                patrolAction = gameObject.AddComponent<Patrol>();
                Debug.Log($"[TEAM AGENT] Added missing Patrol action to {gameObject.name}");
            }
    
            // Generate patrol points if needed.
            patrolAction.ForceGenerateRandomPoints();
    
            // Set initial goal as patrolling to start.
            SetGoal("IsPatrolling", true);
        }
        
        private void UpdateTeamColorVisual()
        {
            if (meshRenderer == null) return;
            
            // Setting GO material color based on the team of course.
            teamColorValue = teamColor switch
            {
                TeamColor.Red => Color.red,
                TeamColor.Blue => Color.blue,
                TeamColor.Green => Color.green,
                TeamColor.Yellow => Color.yellow,
                _ => Color.gray
            };
            
            meshRenderer.material.color = teamColorValue;
        }
        
        public void SetTeamColor(TeamColor color)
        {
            teamColor = color;
            UpdateTeamColorVisual();
        }
        
        public void Die()
        {
            if (isDead) return;
            
            isDead = true;
            currentState = AgentState.Dead;
            WorldState.SetState("IsDead", true);
            
            // Disable the agent movement.
            Agent pathAgent = GetPathfindingAgent();
            if (pathAgent != null)
                pathAgent.enabled = false;
                
            // Disable the colliders.
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
                collider.enabled = false;
                
            // Visual feedback.
            if (meshRenderer != null)
                meshRenderer.enabled = false;
                
            // Start timer for respawn.
            StartCoroutine(RespawnAfterDelay());
            
            Debug.Log($"[TEAM AGENT] {gameObject.name} ({teamColor}) has died and will respawn in {respawnTime} seconds.");
        }
        
        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnTime);
            Respawn();
        }
        
        private void Respawn()
        {
            if (!isDead) return;
            
            isDead = false;
            currentState = AgentState.Patrolling;
            WorldState.SetState("IsDead", false);
            
            // Position agent to territory (homebase).
            if (homeBase != null)
                transform.position = homeBase.position;
                
            // Eenable components again.
            Agent pathAgent = GetPathfindingAgent();
            if (pathAgent != null)
                pathAgent.enabled = true;
                
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
                collider.enabled = true;
                
            if (meshRenderer != null)
                meshRenderer.enabled = true;
                
            // Reset goal.
            SetGoal("IsPatrolling", true);
            
            Debug.Log($"[TEAM AGENT] {gameObject.name} ({teamColor}) has respawned at home base!");
        }
        
        public void SetAgentState(AgentState newState)
        {
            if (isDead && newState != AgentState.Respawning) return;
            
            currentState = newState;
            
            // TODO: Update visualization or any state-specific behavior:
            // (ADD VISUAL INDICATORS FOR STATES!!)
        }
        
        // Override to handle team-specific functionality:
        public void OnSeeEnemy(TeamAgent enemyAgent)
        {
            if (isDead || enemyAgent.IsDead) return;
            
            Debug.Log($"[TEAM AGENT] {gameObject.name} ({teamColor}) spotted enemy {enemyAgent.gameObject.name} ({enemyAgent.TeamColor})!");
            
            // Goal is attack now.
            SetGoal("EnemyEliminated", true);
        }
        
        public void OnSeeFlag(Flag flag)
        {
            if (isDead) return;
            
            Debug.Log($"[TEAM AGENT] {gameObject.name} ({teamColor}) spotted flag at {flag.transform.position}!");
            
            // Check if flag isn't own team's flag.
            if (flag.OwnerTeam != teamColor)
            {
                // Goal is to capture flag now.
                SetGoal("FlagCaptured", true);
            }
        }

        public void OnEnterTerritory(TeamColor territoryColor)
        {
            if (isDead) return;
            
            // Update world state based on the territory agent is in.
            WorldState.SetState("IsInHomeTerritory", territoryColor == teamColor);
            
            if (territoryColor != teamColor)
            {
                Debug.Log($"[TEAM AGENT] {gameObject.name} ({teamColor}) entered {territoryColor} territory!");
            }
        }
        
        public AgentState GetAgentState()
        {
            return currentState;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (isDead) return;
            
            TeamAgent enemyAgent = other.GetComponentInParent<TeamAgent>();
            if (enemyAgent != null && enemyAgent.TeamColor != teamColor)
            {
                Debug.Log($"[TEAM AGENT] {gameObject.name} detected enemy {enemyAgent.gameObject.name}");
                OnSeeEnemy(enemyAgent);
                return;
            }
            
            Flag flag = other.GetComponentInParent<Flag>();
            if (flag != null && flag.OwnerTeam != teamColor)
            {
                Debug.Log($"[TEAM AGENT] {gameObject.name} detected flag");
                OnSeeFlag(flag);
                return;
            }
        }
    }
}