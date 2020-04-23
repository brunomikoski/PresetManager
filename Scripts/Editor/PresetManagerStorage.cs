using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace BrunoMikoski.PresetManager
{
    public struct PresetData
    {
        public Preset Preset;
        public string[] TargetParameters;

        public PresetData(Preset preset, string[] targetParameters)
        {
            Preset = preset;
            TargetParameters = targetParameters;
        }
    }

    
    public class PresetManagerStorage : ScriptableObject
    {
        private const string DEFAULT_STORAGE_PATH = "Assets/PresetManager/PresetManager.asset";

        [SerializeField]
        private List<FolderToPresetData> foldersPresets = new List<FolderToPresetData>();
        public List<FolderToPresetData> FoldersPresets => foldersPresets;

        private static PresetManagerStorage instance;
        public static PresetManagerStorage Instance
        {
            get
            {
                GetOrCreateInstance();
                return instance;
            }
        }

        public static bool IsInstanceAvailable()
        {
            return Instance != null;
        }
        
        private static void GetOrCreateInstance()
        {
            if (instance != null)
                return;
            
            string[] avaialbleGUIDs = AssetDatabase.FindAssets("t:PresetManagerStorage");
            if (avaialbleGUIDs.Length == 0)
            {
                string directoryPath = Path.GetDirectoryName(Path.GetFullPath(DEFAULT_STORAGE_PATH));
                
                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                instance = CreateInstance<PresetManagerStorage>();

                AssetDatabase.CreateAsset(instance, DEFAULT_STORAGE_PATH);
                AssetDatabase.SaveAssets();
            }
            else
            {
                instance = AssetDatabase.LoadAssetAtPath<PresetManagerStorage>(
                    AssetDatabase.GUIDToAssetPath(avaialbleGUIDs[0]));

                ValidateInstances();
            }
        }

        private static void ValidateInstances()
        {
            for (int i = Instance.foldersPresets.Count - 1; i >= 0; i--)
            {
                FolderToPresetData instanceFoldersPreset = Instance.foldersPresets[i];
                if (!instanceFoldersPreset.IsValid)
                    Instance.foldersPresets.RemoveAt(i);
            }
        }

        public bool HasAnyPresetForFolder(string relativeFolderPath)
        {
            return TryGetPresetsForFolder(relativeFolderPath, out PresetData[] presets);
        }

        public bool TryGetPresetFolderPathFromFolder(string relativeFolderPath, AssetImporter assetImporter,
            out string ownerFolder)
        {
            ownerFolder = string.Empty;
            if (TryGetPresetsForFolder(relativeFolderPath, out PresetData[] presets))
            {
                for (int i = 0; i < presets.Length; i++)
                {
                    PresetData currentPreset = presets[i];
                    if (currentPreset.Preset.CanBeAppliedTo(assetImporter))
                    {
                        ownerFolder = relativeFolderPath;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryGetAssetPresetFromFolder(string relativeFolderPath, AssetImporter assetImporter, out PresetData preset)
        {
            if (TryGetPresetsForFolder(relativeFolderPath, out PresetData[] presets))
            {
                for (int i = 0; i < presets.Length; i++)
                {
                    PresetData currentPreset = presets[i];
                    if (currentPreset.Preset.CanBeAppliedTo(assetImporter))
                    {
                        preset = currentPreset;
                        return true;
                    }
                }
            }

            preset = default;
            return false;
        }
        
        public bool TryGetPresetsForFolder(string relativeFolderPath, out PresetData[] preset)
        {
            List<PresetData> presetsList = new List<PresetData>();
            string folderGUID = AssetDatabase.AssetPathToGUID(relativeFolderPath);
            for (int i = 0; i < foldersPresets.Count; i++)
            {
                FolderToPresetData folderToPresetReference = foldersPresets[i];
                if (string.Equals(folderToPresetReference.FolderGuid, folderGUID, StringComparison.Ordinal))
                {
                    string presetPath = AssetDatabase.GUIDToAssetPath(folderToPresetReference.PresetGuid);
                    Preset loadedPreset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);
                    if (loadedPreset != null)
                    {
                        presetsList.Add(new PresetData(loadedPreset,
                            folderToPresetReference.GetModifications(loadedPreset)));
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
                foldersPresets.Add(new FolderToPresetData(AssetDatabase.AssetPathToGUID(relativeFolderPath),
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(preset))));
            }

            EditorUtility.SetDirty(this);
        }

        private bool TryGetFolderPresetIndex(string relativeFolderPath, out int index)
        {
            string folderGUID = AssetDatabase.AssetPathToGUID(relativeFolderPath);
            for (int i = 0; i < foldersPresets.Count; i++)
            {
                FolderToPresetData folderToPresetReference = foldersPresets[i];
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
                FolderToPresetData folderToPresetReference = foldersPresets[i];

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
                FolderToPresetData folderToPresetReference = foldersPresets[i];

                if (string.Equals(folderToPresetReference.FolderGuid, folderPathGUID, StringComparison.Ordinal))
                {
                    foldersPresets.RemoveAt(i);
                }
            }

            EditorUtility.SetDirty(this);
        }
    }
}
