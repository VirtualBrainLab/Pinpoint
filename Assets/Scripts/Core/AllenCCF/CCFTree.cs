using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class CCFTree
{
    public CCFTreeNode root;
    private Material brainRegionMaterial;
    private float scale;
    private Dictionary<int, CCFTreeNode> fastSearchDictionary;
    private Transform brainModelParent;

    public CCFTree(int rootID, int atlasID, string rootName, float scale, Color color, Material material)
    {
        brainModelParent = GameObject.Find("BrainAreas").transform;

        this.scale = scale;
        root = new CCFTreeNode(rootID, atlasID, 0, scale, null, rootName, "", color, material, brainModelParent);
        brainRegionMaterial = material;

        fastSearchDictionary = new Dictionary<int, CCFTreeNode>();
        fastSearchDictionary.Add(rootID, root);

    }

    public CCFTreeNode addNode(int parentID, int id, int atlasID, int depth, string name, string acronym, Color color)
    {
        // find the parent ID node
        CCFTreeNode parentNode = findNode(parentID);

        // return if you fail to find it
        if (parentNode==null) {Debug.Log("Can't add new node: parent not found");return null;}

        // add the node if you succeeded
        CCFTreeNode newNode = new CCFTreeNode(id, atlasID, depth, scale, parentNode, name, acronym, color, brainRegionMaterial, brainModelParent);
        parentNode.appendNode(newNode);

        fastSearchDictionary.Add(id, newNode);

        return newNode;
    }

    public int nodeCount()
    {
        return root.nodeCount();
    }

    public CCFTreeNode findNode(int ID)
    {
        if (fastSearchDictionary.ContainsKey(ID))
            return fastSearchDictionary[ID];
        return null;
    }

    [Obsolete("Deprecated in favor of findNode with dictionary")]
    public CCFTreeNode findNodeRecursive(int ID)
    {
        return root.findNode(ID);
    }
}

public class CCFTreeNode
{
    private CCFTreeNode parent;
    private List<CCFTreeNode> childNodes;
    public int ID { get;}
    public int AtlasID { get; }
    public string Name { get; }
    public string ShortName { get; }
    public int Depth { get; }
    private Color _defaultColor;
    private Color _color;
    private float _scale;

    public Color Color { get { return _color; } }
    public Color DefaultColor { get { return _defaultColor; } }

    #region gameobjects
    private GameObject _nodeModelParentGO;
    private GameObject _nodeModelGO;
    private GameObject _nodeModelLeftGO;
    private GameObject _nodeModelRightGO;

    public GameObject NodeModelParentGO { get { return _nodeModelParentGO; } }
    public GameObject NodeModelGO { get { return _nodeModelGO; } }
    public GameObject NodeModelLeftGO { get { return _nodeModelLeftGO; } }
    public GameObject NodeModelRightGO { get { return _nodeModelRightGO; } }
    #endregion

    private Transform _brainModelParent;
    private Material _material;

    private Vector3[] verticesFull;
    private Vector3[] verticesSided;

    private TaskCompletionSource<bool> _loadedSourceFull;
    private TaskCompletionSource<bool> _loadedSourceSeparated;

    public CCFTreeNode(int ID, int atlasID, int depth, float scale, CCFTreeNode parent, string Name, string ShortName, Color color, Material material, Transform brainModelParent)
    {
        this.ID = ID;
        this.AtlasID = atlasID;
        this.Name = Name;
        this.parent = parent;
        this.Depth = depth;
        this._scale = scale;
        this.ShortName = ShortName;
        color.a = 1.0f;
        this._color = color;
        _defaultColor = new Color(color.r, color.g, color.b, color.a);
        this._material = material;
        this._brainModelParent = brainModelParent;
        childNodes = new List<CCFTreeNode>();

        _loadedSourceFull = new TaskCompletionSource<bool>();
        _loadedSourceSeparated = new TaskCompletionSource<bool>();
    }

