using UnityEngine;

public class UnitPlacementManager : MonoBehaviour
{
    public static UnitPlacementManager Instance;

    [Header("����")]
    public GameObject unitPrefab; // �ν��Ͻ�ȭ�� ���� ��ü (�� ������Ʈ�� SpriteRenderer + UnitInstance.cs ����)

    private UnitData selectedUnitData = null;
    private bool isPlacing = false;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// �������� ���ŵ� ������ ��ġ �غ� ���·� ����
    /// </summary>
    public void SetSelectedUnit(UnitData data)
    {
        selectedUnitData = data;
        isPlacing = true;
        Debug.Log($"{data.unitName} ��ġ �غ��");
    }

    /// <summary>
    /// ���� Ÿ���� Ŭ���Ǿ��� �� ȣ��
    /// </summary>
    public void TryPlaceUnit(Vector3 worldPos)
    {
        if (!isPlacing || selectedUnitData == null)
            return;

        Vector3Int gridPos = GridManager.Instance.WorldToCell(worldPos);
        if (!GridManager.Instance.IsPlaceable(gridPos))
        {
            Debug.Log("�ش� ��ġ�� ��ġ �Ұ�");
            return;
        }

        GameObject unit = Instantiate(unitPrefab, GridManager.Instance.CellToWorldCenter(gridPos), Quaternion.identity);
        unit.GetComponent<Unit>().Init(selectedUnitData);

        GridManager.Instance.Occupy(gridPos);
        isPlacing = false;
        selectedUnitData = null;
    }
}
