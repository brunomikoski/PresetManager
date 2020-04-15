using System;
using UnityEngine;

namespace BrunoMikoski.PresetManager
{
    [Serializable]
    public struct FolderToPresetDataNew
    {
        [SerializeField]
        private string folderGUID;
        public string FolderGuid => folderGUID;

        [SerializeField]
        private string presetGUID;
        public string PresetGuid => presetGUID;

        public FolderToPresetDataNew(string folderGUID, string presetGUID)
        {
            this.folderGUID = folderGUID;
            this.presetGUID = presetGUID;
        }

        public void OverridePresetGUID(string presetGUID)
        {
            this.presetGUID = presetGUID;
        }
    }
}