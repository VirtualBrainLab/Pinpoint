using System;
using UnityEngine;

[Serializable]
public struct ProbeData
{
    public string Name;
    public Color Color;
    public int Type;

    public InsertionData Insertion;
    public ManipulatorControllerData ManipulatorController;
}
