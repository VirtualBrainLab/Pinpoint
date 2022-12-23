using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderManager : MonoBehaviour
{
    #region Static instances

    public static HashSet<Collider> ProbeColliderInstances = new HashSet<Collider>();
    public static HashSet<Collider> RigColliderInstances = new HashSet<Collider>();
    public static HashSet<Collider> InactiveColliderInstances = new HashSet<Collider>();

    #endregion

    [SerializeField] private static GameObject collisionPanelGO;

    public static void SetCollisionPanelVisibility(bool visible)
    {
        collisionPanelGO.SetActive(visible);
    }

    public static void AddProbeColliderInstances(IEnumerable<Collider> colliders, bool active = true)
    {
        if (active)
            ProbeColliderInstances.UnionWith(colliders);
        else
            InactiveColliderInstances.UnionWith(colliders);
    }

    public static void AddRigColliderInstances(IEnumerable<Collider> colliders, bool active = true)
    {
        if (active)
            RigColliderInstances.UnionWith(colliders);
        else
            InactiveColliderInstances.UnionWith(colliders);
    }

    public static void RemoveRigColliderInstances(IEnumerable<Collider> colliders)
    {
        foreach (Collider c in colliders)
        {
            RigColliderInstances.Remove(c);
            InactiveColliderInstances.Remove(c);
        }
    }
    
    public static void MoveColliderInstances(IEnumerable<Collider> colliders, bool active)
    {
        if (active)
        {
            foreach (Collider c in colliders)
            {
                ProbeColliderInstances.Remove(c);
            }
            InactiveColliderInstances.UnionWith(colliders);
        }
        else
        {
            foreach (Collider c in colliders)
            {
                InactiveColliderInstances.Remove(c);
            }
            ProbeColliderInstances.UnionWith(colliders);
        }
    }
}
