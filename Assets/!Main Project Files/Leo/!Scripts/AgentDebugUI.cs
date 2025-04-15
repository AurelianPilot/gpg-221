using _Main_Project_Files.Leo._Scripts.Agents;
using _Main_Project_Files.Leo._Scripts.GOAP;
using _Main_Project_Files.Leo._Scripts.Pathfinding;
using UnityEngine;
using UnityEngine.UI;

namespace _Main_Project_Files.Leo._Scripts
{
    public class AgentDebugUI : MonoBehaviour
    {
        [SerializeField] private Text stateText;
        [SerializeField] private Text pathText;
        [SerializeField] private Text actionText;
        [SerializeField] private Text errorText;
    
        private TeamAgent teamAgent;
        private Agent pathAgent;
        private GoapAgent goapAgent;
    
        void Start()
        {
            teamAgent = GetComponentInParent<TeamAgent>();
            pathAgent = GetComponentInParent<Agent>();
            goapAgent = GetComponentInParent<GoapAgent>();
        
            Transform mainCamera = Camera.main.transform;
            transform.LookAt(transform.position + mainCamera.rotation * Vector3.forward,
                mainCamera.rotation * Vector3.up);
        }
    
        void Update()
        {
            if (teamAgent != null)
                stateText.text = $"State: {teamAgent.GetAgentState()}";
            
            if (pathAgent != null)
            {
                string pathStatus = pathAgent.IsFollowingPath() ? "Following" : "Idle";
                Vector3 target = pathAgent.GetTargetPosition();
                pathText.text = $"Path: {pathStatus}\nTarget: {target.ToString("F1")}";
            
                if (pathAgent.GetAstar() == null)
                    errorText.text = "ERROR: No Astar ref!";
            }
        
            if (goapAgent != null)
            {
                actionText.text = $"Goal: {goapAgent.ActiveGoalState}";
            }
        }
    }
}