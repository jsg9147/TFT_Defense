using UnityEngine;

/// <summary>
/// 씬 전역 싱글톤 클래스 (DontDestroyOnLoad 미사용)
/// 씬이 바뀌면 인스턴스가 파괴되며, 새 씬에서 다시 생성됩니다.
/// </summary>
public abstract class SceneSingleton<T> : MonoBehaviour where T : SceneSingleton<T>
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<T>();

                if (instance == null)
                {
                    GameObject obj = new GameObject(typeof(T).Name);
                    instance = obj.AddComponent<T>();
                }
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = (T)this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}

