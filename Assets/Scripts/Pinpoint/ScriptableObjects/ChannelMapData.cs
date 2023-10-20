using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable, PreferBinarySerialization]
public class ChannelMapData : ScriptableObject
{
    public Vector3[] ChannelCoords;
    public Vector3 ChannelShape;

    public float FullHeight;

    public string DefaultSelectionLayer;
    public List<string> SelectionLayerNames;
    public List<BoolArray> SelectionLayers;

    [Serializable]
    public class BoolArray
    {
        public bool[] data;
    }
}
