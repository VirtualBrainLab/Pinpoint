using System.Threading.Tasks;
using UnityEngine;

public class AddressablesRemoteTest : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        AsyncTest();
    }

    public async void AsyncTest()
    {
        Task<Mesh> handle = AddressablesRemoteLoader.LoadCCFMesh("8.obj");
        await handle;

        Debug.Log("Loaded 8.obj");

        Task<Texture3D> handleTex = AddressablesRemoteLoader.LoadAnnotationTexture();
        await handleTex;

        Debug.Log("Loaded texture");

        Task<byte[]> volumeHandle = AddressablesRemoteLoader.LoadVolumeIndexes();
        await volumeHandle;

        Debug.Log("Loaded volume indices");
    }
}
