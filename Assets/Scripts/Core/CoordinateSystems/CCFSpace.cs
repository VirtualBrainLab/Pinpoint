using UnityEngine;

namespace CoordinateSpaces
{
    /// <summary>
    /// CCF CoordinateSpace defined in (AP,ML,DV) coordinates
    /// </summary>
    public sealed class CCFSpace : CoordinateSpace
    {
        private string _name = "CCF";
        private Vector3 _dimensions = new Vector3(13.2f, 11.4f, 8f);
        private Vector3 _zeroOffset = new Vector3(-5.7f, -4.0f, +6.6f); // note: zero offset is in *world* coordinates!

        public override Vector3 Dimensions
        {
            get
            {
                return _dimensions;
            }
        }

        public override string Name { get => _name; }

        public override Vector3 Space2World(Vector3 coord)
        {
            return Space2WorldAxisChange(coord + RelativeOffset) - _zeroOffset;
        }

        public override Vector3 World2Space(Vector3 world)
        {
            return World2SpaceAxisChange(world + _zeroOffset) - RelativeOffset;
        }

        public override Vector3 Space2WorldAxisChange(Vector3 coord)
        {
            return new Vector3(-coord.y, -coord.z, coord.x);
        }

        public override Vector3 World2SpaceAxisChange(Vector3 world)
        {
            return new Vector3(world.z, -world.x, -world.y);
        }

    }
}
