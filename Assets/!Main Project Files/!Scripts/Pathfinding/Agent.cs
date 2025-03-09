using System.Collections;
using System.IO;
using UnityEngine;

public class Agent : MonoBehaviour
{
    private Astar astar;
    
    void Awake()
    {
        if (astar == null)
        {
            astar = FindObjectOfType<Astar>();
            if (astar == null)
            {
                Debug.LogError("Agent.cs: No Astar found in scene.");
            }
        }
    }

    public void FollowPath()
    {
        Vector3 startPosition = transform.position;
        
        astar.FindPath(startPosition, targetPosition);
        
    }

    private IEnumerator WaitForPathAndFollow()
    {
        currentPath = astar.CurrentPath;

        if (currentPath != null && currentPath.Count > 0)
        {
            
        }
    }
}
