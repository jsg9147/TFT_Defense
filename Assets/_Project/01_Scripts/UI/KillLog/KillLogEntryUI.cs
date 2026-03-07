using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.KillLog
{

    /// <summary>
    /// 킬 로그 패널의 항목 하나. 유닛 이름, 누적 데미지, 기여도(%)를 표시한다.
    /// </summary>
    public class KillLogEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private TextMeshProUGUI percentText;
        [SerializeField] private Image fillBar;

        /// <summary>항목 데이터를 설정한다.</summary>
        /// <param name="unitName">유닛 이름</param>
        /// <param name="damage">해당 유닛의 누적 피해량</param>
        /// <param name="totalDamage">전체 피해량 합계 (기여도 계산용)</param>
        public void SetData(string unitName, int damage, int totalDamage)
        {
            if (nameText) nameText.text = unitName;
            if (damageText) damageText.text = damage.ToString("N0");

            float ratio = totalDamage > 0 ? (float)damage / totalDamage : 0f;
            if (percentText) percentText.text = $"{ratio * 100f:F1}%";
            if (fillBar) fillBar.fillAmount = ratio;
        }
    }

}
