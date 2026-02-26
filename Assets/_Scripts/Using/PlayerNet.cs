using Unity.Netcode;
using TMPro;
using UnityEngine;
using Unity.Collections;

public class PlayerNet : NetworkBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;

    NetworkVariable<FixedString32Bytes> playerName =
        new NetworkVariable<FixedString32Bytes>();

    public override void OnNetworkSpawn()
    {
        playerName.OnValueChanged += OnNameChanged;

        nameText.text = playerName.Value.ToString();

        if (IsOwner)
        {
            SubmitNameServerRpc(
                PlayerData.Instance.PlayerName);
        }
    }


    void OnNameChanged(
        FixedString32Bytes oldValue,
        FixedString32Bytes newValue)
    {
        nameText.text = newValue.ToString();
    }

    [ServerRpc]
    void SubmitNameServerRpc(string name)
    {
        Debug.Log("Server nhận tên: " + name);
        playerName.Value = name;
    }

}
