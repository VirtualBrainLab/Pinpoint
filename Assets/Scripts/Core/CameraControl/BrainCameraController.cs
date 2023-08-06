using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BrainCameraController : MonoBehaviour
{
    [SerializeField] private Camera brainCamera;
    [SerializeField] private GameObject brainCameraRotator;
    [SerializeField] private GameObject brain;

    private Vector3 initialCameraRotatorPosition;
    private Vector3 cameraPositionOffset;

    public float minFoV = 15.0f;
    public float maxFoV = 90.0f;
    public float fovDelta = 15.0f;
    public float orthoDelta = 5.0f;
    public float moveSpeed = 10.0f;
    public float rotSpeed = 200.0f;
    [SerializeField] private float shiftMult = 2f;
    [SerializeField] private float ctrlMult = 0.5f;
    public float minXRotation = -90;
    public float maxXRotation = 90;
    public float minZRotation = -90;
    public float maxZRotation = 90;

    private bool mouseDownOverBrain;
    private int mouseButtonDown;
    private bool brainTransformChanged;
    private float lastLeftClick;
    private float lastRightClick;

    private float totalYaw;
    private float totalPitch;
    private float totalSpin;

    public static bool BlockBrainControl;

    // auto-rotation
    private bool autoRotate;
    private float autoRotateSpeed = 10.0f;

    public static float doubleClickTime = 0.4f;
    // Targeting
    private Vector3 cameraTarget;

    private void Awake()
    {
        // Artifically limit the framerate
#if !UNITY_WEBGL
        Application.targetFrameRate = 144;
#endif

        cameraTarget = brain.transform.position;
        initialCameraRotatorPosition = brainCameraRotator.transform.position;
        cameraPositionOffset = Vector3.zero;
        autoRotate = false;
    }
    // Start is called before the first frame update
    void Start()
    {
        lastLeftClick = Time.realtimeSinceStartup;
        lastRightClick = Time.realtimeSinceStartup;
    }

    // Update is called once per frame
    void Update()
    {
        // Check the scroll wheel and deal with the field of view
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            float fov = GetZoom();

            float scroll = -Input.GetAxis("Mouse ScrollWheel");
            fov += (brainCamera.orthographic ? orthoDelta : fovDelta) * scroll * SpeedMultiplier();
            fov = Mathf.Clamp(fov, minFoV, maxFoV);

            SetZoom(fov);
        }

        // Now check if the mouse wheel is being held down
        if (Input.GetMouseButton(1) && !BlockBrainControl && !EventSystem.current.IsPointerOverGameObject())
        {
            mouseDownOverBrain = true;
            mouseButtonDown = 1;
        }

        // Now deal with dragging
        if (Input.GetMouseButtonDown(0) && !BlockBrainControl && !EventSystem.current.IsPointerOverGameObject())
        {
            //BrainCameraDetectTargets();
            mouseDownOverBrain = true;
            mouseButtonDown = 0;
            autoRotate = false;
        }

        if (autoRotate)
        {
            totalSpin += autoRotateSpeed * Time.deltaTime;
            ApplyBrainCameraPositionAndRotation();
        }
        else
            BrainCameraControl_noTarget();
    }

    public void SetControlBlock(bool state)
    {
        BlockBrainControl = state;
    }


    void BrainCameraControl_noTarget()
    {
        if (Input.GetMouseButtonUp(0))
            SetControlBlock(false);

        if (mouseDownOverBrain)
        {
            // Deal with releasing the mouse (anywhere)
            if (mouseButtonDown == 0 && Input.GetMouseButtonUp(0))
            {
                lastLeftClick = Time.realtimeSinceStartup;
                ClearMouseDown(); return;
            }
            if (mouseButtonDown == 1 && Input.GetMouseButtonUp(1))
            {
                if (!brainTransformChanged)
                {
                    // Check for double click
                    if ((Time.realtimeSinceStartup - lastRightClick) < doubleClickTime)
                    {
                        // Reset the brainCamera transform position
                        brainCamera.transform.localPosition = Vector3.zero;
                    }
                }

                lastRightClick = Time.realtimeSinceStartup;
                ClearMouseDown(); return;
            }

            if (mouseButtonDown == 1)
            {
                // While right-click is held down 
                float xMove = -Input.GetAxis("Mouse X") * moveSpeed * SpeedMultiplier() * Time.deltaTime;
                float yMove = -Input.GetAxis("Mouse Y") * moveSpeed * SpeedMultiplier() * Time.deltaTime;

                if (xMove != 0 || yMove != 0)
                {
                    brainTransformChanged = true;
                    brainCamera.transform.Translate(xMove, yMove, 0, Space.Self);
                }
            }

            // If the mouse is down, even if we are far way now we should drag the brain
            if (mouseButtonDown == 0)
            {
                float xRot = -Input.GetAxis("Mouse X") * rotSpeed * SpeedMultiplier() * Time.deltaTime;
                float yRot = Input.GetAxis("Mouse Y") * rotSpeed * SpeedMultiplier() * Time.deltaTime;

                if (xRot != 0 || yRot != 0)
                {
                    brainTransformChanged = true;

                    // Pitch Locally, Yaw Globally. See: https://gamedev.stackexchange.com/questions/136174/im-rotating-an-object-on-two-axes-so-why-does-it-keep-twisting-around-the-thir

                    // if space is down, we can apply spin instead of yaw
                    if (Input.GetKey(KeyCode.Space))
                    {
                        totalSpin = Mathf.Clamp(totalSpin + xRot, minXRotation, maxXRotation);
                    }
                    else
                    {
                        // [TODO] Pitch and Yaw are flipped?
                        totalYaw = Mathf.Clamp(totalYaw + yRot, minXRotation, maxXRotation);
                        totalPitch = Mathf.Clamp(totalPitch + xRot, minZRotation, maxZRotation);
                    }
                    ApplyBrainCameraPositionAndRotation();
                }
            }
        }
    }

    private float SpeedMultiplier()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            return shiftMult;
        else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            return ctrlMult;
        else
            return 1f;
    }

    void ApplyBrainCameraPositionAndRotation()
    {
        Quaternion curRotation = Quaternion.Euler(totalYaw, totalSpin, totalPitch);
        // Move the camera back to zero, perform rotation, then offset back
        brainCameraRotator.transform.position = initialCameraRotatorPosition + cameraPositionOffset;
        brainCameraRotator.transform.LookAt(cameraTarget, Vector3.back);
        brainCameraRotator.transform.position = curRotation * (brainCameraRotator.transform.position - cameraTarget) + cameraTarget;
        brainCameraRotator.transform.rotation = curRotation * brainCameraRotator.transform.rotation;
    }

    void ClearMouseDown()
    {
        mouseDownOverBrain = false;
        //brainCameraClickthroughTarget = null;
        brainTransformChanged = false;
    }

    public Vector2 GetPitchYaw()
    {
        return new Vector2(totalPitch, totalYaw);
    }

    public Vector3 GetAngles()
    {
        return new Vector3(totalPitch, totalYaw, totalSpin);
    }

    public float GetZoom()
    {
        return brainCamera.orthographic ? brainCamera.orthographicSize : brainCamera.fieldOfView;
    }

    public void SetZoom(float zoom)
    {
        if (brainCamera.orthographic)
            brainCamera.orthographicSize = zoom;
        else
            brainCamera.fieldOfView = zoom;
    }

    public void SetBrainAxisAngles(Vector2 pitchYaw)
    {
        totalPitch = pitchYaw.x;
        totalYaw = pitchYaw.y;
        totalSpin = 0f;
        ApplyBrainCameraPositionAndRotation();
    }
    public void SetBrainAxisAngles(Vector3 pitchYawSpin)
    {
        totalPitch = pitchYawSpin.x;
        totalYaw = pitchYawSpin.y;
        totalSpin = pitchYawSpin.z;
        ApplyBrainCameraPositionAndRotation();
    }

    public void SetYaw(float newYaw)
    {
        totalYaw = newYaw;
    }

    public void SetPitch(float newPitch)
    {
        totalPitch = newPitch;
    }

    public void SetSpin(float newSpin)
    {
        totalSpin = newSpin;
    }

    public Vector3 GetCameraTarget()
    {
        return cameraTarget;
    }

    public void SetCameraTarget(Vector3 newTarget)
    {
        Debug.Log("Setting camera target to: " + newTarget);

        // Reset any panning 
        brainCamera.transform.localPosition = Vector3.zero;

        cameraTarget = newTarget;
        cameraPositionOffset = newTarget;

        ApplyBrainCameraPositionAndRotation();
    }

    public void ResetCameraTarget()
    {
        cameraTarget = brain.transform.position;
        ApplyBrainCameraPositionAndRotation();
    }

    public void SetCameraContinuousRotation(bool state)
    {
        autoRotate = state;
    }

    public void SetCameraRotationSpeed(float speed)
    {
        autoRotateSpeed = speed;
    }

    public void SetCamera(Camera newCamera)
    {
        brainCamera = newCamera;
        ApplyBrainCameraPositionAndRotation();
    }

    public Camera GetCamera()
    {
        return brainCamera;
    }

    public void SetCameraBackgroundColor(Color newColor)
    {
        brainCamera.backgroundColor = newColor;
    }

    public void SetOffsetPosition(Vector3 newOffset)
    {
        cameraPositionOffset = newOffset;
        ApplyBrainCameraPositionAndRotation();
    }
}
