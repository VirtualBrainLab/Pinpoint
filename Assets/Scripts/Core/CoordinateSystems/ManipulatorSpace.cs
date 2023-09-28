using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoordinateSpaces
{
    public sealed class ManipulatorSpace : CoordinateSpace
    {
        public override string Name => "Manipulator";
        public override Vector3 Dimensions => new(20, 20, 20);
        public override Vector3 Space2World(Vector3 coord)
        {
            return new Vector3(-coord.x, coord.y, -coord.z);
        }

        public override Vector3 World2Space(Vector3 world)
        {
            return new Vector3(-world.x, world.y, -world.z);
        }

        public override Vector3 Space2WorldAxisChange(Vector3 coord)
        {
            return Space2World(coord);
        }

        public override Vector3 World2SpaceAxisChange(Vector3 world)
        {
            return World2Space(world);
        }
    }
}

