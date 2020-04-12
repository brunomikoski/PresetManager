using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace BrunoMikoski.PresetManager
{
    public static class PresetManagerUtils
    {
        private static string PRESET_MANAGER_DATA_STORAGE_KEY = $"{Application.productName}_Preset_Manager_Data_Key";
        
        private static List<Preset> projectPresets;
        private static List<Preset> ProjectPresets
        {
            get
            {
                if (projectPresets == null)
                    LoadProjectPresets();
                return projectPresets;
            }
        }

        private static PresetManagerData presetManagerData;
        private static PresetManagerData PresetManagerData
        {
            get
            {
                if (presetManagerData == null)
                {
                    string storedJson = EditorPrefs.GetString(PRESET_MANAGER_DATA_STORAGE_KEY, string.Empty);
                    presetManagerData = new PresetManagerData();
                    if (!string.IsNullOrEmpty(storedJson))
                    {
                        EditorJsonUtility.FromJsonOverwrite(storedJson, presetManagerData);
                    }
                }
                

                return presetManagerData;
            }
        }
        
        private static bool isFolderDataDirty;



        private static void LoadProjectPresets()
        {
            string[] presetsGUIDs = AssetDatabase.FindAssets("t:Preset");

            projectPresets = new List<Preset>();
            foreach (string presetsGuiD in presetsGUIDs)
            {
                projectPresets.Add(AssetDatabase.LoadAssetAtPath<Preset>(AssetDatabase.GUIDToAssetPath(presetsGuiD)));
            }
        }

        public static Preset[] GetAvailablePresetsForAssetImporter(AssetImporter assetImporter)
        {
            List<Preset> resultPresets = new List<Preset>();
            for (var i = 0; i < ProjectPresets.Count; i++)
            {
                Preset preset = ProjectPresets[i];
                if (!preset.ApplyTo(assetImporter))
                    continue;
                
                resultPresets.Add(preset);
            }

            return resultPresets.ToArray();
        }

        public static bool HasAnyPresetForFolder(string relativeFolderPath)
        {
            return PresetManagerData.HasAnyPresetForFolder(relativeFolderPath);
        }
        
        public static bool HasPresetFor(AssetImporter assetImporter)
        {
            for (var i = 0; i < ProjectPresets.Count; i++)
            {
                Preset preset = ProjectPresets[i];
                if (!preset.ApplyTo(assetImporter))
                    continue;

                return true;
            }

            return false;
        }

        public static bool TryGetAssetPresetFromFolder(string relativeFolderPath, AssetImporter assetImporter,
            out Preset preset)
        {
            return PresetManagerData.TryGetAssetPresetFromFolder(relativeFolderPath, assetImporter, out preset);
        }

        public static void SetPresetForFolder(string relativeFolderPath, Preset preset)
        {
            PresetManagerData.SetPresetForFolder(relativeFolderPath, preset);
            isFolderDataDirty = true;
        }

        public static void ClearPresetForFolder(string relativeFolderPath)
        {
            PresetManagerData.ClearPresetForFolder(relativeFolderPath);
            isFolderDataDirty = true;
        }
        
        public static void ClearAllPresetsForFolder(string relativeFolderPath)
        {
            PresetManagerData.ClearAllPresetForFolder(relativeFolderPath);
            isFolderDataDirty = true;
        }

        public static void SaveData()
        {
            if (!isFolderDataDirty)
                return;

            EditorPrefs.SetString(PRESET_MANAGER_DATA_STORAGE_KEY, EditorJsonUtility.ToJson(PresetManagerData));
            isFolderDataDirty = false;
        }

        public static void ProjectPresetsChanged()
        {
            projectPresets = null;
        }

        public static bool TryToGetParentPresetSettings(string relativeFolderPath, AssetImporter assetImporter,
            out string relativeParentPath)
        {
            DirectoryInfo currentDirectory = new DirectoryInfo(RelativeToAbsolutePath(relativeFolderPath)).Parent;

            relativeParentPath = string.Empty;
            while (currentDirectory != null && !string.Equals(currentDirectory.FullName, Directory.GetCurrentDirectory(), StringComparison.Ordinal))
            {
                if (PresetManagerData.TryGetPresetFolderPathFromFolder(AbsoluteToRelativePath(currentDirectory.FullName),
                    assetImporter, out string ownerFolderPath))
                {
                    relativeParentPath = ownerFolderPath;
                    break;
                }
                currentDirectory = currentDirectory.Parent;
            }

            return !string.IsNullOrEmpty(relativeParentPath);
        }
        
        public static bool TryToGetParentPresetSettings(string relativeFolderPath, AssetImporter assetImporter,
            out Preset preset)
        {
            DirectoryInfo currentDirectory = new DirectoryInfo(RelativeToAbsolutePath(relativeFolderPath)).Parent;

            preset = null;
            while (currentDirectory != null && !string.Equals(currentDirectory.FullName, Directory.GetCurrentDirectory(), StringComparison.Ordinal))
            {
                if (PresetManagerData.TryGetAssetPresetFromFolder(AbsoluteToRelativePath(currentDirectory.FullName),
                    assetImporter, out preset))
                    break;
                currentDirectory = currentDirectory.Parent;
            }

            return preset != null;
        }
        
        
        public static string AbsoluteToRelativePath(string absoluteFilePath)
        {
            absoluteFilePath = absoluteFilePath.Replace("\\", "/");
            return $"Assets{absoluteFilePath.Replace(Application.dataPath, "")}";
        }

        public static string RelativeToAbsolutePath(string relativeFilePath)
        {
            return (Application.dataPath.Replace("/Assets", "") + "/" + relativeFilePath).Replace("/", "\\");
        }

        public static void ApplySettingsToAsset(string relativeFolderPath, AssetImporter assetImporter)
        {
            if (TryGetAssetPresetFromFolder(relativeFolderPath, assetImporter, out Preset preset))
            {
                preset.ApplyTo(assetImporter);
            }
            else
            {
                if(TryToGetParentPresetSettings(relativeFolderPath, assetImporter, out preset))
                {
                    preset.ApplyTo(assetImporter);
                }
            }
        }
        
        public static void ApplyPresetsToFolder(string relativeFolderPath)
        {
            projectPresets = null;
            string[] assetPaths = GetAllAssetsAtDirectory(relativeFolderPath);
            for (int i = 0; i < assetPaths.Length; i++)
            {
                AssetImporter assetImporter = AssetImporter.GetAtPath(assetPaths[i]);
                ApplySettingsToAsset(relativeFolderPath, assetImporter);
            }

            string[] subFolder = AssetDatabase.GetSubFolders(relativeFolderPath);
            for (var i = 0; i < subFolder.Length; i++)
            {
                string subFolderPath = subFolder[i];
                ApplyPresetsToFolder(subFolderPath);
            }
        }


        public static string[] GetAllAssetsAtDirectory(string relativeDirectoryPath)
        {
            string[] fileEntries = Directory.GetFiles(RelativeToAbsolutePath(relativeDirectoryPath));
            List<string> resuts = new List<string>();

            for (var i = 0; i < fileEntries.Length; i++)
            {
                string fileEntry = fileEntries[i];
                if (fileEntry.EndsWith(".meta"))
                    continue;

                resuts.Add(AbsoluteToRelativePath(fileEntry));
            }

            return resuts.ToArray();
        }
    }
}