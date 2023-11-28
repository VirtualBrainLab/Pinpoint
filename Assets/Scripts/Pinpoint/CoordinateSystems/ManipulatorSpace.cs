using BrainAtlas.CoordinateSystems;
using UnityEngine;

namespace Pinpoint.CoordinateSystems
{
    public sealed class ManipulatorSpace : CoordinateSpace
    {
        // Default to Sensapex dimensions
        public override Vector3 Dimensions { get; }
        public override string Name => "Manipulator";

        public ManipulatorSpace(Vector3 dimensions)
        {
            Dimensions = dimensions;
        }

        public override Vector3 Space2World(Vector3 coordSpace, bool useReference = true)
        {
            return coordSpace;
        }

        public override Vector3 World2Space(Vector3 coordWorld, bool useReference = true)
        {
            return coordWorld;
        }

        public override Vector3 Space2World_Vector(Vector3 vecSpace)
        {
            return vecSpace;
        }

        public override Vector3 World2Space_Vector(Vector3 vecWorld)
        {
            return vecWorld;
        }
    }
}