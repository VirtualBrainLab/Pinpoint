using BrainAtlas.CoordinateSystems;
using UnityEngine;

namespace CoordinateTransforms
{
    public class Qiu2018Transform : AffineTransform
    {
        //ML_SCALE = 0.952
        //DV_SCALE = 0.885  # multiplicative factor on DV dimension, determined from MRI->CCF transform
        //AP_SCALE = 1.031  # multiplicative factor on AP dimension


        public override string Name { get { return "Qiu2018"; } }

        public override string Prefix { get { return "q18"; } }

        /// <summary>
        /// Angles are (yaw, pitch, spin)
        /// </summary>
        public Qiu2018Transform() : base(new Vector3(-1.031f, 0.952f, -0.885f), new Vector3(0f, -5f, 0f))
        {

        }
    }
}