    public bool IsLoaded(bool full)
    {
        return full ? _loadedSourceFull.Task.IsCompleted : _loadedSourceSeparated.Task.IsCompleted;
    }

    public Task<bool> GetLoadedTask(bool full)
    {
        return full ? _loadedSourceFull.Task : _loadedSourceSeparated.Task;
    }

    public async void LoadNodeModel(bool loadFull, bool loadSeparated)
    {
        _nodeModelParentGO = new GameObject(Name);
        _nodeModelParentGO.transform.parent = _brainModelParent;
        _nodeModelParentGO.transform.localPosition = Vector3.zero;
        _nodeModelParentGO.transform.localRotation = Quaternion.identity;

        if (loadFull)
        {
            string path = ID + ".obj";
            Task<Mesh> meshTask = AddressablesRemoteLoader.LoadCCFMesh(path);
            await meshTask;

            _nodeModelGO = new GameObject(Name);
            _nodeModelGO.transform.SetParent(_nodeModelParentGO.transform);
            _nodeModelGO.transform.localScale = new Vector3(_scale, _scale, _scale);
            _nodeModelGO.AddComponent<MeshFilter>();
            _nodeModelGO.AddComponent<MeshRenderer>();
            _nodeModelGO.layer = 13;
            _nodeModelGO.tag = "BrainRegion";
            Renderer rend = _nodeModelGO.GetComponent<Renderer>();
            rend.material = _material;
            rend.material.SetColor("_Color", _color);
            rend.receiveShadows = false;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _nodeModelGO.GetComponent<MeshFilter>().mesh = meshTask.Result;

            _nodeModelGO.transform.localPosition = Vector3.zero;
            _nodeModelGO.transform.localRotation = Quaternion.identity;
            _nodeModelGO.SetActive(false);

            _loadedSourceFull.SetResult(true);
        }

        if (loadSeparated)
        {
            string path = ID + "L.obj";
            Task<Mesh> meshTask = AddressablesRemoteLoader.LoadCCFMesh(path);
            await meshTask;

            // Create the left/right meshes
            _nodeModelLeftGO = new GameObject(Name + "_L");
            _nodeModelLeftGO.transform.SetParent(_nodeModelParentGO.transform);
            _nodeModelLeftGO.transform.localScale = new Vector3(_scale, _scale, _scale);
            _nodeModelLeftGO.AddComponent<MeshFilter>();
            _nodeModelLeftGO.AddComponent<MeshRenderer>();
            _nodeModelLeftGO.layer = 13;
            _nodeModelLeftGO.tag = "BrainRegion";
            Renderer leftRend = _nodeModelLeftGO.GetComponent<Renderer>();
            leftRend.material = _material;
            leftRend.material.SetColor("_Color", _color);
            leftRend.receiveShadows = false;
            leftRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _nodeModelLeftGO.GetComponent<MeshFilter>().mesh = meshTask.Result;
            _nodeModelLeftGO.AddComponent<MeshCollider>();

            _nodeModelLeftGO.transform.localPosition = Vector3.zero;
            _nodeModelLeftGO.transform.localRotation = Quaternion.identity;
            _nodeModelLeftGO.SetActive(false);

            // Create the right meshes
            _nodeModelRightGO = new GameObject(Name + "_R");
            _nodeModelRightGO.transform.SetParent(_nodeModelParentGO.transform);
            _nodeModelRightGO.transform.localScale = new Vector3(_scale, _scale, -_scale);
            _nodeModelRightGO.AddComponent<MeshFilter>();
            _nodeModelRightGO.AddComponent<MeshRenderer>();
            _nodeModelRightGO.layer = 13;
            _nodeModelRightGO.tag = "BrainRegion";
            Renderer rightRend = _nodeModelRightGO.GetComponent<Renderer>();
            rightRend.material = _material;
            rightRend.material.SetColor("_Color", _color);
            rightRend.receiveShadows = false;
            rightRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _nodeModelRightGO.GetComponent<MeshFilter>().mesh = meshTask.Result;

            _nodeModelRightGO.transform.localPosition = Vector3.zero;
            _nodeModelRightGO.transform.localRotation = Quaternion.identity;
            _nodeModelRightGO.SetActive(false);

            _loadedSourceSeparated.SetResult(true);
        }

#if UNITY_EDITOR
        Debug.Log("Node: " + ID + " finished loading");
#endif
    }

