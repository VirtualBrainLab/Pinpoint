using BrainAtlas.CoordinateSystems;
using UnityEngine;

namespace Pinpoint.CoordinateSystems
{
    public sealed class ManipulatorSpace : CoordinateSpace
    {
        // Default to Sensapex dimensions
        public override Vector3 Dimensions { get; } = Vector3.one * 20;
        public override string Name => "Manipulator";

        public override Vector3 Space2World(Vector3 coordSpace, bool useReference = true)
        {
            return new Vector3(-coordSpace.x, coordSpace.y, -coordSpace.z);
        }

        public override Vector3 World2Space(Vector3 coordWorld, bool useReference = true)
        {
            return new Vector3(-coordWorld.x, coordWorld.y, -coordWorld.z);
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