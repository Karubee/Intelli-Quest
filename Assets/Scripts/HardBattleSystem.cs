using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum HardBattleState
{
    START,
    PLAYERTURN,
    ENEMYTURN,
    WON,
    LOST,
    GAMEOVER,
    PAUSED,
}

public class HardBattleSystem : MonoBehaviour
{
    public GameObject playerPrefab;
    public GameObject[] enemyPrefabs; // Array to hold different enemy prefabs

    public Transform playerBattleStation;
    public Transform enemyBattleStation;
    Unit playerUnit;
    Unit enemyUnit;

    public TextMeshProUGUI dialogueText;
    public Text timerText;
    public TextMeshProUGUI questionText;
    public InputField answerInput;
    public Button attackButton;

    public BattleHUD playerHUD;
    public BattleHUD enemyHUD;

    public HardBattleState state;

    public float turnDuration; // player's turn duration
    private float timer; // current timer value
    public Animator playerAnimator;
    public Animator[] enemyAnimators; // Array to hold animators for each enemy

    private int enemyIndex = 0; // To keep track of the current enemy
    private int enemiesDefeated = 0; // To keep track of the number of enemies defeated
    public int maxEnemiesToDefeat = 5; // Number of enemies to defeat to win

    private List<(string, string)> questionsAndAnswers; // List of questions and their correct answers
    private string currentAnswer; // The current correct answer

    // Reference to UI Elements for Game Over and Pause Screens
    public GameObject gameOverPanel;
    public GameObject pausePanel;
    public GameObject mainMenuConfirmPanel;

    public Slider volumeSlider; // Reference to volume slider
    public Slider brightnessSlider; // Reference to brightness slider
    public Image brightnessOverlay; // Reference to brightness overlay

    public AudioSource bgmSource;
    public float pausedVolume = 0;
    public float normalVolume = 0.3f;

    public AudioClip playerAttackSFX; // Player's attack sound
    public AudioClip playerDeathSFX; // Player's death sound
    public AudioSource playerAudioSource; // Player's audio source
    public AudioClip correctAnswerSFX;
    public AudioClip incorrectAnswerSFX;

    public AudioClip[] enemyAttackSFX;
    public AudioClip[] enemyDeathSFX;
    public AudioSource[] enemyAudioSources;

    public AudioClip victoryMusic; // Music that plays when you win
    public AudioClip defeatMusic; // Music that plays when you lose

    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI gameOverScoreText;
    private int score = 0;

    private bool isPlayerAttacking = false;

    public GameObject settingsPanel;
    public int selectedEnemyCount = 5; // Default to 5 enemy count
    public Button[] enemyCountButtons; // Array to hold the enemy count buttons
    public TMP_InputField turnDurationInputField;

    public Color defaultButtonColor = Color.white;
    public Color selectedButtonColor = Color.green;

    public TextMeshProUGUI playerPointsText; // Text to show points awarded
    public TextMeshProUGUI playerDamageText; // Text to show damage dealt
    public TextMeshProUGUI enemyDamageText; // Text to show enemy damage

    void Start()
    {
        // Initialize the sliders with default values
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
        settingsPanel.SetActive(true);
        bgmSource.volume = normalVolume;
        bgmSource.Play();
    }

    public void ConfirmSettingsAndStart()
    {
        float turnTime = 0;
        // Try to parse the turn duration input
        if (float.TryParse(turnDurationInputField.text, out turnTime) && turnTime > 0)
        {
            turnDuration = turnTime;
        }
        else
        {
            Debug.LogWarning("Invalid turn duration. Setting to default (30 seconds).");
            turnDuration = 30f;
        }
        maxEnemiesToDefeat = selectedEnemyCount;
        settingsPanel.SetActive(false); // Hide settings panel after confirmation
        state = HardBattleState.START;
        score = 0;
        UpdateScoreText();
        StartCoroutine(WaitForQuestions()); // Start the battle setup
    }


    IEnumerator WaitForQuestions()
    {
        // Wait until PdfUploader has finished processing the PDF
        while (
            QAGenerator.Instance.Questions == null || QAGenerator.Instance.Questions.Count() <= 0
        )
        {
            yield return new WaitForSeconds(0.5f); // Check every half second
        }

        InitializeQuestions();
        StartCoroutine(SetupBattle());
    }

