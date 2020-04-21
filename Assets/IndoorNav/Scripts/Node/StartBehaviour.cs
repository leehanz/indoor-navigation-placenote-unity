using UnityEngine;
using System.Collections;

public class StartBehaviour : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return null;
#if UNITY_EDITOR
        GetComponent<Node>().Activate(true);
#endif
    }
    // Update is called once per frame
    void Update()
    {

    }
}
