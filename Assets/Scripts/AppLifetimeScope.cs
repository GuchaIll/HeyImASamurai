using UnityEngine;
using VContainer;
using VContainer.Unity;

public class AppLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<UnitySceneLoader>(Lifetime.Singleton).As<ISceneLoader>().AsSelf();
        builder
            .RegisterComponentOnNewGameObject<InputReader>(Lifetime.Singleton, nameof(InputReader))
            .UnderTransform(transform)
            .As<IPlayerInputSource>();
    }
}
