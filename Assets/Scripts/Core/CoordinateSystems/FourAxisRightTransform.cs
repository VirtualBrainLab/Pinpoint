using UnityEngine;

namespace CoordinateTransforms
{
    public class FourAxisRightTransform : AffineTransform
    {
        public override string Name => "Four Axis Right";
        public override string Prefix => "4r";

        public FourAxisRightTransform(float phi) : base(Vector3.one, new Vector3(0, 0, phi))
        {
        }

    }
}
