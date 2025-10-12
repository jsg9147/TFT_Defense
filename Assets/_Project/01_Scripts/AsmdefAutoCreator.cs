using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public class AsmdefAutoCreator : EditorWindow
{
    [MenuItem("Tools/Generate ASMDEF Files (Safe)")]
    public static void GenerateAsmdefs()
    {
        string basePath = "Assets/_Project/01_Scripts"; //
        string corePath = Path.Combine(basePath, "Core");
        string systemsPath = Path.Combine(basePath, "Systems");
        string uiPath = Path.Combine(basePath, "UI");
        string editorPath = Path.Combine(basePath, "Editor");

        // Core
        CreateAsmdef(corePath, "TFTDefense.Core");

        // Systems/*
        if (Directory.Exists(systemsPath))
        {
            foreach (var dir in Directory.GetDirectories(systemsPath))
            {
                string name = Path.GetFileName(dir);
                CreateAsmdef(dir, $"TFTDefense.Systems.{name}", new[] { "TFTDefense.Core" });
            }
        }

        // UI
        CreateAsmdef(uiPath, "TFTDefense.UI", new[] { "TFTDefense.Core" });

        // Editor
        CreateAsmdef(editorPath, "TFTDefense.Editor", new[] { "TFTDefense.Core" }, true);

        AssetDatabase.Refresh();
        Debug.Log("✅ 모든 ASMDEF 생성 완료!");
    }

    static void CreateAsmdef(string folderPath, string asmName, string[] refs = null, bool editorOnly = false)
    {
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            return;

        string path = Path.Combine(folderPath, $"{asmName}.asmdef");
        if (File.Exists(path))
            return;

        refs ??= new string[0];
        string refsJson = string.Join(",", refs.Select(r => $"\"{r}\""));
        if (string.IsNullOrEmpty(refsJson))
            refsJson = "";

        string platformJson = editorOnly ? "\"includePlatforms\": [\"Editor\"]," : "";

        string json = "{\n" +
                      $"  \"name\": \"{asmName}\",\n" +
                      $"  {platformJson}\n" +
                      $"  \"references\": [{refsJson}],\n" +
                      $"  \"autoReferenced\": true\n" +
                      "}";

        File.WriteAllText(path, json);
    }
}
