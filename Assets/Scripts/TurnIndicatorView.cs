using System.Collections;
using TMPro;
using UnityEngine;

public class TurnIndicatorView : MonoBehaviour
{
    [System.Serializable]
    public class PlayerSlot
    {
        public Transform root;
        public GameObject inactivePlate;
        public GameObject activePlate;
        public TMP_Text playerNameText;
    }

    [SerializeField] private PlayerSlot[] playerSlots;

    private static readonly string[] SlotObjectNames =
    {
        "PlayerSlot_Local",
        "PlayerSlot_Left",
        "PlayerSlot_Top",
        "PlayerSlot_Right"
    };

    private PlayerIndexMapper boundMapper;
    private Coroutine bindMapperRoutine;

    private void Awake()
    {
        ResolvePlayerSlots();
        SetActiveDisplayIndex(-1);
        RefreshPlayerNames();
    }

    private void OnEnable()
    {
        GameEvents.OnTurnChanged += HandleTurnChanged;
        bindMapperRoutine = StartCoroutine(BindPlayerMapper());
    }

    private void OnDisable()
    {
        GameEvents.OnTurnChanged -= HandleTurnChanged;

        if (bindMapperRoutine != null)
        {
            StopCoroutine(bindMapperRoutine);
            bindMapperRoutine = null;
        }

        if (boundMapper != null)
        {
            boundMapper.PlayerNamesChanged -= RefreshPlayerNames;
            boundMapper = null;
        }
    }

    private void HandleTurnChanged(int playerIndex)
    {
        SetActiveDisplayIndex(GetDisplayIndex(playerIndex));
    }

    private void SetActiveDisplayIndex(int displayIndex)
    {
        if (playerSlots == null)
            return;

        for (int i = 0; i < playerSlots.Length; i++)
        {
            PlayerSlot slot = playerSlots[i];
            if (slot == null)
                continue;

            bool isActive = i == displayIndex;

            if (slot.inactivePlate != null)
                slot.inactivePlate.SetActive(!isActive);

            if (slot.activePlate != null)
                slot.activePlate.SetActive(isActive);
        }
    }

    private IEnumerator BindPlayerMapper()
    {
        while (PlayerIndexMapper.Instance == null)
            yield return null;

        boundMapper = PlayerIndexMapper.Instance;
        boundMapper.PlayerNamesChanged += RefreshPlayerNames;
        RefreshPlayerNames();
        bindMapperRoutine = null;
    }

    private void RefreshPlayerNames()
    {
        if (playerSlots == null)
            return;

        for (int playerIndex = 0; playerIndex < playerSlots.Length; playerIndex++)
        {
            int displayIndex = GetDisplayIndex(playerIndex);
            if (displayIndex < 0 || displayIndex >= playerSlots.Length)
                continue;

            PlayerSlot slot = playerSlots[displayIndex];
            if (slot == null || slot.playerNameText == null)
                continue;

            slot.playerNameText.text = PlayerIndexMapper.Instance != null
                ? PlayerIndexMapper.Instance.GetPlayerName(playerIndex)
                : $"Player {playerIndex + 1}";
        }
    }

    private void ResolvePlayerSlots()
    {
        if (playerSlots == null || playerSlots.Length != SlotObjectNames.Length)
            playerSlots = new PlayerSlot[SlotObjectNames.Length];

        for (int i = 0; i < SlotObjectNames.Length; i++)
        {
            if (playerSlots[i] == null)
                playerSlots[i] = new PlayerSlot();

            PlayerSlot slot = playerSlots[i];

            if (slot.root == null)
            {
                GameObject rootObject = GameObject.Find(SlotObjectNames[i]);
                if (rootObject != null)
                    slot.root = rootObject.transform;
            }

            if (slot.root == null)
                continue;

            Transform turnView = FindTurnView(slot.root);
            if (turnView == null)
                continue;

            if (slot.inactivePlate == null)
            {
                Transform inactivePlate = turnView.Find("InActivePlate");
                if (inactivePlate != null)
                    slot.inactivePlate = inactivePlate.gameObject;
            }

            if (slot.activePlate == null)
            {
                Transform activePlate = turnView.Find("ActivePlate");
                if (activePlate != null)
                    slot.activePlate = activePlate.gameObject;
            }

            if (slot.playerNameText == null)
            {
                Transform nameText = turnView.Find("PlayerNameText");
                if (nameText != null)
                    slot.playerNameText = nameText.GetComponent<TMP_Text>();
            }
        }
    }

    private static Transform FindTurnView(Transform slotRoot)
    {
        Transform exactMatch = slotRoot.Find("TurnView");
        if (exactMatch != null)
            return exactMatch;

        for (int i = 0; i < slotRoot.childCount; i++)
        {
            Transform child = slotRoot.GetChild(i);
            if (child.name.StartsWith("TurnView"))
                return child;
        }

        return null;
    }

    private int GetDisplayIndex(int playerIndex)
    {
        if (PlayerIndexMapper.Instance == null || PlayerIndexMapper.Instance.PlayerCount == 0)
            return playerIndex;

        int localIndex = PlayerIndexMapper.Instance.GetLocalPlayerIndex();
        if (localIndex < 0)
            return playerIndex;

        int playerCount = PlayerIndexMapper.Instance.PlayerCount;
        return (playerIndex - localIndex + playerCount) % playerCount;
    }
}
