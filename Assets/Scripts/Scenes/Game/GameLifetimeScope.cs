using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    protected override LifetimeScope FindParent()
    {
        return Object.FindFirstObjectByType<AppLifetimeScope>();
    }

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<PlayerMotor>();
        builder.RegisterComponentInHierarchy<PlayerController>();
    }
}
