#region copyright
// -------------------------------------------------------
// Copyright (C) Dmitry Yuhanov [https://codestage.net]
// -------------------------------------------------------
#endregion

namespace CodeStage.Maintainer.Core
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;
	using Map.Storage;
	using Map.Progress;
	using Map.Processing;

	[Serializable]
	public class AssetsMap
	{
		private const string MapPath = "Library/MaintainerMap.dat";
		private static AssetsMap cachedMap;
		private static readonly IAssetsMapStorage storage = new BinaryAssetsMapStorage();
		private static readonly IProgressReporter progressReporter = new MapUpdateProgressReporter();
		private static readonly AssetsMapProcessor processor = new AssetsMapProcessor(progressReporter);

		internal readonly List<AssetInfo> assets = new List<AssetInfo>();
		public string version;

		[NonSerialized]
		internal bool isDirty;

		public static AssetsMap CreateNew(out bool canceled)
		{
			Delete();
			return GetUpdated(out canceled);
		}

		public static void Delete()
		{
			cachedMap = null;
			storage.Delete(MapPath);
		}

		public static AssetsMap GetUpdated(out bool canceled)
		{
			canceled = false;
			
			if (cachedMap == null)
				cachedMap = storage.Load(MapPath);

			if (cachedMap == null)
			{
				cachedMap = new AssetsMap {version = Maintainer.Version};
				cachedMap.isDirty = true;
			}

			try
			{
				if (processor.ProcessMap(cachedMap))
				{
					if (cachedMap.isDirty)
					{
						storage.Save(MapPath, cachedMap);
						cachedMap.isDirty = false;
					}
				}
				else
				{
					canceled = true;
					cachedMap.assets.Clear();
					cachedMap = null;
				}
			}
			catch (Exception e)
			{
				Debug.LogError(e);
			}

			return cachedMap;
		}

		public static void Save()
		{
			if (cachedMap != null)
			{
				storage.Save(MapPath, cachedMap);
			}
		}
		
		internal static void ResetReferenceEntries()
		{
			if (cachedMap == null)
				cachedMap = storage.Load(MapPath);

			if (cachedMap?.assets == null || cachedMap.assets.Count == 0)
				return;

			var dirty = false;
			
			foreach (var assetInfo in cachedMap.assets)
			{
				foreach (var info in assetInfo.referencedAtInfoList)
				{
					if (info.entries != null && info.entries.Length > 0)
					{
						dirty = true;
						info.entries = null;
					}
				}
			}
			
			if (dirty)
				storage.Save(MapPath, cachedMap);
		}

		internal static AssetInfo GetAssetInfoWithGUID(string guid, out bool canceled)
		{
			var map = cachedMap;
			canceled = false;

			map ??= GetUpdated(out canceled);

			return map?.assets.FirstOrDefault(item => item.GUID == guid);
		}
	}
}