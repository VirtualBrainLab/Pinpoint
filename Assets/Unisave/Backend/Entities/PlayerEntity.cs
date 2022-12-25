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

    [Fillable]
    public string activeExperiment;

    [Fillable]
    public Dictionary<string, Dictionary<string, ServerProbeInsertion>> experiments;

    [Fillable]
    public List<int> visibleRigParts;

    public PlayerEntity()
    {
        experiments = new Dictionary<string, Dictionary<string, ServerProbeInsertion>>();
        visibleRigParts = new List<int>();
    }
}

public class ServerProbeInsertion
{
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

    public ServerProbeInsertion() { }

    public ServerProbeInsertion(float ap, float ml, float dv, float phi, float theta, float spin, int probeType, string coordinateSpaceName, string coordinateTransformName, bool active, bool recorded, string UUID)
    {
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
    }
}