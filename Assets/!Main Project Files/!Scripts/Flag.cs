using System.Collections;
using _Main_Project_Files._Scripts.Agents;
using _Main_Project_Files._Scripts.Pathfinding;
using UnityEngine;

namespace _Main_Project_Files._Scripts
{
    public class Flag : MonoBehaviour
    {
        [Header("- Flag Settings")] [SerializeField]
        private TeamColor ownerTeam = TeamColor.Red;

        [SerializeField] private float captureTime = 3f;
        [SerializeField] private MeshRenderer flagRenderer;
        [SerializeField] private Transform flagpole;

        [Header("- Capture Feedback")] [SerializeField]
        private Transform captureProgressIndicator;

        [SerializeField] private float maxProgressScale = 1f;

        private TeamAgent capturingAgent = null;
        private float captureProgress = 0f;
        private Coroutine captureCoroutine = null;
        private GameManager gameManager;

        public TeamColor OwnerTeam => ownerTeam;

        private void Awake()
        {
            if (flagRenderer == null)
                flagRenderer = GetComponentInChildren<MeshRenderer>();

            if (captureProgressIndicator != null)
                captureProgressIndicator.localScale = Vector3.zero;

            gameManager = FindObjectOfType<GameManager>();
        }

        private void Start()
        {
            UpdateFlagColor();
            if (gameManager != null)
                gameManager.RegisterFlag(this);
        }

        private void UpdateFlagColor()
        {
            if (flagRenderer == null) return;
            Color flagColor = ownerTeam switch
            {
                TeamColor.Red => Color.red,
                TeamColor.Blue => Color.blue,
                TeamColor.Green => Color.green,
                TeamColor.Yellow => Color.yellow,
                _ => Color.white
            };
            flagRenderer.material.color = flagColor;
        }

        public void SetOwner(TeamColor newOwner)
        {
            if (ownerTeam == newOwner) return;
            TeamColor previousOwner = ownerTeam;
            ownerTeam = newOwner;
            UpdateFlagColor();
            Debug.Log($"[FLAG] Flag at {transform.position} is now owned by {ownerTeam} team!");
            if (gameManager != null) gameManager.OnFlagCaptured(this, newOwner);
            UpdateTerritoryOwnership(previousOwner, newOwner);
        }

        private void UpdateTerritoryOwnership(TeamColor previousOwner, TeamColor newOwner)
        {
            if (gameManager == null) return;
            Territory previousTerritory = gameManager.GetTeamTerritory(previousOwner);
            Territory newTerritory = gameManager.GetTeamTerritory(newOwner);
            if (previousTerritory != null && newTerritory != null)
            {
                while (previousTerritory.TerritoryNodes.Count > 0)
                {
                    Node node = previousTerritory.TerritoryNodes[0];
                    previousTerritory.RemoveNode(node);
                    newTerritory.AddNode(node);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TeamAgent agent = other.GetComponentInParent<TeamAgent>();
            if (agent != null && !agent.IsDead && agent.TeamColor != ownerTeam)
            {
                BeginCapture(agent);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            TeamAgent agent = other.GetComponentInParent<TeamAgent>();
            if (agent != null && agent == capturingAgent)
            {
                CancelCapture();
            }
        }

        private void BeginCapture(TeamAgent agent)
        {
            CancelCapture();
            capturingAgent = agent;
            agent.SetAgentState(AgentState.CapturingFlag);
            Debug.Log($"[FLAG] {agent.gameObject.name} ({agent.TeamColor}) is attempting to capture {ownerTeam} flag!");
            captureCoroutine = StartCoroutine(CaptureProcess());
        }

        private void CancelCapture()
        {
            if (captureCoroutine != null)
            {
                StopCoroutine(captureCoroutine);
                captureCoroutine = null;
            }
            captureProgress = 0f;
            if (captureProgressIndicator != null)
                captureProgressIndicator.localScale = Vector3.zero;
            if (capturingAgent != null)
            {
                capturingAgent.SetAgentState(AgentState.Patrolling);
                capturingAgent = null;
            }
        }

        private IEnumerator CaptureProcess()
        {
            captureProgress = 0f;
            while (captureProgress < captureTime)
            {
                captureProgress += Time.deltaTime;
                if (captureProgressIndicator != null)
                {
                    float progressScale = (captureProgress / captureTime) * maxProgressScale;
                    captureProgressIndicator.localScale = new Vector3(progressScale, progressScale, progressScale);
                }
                yield return null;
            }
            if (capturingAgent != null)
            {
                TeamColor newOwner = capturingAgent.TeamColor;
                SetOwner(newOwner);
                capturingAgent.SetAgentState(AgentState.Patrolling);
                capturingAgent = null;
            }
            captureProgress = 0f;
            if (captureProgressIndicator != null)
                captureProgressIndicator.localScale = Vector3.zero;
        }
    }
}