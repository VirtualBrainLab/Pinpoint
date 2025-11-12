using System;
using UnityEngine;

namespace Models.Settings
{
  [Serializable]
    public record AtlasSettingsState
    {
        public string AtlasName = "allen_mouse_25um";
        public string AtlasTransformName = "Default";
        public Vector3 ReferenceCoord = new(float.NaN, float.NaN, float.NaN);
        public bool Show3DSlices = false;
    }
}
