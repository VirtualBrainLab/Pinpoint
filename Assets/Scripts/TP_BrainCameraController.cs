using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TP_BrainCameraController : MonoBehaviour
{
    [SerializeField] private Camera brainCamera;
    [SerializeField] private GameObject brainCameraRotator;
    [SerializeField] private GameObject brain;
    [SerializeField] private TP_TrajectoryPlannerManager tpmanager;

    private Vector3 initialCameraRotatorPosition;

    public float minFoV = 15.0f;
    public float maxFoV = 90.0f;
    public float fovDelta = 15.0f;
    public float orthoDelta = 5.0f;
    public float moveSpeed = 50.0f;
    public float rotSpeed = 1000.0f;
    public float minXRotation = -90;
    public float maxXRotation = 90;
    public float minZRotation = -90;
    public float maxZRotation = 90;

    // Start is called before the first frame update
    void Start()
    {
        // Artifically limit the framerate
        Application.targetFrameRate = 144;

        initialCameraRotatorPosition = brainCameraRotator.transform.position;
        lastLeftClick = Time.realtimeSinceStartup;
        lastRightClick = Time.realtimeSinceStartup;
    }

    // Update is called once per frame
    void Update()
    {
        // Check the scroll wheel and deal with the field of view
        float fov = brainCamera.orthographic ? brainCamera.orthographicSize : brainCamera.fieldOfView;

        float scroll = -Input.GetAxis("Mouse ScrollWheel");
        fov += (brainCamera.orthographic ? orthoDelta : fovDelta) * scroll;
        fov = Mathf.Clamp(fov, minFoV, maxFoV);

        if (brainCamera.orthographic)
            brainCamera.orthographicSize = fov;
        else
            brainCamera.fieldOfView = fov;

        // Now check if the mouse wheel is being held down
        if (Input.GetMouseButton(1) && !tpmanager.ProbeControl && !EventSystem.current.IsPointerOverGameObject())
        {
            mouseDownOverBrain = true;
            mouseButtonDown = 1;
            tpmanager.BrainControl = true;
        }

        // Now deal with dragging
        if (Input.GetMouseButtonDown(0) && !tpmanager.ProbeControl && !EventSystem.current.IsPointerOverGameObject())
        {
            //BrainCameraDetectTargets();
            mouseDownOverBrain = true;
            mouseButtonDown = 0;
            tpmanager.BrainControl = true;
        }

        BrainCameraControl_noTarget();
    }

    private bool mouseDownOverBrain;
    private int mouseButtonDown;
    private bool brainTransformChanged;
    private float doubleClickTime = 0.15f;
    private float lastLeftClick;
    private float lastRightClick;

    private float totalYaw;
    private float totalPitch;

    void BrainCameraControl_noTarget()
    {
        if (mouseDownOverBrain)
        {
            // Deal with releasing the mouse (anywhere)
            if (mouseButtonDown == 0 && Input.GetMouseButtonUp(0))
            {
                if (!brainTransformChanged)
                {
                    // All we did was click through the brain window 
                    //if (brainCameraClickthroughTarget)
                    //{
                    //    BrainCameraClickthrough();
                    //}
                    //if ((Time.realtimeSinceStartup - lastLeftClick) < doubleClickTime)
                    //{
                    //    totalYaw = 0f;
                    //    totalPitch = 0f;
                    //    ApplyBrainCameraRotatorRotation();
                    //}
                }

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
                float xMove = -Input.GetAxis("Mouse X") * moveSpeed * Time.deltaTime;
                float yMove = -Input.GetAxis("Mouse Y") * moveSpeed * Time.deltaTime;

                if (xMove != 0 || yMove != 0)
                {
                    brainTransformChanged = true;
                    brainCamera.transform.Translate(xMove, yMove, 0, Space.Self);
                }
            }

            // If the mouse is down, even if we are far way now we should drag the brain
            if (mouseButtonDown == 0)
            {
                float xRot = -Input.GetAxis("Mouse X") * rotSpeed * Time.deltaTime;
                float yRot = Input.GetAxis("Mouse Y") * rotSpeed * Time.deltaTime;

                if (xRot != 0 || yRot != 0)
                {
                    brainTransformChanged = true;

                    // Pitch Locally, Yaw Globally. See: https://gamedev.stackexchange.com/questions/136174/im-rotating-an-object-on-two-axes-so-why-does-it-keep-twisting-around-the-thir
                    totalYaw = Mathf.Clamp(totalYaw + yRot, minXRotation, maxXRotation);
                    totalPitch = Mathf.Clamp(totalPitch + xRot, minZRotation, maxZRotation);

                    ApplyBrainCameraRotatorRotation();
                }
            }
        }
    }
    void ApplyBrainCameraRotatorRotation()
    {
        Quaternion curRotation = Quaternion.Euler(totalYaw, 0, totalPitch);

        // Move the camera back to zero, perform rotation, then offset back
        brainCameraRotator.transform.position = initialCameraRotatorPosition;
        brainCameraRotator.transform.LookAt(brain.transform, Vector3.back);
        brainCameraRotator.transform.position = curRotation * (brainCameraRotator.transform.position - brain.transform.position) + brain.transform.position;
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

    public void SetBrainAxisAngles(Vector2 newPitchYaw)
    {
        totalPitch = newPitchYaw.x;
        totalYaw = newPitchYaw.y;
        ApplyBrainCameraRotatorRotation();
    }
}
