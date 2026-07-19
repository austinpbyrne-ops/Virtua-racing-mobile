using UnityEngine;
using UnityEngine.SceneManagement;
using VRacer.Core;

namespace VRacer.UI
{
    /// <summary>
    /// Full menu system: Title → Main Menu → Transmission → Race.
    /// Arcade-cabinet-inspired UI — bold, chunky, designed for touch.
    /// </summary>
    public class MenuManager : MonoBehaviour
    {
        [Header("Title Screen")]
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject tapToStartText;
        [SerializeField] private float tapPulseSpeed = 1.5f;

        [Header("Main Menu")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject[] menuButtons; // RACE, GRAND PRIX, TIME TRIAL, OPTIONS, HIGH SCORES

        [Header("Transmission Select")]
        [SerializeField] private GameObject transmissionPanel;

        [Header("Options")]
        [SerializeField] private GameObject optionsPanel;

        [Header("High Scores")]
        [SerializeField] private GameObject highScoresPanel;

        [Header("Track Select (for single race / time trial)")]
        [SerializeField] private GameObject trackSelectPanel;

        // State
        private bool titleActive = true;
        private float tapAlpha = 1f;
        private bool tapAlphaIncreasing = false;

        private void Start()
        {
            ShowTitleScreen();
        }

        private void Update()
        {
            // Pulse "TAP TO START"
            if (tapToStartText != null && tapToStartText.activeSelf)
            {
                PulseTapText();
            }

            // Any tap on title = go to main menu
            if (titleActive && UnityEngine.Input.GetMouseButtonDown(0))
            {
                ShowMainMenu();
            }
        }

        private void PulseTapText()
        {
            float speed = tapPulseSpeed * Time.deltaTime;
            if (tapAlphaIncreasing)
            {
                tapAlpha += speed;
                if (tapAlpha >= 1f) tapAlphaIncreasing = false;
            }
            else
            {
                tapAlpha -= speed;
                if (tapAlpha <= 0.3f) tapAlphaIncreasing = true;
            }

            var text = tapToStartText.GetComponent<UnityEngine.UI.Text>();
            if (text != null)
            {
                Color c = text.color;
                c.a = tapAlpha;
                text.color = c;
            }
        }

        // ============================================================
        // SCREEN TRANSITIONS
        // ============================================================

        public void ShowTitleScreen()
        {
            titleActive = true;
            SetActive(titlePanel, true);
            SetActive(mainMenuPanel, false);
            SetActive(transmissionPanel, false);
            SetActive(optionsPanel, false);
            SetActive(highScoresPanel, false);
            SetActive(trackSelectPanel, false);
        }

        public void ShowMainMenu()
        {
            titleActive = false;
            SetActive(titlePanel, false);
            SetActive(mainMenuPanel, true);
            AudioManager.Instance?.PlayMenuSelect();
        }

        public void ShowTransmissionSelect()
        {
            SetActive(mainMenuPanel, false);
            SetActive(transmissionPanel, true);
            AudioManager.Instance?.PlayMenuConfirm();
        }

        public void ShowOptions()
        {
            SetActive(mainMenuPanel, false);
            SetActive(optionsPanel, true);
            AudioManager.Instance?.PlayMenuSelect();
        }

        public void ShowHighScores()
        {
            SetActive(mainMenuPanel, false);
            SetActive(highScoresPanel, true);
            AudioManager.Instance?.PlayMenuSelect();
        }

        public void ShowTrackSelect()
        {
            SetActive(mainMenuPanel, false);
            SetActive(trackSelectPanel, true);
            AudioManager.Instance?.PlayMenuSelect();
        }

        // ============================================================
        // BUTTON HANDLERS (Wired to Unity UI Buttons)
        // ============================================================

        public void OnRaceButton()
        {
            AudioManager.Instance?.PlayMenuConfirm();
            ShowTrackSelect();
        }

        public void OnGrandPrixButton()
        {
            AudioManager.Instance?.PlayMenuConfirm();
            GameManager.Instance?.StartGrandPrix();
        }

        public void OnTimeTrialButton()
        {
            AudioManager.Instance?.PlayMenuConfirm();
            ShowTrackSelect(); // Then start time trial
        }

        public void OnOptionsButton()
        {
            AudioManager.Instance?.PlayMenuSelect();
            ShowOptions();
        }

        public void OnHighScoresButton()
        {
            AudioManager.Instance?.PlayMenuSelect();
            ShowHighScores();
        }

        public void OnTrackSelected(int trackIndex)
        {
            AudioManager.Instance?.PlayMenuConfirm();
            ShowTransmissionSelect();
        }

        public void OnTransmissionAutomatic()
        {
            AudioManager.Instance?.PlayMenuConfirm();
            GameManager.Instance?.OnTransmissionSelected(false);
        }

        public void OnTransmissionManual()
        {
            AudioManager.Instance?.PlayMenuConfirm();
            GameManager.Instance?.OnTransmissionSelected(true);
        }

        public void OnBackButton()
        {
            AudioManager.Instance?.PlayMenuSelect();
            ShowMainMenu();
        }

        public void OnQuitButton()
        {
            AudioManager.Instance?.PlayMenuConfirm();
            Application.Quit();
        }

        // ============================================================
        // OPTIONS HANDLERS
        // ============================================================

        public void OnTiltSensitivityChanged(float value)
        {
            Input.InputManager.Instance?.SetTiltSensitivity(value);
            Data.SaveManager.Instance?.SaveSettings();
        }

        public void OnSFXVolumeChanged(float value)
        {
            AudioManager.Instance?.SetSFXVolume(value);
            Data.SaveManager.Instance?.SaveSettings();
        }

        public void OnMusicVolumeChanged(float value)
        {
            AudioManager.Instance?.SetMusicVolume(value);
            Data.SaveManager.Instance?.SaveSettings();
        }

        // ============================================================
        // UTILITY
        // ============================================================

        private void SetActive(GameObject obj, bool active)
        {
            if (obj != null) obj.SetActive(active);
        }
    }
}
