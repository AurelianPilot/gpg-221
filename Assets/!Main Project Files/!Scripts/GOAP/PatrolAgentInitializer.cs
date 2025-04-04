using System;
using _Main_Project_Files._Scripts.GOAP.Actions;
using _Main_Project_Files._Scripts.Pathfinding;
using NUnit.Framework.Constraints;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace _Main_Project_Files._Scripts.GOAP
{
    public class PatrolAgentInitializer : MonoBehaviour
    {
        [SerializeField] private GoapAgent goapAgent;
        [SerializeField] private Transform targetLocation;
        [SerializeField] private Transform[] patrolPoints;

        private void Start()
        {
            if (goapAgent == null)
            {
                goapAgent = GetComponent<GoapAgent>();
            }
            
            WorldState worldState = goapAgent.WorldState;
            
            worldState.SetState("AtLocation", false);
            worldState.SetState("IsPatrolling", false);
            worldState.SetState("AreaPatrolled", false);
            worldState.SetState("IsEnemyVisible", false);

            MoveToLocation moveAction = GetComponent<MoveToLocation>();
            if (moveAction != null && targetLocation != null)
            {
                moveAction.SetTargetLocation(targetLocation);
            }

            Patrol patrolAction = GetComponent<Patrol>();
            
            if (patrolAction != null)
            {
                patrolAction.SetWaypoints(patrolPoints);
            }
            
            goapAgent.SetGoal("AreaPatrolled", true);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                OnEnemySighted();
            }
        }

        public void OnEnemySighted()
        {
            WorldState worldState = goapAgent.WorldState;
            worldState.SetState("IsEnemyVisible", true);
            
            goapAgent.SetGoal("AtLocation",true);
        }
    }
}
