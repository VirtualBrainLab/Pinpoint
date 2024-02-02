// #define DEBUG_UNISAVE_PREFERENCES

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LightJson;
using Unisave.Serialization;
using UnityEngine;

namespace Unisave.Foundation
{
	/// <summary>
	/// Holds all preferences of Unisave
	/// </summary>
	public class UnisavePreferences
	{
		#region "Preferences"

		/// <summary>
		/// URL of the Unisave server
		/// </summary>
		public string ServerUrl { get; set; } = "https://unisave.cloud/";

		/// <summary>
		/// Token that uniquely identifies your game
		/// </summary>
		public string GameToken { get; set; } = null;
		
		/// <summary>
		/// Authentication key for Unity editor. This is to make sure noone else
		/// who knows the game token can mess with your game.
		/// 
		/// The editor is actually not stored inside preferences file, but in EditorPrefs
		/// instead to prevent accidental leakage by releasing your game.
		/// </summary>
		public string EditorKey { get; set; } = null;
		
		/// <summary>
		/// Upload backend automatically after compilation finishes
		/// </summary>
		public bool AutomaticBackendUploading { get; set; } = true;

		/// <summary>
		/// Hash of the backend folder
		/// Important, it's used to identify clients
		/// </summary>
		public string BackendHash { get; set; } = null;
		
		/// <summary>
		/// Last time backend uploading took place
		/// </summary>
		public DateTime? LastBackendUploadAt { get; set; }
		
		/// <summary>
		/// Last backend hash that has been uploaded
		/// </summary>
		public string LastUploadedBackendHash { get; set; }

		/// <summary>
		/// Set of backend folder names (definition files),
		/// that are enabled. (but only those that state they let
		/// themselves be controlled by Unisave preferences)
		/// These are typically built-in modules that are disabled
		/// by default, can be enabled, and this setting should survive
		/// Unisave core asset upgrade. So for example,
		/// Heapstore is one such module.
		/// </summary>
		public ISet<string> PreferencesEnabledBackendFolders { get; set; }
			= new SortedSet<string>();
		
		#endregion
		
		#region "Serialization"

		private void SerializeFileFields(JsonObject output)
		{
			// ServerUrl
			output["ServerUrl"] = ServerUrl;
			
			// GameToken
			output["GameToken"] = GameToken;
			
			// AutomaticBackendUploading
			output["AutomaticBackendUploading"] = AutomaticBackendUploading;
			
			// BackendHash
			output["BackendHash"] = BackendHash;
			
			// PreferencesEnabledBackendFolders
			output["PreferencesEnabledBackendFolders"] = new JsonArray(
				PreferencesEnabledBackendFolders
					.Select(x => (JsonValue) x)
					.ToArray()
			);
		}

		private void DeserializeFileFields(JsonObject input)
		{
			// ServerUrl
			if (input.ContainsKey("ServerUrl"))
				ServerUrl = input["ServerUrl"].AsString;
			
			// GameToken
			if (input.ContainsKey("GameToken"))
				GameToken = input["GameToken"].AsString;
			
			// AutomaticBackendUploading
			if (input.ContainsKey("AutomaticBackendUploading"))
				AutomaticBackendUploading = input["AutomaticBackendUploading"].AsBoolean;
			
			// BackendHash
			if (input.ContainsKey("BackendHash"))
				BackendHash = input["BackendHash"].AsString;
			
			// PreferencesEnabledBackendFolders
			if (input.ContainsKey("PreferencesEnabledBackendFolders")
			    && input["PreferencesEnabledBackendFolders"].IsJsonArray)
			{
				PreferencesEnabledBackendFolders = new SortedSet<string>(
					input["PreferencesEnabledBackendFolders"]
						.AsJsonArray
						.Select(x => x.AsString)
				);
			}
		}

		private void SerializeEditorPrefsFields()
		{
			#if UNITY_EDITOR
			
			string keySuffix = GameToken ?? "null";
			
			// EditorKey
			UnityEditor.EditorPrefs.SetString(
				"unisave.editorKey:" + keySuffix,
				EditorKey
			);
			
			// LastBackendUploadAt
			UnityEditor.EditorPrefs.SetString(
				"unisave.lastBackendUploadAt:" + keySuffix,
				Serializer.ToJsonString(LastBackendUploadAt)
			);
			
			// LastUploadedBackendHash
			UnityEditor.EditorPrefs.SetString(
				"unisave.lastUploadedBackendHash:" + keySuffix,
				LastUploadedBackendHash
			);
			
			#endif
		}
		
		private void DeserializeEditorPrefsFields()
		{
			// NOTE: needs to be called AFTER the file fields,
			// since it uses the GameToken field!
			
			#if UNITY_EDITOR
			
			string keySuffix = GameToken ?? "null";
			
			// EditorKey
			EditorKey = UnityEditor.EditorPrefs.GetString(
				"unisave.editorKey:" + keySuffix, null
			);

			// LastBackendUploadAt
			{
				string json = UnityEditor.EditorPrefs.GetString(
					"unisave.lastBackendUploadAt:" + keySuffix, null
				);
				LastBackendUploadAt = string.IsNullOrEmpty(json)
					? (DateTime?) null
					: Serializer.FromJsonString<DateTime>(json);
			}
			
			// LastUploadedBackendHash
			LastUploadedBackendHash = UnityEditor.EditorPrefs.GetString(
				"unisave.lastUploadedBackendHash:" + keySuffix, null
			);
			
			#endif
		}
		
		#endregion
		
		#region "Persistence"
		
