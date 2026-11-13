using UnityEngine;
using UnityEngine.SceneManagement;

public static class XiaoqiBoot
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (SceneManager.GetActiveScene().name != "Mate Engine Main") return;
        var prefab = Resources.Load<GameObject>("XiaoqiSystems");
        if (prefab == null)
        {
            Debug.LogError("XiaoqiBoot: Resources/XiaoqiSystems.prefab missing");
            return;
        }
        var go = Object.Instantiate(prefab);
        go.name = "XiaoqiSystems";
        Object.DontDestroyOnLoad(go);
    }
}
