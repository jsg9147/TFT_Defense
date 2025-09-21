#region copyright
// -------------------------------------------------------
// Copyright (C) Dmitry Yuhanov [https://codestage.net]
// -------------------------------------------------------
#endregion

namespace CodeStage.Maintainer.Core
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using Dependencies;
	using UnityEditor;
	using UnityEngine;
	using System.Runtime.Serialization;

	using Tools;

	internal class RawAssetInfo
	{
		public string path;
		public string guid;
		public AssetOrigin origin;
	}

	[Serializable]
	public class AssetInfo : IEquatable<AssetInfo>, IDeserializationCallback
	{
		/// <summary>
		/// Asset GUID as reported by AssetDatabase.
		/// </summary>
		public string GUID { get; private set; }
		
		/// <summary>
		/// Path to the Asset, as reported by AssetDatabase, with enforced forward slash delimiter (/).
		/// </summary>
		public string Path { get; private set; }
		
		/// <summary>
		/// Represents the asset origin.
		/// </summary>
		public AssetOrigin Origin { get; private set; }
		
		public AssetSettingsKind SettingsKind { get; private set; }
		public Type Type { get; private set; }

		[NonSerialized] 
		private long size = -1;
		
		public long Size 
		{ 
			get
            {
                if (size == -1 && !string.IsNullOrEmpty(Path) && File.Exists(Path))
                    size = new FileInfo(Path).Length;
                return size;
            }
            private set => size = value;
		}
		
		[field:NonSerialized]
		public bool IsUntitledScene { get; private set; }

		internal string[] dependenciesGUIDs = Array.Empty<string>();
		internal AssetReferenceInfo[] assetReferencesInfo = Array.Empty<AssetReferenceInfo>();
		internal ReferencedAtAssetInfo[] referencedAtInfoList = Array.Empty<ReferencedAtAssetInfo>();

		private int lastHash;
		internal bool needToRebuildReferences = true;

		[NonSerialized] private int[] allAssetObjects;

		internal static AssetInfo Create(RawAssetInfo rawAssetInfo, Type type, AssetSettingsKind settingsKind)
		{
			if (string.IsNullOrEmpty(rawAssetInfo.guid))
			{
				Debug.LogError(Maintainer.ErrorForSupport("Can't create AssetInfo since guid for file " + rawAssetInfo.path + " is invalid!"));
				return null;
			}

			var newAsset = new AssetInfo
			{
				GUID = rawAssetInfo.guid,
				Path = rawAssetInfo.path,
				Origin = rawAssetInfo.origin,
				Type = type,
				SettingsKind = settingsKind,
			};

			newAsset.UpdateIfNeeded();

			return newAsset;
		}

		internal static AssetInfo CreateUntitledScene()
		{
			return new AssetInfo
			{
				GUID = CSPathTools.UntitledScenePath,
				Path = CSPathTools.UntitledScenePath,
				Origin = AssetOrigin.AssetsFolder,
				Type = CSReflectionTools.sceneAssetType,
				IsUntitledScene = true
			};
		}

		private AssetInfo() { }

		internal bool Exists(bool actualizePath = true)
		{
			if (actualizePath)
				ActualizePath();
			return File.Exists(Path);
		}

		internal bool UpdateIfNeeded()
		{
			if (string.IsNullOrEmpty(Path))
			{
				Debug.LogWarning(Maintainer.ConstructLog("Can't update Asset since path is not set!"));
				return false;
			}

			/*if (Path.Contains("qwerty.unity"))
			{
				Debug.Log(Path);
			}*/

			if (!Exists(false))
			{
				Debug.LogWarning(Maintainer.ConstructLog("Can't update asset since file is not found:\n" + Path));
				return false;
			}

			var currentHash = AssetDatabase.GetAssetDependencyHash(Path).GetHashCode();
			if (lastHash == currentHash)
			{
				var dirty = false;

				for (var i = dependenciesGUIDs.Length - 1; i > -1; i--)
				{
					var guid = dependenciesGUIDs[i];
					var path = AssetDatabase.GUIDToAssetPath(guid);
					path = CSPathTools.EnforceSlashes(path);
					if (!string.IsNullOrEmpty(path) && (File.Exists(path) || AssetDatabase.IsValidFolder(path))) 
						continue;

					dirty = true;

					ArrayUtility.RemoveAt(ref dependenciesGUIDs, i);
					foreach (var referenceInfo in assetReferencesInfo)
					{
						if (referenceInfo.assetInfo.GUID != guid) 
							continue;

						ArrayUtility.Remove(ref assetReferencesInfo, referenceInfo);
						break;
					}
				}

				if (!needToRebuildReferences) return dirty;
			}
			else
			{
				Size = -1;
			}

			foreach (var referenceInfo in assetReferencesInfo)
			{
				foreach (var info in referenceInfo.assetInfo.referencedAtInfoList)
				{
					if (!info.assetInfo.Equals(this)) continue;

					ArrayUtility.Remove(ref referenceInfo.assetInfo.referencedAtInfoList, info);
					break;
				}
			}
			
			lastHash = currentHash;
			needToRebuildReferences = true;

			assetReferencesInfo = Array.Empty<AssetReferenceInfo>();
			dependenciesGUIDs = AssetDependenciesSearcher.FindDependencies(this);
			
			return true;
		}

		internal List<AssetInfo> GetReferencesRecursive()
		{
			var result = new List<AssetInfo>();

			WalkReferencesRecursive(result, assetReferencesInfo);

			return result;
		}

		internal List<AssetInfo> GetReferencedAtRecursive()
		{
			var result = new List<AssetInfo>();

			WalkReferencedAtRecursive(result, referencedAtInfoList);

			return result;
		}

		internal void Clean()
		{
			foreach (var referenceInfo in assetReferencesInfo)
			{
				foreach (var info in referenceInfo.assetInfo.referencedAtInfoList)
				{
					if (!info.assetInfo.Equals(this)) 
						continue;
					ArrayUtility.Remove(ref referenceInfo.assetInfo.referencedAtInfoList, info);
					break;
				}
			}

			foreach (var referencedAtInfo in referencedAtInfoList)
			{
				foreach (var info in referencedAtInfo.assetInfo.assetReferencesInfo)
				{
					if (!info.assetInfo.Equals(this)) 
						continue;
					ArrayUtility.Remove(ref referencedAtInfo.assetInfo.assetReferencesInfo, info);
					referencedAtInfo.assetInfo.needToRebuildReferences = true;
					break;
				}
			}
		}

		internal int[] GetAllAssetObjects()
		{
			if (allAssetObjects != null) return allAssetObjects;

			var assetType = Type;
			var assetTypeName = assetType != null ? assetType.Name : null;

			if ((assetType == CSReflectionTools.fontType ||
				assetType == CSReflectionTools.texture2DType ||
				assetType == CSReflectionTools.gameObjectType ||
				assetType == CSReflectionTools.defaultAssetType && Path.EndsWith(".dll") ||
				assetTypeName == "AudioMixerController" ||
				Path.EndsWith("LightingData.asset")) &&
				assetType != CSReflectionTools.lightingDataAsset
			    && assetType != CSReflectionTools.lightingSettings
				)
			{
				var loadedObjects = AssetDatabase.LoadAllAssetsAtPath(Path);
				var referencedObjectsCandidatesList = new List<int>(loadedObjects.Length);
				foreach (var loadedObject in loadedObjects)
				{
					if (loadedObject == null) 
						continue;
					
					var instance = loadedObject.GetInstanceID();
					if (assetType == CSReflectionTools.gameObjectType)
					{
						var isComponent = loadedObject is Component;
						if (!isComponent && 
							!AssetDatabase.IsSubAsset(instance) && 
							!AssetDatabase.IsMainAsset(instance)) continue;
					}

					referencedObjectsCandidatesList.Add(instance);
				}

				allAssetObjects = referencedObjectsCandidatesList.ToArray();
			}
			else
			{
				var mainAsset = AssetDatabase.LoadMainAssetAtPath(Path);
				allAssetObjects = mainAsset != null ? 
					new[] { AssetDatabase.LoadMainAssetAtPath(Path).GetInstanceID() } : 
					new int[0];
			}

			return allAssetObjects;
		}

		private void WalkReferencesRecursive(List<AssetInfo> result, AssetReferenceInfo[] assetReferenceInfos)
		{
			foreach (var referenceInfo in assetReferenceInfos)
			{
				if (result.IndexOf(referenceInfo.assetInfo) == -1)
				{
					result.Add(referenceInfo.assetInfo);
					WalkReferencesRecursive(result, referenceInfo.assetInfo.assetReferencesInfo);
				}
			}
		}

		private void WalkReferencedAtRecursive(List<AssetInfo> result, ReferencedAtAssetInfo[] referencedAtInfos)
		{
			foreach (var referencedAtInfo in referencedAtInfos)
			{
				if (result.IndexOf(referencedAtInfo.assetInfo) == -1)
				{
					result.Add(referencedAtInfo.assetInfo);
					WalkReferencedAtRecursive(result, referencedAtInfo.assetInfo.referencedAtInfoList);
				}
			}
		}

		private void ActualizePath()
		{
			if (Origin == AssetOrigin.ImmutablePackage) return;

			var actualPath = CSPathTools.EnforceSlashes(AssetDatabase.GUIDToAssetPath(GUID));
			if (!string.IsNullOrEmpty(actualPath) && actualPath != Path)
				Path = actualPath;
		}

		public override string ToString()
		{
			var baseType = "N/A";
			if (Type != null && Type.BaseType != null)
				baseType = Type.BaseType.ToString();
			
			return "Asset Info\n" +
				   "Path: " + Path + "\n" +
				   "GUID: " + GUID + "\n" +
				   "Kind: " + Origin + "\n" +
				   "SettingsKind: " + SettingsKind + "\n" +
				   "Size: " + Size + "\n" +
				   "Type: " + Type + "\n" +
				   "Type.BaseType: " + baseType;
		}
		
		public bool Equals(AssetInfo other)
		{
			if (ReferenceEquals(null, other))
				return false;

			if (ReferenceEquals(this, other))
				return true;

			return GUID == other.GUID;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;

			if (ReferenceEquals(this, obj))
				return true;

			if (obj.GetType() != GetType())
				return false;

			return Equals((AssetInfo)obj);
		}

		public override int GetHashCode()
		{
			return GUID != null ? GUID.GetHashCode() : 0;
		}

		public void OnDeserialization(object sender)
		{
			// Reset size to -1 after deserialization since it's [NonSerialized]
			// This ensures Size property will recalculate the file size correctly
			size = -1;
		}
	}
}