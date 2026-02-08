using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class UnitRangeDetector : MonoBehaviour
{
    [Tooltip("보통 부모에 Unit이 붙어 있음")]
    public Unit unit;

    [Header("자동 세팅")]
    public string monsterTag = "Monster";
    public string detectorLayerName = "UnitRange"; // 프로젝트에 맞춰 설정

    CircleCollider2D col;
    Rigidbody2D rb;

    void Awake()
    {
        if (!unit) unit = GetComponentInParent<Unit>(); 
        col = GetComponent<CircleCollider2D>();
        rb = GetComponent<Rigidbody2D>();

        // 필수 물리 설정
        col.isTrigger = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.gravityScale = 0f;

        // 레이어 자동 세팅(옵션)
        if (!string.IsNullOrEmpty(detectorLayerName))
        {
            int layer = LayerMask.NameToLayer(detectorLayerName);
            if (layer >= 0) gameObject.layer = layer;
        }

        // 반경 동기화
        SyncRadius();
    }

    void OnEnable() => SyncRadius();

    public void SyncRadius()
    {
        if (unit != null && unit.data != null && col != null)
            col.radius = unit.data.range + 1;  // 데이터 변경 시마다 호출 가능
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

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        DrawRangeGizmo(0.4f);
    }

    void OnDrawGizmos()
    {
        DrawRangeGizmo(0.15f);
    }

    void DrawRangeGizmo(float alpha)
    {
        var c = GetComponent<CircleCollider2D>();
        if (c == null) return;

        float radius = c.radius;
        if (unit != null && unit.data != null)
            radius = unit.data.range + 1f;

        Vector3 center = transform.position;
        Gizmos.color = new Color(0f, 1f, 1f, alpha);
        Gizmos.DrawWireSphere(center, radius);
    }
#endif
}
