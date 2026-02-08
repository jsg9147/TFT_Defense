using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// RTS 스타일 카메라 패닝 매니저.
/// 마우스를 화면 가장자리로 이동하면 카메라가 해당 방향으로 이동합니다.
/// </summary>
public class CameraMovementManager : SceneSingleton<CameraMovementManager>
{
    /// <summary>카메라 이동 속도 (units/sec)</summary>
    [SerializeField] private float panSpeed = 20f;

    /// <summary>화면 가장자리 감지 두께 (pixels)</summary>
    [SerializeField] private float edgeThickness = 20f;

    /// <summary>카메라 이동 범위 최소값</summary>
    [SerializeField] private Vector2 mapMinLimit = new Vector2(-10f, -5f);

    /// <summary>카메라 이동 범위 최대값</summary>
    [SerializeField] private Vector2 mapMaxLimit = new Vector2(10f, 5f);

    private void Update()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 moveDirection = Vector3.zero;

        if (mousePos.x <= edgeThickness)
            moveDirection.x = -1f;
        else if (mousePos.x >= Screen.width - edgeThickness)
            moveDirection.x = 1f;

        if (mousePos.y <= edgeThickness)
            moveDirection.y = -1f;
        else if (mousePos.y >= Screen.height - edgeThickness)
            moveDirection.y = 1f;

        if (moveDirection == Vector3.zero) return;

        transform.position += moveDirection * panSpeed * Time.deltaTime;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, mapMinLimit.x, mapMaxLimit.x);
        pos.y = Mathf.Clamp(pos.y, mapMinLimit.y, mapMaxLimit.y);
        transform.position = pos;
    }

    /// <summary>
    /// 카메라 위치를 원점으로 초기화합니다.
    /// </summary>
    public void Reset()
    {
        transform.position = new Vector3(0f, 0f, transform.position.z);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3(
            (mapMinLimit.x + mapMaxLimit.x) * 0.5f,
            (mapMinLimit.y + mapMaxLimit.y) * 0.5f,
            0f
        );
        Vector3 size = new Vector3(
            mapMaxLimit.x - mapMinLimit.x,
            mapMaxLimit.y - mapMinLimit.y,
            0f
        );
        Gizmos.DrawWireCube(center, size);
    }
}
