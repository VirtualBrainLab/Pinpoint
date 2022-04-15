using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TP_CameraMiniController : MonoBehaviour
{
    [SerializeField] TP_BrainCameraController brainCameraController;

    // Update is called once per frame
    void Update()
    {
        Vector2 cameraPitchYaw = brainCameraController.GetPitchYaw();
        transform.localRotation = Quaternion.Euler(cameraPitchYaw.y, cameraPitchYaw.x, 0);
    }
}
