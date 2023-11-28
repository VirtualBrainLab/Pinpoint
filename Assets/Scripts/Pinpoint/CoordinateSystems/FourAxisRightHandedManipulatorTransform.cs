using BrainAtlas.CoordinateSystems;
using UnityEngine;

namespace Pinpoint.CoordinateSystems
{
    public class FourAxisRightHandedManipulatorTransform : AffineTransform
    {
        public override string Name => "Four Axis Right Handed Manipulator";
        public override string Prefix => "4rhm";

        public FourAxisRightHandedManipulatorTransform(float phi) : base(2 * Vector3.left + Vector3.one,
            new Vector3(0, -phi, 0))
        {
        }
    }
}