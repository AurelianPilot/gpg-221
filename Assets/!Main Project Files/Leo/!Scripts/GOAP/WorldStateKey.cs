using UnityEngine;

namespace _Main_Project_Files.Leo._Scripts.GOAP
{
    /// <summary>
    /// Rather than using strings (like in my previous project) we'll use enums which just defines
    /// keys (names) for states in the AgentWorldState (aka the world state). This is just for safety.
    /// </summary>
    public enum WorldStateKey
    {
        // Enemy related.
        HasEnemyTarget,
        IsEnemyInRange,
        IsEnemyInSight,
        EnemyDetected,

        // Ally related.
        AllyDetected,
        AllyNeedsHealing,
        HasAllyTarget,
        IsInHealRange,
        
        // Combat related.
        HasWeaponEquipped,
        IsInCombat,
        IsInAttackRange,
        
        // Territory related.
        IsInOwnTerritory,
        IsCapturingTerritory,
        IsTerritorySecure,
        
        // Movement Related.
        IsAtTargetLocation,
        IsWandering,
        IsRunningAway,
        
        // ! Role of the agent !
        IsHealerRole,
        IsWarriorRole
    }
}