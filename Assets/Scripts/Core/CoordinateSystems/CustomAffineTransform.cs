using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoordinateTransforms
{
    public class CustomAffineTransform : AffineTransform
    {
        public override string Name { get { return "Custom"; } }

        public override string Prefix { get { return "cu"; } }

        /// <summary>
        /// Angles are (yaw, pitch, spin)
        /// </summary>
        public CustomAffineTransform(Vector3 scaling, Vector3 angles) : base(scaling, angles)
        {

        }
    }
}