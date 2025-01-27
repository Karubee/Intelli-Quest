using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class QAGenerator : MonoBehaviour
{
    public static QAGenerator Instance { get; set; }

    public string[] Questions { get; private set; }
    public string[] Answers { get; private set; }

    public GameObject returnButton;
    public GameObject errorPanel;
    public TextMeshProUGUI errorText;

    public FirebaseFile PdfFile { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public IEnumerator GenerateQuestionsAndAnswers(
        FirebaseFile pdf,
        string difficulty = "easy",
        System.Action callback = null
    )
    {
        if (string.IsNullOrEmpty(pdf.url))
        {
            Debug.LogError("pdfUrl is null or empty");
            Debug.LogError("No PDF selected.");
            errorPanel.SetActive(true);
            errorText.text = "No PDF selected.";

            StartCoroutine(HideErrorPanelAfterDelay(2f)); // Adjust the time as needed
            yield break;
        }

        RequestData requestData =
            new()
            {
                pdfUrl = pdf.url,
                numQuestions = 20,
                difficulty = difficulty,
            };

        string jsonData = JsonUtility.ToJson(requestData);
        Debug.Log("Request Data: " + jsonData);

        using UnityWebRequest request =
            new(
                "https://us-central1-intelliquest-3401f.cloudfunctions.net/generateQuestionsAndAnswersFromPdf",
                "POST"
            );
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            errorPanel.SetActive(true);
            errorText.text = "Your Selected PDF exceed the limits of our service, please try again!";
            Debug.LogError("Error generating questions and answers: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
            StartCoroutine(HideErrorPanelAfterDelay(2f));

            returnButton.SetActive(true);
        }
        else
        {
            Debug.Log("Response: " + request.downloadHandler.text);
            try
            {
                ResponseData response = JsonUtility.FromJson<ResponseData>(
                    request.downloadHandler.text
                );
                if (
                    response != null
                    && response.questions != null
                    && response.answers != null
                    && response.questions.Length > 0
                    && response.answers.Length > 0
                )
                {
                    foreach (var question in response.questions)
                    {
                        Debug.Log("Question: " + question);
                    }

                    foreach (var answer in response.answers)
                    {
                        Debug.Log("Answers: " + answer);
                    }
                    PdfFile = pdf;
                    UpdateQuestionsAndAnswers(response.questions, response.answers);
                    callback?.Invoke();
                }
                else
                {
                    Debug.LogWarning("No answers received from the server or response is null.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error parsing JSON response: " + ex.Message);
                Debug.LogError("Raw response: " + request.downloadHandler.text);
            }
        }
    }

    private void UpdateQuestionsAndAnswers(string[] questions, string[] answers)
    {
        Instance.Questions = questions; // Store as an array now
        Instance.Answers = answers; // Store as an array now

    }

    public IEnumerator HideErrorPanelAfterDelay(float delay)
    {
        Debug.Log("Hiding error panel after delay.");
        yield return new WaitForSeconds(delay);
        errorPanel.SetActive(false);
    }

    [System.Serializable]
    private class RequestData
    {
        public string pdfUrl;
        public int numQuestions;
        public string difficulty;
    }

    [System.Serializable]
    private class ResponseData
    {
        public string[] questions;
        public string[] answers;
    }
}
