using System;
using UnityEngine;

[Serializable]
public struct ProbeData
{
    public string Name;
    public Color Color;
    public int Type;
    public string UUID;

    // ChannelMap
    public string SelectionLayerName;

    // API
    public string APITarget;

    public InsertionData Insertion;
    public ManipulatorControllerData ManipulatorController;
}
