using UnityEngine;
using UnityEngine.UI;

public class TogglePanelBinder : MonoBehaviour
{
    [Header("������ ���")]
    [SerializeField] private Toggle toggle;

    [Header("��۵Ǹ� ���� �г�")]
    [SerializeField] private GameObject targetPanel;

    private void Reset()
    {
        // �ڵ����� �ڱ� �ڽſ��� ã�ƿ�
        if (toggle == null) toggle = GetComponent<Toggle>();
    }

    private void Awake()
    {
        if (toggle != null)
            toggle.onValueChanged.AddListener(OnToggleChanged);

        // �ʱ� ���� ���߱�
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
