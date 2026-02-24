using UnityEngine;
using VContainer.Unity;

public static class AppBootstrap
{
    private const string PrefabPath = "Bootstrap/AppLifetimeScope";
    private static bool _initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        var prefab = Resources.Load<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError(
                $"AppLifetimeScope prefab not found at Resources/{PrefabPath}.prefab"
            );
            return;
        }

        var instance = Object.Instantiate(prefab);
        Object.DontDestroyOnLoad(instance);
    }
}