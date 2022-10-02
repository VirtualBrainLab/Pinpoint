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
    public string email;
    [DontLeaveServer]
    public string password;
    public DateTime lastLoginAt = DateTime.UtcNow;
    //

    public string activeExperiment;

    public Dictionary<string, List<ServerProbeInsertion>> experiments;

    public PlayerEntity()
    {
        experiments = new Dictionary<string, List<ServerProbeInsertion>>();
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
    public float probeType;
    public string coordinateSpaceName;
    public string coordinateTransformName;
    public bool active;
    public bool enabled;
    public bool recorded;
}