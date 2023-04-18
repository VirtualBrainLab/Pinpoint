using UnityEngine;

namespace CoordinateTransforms
{
    public abstract class AffineTransform : CoordinateTransform
    {
        private Vector3 _scaling;
        private Vector3 _inverseScaling;
        private Quaternion _rotation;
        private Quaternion _inverseRotation;

        /// <summary>
        /// Define an AffineTransform by passing the translation, scaling, and rotation that go from origin space to this space
        /// </summary>
        /// <param name="centerCoord">(0,0,0) coordinate of transform x/y/z</param>
        /// <param name="scaling">scaling on x/y/z</param>
        /// <param name="rotation">rotation around z, y, x in that order (or on xy plane, then xz plane, then yz plane)</param>
        public AffineTransform(Vector3 scaling, Vector3 rotation)
        {
            _scaling = scaling;
            _inverseScaling = new Vector3(1f / _scaling.x, 1f / _scaling.y, 1f / _scaling.z);
            _rotation = Quaternion.Euler(rotation);
            _inverseRotation = Quaternion.Inverse(_rotation);
        }

        /// <summary>
        /// Transform a coordinate by this AffineTransform
        /// </summary>
        /// <param name="ccfCoord"></param>
        /// <returns></returns>
        public override Vector3 Space2Transform(Vector3 ccfCoord)
        {
            return Vector3.Scale(_rotation*ccfCoord, _scaling);
        }

        /// <summary>
        /// Invert a coordinate from this AffineTransform back to it's CoordinateSpace
        /// </summary>
        /// <param name="coordTransformed"></param>
        /// <returns></returns>
        public override Vector3 Transform2Space(Vector3 coordTransformed)
        {
            return _inverseRotation*Vector3.Scale(coordTransformed, _inverseScaling);
        }

        /// <summary>
        /// Rotate any axes that have been flipped in the Transformed space
        /// 
        /// Note: this does **NOT** apply the AffineTransform rotation!
        /// </summary>
        /// <param name="coordSpace"></param>
        /// <returns></returns>
        public override Vector3 Space2TransformAxisChange(Vector3 coordSpace)
        {
            return new Vector3(
                Mathf.Sign(_scaling.x) * coordSpace.x,
                Mathf.Sign(_scaling.y) * coordSpace.y,
                Mathf.Sign(_scaling.z) * coordSpace.z);
        }

        /// <summary>
        /// Un-rotate any axes that have been flipped in the Transformed space
        /// 
        /// Note: this does **NOT** reverse the AffineTransform rotation!
        /// </summary>
        /// <param name="coordTransformed"></param>
        /// <returns></returns>
        public override Vector3 Transform2SpaceAxisChange(Vector3 coordTransformed)
        {
            return new Vector3(
                Mathf.Sign(_inverseScaling.x) * coordTransformed.x,
                Mathf.Sign(_inverseScaling.y) * coordTransformed.y,
                Mathf.Sign(_inverseScaling.z) * coordTransformed.z);
        }
    }
}
