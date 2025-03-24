using System.Collections.Generic;
using UnityEngine;

namespace _Main_Project_Files._Scripts.GOAP
{
    public class WorldState : MonoBehaviour
    {
        private readonly Dictionary<string, bool> states = new Dictionary<string, bool>();

        public void SetState(string key, bool value)
        {
            if (states.ContainsKey(key))
            {
                states[key] = value;
            }
            else
            {
                states.Add(key, value);
            }
        }
        
        public bool GetState(string key)
        {
            if (states.ContainsKey(key))
            {
                return states[key];
            }

            return false;
        }
    }
}