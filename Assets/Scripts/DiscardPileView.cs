using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DiscardPileView : MonoBehaviour
{
    [SerializeField] private Image cardImage;
    [SerializeField] private CardSpriteMap cardSpriteMap;
    [SerializeField] private float punchScale = 1.1f;
    [SerializeField] private float punchDuration = 0.15f;

    private Vector3 defaultScale;
    private Coroutine punchRoutine;

    private void Awake()
    {
        defaultScale = transform.localScale;
    }

    private void OnEnable()
    {
        GameEvents.OnDiscardChanged += HandleDiscardChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnDiscardChanged -= HandleDiscardChanged;
    }

    private void HandleDiscardChanged(UnoCard topCard)
    {
        if (cardSpriteMap != null && cardImage != null && topCard != null)
        {
            cardImage.sprite = cardSpriteMap.GetSprite(topCard.Color, topCard.Type, topCard.Number);
        }

        if (punchRoutine != null)
        {
            StopCoroutine(punchRoutine);
        }

        // Simple scale-punch animation to emphasize the new top card.
        punchRoutine = StartCoroutine(PunchScale());
    }

    private IEnumerator PunchScale()
    {
        float half = punchDuration * 0.5f;
        float time = 0f;
        Vector3 targetScale = defaultScale * punchScale;

        while (time < half)
        {
            float t = time / half;
            transform.localScale = Vector3.Lerp(defaultScale, targetScale, t);
            time += Time.deltaTime;
            yield return null;
        }

        time = 0f;
        while (time < half)
        {
            float t = time / half;
            transform.localScale = Vector3.Lerp(targetScale, defaultScale, t);
            time += Time.deltaTime;
            yield return null;
        }

        transform.localScale = defaultScale;
        punchRoutine = null;
    }

}
