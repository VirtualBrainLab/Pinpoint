using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TP_CameraMiniControllerHandle : MonoBehaviour
{
    [SerializeField] TP_BrainCameraController cameraController;
    [SerializeField] Vector3 eulerAngles;
    private float doubleClickTime = 0.2f;
    private float lastClick = 0f;

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if ((Time.realtimeSinceStartup - lastClick) < doubleClickTime)
                cameraController.SetBrainAxisAngles(eulerAngles);
            else
                lastClick = Time.realtimeSinceStartup;
        }
    }
}
