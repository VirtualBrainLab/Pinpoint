using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unisave;
using Unisave.Facades;
using Unisave.Foundation;

namespace Unisave.Components
{
    [ExecuteInEditMode]
    public class OverrideUnisavePreferences : MonoBehaviour
    {
        /*
            Allows example scenes to be isolated from the user's game.
            There should be one instance of this component in each example scene
            and it should point to the same overriding preferences file.

            Use with care! When using different overriding files in successive scenes,
            you might get into trouble because different databases have different players,
            but overriding preferences file does not refresh the authenticators.

            Just remember that this tool is sharp and can damage you unexpectedly if used badly.
         */

        public UnisavePreferences preferences;

        void Awake()
        {
            ClientFacade.SetNewFromPreferences(preferences);
        }

        // used if awake is not called
        // e.g. after compilation
        // UnisaveServer makes sure no duplicate overriding takes place
        void OnEnable()
        {
            ClientFacade.SetNewFromPreferences(preferences);
        }

        void OnDisable()
        {
            ClientFacade.SetApplication(null);
        }

        void OnDestroy()
        {
            ClientFacade.SetApplication(null);
        }
    }
}