using System;
using UnityEngine;

public interface IPlayerInputSource
{
    Vector2 Move { get; }
    bool RunHeld { get; }

    event Action InteractPressed;
    event Action JumpPressed;
    event Action DashPressed;
}
