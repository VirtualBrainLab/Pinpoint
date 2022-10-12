using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EightShankProbeControl : MonoBehaviour
{
    [SerializeField] GameObject SecondProbeGO;
    private bool _movedThisFrame;
    public bool MovedThisFrame { get { return _movedThisFrame; } }

    // Update is called once per frame
    void Update()
    {
        _movedThisFrame = false;
        if (Input.GetKeyDown(KeyCode.LeftBracket) && Input.GetKey(KeyCode.LeftControl))
        {
            Vector3 pos = SecondProbeGO.transform.localPosition;
            pos.x += 0.1f;
            SecondProbeGO.transform.localPosition = pos;
            _movedThisFrame = true;
        }
        else if (Input.GetKeyDown(KeyCode.RightBracket) && Input.GetKey(KeyCode.LeftControl))
        {
            Vector3 pos = SecondProbeGO.transform.localPosition;
            pos.x -= 0.1f;
            SecondProbeGO.transform.localPosition = pos;
            _movedThisFrame = true;
        }
    }
}