		/// <summary>
		/// Name of the preferences asset inside a Resources folder
		/// (without file extension)
		/// </summary>
		public const string PreferencesFileName = "UnisavePreferencesFile";
		
		/// <summary>
		/// Path to the preferences file in the filesystem
		/// (relative to the project root)
		/// </summary>
		public const string PreferencesFilePath
			= "Assets/Resources/" + PreferencesFileName + ".json";

		/// <summary>
		/// Caches the preferences instance returned by the Resolve method.
		/// </summary>
		private static UnisavePreferences cache;

		/// <summary>
		/// Whether the cached preferences are runtime-resolved or editor-resolved
		/// </summary>
		private static bool cachedAreRuntime;
		
		/// <summary>
		/// Loads preferences from the file or creates them if missing.
		/// Preferences are cached so this method can be called fairly often.
		/// </summary>
		public static UnisavePreferences Resolve(bool bustCache = false)
		{
			// bust cache if requested by the user
			if (bustCache)
				cache = null;
			
			// bust cache if incorrect runtime state
			if (cache != null && Application.isPlaying != cachedAreRuntime)
				cache = null;
			
			// return from cache
			if (cache != null)
				return cache;

			// load and store in cache
			cache = Application.isPlaying ? LoadInRuntime() : LoadInEditor();
			cachedAreRuntime = Application.isPlaying;
			return cache;
		}

		/// <summary>
		/// Loads the unisave preferences asset via .NET file interface
		/// in the editor (sidesteps Unity's asset logic)
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		private static UnisavePreferences LoadInEditor()
		{
			#if DEBUG_UNISAVE_PREFERENCES
			Debug.Log($"{nameof(UnisavePreferences)}.{nameof(LoadInEditor)}");
			#endif
			
			if (Application.isPlaying)
				throw new InvalidOperationException(
					$"{nameof(LoadInEditor)} can only be called from editor."
				);
			
			// create the instance that will be populated by deserialization
			var preferences = new UnisavePreferences();

			// if not existing, return the default preferences
			// (effectively create a new instance, but do not save yet)
			if (!File.Exists(PreferencesFilePath))
				return preferences;

			try
			{
				string jsonString = File.ReadAllText(
					PreferencesFilePath,
					Encoding.UTF8
				);
				var json = Serializer.FromJsonString<JsonObject>(jsonString);
				preferences.DeserializeFileFields(json);
				preferences.DeserializeEditorPrefsFields();
				return preferences;
			}
			catch (Exception e)
			{
				Debug.LogError(
					"[Unisave] Loading Unisave Preferences file failed:\n" + e
				);
				return preferences;
			}
		}
		
		/// <summary>
		/// Loads the unisave preferences asset via the Resources at game runtime
		/// </summary>
		private static UnisavePreferences LoadInRuntime()
		{
			#if DEBUG_UNISAVE_PREFERENCES
			Debug.Log($"{nameof(UnisavePreferences)}.{nameof(LoadInRuntime)}");
			#endif
			
			if (!Application.isPlaying)
				throw new InvalidOperationException(
					$"{nameof(LoadInRuntime)} can only be called during runtime."
				);
			
			// create the instance that will be populated by deserialization
			var preferences = new UnisavePreferences();
            
			var asset = Resources.Load<TextAsset>(PreferencesFileName);

			if (asset == null)
			{
				Debug.LogError(
					"[Unisave] Unisave preferences have not been found, " +
					"Unisave will not be able to connect to the cloud."
				);
				return preferences;
			}
			
			try
			{
				var json = Serializer.FromJsonString<JsonObject>(asset.text);
				preferences.DeserializeFileFields(json);
				preferences.DeserializeEditorPrefsFields(); // needed in editor-runtime
				return preferences;
			}
			catch (Exception e)
			{
				Debug.LogError(
					"[Unisave] Loading Unisave Preferences failed:\n" + e
				);
				return preferences;
			}
		}

		/// <summary>
		/// Saves preferences. Should only be called from inside the editor.
		/// </summary>
		/// <param name="ensureInEditor">
		/// When true, the method checks that it's only being executed
		/// from the Unity editor and not the built game.
		/// </param>
		public void Save()
		{
			#if DEBUG_UNISAVE_PREFERENCES
			Debug.Log($"{nameof(UnisavePreferences)}.{nameof(Save)}");
			#endif
			
			#if UNITY_EDITOR
			
			// NOTE: do not perform this check, since for example, fullstack
			// tests pretend to run in play mode, yet they need to upload
			// custom backends and tinker with unisave preferences.
			// So really, it's up to the caller to not call this from a build.
			//
			// if (Application.isPlaying)
			// 	throw new InvalidOperationException(
			// 		$"{nameof(Save)} can only be called from editor."
			// 	);
			
			// first, save editor preferences
			SerializeEditorPrefsFields();

			// then save file fields
			bool wasCreated = !File.Exists(PreferencesFilePath);
			JsonObject json = new JsonObject();
			SerializeFileFields(json);
			
			// make sure the core resources directory exists
			string directory = Path.GetDirectoryName(PreferencesFilePath);
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);
			
			File.WriteAllText(
				PreferencesFilePath,
				json.ToString(pretty: true),
				Encoding.UTF8
			);

			if (wasCreated)
			{
				// make sure the file and folder show up
				// in the Unity assets window and the metafile is created
				UnityEditor.AssetDatabase.Refresh();
				
				Debug.Log(
					"[Unisave] Unisave preferences file was " +
					"created at " + PreferencesFilePath
				);
			}
			
			#endif
		}
		
		#endregion
	}
}
