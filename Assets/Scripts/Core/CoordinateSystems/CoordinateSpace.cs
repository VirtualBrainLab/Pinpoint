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

        public abstract Vector3 Atlas2World(Vector3 coordU);
        public abstract Vector3 World2Atlas(Vector3 coordWorld);

        public abstract Vector3 Atlas2World_Vector(Vector3 vecU);

        public abstract Vector3 World2Atlas_Vector(Vector3 vecWorld);

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
