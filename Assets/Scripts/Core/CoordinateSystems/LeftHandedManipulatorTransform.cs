using UnityEngine;

namespace CoordinateTransforms
{
    public sealed class LeftHandedManipulatorTransform : AffineTransform
    {
        public LeftHandedManipulatorTransform(float phi) : base(Vector3.one, new Vector3(0, -phi, 0))
        {
        }

        public override string Name => "Left Handed Manipulator";
        public override string Prefix => "lhm";
    }
}