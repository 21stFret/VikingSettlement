using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance { get; private set; }

    [SerializeField] private GameObject loadingRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image part1;
    [SerializeField] private Image part2;
    [SerializeField] private Image part3;

    [Header("Timing")]
    [SerializeField] private float minimumDisplayTime = 5f;
    [SerializeField] private float panelFadeInDuration = 0.3f;
    [SerializeField] private float panelFadeOutDuration = 0.8f;
    [SerializeField] private float crossFadeDuration = 0.6f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        loadingRoot.SetActive(false);
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        loadingRoot.SetActive(true);
        canvasGroup.alpha = 0f;
        yield return FadeCanvasGroup(1f, panelFadeInDuration);

        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
        asyncOp.allowSceneActivation = false;

        Image[] parts = { part1, part2, part3 };

        int current = 0;
        yield return FadePart(parts[current], 1f, crossFadeDuration);

        float startTime = Time.unscaledTime;
        bool sceneReady = false;

        do
        {
            int next = (current + 1) % 3;
            yield return CrossFade(parts[current], parts[next], crossFadeDuration);
            current = next;

            sceneReady = asyncOp.progress >= 0.9f && (Time.unscaledTime - startTime) >= minimumDisplayTime;
        }
        while (!sceneReady);

        yield return FadePart(parts[current], 0f, crossFadeDuration);

        asyncOp.allowSceneActivation = true;
        yield return asyncOp;

        yield return new WaitForSeconds(panelFadeOutDuration);
        yield return FadeCanvasGroup(0f, panelFadeOutDuration);
        loadingRoot.SetActive(false);
    }

    private IEnumerator CrossFade(Image fadeOut, Image fadeIn, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(fadeOut, 1f - t);
            SetAlpha(fadeIn, t);
            yield return null;
        }
    }

    private IEnumerator FadePart(Image img, float targetAlpha, float duration)
    {
        float startAlpha = img.color.a;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(img, Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }
    }

    private IEnumerator FadeCanvasGroup(float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
    }

    private static void SetAlpha(Image img, float alpha)
    {
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}
