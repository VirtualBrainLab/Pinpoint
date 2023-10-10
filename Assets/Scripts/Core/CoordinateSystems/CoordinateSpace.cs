using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoordinateSpaces
{
    public abstract class CoordinateSpace
    {
        public abstract string Name { get; }

        public abstract Vector3 Dimensions { get; set;  }

        public Vector3 RelativeOffset { get; set; } = Vector3.zero;

        /// <summary>
        /// Convert coordinates from this coordinate space to Unity world space
        /// </summary>
        /// <param name="coord">X, Y, Z coordinate in this coordinate space</param>
        /// <returns>Unity world space coordinates</returns>
        public abstract Vector3 Space2World(Vector3 coord);

        /// <summary>
        /// Convert coordinates from Unity world space to this coordinate space
        /// </summary>
        /// <param name="world">X, Y, Z coordinates in Unity world space</param>
        /// <returns>Coordinate space coordinates</returns>
        public abstract Vector3 World2Space(Vector3 world);

        public abstract Vector3 Space2WorldAxisChange(Vector3 coord);

        public abstract Vector3 World2SpaceAxisChange(Vector3 world);

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            return obj is CoordinateSpace space &&
                   Name == space.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name);
        }
    }
}