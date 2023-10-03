using UnityEngine;

namespace CoordinateTransforms
{
    public class RightHandedManipulatorTransform : AffineTransform
    {
        public override string Name => "Four Axis Right";
        public override string Prefix => "4r";

        public RightHandedManipulatorTransform(float phi) : base(2 * Vector3.left + Vector3.one,
            new Vector3(0, -phi, 0))
        {
        }
    }
}