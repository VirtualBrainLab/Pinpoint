using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class RecordingRegion : MonoBehaviour
{
    [FormerlySerializedAs("recordingRegionGOs")][SerializeField] private List<GameObject> _recordingRegionGOs;

    /// <summary>
    /// Set the height of the recording region GameObject
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    public void SetSize(float startPos, float endPos)
    {
        float height = endPos - startPos;

        foreach (GameObject go in _recordingRegionGOs)
        {
            // This is a little complicated if we want to do it right (since you can accidentally scale the recording region off the probe.
            // For now, we will just reset the y position to be back at the bottom of the probe.
            Vector3 scale = go.transform.localScale;
            scale.y = height;
            go.transform.localScale = scale;

            Vector3 pos = go.transform.localPosition;
            pos.y = height / 2f + startPos;
            go.transform.localPosition = pos;
        }
    }

    public void SetVisibility(bool visible)
    {
        foreach (GameObject go in _recordingRegionGOs)
            go.SetActive(visible);
    }
}
