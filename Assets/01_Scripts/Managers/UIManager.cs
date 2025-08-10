using UnityEngine;

public class UIManager : MonoSingleton<UIManager>
{
    [Header("UI 패널 참조")]
    public GameObject shopPanel;
    public GameObject battlePanel;
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("UI 풀링")]
    public ShopSlotPool ShopSlotPool { get; private set; } // 외부 접근용

    protected override void Awake()
    {
        base.Awake();
        HideAllPanels();

        // 씬에 ShopSlotPool이 없으면 자동 생성
        ShopSlotPool = GetComponentInChildren<ShopSlotPool>();
        if (ShopSlotPool == null)
        {
            var poolObj = new GameObject("ShopSlotPool");
            poolObj.transform.SetParent(transform);
            ShopSlotPool = poolObj.AddComponent<ShopSlotPool>();
        }
    }

    #region Panel Controls
    public void ShowShopUI()
    {
        HideAllPanels();
        shopPanel?.SetActive(true);
    }

    public void ShowBattleUI()
    {
        HideAllPanels();
        battlePanel?.SetActive(true);
    }

    public void ShowWinUI()
    {
        HideAllPanels();
        winPanel?.SetActive(true);
    }

    public void ShowLoseUI()
    {
        HideAllPanels();
        losePanel?.SetActive(true);
    }

    public void HideAllPanels()
    {
        shopPanel?.SetActive(false);
        battlePanel?.SetActive(false);
        winPanel?.SetActive(false);
        losePanel?.SetActive(false);
    }
    #endregion
}
