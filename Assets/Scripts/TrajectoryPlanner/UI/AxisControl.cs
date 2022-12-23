using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class AxisControl : MonoBehaviour
{
    [FormerlySerializedAs("apGO")] [SerializeField] private static GameObject _apGo;
    [FormerlySerializedAs("dvGO")] [SerializeField] private static GameObject _dvGo;
    [FormerlySerializedAs("mlGO")] [SerializeField] private static GameObject _mlGo;
    [FormerlySerializedAs("depthGO")] [SerializeField] private static GameObject _depthGo;

    public static bool Enabled;

    public static void SetAxisPosition(Transform transform)
    {
        if (transform == null)
            return;
        _apGo.transform.position = transform.position;
        _dvGo.transform.position = transform.position;
        _mlGo.transform.position = transform.position;
        _depthGo.transform.position = transform.position;
        _depthGo.transform.rotation = transform.rotation;
    }
    public static void SetAPVisibility(bool state)
    {
        _apGo.SetActive(state);
    }
    public static void SetDVVisibility(bool state)
    {
        _dvGo.SetActive(state);
    }
    public static void SetMLVisibility(bool state)
    {
        _mlGo.SetActive(state);
    }
    public static void SetDepthVisibility(bool state)
    {
        _depthGo.SetActive(state);
    }

    public static void SetAxisVisibility(bool AP, bool ML, bool DV, bool depth, Transform transform)
    {
        SetAxisPosition(transform);
        SetAPVisibility(AP);
        SetMLVisibility(ML);
        SetDVVisibility(DV);
        SetDepthVisibility(depth);
    }
}
