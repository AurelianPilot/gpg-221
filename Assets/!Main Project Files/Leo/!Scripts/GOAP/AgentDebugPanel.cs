using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace _Main_Project_Files.Leo._Scripts.GOAP
{
    /// <summary>
    /// Displays debug information about a GOAP agent.
    /// </summary>
    public class AgentDebugPanel : MonoBehaviour
    {
        public enum DebugPanelMode
        {
            ScreenSpace,
            WorldSpace
        }
        
        [Header("- Mode Settings")]
        [SerializeField] private DebugPanelMode mode = DebugPanelMode.WorldSpace;
        
        [Header("- References")]
        [SerializeField] private GameObject debugPanelRoot;
        [SerializeField] private TextMeshProUGUI agentNameText;
        [SerializeField] private TextMeshProUGUI worldStateText;
        [SerializeField] private TextMeshProUGUI currentGoalText;
        [SerializeField] private TextMeshProUGUI currentActionText;
        [SerializeField] private TextMeshProUGUI pathfindingText;
        
        [Header("- Agent Reference")]
        [SerializeField] private GOAP.GladiatorAgent targetAgent;
        
        [Header("- World Space Settings")]
        [SerializeField] private Vector3 offsetFromAgent = new Vector3(0, 2.5f, 0);
        [SerializeField] private bool lookAtCamera = true;
        
        [Header("- Update Settings")]
        [SerializeField] private float updateInterval = 0.2f;
        [SerializeField] private bool showDebugPanel = true;
        [SerializeField] private KeyCode togglePanelKey = KeyCode.F1;
        [SerializeField] private bool compactMode = false;
        
        // References to components.
        private AgentWorldState agentWorldState;
        private Pathfinding.PathFindingAgent pathfindingAgent;
        private Camera mainCamera;
        private float nextUpdateTime;
        private Canvas panelCanvas;
        
        private void Start()
        {
            FindAgentIfNeeded();
            
            if (targetAgent != null)
            {
                agentWorldState = targetAgent.GetComponent<GOAP.AgentWorldState>();
                pathfindingAgent = targetAgent.GetComponent<_Main_Project_Files.Leo._Scripts.Pathfinding.PathFindingAgent>();
            }
            
            mainCamera = Camera.main;
            panelCanvas = GetComponentInParent<Canvas>();
            
            ValidateCanvasSetup();
            
            debugPanelRoot.SetActive(showDebugPanel);
            
            if (agentNameText != null && targetAgent != null)
            {
                agentNameText.text = targetAgent.gameObject.name;
            }
            
            UpdateDebugInfo();
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(togglePanelKey))
            {
                showDebugPanel = !showDebugPanel;
                debugPanelRoot.SetActive(showDebugPanel);
            }
            
            if (mode == DebugPanelMode.WorldSpace)
            {
                UpdatePanelPosition();
            }
            
            if (showDebugPanel && Time.time >= nextUpdateTime)
            {
                UpdateDebugInfo();
                nextUpdateTime = Time.time + updateInterval;
            }
        }
        
        /// <summary>
        /// Finds an agent if none is assigned.
        /// </summary>
        private void FindAgentIfNeeded()
        {
            if (targetAgent != null) return;
            
            if (mode == DebugPanelMode.WorldSpace)
            {
                targetAgent = GetComponentInParent<GladiatorAgent>();
            }
            else
            {
                targetAgent = FindObjectOfType<GladiatorAgent>();
            }
            
            if (targetAgent == null)
            {
                Debug.LogError("AgentDebugPanel.cs: No GladiatorAgent found. Debug panel will not function.");
                enabled = false;
            }
        }
        
        /// <summary>
        /// Validates that the canvas is properly set up for the chosen mode.
        /// </summary>
        private void ValidateCanvasSetup()
        {
            if (panelCanvas == null)
            {
                Debug.LogWarning("AgentDebugPanel.cs: No Canvas found in parent hierarchy.");
                return;
            }
            
            if (mode == DebugPanelMode.WorldSpace && panelCanvas.renderMode != RenderMode.WorldSpace)
            {
                Debug.LogWarning("AgentDebugPanel.cs: Canvas is not in World Space mode but the panel is configured for World Space.");
            }
            else if (mode == DebugPanelMode.ScreenSpace && panelCanvas.renderMode == RenderMode.WorldSpace)
            {
                Debug.LogWarning("AgentDebugPanel.cs: Canvas is in World Space mode but the panel is configured for Screen Space.");
            }
        }
        
        /// <summary>
        /// Updates the panel position to follow the agent in world space mode.
        /// </summary>
        private void UpdatePanelPosition()
        {
            if (targetAgent == null || mode != DebugPanelMode.WorldSpace) return;
            
            transform.position = targetAgent.transform.position + offsetFromAgent;
            
            if (lookAtCamera && mainCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
            }
        }
        
        /// <summary>
        /// Updates all the debug information displayed in the panel.
        /// </summary>
        private void UpdateDebugInfo()
        {
            if (!showDebugPanel || targetAgent == null) return;
            
            UpdateWorldStateInfo();
            UpdateGoalInfo();
            UpdateActionInfo();
            UpdatePathfindingInfo();
        }
        
        /// <summary>
        /// Updates the world state information panel.
        /// </summary>
        private void UpdateWorldStateInfo()
        {
            if (worldStateText == null || agentWorldState == null) return;
            
            StringBuilder sb = new StringBuilder("World State:\n");
            Dictionary<GOAP.WorldStateKey, bool> allStates = agentWorldState.GetAllStates();
            
            int maxStatesToShow = compactMode ? 3 : int.MaxValue;
            int count = 0;
            
            foreach (var state in allStates)
            {
                sb.AppendLine($"• {state.Key}: {state.Value}");
                count++;
                if (count >= maxStatesToShow) break;
            }
            
            if (count < allStates.Count)
            {
                sb.AppendLine($"• ...and {allStates.Count - count} more");
            }
            
            worldStateText.text = sb.ToString();
        }
        
        /// <summary>
        /// Updates the current goal information panel.
        /// </summary>
        private void UpdateGoalInfo()
        {
            if (currentGoalText == null) return;
            
            GoapGoal currentGoal = targetAgent.GetCurrentGoal();
            
            if (currentGoal != null)
            {
                StringBuilder sb = new StringBuilder("Current Goal:\n");
                sb.AppendLine($"• {currentGoal.GoalName} (Priority: {currentGoal.Priority})");
                
                if (!compactMode)
                {
                    Dictionary<WorldStateKey, bool> goalState = currentGoal.GetGoalState();
                    if (goalState.Count > 0)
                    {
                        sb.AppendLine("• Conditions:");
                        foreach (var condition in goalState)
                        {
                            sb.AppendLine($"  - {condition.Key}: {condition.Value}");
                        }
                    }
                }
                
                currentGoalText.text = sb.ToString();
            }
            else
            {
                currentGoalText.text = "Current Goal:\n• None";
            }
        }
        
        /// <summary>
        /// Updates the current action information panel.
        /// </summary>
        private void UpdateActionInfo()
        {
            if (currentActionText == null) return;
            
            GoapAction currentAction = targetAgent.GetCurrentAction();
            
            if (currentAction != null)
            {
                StringBuilder sb = new StringBuilder("Current Action:\n");
                sb.AppendLine($"• {currentAction.GetType().Name}");
                
                if (!compactMode)
                {
                    sb.AppendLine($"• Cost: {currentAction.GetCost()}");
                    
                    // ! Display prerequisites.
                    Dictionary<WorldStateKey, bool> prerequisites = currentAction.GetPrerequisites();
                    if (prerequisites.Count > 0)
                    {
                        sb.AppendLine("• Prerequisites:");
                        foreach (var prereq in prerequisites)
                        {
                            sb.AppendLine($"  - {prereq.Key}: {prereq.Value}");
                        }
                    }
                    
                    // ! Display effects.
                    Dictionary<GOAP.WorldStateKey, bool> effects = currentAction.GetEffects();
                    if (effects.Count > 0)
                    {
                        sb.AppendLine("• Effects:");
                        foreach (var effect in effects)
                        {
                            sb.AppendLine($"  - {effect.Key}: {effect.Value}");
                        }
                    }
                }
                
                currentActionText.text = sb.ToString();
            }
            else
            {
                currentActionText.text = "Current Action:\n• None";
            }
        }
        
        /// <summary>
        /// Updates the pathfinding information panel.
        /// </summary>
        private void UpdatePathfindingInfo()
        {
            if (pathfindingText == null || pathfindingAgent == null) return;
            
            StringBuilder sb = new StringBuilder("Pathfinding:\n");
            
            sb.AppendLine($"• Following Path: {pathfindingAgent.IsFollowingPath()}");
            
            Vector3 targetPosition = pathfindingAgent.GetTargetPosition();
            if (targetPosition != Vector3.zero)
            {
                if (compactMode)
                {
                    sb.AppendLine("• Has Target: Yes");
                }
                else
                {
                    sb.AppendLine($"• Target: ({targetPosition.x:F1}, {targetPosition.y:F1}, {targetPosition.z:F1})");
                    
                    // Distance to target
                    float distance = Vector3.Distance(targetAgent.transform.position, targetPosition);
                    sb.AppendLine($"• Distance: {distance:F2}");
                }
            }
            else
            {
                sb.AppendLine("• Target: None");
            }
            
            pathfindingText.text = sb.ToString();
        }
        
        /// <summary>
        /// Sets a new target agent to monitor.
        /// </summary>
        /// <param name="newTarget">The new agent to monitor.</param>
        public void SetTargetAgent(GladiatorAgent newTarget)
        {
            if (newTarget == null) return;
            
            targetAgent = newTarget;
            agentWorldState = targetAgent.GetComponent<AgentWorldState>();
            pathfindingAgent = targetAgent.GetComponent<Pathfinding.PathFindingAgent>();
            
            if (agentNameText != null)
            {
                agentNameText.text = targetAgent.gameObject.name;
            }
            
            UpdateDebugInfo();
        }
        
        /// <summary>
        /// Toggles between compact and detailed display modes.
        /// </summary>
        public void ToggleCompactMode()
        {
            compactMode = !compactMode;
            UpdateDebugInfo();
        }
    }
}