using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.Events;
/*========================================
 * Class for Navigating nodes
======================================== */
public class NavController : MonoBehaviour {
    public enum State { Idle, Searching, Completed }
    public State navigationState = State.Idle;
    [SerializeField] MinimapController minimap;

    private AStarSearch AStar;
    private List<Node> path = new List<Node>();
    private int currNodeIndex = 0;
    private float maxDistance = 1.1f;

    [SerializeField] bool shouldStart = false;
    [SerializeField] bool drawPathFinished = false;
    public UnityEvent OnStartedEvent = new UnityEvent();

    void Start()
    {
        AStar = GetComponent<AStarSearch>();
        Debug.Assert(AStar != null, "AStar is missing.");

#if UNITY_EDITOR
        InitNavigationPath();
#endif
    }

    public void InitNavigationPath()
    {
        StopAllCoroutines();
        StartCoroutine(WaitForShapes(.5f));
    }

    public void DrawNavigation()
    {
        minimap.DrawPath(path.Select(x => x.transform).ToArray());
        drawPathFinished = true;
        path[1].Activate(true);
    }

    IEnumerator WaitForShapes(float duration)
    {
        //search new path every .5f sec if destination didnt show up 
        while (FindObjectOfType<DestinationBehaviour>() == null)
        {
            yield return new WaitForSeconds(duration);
            Debug.Log("waiting...");
        }
        InitPath(true);
    }

    void InitPath(bool fromStart)
    {
        if (navigationState == State.Searching) return;

        navigationState = State.Searching;

        Node[] allNodes = FindObjectsOfType<Node>();
        Debug.LogFormat("found {0} nodes", allNodes.Length);

        Node firstNode = (fromStart)? GetFirstNode(allNodes) : GetClosestNode(allNodes, transform.position);
        Debug.Log("firstNode: " + firstNode.gameObject.name);

        Node targetNode = GetLastNode(allNodes);
        Debug.Log("target: " + targetNode.gameObject.name);

        //set neighbor nodes for all nodes
        foreach (Node node in allNodes)
        {
            node.FindNeighbors(maxDistance);
        }

        //get path from A* algorithm
        path = AStar.FindPath(firstNode, targetNode);

        if (path == null)
        {
            //increase search distance for neighbors
            Debug.Log("Increasing search distance: " + maxDistance);
            maxDistance += .1f;
            navigationState = State.Idle;
            InitPath(fromStart);
            return;
        }

        //set next nodes 
        for (int i = 0; i < path.Count - 1; i++)
        {
            path[i].NextInList = path[i + 1];
        }

        path[0].Activate(true);
        navigationState = State.Completed;
    }

    Node GetClosestNode(Node[] nodes, Vector3 point)
    {
        float minDist = Mathf.Infinity;
        Node closestNode = null;
        foreach (Node node in nodes)
        {
            float dist = Vector3.Distance(node.pos, point);
            if (dist < minDist)
            {
                closestNode = node;
                minDist = dist;
            }
        }
        return closestNode;
    }

    Node GetFirstNode(Node[] nodes)
    {
        return nodes.Where(n => n.IsStartNode).FirstOrDefault();
    }

    Node GetLastNode(Node[] nodes)
    {
        return nodes.Where(n => n.IsEndNode).FirstOrDefault();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("waypoint")) return;

        if (!shouldStart)
        {
            //go to first node please
            OnStartedEvent?.Invoke();
            shouldStart = true;
        }
        else if (drawPathFinished && navigationState == State.Completed)
        {
            currNodeIndex = path.IndexOf(other.GetComponent<Node>());
            if (currNodeIndex < path.Count - 1) 
                path[currNodeIndex + 1].Activate(true);
        }
    }
}
