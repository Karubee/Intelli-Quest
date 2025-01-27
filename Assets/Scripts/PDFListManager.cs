using System;
using System.Collections;
using System.Collections.Generic;
using FirebaseWebGL.Examples.Firestore;
using FirebaseWebGL.Examples.Utils;
using FirebaseWebGL.Scripts.FirebaseBridge;
using FirebaseWebGL.Scripts.Objects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PdfListManager : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI easyScoreText;

    [SerializeField]
    private TextMeshProUGUI mediumScoreText;

    [SerializeField]
    private TextMeshProUGUI hardScoreText;
    public static PdfListManager Instance { get; private set; }
    public GameObject pdfItemPrefab;
    public GameObject deleteConfirmationPanel;
    public ScrollRect scrollRect;
    public RectTransform contentTransform; // This will be the "Sliding Area" RectTransform

    private string currentUser;
    private readonly List<FirebaseFile> pdfList = new();

    private string fileToDelete = "";

    public PdfItem SelectedPdf { get; set; }

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
        Debug.Log("ContentTransform: " + contentTransform);
    }

    private void OnEnable()
    {
        // Ensure this script only runs on "PDFCollectionPanel"
        if (gameObject.name != "PDFCollectionPanel")
        {
            Debug.LogWarning("This script is not attached to the 'PDFCollectionPanel'. Exiting.");
            return;
        }
        StartCoroutine(InitializeWhenEnabled());
    }

    private IEnumerator InitializeWhenEnabled()
    {
        yield return new WaitForSeconds(0.5f); // Wait for half a second to ensure all objects are loaded

        Debug.Log("PDFCollectionPanel is now active. Initializing PdfListManager.");
        InitializeUIElements();

        RefreshPdfList();
        Debug.Log("ScrollRect: " + scrollRect);
        Debug.Log("ContentTransform: " + contentTransform);
        Debug.Log("Viewport: " + scrollRect.viewport);
    }

    private void InitializeUIElements()
    {
        Debug.Log("Initializing UI elements...");
        scrollRect = GetComponentInChildren<ScrollRect>();
        if (scrollRect != null)
        {
            Debug.Log("ScrollRect found and assigned.");

            Transform slidingArea = scrollRect.transform.Find("Viewport/Sliding Area");
            if (slidingArea != null)
            {
                contentTransform = slidingArea.GetComponent<RectTransform>();
                Debug.Log("Content RectTransform (Sliding Area) found and assigned.");
            }
            else
            {
                Debug.LogError("Sliding Area not found in ScrollView hierarchy.");
            }
        }
        else
        {
            Debug.LogError("ScrollRect not found in PDFCollectionPanel.");
        }
    }

    private FirebaseUser GetCurrentUser()
    {
        return AuthFirestoreManager.Instance?.CurrentUser;
    }

    public void RefreshPdfList()
    {
        Debug.Log("Refreshing PDF list...");
        ClearPdfList();

        currentUser = GetCurrentUser()?.uid ?? "";
        if (string.IsNullOrEmpty(currentUser))
        {
            Debug.LogError("Current user is null or empty. Cannot fetch PDF list.");
            return;
        }

        string firestorePath = $"users/{currentUser}/files";
        Debug.Log($"Fetching documents from Firestore path: {firestorePath}");
        FirebaseFirestore.GetDocumentsInCollection(
            firestorePath,
            gameObject.name,
            nameof(OnGetCollectionSuccess),
            nameof(OnGetCollectionFailure)
        );
    }

    private void ClearPdfList()
    {
        pdfList.Clear();
        SelectedPdf = null;

        if (contentTransform != null)
        {
            foreach (Transform child in contentTransform)
            {
                Destroy(child.gameObject);
            }
        }

        if (scrollRect != null && scrollRect.content != null)
        {
            scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, 0);
        }
    }

    public void OnGetCollectionSuccess(string collectionJson)
    {
        Debug.Log("Collection retrieved successfully: " + collectionJson);

        if (string.IsNullOrEmpty(collectionJson))
        {
            Debug.LogWarning("Received empty collection JSON.");
            return;
        }

        try
        {
            var collection =
                (Dictionary<string, FirebaseFile>)
                    StringSerializationAPI.Deserialize(
                        typeof(Dictionary<string, FirebaseFile>),
                        collectionJson
                    );

            if (collection != null && collection.Count > 0)
            {
                Debug.Log($"Deserialized {collection.Count} PDF files.");
                foreach (var file in collection)
                {
                    StartCoroutine(AddPdfToList(file.Value));
                }
            }
            else
            {
                Debug.LogWarning("Deserialized collection is null or empty.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error deserializing collection: {e.Message}");
        }
    }

    public IEnumerator AddPdfToList(FirebaseFile pdfFile)
    {
        if (pdfFile == null)
        {
            Debug.LogError("Attempted to add null PDF file to list.");
            yield break;
        }

        pdfList.Add(pdfFile);

        if (contentTransform == null)
        {
            Debug.LogError("ContentTransform is null. Cannot add PDF to list.");
            yield break;
        }

        if (pdfItemPrefab == null)
        {
            Debug.LogError("pdfItemPrefab is null. Cannot add PDF to list.");
            yield break;
        }

        GameObject newItem = Instantiate(pdfItemPrefab, contentTransform);
        PdfItem pdfItemComponent = newItem.GetComponent<PdfItem>();
        if (pdfItemComponent != null)
        {
            pdfItemComponent.Initialize(pdfFile, OnPdfSelected);
        }
        else
        {
            Debug.LogError("PdfItem component not found on instantiated prefab.");
        }

        // Adjust content height
        float itemHeight = 100f; // Adjust this value based on your PdfItem prefab height
        scrollRect.content.sizeDelta = new Vector2(
            scrollRect.content.sizeDelta.x,
            itemHeight * pdfList.Count
        );

        yield return new WaitForSeconds(0.15f);
    }

    public void OnGetCollectionFailure(string error)
    {
        Debug.LogError("Failed to get collection: " + error);
    }

    public void OnPdfSelected(PdfItem pdfItem)
    {
        Debug.Log("PDF Item Selected: " + pdfItem.Pdf.url);

        if (SelectedPdf != null)
        {
            SelectedPdf.ToggleHighlight();
        }
        SelectedPdf = pdfItem;
        SelectedPdf.ToggleHighlight();

        easyScoreText.text = SelectedPdf.Pdf.easy.score.ToString();
        mediumScoreText.text = SelectedPdf.Pdf.medium.score.ToString();
        hardScoreText.text = SelectedPdf.Pdf.hard.score.ToString();

        Debug.Log($"Current SelectedPdf: {(SelectedPdf != null ? SelectedPdf.Pdf.url : "null")}");
        Debug.Log("PDF: " + pdfItem.Pdf);
        Debug.Log("URL: " + pdfItem.Pdf.url);
        Debug.Log("Name:" + pdfItem.Pdf.name);
        //PlayerPrefs.SetString("LastSelectedPdf", pdfItem.Pdf.url);
        //PlayerPrefs.Save();
    }

    public void DeleteSelectedPdf()
    {
        Debug.Log(
            $"DeleteSelectedPdf called. Current SelectedPdf: {(SelectedPdf.Pdf != null ? SelectedPdf.Pdf.url : "null")}"
        );
        if (SelectedPdf == null)
        {
            //string LastSelectedPdf = PlayerPrefs.GetString("LastSelectedPdf", null);
            Debug.LogError("No PDF selected for deletion.");
            return;
        }
        Debug.Log("Deleting PDF: " + SelectedPdf.Pdf.url);
        DeletePDFFile(SelectedPdf.Pdf);
        deleteConfirmationPanel.SetActive(false);

    }

    private void DeletePDFFile(FirebaseFile pdfFile)
    {
        if (pdfFile == null)
        {
            Debug.LogError("No PDF file selected for deletion.");
            return;
        }
        Debug.Log("Firestore path: " + $"users/{currentUser}/files/{pdfFile.url}");
        string firestorePath = $"users/{currentUser}/files";
        string documentId = pdfFile.url;
        Debug.Log($"Deleting PDF from Firestore path: {firestorePath}");

        FirebaseFirestore.GetDocumentsInCollection(
            firestorePath,
            gameObject.name,
            nameof(OnFetchSuccess),
            nameof(OnFetchFailure)
        );
    }

    public void OnFetchSuccess(string collectionJson)
    {
        Debug.Log("Collection retrieved successfully: " + collectionJson);

        if (string.IsNullOrEmpty(collectionJson))
        {
            Debug.LogWarning("Received empty collection JSON.");
            return;
        }

        try
        {
            var collection =
                (Dictionary<string, FirebaseFile>)
                    StringSerializationAPI.Deserialize(
                        typeof(Dictionary<string, FirebaseFile>),
                        collectionJson
                    );

            if (collection != null && collection.Count > 0)
            {
                Debug.Log($"Deserialized {collection.Count} PDF files.");
                foreach (var file in collection)
                {
                    if (SelectedPdf.Pdf.url != file.Value.url)
                        continue;
                    Debug.Log("File Key: " + file.Key);
                    fileToDelete = file.Key;
                }

                FirebaseFirestore.DeleteDocument(
                    $"users/{currentUser}/files",
                    fileToDelete,
                    gameObject.name,
                    nameof(OnDeleteSuccess),
                    nameof(OnDeleteError)
                );
            }
            else
            {
                Debug.LogWarning("Deserialized collection is null or empty.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error deserializing collection: {e.Message}");
        }
    }

    public void OnFetchFailure()
    {
        throw new NotImplementedException();
    }

    private void RemovePdfFromList(PdfItem pdfItem)
    {
        if (pdfItem == null)
        {
            Debug.LogError("PdfItem is null. Cannot remove from list.");
            return;
        }

        if (contentTransform == null)
        {
            Debug.LogError("contentTransform is null. Cannot remove from list.");
            return;
        }

        FirebaseFile fileToRemove = pdfItem.Pdf;

        if (fileToRemove != null && pdfList.Contains(fileToRemove))
        {
            pdfList.Remove(fileToRemove);
            Debug.Log($"Removed {fileToRemove.url} from pdfList. New count: {pdfList.Count}");
        }
        else
        {
            Debug.LogError($"FirebaseFile {fileToRemove?.url ?? "null"} not found in the list.");
        }

        Destroy(pdfItem.gameObject);
        Debug.Log($"Destroyed GameObject for {pdfItem.Pdf.url}");

        UpdateScrollViewContentSize();

        SelectedPdf = null;
        Debug.Log("Set SelectedPdf to null");
    }

    private void UpdateScrollViewContentSize()
    {
        if (scrollRect == null || scrollRect.content == null)
        {
            Debug.LogError("ScrollRect or its content is null. Cannot update content size.");
            return;
        }

        float itemHeight = 100f; // Adjust this value based on your PdfItem prefab height
        scrollRect.content.sizeDelta = new Vector2(
            scrollRect.content.sizeDelta.x,
            itemHeight * pdfList.Count
        );
        Debug.Log($"Updated scroll view content size. New height: {itemHeight * pdfList.Count}");
    }

    public void OnDeleteSuccess(string message)
    {
        Debug.Log("File successfully deleted from Firestore: " + message);
        // After successful Firestore deletion, refresh the list to update UI
        RefreshPdfList();
    }

    public void OnDeleteError(string error)
    {
        Debug.LogError("Failed to delete PDF file from Firestore: " + error);
    }
}
