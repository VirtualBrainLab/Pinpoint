using System;
using LightJson;
using LightJson.Serialization;
using Unisave.Serialization;
using UnityEngine;

namespace Unisave.Foundation
{
	/// <summary>
	/// Holds all preferences of Unisave
	/// </summary>
	public class UnisavePreferences : ScriptableObject
	{
		/// <summary>
		/// Name of the preferences asset inside a Resources folder (without extension)
		/// </summary>
		public const string PreferencesFileName = "UnisavePreferencesFile";

		/// <summary>
		/// Loads preferences from the file
		/// </summary>
		public static UnisavePreferences LoadOrCreate()
		{
			// try to load
			var preferences = Resources.Load<UnisavePreferences>(PreferencesFileName);

			// load failed, create them instead (if inside editor)
			if (preferences == null)
			{
				#if UNITY_EDITOR
					var path = "Assets/Unisave/Resources/" + PreferencesFileName + ".asset";

					preferences = ScriptableObject.CreateInstance<UnisavePreferences>();
					UnityEditor.AssetDatabase.CreateAsset(preferences, path);
					UnityEditor.AssetDatabase.SaveAssets();
					UnityEditor.AssetDatabase.Refresh();
				#else
					throw new InvalidOperationException(
						"Unisave preferences have not been found. " +
						"Make sure you configure unisave before building your game."
					);
				#endif
			}

			return preferences;
		}

		/// <summary>
		/// Saves preferences. Callable only from inside the editor
		/// </summary>
		public void Save()
		{
			#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(this);
				UnityEditor.AssetDatabase.SaveAssets();
				UnityEditor.AssetDatabase.Refresh();

				// note that editor key is saved continuously
			#else
				throw new InvalidOperationException(
					"You can save Unsiave preferences only when running inside the editor."
				);
			#endif
		}

		/////////////////
		// Preferences //
		/////////////////
		
		/// <summary>
		/// URL of the Unisave server
		/// </summary>
		public string ServerUrl
		{
			get => serverUrl;
			set => serverUrl = value;
		}
		
		[SerializeField]
		private string serverUrl = "https://unisave.cloud/";

		/// <summary>
		/// Token that uniquely identifies your game
		/// </summary>
		public string GameToken
		{
			get => gameToken;
			set => gameToken = value;
		}

		[SerializeField]
		private string gameToken;

		/// <summary>
		/// Returns a suffix for keys stored inside editor prefs
		/// That makes sure editor prefs are not shared between projects
		/// </summary>
		private string KeySuffix => gameToken ?? "null";

		/// <summary>
		/// Authentication key for Unity editor. This is to make sure noone else
		/// who knows the game token can mess with your game.
		/// 
		/// The editor is actually not stored inside preferences file, but in EditorPrefs
		/// instead to prevent accidental leakage by releasing your game.
		/// </summary>
		public string EditorKey
		{
			get
			{
				#if UNITY_EDITOR
					if (!editorKeyCacheActive)
					{
						editorKeyCache = UnityEditor.EditorPrefs.GetString(
							"unisave.editorKey:" + KeySuffix, null
						);
						editorKeyCacheActive = true;
					}
					return editorKeyCache;
				#else
					// NOTE: It's important that no exception is thrown,
					// since the editor key may be requested in non-editor
					// contexts (e.g. facet call).
					return null;
				#endif
			}

			set
			{
				#if UNITY_EDITOR
					if (value == EditorKey)
						return;

					UnityEditor.EditorPrefs.SetString(
						"unisave.editorKey:" + KeySuffix, value
					);
					
					editorKeyCache = value;
					editorKeyCacheActive = true;
				#else
					throw new InvalidOperationException(
						"You cannot access editor key during runtime."
					);
				#endif
			}
		}
		
		#if UNITY_EDITOR
			
			[NonSerialized]
			private string editorKeyCache;

			[NonSerialized]
			private bool editorKeyCacheActive;
		
		#endif

		/// <summary>
		/// Path (relative to the assets folder) to directory that contains
		/// backend related files, like facets, entities and config
		/// 
		/// Contents of this folder are uploaded to the server
		/// </summary>
		public string BackendFolder
		{
			get => backendFolder;
			set => backendFolder = value;
		}

		[SerializeField]
		private string backendFolder = "Backend";

		/// <summary>
		/// Upload backend automatically after compilation finishes
		/// </summary>
		public bool AutomaticBackendUploading
		{
			get => automaticBackendUploading;
			set => automaticBackendUploading = value;
		}

		[SerializeField]
		private bool automaticBackendUploading = true;

		/// <summary>
		/// Last time backend uploading took place
		/// </summary>
		public DateTime? LastBackendUploadAt
		{
			get
			{
				#if UNITY_EDITOR
					var data = UnityEditor.EditorPrefs.GetString(
						"unisave.lastBackendUploadAt:" + KeySuffix, null
					);

					if (String.IsNullOrEmpty(data))
						return null;

					return Serializer.FromJsonString<DateTime>(data);
				#else
					return null;
				#endif
			}

			set
			{
				#if UNITY_EDITOR
					UnityEditor.EditorPrefs.SetString(
						"unisave.lastBackendUploadAt:" + KeySuffix,
						Serializer.ToJson(value).ToString()
					);
				#endif
			}
		}
		
		/// <summary>
		/// Hash of the backend folder
		/// Important, it's used to identify clients
		/// </summary>
		public string BackendHash
		{
			get => backendHash;
			set => backendHash = value;
		}

		[SerializeField]
		private string backendHash;
		
		/// <summary>
		/// Last backend hash that has been uploaded
		/// </summary>
		public string LastUploadedBackendHash
		{
			get
			{
				#if UNITY_EDITOR
					return UnityEditor.EditorPrefs.GetString(
						"unisave.lastUploadedBackendHash:" + KeySuffix, null
					);
				#else
					return null;
				#endif
			}

			set
			{
				#if UNITY_EDITOR
					UnityEditor.EditorPrefs.SetString(
						"unisave.lastUploadedBackendHash:" + KeySuffix,
						value
					);
				#endif
			}
		}
		
		/// <summary>
		/// File containing development env configuration
		/// </summary>
		public TextAsset DevelopmentEnv
		{
			get => developmentEnv;
			set => developmentEnv = value;
		}

		[SerializeField]
		private TextAsset developmentEnv;
		
		/// <summary>
		/// File containing testing env configuration
		/// </summary>
		public TextAsset TestingEnv
		{
			get => testingEnv;
			set => testingEnv = value;
		}

		[SerializeField]
		private TextAsset testingEnv;
	}
}
