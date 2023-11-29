using System;
using BrainAtlas.CoordinateSystems;
using UnityEngine;

namespace Pinpoint.CoordinateSystems
{
    public class PathfinderSpace : CoordinateSpace
    {
        #region Properties

        public override string Name => "Pathfinder";
        public override Vector3 Dimensions => Vector3.one * 15f;

        #endregion


        public override Vector3 Space2World(Vector3 coordSpace, bool useReference = true)
        {
            return new Vector3(coordSpace.x, -coordSpace.z, -coordSpace.y);
        }

        public override Vector3 World2Space(Vector3 coordWorld, bool useReference = true)
        {
            return new Vector3(coordWorld.x, -coordWorld.z, -coordWorld.y);
        }

        public override Vector3 Space2World_Vector(Vector3 vecSpace)
        {
            return Space2World(vecSpace);
        }

        public override Vector3 World2Space_Vector(Vector3 vecWorld)
        {
            return World2Space(vecWorld);
        }
    }
}