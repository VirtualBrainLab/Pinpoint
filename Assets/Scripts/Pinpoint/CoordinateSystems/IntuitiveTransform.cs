using BrainAtlas.CoordinateSystems;
using UnityEngine;

namespace CoordinateTransforms
{
    /// <summary>
    /// Rotates the axes to be in the intuitive "left-handed" directions (AP forward, ML right, DV up)
    /// </summary>
    public class IntuitiveTransform : AffineTransform
    {

        public override string Name { get { return "Default"; } }

        public override string Prefix { get { return ""; } }

        /// <summary>
        /// Angles are (yaw, pitch, spin)
        /// </summary>
        public IntuitiveTransform() : base(new Vector3(-1f, 1f, -1f), new Vector3(0f, 0f, 0f))
        {

        }
    }
}