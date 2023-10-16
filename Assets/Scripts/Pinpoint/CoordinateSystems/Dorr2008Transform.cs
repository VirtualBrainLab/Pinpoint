using BrainAtlas.CoordinateSystems;
using UnityEngine;

namespace CoordinateTransforms
{
    public class Dorr2008Transform : AffineTransform
    {
        public override string Name { get { return "Dorr2008"; } }

        public override string Prefix { get { return "d08"; } }


        //private Vector3 invivoConversionAPMLDV = new Vector3(-1.087f, 1f, -0.952f);
        //private Vector3 inverseConversion = new Vector3(-1 / 1.087f, 1f, -1 / 0.952f);
        //private Vector3 bregma = new Vector3(5.4f, 5.7f, 0.332f);

        /// <summary>
        /// Angles are (yaw, pitch, spin)
        /// </summary>
        public Dorr2008Transform() : base(new Vector3(-1.087f, 1f, -0.952f), new Vector3(0f, -5f, 0f))
        {

        }
    }

    public class Dorr2008IBLTransform : AffineTransform
    {
        public override string Name { get { return "IBL-Dorr2008"; } }

        public override string Prefix { get { return "i-d08"; } }


        //private Vector3 invivoConversionAPMLDV = new Vector3(-1.087f, 1f, -0.952f);
        //private Vector3 inverseConversion = new Vector3(-1 / 1.087f, 1f, -1 / 0.952f);
        //private Vector3 bregma = new Vector3(5.4f, 5.7f, 0.332f);

        /// <summary>
        /// Angles are (yaw, pitch, spin)
        /// </summary>
        public Dorr2008IBLTransform() : base(new Vector3(-1.087f, 1f, -0.952f), new Vector3(0f, 0f, 0f))
        {

        }
    }
}