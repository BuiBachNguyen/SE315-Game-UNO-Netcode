using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SceneLoadingOverlay : MonoBehaviour
{
    const int SortingOrder = 32000;

    static SceneLoadingOverlay instance;

    Canvas canvas;
    TextMeshProUGUI messageText;

    public static void Show(string message)
    {
        EnsureInstance();
        instance.SetVisible(true, message);
    }

    public static void SetMessage(string message)
    {
        EnsureInstance();
        instance.SetVisible(true, message);
    }

    public static void Hide()
    {
        if (instance == null)
            return;

        instance.SetVisible(false, "");
    }

    static void EnsureInstance()
    {
        if (instance != null)
            return;

        GameObject root = new GameObject("SceneLoadingOverlay", typeof(RectTransform));
        DontDestroyOnLoad(root);

        instance = root.AddComponent<SceneLoadingOverlay>();
        instance.Build();
        instance.SetVisible(false, "");
    }

    void Build()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = gameObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        GameObject blocker = new GameObject("Blocker");
        blocker.transform.SetParent(transform, false);

        RectTransform blockerRect = blocker.AddComponent<RectTransform>();
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;

        Image background = blocker.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.62f);
        background.raycastTarget = true;

        GameObject textObject = new GameObject("Message");
        textObject.transform.SetParent(blocker.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(720f, 180f);

        messageText = textObject.AddComponent<TextMeshProUGUI>();
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.color = Color.white;
        messageText.enableAutoSizing = true;
        messageText.fontSizeMin = 24f;
        messageText.fontSizeMax = 64f;
        messageText.raycastTarget = false;
    }

    void SetVisible(bool visible, string message)
    {
        if (messageText != null)
            messageText.text = message;

        gameObject.SetActive(visible);
    }
}
