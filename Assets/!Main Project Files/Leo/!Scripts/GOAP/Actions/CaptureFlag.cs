using System.Collections;
using _Main_Project_Files._Scripts.Agents;
using _Main_Project_Files._Scripts.Pathfinding;
using UnityEngine;

namespace _Main_Project_Files._Scripts.GOAP.Actions
{
    public class CaptureFlag : Action
    {
        [Header("- Capture Settings")]
        [SerializeField] private float captureRange = 1.5f;
        [SerializeField] private string flagCapturedStateName = "FlagCaptured";

        private Flag targetFlag;

        protected override void Awake()
        {
            actionName = "CaptureFlag";
            isActionAchivable = true;
            if (preRequisites.Count == 0)
            {
                AddPreRequisite("IsDead", false);
                AddPreRequisite("TeamDefeated", true);
            }
            if (effects.Count == 0)
            {
                AddEffect(flagCapturedStateName, true);
            }
            teamAgent = GetComponent<TeamAgent>();
            gameManager = FindObjectOfType<GameManager>();
        }

        protected override IEnumerator PerformAction()
        {
            Debug.Log($"[ACTION] CaptureFlag.cs: {gameObject.name} is trying to capture a flag.");
            targetFlag = FindClosestEnemyFlag();
            if (targetFlag == null)
            {
                Debug.LogWarning("[ACTION] CaptureFlag.cs: No enemy flag found to capture.");
                yield break;
            }
            Vector3 flagPosition = targetFlag.transform.position;
            Agent pathfindingAgent = teamAgent?.GetPathfindingAgent();
            if (pathfindingAgent == null)
            {
                Debug.LogError("[ACTION] CaptureFlag.cs: No pathfinding agent found.");
                yield break;
            }
            teamAgent.SetAgentState(AgentState.CapturingFlag);
            Debug.Log($"[ACTION] CaptureFlag.cs: Moving to flag at {flagPosition}.");
            pathfindingAgent.FollowPath(flagPosition);
            int maxIterations = 300;
            int currentIteration = 0;
            while (currentIteration < maxIterations)
            {
                float distanceToFlag = Vector3.Distance(transform.position, flagPosition);
                if (distanceToFlag <= captureRange)
                {
                    Debug.Log($"[ACTION] CaptureFlag.cs: Reached flag position, distance: {distanceToFlag}");
                    break;
                }
                if (targetFlag.OwnerTeam == teamAgent.TeamColor)
                {
                    Debug.Log("[ACTION] CaptureFlag.cs: Flag is already owned by our team, aborting capture.");
                    yield break;
                }
                currentIteration++;
                yield return null;
            }
            Debug.Log("[ACTION] CaptureFlag.cs: Waiting at flag position for capture...");
            yield return new WaitForSeconds(3.5f);
            Debug.Log("[ACTION] CaptureFlag.cs: Flag capture action completed.");
            teamAgent.SetAgentState(AgentState.Patrolling);
        }

        private Flag FindClosestEnemyFlag()
        {
            if (teamAgent == null || gameManager == null) return null;
            Flag closestFlag = null;
            float closestDistance = float.MaxValue;
            foreach (TeamColor color in System.Enum.GetValues(typeof(TeamColor)))
            {
                if (color == teamAgent.TeamColor) continue;
                Flag flag = gameManager.GetTeamFlag(color);
                if (flag != null)
                {
                    float distance = Vector3.Distance(transform.position, flag.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestFlag = flag;
                    }
                }
            }
            return closestFlag;
        }
    }
}
