using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class LoadingScreenController : MonoBehaviour
{
    public Slider progressBar;
    public TextMeshProUGUI progressText;
    public Image fadeImage;  // Reference to the fade screen
    public float fadeDuration = 1.0f;  // Duration of fade in/out

    public Image slideshowImage;  // Reference to the slideshow image component
    public Sprite[] slideshowSprites;  // Array to hold the slideshow images
    public float slideshowDuration = 2.0f;  // Duration each slide is displayed

    private static string sceneToLoad;

    public static void LoadScene(string sceneName)
    {
        sceneToLoad = sceneName;
        SceneManager.LoadScene("LoadingScreen");
    }

    void Start()
    {
        // Start the fade-in effect and loading process
        StartCoroutine(FadeInAndLoad());
        // Start the slideshow
        StartCoroutine(Slideshow());

        Debug.Log($"Progress Text: {progressText != null}");
        Debug.Log($"Fade Image: {fadeImage != null}");
        Debug.Log($"Progress Bar: {progressBar != null}");
    }

    IEnumerator FadeInAndLoad()
    {
        // Fade in from black
        yield return StartCoroutine(Fade(1, 0));

        // Start loading the new scene asynchronously
        StartCoroutine(LoadSceneAsync());
    }

    IEnumerator LoadSceneAsync()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            progressBar.value = progress;
            progressText.text = (progress * 100f).ToString("F2") + "%";

            if (operation.progress >= 0.9f)
            {
                // When loading is complete, change the text
                progressText.text = "Press the screen to continue";

                // Wait for the player to press any key or touch the screen
                if (Input.anyKeyDown)
                {
                    // Fade out before activating the scene
                    yield return StartCoroutine(Fade(0, 1));
                    operation.allowSceneActivation = true;
                }
            }

            yield return null;
        }
    }

    IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, alpha);
            yield return null;
        }

        // Ensure the final alpha value is set correctly
        fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, endAlpha);
    }

    IEnumerator Slideshow()
    {
        int index = 0;
        while (true)
        {
            slideshowImage.sprite = slideshowSprites[index];
            index = (index + 1) % slideshowSprites.Length;
            yield return new WaitForSeconds(slideshowDuration);
        }
    }
}
