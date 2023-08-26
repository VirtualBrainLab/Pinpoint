using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class EightShankProbeControl : MonoBehaviour
{
    [FormerlySerializedAs("SecondProbeGO")] [SerializeField] private GameObject _secondProbeGo;

    public UnityEvent MovedThisFrameEvent;

    // Update is called once per frame
    void Update()
    {
        bool movedThisFrame = false;

        if (Input.GetKeyDown(KeyCode.LeftBracket) && Input.GetKey(KeyCode.LeftControl))
        {
            Vector3 pos = _secondProbeGo.transform.localPosition;
            pos.x += 0.1f;
            _secondProbeGo.transform.localPosition = pos;
            movedThisFrame = true;
        }
        else if (Input.GetKeyDown(KeyCode.RightBracket) && Input.GetKey(KeyCode.LeftControl))
        {
            Vector3 pos = _secondProbeGo.transform.localPosition;
            pos.x -= 0.1f;
            _secondProbeGo.transform.localPosition = pos;
            movedThisFrame = true;
        }

        if (movedThisFrame)
            MovedThisFrameEvent.Invoke();
    }
}