    public List<CCFTreeNode> GetChildren()
    {
        return childNodes;
    }

    public void ResetColor()
    {
        SetColor(_defaultColor, true);
    }

    public void SetColor(Color newColor, bool saveColor = true)
    {
        if (saveColor)
            _color = newColor;

        if (_nodeModelGO != null)
            _nodeModelGO.GetComponent<Renderer>().material.SetColor("_Color", newColor);

        if (_nodeModelLeftGO != null)
        {
            _nodeModelLeftGO.GetComponent<Renderer>().material.SetColor("_Color", newColor);
            _nodeModelRightGO.GetComponent<Renderer>().material.SetColor("_Color", newColor);
        }
    }

    public void SetColorOneSided(Color newColor, bool leftSide, bool saveColor = true)
    {
        if (saveColor)
            _color = newColor;

        if (_nodeModelLeftGO != null)
        {
            if (leftSide)
                _nodeModelLeftGO.GetComponent<Renderer>().material.SetColor("_Color", newColor);
            else
                _nodeModelRightGO.GetComponent<Renderer>().material.SetColor("_Color", newColor);
        }
        else
            Debug.LogError("Model must be loaded before rendering");
    }

    public void SetMaterial(Material newMaterial)
    {
        _material = newMaterial;

        if (_nodeModelGO != null)
            _nodeModelGO.GetComponent<Renderer>().material = newMaterial;
        if (_nodeModelLeftGO != null)
        {
            _nodeModelLeftGO.GetComponent<Renderer>().material = newMaterial;
            _nodeModelRightGO.GetComponent<Renderer>().material = newMaterial;
        }

        SetColor(_color, false);
    }
    public void SetMaterialOneSided(Material newMaterial, bool leftSide)
    {
        _material = newMaterial;

        if (_nodeModelLeftGO != null)
        {
            if (leftSide)
                _nodeModelLeftGO.GetComponent<Renderer>().material = newMaterial;
            else
                _nodeModelRightGO.GetComponent<Renderer>().material = newMaterial;
        }
        else
            Debug.LogError("Model must be loaded before rendering");

        SetColorOneSided(_color, leftSide, false);
    }

    public void SetShaderProperty(string property, Vector4 value)
    {
        if (_nodeModelGO != null)
            _nodeModelGO.GetComponent<Renderer>().material.SetVector(property, value);
        
        if (_nodeModelLeftGO != null)
        {
            _nodeModelLeftGO.GetComponent<Renderer>().material.SetVector(property, value);
            _nodeModelRightGO.GetComponent<Renderer>().material.SetVector(property, value);
        }
    }

    public void SetShaderPropertyOneSided(string property, Vector4 value, bool leftSide)
    {
        if (_nodeModelLeftGO != null)
        {
            if (leftSide)
                _nodeModelLeftGO.GetComponent<Renderer>().material.SetVector(property, value);
            else
                _nodeModelRightGO.GetComponent<Renderer>().material.SetVector(property, value);
        }
        else
            Debug.LogError("Model must be loaded before rendering");
    }

    public void SetShaderProperty(string property, float value)
    {
        if (_nodeModelGO != null)
            _nodeModelGO.GetComponent<Renderer>().material.SetFloat(property, value);

        if (_nodeModelLeftGO != null)
        {
            _nodeModelLeftGO.GetComponent<Renderer>().material.SetFloat(property, value);
            _nodeModelRightGO.GetComponent<Renderer>().material.SetFloat(property, value);
        }
    }
    public void SetShaderPropertyOneSided(string property, float value, bool leftSide)
    {
        if (_nodeModelLeftGO != null)
        {
            if (leftSide)
                _nodeModelLeftGO.GetComponent<Renderer>().material.SetFloat(property, value);
            else
                _nodeModelRightGO.GetComponent<Renderer>().material.SetFloat(property, value);
        }
        else
            Debug.LogError("Model must be loaded before rendering");
    }


