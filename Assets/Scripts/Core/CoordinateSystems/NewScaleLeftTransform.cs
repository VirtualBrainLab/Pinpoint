using UnityEngine;

namespace CoordinateTransforms
{
    public class NewScaleLeftTransform : AffineTransform
    {
        public NewScaleLeftTransform(float phi, float theta) : base(Vector3.one, new Vector3(theta, 0, phi))
        {
        }

        public override string Name => "New Scale Left";
        public override string Prefix => "ns-l";
    }
}