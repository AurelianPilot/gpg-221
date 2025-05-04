using _Main_Project_Files.Leo._Scripts.GOAP.Status_Systems;
using _Main_Project_Files.Leo._Scripts.GOAP;
using System.Collections;
using System.Collections.Generic;
using _Main_Project_Files.Leo._Scripts.GOAP.Actions;
using UnityEngine;
using _Main_Project_Files.Leo._Scripts.Pathfinding;


[RequireComponent(typeof(AgentHealthSystem))]
[RequireComponent(typeof(GladiatorAgent))]
[RequireComponent(typeof(AgentWorldState))]
public class BuildAction : GoapAction
{

    [SerializeField] private GameObject buildingPrefab;
    [SerializeField] private float buildingCooldown;

    /// <summary>
    /// Gets component references.
    /// </summary>
    protected override void Awake()
    {
        
    }

    /// <summary>
    /// Sets the static prerequisites for this action.
    /// </summary>
    protected override void SetUpPreRequisites()
    {
        AddPrerequisite(WorldStateKey.EnemyDetected, true);
        AddPrerequisite(WorldStateKey.IsInAttackRange, true);
        AddPrerequisite(WorldStateKey.HasBuilt, false);
    }

    /// <summary>
    /// Sets the effects this action has on the world state upon successful completion.
    /// </summary>
    protected override void SetUpEffects()
    {
        AddEffect(WorldStateKey.IsInAttackRange, false);
        AddEffect(WorldStateKey.HasBuilt, true);
        AddEffect(WorldStateKey.IsWandering, true);
    }

    /// <summary>
    /// Checks dynamic conditions immediately before execution. Validates the assigned target enemy.
    /// </summary>
    public override bool CheckProceduralPreconditions()
    {
        return true;
    }

    /// <summary>
    /// Executes the movement logic towards the target enemy.
    /// </summary>
    public override IEnumerator PerformAction()
    {

        Instantiate(buildingPrefab, new Vector3(transform.position.x + 2f, transform.position.y, transform.position.z), Quaternion.identity);
            yield break;
        

            AgentWorldState.SetState(WorldStateKey.HasBuilt, true);
        
        
    }

    


}
