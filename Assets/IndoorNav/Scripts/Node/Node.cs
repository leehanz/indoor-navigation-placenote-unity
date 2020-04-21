using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
/*========================================
 * Class for Reading Map
======================================== */
public class Node : MonoBehaviour {
    public Vector3 pos;

    [Header("A*")]
    public List<Node> neighbors = new List<Node>();
    public float FCost { get { return GCost + HCost; } }
    public float HCost { get; set; }
    public float GCost { get; set; }
    public float Cost { get; set; }
    public Node Parent { get; set; }
    //next node in navigation list
    public Node NextInList { get; set; }

    Vector3 scale = Vector3.one;

    GameObject mModel;
    Collider mCollider;

    bool activated = false;
    bool isDestination = false;
    bool isStart = false;
    public bool IsStartNode{ get { return isStart; } }
    public bool IsEndNode { get { return isDestination; } }

    void Awake()
    {
        mModel = transform.GetChild(0).gameObject;
        mCollider = transform.GetComponent<BoxCollider>();

        Debug.Assert(mModel != null, "model is missing.");
        Debug.Assert(mCollider != null, "collider is missing.");

        //initialize
        Activate(false);

        //check destination
        isStart = GetComponent<StartBehaviour>() != null;
        isDestination = GetComponent<DestinationBehaviour>() != null;

#if UNITY_EDITOR
        pos = transform.position;
#endif
    }

    void Update()
    {
        if (activated && !isDestination && !isStart)
            transform.localScale = scale * (1 + Mathf.Sin(Mathf.PI * Time.time) * .2f);
    }

    public void Activate(bool active)
    {
        mModel.SetActive(active);
        mCollider.enabled = active;

        if (NextInList != null)
            transform.LookAt(NextInList.transform);

        activated = active;
    }

    public void FindNeighbors(float maxDistance)
    {
        foreach (Node node in FindObjectsOfType<Node>())
        {
            if (Vector3.Distance(node.pos, this.pos) < maxDistance) 
                neighbors.Add(node);
        }
    }
}
