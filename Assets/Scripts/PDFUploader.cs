using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using FirebaseWebGL.Scripts.FirebaseBridge;
using SFB;
using TMPro;
using UnityEngine;

public class PdfUploader : MonoBehaviour
{
    public MenuManager menuManager;

    public GameObject loadingPanel;
    public GameObject errorPanel;
    public TextMeshProUGUI errorText;
    public GameObject pdfcollectionPanel;
    public string fileName;
    public static PdfUploader Instance { get; private set; }

    private void Awake()
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

    public void OpenFileBrowser()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        OpenFileBrowserWebGL();
#else
        OpenFileBrowserStandalone();
#endif
    }

    private void OpenFileBrowserStandalone()
    {
        // Define file type filter for PDFs
        ExtensionFilter[] extensions = new ExtensionFilter[]
        {
            new ExtensionFilter("PDF Files", "pdf"),
        };

        var paths = StandaloneFileBrowser.OpenFilePanel("Select PDF", "", extensions, false);

        if (paths.Length > 0)
        {
            string filePath = paths[0];
            if (ValidatePdfFile(filePath))
            {
                Debug.Log("Selected file: " + filePath);
                StartCoroutine(UploadPdf(filePath));
            }
            else
            {
                ShowError("Please select a valid PDF file.");
            }
        }
        else
        {
            Debug.LogWarning("No file selected.");
        }
    }

    [DllImport("__Internal")]
    private static extern void OpenFileDialog();

    private void OpenFileBrowserWebGL()
    {
        OpenFileDialog();
    }

    public void OnFileSelected(string fileInfoString)
    {
        // Deserialize the JSON string to the FileInfo object
        FileInfo fileInfo = JsonUtility.FromJson<FileInfo>(fileInfoString);

        string fileName = fileInfo.name;
        string base64Data = fileInfo.data;

        // Validate file extension
        if (!fileName.ToLower().EndsWith(".pdf"))
        {
            errorPanel.SetActive(true);
            errorText.text = "Please select a valid PDF file.";
            ShowError("Please select a valid PDF file.");
            return;
        }

        Debug.Log("Selected file: " + fileName);

        byte[] fileData = System.Convert.FromBase64String(base64Data);

        // Additional PDF header validation
        if (!ValidatePdfBytes(fileData))
        {
            ShowError("Invalid PDF file format.");
            return;
        }

        this.fileName = fileName;
        StartCoroutine(UploadPdfData(fileData, fileName));
        loadingPanel.SetActive(true);
    }

    private bool ValidatePdfFile(string filePath)
    {
        if (!filePath.ToLower().EndsWith(".pdf"))
            return false;

        try
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            return ValidatePdfBytes(fileBytes);
        }
        catch
        {
            return false;
        }
    }

    private bool ValidatePdfBytes(byte[] fileData)
    {
        if (fileData.Length < 5)
            return false;

        // Check for PDF magic number (%PDF-)
        byte[] pdfHeader = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D };
        for (int i = 0; i < 5; i++)
        {
            if (fileData[i] != pdfHeader[i])
                return false;
        }
        return true;
    }

    private void ShowError(string message)
    {
        Debug.LogError(message);
        errorPanel.SetActive(true);
        errorText.text = message;
        StartCoroutine(HideErrorPanelDelay());
    }

    public IEnumerator UploadPdf(string filePath)
    {
        fileName = Path.GetFileName(filePath);
        byte[] fileData = File.ReadAllBytes(filePath);
        return UploadPdfData(fileData, Path.GetFileName(filePath));
    }

    public IEnumerator UploadPdfData(byte[] fileData, string fileName)
    {
        string base64File = System.Convert.ToBase64String(fileData);
        string currentUser = AuthFirestoreManager.Instance.CurrentUser.uid;
        string path = $"uploads/{currentUser}/{fileName}.pdf";
        Debug.Log(AuthFirestoreManager.Instance.CurrentUser.uid);
        FirebaseStorage.UploadFileToStorage(
            path,
            base64File,
            "PdfUploader",
            nameof(OnUploadSuccess),
            nameof(OnUploadFailure)
        );

        yield return null;
    }

    public void OnUploadSuccess(string downloadUrl)
    {
        Debug.Log("File uploaded successfully. URL: " + downloadUrl);

        string currentUser = AuthFirestoreManager.Instance.CurrentUser.uid;
        FirebaseFile pdfFile = new(fileName, downloadUrl);
        string jsonData = JsonUtility.ToJson(pdfFile);
        string path = $"users/{currentUser}/files";

        FirebaseFirestore.AddDocument(
            path,
            jsonData,
            "PdfUploader",
            nameof(OnFirestoreSuccess),
            nameof(OnFirestoreFailure)
        );
        Debug.Log("jsonData" + jsonData);

        loadingPanel.SetActive(false);
        PdfListManager.Instance.RefreshPdfList();
        StartCoroutine(menuManager.SwitchPanelWithDelay(pdfcollectionPanel));
    }

    public void OnFirestoreSuccess(string message)
    {
        Debug.Log("Firestore document saved successfully: " + message);
    }

    public void OnFirestoreFailure(string error)
    {
        Debug.LogError("Failed to save document in Firestore: " + error);
        errorPanel.SetActive(true);
        errorText.text = "Unable to save the document! Please try again later.";
        StartCoroutine(HideErrorPanelDelay());
    }

    public void OnUploadFailure(string error)
    {
        Debug.LogError("File upload failed: " + error);
        ShowError("File upload failed. Please try again.");
    }

    public IEnumerator HideErrorPanelDelay()
    {
        yield return new WaitForSeconds(2f);
        errorPanel.SetActive(false);
    }
}

[System.Serializable]
public class FirebaseFile
{
    public string url;
    public Level medium;
    public Level easy;
    public string name;
    public Level hard;

    public FirebaseFile()
    {
        easy = new Level();
        medium = new Level();
        hard = new Level();
    }

    public FirebaseFile(string name, string url)
        : this()
    {
        this.name = name;
        this.url = url;
    }

    public FirebaseFile(string name, string url, Level level)
        : this()
    {
        this.name = name;
        this.url = url;
    }
}

[System.Serializable]
public class Level
{
    public int score;
    public List<string> questions;
    public List<string> answers;

    public Level()
    {
        questions = new List<string>();
        answers = new List<string>();
        score = 0;
    }

    public Level(int score)
    {
        this.score = score;
    }
}

[System.Serializable]
public class FileInfo
{
    public string name;
    public string data;
}
