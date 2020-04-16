using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace BrunoMikoski.PresetManager
{
    [Serializable]
    public struct FolderToPresetData
    {
        [SerializeField]
        private string folderGUID;
        public string FolderGuid => folderGUID;

        [SerializeField]
        private string presetGUID;
        public string PresetGuid => presetGUID;

        public bool IsValid => !string.IsNullOrEmpty(folderGUID) && !string.IsNullOrEmpty(presetGUID);

        private List<string> ignoredProperties;

        public FolderToPresetData(string folderGUID, string presetGUID)
        {
            this.folderGUID = folderGUID;
            this.presetGUID = presetGUID;
            
            ignoredProperties = new List<string>();
            
        }

        public void OverridePresetGUID(string presetGUID)
        {
            this.presetGUID = presetGUID;
        }

        public bool IsSettingEnabled(string propertyModificationPropertyPath)
        {
            if (ignoredProperties == null)
                return true;
            
            for (int i = 0; i < ignoredProperties.Count; i++)
            {
                string ignoredProperty = ignoredProperties[i];
                if (string.Equals(ignoredProperty, propertyModificationPropertyPath, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public void SetSettingEnabled(string propertyModificationPropertyPath, bool settingEnabled)
        {
            if (settingEnabled)
            {
                if (ignoredProperties == null)
                    return;
                
                ignoredProperties.Remove(propertyModificationPropertyPath);
            }
            else
            {
                if (ignoredProperties == null)
                    ignoredProperties = new List<string>();
                
                ignoredProperties.Add(propertyModificationPropertyPath);
            }
        }

        public string[] GetModifications(Preset preset)
        {
            List<string> finalItems = new List<string>();
            for (int i = 0; i < preset.PropertyModifications.Length; i++)
            {
                PropertyModification presetPropertyModification = preset.PropertyModifications[i];
                if(ignoredProperties != null && ignoredProperties.Contains(presetPropertyModification.propertyPath))
                    continue;
                
                finalItems.Add(presetPropertyModification.propertyPath);
            }

            return finalItems.ToArray();
        }
    }
}