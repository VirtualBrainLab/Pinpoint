using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System.Linq;
#endif

public class ColliderManager : MonoBehaviour
{
    #region Static instances

    public static HashSet<Collider> ActiveColliderInstances = new();
    public static HashSet<Collider> InactiveColliderInstances = new();

    private static GameObject CollisionPanelGO;
    private static Material CollisionMaterial;

    private static HashSet<Collider> VisibleProbeColliders = new();
    private static Dictionary<GameObject, Material> VisibleRigGOs = new();

    #endregion

    [SerializeField] private GameObject _collisionPanelGO;
    [SerializeField] private Material _collisionMaterial;

    private void Awake()
    {
        CollisionPanelGO = _collisionPanelGO;
        CollisionMaterial = _collisionMaterial;
    }

    public static void SetCollisionPanelVisibility(bool visible)
    {
        CollisionPanelGO.SetActive(visible);
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

    public static void RemoveProbeColliderInstances(IEnumerable<Collider> colliders)
    {
        foreach (Collider c in colliders)
            ActiveColliderInstances.Remove(c);
        foreach (Collider c in colliders)
            InactiveColliderInstances.Remove(c);
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

    public static void CheckForCollisions()
    {
        if (PlayerPrefs.GetCollisions())
        {
            bool collided = CheckCollisionsHelper();

            if (collided)
                SetCollisionPanelVisibility(true);
            else
            {
                SetCollisionPanelVisibility(false);
                ClearCollisionMesh();
            }
        }
        else
        {
            SetCollisionPanelVisibility(false);
            ClearCollisionMesh();
        }
    }

    /// <summary>
    /// Internal function to perform collision checks between Collider components
    /// </summary>
    /// <param name="otherColliders"></param>
    /// <returns></returns>
    private static bool CheckCollisionsHelper()
    {
        foreach (Collider activeCollider in ActiveColliderInstances)
        {
            foreach (Collider otherCollider in InactiveColliderInstances)
            {
                if (otherCollider != null)
                {
                    Vector3 dir;
                    float dist;
                    if (Physics.ComputePenetration(activeCollider, activeCollider.transform.position, activeCollider.transform.rotation, otherCollider, otherCollider.transform.position, otherCollider.transform.rotation, out dir, out dist))
                    {
                        CreateCollisionMesh(activeCollider, otherCollider);
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// When collisions occur we want to make the colliders we hit change material, but we might need to later swap them back
    /// </summary>
    /// <param name="activeCollider"></param>
    /// <param name="otherCollider"></param>
    private static void CreateCollisionMesh(Collider activeCollider, Collider otherCollider)
    {
        if (!VisibleProbeColliders.Contains(activeCollider))
        {
            VisibleProbeColliders.Add(activeCollider);
            activeCollider.gameObject.GetComponent<Renderer>().enabled = true;
        }

        GameObject otherColliderGO = otherCollider.gameObject;
        if (!VisibleRigGOs.ContainsKey(otherColliderGO))
        {
            VisibleRigGOs.Add(otherColliderGO, otherColliderGO.GetComponent<Renderer>().material);
            otherColliderGO.GetComponent<Renderer>().material = CollisionMaterial;
        }
    }

    // Clear probe colliders by disabling the renderers and then clear the other colliders by swapping back their materials
    private static void ClearCollisionMesh()
    {
        if (VisibleProbeColliders.Count > 0 || VisibleRigGOs.Count > 0)
        {
            foreach (Collider probeCollider in VisibleProbeColliders)
                probeCollider.gameObject.GetComponent<Renderer>().enabled = false;
            foreach (KeyValuePair<GameObject, Material> kvp in VisibleRigGOs)
                kvp.Key.GetComponent<Renderer>().material = kvp.Value;

            VisibleProbeColliders.Clear();
            VisibleRigGOs.Clear();
        }
    }
}
