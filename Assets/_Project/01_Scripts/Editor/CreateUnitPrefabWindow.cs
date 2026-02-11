#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 템플릿 프리팹을 복제하여 유닛 프리팹을 자동 생성하고 UnitData와 양방향 연결하는 에디터 도구.
/// </summary>
public class CreateUnitPrefabWindow : EditorWindow
{
    private const string DefaultTemplatePath = "Assets/_Project/02_Prefabs/UnitPrefab.prefab";
    private const string DefaultSaveFolderPath = "Assets/_Project/02_Prefabs/SpumPrefabs/Unit";

    // === Template ===
    [SerializeField] private GameObject templatePrefab;

    // === UnitData 리스트 ===
    [SerializeField] private List<UnitData> unitDatas = new();
    private Vector2 listScrollPos;

    // === Save ===
    [SerializeField] private DefaultAsset saveFolder;
    [SerializeField] private bool useCostSubFolder = true;

    // === Options ===
    [SerializeField] private bool autoBackLink = true;
    [SerializeField] private bool renameRoot = true;

    // ──────────────────────────── Menu Items ────────────────────────────

    [MenuItem("Tools/Units/Create Unit Prefab")]
    public static void Open()
    {
        var win = GetWindow<CreateUnitPrefabWindow>("Create Unit Prefab");
        win.minSize = new Vector2(420, 380);
        win.LoadDefaults();
        win.Show();
    }

    [MenuItem("Assets/Create Unit Prefab from UnitData", true)]
    private static bool ValidateContextMenu()
    {
        return Selection.objects.Any(o => o is UnitData);
    }

    [MenuItem("Assets/Create Unit Prefab from UnitData")]
    private static void CreateFromContext()
    {
        var win = GetWindow<CreateUnitPrefabWindow>("Create Unit Prefab");
        win.LoadDefaults();
        win.unitDatas = Selection.objects.OfType<UnitData>().ToList();
        win.Show();
    }

    // ──────────────────────────── Defaults ────────────────────────────

    private void LoadDefaults()
    {
        if (templatePrefab == null)
            templatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultTemplatePath);

