using UnityEngine;

public class SpeedControl : MonoBehaviour
{
    // Input system
    ProbeControlInputActions inputActions;

    private void Awake()
    {
        inputActions = new();
        var probeControlClick = inputActions.ProbeControl;
        probeControlClick.Enable();

        // Modifier keys
        probeControlClick.Slow.performed += x => Settings.ProbeSpeed--;
        probeControlClick.Fast.performed += x => Settings.ProbeSpeed++;
    }

}
