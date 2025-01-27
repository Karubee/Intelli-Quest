using System; // Required for Coroutines
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject difficultySelectPanel;
    public FlashcardManager flashcardManager;
    public GameObject pdfCollectionPanel;
    public GameObject uploadPanel;
    public PdfUploader pdfUploader;
    public GameObject loadingPanel;

    public GameObject errorPanel;
    public TextMeshProUGUI errorText;
    public float panelTransitionDelay = 0.5f; // Delay time in seconds

    public AudioSource buttonSFX; // AudioSource to play button click sound
    public AudioSource bgmSource;
    public GameObject loadingScreen;
    public GameObject signOutConfirmPanel;
    public GameObject deleteConfirmPanel;
    public GameObject howToPlayPanel;

    public GameObject settingsPanel;
    public Slider volumeSlider;
    public Slider brightnessSlider;
    public GameObject brightnessOverlay;

    void Start()
    {
        ShowMainMenuPanel();
        bgmSource.Play();
        PdfListManager.Instance.RefreshPdfList();
        Debug.Log("PDFCollectionPanel: " + pdfCollectionPanel);
        Debug.Log("UploadPanel: " + uploadPanel);
        Debug.Log("DifficultySelectPanel: " + difficultySelectPanel);
        Debug.Log("MainMenuPanel: " + mainMenuPanel);
    }

    public void ShowMainMenuPanel()
    {
        PlayButtonSound(); // Play sound on button click
        StartCoroutine(SwitchPanelWithDelay(mainMenuPanel));
    }

    public void ShowPDFCollectionPanel()
    {
        PlayButtonSound();
        FlashcardManager.Instance.HideFlashcardPanel();
        StartCoroutine(SwitchPanelWithDelay(pdfCollectionPanel));
    }

    public void ShowUploadPanel()
    {
        PlayButtonSound();
        StartCoroutine(SwitchPanelWithDelay(uploadPanel));
    }

    public void ShowDiffSelectPanel()
    {
        if (PdfListManager.Instance.SelectedPdf == null)
        {
            Debug.LogError("No PDF selected.");
            errorPanel.SetActive(true);
            errorText.text = "No PDF selected.";

            StartCoroutine(HideErrorPanelAfterDelay(2f)); // Adjust the time as needed
            return;
        }
        PlayButtonSound();
        StartCoroutine(SwitchPanelWithDelay(difficultySelectPanel));
    }

    public void ShowSettingsPanel()
    {
        PlayButtonSound();
        settingsPanel.SetActive(true);

        // Initialize volume slider
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(
                delegate
                {
                    AdjustVolume();
                }
            );
        }

        // Initialize brightness slider
        if (brightnessSlider != null)
        {
            brightnessSlider.value = 1f; // Default brightness value is 1 (fully bright)
            brightnessSlider.onValueChanged.AddListener(
                delegate
                {
                    AdjustBrightness();
                }
            );
        }
    }
    public void AdjustVolume()
    {
        if (volumeSlider != null)
        {
            AudioListener.volume = volumeSlider.value;
        }
    }
    public void AdjustBrightness()
    {
        float brightness = brightnessSlider.value; // Get the brightness value from the slider

        // Adjust the alpha of the brightness overlay based on the slider
        Color overlayColor = brightnessOverlay.GetComponent<Image>().color;
        overlayColor.a = 1f - brightness; // Invert brightness for the overlay (1 is fully transparent, 0 is fully opaque)
        brightnessOverlay.GetComponent<Image>().color = overlayColor;
    }
    public void HideSettingsPanel()
    {
        PlayButtonSound();
        settingsPanel.SetActive(false);
    }

    public void LogOutUser()
    {
        PlayButtonSound();
        AuthFirestoreManager.Instance.SignOutUser();
    }

    public void DeleteSelectedFile()
    {
        //PdfListManager.Instance.DeleteFile();
    }

    public void OnFlashcardButtonClick()
    {
        if (PdfListManager.Instance.SelectedPdf == null)
        {
            Debug.LogError("No PDF selected.");
            errorPanel.SetActive(true);
            errorText.text = "No PDF selected.";

            StartCoroutine(HideErrorPanelAfterDelay(2f)); // Adjust the time as needed
            return;
        }

        if (QAGenerator.Instance == null)
        {
            Debug.LogError("QAGenerator instance is not initialized.");
            errorPanel.SetActive(true);
            errorText.text = "Internal Server Error! Please Refresh the Page.";
            StartCoroutine(HideErrorPanelAfterDelay(2f));
            return;
        }

        PlayButtonSound();
        StartCoroutine(GenerateQuestionsAndShowFlashcards("hard"));
    }

    private IEnumerator GenerateQuestionsAndShowFlashcards(string difficulty)
    {
        loadingPanel.SetActive(true);

        yield return QAGenerator.Instance.GenerateQuestionsAndAnswers(
            PdfListManager.Instance.SelectedPdf.Pdf,
            difficulty,
            OnQuestionsGenerated
        );

        Debug.Log("Questions generated, proceeding to display flashcards.");
        loadingPanel.SetActive(false);
    }

    private void OnQuestionsGenerated()
    {

        if (QAGenerator.Instance.Questions != null && QAGenerator.Instance.Answers != null)
        {
            Debug.Log($"Received {QAGenerator.Instance.Questions.Length} questions and {QAGenerator.Instance.Answers.Length} answers");
            flashcardManager.InitializeFlashcards();
            flashcardManager.DisplayFlashcards();
        }
        else
        {
            Debug.LogError("Questions or Answers are null in QAGenerator");
        }
    }
    public void OnFlashcardButtonClicked()
    {
        StartCoroutine(GenerateQuestionsAndShowFlashcards("hard")); // You can change the difficulty as needed
    }

    public void LoadEasyLevel()
    {
        PlayButtonSound();
        PlayerPrefs.SetString("SelectedDifficulty", "easy");
        PlayerPrefs.Save();
        loadingScreen.SetActive(true);
        StartCoroutine(
            QAGenerator.Instance.GenerateQuestionsAndAnswers(
                PdfListManager.Instance.SelectedPdf.Pdf,
                "easy",
                GetLevelLoadAction("EasyLevel")
            )
        );
    }

    public void LoadMediumLevel()
    {
        PlayButtonSound();
        PlayerPrefs.SetString("SelectedDifficulty", "medium");
        PlayerPrefs.Save();
        loadingScreen.SetActive(true);
        StartCoroutine(
            QAGenerator.Instance.GenerateQuestionsAndAnswers(
                PdfListManager.Instance.SelectedPdf.Pdf,
                "medium",
                GetLevelLoadAction("MediumLevel")
            )
        );
    }

    public void LoadHardLevel()
    {
        PlayButtonSound();
        PlayerPrefs.SetString("SelectedDifficulty", "hard");
        PlayerPrefs.Save();
        loadingScreen.SetActive(true);
        StartCoroutine(
            QAGenerator.Instance.GenerateQuestionsAndAnswers(
                PdfListManager.Instance.SelectedPdf.Pdf,
                "hard",
                GetLevelLoadAction("HardLevel")
            )
        );
    }

    private Action GetLevelLoadAction(string levelName)
    {
        return () => StartCoroutine(LoadLevelWithProgress(levelName));
    }

    private Action LoadLevel(string levelName)
    {
        return () => LoadingScreenController.LoadScene(levelName);
    }

    private IEnumerator LoadLevelWithProgress(string levelName)
    {
        // Start loading the scene
        AsyncOperation operation = SceneManager.LoadSceneAsync(levelName);
        operation.allowSceneActivation = false;

        // Update loading bar as the scene loads
        while (!operation.isDone)
        {
            // The progress is between 0 and 0.9, so we scale it to full bar
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            // Once loading is complete, activate the scene
            if (operation.progress >= 0.9f)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
        loadingScreen.SetActive(false);
    }

    // Function to hide all panels
    void HideAllPanels()
    {
        mainMenuPanel.SetActive(false);
        pdfCollectionPanel.SetActive(false);
        difficultySelectPanel.SetActive(false);
        uploadPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    public IEnumerator HideErrorPanelAfterDelay(float delay)
    {
        Debug.Log("Hiding error panel after delay.");
        yield return new WaitForSeconds(delay);
        errorPanel.SetActive(false);
    }

    // Function to switch panels with a delay
    public IEnumerator SwitchPanelWithDelay(GameObject panelToShow)
    {
        // Add the delay here before switching panels
        yield return new WaitForSeconds(panelTransitionDelay);

        // Hide all panels first
        HideAllPanels();

        // Show the desired panel
        panelToShow.SetActive(true);
    }

    // Function to play the button sound effect
    void PlayButtonSound()
    {
        if (buttonSFX != null)
        {
            buttonSFX.Play();
        }
        else
        {
            Debug.LogWarning("Button SFX AudioSource not assigned.");
        }
    }
    public void OnSignOutButton()
    {
        signOutConfirmPanel.SetActive(true);
    }

    public void HideSignOutConfirmation()
    {
        signOutConfirmPanel.SetActive(false);
    }
    public void OnDeleteButton()
    {
        if (PdfListManager.Instance.SelectedPdf != null)
        {
            deleteConfirmPanel.SetActive(true);
        }
        else
        {
            errorPanel.SetActive(true);
            errorText.text = "No PDF selected.";

            StartCoroutine(HideErrorPanelAfterDelay(2f)); // Adjust the time as needed
            return;
        }
    }
    public void HideDeleteConfirmation()
    {
        deleteConfirmPanel.SetActive(false);
    }

    public void OnLoadingExitButton()
    {
        loadingScreen.SetActive(false);
    }
    public void ShowHowToPlayPanel()
    {
        PlayButtonSound();
        howToPlayPanel.SetActive(true);
    }
    public void HideHowToPlayPanel()
    {
        PlayButtonSound();
        howToPlayPanel.SetActive(false);
    }
}
