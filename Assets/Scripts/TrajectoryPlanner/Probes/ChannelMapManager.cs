using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ChannelMapManager : MonoBehaviour
{
    [SerializeField] List<AssetReference> _channelMapAssetRefs;
    [SerializeField] List<string> _channelMapNames;

    private static Dictionary<string, ChannelMap> _channelMaps;

    private void Awake()
    {
        _channelMaps = new();

        if (_channelMapAssetRefs.Count != _channelMapNames.Count)
            throw new System.Exception("(ChannelMapManager) Asset references must match number of names");

        for (int i = 0; i < _channelMapAssetRefs.Count; i++)
        {
            _channelMaps.Add(_channelMapNames[i], new ChannelMap(_channelMapAssetRefs[i]));
        }
    }

    public static ChannelMap GetChannelMap(string channelMapName)
    {
        if (_channelMaps.ContainsKey(channelMapName))
            return _channelMaps[channelMapName];
        else
            throw new System.Exception($"Channel map {channelMapName} does not exist");
    }
}
