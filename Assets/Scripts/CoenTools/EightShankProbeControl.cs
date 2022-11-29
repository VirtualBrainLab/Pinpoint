using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class EightShankProbeControl : MonoBehaviour
{
    [FormerlySerializedAs("SecondProbeGO")] [SerializeField] private GameObject _secondProbeGo;
    private bool _movedThisFrame;
    public bool MovedThisFrame { get { return _movedThisFrame; } }

    // Update is called once per frame
    void Update()
    {
        _movedThisFrame = false;
        if (Input.GetKeyDown(KeyCode.LeftBracket) && Input.GetKey(KeyCode.LeftControl))
        {
            Vector3 pos = _secondProbeGo.transform.localPosition;
            pos.x += 0.1f;
            _secondProbeGo.transform.localPosition = pos;
            _movedThisFrame = true;
        }
        else if (Input.GetKeyDown(KeyCode.RightBracket) && Input.GetKey(KeyCode.LeftControl))
        {
            Vector3 pos = _secondProbeGo.transform.localPosition;
            pos.x -= 0.1f;
            _secondProbeGo.transform.localPosition = pos;
            _movedThisFrame = true;
        }
    }
}
