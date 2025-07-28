using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class DestinationLineDrawer : MonoBehaviour
{
    public Transform destination;
    [SerializeField] private GameObject floor;
    public NavMeshAgent navAgent;
    public LineRenderer lineRdr;
    float floorY;
    

    void Awake()
    {
        floorY = floor.transform.position.y+ 0.1f;
    }


    void Update()
    {
        navAgent.enabled = true;
        lineRdr.enabled = true;

        navAgent.SetDestination (destination.position);
        navAgent.isStopped = true;
        var path = navAgent.path;
        if (path.corners.Length < 2)
            return ;

        lineRdr.positionCount = path.corners.Length;
        for( int i = 0; i < path.corners.Length; i++)
        {
            Vector3 newcorner = new Vector3 (path.corners[i].x, floorY, path.corners[i].z);
            lineRdr.SetPosition( i, newcorner);
        }
    }

    void OnDisable()
    {
        if(lineRdr) lineRdr.enabled = false;
        if(navAgent) navAgent.enabled = false;
    }

    public void ChangeDestination(Transform new_one)
    {
        destination = new_one;
    }
    public void ChangeNavAgent(NavMeshAgent new_one)
    {
        navAgent = new_one;
    }
}
