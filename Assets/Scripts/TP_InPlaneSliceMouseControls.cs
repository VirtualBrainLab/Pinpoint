using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TP_InPlaneSliceMouseControls : MonoBehaviour
{
    [SerializeField] TP_InPlaneSlice inPlaneSlice;
    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    private void OnMouseOver()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (_collider.Raycast(ray, out hit, 100.0f))
        {
            Vector3 objectPosition = hit.point - transform.position;
            Vector2 pointerData = new Vector2(Vector3.Dot(objectPosition,transform.right), Vector3.Dot(objectPosition, transform.forward));

            Debug.Log(pointerData);

            if (Input.GetMouseButtonDown(0))
            {
                // if the user clicked call the target function
                inPlaneSlice.TargetBrainArea(pointerData);
            }
            else
            {
                inPlaneSlice.InPlaneSliceHover(pointerData);
            }
        }
    }
}
