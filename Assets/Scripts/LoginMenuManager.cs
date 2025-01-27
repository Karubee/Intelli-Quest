using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class LoginMenuManager : MonoBehaviour
{
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject loadingPanel;
    public GameObject errorPanel;

    public GameObject forgotPassPanel;

    public InputField loginEmailInput;
    public InputField loginPasswordInput;
    public InputField registerEmailInput;
    public InputField registerPasswordInput;
    public InputField registerConfirmPasswordInput;

    public Button loginPasswordToggleBtn;
    public Button registerPasswordToggleBtn;
    public Button confirmPasswordToggleBtn;

    private bool isLoginPasswordVisible = false;
    private bool isRegisterPasswordVisible = false;
    private bool isConfirmPasswordVisible = false;

    public InputField forgotPasswordEmailInput;

    public TextMeshProUGUI errorText;

    public GameObject currentActivePanel;



    public float panelTransitionDelay = 1.5f;

    void Start()
    {
        loginPasswordToggleBtn.onClick.AddListener(() => TogglePasswordVisibility(loginPasswordInput, ref isLoginPasswordVisible));
        registerPasswordToggleBtn.onClick.AddListener(() => TogglePasswordVisibility(registerPasswordInput, ref isRegisterPasswordVisible));
        confirmPasswordToggleBtn.onClick.AddListener(() => TogglePasswordVisibility(registerConfirmPasswordInput, ref isRegisterPasswordVisible));

        ShowLoginPanel();

        AuthFirestoreManager.Instance.OnAuthenticationSuccess += HandleAuthenticationSuccess;
        AuthFirestoreManager.Instance.OnAuthenticationError += HandleAuthenticationError;
        AuthFirestoreManager.Instance.OnUserDataStored += HandleUserDataStored;
        AuthFirestoreManager.Instance.OnUserDataRetrieved += HandleUserDataRetrieved;
    }

    private void TogglePasswordVisibility(InputField passwordField, ref bool isVisible)
    {
        isVisible = !isVisible;
        passwordField.contentType = isVisible ? InputField.ContentType.Standard : InputField.ContentType.Password;
        passwordField.ForceLabelUpdate(); // Refresh the input field
    }

    public void ShowLoginPanel()
    {
        HideAllPanels();
        loginPanel.SetActive(true);
        currentActivePanel = loginPanel;
    }

    public void ShowForgotPasswordPanel()
    {
        HideAllPanels();
        forgotPassPanel.SetActive(true);
        currentActivePanel = forgotPassPanel;
    }

    public void ShowRegisterPanel()
    {
        HideAllPanels();
        registerPanel.SetActive(true);
        currentActivePanel = registerPanel;
    }

    public void AttemptLogin()
    {
        string email = loginEmailInput.text;
        string password = loginPasswordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowErrorLogin("Please enter both email and password.");
            return;
        }

        ShowLoading();
        AuthFirestoreManager.Instance.SignInWithEmail(email, password);
    }

    public void AttemptRegister()
    {
        string email = registerEmailInput.text;
        string password = registerPasswordInput.text;
        string confirmPassword = registerConfirmPasswordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            ShowErrorRegister("Please fill in all fields.");
            return;
        }

        if (password != confirmPassword)
        {
            ShowErrorRegister("Passwords do not match.");
            return;
        }

        ShowLoading();
        AuthFirestoreManager.Instance.SignUpWithEmail(email, password);
    }

    private void OnRegisterSuccess()
    {
        ShowError("Registration successful! Please verify your email before logging in.");
    }

    private void OnRegisterFailure(string errorMessage)
    {
        ShowError(errorMessage);
    }

    public void AttemptForgotPassword()
    {
        string email = forgotPasswordEmailInput.text;
        if (string.IsNullOrEmpty(email))
        {
            ShowErrorForgotPassword("Please enter your email.");
            Debug.Log("Entered Email: " + email);
            return;
        }

        ShowLoading();
        AuthFirestoreManager.Instance.ForgotPassword(email);
        Debug.Log("Email Entered: " + email);
        ShowError("Password reset email sent. Please check your email.");
        HideLoading();
    }

    public void AttemptGoogleSignIn()
    {
        ShowLoading();
        AuthFirestoreManager.Instance.SignInWithGoogle();
    }

    private void HandleAuthenticationSuccess(string userId)
    {
        if (AuthFirestoreManager.Instance.CurrentUser.emailVerified)
        {
            Debug.Log($"User email verified. Proceeding to main menu.");
            HideLoading();
            LoadMainMenuPanel();
        }
        else
        {
            Debug.Log("User email not verified.");
            HideLoading();
            ShowError("Please verify your email before logging in. Check your email for the verification link. The Verification link is valid for 1 hour");
        }
    }

    private void HandleAuthenticationError(string errorMessage)
    {
        Debug.LogError($"Authentication error: {errorMessage}");
        HideLoading();
        ShowError(errorMessage);
    }

    private void HandleUserDataStored(string message)
    {
        Debug.Log($"User data stored: {message}");
        // You might want to do something here, like updating the UI
    }

    private void HandleUserDataRetrieved(Dictionary<string, object> userData)
    {
        Debug.Log("User data retrieved:");
        foreach (var kvp in userData)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value}");
        }
        // You might want to use this data to update the UI or game state
    }

    public void LoadMainMenuPanel()
    {
        SceneManager.LoadScene("MainMenu");
    }

    void HideAllPanels()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        forgotPassPanel.SetActive(false);
        loadingPanel.SetActive(false);
        errorPanel.SetActive(false);
    }

    void ShowLoading()
    {
        loadingPanel.SetActive(true);
    }

    void HideLoading()
    {
        loadingPanel.SetActive(false);

    }

    void ShowError(string message)
    {
        errorPanel.SetActive(true);
        errorText.text = message;
        StartCoroutine(SwitchPanelWithDelay(currentActivePanel));
    }

    void ShowErrorLogin(string message)
    {
        errorPanel.SetActive(true);
        errorText.text = message;
        StartCoroutine(SwitchPanelWithDelay(loginPanel));
    }

    void ShowErrorForgotPassword(string message)
    {
        errorPanel.SetActive(true);
        errorText.text = message;
        StartCoroutine(SwitchPanelWithDelay(forgotPassPanel));
    }
    void ShowErrorRegister(string message)
    {
        errorPanel.SetActive(true);
        errorText.text = message;
        StartCoroutine(SwitchPanelWithDelay(registerPanel));
    }

    void OnDestroy()
    {
        // Unsubscribe from events when the LoginMenuManager is destroyed
        if (AuthFirestoreManager.Instance != null)
        {
            AuthFirestoreManager.Instance.OnAuthenticationSuccess -= HandleAuthenticationSuccess;
            AuthFirestoreManager.Instance.OnAuthenticationError -= HandleAuthenticationError;
            AuthFirestoreManager.Instance.OnUserDataStored -= HandleUserDataStored;
            AuthFirestoreManager.Instance.OnUserDataRetrieved -= HandleUserDataRetrieved;
        }
    }

    IEnumerator SwitchPanelWithDelay(GameObject panelToShow)
    {
        // Add the delay here before switching panels
        yield return new WaitForSeconds(panelTransitionDelay);

        HideAllPanels();

        // Show the desired panel
        panelToShow.SetActive(true);

    }
}