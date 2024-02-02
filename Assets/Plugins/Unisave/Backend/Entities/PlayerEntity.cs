using System;
using System.Collections;
using System.Collections.Generic;
using Unisave;
using Unisave.Entities;
using Unisave.Facades;

/*
 * This entity represents a player of your game. To learn how to add
 * player registration and authentication, check out the documentation:
 * https://unisave.cloud/docs/authentication
 *
 * If you don't need to register players, remove this class.
 */

public class PlayerEntity : Entity
{
    // Add authentication via email:
    // https://unisave.cloud/docs/email-authentication
    //
    [Fillable]
    public string email;
    [DontLeaveServer]
    public string password;
    public DateTime lastLoginAt = DateTime.UtcNow;
    //

    // Token for persistent login
    [DontLeaveServer]
    public string token;
    [DontLeaveServer]
    public DateTime tokenExpiration;
     
    /// <summary>
    /// UUID
    /// </summary>
    [Fillable]
    public string activeExperiment;

    /// <summary>
    /// UUID -> ProbeInsertion
    /// Note that this key list is also the key list that handles 
    /// </summary>
    [Fillable]
    public Dictionary<string, ServerProbeInsertion> UUID2InsertionData;

    /// <summary>
    /// UUID -> List of strings
    /// </summary>
    [Fillable]
    public Dictionary<string, HashSet<string>> UUID2Experiment;

    /// <summary>
    /// string -> List of UUIDs
    /// </summary>
    [Fillable]
    public Dictionary<string, HashSet<string>> Experiment2UUID;

    [Fillable]
    public List<int> VisibleRigParts;

    public PlayerEntity()
    {
        UUID2InsertionData = new Dictionary<string, ServerProbeInsertion>();
        UUID2Experiment = new Dictionary<string, HashSet<string>>();
        Experiment2UUID = new Dictionary<string, HashSet<string>>();

        VisibleRigParts = new List<int>();
    }
}

[Serializable]
public class ServerProbeInsertion
{
    public string name;
    public float ap;
    public float ml;
    public float dv;
    public float phi;
    public float theta;
    public float spin;
    public int probeType;
    public string coordinateSpaceName;
    public string coordinateTransformName;
    public bool active;
    public bool recorded;
    public string UUID;
    public float[] color;
    
    public ServerProbeInsertion() { }

    public ServerProbeInsertion(string name, float ap, float ml, float dv, float phi, float theta, float spin,
        int probeType, string coordinateSpaceName, string coordinateTransformName,
        bool active, bool recorded, string UUID,
        float[] color)
    {
        this.name = name;
        this.ap = ap;
        this.ml = ml;
        this.dv = dv;
        this.phi = phi;
        this.theta = theta;
        this.spin = spin;
        this.probeType = probeType;
        this.coordinateSpaceName = coordinateSpaceName;
        this.coordinateTransformName = coordinateTransformName;
        this.active = active;
        this.recorded = recorded;
        this.UUID = UUID;
        this.color = color;
    }
}