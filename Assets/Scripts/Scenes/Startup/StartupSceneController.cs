using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

public class StartupSceneController : MonoBehaviour
{
    [Header("Initial Scene")]
    [Tooltip("First gameplay/level scene to load from _Startup.")]
    [SerializeField] private string initialLevelScene = "GameScene";

    [Tooltip("Load the initial scene automatically on startup.")]
    [SerializeField] private bool autoLoadInitialScene = true;

    [Tooltip("Set the loaded level scene as the active scene.")]
    [SerializeField] private bool setLoadedSceneActive = true;

    [Header("Startup Scene")]
    [Tooltip("If false, _Startup stays loaded as the persistent app root while level scenes are loaded additively.")]
    [SerializeField] private bool unloadStartupSceneAfterLoad = false;

    private ISceneLoader sceneLoader;
    private bool started;

    [Inject]
    public void Construct(ISceneLoader sceneLoader)
    {
        this.sceneLoader = sceneLoader;
    }

    private void Start()
    {
        if (!autoLoadInitialScene || started)
            return;

        started = true;
        _ = LoadInitialSceneAsync();
    }

    public async Task LoadInitialSceneAsync()
    {
        if (sceneLoader == null)
        {
            Debug.LogError("[StartupSceneController] ISceneLoader was not injected. Check AppLifetimeScope setup.", this);
            return;
        }

        await sceneLoader.LoadLevelAsync(initialLevelScene, setLoadedSceneActive);

        if (unloadStartupSceneAfterLoad)
        {
            var startupScene = gameObject.scene;
            if (startupScene.IsValid() && startupScene.isLoaded)
            {
                // Only unload if another scene is active, otherwise you'll kill the app root.
                if (SceneManager.sceneCount > 1)
                {
                    await sceneLoader.UnloadSceneAsync(startupScene.name);
                }
            }
        }
    }
}
