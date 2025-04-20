using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class RaceStarter : MonoBehaviour
{
    [Header("References")]
    public TMP_Text countdownText;
    public GameObject startingBarrier;
    public AudioSource countdownAudio;
    public AudioSource raceStartAudio;
    public AudioSource raceBGM;
    
    [Header("Settings")]
    public int countdownFrom = 3;
    public float countdownInterval = 1f;
    public float goTextDuration = 0.5f; // Duration to show "GO!" text
    
    private bool raceStarted = false;
    
    void Start()
    {
        if (countdownText == null)
        {
            Debug.LogError("Countdown Text is not assigned!");
            return;
        }
        
        countdownText.text = countdownFrom.ToString();
        StartCoroutine(CountdownSequence());
    }
    
    IEnumerator CountdownSequence()
    {
        // Initial delay before countdown starts
        yield return new WaitForSeconds(1f);
        
        // Countdown loop
        for (int i = countdownFrom; i > 0; i--)
        {
            countdownText.text = i.ToString();
            if (countdownAudio != null) countdownAudio.Play();
            yield return new WaitForSeconds(countdownInterval);
        }
        
        // Race start
        raceStarted = true;
        countdownText.text = "GO!";
        if (raceStartAudio != null) raceStartAudio.Play();
        if (raceBGM != null) raceBGM.Play();
        
        // Remove barrier
        if (startingBarrier != null)
        {
            startingBarrier.SetActive(false);
        }
        
        // Enable all horses
        foreach (Horse horse in GameManager.Instance.horses)
        {
            horse.StartMoving();
        }
        GameManager.Instance.StartRaceTimer();
        yield return new WaitForSeconds(goTextDuration);
        countdownText.text = "";
        countdownText.gameObject.SetActive(false);
    }
    
    public bool IsRaceStarted()
    {
        return raceStarted;
    }
}
