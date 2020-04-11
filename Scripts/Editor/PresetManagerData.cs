using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace BrunoMikoski.PresetManager
{
    [Serializable]
    public sealed class PresetManagerData
    {
        [Serializable]
        private class FolderToPresetReference
        {
            [SerializeField]
            private string folderGUID;
            public string FolderGuid => folderGUID;

            [SerializeField]
            private string presetGUID;
            public string PresetGuid => presetGUID;

            public FolderToPresetReference(string folderGUID, string presetGUID)
            {
                this.folderGUID = folderGUID;
                this.presetGUID = presetGUID;
            }

            public void OverridePresetGUID(string presetGUID)
            {
                this.presetGUID = presetGUID;
            }
        }

        [SerializeField]
        private List<FolderToPresetReference> foldersToPreset = new List<FolderToPresetReference>();

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
            for (int i = 0; i < foldersToPreset.Count; i++)
            {
                FolderToPresetReference folderToPresetReference = foldersToPreset[i];
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
                foldersToPreset[targetIndex].OverridePresetGUID(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(preset)));
            }
            else
            {
                foldersToPreset.Add(new FolderToPresetReference(AssetDatabase.AssetPathToGUID(relativeFolderPath),
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(preset))));
            }
        }

        private bool TryGetFolderPresetIndex(string relativeFolderPath, out int index)
        {
            string folderGUID = AssetDatabase.AssetPathToGUID(relativeFolderPath);
            for (int i = 0; i < foldersToPreset.Count; i++)
            {
                FolderToPresetReference folderToPresetReference = foldersToPreset[i];
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
            for (int i = 0; i < foldersToPreset.Count; i++)
            {
                FolderToPresetReference folderToPresetReference = foldersToPreset[i];

                if (string.Equals(folderToPresetReference.FolderGuid, folderPathGUID, StringComparison.Ordinal))
                {
                    foldersToPreset.RemoveAt(i);
                    break;
                }
            }
        }

        public void ClearAllPresetForFolder(string relativeFolderPath)
        {
            string folderPathGUID = AssetDatabase.AssetPathToGUID(relativeFolderPath);
            for (int i = foldersToPreset.Count - 1; i >= 0; i--)
            {
                FolderToPresetReference folderToPresetReference = foldersToPreset[i];

                if (string.Equals(folderToPresetReference.FolderGuid, folderPathGUID, StringComparison.Ordinal))
                {
                    foldersToPreset.RemoveAt(i);
                }
            }
        }

    }
}