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
            string path = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(path))
                return;

            PresetManagerUtils.ApplySettingsToAsset(path, assetImporter);

        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (IsPresetAsset(importedAssets) || IsPresetAsset(deletedAssets))
            {
                PresetManagerUtils.ProjectPresetsChanged();
                return;
            }
        }

        private static bool IsPresetAsset(string[] assetsPath)
        {
            for (var i = 0; i < assetsPath.Length; i++)
            {
                string assetPath = assetsPath[i];

                if (assetPath.EndsWith(".preset"))
                    return true;
            }

            return false;
        }
    }
}