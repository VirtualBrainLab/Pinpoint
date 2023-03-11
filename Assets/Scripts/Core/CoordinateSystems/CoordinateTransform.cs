using System;
using UnityEngine;


namespace CoordinateTransforms
{
    public abstract class CoordinateTransform
    {
        public abstract string Name { get; }
        public abstract string Prefix { get; }

        /// <summary>
        /// Convert from transformed coords back to CoordinateSpace coords
        /// </summary>
        /// <param name="coordTransformed">Transformed coordinate</param>
        /// <returns></returns>
        public abstract Vector3 Transform2Space(Vector3 coordTransformed);

        /// <summary>
        /// Convert from CoordinateSpace coords to transformed coords
        /// </summary>
        /// <param name="coordSpace">CCF coordinate in ap/dv/lr</param>
        /// <returns></returns>
        public abstract Vector3 Space2Transform(Vector3 coordSpace);

        public abstract Vector3 Transform2SpaceAxisChange(Vector3 coordTransformed);

        public abstract Vector3 Space2TransformAxisChange(Vector3 coordSpace);


        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            return obj is CoordinateTransform transform &&
                   Name == transform.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name);
        }
    }
}