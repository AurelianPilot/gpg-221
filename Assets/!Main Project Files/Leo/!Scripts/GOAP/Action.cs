using System.Collections;
using System.Collections.Generic;
using _Main_Project_Files.Leo._Scripts.Agents;
using _Main_Project_Files.Leo._Scripts.Pathfinding;
using UnityEngine;

namespace _Main_Project_Files.Leo._Scripts.GOAP
{
    /// <summary>
    /// An action that is performed by AI (as in, an agent).
    /// </summary>
    public abstract class Action : MonoBehaviour
    {
        [SerializeField] protected string actionName = "Unnamed Action";
        [SerializeField] protected float actionCost = 1f;
        [SerializeField] protected bool isActionAchivable;

        /// <summary>
        /// Effect caused by this action being performed.
        /// </summary>
        [SerializeField] protected List<Effect> effects = new List<Effect>();

        /// <summary>
        /// Pre-requisites for this action to be performed.
        /// </summary>
        [SerializeField] protected List<PreRequisite> preRequisites = new List<PreRequisite>();

        [SerializeField] protected bool debug = false;

        protected GoapAgent owner;
        protected bool isRunning = false;
        protected TeamAgent teamAgent;
        protected GameManager gameManager;

        public string ActionName => actionName;
        public float ActionCost => actionCost;
        public List<PreRequisite> PreRequisites => preRequisites;
        public List<Effect> Effects => effects;

        protected virtual void Awake()
        {
            teamAgent = GetComponent<TeamAgent>();
            gameManager = FindObjectOfType<GameManager>();
        }

        public void SetOwner(GoapAgent agent)
        {
            owner = agent;
        }

        /*/// <summary>
        ///  Checks the world state lists if the preRequisites are met to perform an action.
        /// </summary>
        /// <param name="worldState">Reference to the world state.</param>
        /// <returns></returns>
        public bool ArePreRequisitesSatisfied(WorldState worldState)
        {
            if (!isActionAchivable) return false;

            foreach (var preRequisite in preRequisites)
            {
                if (!preRequisite.IsSatisfied(worldState)) return false;
            }
            return true;
        }*/

        /// <summary>
        /// Apply all effects to the world state.
        /// </summary>
        /// <param name="worldState">Reference to the world state.</param>
        public void ApplyEffects(WorldState worldState)
        {
            foreach (var effect in effects)
            {
                effect.ApplyEffect(worldState);
            }
        }

        public IEnumerator Execute()
        {
            isRunning = true;
            yield return PerformAction();
        }

        protected abstract IEnumerator PerformAction();

        protected void AddPreRequisite(string stateName, bool value)
        {
            preRequisites.Add(new PreRequisite(value, stateName));
        }

        protected void AddEffect(string stateName, bool value)
        {
            effects.Add(new Effect(stateName, value));
        }

        protected Agent GetPathAgent()
        {
            return GetComponent<Agent>() ?? owner?.GetPathfindingAgent();
        }

        /*protected void Log(string message)
        {
            if (debug) Debug.Log(message);
        }*/
    }
}
 