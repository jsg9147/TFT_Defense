// Assets/Scripts/Editor/CreateUnitDataWindow.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class CreateUnitDataWindow : EditorWindow
{
    // ����
    private DefaultAsset saveFolder;       // Project â���� ���� drag&drop
    private bool useCostSubFolder = true;  // CostXX ���� ���� �ڵ� �з�
    private int cost = 1;                  // ������/���ϸ� ���� �ڽ�Ʈ(���� ����)

    // ����/�̸�
    private string baseFileName = "Unit";  // ex) Unit
    private int createCount = 1;           // �ѹ��� ������ ����
    private bool addIndexSuffix = true;    // _001, _002 ������
    private bool allowStarChar = true;     // ���ϸ� '��' ��� (Windows NTFS�� OK)

    // ������ �ʵ�
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
        useCostSubFolder = EditorGUILayout.Toggle(new GUIContent("Use Cost SubFolder", "CostXX ���� ���� �ڵ� �з�"), useCostSubFolder);
        using (new EditorGUI.DisabledScope(!useCostSubFolder))
        {
            cost = Mathf.Clamp(EditorGUILayout.IntField("Cost", cost), 0, 99);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("File Name", EditorStyles.boldLabel);
        baseFileName = EditorGUILayout.TextField("Base", baseFileName);
        createCount = Mathf.Max(1, EditorGUILayout.IntField("Count", createCount));
        addIndexSuffix = EditorGUILayout.Toggle(new GUIContent("Add Index Suffix", "_001 ���̻�"), addIndexSuffix);
        allowStarChar = EditorGUILayout.Toggle(new GUIContent("Allow '��' in Name", "���ϸ� ��� ����. ������ �ڵ� ġȯ"), allowStarChar);

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
            reason = "���� ������ �����ϼ���.";
            return false;
        }
        var path = AssetDatabase.GetAssetPath(saveFolder);
        if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
        {
            reason = "��ȿ�� ������ �ƴմϴ�.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(baseFileName))
        {
            reason = "Base ���ϸ��� �Է��ϼ���.";
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

            // '��' ���/ġȯ
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

            // ������ ����� ������ ���� ���� ���� ����
            Selection.activeObject = data;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.DisplayDialog("Create UnitData", $"���� �Ϸ�: {createCount}��", "OK");
    }

    // Windows/macOS ��� ������ ���ϸ����� ���� (���ϸ� �� ���)
    private static string SanitizeFileName(string name, bool allowStar)
    {
        // �ý��� ���� ����: \ / : * ? " < > | (Windows)
        // Unity/OS �������� ������ �� �ִ� ���� ����/ġȯ
        string safe = name;

        char[] invalid = Path.GetInvalidFileNameChars();
        foreach (char c in invalid)
        {
            // ���� ���� ���
            if (allowStar && c == '��') continue;
            safe = safe.Replace(c.ToString(), "_");
        }

        // �߰��� �����/�� �� ����
        foreach (char c in safe)
        {
            if (char.IsControl(c))
                safe = safe.Replace(c.ToString(), "");
        }

        // ���� Ʈ��
        safe = safe.Trim();

        // �� ���ڿ� ����
        if (string.IsNullOrEmpty(safe)) safe = "Unit";

        return safe;
    }
}
#endif
