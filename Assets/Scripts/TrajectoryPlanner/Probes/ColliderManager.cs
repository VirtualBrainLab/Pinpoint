using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderManager : MonoBehaviour
{
    [SerializeField] private static GameObject collisionPanelGO;

    public static void SetCollisionPanelVisibility(bool visible)
    {
        collisionPanelGO.SetActive(visible);
    }
}
