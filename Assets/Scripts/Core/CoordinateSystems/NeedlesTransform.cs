using UnityEngine;

namespace CoordinateTransforms
{
    public class NeedlesTransform : AffineTransform
    {
        public override string Name { get { return "Dorr2008"; } }

        public override string Prefix { get { return "d08"; } }


        //private Vector3 invivoConversionAPMLDV = new Vector3(-1.087f, 1f, -0.952f);
        //private Vector3 inverseConversion = new Vector3(-1 / 1.087f, 1f, -1 / 0.952f);
        //private Vector3 bregma = new Vector3(5.4f, 5.7f, 0.332f);

        /// <summary>
        /// Angles are (yaw, pitch, spin)
        /// </summary>
        public NeedlesTransform() : base(new Vector3(-1.087f, 1f, -0.952f), new Vector3(0f, -5f, 0f))
        {

        }
    }

    public class IBLNeedlesTransform : AffineTransform
    {
        public override string Name { get { return "IBL-Dorr2008"; } }

        public override string Prefix { get { return "i-d08"; } }


        //private Vector3 invivoConversionAPMLDV = new Vector3(-1.087f, 1f, -0.952f);
        //private Vector3 inverseConversion = new Vector3(-1 / 1.087f, 1f, -1 / 0.952f);
        //private Vector3 bregma = new Vector3(5.4f, 5.7f, 0.332f);

        /// <summary>
        /// Angles are (yaw, pitch, spin)
        /// </summary>
        public IBLNeedlesTransform() : base(new Vector3(-1.087f, 1f, -0.952f), new Vector3(0f, 0f, 0f))
        {

        }
    }
}