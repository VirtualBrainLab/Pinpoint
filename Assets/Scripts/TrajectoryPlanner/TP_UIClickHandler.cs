using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TrajectoryPlanner;
using UnityEngine.Serialization;

public class TP_UIClickHandler : MonoBehaviour
{
    // Raycaster
    private GraphicRaycaster raycaster;
    [FormerlySerializedAs("tpmanager")] [SerializeField] private TrajectoryPlannerManager _tpmanager;
    [FormerlySerializedAs("inPlaneSlice")] [SerializeField] private TP_InPlaneSlice _inPlaneSlice;
    //[SerializeField] private UM_CameraController cameraController;

    // Start is called before the first frame update
    void Start()
    {
        raycaster = GetComponent<GraphicRaycaster>();
    }

    // Update is called once per frame
    void Update()
    {
        // Check if user is over a UI element
        if (EventSystem.current.IsPointerOverGameObject())
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            GameObject uiTarget = GetUIRaycastTarget(pointerData);

            // Check if the element they are over is the in-plane slice element
            if (uiTarget != null)
            {
                // check if this is the in-plane slice panel
                if (uiTarget.name == "InPlaneSlicePanel")
                {
                    _inPlaneSlice.InPlaneSliceHover(pointerData.position);
                }
                else if (Input.GetMouseButtonDown(0))
                {
                    switch (uiTarget.tag)
                    {
                        case "ProbePanel":
                            _tpmanager.SetActiveProbe(uiTarget.GetComponent<TP_ProbePanel>().GetProbeController());
                            break;
                        case "AreaPanel":
                            _tpmanager.ClickSearchArea(uiTarget);
                            break;
                        case "UIEvent":
                            if (uiTarget.name.Equals("Text2ClipboardButton"))
                                _tpmanager.CopyText();
                            break;
                    }
                }
            }
        }
    }

    GameObject GetUIRaycastTarget(PointerEventData pointerData)
    {
        List<RaycastResult> results = new List<RaycastResult>();

        //Raycast using the Graphics Raycaster and mouse click position
        pointerData.position = Input.mousePosition;
        raycaster.Raycast(pointerData, results);

        if (results.Count == 1)
        {
            return results[0].gameObject;
        }
        if (results.Count > 1)
        {
            //Debug.Log("Warning: multiple raycast results");
            ////For every result returned, output the name of the GameObject on the Canvas hit by the Ray
            //foreach (RaycastResult result in results)
            //{
            //    Debug.Log("Hit " + result.gameObject.name);
            //}
            return results[0].gameObject;
        }

        return null;
    }
}
