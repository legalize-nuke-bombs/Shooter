using UnityEngine;
using UnityEngine.SceneManagement;

public static class ServerBootstrap
{
    private const string GameSceneName = "SampleScene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (!Application.isBatchMode) return;

        var go = new GameObject("GameServer");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<GameServer>();

        if (SceneManager.GetActiveScene().name != GameSceneName)
            SceneManager.LoadScene(GameSceneName);
    }
}
