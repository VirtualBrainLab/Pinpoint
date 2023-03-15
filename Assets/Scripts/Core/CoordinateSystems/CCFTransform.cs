using UnityEngine;


namespace CoordinateTransforms
{
    public class CCFTransform : CoordinateTransform
    {
        public override string Name
        {
            get
            {
                return "CCF";
            }
        }

        public override string Prefix
        {
            get
            {
                return "ccf";
            }
        }

        public override Vector3 Transform2Space(Vector3 coord)
        {
            return coord;
        }

        public override Vector3 Space2Transform(Vector3 coord)
        {
            return coord;
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