    public void SetNodeModelVisibility(bool fullVisible = false, bool leftVisible = false, bool rightVisible = false)
    {
        if (_nodeModelGO != null)
            _nodeModelGO.SetActive(fullVisible);

        if (_nodeModelLeftGO != null)
        {
            _nodeModelLeftGO.SetActive(leftVisible);
            _nodeModelRightGO.SetActive(rightVisible);
        }
    }

    public int nodeCount()
    {
        int count = childNodes.Count;
        foreach (CCFTreeNode node in childNodes)
        {
            count += node.nodeCount();
        }
        return count;
    }

    public CCFTreeNode findNode(int ID)
    {
        if (this.ID == ID) { return this; }
        foreach (CCFTreeNode node in childNodes)
        {
            CCFTreeNode found = node.findNode(ID);
            if (found != null) { return found; }
        }
        return null;
    }

    public CCFTreeNode Parent()
    {
        return parent;
    }

    public List<CCFTreeNode> Nodes()
    {
        return childNodes;
    }

    public void appendNode(CCFTreeNode newNode)
    {
        childNodes.Add(newNode);
    }

    public Transform GetNodeTransform()
    {
        return _nodeModelParentGO.transform;
    }

    public Vector3 GetMeshCenterFull()
    {
        return _nodeModelGO.GetComponent<Renderer>().bounds.center;
    }

    public Vector3 GetMeshCenterSided(bool left)
    {
        if (left)
            return _nodeModelLeftGO.GetComponent<Renderer>().bounds.center;
        else
            return _nodeModelRightGO.GetComponent<Renderer>().bounds.center;
    }

    /// <summary>
    /// This may be a very expensive function to run.
    /// </summary>
    /// <param name="transformFunction"></param>
    /// <param name="full"></param>
    public void TransformVertices(Func<Vector3, Vector3> transformFunction, bool full)
    {
        if (full)
        {
            if (verticesFull==null)
                verticesFull = _nodeModelGO.GetComponent<MeshFilter>().mesh.vertices;

            Vector3[] verticesNew = new Vector3[verticesFull.Length];
            for (var i = 0; i < verticesFull.Length; i++)
            {
                // transform to world space
                // transform from world space to *transformed* world space, using the transformFunction we were passed
                // transform back to local space
                verticesNew[i] = _nodeModelGO.transform.InverseTransformPoint(transformFunction(_nodeModelGO.transform.TransformPoint(verticesFull[i])));
            }

            _nodeModelGO.GetComponent<MeshFilter>().mesh.vertices = verticesNew;
        }
        else
        {
            if (verticesSided == null)
                verticesSided = _nodeModelLeftGO.GetComponent<MeshFilter>().mesh.vertices;

            Vector3[] verticesNew = new Vector3[verticesSided.Length];
            for (var i = 0; i < verticesSided.Length; i++)
            {
                verticesNew[i] = _nodeModelLeftGO.transform.InverseTransformPoint(transformFunction(_nodeModelLeftGO.transform.TransformPoint(verticesSided[i])));
            }

            _nodeModelLeftGO.GetComponent<MeshFilter>().mesh.vertices = verticesNew;
            _nodeModelRightGO.GetComponent<MeshFilter>().mesh.vertices = verticesNew;
        }
    }

    public void ClearTransform(bool full)
    {
        if (full)
        {
            _nodeModelGO.GetComponent<MeshFilter>().mesh.vertices = verticesFull;
        }
        else
        {
            _nodeModelLeftGO.GetComponent<MeshFilter>().mesh.vertices = verticesSided;
            _nodeModelRightGO.GetComponent<MeshFilter>().mesh.vertices = verticesSided;
        }
    }
}