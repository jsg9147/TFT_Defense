using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartSceneController : MonoBehaviour
{
    [Header("씬 설정")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("UI 설정")]
    [SerializeField] private Button startButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startButton.onClick.AddListener(LoadGameScene);
    }
    /// <summary>
    /// GameScene으로 이동하는 메서드 (버튼에서 호출 가능)
    /// </summary>
    public void LoadGameScene()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}
