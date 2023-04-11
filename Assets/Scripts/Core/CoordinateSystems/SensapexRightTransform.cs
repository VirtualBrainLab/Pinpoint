using UnityEngine;

namespace CoordinateTransforms
{
    public class SensapexRightTransform : AffineTransform
    {
        public override string Name => "Sensapex Right";
        public override string Prefix => "spx";

        public SensapexRightTransform(Vector3 rotation) : base(new Vector3(1, 1, 1), rotation)
        {
        }

    }
}
