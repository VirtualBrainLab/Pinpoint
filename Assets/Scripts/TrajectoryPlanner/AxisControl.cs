using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AxisControl : MonoBehaviour
{
    [SerializeField] GameObject _apGO;
    [SerializeField] GameObject _dvGO;
    [SerializeField] GameObject _mlGO;
    [SerializeField] GameObject _depthGO;

    public void SetAxisPosition(Transform transform)
    {
        if (transform == null)
            return;
        _apGO.transform.position = transform.position;
        _dvGO.transform.position = transform.position;
        _mlGO.transform.position = transform.position;
        _depthGO.transform.position = transform.position;
        _depthGO.transform.rotation = transform.rotation;
    }
    public void SetAPVisibility(bool state)
    {
        _apGO.SetActive(state);
    }
    public void SetDVVisibility(bool state)
    {
        _dvGO.SetActive(state);
    }
    public void SetMLVisibility(bool state)
    {
        _mlGO.SetActive(state);
    }
    public void SetDepthVisibility(bool state)
    {
        _depthGO.SetActive(state);
    }
}
