using UnityEngine;

public class UIManager : MonoSingleton<UIManager>
{
    [Header("UI �г� ����")]
    public GameObject shopPanel;
    public GameObject battlePanel;
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("UI Ǯ��")]
    public ShopSlotPool ShopSlotPool { get; private set; } // �ܺ� ���ٿ�

    protected override void Awake()
    {
        base.Awake();
        HideAllPanels();

        // ���� ShopSlotPool�� ������ �ڵ� ����
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
