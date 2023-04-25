using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class VolumeDatasetManager : MonoBehaviour
{
    public static CCFAnnotationDataset AnnotationDataset;
    public static Texture3D AnnotationDatasetTexture3D;

    [SerializeField] private TP_Utils utils;
    [SerializeField] private TMP_InputField coverageURL;
    
    // Annotations
    private byte[] datasetIndexes_bytes;
    private ushort[] annotationIndexes_shorts;
    private uint[] annotationMap_ints;

    private static TaskCompletionSource<bool> _texture3DLoadedSource;
    private void Awake()
    {
        _texture3DLoadedSource = new TaskCompletionSource<bool>();
    }

    private async void Start()
    {
        Task<Texture3D> textureTask = AddressablesRemoteLoader.LoadAnnotationTexture();
        await textureTask;

        AnnotationDatasetTexture3D = textureTask.Result;
        _texture3DLoadedSource.SetResult(true);

        Debug.Log("(VDManager) Annotation dataset texture loaded");
    }

    public async void DelayedLoadCoverage(bool showCoverage)
    {
        if (showCoverage)
            utils.LoadCoverageData(AnnotationDatasetTexture3D, coverageURL.text);
        else
        {
            Task<Texture3D> textureTask = AddressablesRemoteLoader.LoadAnnotationTexture();
            await textureTask;

            AnnotationDatasetTexture3D = textureTask.Result;
            AnnotationDatasetTexture3D.Apply();
        }
    }

    /// <summary>
    /// Loads the annotation dataset files from their Addressable AssetReference objects
    /// 
    /// Asynchronous dependencies: inPlaneSlice, localPrefs
    /// </summary>
    public async Task<bool> LoadAnnotationDataset()
    {
        bool finished = true;

        Debug.Log("(VDManager) Annotation dataset loading");
        List<Task> dataLoaders = new List<Task>();

        Task<byte[]> dataTask = AddressablesRemoteLoader.LoadVolumeIndexes();
        dataLoaders.Add(dataTask);


        Task<(byte[] index, byte[] map)> annotationTask = AddressablesRemoteLoader.LoadAnnotationIndexMap();
        dataLoaders.Add(annotationTask);

        await Task.WhenAll(dataLoaders);

        // When all loaded, copy the data locally using Buffer.BlockCopy()
        datasetIndexes_bytes = dataTask.Result;

        annotationIndexes_shorts = new ushort[annotationTask.Result.index.Length / 2];
        Buffer.BlockCopy(annotationTask.Result.index, 0, annotationIndexes_shorts, 0, annotationTask.Result.index.Length);

        annotationMap_ints = new uint[annotationTask.Result.map.Length / 4];
        Buffer.BlockCopy(annotationTask.Result.map, 0, annotationMap_ints, 0, annotationTask.Result.map.Length);

        Debug.Log("(VDManager) Annotation dataset files loaded, building dataset");

        AnnotationDataset = new CCFAnnotationDataset((528, 320, 456), annotationIndexes_shorts, annotationMap_ints, datasetIndexes_bytes);
        annotationIndexes_shorts = null;
        annotationMap_ints = null;

        return finished;
    }

    public static Task<bool> Texture3DLoaded()
    { 
        return _texture3DLoadedSource.Task;
    }

}
