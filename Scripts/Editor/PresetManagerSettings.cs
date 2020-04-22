using UnityEditor;

namespace BrunoMikoski.PresetManager
{
    public class PresetManagerSettings
    {
        public static bool DisplayFolderInspector
        {
            get => EditorPrefs.GetBool(nameof(DisplayFolderInspector), true);
            set => EditorPrefs.SetBool(nameof(DisplayFolderInspector), value);
        }

        public static int MaximumDirectorySearch
        {
            get => EditorPrefs.GetInt(nameof(MaximumDirectorySearch), 3);
            set => EditorPrefs.SetInt(nameof(MaximumDirectorySearch), value);
        }

        //I refuse to use this terrible Settings Provider thingy from unity, so until the new preferences package
        //is available, this warning will happen :( 
        //https://docs.unity3d.com/2018.3/Documentation/ScriptReference/SettingsProvider.html

        [PreferenceItem("Preset Manager")]
        public static void PreferencesGUI()
        {
            EditorGUI.BeginChangeCheck();
            bool displayFolderInspector = EditorGUILayout.Toggle("Show Folder Inspector", DisplayFolderInspector);
            int maximumDirectorySearch =
                EditorGUILayout.IntField("Maximum Recursive Search Levels", MaximumDirectorySearch);

            if (EditorGUI.EndChangeCheck())
            {
                DisplayFolderInspector = displayFolderInspector;
                MaximumDirectorySearch = maximumDirectorySearch;
            }
        }
        
    }
}