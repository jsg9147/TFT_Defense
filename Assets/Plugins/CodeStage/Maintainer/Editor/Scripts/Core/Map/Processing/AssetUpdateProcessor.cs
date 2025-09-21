#region copyright
// -------------------------------------------------------
// Copyright (C) Dmitry Yuhanov [https://codestage.net]
// -------------------------------------------------------
#endregion

namespace CodeStage.Maintainer.Core.Map.Processing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using Progress;
    using Tools;
    using Settings;

    internal class AssetUpdateProcessor
    {
        private readonly IProgressReporter progressReporter;
        private HashSet<string> validExistingAssetsGuids;

        public AssetUpdateProcessor(IProgressReporter progressReporter)
        {
            this.progressReporter = progressReporter;
        }

        public bool ProcessExistingAssets(AssetsMap map)
        {
            if (progressReporter.ShowProgress(1, "Checking existing assets in map...", 0, 1))
            {
                Debug.Log(Maintainer.ConstructLog("Assets Map update was canceled by user."));
                return false;
            }

            var count = map.assets.Count;
            validExistingAssetsGuids = new HashSet<string>();
                
            for (var i = count - 1; i > -1; i--)
            {
                var index = count - i;
                if (progressReporter.ShowProgress(1, "Checking existing assets in map... {0}/{1}", index, count))
                {
                    Debug.Log(Maintainer.ConstructLog("Assets Map update was canceled by user."));
                    return false;
                }

                var assetInMap = map.assets[i];
                if (assetInMap.Exists())
                {
                    validExistingAssetsGuids.Add(assetInMap.GUID);
                    if (assetInMap.UpdateIfNeeded())
                    {
                        map.isDirty = true;
                    }
                }
                else
                {
                    assetInMap.Clean();
                    map.assets.RemoveAt(i);
                    map.isDirty = true;
                }
            }

            return true;
        }

        public bool ProcessNewAssets(AssetsMap map)
        {
            if (progressReporter.ShowProgress(2, "Looking for new assets... {0}/{1}", 0, 1))
            {
                Debug.Log(Maintainer.ConstructLog("Assets Map update was canceled by user."));
                return false;
            }

            var allAssetPaths = AssetDatabase.GetAllAssetPaths();
            var validNewAssets = new List<RawAssetInfo>(allAssetPaths.Length);
            foreach (var assetPath in allAssetPaths)
            {
                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (validExistingAssetsGuids.Contains(guid))
                    continue;

                var kind = CSAssetTools.GetAssetOrigin(assetPath);
                if (kind == AssetOrigin.Unknown) continue;

                if (!File.Exists(assetPath)) continue;
                if (AssetDatabase.IsValidFolder(assetPath)) continue;

                var rawInfo = new RawAssetInfo
                {
                    path = CSPathTools.EnforceSlashes(assetPath),
                    guid = guid,
                    origin = kind,
                };

                validNewAssets.Add(rawInfo);
            }

            var count = validNewAssets.Count;
            for (var i = 0; i < count; i++)
            {
                if (progressReporter.ShowProgress(2, "Processing new assets... {0}/{1}", i + 1, count))
                {
                    Debug.Log(Maintainer.ConstructLog("Assets Map update was canceled by user."));
                    return false;
                }

                var rawAssetInfo = validNewAssets[i];
                var rawAssetInfoPath = rawAssetInfo.path;

                var type = AssetDatabase.GetMainAssetTypeAtPath(rawAssetInfoPath);
                if (type == null)
                {
                    var loadedAsset = AssetDatabase.LoadMainAssetAtPath(rawAssetInfoPath);
                    if (loadedAsset == null)
                    {
                        if (rawAssetInfo.origin != AssetOrigin.ImmutablePackage)
                        {
                            if (!CSAssetTools.IsAssetScriptableObjectWithMissingScript(rawAssetInfoPath))
                            {
                                Debug.LogWarning(Maintainer.ConstructLog("Can't retrieve type of the asset:\n" +
                                                                        rawAssetInfoPath));
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        type = loadedAsset.GetType();
                    }
                }

                var settingsKind = rawAssetInfo.origin == AssetOrigin.Settings ? GetSettingsKind(rawAssetInfoPath) : AssetSettingsKind.Undefined;

                var asset = AssetInfo.Create(rawAssetInfo, type, settingsKind);
                map.assets.Add(asset);
                map.isDirty = true;
            }

            return true;
        }

        private static AssetSettingsKind GetSettingsKind(string assetPath)
        {
            var result = AssetSettingsKind.UnknownSettingAsset;

            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            if (!string.IsNullOrEmpty(fileName))
            {
                try
                {
                    result = (AssetSettingsKind)Enum.Parse(CSReflectionTools.assetSettingsKindType, fileName);
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            return result;
        }
    }
} 