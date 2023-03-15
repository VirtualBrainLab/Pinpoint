using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class AddressablesRemoteLoader : MonoBehaviour
{
    [SerializeField] private string addressablesStorageRemotePath = "https://data.virtualbrainlab.org/AddressablesStorage";
    [SerializeField] private string buildVersion = "0.2.1";

    private string fileEnding = ".json";
    private string addressablesStorageTargetPath;

    // Server setup task
    private TaskCompletionSource<bool> catalogLoadedSource;

    // Catalog load task
    private static Task catalogLoadedTask;

    // Delaying the load allows you to set the catalog address
    [SerializeField] private bool delayCatalogLoad = false;

    // Start is called before the first frame update
    void Awake()
    {
        //Register to override WebRequests Addressables creates to download
        Addressables.WebRequestOverride = EditWebRequestURL;

        catalogLoadedSource = new TaskCompletionSource<bool>();
        catalogLoadedTask = catalogLoadedSource.Task;

        if (!delayCatalogLoad) {
            LoadCatalog();
        }
    }

    //Override the url of the WebRequest, the request passed to the method is what would be used as standard by Addressables.
    private void EditWebRequestURL(UnityWebRequest request)
    {
        if (request.url.Contains("http://"))
            request.url = request.url.Replace("http://", "https://");
#if UNITY_EDITOR
        Debug.Log(request.url);
#endif
    }

    public void ChangeCatalogServer(string newAddressablesStorageRemotePath) {
        addressablesStorageRemotePath = newAddressablesStorageRemotePath;
    }

    public async void LoadCatalog() {
        RuntimePlatform platform = Application.platform;
        if (platform == RuntimePlatform.WindowsPlayer || platform == RuntimePlatform.WindowsEditor)
            addressablesStorageTargetPath = addressablesStorageRemotePath + "/StandaloneWindows64/catalog_" + buildVersion + fileEnding;
        else if (platform == RuntimePlatform.WebGLPlayer)
            addressablesStorageTargetPath = addressablesStorageRemotePath + "/WebGL/catalog_" + buildVersion + fileEnding;
        else if (platform == RuntimePlatform.OSXEditor || platform == RuntimePlatform.OSXPlayer)
            addressablesStorageTargetPath = addressablesStorageRemotePath + "/StandaloneOSX/catalog_" + buildVersion + fileEnding;
        else {
            Debug.LogError(string.Format("Running on {0} we do NOT have a built Addressables Storage bundle",platform));
        }

#if UNITY_EDITOR
        Debug.Log("(AddressablesStorage) Loading catalog v" + buildVersion);
#endif
        //Load a catalog and automatically release the operation handle.
        Debug.Log("(AddressablesStorage) Loading content catalog from: " + GetAddressablesPath());

        AsyncOperationHandle<IResourceLocator> catalogLoadHandle
            = Addressables.LoadContentCatalogAsync(GetAddressablesPath(), true);

        await catalogLoadHandle.Task;

        catalogLoadedSource.SetResult(true);
    }

    public Task GetCatalogLoadedTask() {
        return catalogLoadedTask;
    }

    public string GetAddressablesPath()
    {
        return addressablesStorageTargetPath;
    }

    public static async Task<Mesh> LoadCCFMesh(string objPath)
    {
#if UNITY_EDITOR
        Debug.Log("Loading mesh file: " + objPath);
#endif

        // Wait for the catalog to load if this hasn't already happened
        await catalogLoadedTask;


        // Catalog is loaded, load specified mesh file
        string path = "Assets/AddressableAssets/AllenCCF/" + objPath;
        // Not sure why this extra path check is here, I think maybe some objects don't exist and so this hangs indefinitely for those?
        AsyncOperationHandle<IList<IResourceLocation>> pathHandle = Addressables.LoadResourceLocationsAsync(path);
        await pathHandle.Task;

        AsyncOperationHandle<Mesh> loadHandle = Addressables.LoadAssetAsync<Mesh>(path);
        await loadHandle.Task;

        // Copy the mesh so that we can modify it without modifying the original
        Mesh returnMesh = new Mesh();
        returnMesh.vertices = loadHandle.Result.vertices;
        returnMesh.triangles = loadHandle.Result.triangles;
        returnMesh.uv = loadHandle.Result.uv;
        returnMesh.normals = loadHandle.Result.normals;
        returnMesh.colors = loadHandle.Result.colors;
        returnMesh.tangents = loadHandle.Result.tangents;

        Addressables.Release(pathHandle);
        Addressables.Release(loadHandle);

        return returnMesh;
    }

    public static async Task<string> LoadAllenCCFOntology()
    {
#if UNITY_EDITOR
        Debug.Log("Loading Allen CCF");
#endif

        await catalogLoadedTask;

        string path = "Assets/AddressableAssets/AllenCCF/ontology_structure_minimal.csv";

        AsyncOperationHandle loadHandle = Addressables.LoadAssetAsync<TextAsset>(path);
        await loadHandle.Task;

        string returnText = ((TextAsset)loadHandle.Result).text;
        Addressables.Release(loadHandle);

        return returnText;
    }

    public static async Task<Texture3D> LoadAnnotationTexture()
    {
#if UNITY_EDITOR
        Debug.Log("Loading Allen CCF annotation texture");
#endif

        // Wait for the catalog to load if this hasn't already happened
        await catalogLoadedTask;

        // Catalog is loaded, load the Texture3D object
        string path = "Assets/AddressableAssets/Textures/AnnotationDatasetTexture3DAlpha.asset";

        AsyncOperationHandle loadHandle = Addressables.LoadAssetAsync<Texture3D>(path);
        await loadHandle.Task;

        Texture3D returnTexture = (Texture3D)loadHandle.Result;
        //Addressables.Release(loadHandle);

        return returnTexture;
    }

    public static async Task<byte[]> LoadVolumeIndexes()
    {
#if UNITY_EDITOR
        Debug.Log("Loading volume indexes");
#endif

        // Wait for the catalog to load if this hasn't already happened
        await catalogLoadedTask;

        string volumePath = "Assets/AddressableAssets/Datasets/volume_indexes.bytes";
        
        AsyncOperationHandle loadHandle = Addressables.LoadAssetAsync<TextAsset>(volumePath);
        await loadHandle.Task;

        byte[] resultText = ((TextAsset)loadHandle.Result).bytes;
        Addressables.Release(loadHandle);

        return resultText;
    }

    /// <summary>
    /// Loads the annotation data to be reconstructed by the VolumeDatasetManager
    /// </summary>
    /// <returns>List of TextAssets where [0] is the index and [1] is the map</returns>
    public static async Task<(byte[] index, byte[] map)> LoadAnnotationIndexMap()
    {
#if UNITY_EDITOR
        Debug.Log("Loading annotation index mapping");
#endif

        // Wait for the catalog to load if this hasn't already happened
        await catalogLoadedTask;

        string annIndexPath = "Assets/AddressableAssets/Datasets/ann/annotation_indexes.bytes";
        AsyncOperationHandle indexHandle = Addressables.LoadAssetAsync<TextAsset>(annIndexPath);
        await indexHandle.Task;

        string annMapPath = "Assets/AddressableAssets/Datasets/ann/annotation_map.bytes";
        AsyncOperationHandle mapHandle = Addressables.LoadAssetAsync<TextAsset>(annMapPath);
        await mapHandle.Task;

        (byte[] index, byte[] map) data = (((TextAsset)indexHandle.Result).bytes, ((TextAsset)mapHandle.Result).bytes );
        Addressables.Release(indexHandle);
        Addressables.Release(mapHandle);

        return data;
    }
}