    void Update()
    {
        if (state == HardBattleState.PLAYERTURN && !isPlayerAttacking)
        {
            timer -= Time.deltaTime;

            if (timerText != null)
            {
                timerText.text = Mathf.Ceil(timer).ToString();
            }

            if (timer <= 0)
            {
                EndPlayerTurn();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    void InitializeQuestions()
    {
        Debug.Log("QA Instance: " + QAGenerator.Instance);
        // Debug.Log("QuestionText: " + QAGenerator.Instance.Questions);
        // Debug.Log("AnswerText: " + QAGenerator.Instance.Answers);

        string[] questionsText = QAGenerator.Instance.Questions;
        string[] answersText = QAGenerator.Instance.Answers;

        if (
            questionsText != null
            && questionsText.Length > 0
            && answersText != null
            && answersText.Length == questionsText.Length
        )
        {
            // Assuming the questions and answers arrays are the same length
            questionsAndAnswers = new List<(string, string)>();
            for (int i = 0; i < questionsText.Length; i++)
            {
                questionsAndAnswers.Add((questionsText[i], answersText[i]));
            }
        }
        else
        {
            Debug.LogError("No questions or answers available from PdfUploader.");
        }
    }

    IEnumerator SetupBattle()
    {
        if (playerUnit == null)
        {
            GameObject playerGO = Instantiate(playerPrefab, playerBattleStation);
            playerUnit = playerGO.GetComponent<Unit>();
        }

        if (enemyUnit != null)
        {
            Destroy(enemyUnit.gameObject);
        }

        // Instantiate the current enemy at the enemyBattleStation
        GameObject currentEnemy = enemyPrefabs[enemyIndex];
        currentEnemy.transform.position = enemyBattleStation.position;
        currentEnemy.transform.SetParent(enemyBattleStation);
        enemyUnit = currentEnemy.GetComponent<Unit>();

        dialogueText.text = "A wandering " + enemyUnit.unitName + " approaches...";

        playerHUD.SetHUD(playerUnit);
        enemyHUD.SetHUD(enemyUnit);

        yield return new WaitForSeconds(3f);

        state = HardBattleState.PLAYERTURN;
        StartPlayerTurn();
    }

    void StartPlayerTurn()
    {
        timer = turnDuration;

        if (timerText != null)
        {
            timerText.text = Mathf.Ceil(timer).ToString();
        }

        dialogueText.text = "Answer the question to attack:";
        DisplayRandomQuestion();

        attackButton.interactable = true;
        isPlayerAttacking = false;
    }

    void DisplayRandomQuestion()
    {
        int randomIndex = Random.Range(0, questionsAndAnswers.Count - 1);
        var question = questionsAndAnswers[randomIndex];
        questionText.text = question.Item1;
        currentAnswer = question.Item2;
        answerInput.text = ""; // Clear the input field for the new question
    }

    void EndPlayerTurn()
    {
        isPlayerAttacking = true;
        dialogueText.text = "The enemy prepares to attack...";

        state = HardBattleState.ENEMYTURN;
        StartCoroutine(EnemyTurn());
    }

    IEnumerator PlayerAttack()
    {
        if (playerAnimator != null)
        {
            isPlayerAttacking = true;
            playerAnimator.SetTrigger("AttackW");
            playerAudioSource.PlayOneShot(playerAttackSFX);
        }

        // Calculate damage based on remaining time
        float damageMultiplier = timer / turnDuration;
        int damage = Mathf.CeilToInt(playerUnit.damage * damageMultiplier);
        ShowPlayerAttackText(damage);

        // Player deals damage to the enemy
        bool isDead = enemyUnit.TakeDamage(damage);

        enemyHUD.SetHP(enemyUnit.currentHP);
        dialogueText.text = "The attack is successful! Damage dealt: " + damage;

        yield return new WaitForSeconds(3f);

        // If the enemy is dead
        if (isDead)
        {
            enemyAudioSources[enemyIndex].PlayOneShot(enemyDeathSFX[enemyIndex]);
            enemyAnimators[enemyIndex].SetTrigger("DeathTrigger");
            yield return new WaitForSeconds(2f);
            Destroy(enemyUnit.gameObject, 2.5f);
            enemiesDefeated++;
            HealPlayer(0.2f);
            state = HardBattleState.WON;
            EndBattle();
        }
        else
        {
            // Skip enemy's turn if the player's attack was successful
            state = HardBattleState.PLAYERTURN;
            dialogueText.text =
                "Your attack was successful, and the enemy will not counter-attack!";
            yield return new WaitForSeconds(1f);
            isPlayerAttacking = false;
            StartPlayerTurn();
        }
    }

    IEnumerator EnemyTurn()
    {
        if (state == HardBattleState.ENEMYTURN)
        {
            dialogueText.text = enemyUnit.unitName + " attacks!";

            if (enemyAnimators[enemyIndex] != null)
            {
                enemyAnimators[enemyIndex].SetTrigger("AttackTrigger");
                enemyAudioSources[enemyIndex].PlayOneShot(enemyAttackSFX[enemyIndex]);
            }

            yield return new WaitForSeconds(1f);

            bool isDead = playerUnit.TakeDamage(enemyUnit.damage);
            ShowEnemyDamageText(enemyUnit.damage);

            playerHUD.SetHP(playerUnit.currentHP);

            yield return new WaitForSeconds(1f);

            if (isDead)
            {
                state = HardBattleState.LOST;
                EndBattle();
            }
            else
            {
                state = HardBattleState.PLAYERTURN;
                isPlayerAttacking = false;
                StartPlayerTurn();
            }
        }
    }

    void EndBattle()
    {
        if (state == HardBattleState.WON)
        {
            if (enemiesDefeated >= maxEnemiesToDefeat)
            {
                state = HardBattleState.GAMEOVER;
                StartCoroutine(FadeOutBGM(2.0f)); // Fade out background music over 2 seconds
                dialogueText.text = "You defeated all the enemies! You won the game!";
                StartCoroutine(ShowGameOverWithDelayVictory());
            }
            else
            {
                dialogueText.text = "You won the battle!";
                enemyIndex = (enemyIndex + 1) % enemyPrefabs.Length;
                state = HardBattleState.START;
                StartCoroutine(SetupBattle());
            }
        }
        else if (state == HardBattleState.LOST)
        {
            state = HardBattleState.GAMEOVER;
            playerAudioSource.PlayOneShot(playerDeathSFX);
            playerAnimator.SetTrigger("DeathW");
            StartCoroutine(FadeOutBGM(2.0f)); // Fade out background music over 2 seconds
            dialogueText.text = "You were defeated. Game Over.";
            StartCoroutine(ShowGameOverWithDelayDefeat());
        }
    }

    void ShowCorrectAnswer()
    {
        // Show the correct answer in the dialogue text
        isPlayerAttacking = true;
        dialogueText.text = "Incorrect! The correct answer was: " + currentAnswer;
        answerInput.text = ""; // Clear the input field
        StartCoroutine(WaitBeforeEnemyTurn()); // Wait before the enemy turn
    }

    IEnumerator WaitBeforeEnemyTurn()
    {
        yield return new WaitForSeconds(3f); // Give the player 2 seconds to see the correct answer
        EndPlayerTurn(); // Proceed to enemy's turn
    }

    private IEnumerator ShowGameOverWithDelayVictory()
    {
        yield return new WaitForSeconds(2.0f);
        bgmSource.Stop(); // Stop the background music
        PlayVictoryMusic(); // Play victory music when you win
        ShowGameOverScreen();
    }

    private IEnumerator ShowGameOverWithDelayDefeat()
    {
        yield return new WaitForSeconds(2.0f);
        bgmSource.Stop(); // Stop the background music
        PlayDefeatMusic(); // Play defeat music when you lose
        ShowGameOverScreen();
    }

    void HealPlayer(float healPercentage)
    {
        int healAmount = Mathf.CeilToInt(playerUnit.maxHP * healPercentage);
        playerUnit.Heal(healAmount);
        playerHUD.SetHP(playerUnit.currentHP);
        dialogueText.text =
            "You feel renewed strength! Health restored by " + healAmount + " points.";
    }

    public void OnAttackButton()
    {
        if (state != HardBattleState.PLAYERTURN)
            return;
        attackButton.interactable = false;
        isPlayerAttacking = true; // Stop the timer
        if (
            answerInput.text.Trim().Equals(currentAnswer, System.StringComparison.OrdinalIgnoreCase)
        )
        {
            playerAudioSource.PlayOneShot(correctAnswerSFX);
            isPlayerAttacking = true;
            int pointsAwarded = Mathf.CeilToInt(10 + (10 * (timer / turnDuration))); // 10-20 points depending on remaining time
            score += pointsAwarded; // Add points to the score
            UpdateScoreText(); // Update the score display
            StartCoroutine(PlayerAttack());
        }
        else
        {
            playerAudioSource.PlayOneShot(incorrectAnswerSFX);
            isPlayerAttacking = true;
            score -= 10;
            score = Mathf.Max(score, 0);
            UpdateScoreText(); // Update the score display
            ShowCorrectAnswer();
        }
    }

    public void ShowGameOverScreen()
    {
        gameOverScoreText.text = "Score: " + score;
        gameOverPanel.SetActive(true);
    }

    public void HideGameOverScreen()
    {
        gameOverPanel.SetActive(false);
    }

    public void ShowPauseScreen()
    {
        pausePanel.SetActive(true);
        state = HardBattleState.PAUSED;
    }

    public void HidePauseScreen()
    {
        pausePanel.SetActive(false);
        state = HardBattleState.PLAYERTURN;
    }

    public void TogglePause()
    {
        if (state == HardBattleState.PAUSED)
        {
            Time.timeScale = 1f;
            HidePauseScreen();
            bgmSource.mute = false;
        }
        else
        {
            Time.timeScale = 0f;
            ShowPauseScreen();
            bgmSource.mute = true;
        }
    }

    void UpdateScoreText()
    {
        scoreText.text = "Score: " + score;
    }

    public void OnRestartButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

      public void OnMainMenuButton()
    {
        mainMenuConfirmPanel.SetActive(true);
    }

    public void OnMainMenuConfirm()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void HideMenuConfirmation()
    {
        mainMenuConfirmPanel.SetActive(false);
    }

    public void OnContinueButton()
    {
        HidePauseScreen();
    }

    void PlayVictoryMusic()
    {
        bgmSource.clip = victoryMusic; // Assign victory music to the game over audio source
        bgmSource.Play(); // Play the victory music
    }

    void PlayDefeatMusic()
    {
        bgmSource.clip = defeatMusic; // Assign defeat music to the game over audio source
        bgmSource.Play(); // Play the defeat music
    }

    IEnumerator FadeOutBGM(float fadeDuration)
    {
        float startVolume = bgmSource.volume;

        while (bgmSource.volume > 0)
        {
            bgmSource.volume -= startVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.volume = startVolume; // Reset the volume
    }

    public void CheckInput()
    {
        // Show the button if the input field is not empty, hide it otherwise
        if (!string.IsNullOrEmpty(answerInput.text.Trim()))
        {
            attackButton.gameObject.SetActive(true);
        }
        else
        {
            attackButton.gameObject.SetActive(false);
        }
    }

    public void AdjustVolume()
    {
        AudioListener.volume = volumeSlider.value; // Set the global volume
    }

    public void AdjustBrightness()
    {
        float brightness = brightnessSlider.value; // Get the brightness value from the slider

        // Adjust the alpha of the brightness overlay based on the slider
        Color overlayColor = brightnessOverlay.color;
        overlayColor.a = 1f - brightness; // Invert brightness for the overlay (1 is fully transparent, 0 is fully opaque)
        brightnessOverlay.color = overlayColor;
    }

    public void SetEnemyCount(int count)
    {
        selectedEnemyCount = Mathf.Clamp(count, 1, 5);

        // Reset all buttons to default color
        foreach (Button button in enemyCountButtons)
        {
            button.GetComponent<Image>().color = defaultButtonColor;
        }

        // Set the selected button to the highlight color
        enemyCountButtons[count - 1].GetComponent<Image>().color = selectedButtonColor;
    }

    void ShowPlayerAttackText(int damage)
    {
        // Set and display points text
        playerPointsText.text = "+" + score + " points";
        playerPointsText.gameObject.SetActive(true);
        StartCoroutine(FadeOutText(playerPointsText));

        // Set and display damage text
        playerDamageText.text = "-" + damage + " HP";
        playerDamageText.gameObject.SetActive(true);
        StartCoroutine(FadeOutText(playerDamageText));
    }

    void ShowEnemyDamageText(int damage)
    {
        enemyDamageText.text = "-" + damage + " HP";
        enemyDamageText.gameObject.SetActive(true);
        StartCoroutine(FadeOutText(enemyDamageText));
    }

    IEnumerator FadeOutText(TextMeshProUGUI text)
    {
        Color originalColor = text.color;
        for (float t = 0; t < 1f; t += Time.deltaTime / 1f) // Adjust duration as needed
        {
            text.color = new Color(originalColor.r, originalColor.g, originalColor.b, Mathf.Lerp(1, 0, t));
            yield return null;
        }
        text.gameObject.SetActive(false); // Hide text after fade-out
        text.color = originalColor; // Reset the color for future use
    }
}
