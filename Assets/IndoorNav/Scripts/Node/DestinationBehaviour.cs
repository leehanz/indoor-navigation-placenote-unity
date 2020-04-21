using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestinationBehaviour : MonoBehaviour
{
    Vector3 rotate = new Vector3(0, 1, 0);

    void Start()
    {

    }

    void Update()
    {
        transform.eulerAngles += rotate;
    }
}
