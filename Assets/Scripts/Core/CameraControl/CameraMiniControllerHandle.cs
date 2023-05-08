using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMiniControllerHandle : MonoBehaviour
{
    [SerializeField] BrainCameraController cameraController;
    [SerializeField] Vector3 eulerAngles;
    private float lastClick = 0f;

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if ((Time.realtimeSinceStartup - lastClick) < BrainCameraController.doubleClickTime)
                cameraController.SetBrainAxisAngles(eulerAngles);
            else
                lastClick = Time.realtimeSinceStartup;
        }
    }
}
