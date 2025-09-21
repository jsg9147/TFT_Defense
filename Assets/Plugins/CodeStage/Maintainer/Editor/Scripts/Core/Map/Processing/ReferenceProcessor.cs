#region copyright
// -------------------------------------------------------
// Copyright (C) Dmitry Yuhanov [https://codestage.net]
// -------------------------------------------------------
#endregion

namespace CodeStage.Maintainer.Core.Map.Processing
{
	using System;
	using System.Collections.Generic;
	using Progress;
	using Debug = UnityEngine.Debug;

	internal class ReferenceProcessor
	{
		private readonly IProgressReporter progressReporter;
		private Dictionary<string, AssetInfo> assetsByGuid;
		private List<AssetReferenceInfo> referenceInfosPool;
		private List<ReferencedAtAssetInfo> referencedAtInfosPool;

		public ReferenceProcessor(IProgressReporter progressReporter)
		{
			this.progressReporter = progressReporter;
		}

		public bool ProcessReferences(AssetsMap map)
		{
			if (progressReporter.ShowProgress(3, "Generating links...", 0, 1))
			{
				Debug.Log(Maintainer.ConstructLog("Assets Map update was canceled by user."));
				return false;
			}

			try
			{
				InitializeProcessing(map);
				var count = map.assets.Count;

				for (var i = 0; i < count; i++)
				{
					if (progressReporter.ShowProgress(3, "Generating links... {0}/{1}", i + 1, count))
					{
						Debug.Log(Maintainer.ConstructLog("Assets Map update was canceled by user."));
						return false;
					}

					var asset = map.assets[i];
					
					if (!asset.needToRebuildReferences) continue;

					var dependencies = asset.dependenciesGUIDs;
					if (dependencies == null || dependencies.Length == 0) continue;

					ProcessAssetDependencies(asset, dependencies);
					map.isDirty = true;
				}
			}
			finally
			{
				CleanupProcessing();
			}

			return true;
		}

		private void InitializeProcessing(AssetsMap map)
		{
			// Create GUID lookup dictionary
			assetsByGuid = new Dictionary<string, AssetInfo>(map.assets.Count);
			foreach (var asset in map.assets)
			{
				assetsByGuid[asset.GUID] = asset;
			}

			// Initialize reusable collections
			referenceInfosPool = new List<AssetReferenceInfo>(128);
			referencedAtInfosPool = new List<ReferencedAtAssetInfo>(128);
		}

		private void CleanupProcessing()
		{
			assetsByGuid = null;
			referenceInfosPool = null;
			referencedAtInfosPool = null;
		}

		private int ProcessAssetDependencies(AssetInfo asset, string[] dependencies)
		{
			var processedCount = 0;
			referenceInfosPool.Clear();

			foreach (var dependencyGuid in dependencies)
			{
				processedCount++;
				
				if (!assetsByGuid.TryGetValue(dependencyGuid, out var referencedAsset))
					continue;

				// Add forward reference
				referenceInfosPool.Add(new AssetReferenceInfo { assetInfo = referencedAsset });

				// Add backward reference
				var referencedAtInfo = new ReferencedAtAssetInfo { assetInfo = asset };
				var currentList = referencedAsset.referencedAtInfoList;
				var newList = new ReferencedAtAssetInfo[currentList.Length + 1];
				if (currentList.Length > 0)
				{
					Array.Copy(currentList, newList, currentList.Length);
				}
				newList[currentList.Length] = referencedAtInfo;
				referencedAsset.referencedAtInfoList = newList;
			}

			// Set forward references
			asset.assetReferencesInfo = referenceInfosPool.Count > 0 ? 
				referenceInfosPool.ToArray() : 
				Array.Empty<AssetReferenceInfo>();

			asset.needToRebuildReferences = false;
			return processedCount;
		}
	}
} 