using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoordinateSpaces
{
    public abstract class CoordinateSpace
    {
        public abstract string Name { get; }

        public abstract Vector3 Dimensions { get; }

        public Vector3 RelativeOffset { get; set; } = Vector3.zero;

        public abstract Vector3 Space2World(Vector3 coord);
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
