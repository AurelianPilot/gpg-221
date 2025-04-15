using System;
using _Main_Project_Files._Scripts.Pathfinding;
using UnityEngine;

namespace _Main_Project_Files._Scripts.GOAP
{
    public class Effect
    {
        [SerializeField] private bool isEffectActive = true;
        [SerializeField] private string effectName = "";
        
        public bool IsEffectActive => isEffectActive;
        public string EffectName => effectName;

        public Effect(string effectName, bool isEffectActive)
        {
            this.isEffectActive = isEffectActive;
            this.effectName = effectName;
        }
        
        public void ApplyEffect(WorldState worldState)
        {
            worldState.SetState(effectName, isEffectActive);
        }
    }
}
