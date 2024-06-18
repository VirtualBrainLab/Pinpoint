using System;
[Serializable]
public struct PinpointIdResponse
{
    public string PinpointId;
    public bool IsRequester;

    public PinpointIdResponse(string pinpointId, bool isRequester)
    {
        PinpointId = pinpointId;
        IsRequester = isRequester;
    }
}

