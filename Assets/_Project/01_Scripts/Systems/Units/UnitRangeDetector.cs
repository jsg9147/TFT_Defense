using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class UnitRangeDetector : MonoBehaviour
{
    [Tooltip("���� �θ� Unit�� �پ� ����")]
    public Unit unit;

    [Header("�ڵ� ����")]
    public string monsterTag = "Monster";
    public string detectorLayerName = "UnitRange"; // ������Ʈ�� ���� ����

    CircleCollider2D col;
    Rigidbody2D rb;

    void Awake()
    {
        if (!unit) unit = GetComponentInParent<Unit>();
        col = GetComponent<CircleCollider2D>();
        rb = GetComponent<Rigidbody2D>();

        // �ʼ� ���� ����
        col.isTrigger = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.gravityScale = 0f;

        // ���̾� �ڵ� ����(�ɼ�)
        if (!string.IsNullOrEmpty(detectorLayerName))
        {
            int layer = LayerMask.NameToLayer(detectorLayerName);
            if (layer >= 0) gameObject.layer = layer;
        }

        // �ݰ� ����ȭ
        SyncRadius();
    }

    void OnEnable() => SyncRadius();

    public void SyncRadius()
    {
        if (unit != null && unit.data != null && col != null)
            col.radius = unit.data.range + 1;  // ������ ���� �ø��� ȣ�� ����
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!string.IsNullOrEmpty(monsterTag) && !other.CompareTag(monsterTag)) return;
        if (other.TryGetComponent(out Monster m)) unit?.AddMonsterInRange(m);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!string.IsNullOrEmpty(monsterTag) && !other.CompareTag(monsterTag)) return;
        if (other.TryGetComponent(out Monster m)) unit?.RemoveMonsterInRange(m);
    }
}
