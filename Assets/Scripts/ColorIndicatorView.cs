using UnityEngine;
using UnityEngine.UI;

public class ColorIndicatorView : MonoBehaviour
{
    [SerializeField] private Image colorImage;

    private void OnEnable()
    {
        GameEvents.OnColorChanged += HandleColorChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnColorChanged -= HandleColorChanged;
    }

    private void HandleColorChanged(CardColor color)
    {
        if (colorImage == null)
        {
            return;
        }

        colorImage.color = ToUnityColor(color);
    }

    private static Color ToUnityColor(CardColor color)
    {
        switch (color)
        {
            case CardColor.Red:    return new Color(0.9f, 0.2f, 0.2f, 1f);
            case CardColor.Green:  return new Color(0.2f, 0.7f, 0.3f, 1f);
            case CardColor.Blue:   return new Color(0.2f, 0.4f, 0.9f, 1f);
            case CardColor.Yellow: return new Color(0.95f, 0.8f, 0.2f, 1f);
            default:               return Color.white;
        }
    }
}
