using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuseumFloor : MonoBehaviour
{
    // Events
    public static EventHandler<EventArgs> teleportedFloor;

    public void TeleportedToFloor()
    {
        teleportedFloor?.Invoke(this, EventArgs.Empty);
    }
}
