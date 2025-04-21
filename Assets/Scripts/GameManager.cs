using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
    public Text winnerText;
    public float fadeDuration = 1f;
    public float popupDelay = 0.3f;
    public float popupAnimationDuration = 0.75f;
    public AudioClip victorySound;
    public AudioClip popupSound;
    public GameObject raceBGM;

    [Header("UI Elements")]
    public Text timerText; // Add this with other UI references
    private float raceTime;
    private bool timerRunning;
    [Header("Countdown Settings")]
    public float countdownDuration = 60; // Default 60 seconds countdown
    public bool isCountingDown = false;
    private float countdownTimer = 60;
    public Text countdownText;

    
    private bool raceFinished = false;
    private AudioSource audioSource;
    void Start()
    {
        // Add this with other initialization
        timerText.text = "00:00:000";
        timerRunning = false;
    }
    public void StartCountdown()
    {
        countdownTimer = countdownDuration;
        isCountingDown = true;
        
        if (countdownText != null)
            countdownText.gameObject.SetActive(true);
    }

    void Update()
    {
        if (timerRunning)
        {
            raceTime += Time.deltaTime;
            UpdateTimerDisplay();
        }
        
        if (isCountingDown)
        {
            countdownTimer -= Time.deltaTime;
            
            if (countdownText != null)
            {
                int seconds = Mathf.CeilToInt(countdownTimer);
                countdownText.text = seconds.ToString();
            }
            
            // When countdown reaches zero, start the race
            if (countdownTimer <= 0)
            {
                isCountingDown = false;            
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                );
            }
        }
        
        if (raceFinished && Input.GetKeyDown(KeyCode.R))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
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

    void ResetWinScreen()
    {
        fadeOverlay.color = new Color(0, 0, 0, 0);
        fadeOverlay.gameObject.SetActive(false);
        winPopup.localScale = Vector3.zero;
        winPopup.gameObject.SetActive(false);
        winnerText.gameObject.SetActive(false);
    }

    public void HorseWon(Horse winningHorse)
    {
        if (raceFinished) return;
        
        raceFinished = true;
        StartCoroutine(WinningSequence(winningHorse));
    }

    IEnumerator WinningSequence(Horse winningHorse)
    {
        raceBGM.gameObject.SetActive(false);
        StopRaceTimer();
        isCountingDown = true;
        
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

            // Add restart option
            yield return new WaitForSeconds(1f);
            winnerText.text += "\nPress R to Restart";
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
    public void ResetRace()
    {
        // Alternative reset logic if not reloading scene
        raceFinished = false;
        ResetWinScreen();
        foreach (Horse horse in horses)
        {
            //horse.ResetHorse();
        }
    }
}