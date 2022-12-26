using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System.Linq;
#endif

public class ColliderManager : MonoBehaviour
{
    #region Static instances

    public static HashSet<Collider> ActiveColliderInstances = new HashSet<Collider>();
    public static HashSet<Collider> InactiveColliderInstances = new HashSet<Collider>();

    #endregion

    [SerializeField] public GameObject collisionPanelGO;

    public void SetCollisionPanelVisibility(bool visible)
    {
        collisionPanelGO.SetActive(visible);
    }

    public static void AddProbeColliderInstances(IEnumerable<Collider> colliders, bool active = true)
    {
#if UNITY_EDITOR
        Debug.Log($"Adding {colliders.Count()} colliders to {(active ? "active" : "inactive")} set");
#endif
        if (active)
            foreach (Collider c in colliders)
                InactiveColliderInstances.Remove(c);
        else
            foreach (Collider c in colliders)
                ActiveColliderInstances.Remove(c);

        if (active)
            ActiveColliderInstances.UnionWith(colliders);
        else
            InactiveColliderInstances.UnionWith(colliders);
    }

    public static void AddRigColliderInstances(IEnumerable<Collider> colliders)
    {
#if UNITY_EDITOR
        Debug.Log($"Adding {colliders.Count()} rig colliders");
#endif
        InactiveColliderInstances.UnionWith(colliders);
    }

    public static void RemoveRigColliderInstances(IEnumerable<Collider> colliders)
    {
#if UNITY_EDITOR
        Debug.Log($"Removing {colliders.Count()} rig colliders");
#endif
        foreach (Collider c in colliders)
            InactiveColliderInstances.Remove(c);
    }
}
