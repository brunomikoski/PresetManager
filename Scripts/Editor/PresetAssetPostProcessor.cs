using System.IO;
using UnityEditor;

namespace BrunoMikoski.PresetManager
{
    public class PresetAssetPostProcessor : AssetPostprocessor
    {
        private void OnPreprocessAsset()
        {
            if (!assetImporter.importSettingsMissing)
                return;

            if (IsPresetAsset(assetImporter.assetPath))
            {
                PresetManagerUtils.ProjectPresetsChanged();
                return;
            }
            
            string path = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(path))
                return;
            
            PresetManagerUtils.ApplySettingsToAsset(path, assetImporter);
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!PresetManagerStorage.IsInstanceAvailable())
                return;

            if (!IsPresetAsset(importedAssets) && !IsPresetAsset(deletedAssets)) 
                return;
            
            PresetManagerUtils.ProjectPresetsChanged();
        }

        private static bool IsPresetAsset(params string[] assetsPath)
        {
            for (int i = 0; i < assetsPath.Length; i++)
            {
                string assetPath = assetsPath[i];

                if (assetPath.EndsWith(".preset"))
                    return true;
            }

            return false;
        }
    }
}
