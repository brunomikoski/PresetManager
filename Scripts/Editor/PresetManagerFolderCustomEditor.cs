using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace BrunoMikoski.PresetManager
{
    [CustomEditor(typeof(DefaultAsset), true)]
    public class PresetManagerFolderCustomEditor : Editor
    {

        [SerializeField] 
        private bool isFolder;
        [SerializeField] 
        private string relativeFolderPath;
        [SerializeField]
        private string absoluteFolderPath;
        
        [SerializeField]
        private AssetImporter[] assetImportersType;
        [SerializeField]
        private bool[] assetImportersTypeFoldout;

        [SerializeField] 
        private int selectedIndex  = -1;

        private void OnEnable()
        {
            relativeFolderPath = AssetDatabase.GetAssetPath(target);
            absoluteFolderPath = Application.dataPath.Replace("/Assets", "") + "/" + relativeFolderPath;
            isFolder = Directory.Exists(absoluteFolderPath);

            if (!isFolder)
                return;
            
            ReadFolder();
        }

        private void OnDisable()
        {
            PresetManagerUtils.SaveData();
        }

        protected override void OnHeaderGUI()
        {
            base.OnHeaderGUI();

            if (!isFolder)
                return;

            if (assetImportersType == null || assetImportersType.Length == 0)
                return;

            DrawPresetManager();

        }

        private void DrawPresetManager()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Assets Preset Manager", EditorStyles.toolbarTextField);
            for (var i = 0; i < assetImportersType.Length; i++)
            {
                AssetImporter assetImporter = assetImportersType[i];
                assetImportersTypeFoldout[i] = EditorGUILayout.Foldout(assetImportersTypeFoldout[i],
                    assetImporter.GetType().Name, EditorStyles.foldout);

                if (assetImportersTypeFoldout[i])
                {
                    EditorGUI.indentLevel++;
                    ShowOptionsForImporter(assetImporter);
                    EditorGUI.indentLevel--;

                }
            }

            DrawOptions();

            EditorGUILayout.EndVertical();
        }

        private void DrawOptions()
        {
            EditorGUILayout.BeginHorizontal("Box");
            bool hasAnyPresetForFolder = PresetManagerUtils.HasAnyPresetForFolder(relativeFolderPath);
            EditorGUI.BeginDisabledGroup(!hasAnyPresetForFolder);
            if (GUILayout.Button("Apply to all", EditorStyles.toolbarButton))
            {
                
            }
            
            if (GUILayout.Button("Delete Folder Settings", EditorStyles.toolbarButton))
            {
                PresetManagerUtils.ClearAllPresetsForFolder(relativeFolderPath);
                selectedIndex = 0;
            }
            EditorGUI.EndDisabledGroup();

            
            EditorGUILayout.EndHorizontal();
        }

        private void ShowOptionsForImporter(AssetImporter assetImporter)
        {
            Preset[] presets = PresetManagerUtils.GetAvailablePresetsForAssetImporter(assetImporter);
            string[] presetsNames = GetNamesFromList(presets, true);

            if (selectedIndex == -1)
            {
                if (PresetManagerUtils.TryGetAssetPresetFromFolder(relativeFolderPath, assetImporter, out Preset appliedPreset))
                    selectedIndex = Array.IndexOf(presets, appliedPreset) + 1;
                else
                    selectedIndex = 0;
            }

            if (selectedIndex == 0)
            {
                if (PresetManagerUtils.TryToGetParentPresetSettings(relativeFolderPath, assetImporter,
                    out string parentRelativePath))
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField("Inheriting settings from:",
                        AssetDatabase.LoadAssetAtPath<DefaultAsset>(parentRelativePath), typeof(DefaultAsset), false);
                    EditorGUI.EndDisabledGroup();
                }
            }
            
            EditorGUI.BeginChangeCheck();
            selectedIndex = GUILayout.SelectionGrid(selectedIndex, presetsNames, 1, EditorStyles.radioButton);

            if (EditorGUI.EndChangeCheck())
            {
                if (selectedIndex > 0)
                {
                    PresetManagerUtils.SetPresetForFolder(relativeFolderPath, presets[selectedIndex - 1]);
                }
                else
                {
                    PresetManagerUtils.ClearPresetForFolder(relativeFolderPath);
                }
            }
        }    

        private string[] GetNamesFromList(Preset[] presets, bool includeNone)
        {
            List<string> options = new List<string>();
            if (includeNone)
            {
                options.Add("None");
            }
            for (var i = 0; i < presets.Length; i++)
            {
                Preset preset = presets[i];
                options.Add(preset.name);
            }

            return options.ToArray();
        }

        private void ReadFolder()
        {
            string[] files = Directory.GetFiles(absoluteFolderPath);

            HashSet<AssetImporter> assetImporters = new HashSet<AssetImporter>();
            for (var i = 0; i < files.Length; i++)
            {
                string absoluteFilePath = files[i];
                if (absoluteFilePath.EndsWith(".meta"))
                    continue;
                string relativeFilePath = PresetManagerUtils.AbsoluteToRelativePath(absoluteFilePath);

                AssetImporter assetImporter = AssetImporter.GetAtPath(relativeFilePath);

                if (!PresetManagerUtils.HasPresetFor(assetImporter))
                    continue;
                
                assetImporters.Add(assetImporter);
            }

            assetImportersType = assetImporters.ToArray();
            if (assetImportersTypeFoldout == null || assetImportersTypeFoldout.Length != assetImportersType.Length)
                assetImportersTypeFoldout = new bool[assetImportersType.Length];
        }
    }
}