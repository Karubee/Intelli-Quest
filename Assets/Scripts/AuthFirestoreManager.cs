using UnityEngine;
using System;
using System.Collections.Generic;
using FirebaseWebGL.Scripts.FirebaseBridge;
using FirebaseWebGL.Scripts.Objects;
using FirebaseWebGL.Examples.Utils;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.VisualScripting;

public class AuthFirestoreManager : MonoBehaviour
{
    private static AuthFirestoreManager _instance;
    public static AuthFirestoreManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AuthFirestoreManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AuthFirestoreManager");
                    _instance = go.AddComponent<AuthFirestoreManager>();
                }
            }
            return _instance;
        }
    }

    public FirebaseUser CurrentUser { get => Instance.currentUser; }

    public event Action<string> OnAuthenticationSuccess;
    public event Action<string> OnAuthenticationError;
    public event Action<string> OnUserDataStored;
    public event Action<Dictionary<string, object>> OnUserDataRetrieved;

    private FirebaseUser currentUser;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
    }


    public void SignUpWithEmail(string email, string password)
    {
        try
        {
            FirebaseAuth.CreateUserWithEmailAndPassword(email, password, gameObject.name, "OnSignUpSuccess", "OnSignUpFailure");
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    public void SignInWithEmail(string email, string password)
    {
        FirebaseAuth.SignInWithEmailAndPassword(email, password, gameObject.name, "OnAuthSuccess", "OnAuthError");
    }

    public void SignInWithGoogle()
    {
        FirebaseAuth.SignInWithGoogle(gameObject.name, "OnAuthSuccess", "OnAuthError");
    }

    public void SignOutUser()
    {
        FirebaseAuth.SignOut(gameObject.name, "OnSignOutSuccess", "OnAuthError");
    }

    public void GetCurrentUserInfo()
    {
        FirebaseFirestore.GetUserData(currentUser.uid, gameObject.name, "OnGetCurrentUserSuccess", "OnAuthError");
    }

    public void StoreUserData(Dictionary<string, object> userData)
    {
        if (string.IsNullOrEmpty(currentUser.uid))
        {
            Debug.LogError("No user is currently logged in");
            return;
        }

        string jsonData = JsonUtility.ToJson(new SerializableDictionary<string, object>(userData));
        FirebaseFirestore.SetUserData(currentUser.uid, jsonData, gameObject.name, "OnUserDataStoreSuccess", "OnUserDataStoreError");
    }

    public void RetrieveUserData()
    {
        if (string.IsNullOrEmpty(currentUser.uid))
        {
            Debug.LogError("No user is currently logged in");
            return;
        }

        FirebaseFirestore.GetUserData(currentUser.uid, gameObject.name, "OnUserDataRetrieveSuccess", "OnUserDataRetrieveError");
    }

    private void OnAuthSuccess(string userId)
    {
        currentUser = StringSerializationAPI.Deserialize(typeof(FirebaseUser), userId) as FirebaseUser;
        OnAuthenticationSuccess?.Invoke(userId);
        Debug.Log($"Authentication successful. User ID: {AuthFirestoreManager.Instance.CurrentUser.uid}");
    }

    private void OnAuthError(string errorJson)
    {
        string errorMessage = ParseAndTranslateFirebaseError(errorJson);
        OnAuthenticationError?.Invoke(errorMessage);
        Debug.LogError($"Authentication error: {errorMessage}");
    }

    private void OnSignUpSuccess(string userId)
    {
        currentUser = StringSerializationAPI.Deserialize(typeof(FirebaseUser), userId) as FirebaseUser;
        OnAuthenticationSuccess?.Invoke(userId);
        Debug.Log($"Sign up successful. User ID: {AuthFirestoreManager.Instance.CurrentUser.uid}");

        RequestEmailVerification();
        Debug.Log("Verification email sent. Please verify your email before logging in.");
    }

    private void OnSignUpFailure(string errorJson)
    {
        string errorMessage = ParseAndTranslateFirebaseError(errorJson);
        OnAuthenticationError?.Invoke(errorMessage);
        Debug.LogError($"Sign up failed: {errorJson}");
    }

    //Email Verification Methods
    public void RequestEmailVerification()
    {
        FirebaseAuth.SendEmailVerification(gameObject.name, "OnEmailVerificationSuccess", "OnEmailVerificationError");
    }

    private void OnEmailVerificationSuccess(string message)
    {
        Debug.Log($"Email verification sent: {message}");
    }

    private void OnEmailVerificationError(string errorMessage)
    {
        Debug.LogError($"Error sending email verification: {errorMessage}");
    }

    public void ForgotPassword(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            Debug.LogError("Email field is empty. Please enter an email address.");
            return;
        }

        string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        if (!System.Text.RegularExpressions.Regex.IsMatch(email, emailPattern))
        {
            Debug.LogError("Invalid email format. Please enter a valid email address.");
            return;
        }

        Debug.Log("Sending password reset email to: " + email);
        FirebaseAuth.SendPasswordResetEmail(email, gameObject.name, "OnForgotPasswordSuccess", "OnForgotPasswordError");
    }

    private void OnForgotPasswordSuccess(string message)
    {
        Debug.Log($"Password reset email sent: {message}");
    }

    private void OnForgotPasswordError(string errorMessage)
    {
        Debug.LogError($"Error sending password reset email: {errorMessage}");
    }

    private void OnSignOutSuccess(string message)
    {
        currentUser = null;
        Debug.Log("Sign out successful");
        StartCoroutine(DelayedLoginSceneSwitch());
    }

    private IEnumerator DelayedLoginSceneSwitch()
    {
        yield return new WaitForSeconds(0.1f); // Small delay to ensure all sign-out processes complete
        SceneManager.LoadScene("LoginMenu");
    }

    private void OnGetCurrentUserSuccess(string userInfo)
    {
        Debug.Log($"Current user info: {userInfo}");
    }

    private void OnUserDataStoreSuccess(string message)
    {
        OnUserDataStored?.Invoke(message);
        Debug.Log($"User data stored successfully: {message}");
    }

    private void OnUserDataStoreError(string errorMessage)
    {
        Debug.LogError($"Error storing user data: {errorMessage}");
    }

    private void OnUserDataRetrieveSuccess(string data)
    {
        var userData = JsonUtility.FromJson<SerializableDictionary<string, object>>(data).ToDictionary();
        OnUserDataRetrieved?.Invoke(userData);
        Debug.Log($"User data retrieved successfully: {data}");
    }

    private void OnUserDataRetrieveError(string errorMessage)
    {
        Debug.LogError($"Error retrieving user data: {errorMessage}");
    }

    private string ParseAndTranslateFirebaseError(string errorJson)
    {
        try
        {
            FirebaseError errorObj = JsonUtility.FromJson<FirebaseError>(errorJson);

            if (string.IsNullOrEmpty(errorObj.code))
            {
                // If we can't find the error code, return the full message
                return !string.IsNullOrEmpty(errorObj.message) ? errorObj.message : "Unknown error occurred";
            }

            return TranslateFirebaseError(errorObj.code);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing Firebase error: {ex.Message}");
            return "An unexpected error occurred. Please try again.";
        }
    }

    private string TranslateFirebaseError(string errorCode)
    {
        switch (errorCode)
        {
            // Registration errors
            case "auth/email-already-in-use":
                return "The email address is already in use by another account.";
            case "auth/invalid-email":
                return "The email address is not valid.";
            case "auth/operation-not-allowed":
                return "Email/password accounts are not enabled. Please contact support.";
            case "auth/weak-password":
                return "The password is too weak. Please use a stronger password.";

            // Login errors
            case "auth/user-disabled":
                return "This user account has been disabled. Please contact support.";
            case "auth/user-not-found":
                return "There is no user account associated with this email.";
            case "auth/invalid-login-credentials":
                return "Wrong email address or password. Please try again.";
            case "auth/invalid-credential":
                return "Wrong credentials entered. Please try again.";
            case "auth/wrong-password":
                return "The password is invalid. Please try again.";
            case "auth/email-already-exists":
                return "Email already exists! Please try another email.";

            // Google Sign-In errors
            case "auth/account-exists-with-different-credential":
                return "An account already exists with the same email address but different sign-in credentials.";
            case "auth/popup-blocked":
                return "The sign-in popup was blocked. Please enable popups for this site and try again.";
            case "auth/popup-closed-by-user":
                return "The sign-in popup was closed before authentication was completed.";

            // Generic errors
            case "auth/network-request-failed":
                return "A network error occurred. Please check your internet connection and try again.";
            case "auth/too-many-requests":
                return "Too many unsuccessful login attempts. Please try again later.";

            default:
                return $"An unexpected error occurred: {errorCode}";
        }
    }
}

[Serializable]
public class SerializableDictionary<TKey, TValue>
{
    [SerializeField]
    private List<TKey> keys = new List<TKey>();

    [SerializeField]
    private List<TValue> values = new List<TValue>();

    public SerializableDictionary() { }

    public SerializableDictionary(Dictionary<TKey, TValue> dictionary)
    {
        foreach (var kvp in dictionary)
        {
            keys.Add(kvp.Key);
            values.Add(kvp.Value);
        }
    }

    public Dictionary<TKey, TValue> ToDictionary()
    {
        var dict = new Dictionary<TKey, TValue>();
        for (int i = 0; i < keys.Count; i++)
        {
            dict[keys[i]] = values[i];
        }
        return dict;
    }

    public class FirebaseError
    {
        public string code;
        public string message;
        public string name;
        public string stack;
    }
}