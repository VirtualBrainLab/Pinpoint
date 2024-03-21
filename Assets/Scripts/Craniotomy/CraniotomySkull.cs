using UnityEngine;
using UnityEngine.Serialization;

public class CraniotomySkull : MonoBehaviour
{
    [FormerlySerializedAs("skullMeshGO")] [SerializeField] private GameObject _skullMeshGo;

    private Renderer skullRenderer;

    private int _activeCraniotomy = 0;
    private float[] _craniotomySizes = { 1, 0, 0, 0, 0 };
    private Vector3[] _craniotomyPositions = new Vector3[5];

    private void Awake()
    {
        skullRenderer = _skullMeshGo.GetComponent<Renderer>();
        Disable();
    }

    public void Disable()
    {
        for (int i = 0; i < 5; i++)
            if (skullRenderer != null)
                skullRenderer.material.SetFloat(string.Format("_CraniotomySize_{0}", i), -1);
    }

    public void Enable()
    {
        if (skullRenderer != null)
        {
            for (int i = 0; i < 5; i++)
                skullRenderer.material.SetFloat(string.Format("_CraniotomySize_{0}", i), _craniotomySizes[i] / 2);
        }
    }

    private void UpdateVisibility()
    {
        if (skullRenderer != null)
        {
            Vector3 xyz = _craniotomyPositions[_activeCraniotomy];
            skullRenderer.material.SetVector(string.Format("_CraniotomyPosition_{0}", _activeCraniotomy), new Vector2(xyz.x, xyz.z));
            skullRenderer.material.SetFloat(string.Format("_CraniotomySize_{0}", _activeCraniotomy), _craniotomySizes[_activeCraniotomy] / 2);
        }
    }

    public void SetActiveCraniotomy(int activeCraniotomy)
    {
        _activeCraniotomy = activeCraniotomy;
        UpdateVisibility();
    }

    public void SetCraniotomyPosition(Vector3 xyz)
    {
        _craniotomyPositions[_activeCraniotomy] = xyz;
        UpdateVisibility();
    }

    public Vector3 GetCraniotomyPosition()
    {
        return _craniotomyPositions[_activeCraniotomy];
    }

    public float GetCraniotomySize()
    {
        return _craniotomySizes[_activeCraniotomy];
    }

    public void SetCraniotomySize(float sizeUM)
    {
        _craniotomySizes[_activeCraniotomy] = sizeUM;
        UpdateVisibility();
    }
}
