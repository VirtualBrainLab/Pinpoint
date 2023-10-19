using BrainAtlas;
using BrainAtlas.CoordinateSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReferenceCoordBehavior : MonoBehaviour
{
    [SerializeField] private LineRenderer _apLine;
    [SerializeField] private LineRenderer _mlLine;
    [SerializeField] private LineRenderer _dvLine;

    private bool _started;

    private void Start()
    {
        _started = true;
    }

    private void OnEnable()
    {
        if (_started)
        {
            UpdateReferenceCoordinate();
            UpdateAxisDirections();
        }
    }

    public void UpdateReferenceCoordinate()
    {
        CoordinateSpace atlasSpace = BrainAtlasManager.ActiveReferenceAtlas.AtlasSpace;
        SetReferenceCoordinate(atlasSpace.Space2World(Vector3.zero));
        UpdateAxisDirections();
    }

    private void SetReferenceCoordinate(Vector3 refWorldU)
    {
        transform.position = refWorldU;
    }

    public void UpdateAxisDirections()
    {
        Vector3 apDir = BrainAtlasManager.T2World_Vector(Vector3.right);
        Vector3 mlDir = BrainAtlasManager.T2World_Vector(Vector3.up);
        Vector3 dvDir = BrainAtlasManager.T2World_Vector(Vector3.forward);
        SetAxisDirections(apDir, mlDir, dvDir);
    }

    private void SetAxisDirections(Vector3 apDir, Vector3 mlDir, Vector3 dvDir)
    {
        _apLine.SetPosition(0, transform.position);
        _mlLine.SetPosition(0, transform.position);
        _dvLine.SetPosition(0, transform.position);
        _apLine.SetPosition(1, transform.position + apDir);
        _mlLine.SetPosition(1, transform.position + mlDir);
        _dvLine.SetPosition(1, transform.position + dvDir);
    }
}
