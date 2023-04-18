using UnityEngine;

namespace CoordinateTransforms
{
    public class SensapexRightTransform : AffineTransform
    {
        public override string Name => "Sensapex Right";
        public override string Prefix => "spx-r";

        public SensapexRightTransform(Vector3 rotation) : base(Vector3.one, rotation)
        {
        }

    }
}
