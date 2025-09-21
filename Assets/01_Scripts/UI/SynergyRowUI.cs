using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SynergyRowUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private Image icon;                 // 선택: 없으면 비워둠
    [SerializeField] private TextMeshProUGUI nameText;   // "마법사"
    [SerializeField] private TextMeshProUGUI totalText;  // "x4" 같은 총 카운트
    [SerializeField] private TextMeshProUGUI tiersText;  // "3 > 5 > 7 > 10" (부분 색상)

    [Header("표시 옵션")]
    [SerializeField] private int[] defaultThresholds = new int[] { 3, 5, 7, 10 };
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.45f);
    [SerializeField] private bool boldActive = true;        // 활성 컷은 굵게
    [SerializeField] private bool tintNameWhenActive = false; // 한 컷이라도 달성 시 name 색 틴트

    /// <summary>
    /// 기본 셋업: 라벨/총카운트/티어컷 표시. thresholds가 null이면 defaultThresholds 사용.
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

    /// <summary>아이콘만 따로 세팅하고 싶을 때</summary>
    public void SetIcon(Sprite s)
    {
        if (!icon) return;
        icon.sprite = s;
        icon.gameObject.SetActive(s != null);
    }

    // --- 내부 유틸 ---

    private string BuildTierRichText(int total, int[] thresholds)
    {
        // 각 임계값을 total과 비교해서 개별 색/스타일 적용
        var parts = thresholds.Select(t =>
        {
            bool hit = total >= t;
            var hex = ColorUtility.ToHtmlStringRGBA(hit ? activeColor : inactiveColor);
            string num = t.ToString();
            if (hit && boldActive) num = $"<b>{num}</b>";
            return $"<color=#{hex}>{num}</color>";
        });

        // 가독성용 화살표(> 사이에 공백)
        return string.Join("  >  ", parts);
    }
}
