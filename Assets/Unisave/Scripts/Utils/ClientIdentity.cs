using UnityEngine;

namespace Unisave.Utils
{
    /// <summary>
    /// Utility functions for getting the game client identity
    /// </summary>
    public static class ClientIdentity
    {
        public static string BuildGuid
        {
            get
            {
                // make sure BuildGUID is always null in the editor,
                // not matter the Unity version
                if (Application.isEditor)
                    return null;

                return Application.buildGUID;
            }
        }
    }
}