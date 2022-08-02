using Unisave.Editor.BackendUploading;
using Unisave.Foundation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Application = UnityEngine.Application;

namespace Unisave.Editor
{
	public class UnisaveEditorWindow : EditorWindow
	{
		/// <summary>
		/// Reference to the preferences file
		/// </summary>
		private UnisavePreferences preferences;

		private Texture unisaveLogo;

		private Vector2 windowScroll = Vector3.zero;

		[MenuItem("Window/Unisave/Preferences", false, 1)]
		public static void ShowWindow()
		{
			EditorWindow.GetWindow(
				typeof(UnisaveEditorWindow),
				false,
				"Unisave"
			);
		}

		void OnEnable()
		{
			titleContent.image = AssetDatabase.LoadAssetAtPath<Texture>(
				EditorGUIUtility.isProSkin ?
				"Assets/Unisave/Images/WindowIconWhite.png" :
				"Assets/Unisave/Images/WindowIcon.png"
			);
		}

		void OnFocus()
		{
			// force the file to reload by forgetting it
			// (loading happens inside OnGUI)
			preferences = null;
		}

		// called by unity, when keyboard focus is lost
		// BUT ALSO by this window when mouse leaves the window
		void OnLostFocus()
		{
			if (preferences != null)
			{
				BeforePreferencesSave();
				preferences.Save();
			}
		}

		/// <summary>
		/// Called when preferences get reloaded
		/// (usually on focus or creation)
		/// </summary>
		private void OnPreferencesLoaded()
		{
			// ...
		}

		/// <summary>
		/// Called just before preferences get saved
		/// (usually on lost focus)
		/// </summary>
		private void BeforePreferencesSave()
		{
			// ...
		}

		void OnGUI()
		{
			if (preferences == null)
			{
				preferences = UnisavePreferences.LoadOrCreate();
				OnPreferencesLoaded();
			}

			windowScroll = GUILayout.BeginScrollView(windowScroll);

			DrawUnisaveLogo();

			GUILayout.Label("Unisave server connection", EditorStyles.boldLabel);
			preferences.ServerUrl = EditorGUILayout.TextField("Server URL", preferences.ServerUrl);
			preferences.GameToken = EditorGUILayout.TextField("Game token", preferences.GameToken);
			preferences.EditorKey = EditorGUILayout.TextField("Editor key", preferences.EditorKey);

			GUILayout.Space(15f);
			
			GUILayout.Label("Backend folder uploading", EditorStyles.boldLabel);
			preferences.BackendFolder = EditorGUILayout.TextField("Backend assets folder", preferences.BackendFolder);
			preferences.AutomaticBackendUploading = EditorGUILayout.Toggle("Automatic", preferences.AutomaticBackendUploading);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Manual", GUILayout.Width(EditorGUIUtility.labelWidth - 4));
			if (GUILayout.Button("Upload", GUILayout.Width(50f)))
				RunManualCodeUpload();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Last upload at", GUILayout.Width(EditorGUIUtility.labelWidth - 4));
			EditorGUILayout.LabelField(preferences.LastBackendUploadAt?.ToString("yyyy-MM-dd H:mm:ss") ?? "Never");
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Backend hash", GUILayout.Width(EditorGUIUtility.labelWidth - 4));
			EditorGUILayout.LabelField(
				string.IsNullOrWhiteSpace(preferences.BackendHash)
					? "<not computed yet>"
					: preferences.BackendHash
			);
			EditorGUILayout.EndHorizontal();
			
//			GUILayout.Label("Environment configuration", EditorStyles.boldLabel);
//			preferences.DevelopmentEnv = (TextAsset) EditorGUILayout.ObjectField(
//				"Development", preferences.DevelopmentEnv, typeof(TextAsset), false
//			);
//			preferences.TestingEnv = (TextAsset) EditorGUILayout.ObjectField(
//				"Testing", preferences.TestingEnv, typeof(TextAsset), false
//			);

			GUILayout.Space(30f);

			GUILayout.Label("Changes to configuration are saved automatically.");

			GUILayout.Space(30f);

			GUILayout.Label("Unisave framework version: " + FrameworkMeta.Version);

			GUILayout.EndScrollView();

			// detect mouse leave
			if (Event.current.type == EventType.MouseLeaveWindow)
				OnLostFocus();
		}

		void DrawUnisaveLogo()
		{
			const float margin = 10f;
			const float maxWidth = 400f;

			if (unisaveLogo == null)
			{
				unisaveLogo = AssetDatabase.LoadAssetAtPath<Texture>(
					EditorGUIUtility.isProSkin ?
					"Assets/Unisave/Images/PropertiesLogoWhite.png" :
					"Assets/Unisave/Images/PropertiesLogo.png"
				);
			}

			float width = Mathf.Min(position.width, maxWidth) - 2 * margin;
			float height = width * (unisaveLogo.height / (float)unisaveLogo.width);

			GUI.DrawTexture(
				new Rect((position.width - width) / 2, margin, width, height),
				unisaveLogo
			);
			GUILayout.Space(height + 2 * margin);
		}

		void RunManualCodeUpload()
		{
			Uploader
				.GetDefaultInstance()
				.UploadBackend(
					verbose: true,
					useAnotherThread: true // yes, here we can run in background
				);
		}
	}
}
