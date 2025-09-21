using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SynergyRowUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private Image icon;                 // ����: ������ �����
    [SerializeField] private TextMeshProUGUI nameText;   // "������"
    [SerializeField] private TextMeshProUGUI totalText;  // "x4" ���� �� ī��Ʈ
    [SerializeField] private TextMeshProUGUI tiersText;  // "3 > 5 > 7 > 10" (�κ� ����)

    [Header("ǥ�� �ɼ�")]
    [SerializeField] private int[] defaultThresholds = new int[] { 3, 5, 7, 10 };
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.45f);
    [SerializeField] private bool boldActive = true;        // Ȱ�� ���� ����
    [SerializeField] private bool tintNameWhenActive = false; // �� ���̶� �޼� �� name �� ƾƮ

    /// <summary>
    /// �⺻ �¾�: ��/��ī��Ʈ/Ƽ���� ǥ��. thresholds�� null�̸� defaultThresholds ���.
    /// </summary>
    public void Set(string label, int totalCount, int[] thresholds = null, Sprite setIcon = null)
    {
        if (nameText) nameText.text = label;
        if (totalText) totalText.text = $"{totalCount}";
        if (icon)
        {
            icon.sprite = setIcon;
            icon.gameObject.SetActive(setIcon != null);
        }

        var th = (thresholds == null || thresholds.Length == 0)
            ? defaultThresholds
            : thresholds.OrderBy(x => x).ToArray();

        if (tiersText)
        {
            tiersText.gameObject.SetActive(th.Length > 0);
            tiersText.text = BuildTierRichText(totalCount, th);
        }

        if (tintNameWhenActive && nameText)
        {
            bool anyActive = th.Any(t => totalCount >= t);
            nameText.color = anyActive ? activeColor : inactiveColor;
        }
    }

    /// <summary>�����ܸ� ���� �����ϰ� ���� ��</summary>
    public void SetIcon(Sprite s)
    {
        if (!icon) return;
        icon.sprite = s;
        icon.gameObject.SetActive(s != null);
    }

    // --- ���� ��ƿ ---

    private string BuildTierRichText(int total, int[] thresholds)
    {
        // �� �Ӱ谪�� total�� ���ؼ� ���� ��/��Ÿ�� ����
        var parts = thresholds.Select(t =>
        {
            bool hit = total >= t;
            var hex = ColorUtility.ToHtmlStringRGBA(hit ? activeColor : inactiveColor);
            string num = t.ToString();
            if (hit && boldActive) num = $"<b>{num}</b>";
            return $"<color=#{hex}>{num}</color>";
        });

        // �������� ȭ��ǥ(> ���̿� ����)
        return string.Join("  >  ", parts);
    }
}
