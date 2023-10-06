using UnityEngine;

namespace CoordinateTransforms
{
    public class ThreeAxisLeftTransform : CoordinateTransform
    {
        private readonly Quaternion _inverseRotation;
        private readonly Quaternion _rotation;

        public ThreeAxisLeftTransform(float yaw, float pitch)
        {
            // var yawRotation = Quaternion.AngleAxis(-yaw, Vector3.up);
            // var pitchRotation = Quaternion.AngleAxis(pitch, Vector3.right);
            //
            // _rotation = yawRotation * pitchRotation;
            _rotation = Quaternion.Euler(pitch, -yaw, 0);
            _inverseRotation = Quaternion.Inverse(_rotation);
            
            Debug.DrawRay(Vector3.zero, _rotation * Vector3.back, Color.cyan, 1000);
            Debug.DrawRay(Vector3.zero, _rotation * Vector3.up, Color.green, 1000);
            Debug.DrawRay(Vector3.zero, _rotation * Vector3.left, Color.red, 1000);
        }

        public override string Name => "Three Axis Left";
        public override string Prefix => "3l";

        public override Vector3 Transform2Space(Vector3 coordTransformed)
        {
            return _inverseRotation * coordTransformed;
        }

        public override Vector3 Space2Transform(Vector3 coordSpace)
        {
            return _rotation * coordSpace;
        }

        public override Vector3 Transform2SpaceAxisChange(Vector3 coordTransformed)
        {
            return coordTransformed;
        }

        public override Vector3 Space2TransformAxisChange(Vector3 coordSpace)
        {
            return coordSpace;
        }
    }
}