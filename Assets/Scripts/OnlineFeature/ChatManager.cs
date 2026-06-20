using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;

public class ChatManager : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private TextMeshProUGUI chatHistoryText;
    [SerializeField] private Button sendButton;

    private void Start()
    {
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendButtonClicked);
        }
        
        if (chatInputField != null)
        {
            chatInputField.onSubmit.AddListener(OnInputSubmit);
        }

        if (chatHistoryText != null)
        {
            chatHistoryText.text = "";
        }
    }

    private void OnSendButtonClicked()
    {
        SendChatMessage();
    }

    private void OnInputSubmit(string text)
    {
        SendChatMessage();
        
        // Giữ focus lại vào input field để có thể chat tiếp nhanh chóng
        if (chatInputField != null)
        {
            chatInputField.ActivateInputField();
        }
    }

    private void SendChatMessage()
    {
        if (chatInputField == null) return;

        string message = chatInputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        // Lấy tên người chơi từ PlayerData
        string playerName = "Player";
        if (PlayerData.Instance != null && !string.IsNullOrEmpty(PlayerData.Instance.PlayerName))
        {
            playerName = PlayerData.Instance.PlayerName;
        }

        // Gửi nội dung chat lên server (Netcode)
        SendChatMessageServerRpc(playerName, message);

        // Xóa nội dung ở ô input sau khi gửi
        chatInputField.text = "";
    }

    // RequireOwnership = false cho phép bất kỳ client nào (kể cả không phải host) cũng gọi được hàm này lên server
    [ServerRpc(RequireOwnership = false)]
    private void SendChatMessageServerRpc(string playerName, string message, ServerRpcParams rpcParams = default)
    {
        // Lấy ID của client đã gửi tin nhắn
        ulong senderId = rpcParams.Receive.SenderClientId;

        // Server nhận được message sẽ phát (broadcast) lại cho toàn bộ các Client kèm senderId
        ReceiveChatMessageClientRpc(playerName, message, senderId);
    }

    [ClientRpc]
    private void ReceiveChatMessageClientRpc(string playerName, string message, ulong senderClientId)
    {
        if (chatHistoryText != null)
        {
            // Xác định xem người gửi có phải là mình (LocalClient) không
            bool isMe = senderClientId == NetworkManager.Singleton.LocalClientId;
            
            // Màu Xanh cho mình, màu Cam Đỏ cho đối phương
            string nameColor = isMe ? "#1D4ED8" : "#D97706"; 
            string msgColor = "#000000"; // Đen

            // Thêm nội dung chat có hỗ trợ Rich Text (Màu sắc, in đậm)
            chatHistoryText.text += $"<color={nameColor}><b>{playerName}</b></color> <color={msgColor}>:</color> <color={msgColor}>{message}</color>\n";
        }
    }
}
