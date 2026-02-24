using System.Threading.Tasks;

public interface ISceneLoader
{
    string CurrentLevelSceneName { get; }

    Task LoadLevelAsync(string sceneName, bool setActive = true);
    Task UnloadSceneAsync(string sceneName);
    Task TransitionToLevelAsync(string sceneName);
}
