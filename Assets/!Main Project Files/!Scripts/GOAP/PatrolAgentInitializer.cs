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
                if (goapAgent == null)
                {
                    Debug.LogError($"PatrolAgentInitializer.cs: No GoapAgent script found in object.");
                }
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

            else if (moveAction != null && targetLocation == null)
            {
                GridManager gridManager = FindObjectOfType<GridManager>();
                if (gridManager != null)
                {
                    // LEFT HERE TODO;
                }
            }
            
            Patrol patrolAction = GetComponent<Patrol>();
            
            if (patrolAction != null)
            {
                patrolAction.SetWaypoints(patrolPoints);
            }
            
            goapAgent.SetGoal("AreaPatrolled", true);
        }

        private Node GetRandomWalkableNode(GridManager gridManager)
        {
            for (int i = 0; i < 50; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, gridManager.Nodes.Length);
                Node node = gridManager.Nodes[randomIndex];

                if (node != null && node.Walkable)
                {
                    return node;
                }
            }
            
            Debug.LogWarning("PatrolAgentInitializer.cs: no walkable node found.");
            return null;
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
            
            // TODO: Add actual enemy sighting functionality here.
            Debug.Log("PatrolAgentInitializer.cs: changing goal to move to target location.");
        }
    }
}
