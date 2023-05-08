using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoenProbeBehavior : MonoBehaviour
{
    [SerializeField] ProbeManager _probeManager;

    // Update is called once per frame
    void Update()
    {
        float rate;

        if (Input.GetKey(KeyCode.LeftControl))
            rate = 0.001f;
        else if (Input.GetKey(KeyCode.LeftShift))
            rate = 0.1f;
        else
            rate = 0.01f;

        Vector3 pos = transform.localPosition;
        bool moved = false;

        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.Equals))
        {
            pos.x -= rate;
            moved = true;
        }
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.Underscore))
        {
            pos.x += rate;
            moved = true;
        }

        if (moved)
        {
            transform.localPosition = pos;
            _probeManager.UIUpdateEvent.Invoke();
        }
    }
}
