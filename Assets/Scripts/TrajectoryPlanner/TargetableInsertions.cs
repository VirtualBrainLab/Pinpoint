using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TrajectoryPlanner;

public class TargetableInsertions : MonoBehaviour
{
    [SerializeField] private TrajectoryPlannerManager _tpManager;
    [SerializeField] private GameObject _probePlaceholderPrefab;
    [SerializeField] private Transform _targetableProbeParentT;
    [SerializeField] private UnisaveAccountsManager _accountsManager;

    private Dictionary<string, ProbeManager> _targetableProbes;

    private void Awake()
    {
        _targetableProbes = new();
    }

    public void UpdateTargetableInsertions()
    {
        Dictionary<string, ServerProbeInsertion> data = _accountsManager.GetActiveExperimentInsertions();
        // Doing the most inefficient thing, delete all _targetableProbes and then recreate them if needed
        HashSet<string> allUUID = data.Keys.Union(_targetableProbes.Keys).ToHashSet();

        // loop over ever UUID and decide what to do with it
        foreach (string UUID in allUUID)
        {
            if (data.ContainsKey(UUID))
            {
                // If we are in data, then we either ignore the probe if active, or make sure it's in the targetable list
                if (!data[UUID].active)
                {
                    // This probe is not active, so it needs to be made targetable
                    var insertionData = _tpManager.ServerProbeInsertion2ProbeInsertion(data[UUID]);

                    if (_targetableProbes.ContainsKey(UUID))
                    {
                        // update data
                        _targetableProbes[UUID].ProbeController.SetSpaceTransform(insertionData.space, insertionData.transform);
                        _targetableProbes[UUID].ProbeController.SetProbePosition(insertionData.apmldv);
                        _targetableProbes[UUID].ProbeController.SetProbeAngles(insertionData.angles);
                    }
                    else
                    {
                        //  probe doesn't exist but we'll make it
                        GameObject probeGO = Instantiate(_probePlaceholderPrefab, _targetableProbeParentT);
                        ProbeManager probeManager = probeGO.GetComponent<ProbeManager>();
                        probeManager.OverrideUUID(UUID);
                        probeManager.ProbeController.SetSpaceTransform(insertionData.space, insertionData.transform);
                        probeManager.ProbeController.SetProbePosition(insertionData.apmldv);
                        probeManager.ProbeController.SetProbeAngles(insertionData.angles);
                        _targetableProbes.Add(UUID, probeManager);
                    }
                    continue;
                }
            }

            // Make sure the probe isn't in the targetable list
            if (_targetableProbes.ContainsKey(UUID))
            {
                // This probe is in targetable but it shouldn't be
                ProbeManager probeManager = _targetableProbes[UUID];
                Destroy(probeManager.gameObject);
                _targetableProbes.Remove(UUID);
            }
        }

        Debug.Log($"ProbeInsertions {ProbeInsertion.Instances.Count} Targetable {ProbeInsertion.TargetableInstances.Count}");
        foreach (var probe in ProbeInsertion.TargetableInstances)
        {
            Debug.Log(probe.ToString());
        }
    }
}
