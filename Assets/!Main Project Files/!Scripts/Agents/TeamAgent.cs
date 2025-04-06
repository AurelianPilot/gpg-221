using _Main_Project_Files._Scripts.GOAP;
using _Main_Project_Files._Scripts.GOAP.Actions;
using _Main_Project_Files._Scripts.Pathfinding;
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
            if (meshRenderer == null) meshRenderer = GetComponentInChildren<MeshRenderer>();
            gameManager = FindObjectOfType<GameManager>();
            UpdateTeamColorVisual();
        }

        private void Start()
        {
            Agent pathAgent = GetPathfindingAgent();
            if (pathAgent != null)
            {
                if (pathAgent.astar == null)
                {
                    pathAgent.astar = FindObjectOfType<Astar>();
                    Debug.Log($"[TEAM AGENT] Set missing Astar reference for {gameObject.name}");
                }
            }

            if (homeBase == null)
            {
                homeBase = new GameObject($"{teamColor}HomeBase_{gameObject.name}").transform;
                homeBase.position = transform.position;
                homeBase.parent = transform;
                Debug.Log($"[TEAM AGENT] Created home base for {gameObject.name} ({teamColor}) at {transform.position}");
            }

            if (gameManager != null) gameManager.RegisterAgent(this);
            WorldState.SetState("IsDead", false);
            WorldState.SetState("HasFlag", false);
            WorldState.SetState("IsInHomeTerritory", true);

            Patrol patrolAction = GetComponent<Patrol>();
            if (patrolAction == null)
            {
                patrolAction = gameObject.AddComponent<Patrol>();
                Debug.Log($"[TEAM AGENT] Added missing Patrol action to {gameObject.name}");
            }
            patrolAction.ForceGenerateRandomPoints();

            SetGoal("IsPatrolling", true);
        }

        private void UpdateTeamColorVisual()
        {
            if (meshRenderer == null) return;
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

            if (gameManager != null) gameManager.RemoveAgent(this);
            Destroy(gameObject);
        }

        public void SetAgentState(AgentState newState)
        {
            if (isDead && newState != AgentState.Respawning) return;
            currentState = newState;
        }

        public void OnSeeEnemy(TeamAgent enemyAgent)
        {
            if (isDead || enemyAgent.IsDead) return;
            Debug.Log($"[TEAM AGENT] {gameObject.name} ({teamColor}) spotted enemy {enemyAgent.gameObject.name} ({enemyAgent.TeamColor})!");
            SetGoal("EnemyEliminated", true);
        }

        public void OnSeeFlag(Flag flag)
        {
            if (isDead) return;
            Debug.Log($"[TEAM AGENT] {gameObject.name} ({teamColor}) spotted flag at {flag.transform.position}!");
            if (flag.OwnerTeam != teamColor) SetGoal("FlagCaptured", true);
        }

        public void OnEnterTerritory(TeamColor territoryColor)
        {
            if (isDead) return;
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
