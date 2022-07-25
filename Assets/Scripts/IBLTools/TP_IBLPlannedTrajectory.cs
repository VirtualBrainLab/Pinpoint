using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TrajectoryPlanner;

namespace IBLTools
{
    public class TP_IBLPlannedTrajectory : MonoBehaviour
    {
        public Vector3 coords;
        public Vector3 angles;

        private TrajectoryPlannerManager tpmanager;

        // Start is called before the first frame update
        void Start()
        {
            tpmanager = GameObject.Find("main").GetComponent<TrajectoryPlannerManager>();
        }

        //private void OnMouseDown()
        //{
        //    //if (tpmanager.GetActiveProbeController() != null)
        //    //    tpmanager.ManualCoordinateEntry(coords.x, coords.y, coords.z, angles.x, angles.y, 0f);
        //}
    }

}