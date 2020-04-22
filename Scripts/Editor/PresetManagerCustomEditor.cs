using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace BrunoMikoski.PresetManager
{
    [CustomEditor(typeof(PresetManagerStorage))]
    public sealed class PresetManagerCustomEditor : Editor
    {
        private PresetManagerStorage presetManagerStorage;
        private bool[] foldoutPerSettings;
        private bool addNewSetup;
        
        [SerializeField] 
        private DefaultAsset newSetupFolder;

        [SerializeField] 
        private Preset newSetupPreset;

        private void OnEnable()
        {
            presetManagerStorage = (PresetManagerStorage)target;
        }

        public override void OnInspectorGUI()
        {
            List<FolderToPresetData> folderToPresetDatas = presetManagerStorage.FoldersPresets;
            for (int i = folderToPresetDatas.Count - 1; i >= 0; i--)
            {
                FolderToPresetData folderToPresetData = folderToPresetDatas[i];

                if (!folderToPresetData.IsValid)
                    continue;
                
                DrawFolderToPreset(i, folderToPresetData);
            }
            DrawExtraOptions();            
        }

        private void DrawExtraOptions()
        {
            if (!addNewSetup)
                addNewSetup |= GUILayout.Button("+", EditorStyles.toolbarButton);

            if (addNewSetup)
            {
                DrawAddNewSettings();
            }
        }

        private void DrawAddNewSettings()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField($"Add new Folder to Preset config", EditorStyles.toolbarDropDown);
            EditorGUILayout.Space();
            newSetupFolder = (DefaultAsset) EditorGUILayout.ObjectField("Folder", newSetupFolder, typeof(DefaultAsset), false);
            newSetupPreset = (Preset) EditorGUILayout.ObjectField("Preset", newSetupPreset, typeof(Preset), false);
            

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(newSetupFolder == null || newSetupPreset == null || !HasFolderSelected());

            if (GUILayout.Button("Save", EditorStyles.toolbarButton))
            {
                FolderToPresetData folderToPresetData = new FolderToPresetData(
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(newSetupFolder)),
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(newSetupPreset)));
                

                EditorUtility.SetDirty(presetManagerStorage);

                newSetupFolder = null;
                newSetupPreset = null;
                addNewSetup = false;
                foldoutPerSettings = null;
                presetManagerStorage.FoldersPresets.Add(folderToPresetData);
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Cancel", EditorStyles.toolbarButton))
            {
                newSetupFolder = null;
                newSetupPreset = null;
                addNewSetup = false;
            }
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.EndVertical();
        }

        private bool HasFolderSelected()
        {
            if (newSetupFolder == null)
                return false;

            string path = AssetDatabase.GetAssetPath(newSetupFolder);
            return Directory.Exists(PresetManagerUtils.RelativeToAbsolutePath(path));
        }

        private void DrawFolderToPreset(int index, FolderToPresetData folderToPresetData)
        {
            string folderPath = AssetDatabase.GUIDToAssetPath(folderToPresetData.FolderGuid);
            DefaultAsset folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);
            string presetPath = AssetDatabase.GUIDToAssetPath(folderToPresetData.PresetGuid);
            Preset preset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);

            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField($"{preset.name} -> {folder.name}", EditorStyles.toolbarDropDown);

            DrawReferences(folderPath, folder, presetPath, preset);
            DrawPresetOptions(index, folderToPresetData, preset);


            EditorGUILayout.EndVertical();
        }

        private void DrawPresetOptions(int index, FolderToPresetData folderToPresetData, Preset preset)
        {
            EditorGUI.indentLevel++;

            if (foldoutPerSettings == null)
                foldoutPerSettings = new bool[presetManagerStorage.FoldersPresets.Count];
            
            foldoutPerSettings[index] = EditorGUILayout.Foldout(foldoutPerSettings[index],
                "Customize Parameters", EditorStyles.foldout);

            if (foldoutPerSettings[index])
            {
                for (int i = 0; i < preset.PropertyModifications.Length; i++)
                {
                    PropertyModification propertyModification = preset.PropertyModifications[i];
                    
                    EditorGUI.BeginChangeCheck();
                    bool settingEnabled = folderToPresetData.IsSettingEnabled(propertyModification.propertyPath);
                    settingEnabled = EditorGUILayout.ToggleLeft(propertyModification.propertyPath, settingEnabled);
                    if (EditorGUI.EndChangeCheck())
                    {
                        folderToPresetData.SetSettingEnabled(propertyModification.propertyPath, settingEnabled);
                        EditorUtility.SetDirty(presetManagerStorage);
                    }
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select All", EditorStyles.toolbarButton))
                {
                    ToggleAllProperties(folderToPresetData, preset, true);
                }
            
                if (GUILayout.Button("Unselect All", EditorStyles.toolbarButton))
                {
                    ToggleAllProperties(folderToPresetData, preset, false);
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Delete", EditorStyles.toolbarButton))
            {
                presetManagerStorage.FoldersPresets.Remove(folderToPresetData);
            }

            EditorGUI.indentLevel--;

        }

        private void ToggleAllProperties(FolderToPresetData folderToPresetData, Preset preset, bool enabled)
        {
            for (int i = 0; i < preset.PropertyModifications.Length; i++)
            {
                PropertyModification presetPropertyModification = preset.PropertyModifications[i];
                folderToPresetData.SetSettingEnabled(presetPropertyModification.propertyPath, enabled);
            }

            EditorUtility.SetDirty(presetManagerStorage);
        }

        private void DrawReferences(string folderPath, DefaultAsset folder, string presetPath, Preset preset)
        {
            EditorGUILayout.Space();
            EditorGUI.BeginDisabledGroup(true);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{folderPath}", EditorStyles.toolbarTextField);
            EditorGUILayout.ObjectField(folder, typeof(DefaultAsset), false);
            EditorGUILayout.EndHorizontal();

            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{presetPath}", EditorStyles.toolbarTextField);
            EditorGUILayout.ObjectField(preset, typeof(Preset), false);
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.EndDisabledGroup();
        }
    }
}