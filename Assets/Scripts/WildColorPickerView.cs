using UnityEngine;
using UnityEngine.UI;

public class WildColorPickerView : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject backgroundBlocker;
    [SerializeField] private Button redButton;
    [SerializeField] private Button greenButton;
    [SerializeField] private Button blueButton;
    [SerializeField] private Button yellowButton;

    private void Start()
    {
        SetVisible(false);
    }

    private void Awake()
    {
        if (redButton != null)
        {
            redButton.onClick.AddListener(() => SelectColor(CardColor.Red));
        }

        if (greenButton != null)
        {
            greenButton.onClick.AddListener(() => SelectColor(CardColor.Green));
        }

        if (blueButton != null)
        {
            blueButton.onClick.AddListener(() => SelectColor(CardColor.Blue));
        }

        if (yellowButton != null)
        {
            yellowButton.onClick.AddListener(() => SelectColor(CardColor.Yellow));
        }
    }

    private void OnEnable()
    {
        GameEvents.OnWildPlayed += HandleWildPlayed;
    }

    private void OnDisable()
    {
        GameEvents.OnWildPlayed -= HandleWildPlayed;
    }

    private void HandleWildPlayed()
    {
        SetVisible(true);
    }

    private void SelectColor(CardColor color)
    {
        GameEvents.RaiseColorSelected(color);
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (panel != null)
        {
            panel.SetActive(visible);
        }

        if (backgroundBlocker != null)
        {
            // Blocks clicks to prevent interacting with the UI under the color picker.
            backgroundBlocker.SetActive(visible);
        }
    }
}
