using UnityEngine;

namespace CoordinateTransforms
{
    public class SensapexRightTransform : AffineTransform
    {
        public override string Name => "Sensapex Right";
        public override string Prefix => "spx-r";

        public SensapexRightTransform(float phi) : base(Vector3.one, new Vector3(0, 0, phi))
        {
        }

    }
}
