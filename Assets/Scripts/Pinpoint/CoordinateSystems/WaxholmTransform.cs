using BrainAtlas.CoordinateSystems;
using UnityEngine;

namespace CoordinateTransforms
{
    public class WaxholmTransform : AffineTransform
    {

        public override string Name { get { return "Qiu2018"; } }

        public override string Prefix { get { return "q18"; } }

        /// <summary>
        /// Angles are (yaw, pitch, spin)
        /// </summary>
        public WaxholmTransform() : base(Vector3.zero, new Vector3(0f, -4f, 0f))
        {

        }
    }
}