using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abstract interface for AccountsManager classes
/// 
/// The trajectory planner needs to be able to communicate information back-and-forth about accounts:
/// 
/// From accounts:
/// * OnLoadCallback: triggered by AccountsManager when a new account is logged into
/// * ActiveExperiment (string)
/// * ActiveExperimentInsertions (List<probeData>) where probeData is UUID, pos, angles, etc
/// * AvailableExperiments (List<string>)
/// * 
/// </summary>
public abstract class AccountsManager : MonoBehaviour
{

}
