using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoordinateTransforms
{
    public sealed class SensapexLeftTransform : AffineTransform
    {
        public override string Name => "Sensapex Left";
        public override string Prefix => "spx";

        public SensapexLeftTransform(Vector3 rotation) : base(Vector3.one, rotation)
        {
        }
    }

}

