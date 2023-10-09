using BrainAtlas.CoordinateSystems;
using UnityEngine;

namespace CoordinateSpaces
{
    public class NewScaleSpace : CoordinateSpace
    {
        public override string Name => "NewScale";
        public override Vector3 Dimensions => new(15, 15, 15);
        
        /// <summary>
        /// Convert coordinates from New Scale space to Unity world space
        /// </summary>
        /// <param name="coord">X, Y, Z coordinate in Sensapex space</param>
        /// <returns>World space coordinates</returns>
        public override Vector3 Space2World(Vector3 coord)
        {
            return new Vector3(coord.x, coord.z, -coord.y);
        }

        /// <summary>
        /// Convert coordinates from world space to New Scale space
        /// </summary>
        /// <param name="world">X, Y, Z coordinates in World space</param>
        /// <returns>New Scale Space coordinates</returns>
        public override Vector3 World2Space(Vector3 world)
        {
            return new Vector3(world.x, -world.z, world.y);
        }

        public override Vector3 Space2World_Vector(Vector3 coord)
        {
            return Space2World(coord);
        }

        public override Vector3 World2Space_Vector(Vector3 world)
        {
            return World2Space(world);
        }
    }
}

