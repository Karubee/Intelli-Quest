using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResolutionManagerScript : MonoBehaviour
{
    private static bool _initialized = false;

    void Awake()
    {
        // Check if this is the first instance of the GameObject
        if (!_initialized)
        {
            // Set resolution
            Screen.SetResolution(960, 540, false);
            DontDestroyOnLoad(gameObject); // Keep this GameObject across all scenes
            _initialized = true;
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }
}
