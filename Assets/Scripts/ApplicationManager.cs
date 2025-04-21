using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplicationManager : MonoBehaviour
{
    [Header("Background Running Settings")]
    [Tooltip("Enable to keep the game running when tabbed out")]
    public bool runInBackground = true;
    
    [Tooltip("Set the target framerate (use -1 for unlimited)")]
    public int targetFrameRate = 60;
    
    [Tooltip("Enable to maintain full game speed when tabbed out")]
    public bool maintainTimeScale = true;
    
    [Header("Focus Change Audio")]
    [Tooltip("Mute audio when application loses focus")]
    public bool muteWhenUnfocused = false;
    
    // Cache the original time scale
    private float originalTimeScale;
    // Cache the original audio listener volume
    private float originalAudioVolume;
    
    void Awake()
    {
        // Set whether the application should run in the background
        Application.runInBackground = runInBackground;
        
        // Set target framerate
        if (targetFrameRate > 0)
            Application.targetFrameRate = targetFrameRate;
        
        // Cache the original values
        originalTimeScale = Time.timeScale;
        originalAudioVolume = AudioListener.volume;
        
        // Add focus change event listeners
        Application.focusChanged += OnApplicationFocusChanged;
    }
    
    void OnDestroy()
    {
        // Remove event listener when object is destroyed
        Application.focusChanged -= OnApplicationFocusChanged;
    }
    
    private void OnApplicationFocusChanged(bool hasFocus)
    {
        if (!runInBackground)
            return;
            
        if (hasFocus)
        {
            // Application regained focus
            if (maintainTimeScale)
                Time.timeScale = originalTimeScale;
                
            if (muteWhenUnfocused)
                AudioListener.volume = originalAudioVolume;
        }
        else
        {
            // Application lost focus
            if (maintainTimeScale)
            {
                // Store current time scale before potentially changing it
                originalTimeScale = Time.timeScale;
                // We don't change time scale here since we want the game to continue running
            }
            
            if (muteWhenUnfocused)
            {
                // Store current volume before muting
                originalAudioVolume = AudioListener.volume;
                AudioListener.volume = 0f;
            }
        }
    }
    
    // Optional method to check if application is currently in focus
    public bool IsApplicationFocused()
    {
        return Application.isFocused;
    }
}
