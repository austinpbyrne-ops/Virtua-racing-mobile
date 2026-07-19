using UnityEngine;
using VRacer.Core;
using VRacer.Input;
using VRacer.Audio;
using VRacer.UI;
using VRacer.Camera;
using VRacer.Data;
using VRacer.AI;
using VRacer.GameModes;
using VRacer.Car;

/// <summary>
/// Bootstrap: initializes all manager singletons in correct order.
/// Attach this to a GameObject in the first scene.
/// </summary>
public class VRacerBootstrap : MonoBehaviour
{
    [Header("Auto-Create Managers")]
    [SerializeField] private bool autoCreate = true;

    [Header("Quality")]
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private int vSyncCount = 0;
    [SerializeField] private bool limitFrameRateOnBattery = true;

    private void Awake()
    {
        // Quality settings
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = vSyncCount;
        Screen.sleepTimeout = SleepTimeout.NeverSleep; // Keep screen on during gameplay

        if (!autoCreate) return;

        // Initialize all singleton managers in dependency order
        InitializeManager<SaveManager>("SaveManager");
        InitializeManager<AudioManager>("AudioManager");
        InitializeManager<InputManager>("InputManager");
        InitializeManager<CameraManager>("CameraManager");
        InitializeManager<RaceManager>("RaceManager");
        InitializeManager<PositionTracker>("PositionTracker");
        InitializeManager<GameManager>("GameManager");
        InitializeManager<ReplaySystem>("ReplaySystem");
        InitializeManager<HUDController>("HUDController");
        InitializeManager<MenuManager>("MenuManager");
        InitializeManager<GrandPrixManager>("GrandPrixManager");
        InitializeManager<TimeTrialManager>("TimeTrialManager");
        InitializeManager<RubberBandManager>("RubberBandManager");
    }

    private void InitializeManager<T>(string name) where T : MonoBehaviour
    {
        if (FindFirstObjectByType<T>() != null) return;

        GameObject go = new GameObject(name);
        go.AddComponent<T>();
        DontDestroyOnLoad(go);
        Debug.Log($"[Bootstrap] Created {name}");
    }

    private void Start()
    {
        Debug.Log("=== VRACER CLASSIC BOOTSTRAP COMPLETE ===");
        Debug.Log($"Target FPS: {Application.targetFrameRate}");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log($"Device: {SystemInfo.deviceModel}");
        Debug.Log($"GPU: {SystemInfo.graphicsDeviceName}");
        Debug.Log("===========================================");

        // Start music
        AudioManager.Instance?.PlayTitleMusic();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // Going to background - pause audio
            AudioManager.Instance?.StopMusic();
        }
        else
        {
            // Returning - resume
            AudioManager.Instance?.PlayTitleMusic();
        }
    }
}
