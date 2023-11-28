using BrainAtlas.CoordinateSystems;
using UnityEngine;

namespace CoordinateTransforms
{
    public class ThreeAxisLeftHandedTransform : CoordinateTransform
    {
        private readonly Quaternion _inverseRotation;
        private readonly Quaternion _rotation;

        public ThreeAxisLeftHandedTransform(float yaw, float pitch)
        {
            var yawRotation = Quaternion.AngleAxis(-yaw, Vector3.up);
            var pitchRotation = Quaternion.AngleAxis(pitch, Vector3.right);

            _rotation = pitchRotation * yawRotation;
            _inverseRotation = Quaternion.Inverse(_rotation);
        }

        public override string Name => "Three Axis Left Handed Manipulator";
        public override string Prefix => "3lhm";

        public override Vector3 T2U(Vector3 coordTransformed)
        {
            return _inverseRotation * coordTransformed;
        }

        public override Vector3 U2T(Vector3 coordSpace)
        {
            return _rotation * coordSpace;
        }

        public override Vector3 T2U_Vector(Vector3 coordTransformed)
        {
            return coordTransformed;
        }

        public override Vector3 U2T_Vector(Vector3 coordSpace)
        {
            return coordSpace;
        }
    }
}