using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace BrunoMikoski.PresetManager
{
    public class PresetManagerStorage : ScriptableObject
    {
        private const string DEFAULT_STORAGE_PATH = "Assets/PresetManager/PresetManager.asset";

        [SerializeField]
        private List<FolderToPresetDataNew> foldersPresets = new List<FolderToPresetDataNew>();

        private static PresetManagerStorage instance;
        public static PresetManagerStorage Instance
        {
            get
            {
                if (instance == null)
                    instance = GetOrCreateInstance();
                return instance;
            }
        }

        public static bool IsInstanceAvailable()
        {
            return instance != null;
        }
        
        public static PresetManagerStorage GetOrCreateInstance()
        {
            string[] avaialbleGUIDs = AssetDatabase.FindAssets("t:PresetManagerStorage");
            PresetManagerStorage getOrCreateInstance;
            if (avaialbleGUIDs.Length == 0)
            {
                string directory = Path.GetFullPath(DEFAULT_STORAGE_PATH);
                
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                getOrCreateInstance = CreateInstance<PresetManagerStorage>();

                AssetDatabase.CreateAsset(getOrCreateInstance, DEFAULT_STORAGE_PATH);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                getOrCreateInstance = AssetDatabase.LoadAssetAtPath<PresetManagerStorage>(
                    AssetDatabase.GUIDToAssetPath(avaialbleGUIDs[0]));
            }

            return getOrCreateInstance;
        }
        
        public bool HasAnyPresetForFolder(string relativeFolderPath)
        {
            return TryGetPresetsForFolder(relativeFolderPath, out Preset[] presets);
        }

        public bool TryGetPresetFolderPathFromFolder(string relativeFolderPath, AssetImporter assetImporter,
            out string ownerFolder)
        {
            ownerFolder = string.Empty;
            if (TryGetPresetsForFolder(relativeFolderPath, out Preset[] presets))
            {
                for (var i = 0; i < presets.Length; i++)
                {
                    Preset currentPreset = presets[i];
                    if (currentPreset.ApplyTo(assetImporter))
                    {
                        ownerFolder = relativeFolderPath;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryGetAssetPresetFromFolder(string relativeFolderPath, AssetImporter assetImporter, out Preset preset)
        {
            if (TryGetPresetsForFolder(relativeFolderPath, out Preset[] presets))
            {
                for (var i = 0; i < presets.Length; i++)
                {
                    Preset currentPreset = presets[i];
                    if (currentPreset.ApplyTo(assetImporter))
                    {
                        preset = currentPreset;
                        return true;
                    }
                }
            }

            preset = null;
            return false;
        }
        
        public bool TryGetPresetsForFolder(string relativeFolderPath, out Preset[] preset)
        {
            List<Preset> presetsList = new List<Preset>();
            string folderGUID = AssetDatabase.AssetPathToGUID(relativeFolderPath);
            for (int i = 0; i < foldersPresets.Count; i++)
            {
                FolderToPresetDataNew folderToPresetReference = foldersPresets[i];
                if (string.Equals(folderToPresetReference.FolderGuid, folderGUID, StringComparison.Ordinal))
                {
                    string presetPath = AssetDatabase.GUIDToAssetPath(folderToPresetReference.PresetGuid);
                    Preset loadedPreset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);
                    if (loadedPreset != null)
                    {
                        presetsList.Add(loadedPreset);
                    }
                }
            }

            preset = presetsList.ToArray();
            return presetsList.Count > 0;
        }
        
        public void SetPresetForFolder(string relativeFolderPath, Preset preset)
        {
            if (TryGetFolderPresetIndex(relativeFolderPath, out int targetIndex))
            {
                foldersPresets[targetIndex].OverridePresetGUID(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(preset)));
            }
            else
            {
                foldersPresets.Add(new FolderToPresetDataNew(AssetDatabase.AssetPathToGUID(relativeFolderPath),
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(preset))));
            }

            EditorUtility.SetDirty(this);
        }

        private bool TryGetFolderPresetIndex(string relativeFolderPath, out int index)
        {
            string folderGUID = AssetDatabase.AssetPathToGUID(relativeFolderPath);
            for (int i = 0; i < foldersPresets.Count; i++)
            {
                FolderToPresetDataNew folderToPresetReference = foldersPresets[i];
                if (string.Equals(folderToPresetReference.FolderGuid, folderGUID, StringComparison.Ordinal))
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        public void ClearPresetForFolder(string relativeFolderPath)
        {
            string folderPathGUID = AssetDatabase.AssetPathToGUID(relativeFolderPath);
            for (int i = 0; i < foldersPresets.Count; i++)
            {
                FolderToPresetDataNew folderToPresetReference = foldersPresets[i];

                if (string.Equals(folderToPresetReference.FolderGuid, folderPathGUID, StringComparison.Ordinal))
                {
                    foldersPresets.RemoveAt(i);
                    EditorUtility.SetDirty(this);
                }
            }
        }

        public void ClearAllPresetForFolder(string relativeFolderPath)
        {
            string folderPathGUID = AssetDatabase.AssetPathToGUID(relativeFolderPath);
            for (int i = foldersPresets.Count - 1; i >= 0; i--)
            {
                FolderToPresetDataNew folderToPresetReference = foldersPresets[i];

                if (string.Equals(folderToPresetReference.FolderGuid, folderPathGUID, StringComparison.Ordinal))
                {
                    foldersPresets.RemoveAt(i);
                }
            }

            EditorUtility.SetDirty(this);
        }

        
    }
}