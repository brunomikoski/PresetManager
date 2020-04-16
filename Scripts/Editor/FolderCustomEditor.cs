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
    public class FolderCustomEditor : Editor
    {
        private bool isFolder;
        private string relativeFolderPath;
        private string absoluteFolderPath;
        private AssetImporter[] assetImportersType;
        private bool[] assetImportersTypeFoldout;
        private int selectedIndex  = -1;

        private void OnEnable()
        {
            relativeFolderPath = AssetDatabase.GetAssetPath(target);
            absoluteFolderPath = PresetManagerUtils.RelativeToAbsolutePath(relativeFolderPath);
            isFolder = Directory.Exists(absoluteFolderPath);

            if (!isFolder)
                return;
            
            ReadFolder();
        }
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!isFolder)
                return;

            if (assetImportersType == null || assetImportersType.Length == 0)
                return;
            
            bool wasGUIEnabled = GUI.enabled;
            
            GUI.enabled = true;
            DrawPresetManager();
            GUI.enabled = wasGUIEnabled;
        }

        private void DrawPresetManager()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Assets Preset Manager", EditorStyles.toolbarDropDown);
            EditorGUI.indentLevel++;

            for (var i = 0; i < assetImportersType.Length; i++)
            {
                AssetImporter assetImporter = assetImportersType[i];
                assetImportersTypeFoldout[i] = EditorGUILayout.Foldout(assetImportersTypeFoldout[i],
                    assetImporter.GetType().Name, EditorStyles.foldout);

                if (assetImportersTypeFoldout[i])
                {
                    ShowOptionsForImporter(assetImporter);
                }
            }
            EditorGUI.indentLevel--;
            DrawOptions();

            EditorGUILayout.EndVertical();
        }

        private void DrawOptions()
        {
            EditorGUILayout.BeginHorizontal("Box");
            bool hasAnyPresetForFolder = PresetManagerUtils.HasAnyPresetForFolder(relativeFolderPath);
            EditorGUI.BeginDisabledGroup(!hasAnyPresetForFolder);
            if (GUILayout.Button("Apply on current", EditorStyles.toolbarButton))
            {
                PresetManagerUtils.ApplyPresetsToFolder(relativeFolderPath);
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
                if (PresetManagerUtils.TryGetAssetPresetFromFolder(relativeFolderPath, assetImporter, out PresetData appliedPreset))
                    selectedIndex = Array.IndexOf(presets, appliedPreset.Preset) + 1;
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

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(20);
                selectedIndex = GUILayout.SelectionGrid(selectedIndex, presetsNames, 1, EditorStyles.radioButton);
            }


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
            string[] files = GetAllFiles(absoluteFolderPath);

            HashSet<AssetImporter> assetImporters = new HashSet<AssetImporter>();
            HashSet<Type> assetImportersTypes = new HashSet<Type>();
            
            for (var i = 0; i < files.Length; i++)
            {
                string absoluteFilePath = files[i];
                if (absoluteFilePath.EndsWith(".meta"))
                    continue;
                string relativeFilePath = PresetManagerUtils.AbsoluteToRelativePath(absoluteFilePath);

                AssetImporter assetImporter = AssetImporter.GetAtPath(relativeFilePath);

                if (assetImporter == null)
                    continue;
                
                if (!PresetManagerUtils.HasPresetFor(assetImporter))
                    continue;
                
                if(assetImportersTypes.Contains(assetImporter.GetType()))
                    continue;
                
                assetImporters.Add(assetImporter);
                assetImportersTypes.Add(assetImporter.GetType());
            }

            assetImportersType = assetImporters.ToArray();
            if (assetImportersTypeFoldout == null || assetImportersTypeFoldout.Length != assetImportersType.Length)
                assetImportersTypeFoldout = new bool[assetImportersType.Length];
        }

        private string[] GetAllFiles(string absoluteFolderPath)
        {
            List<string> filesPath = new List<string>();
            SearchForAllFiles(absoluteFolderPath, ref filesPath);
            return filesPath.ToArray();
        }

        private void SearchForAllFiles(string directoryAbsolutePath, ref List<string> filesPath)
        {
            filesPath.AddRange(Directory.GetFiles(directoryAbsolutePath));

            string[] folderPaths = Directory.GetDirectories(directoryAbsolutePath);
            for (int i = 0; i < folderPaths.Length; i++)
            {
                string folderPath = folderPaths[i];
                if (!PresetManagerUtils.HasAnyPresetForFolder(PresetManagerUtils.AbsoluteToRelativePath(folderPath)))
                    SearchForAllFiles(folderPath, ref filesPath);
            }
        }
    }
}