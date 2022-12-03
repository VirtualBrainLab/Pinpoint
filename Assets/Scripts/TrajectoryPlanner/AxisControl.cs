using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AxisControl : MonoBehaviour
{
    [SerializeField] GameObject apGO;
    [SerializeField] GameObject dvGO;
    [SerializeField] GameObject mlGO;
    [SerializeField] GameObject depthGO;

    public void SetAxisPosition(Transform transform)
    {
        if (transform == null)
            return;
        apGO.transform.position = transform.position;
        dvGO.transform.position = transform.position;
        mlGO.transform.position = transform.position;
        depthGO.transform.position = transform.position;
        depthGO.transform.rotation = transform.rotation;
    }
    public void SetAPVisibility(bool state)
    {
        apGO.SetActive(state);
    }
    public void SetDVVisibility(bool state)
    {
        dvGO.SetActive(state);
    }
    public void SetMLVisibility(bool state)
    {
        mlGO.SetActive(state);
    }
    public void SetDepthVisibility(bool state)
    {
        depthGO.SetActive(state);
    }
}
