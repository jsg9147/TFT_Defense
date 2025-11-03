using UnityEngine;

public class UIManager : MonoSingleton<UIManager>
{
    [Header("UI 패널 참조")]
    public GameObject battlePanel;
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject upgradePanel;
    public GameObject gamblePanel;


    protected override void Awake()
    {
        base.Awake();
        HideAllPanels();
    }

    #region Panel Controls

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

    public void ShowUpgradeUI()
    {
        HideAllPanels();
        upgradePanel?.SetActive(true);
    }
    public void ShowGambleUI()
    {
        HideAllPanels();
        gamblePanel?.SetActive(true);
    }

    public void HideAllPanels()
    {
        battlePanel?.SetActive(false);
        winPanel?.SetActive(false);
        losePanel?.SetActive(false);
        upgradePanel?.SetActive(false);
        gamblePanel?.SetActive(false);
    }
    #endregion
}
