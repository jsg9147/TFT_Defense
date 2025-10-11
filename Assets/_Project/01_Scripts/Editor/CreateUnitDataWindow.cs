// Assets/Scripts/Editor/CreateUnitDataWindow.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class CreateUnitDataWindow : EditorWindow
{
    // 저장
    private DefaultAsset saveFolder;       // Project 창에서 폴더 drag&drop
    private bool useCostSubFolder = true;  // CostXX 하위 폴더 자동 분류
    private int cost = 1;                  // 폴더명/파일명에 쓰일 코스트(선택 사항)

    // 파일/이름
    private string baseFileName = "Unit";  // ex) Unit
    private int createCount = 1;           // 한번에 생성할 개수
    private bool addIndexSuffix = true;    // _001, _002 붙일지
    private bool allowStarChar = true;     // 파일명에 '★' 허용 (Windows NTFS도 OK)

    // 데이터 필드
    private string unitDisplayName = "New Unit";
    private Sprite icon;
    private UnitType types = UnitType.SingleShot | UnitType.Physical;

    private int baseAttack = 10;
    private float attackSpeed = 1.0f;
    private float range = 3.0f;

    private GameObject projectilePrefab;

    private int multishotCount = 3;
    private float areaRadius = 1.5f;
    private int chainCount = 3;
    private float chainRange = 2.5f;

    private int poisonDamagePerTick = 2;
    private float poisonTickInterval = 0.5f;
    private int poisonTickCount = 6;

    private float buffValue = 0f;
    private float debuffValue = 0f;

    [MenuItem("Tools/Units/Create UnitData %#u")] // Shift+Ctrl/Cmd+U
    public static void Open()
    {
        var win = GetWindow<CreateUnitDataWindow>("Create UnitData");
        win.minSize = new Vector2(420, 520);
        win.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Save", EditorStyles.boldLabel);
        saveFolder = (DefaultAsset)EditorGUILayout.ObjectField("Folder", saveFolder, typeof(DefaultAsset), false);
        useCostSubFolder = EditorGUILayout.Toggle(new GUIContent("Use Cost SubFolder", "CostXX 하위 폴더 자동 분류"), useCostSubFolder);
        using (new EditorGUI.DisabledScope(!useCostSubFolder))
        {
            cost = Mathf.Clamp(EditorGUILayout.IntField("Cost", cost), 0, 99);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("File Name", EditorStyles.boldLabel);
        baseFileName = EditorGUILayout.TextField("Base", baseFileName);
        createCount = Mathf.Max(1, EditorGUILayout.IntField("Count", createCount));
        addIndexSuffix = EditorGUILayout.Toggle(new GUIContent("Add Index Suffix", "_001 접미사"), addIndexSuffix);
        allowStarChar = EditorGUILayout.Toggle(new GUIContent("Allow '★' in Name", "파일명 허용 문자. 문제시 자동 치환"), allowStarChar);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Unit Data", EditorStyles.boldLabel);
        unitDisplayName = EditorGUILayout.TextField("Unit Name(Text)", unitDisplayName);
        icon = (Sprite)EditorGUILayout.ObjectField("Icon", icon, typeof(Sprite), false);
        types = (UnitType)EditorGUILayout.EnumFlagsField("Types (Flags)", types);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Combat Stats", EditorStyles.miniBoldLabel);
        baseAttack = EditorGUILayout.IntField("Base Attack", baseAttack);
        attackSpeed = Mathf.Max(0.01f, EditorGUILayout.FloatField("Attack Speed", attackSpeed));
        range = Mathf.Max(0.1f, EditorGUILayout.FloatField("Range", range));

        EditorGUILayout.Space(4);
        projectilePrefab = (GameObject)EditorGUILayout.ObjectField("Projectile Prefab", projectilePrefab, typeof(GameObject), false);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Pattern Params", EditorStyles.miniBoldLabel);
        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUILayout.LabelField("MultiShot");
            multishotCount = Mathf.Max(1, EditorGUILayout.IntField("Count", multishotCount));

            EditorGUILayout.LabelField("Area");
            areaRadius = Mathf.Max(0.1f, EditorGUILayout.FloatField("Radius", areaRadius));

            EditorGUILayout.LabelField("Chain");
            chainCount = Mathf.Max(1, EditorGUILayout.IntField("Chain Count", chainCount));
            chainRange = Mathf.Max(0.1f, EditorGUILayout.FloatField("Chain Range", chainRange));

            EditorGUILayout.LabelField("Poison (DoT)");
            poisonDamagePerTick = Mathf.Max(0, EditorGUILayout.IntField("Damage/Tick", poisonDamagePerTick));
            poisonTickInterval = Mathf.Max(0.05f, EditorGUILayout.FloatField("Tick Interval", poisonTickInterval));
            poisonTickCount = Mathf.Max(0, EditorGUILayout.IntField("Ticks", poisonTickCount));
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Support (Future)", EditorStyles.miniBoldLabel);
        buffValue = EditorGUILayout.FloatField("Buff Value", buffValue);
        debuffValue = EditorGUILayout.FloatField("Debuff Value", debuffValue);

        EditorGUILayout.Space(10);
        using (new EditorGUI.DisabledScope(!ValidateInputs(out string reason)))
        {
            if (GUILayout.Button($"Create {createCount} UnitData Asset(s)", GUILayout.Height(32)))
            {
                CreateAssets();
            }
        }
        if (!ValidateInputs(out string reasonText))
        {
            EditorGUILayout.HelpBox(reasonText, MessageType.Warning);
        }
    }

    private bool ValidateInputs(out string reason)
    {
        if (saveFolder == null)
        {
            reason = "저장 폴더를 지정하세요.";
            return false;
        }
        var path = AssetDatabase.GetAssetPath(saveFolder);
        if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
        {
            reason = "유효한 폴더가 아닙니다.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(baseFileName))
        {
            reason = "Base 파일명을 입력하세요.";
            return false;
        }
        reason = null;
        return true;
    }

    private void CreateAssets()
    {
        string baseFolder = AssetDatabase.GetAssetPath(saveFolder);
        string targetFolder = baseFolder;

        if (useCostSubFolder)
        {
            string sub = $"Cost{cost:00}";
            if (!AssetDatabase.IsValidFolder(Path.Combine(baseFolder, sub)))
            {
                AssetDatabase.CreateFolder(baseFolder, sub);
            }
            targetFolder = Path.Combine(baseFolder, sub).Replace("\\", "/");
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        for (int i = 0; i < createCount; i++)
        {
            string fileName = baseFileName;

            if (addIndexSuffix && createCount > 1)
                fileName += $"_{i + 1:000}";

            // '★' 허용/치환
            fileName = SanitizeFileName(fileName, allowStarChar);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{targetFolder}/{fileName}.asset");

            var data = ScriptableObject.CreateInstance<UnitData>();
            data.unitName = unitDisplayName;
            data.icon = icon;
            data.types = types;

            data.baseAttack = baseAttack;
            data.attackSpeed = attackSpeed;
            data.range = range;
            data.projectilePrefab = projectilePrefab;

            data.multishotCount = multishotCount;
            data.areaRadius = areaRadius;
            data.chainCount = chainCount;
            data.chainRange = chainRange;

            data.poisonDamagePerTick = poisonDamagePerTick;
            data.poisonTickInterval = poisonTickInterval;
            data.poisonTickCount = poisonTickCount;

            data.buffValue = buffValue;
            data.debuffValue = debuffValue;

            AssetDatabase.CreateAsset(data, assetPath);
            Undo.RegisterCreatedObjectUndo(data, "Create UnitData");

            // 아이콘 썸네일 강조를 위해 저장 직후 선택
            Selection.activeObject = data;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.DisplayDialog("Create UnitData", $"생성 완료: {createCount}개", "OK");
    }

    // Windows/macOS 모두 안전한 파일명으로 교정 (원하면 ★ 허용)
    private static string SanitizeFileName(string name, bool allowStar)
    {
        // 시스템 금지 문자: \ / : * ? " < > | (Windows)
        // Unity/OS 공통으로 문제될 수 있는 문자 제거/치환
        string safe = name;

        char[] invalid = Path.GetInvalidFileNameChars();
        foreach (char c in invalid)
        {
            // 별은 선택 허용
            if (allowStar && c == '★') continue;
            safe = safe.Replace(c.ToString(), "_");
        }

        // 추가로 제어문자/탭 등 제거
        foreach (char c in safe)
        {
            if (char.IsControl(c))
                safe = safe.Replace(c.ToString(), "");
        }

        // 공백 트림
        safe = safe.Trim();

        // 빈 문자열 방지
        if (string.IsNullOrEmpty(safe)) safe = "Unit";

        return safe;
    }
}
#endif
