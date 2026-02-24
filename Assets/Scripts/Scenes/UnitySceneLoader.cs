using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UnitySceneLoader : ISceneLoader
{
    public string CurrentLevelSceneName { get; private set; }

    public async Task LoadLevelAsync(string sceneName, bool setActive = true)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[SceneLoader] Refusing to load an empty scene name.");
            return;
        }

        var existing = SceneManager.GetSceneByName(sceneName);
        if (!existing.IsValid() || !existing.isLoaded)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"[SceneLoader] Failed to start loading scene '{sceneName}'. Check Build Settings.");
                return;
            }

            while (!op.isDone)
                await Task.Yield();
        }

        var loaded = SceneManager.GetSceneByName(sceneName);
        if (setActive && loaded.IsValid() && loaded.isLoaded)
        {
            SceneManager.SetActiveScene(loaded);
        }

        CurrentLevelSceneName = sceneName;
    }

    public async Task UnloadSceneAsync(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        var scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        var op = SceneManager.UnloadSceneAsync(scene);
        if (op == null)
            return;

        while (!op.isDone)
            await Task.Yield();

        if (CurrentLevelSceneName == sceneName)
            CurrentLevelSceneName = null;
    }

    public async Task TransitionToLevelAsync(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        var previous = CurrentLevelSceneName;
        if (!string.IsNullOrEmpty(previous) && previous == sceneName)
        {
            var existing = SceneManager.GetSceneByName(sceneName);
            if (existing.IsValid() && existing.isLoaded)
            {
                SceneManager.SetActiveScene(existing);
                return;
            }
        }

        await LoadLevelAsync(sceneName, setActive: true);

        if (!string.IsNullOrEmpty(previous) && previous != sceneName)
        {
            await UnloadSceneAsync(previous);
        }
    }
}
