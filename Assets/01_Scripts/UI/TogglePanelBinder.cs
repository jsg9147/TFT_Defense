using UnityEngine;
using UnityEngine.UI;

public class TogglePanelBinder : MonoBehaviour
{
    [Header("연결할 토글")]
    [SerializeField] private Toggle toggle;

    [Header("토글되면 켜질 패널")]
    [SerializeField] private GameObject targetPanel;

    private void Reset()
    {
        // 자동으로 자기 자신에서 찾아옴
        if (toggle == null) toggle = GetComponent<Toggle>();
    }

    private void Awake()
    {
        if (toggle != null)
            toggle.onValueChanged.AddListener(OnToggleChanged);

        // 초기 상태 맞추기
        if (targetPanel != null)
            targetPanel.SetActive(toggle != null && toggle.isOn);
    }

    private void OnDestroy()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool isOn)
    {
        if (targetPanel != null)
            targetPanel.SetActive(isOn);
    }
}
