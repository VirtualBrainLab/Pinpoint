using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EightShankProbeControl : MonoBehaviour
{
    [SerializeField] private GameObject _secondProbeGO;
    public bool MovedThisFrame { get; private set; }

    // Update is called once per frame
    void Update()
    {
        MovedThisFrame = false;
        if (Input.GetKeyDown(KeyCode.LeftBracket) && Input.GetKey(KeyCode.LeftControl))
        {
            Vector3 pos = _secondProbeGO.transform.localPosition;
            pos.x += 0.1f;
            _secondProbeGO.transform.localPosition = pos;
            MovedThisFrame = true;
        }
        else if (Input.GetKeyDown(KeyCode.RightBracket) && Input.GetKey(KeyCode.LeftControl))
        {
            Vector3 pos = _secondProbeGO.transform.localPosition;
            pos.x -= 0.1f;
            _secondProbeGO.transform.localPosition = pos;
            MovedThisFrame = true;
        }
    }
}
