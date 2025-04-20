using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    [Header("Race Settings")]
    public List<Horse> horses = new List<Horse>();
    
    [Header("Win Screen Settings")]
    public Image fadeOverlay;
    public RectTransform winPopup;
    public Image winnerHorseDisplay;
    public TMP_Text winnerText;
    public float fadeDuration = 1f;
    public float popupDelay = 0.3f;
    public float popupAnimationDuration = 0.75f;
    public AudioClip victorySound;
    public AudioClip popupSound;
    public GameObject raceBGM;

    [Header("Restart Settings")]
    public float autoRestartDelay = 10f;
    private float lastInteractionTime;
    private bool waitingForResetConfirmation = false;

    [Header("UI Elements")]
    public TMP_Text timerText; // Add this with other UI references
    private float raceTime;
    private bool timerRunning;

    [Header("Scoreboard Settings")]
    public TMP_Text scoreboardText;
    public RectTransform scoreboardPanel;
    private static Dictionary<string, (int wins, int races)> persistentHorseStats = 
        new Dictionary<string, (int, int)>();

    private bool raceFinished = false;
    private AudioSource audioSource;

    void Start()
    {
        UpdateScoreboard();
        timerText.text = "00:00:000";
        timerRunning = false;
    }
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        audioSource = GetComponent<AudioSource>();
        ResetWinScreen();
        if (persistentHorseStats.Count == 0)
        {
            InitializeScoreboard();
        }
        else
        {
            horseStats = new Dictionary<string, (int, int)>(persistentHorseStats);
        }
        UpdateScoreboard();
    }

    void ResetWinScreen()
    {
        fadeOverlay.color = new Color(0, 0, 0, 0);
        fadeOverlay.gameObject.SetActive(false);
        winPopup.localScale = Vector3.zero;
        winPopup.gameObject.SetActive(false);
        winnerText.gameObject.SetActive(false);
    }
    void InitializeScoreboard()
    {
        foreach (Horse horse in horses)
        {
            if (!horseStats.ContainsKey(horse.name))
            {
                horseStats.Add(horse.name, (0, 0));
            }
        }
    }
    void UpdateScoreboard()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>HORSE STATS</b>");
        sb.AppendLine("----------------");
        
        foreach (var stat in horseStats)
        {
            float ratio = stat.Value.races > 0 ? 
                (float)stat.Value.wins / stat.Value.races : 0f;
            sb.AppendLine($"{stat.Key}: {stat.Value.wins}-{stat.Value.races - stat.Value.wins} | {ratio:P0}");
        }
        
        scoreboardText.text = sb.ToString();
    }
    public void RecordRaceResult(string winnerName)
    {
        foreach (var horse in horseStats.Keys.ToList())
        {
            horseStats[horse] = (horseStats[horse].wins, horseStats[horse].races + 1);
        }
        
        if (horseStats.ContainsKey(winnerName))
        {
            horseStats[winnerName] = (horseStats[winnerName].wins + 1, horseStats[winnerName].races);
        }
        
        UpdateScoreboard();
    }

    public void HorseWon(Horse winningHorse)
    {
        if (raceFinished) return;
        
        raceFinished = true;
        StartCoroutine(WinningSequence(winningHorse));
    }
    IEnumerator WinningSequence(Horse winningHorse)
    {
        StopRaceTimer();
        RecordRaceResult(winningHorse.name);
        // Stop all horses
        foreach (Horse horse in horses)
        {
            horse.canMove = false;
            horse.rb.velocity = Vector2.zero;
            horse.rb.angularVelocity = 0f;
        }

        // Play victory sound
        if (victorySound != null && audioSource != null)
        {
            audioSource.PlayOneShot(victorySound);
        }

        // FADE SEQUENCE - Ensure this works first
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.color = new Color(0, 0, 0, 0);
            
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                float alpha = Mathf.Lerp(0, 0.85f, elapsed / fadeDuration);
                fadeOverlay.color = new Color(0, 0, 0, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }
            fadeOverlay.color = new Color(0, 0, 0, 0.85f);
        }
        else
        {
            Debug.LogError("Fade Overlay reference is missing!");
        }

        // POPUP SEQUENCE - Only proceed if fade worked
        if (winPopup != null && winnerHorseDisplay != null && winnerText != null)
        {
            // Set winner visuals
            winnerHorseDisplay.color = winningHorse.sr.color;
            winnerText.text = $"{winningHorse.name} Wins!";

            yield return new WaitForSeconds(popupDelay);

            // Activate popup parent first
            winPopup.gameObject.SetActive(true);
            winnerText.gameObject.SetActive(true);
            
            if (popupSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(popupSound);
                raceBGM.gameObject.SetActive(false);
            }

            // Animate popup
            float popupElapsed = 0f;
            while (popupElapsed < popupAnimationDuration)
            {
                float t = EaseOutBack(popupElapsed / popupAnimationDuration);
                winPopup.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
                popupElapsed += Time.deltaTime;
                yield return null;
            }
            winPopup.localScale = Vector3.one;
            lastInteractionTime = Time.time;

            // Add restart option

            yield return new WaitForSeconds(autoRestartDelay - 5f);
            if (raceFinished) // Only if still in finished state
            {
                winnerText.text += $"\nRestarting in 5 seconds... or press R";
            }
        }
        else
        {
            Debug.LogError("Popup references are missing!");
        }
    }

    // Smooth popup animation curve
    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
    }

    void Update()
    {
        if (timerRunning)
        {
            raceTime += Time.deltaTime;
            UpdateTimerDisplay();
        }

        if (raceFinished && Time.time - lastInteractionTime > autoRestartDelay)
        {
            RestartRace();
        }

        // Reset confirmation handling
        if (Input.GetKeyDown(KeyCode.R) && raceFinished)
        {
            if (!waitingForResetConfirmation)
            {
                winnerText.text = "Press R again to confirm reset";
                waitingForResetConfirmation = true;
                lastInteractionTime = Time.time; // Reset the auto-restart timer
            }
            else
            {
                ResetScoreboard();
                RestartRace();
            }
        }
        else if (Input.anyKeyDown)
        {
            lastInteractionTime = Time.time; // Reset idle timer on any key press
        }
    }

    public void RestartRace()
    {
        // Save stats before reloading
        persistentHorseStats = new Dictionary<string, (int, int)>(horseStats);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    void ResetScoreboard()
    {
        persistentHorseStats.Clear();
        horseStats.Clear();
        InitializeScoreboard();
        UpdateScoreboard();
        waitingForResetConfirmation = false;
    }
    void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(raceTime / 60);
        int seconds = Mathf.FloorToInt(raceTime % 60);
        int milliseconds = Mathf.FloorToInt((raceTime * 1000) % 1000);
        timerText.text = string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
    }

    public void StartRaceTimer()
    {
        raceTime = 0f;
        timerRunning = true;
    }

    public void StopRaceTimer()
    {
        timerRunning = false;
    }
}
