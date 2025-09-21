#region copyright
// -------------------------------------------------------
// Copyright (C) Dmitry Yuhanov [https://codestage.net]
// -------------------------------------------------------
#endregion

namespace CodeStage.Maintainer.Core.Map.Storage
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
	using EditorCommon.Tools;
	using UnityEditor;
    using UnityEngine;

    internal class BinaryAssetsMapStorage : IAssetsMapStorage
    {
        public AssetsMap Load(string path)
        {
            if (!File.Exists(path)) 
                return null;

            var fileSize = new FileInfo(path).Length;
            if (fileSize > 500000)
                EditorUtility.DisplayProgressBar("Loading Assets Map", "Please wait...", 0);

            AssetsMap result = null;
            var bf = new BinaryFormatter();
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            try
            {
                result = bf.Deserialize(stream) as AssetsMap;

                if (result != null && result.version != Maintainer.Version)
                {
                    result = null;
                }
            }
            catch (Exception)
            {
                Debug.Log(Maintainer.ConstructLog("Couldn't read assets map (more likely you've updated Maintainer recently).\nThis message is harmless unless repeating on every Maintainer run."));
            }
            finally
            {
                stream.Close();
            }

            EditorUtility.ClearProgressBar();
            return result;
        }

        public void Save(string path, AssetsMap map)
        {
            if (map.assets.Count > 10000)
            {
                EditorUtility.DisplayProgressBar("Saving Assets Map", "Please wait...", 0);
            }

            var bf = new BinaryFormatter();
            var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            bf.Serialize(stream, map);
            stream.Close();

            EditorUtility.ClearProgressBar();
        }

        public void Delete(string path)
        {
            CSFileTools.DeleteFile(path);
        }
    }
} 