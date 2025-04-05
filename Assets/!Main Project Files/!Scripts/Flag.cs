using System.Collections;
using _Main_Project_Files._Scripts.Agents;
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

        // Internal state
        private TeamAgent capturingAgent = null;
        private float captureProgress = 0f;
        private Coroutine captureCoroutine = null;
        private GameManager gameManager;

        public TeamColor OwnerTeam => ownerTeam;

        private void Awake()
        {
            if (flagRenderer == null)
                flagRenderer = GetComponentInChildren<MeshRenderer>();

            // Set progress indicator.
            if (captureProgressIndicator != null)
                captureProgressIndicator.localScale = Vector3.zero;

            gameManager = FindObjectOfType<GameManager>();
        }

        private void Start()
        {
            // Set flag color.
            UpdateFlagColor();

            // Register with game manager.
            if (gameManager != null)
                gameManager.RegisterFlag(this);
        }

        private void UpdateFlagColor()
        {
            if (flagRenderer == null) return;

            // Setting GO material color based on the team of course.
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
            // Skip if already owned by this team.
            if (ownerTeam == newOwner) return;

            ownerTeam = newOwner;
            UpdateFlagColor();

            Debug.Log($"[FLAG] Flag at {transform.position} is now owned by {ownerTeam} team!");

            // Notify game manager of capture.
            if (gameManager != null)
                gameManager.OnFlagCaptured(this, newOwner);
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
            // Cancel any ongoing capture
            CancelCapture();

            // Begin new capture
            capturingAgent = agent;
            agent.SetAgentState(AgentState.CapturingFlag);

            Debug.Log($"[FLAG] {agent.gameObject.name} ({agent.TeamColor}) is attempting to capture {ownerTeam} flag!");

            // Start capture coroutine
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

            // Reset the indicator.
            if (captureProgressIndicator != null)
                captureProgressIndicator.localScale = Vector3.zero;

            // Reset agent state.
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
                // Capture process go up with time.
                captureProgress += Time.deltaTime;

                // Update indicator.
                if (captureProgressIndicator != null)
                {
                    float progressScale = (captureProgress / captureTime) * maxProgressScale;
                    captureProgressIndicator.localScale = new Vector3(progressScale, progressScale, progressScale);
                }

                yield return null;
            }

            // Capture complete!! yay
            if (capturingAgent != null)
            {
                TeamColor newOwner = capturingAgent.TeamColor;
                SetOwner(newOwner);

                // Update agent state.
                capturingAgent.SetAgentState(AgentState.Patrolling);
                capturingAgent = null;
            }

            // Reset capture progress.
            captureProgress = 0f;

            // Reset indicator again.
            if (captureProgressIndicator != null)
                captureProgressIndicator.localScale = Vector3.zero;
        }
    }
}