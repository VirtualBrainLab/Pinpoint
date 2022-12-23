using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class AxisControl : MonoBehaviour
{
    [FormerlySerializedAs("apGO")] [SerializeField] private GameObject _apGo;
    [FormerlySerializedAs("dvGO")] [SerializeField] private GameObject _dvGo;
    [FormerlySerializedAs("mlGO")] [SerializeField] private GameObject _mlGo;
    [FormerlySerializedAs("depthGO")] [SerializeField] private GameObject _depthGo;

    public void SetAxisPosition(Transform transform)
    {
        if (transform == null)
            return;
        _apGo.transform.position = transform.position;
        _dvGo.transform.position = transform.position;
        _mlGo.transform.position = transform.position;
        _depthGo.transform.position = transform.position;
        _depthGo.transform.rotation = transform.rotation;
    }
    public void SetAPVisibility(bool state)
    {
        _apGo.SetActive(state);
    }
    public void SetDVVisibility(bool state)
    {
        _dvGo.SetActive(state);
    }
    public void SetMLVisibility(bool state)
    {
        _mlGo.SetActive(state);
    }
    public void SetDepthVisibility(bool state)
    {
        _depthGo.SetActive(state);
    }
}
