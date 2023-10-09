using UnityEngine;

namespace CoordinateTransforms
{
    public class NewScaleLeftTransform : CoordinateTransform
    {
        private readonly Quaternion _inverseRotation;
        private readonly Quaternion _rotation;

        public NewScaleLeftTransform(float yaw, float pitch)
        {
            var yawRotation = Quaternion.AngleAxis(yaw, Vector3.forward);
            var pitchRotation = Quaternion.AngleAxis(pitch, yawRotation * Vector3.left);

            _rotation = yawRotation * pitchRotation;
            _inverseRotation = Quaternion.Inverse(_rotation);
        }

        public override string Name => "New Scale Left";
        public override string Prefix => "ns-l";

        public override Vector3 T2Atlas(Vector3 coordTransformed)
        {
            return _inverseRotation * coordTransformed;
        }

        public override Vector3 Atlas2T(Vector3 coordSpace)
        {
            return _rotation * coordSpace;
        }

        public override Vector3 T2Atlas_Vector(Vector3 coordTransformed)
        {
            return coordTransformed;
        }

        public override Vector3 Atlas2T_Vector(Vector3 coordSpace)
        {
            return coordSpace;
        }
    }
}