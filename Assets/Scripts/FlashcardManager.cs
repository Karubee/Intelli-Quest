using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class FlashcardManager : MonoBehaviour
{
    public static FlashcardManager Instance;
    public TextMeshProUGUI flashcardText;
    public GameObject flashcardPanel;
    public GameObject nextButton;
    public GameObject prevButton;

    public GameObject errorPanel;
    public TextMeshProUGUI errorText;
    private List<(string question, string answer)> questionsAndAnswers;
    private int currentIndex = 0;
    private bool isShowingAnswer = false;
    private FlashcardManager instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        flashcardPanel.SetActive(false); // Ensure panel is hidden initially
        UpdateButtonVisibility();
    }

    public void InitializeFlashcards()
    {
        if (QAGenerator.Instance != null)
        {
            string[] questionsText = QAGenerator.Instance.Questions;
            string[] answersText = QAGenerator.Instance.Answers;

            if (questionsText != null && questionsText.Length > 0 &&
                answersText != null && answersText.Length == questionsText.Length)
            {
                questionsAndAnswers = new List<(string, string)>();
                for (int i = 0; i < questionsText.Length; i++)
                {
                    questionsAndAnswers.Add((questionsText[i], answersText[i]));
                }
                Debug.Log($"Initialized {questionsAndAnswers.Count} flashcards.");
                UpdateButtonVisibility();
            }
            else
            {
                Debug.LogError("No questions or answers available from QAGenerator.");
            }
        }
        else
        {
            Debug.LogError("QAGenerator instance is not available.");
        }
    }

    public void DisplayFlashcards()
    {
        if (questionsAndAnswers != null && questionsAndAnswers.Count > 0)
        {
            ShowFlashcardPanel();
            currentIndex = 0;
            UpdateFlashcard();
            UpdateButtonVisibility();
        }
        else
        {

            Debug.LogError("No flashcards available to display.");
        }
    }

    private void UpdateButtonVisibility()
    {
        if (nextButton != null)
        {
            // Show next button only if there are more flashcards ahead
            nextButton.SetActive(questionsAndAnswers != null &&
                            questionsAndAnswers.Count > 0 &&
                            currentIndex < questionsAndAnswers.Count - 1);
        }

        if (prevButton != null)
        {
            // Show prev button only if we're not at the first flashcard
            prevButton.SetActive(questionsAndAnswers != null &&
                            questionsAndAnswers.Count > 0 &&
                            currentIndex > 0);
        }
    }

    public void ShowFlashcardPanel()
    {
        flashcardPanel.SetActive(true);
        UpdateButtonVisibility();
    }

    public void HideFlashcardPanel()
    {
        flashcardPanel.SetActive(false);
    }

    private void UpdateFlashcard()
    {
        if (currentIndex >= 0 && currentIndex < questionsAndAnswers.Count)
        {
            var flashcard = questionsAndAnswers[currentIndex];
            flashcardText.text = isShowingAnswer ? flashcard.answer : flashcard.question;
            UpdateButtonVisibility();
        }
        else
        {
            Debug.LogError($"Current index {currentIndex} is out of range. Total flashcards: {questionsAndAnswers.Count}");
        }
    }

    public void ShowNextFlashcard()
    {
        if (currentIndex < questionsAndAnswers.Count - 1)
        {
            currentIndex++;
            isShowingAnswer = false; // Reset to show question first
            UpdateFlashcard();
        }
    }

    public void ShowPreviousFlashcard()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            isShowingAnswer = false; // Reset to show question first
            UpdateFlashcard();
        }
    }

    public void ToggleAnswer()
    {
        isShowingAnswer = !isShowingAnswer;
        UpdateFlashcard();
    }

    private void OnMouseDown()
    {
        // This will toggle the answer when the text is clicked
        ToggleAnswer();
    }
}