        if (saveFolder == null)
            saveFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DefaultSaveFolderPath);
    }

    // ──────────────────────────── GUI ────────────────────────────

    private void OnGUI()
    {
        // --- Template ---
        EditorGUILayout.LabelField("Template", EditorStyles.boldLabel);
        templatePrefab = (GameObject)EditorGUILayout.ObjectField(
            "Template Prefab", templatePrefab, typeof(GameObject), false);

        if (templatePrefab != null && templatePrefab.GetComponent<Unit>() == null)
            EditorGUILayout.HelpBox("Template에 Unit 컴포넌트가 없습니다.", MessageType.Error);

        // --- UnitData List ---
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("UnitData List", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Selected UnitData(s) from Project"))
        {
            foreach (var obj in Selection.objects.OfType<UnitData>())
            {
                if (!unitDatas.Contains(obj))
                    unitDatas.Add(obj);
            }
        }

        listScrollPos = EditorGUILayout.BeginScrollView(listScrollPos, GUILayout.MaxHeight(180));
        for (int i = 0; i < unitDatas.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            unitDatas[i] = (UnitData)EditorGUILayout.ObjectField(
                unitDatas[i], typeof(UnitData), false);

            if (GUILayout.Button("-", GUILayout.Width(24)))
            {
                unitDatas.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+", GUILayout.Width(24)))
            unitDatas.Add(null);
        if (unitDatas.Count > 0 && GUILayout.Button("Clear", GUILayout.Width(50)))
            unitDatas.Clear();
        EditorGUILayout.EndHorizontal();

        // --- Save ---
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Save", EditorStyles.boldLabel);
        saveFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Folder", saveFolder, typeof(DefaultAsset), false);
        useCostSubFolder = EditorGUILayout.Toggle(
            new GUIContent("Use Cost SubFolder", "UnitData.cost 기준 CostXX 하위 폴더 자동 생성"),
            useCostSubFolder);

        // --- Options ---
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
        autoBackLink = EditorGUILayout.Toggle(
            new GUIContent("Auto Back-Link", "UnitData.unitPrefab에 생성된 프리팹 자동 연결"),
            autoBackLink);
        renameRoot = EditorGUILayout.Toggle(
            new GUIContent("Rename Root", "루트 GameObject를 UnitData 이름으로 변경"),
            renameRoot);

        // --- Validation ---
        EditorGUILayout.Space(10);
        bool valid = ValidateAll(out string reason);

        using (new EditorGUI.DisabledScope(!valid))
        {
            int count = unitDatas.Count(d => d != null);
            if (GUILayout.Button($"Create {count} Unit Prefab(s)", GUILayout.Height(32)))
                CreatePrefabs();
        }

        if (!valid)
            EditorGUILayout.HelpBox(reason, MessageType.Warning);
    }

    // ──────────────────────────── Validation ────────────────────────────

    private bool ValidateAll(out string reason)
    {
        if (templatePrefab == null)
        { reason = "Template 프리팹을 지정하세요."; return false; }

        if (templatePrefab.GetComponent<Unit>() == null)
        { reason = "Template에 Unit 컴포넌트가 없습니다."; return false; }

        if (unitDatas.Count == 0 || unitDatas.All(d => d == null))
        { reason = "UnitData를 하나 이상 추가하세요."; return false; }

        if (saveFolder == null)
        { reason = "저장 폴더를 지정하세요."; return false; }

        string path = AssetDatabase.GetAssetPath(saveFolder);
        if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
        { reason = "유효한 폴더가 아닙니다."; return false; }

        reason = null;
        return true;
    }

    // ──────────────────────────── Creation ────────────────────────────

    private void CreatePrefabs()
    {
        var targets = unitDatas.Where(d => d != null).ToList();
        if (targets.Count == 0) return;

        string baseFolder = AssetDatabase.GetAssetPath(saveFolder);
        bool showProgress = targets.Count > 3;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        int created = 0;
        int skipped = 0;

        try
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var data = targets[i];

                if (showProgress)
                {
                    float progress = (float)i / targets.Count;
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Creating Unit Prefabs",
                        $"({i + 1}/{targets.Count}) {data.name}",
                        progress))
                    {
                        break; // 사용자가 취소
                    }
                }

                if (CreateSinglePrefab(data, baseFolder))
                    created++;
                else
                    skipped++;
            }
        }
        finally
        {
            if (showProgress)
                EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Undo.CollapseUndoOperations(undoGroup);

        string msg = $"생성 완료: {created}개";
        if (skipped > 0) msg += $", 건너뜀: {skipped}개";
        EditorUtility.DisplayDialog("Create Unit Prefab", msg, "OK");
    }

    private bool CreateSinglePrefab(UnitData data, string baseFolder)
    {
        // 1) 파일명 결정
        string fileName = SanitizeFileName(data.name);

        // 2) CostXX 폴더
        string targetFolder = baseFolder;
        if (useCostSubFolder)
        {
            int cost = Mathf.Clamp(data.cost, 0, 99);
            string sub = $"Cost{cost:00}";
            string subPath = $"{baseFolder}/{sub}";
            if (!AssetDatabase.IsValidFolder(subPath))
                AssetDatabase.CreateFolder(baseFolder, sub);
            targetFolder = subPath;
        }

        // 3) 기존 프리팹 존재 확인
        string assetPath = $"{targetFolder}/{fileName}.prefab";
        bool exists = File.Exists(assetPath);

        // 4) UnitData.unitPrefab 이미 연결된 경우 경고
        if (autoBackLink && data.unitPrefab != null)
        {
            string existingPath = AssetDatabase.GetAssetPath(data.unitPrefab);
            if (!string.IsNullOrEmpty(existingPath) && existingPath != assetPath)
            {
                int choice = EditorUtility.DisplayDialogComplex(
                    "unitPrefab Already Linked",
                    $"'{data.name}'에 이미 프리팹이 연결되어 있습니다:\n{existingPath}\n\n새 프리팹으로 교체하시겠습니까?",
                    "교체", "건너뛰기", "");
                if (choice == 1) return false; // 건너뛰기
            }
        }

        if (exists)
        {
            if (!EditorUtility.DisplayDialog(
                "Overwrite?",
                $"'{fileName}.prefab'이 이미 존재합니다. 덮어쓰시겠습니까?",
                "덮어쓰기", "건너뛰기"))
                return false;
        }

        // 5) 템플릿 인스턴스 생성
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(templatePrefab);

        try
        {
            // 6) 루트 이름 변경
            if (renameRoot)
                instance.name = fileName;

            // 7) Unit 컴포넌트 설정
            var unit = instance.GetComponent<Unit>();
            if (unit != null)
            {
                unit.data = data;

                if (unit.bulletPrefab == null && data.projectilePrefab != null)
                    unit.bulletPrefab = data.projectilePrefab;
            }

            // 8) 프리팹 저장
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, assetPath);

            // 9) UnitData 역연결
            if (autoBackLink && savedPrefab != null)
            {
                var so = new SerializedObject(data);
                so.FindProperty("unitPrefab").objectReferenceValue = savedPrefab;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(data);
            }

            Selection.activeObject = savedPrefab;
            return true;
        }
        finally
        {
            // 10) 씬 인스턴스 반드시 정리
            if (instance != null)
                DestroyImmediate(instance);
        }
    }

    // ──────────────────────────── Utility ────────────────────────────

    private static string SanitizeFileName(string name)
    {
        string safe = name;

        foreach (char c in Path.GetInvalidFileNameChars())
            safe = safe.Replace(c.ToString(), "_");

        foreach (char c in safe)
        {
            if (char.IsControl(c))
                safe = safe.Replace(c.ToString(), "");
        }

        safe = safe.Trim();
        if (string.IsNullOrEmpty(safe)) safe = "Unit";

        return safe;
    }
}
#endif
