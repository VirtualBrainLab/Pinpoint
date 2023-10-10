using CoordinateTransforms;
using UnityEngine;

namespace Core.CoordinateSystems
{
    public sealed class FourAxisLeftHandedManipulatorTransform : AffineTransform
    {
        public FourAxisLeftHandedManipulatorTransform(float phi) : base(Vector3.one, new Vector3(0, -phi, 0))
        {
        }

        public override string Name => "Four Axis Left Handed Manipulator";
        public override string Prefix => "4lhm";
    }
}