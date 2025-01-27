using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PdfItem : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public Button onPdfSelectedButton;
    public Outline outline;

    private FirebaseFile pdf;
    public FirebaseFile Pdf => pdf;

    public void Initialize(FirebaseFile pdfFile, System.Action<PdfItem> onPdfSelected)
    {
        pdf = pdfFile;
        nameText.text = pdf.name;
        onPdfSelectedButton.onClick.AddListener(() => onPdfSelected(this));
    }

    public void DownloadPdf()
    {
        Application.OpenURL(pdf.url);
    }


    public void ToggleHighlight()
    {
        outline.enabled = !outline.enabled;
    }
}
