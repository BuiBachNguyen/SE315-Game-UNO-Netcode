using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;

public class ChatManager : NetworkBehaviour
{
    private const string JoinRoomMessage = "I have joined the room.";
    private const int MaxChatHistoryMessages = 100;
    private const int MaxPlayerNameLength = 60;
    private const int MaxMessageLength = 500;

    [Header("UI References")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private TextMeshProUGUI chatHistoryText;
    [SerializeField] private Button sendButton;

    private readonly List<ChatMessageEntry> chatHistory = new List<ChatMessageEntry>();
    private bool hasLoadedChatHistory;
    private bool hasSentJoinRoomMessage;

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

        if (chatHistoryText != null && !hasLoadedChatHistory)
        {
            chatHistoryText.text = "";
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            RequestChatHistoryServerRpc();
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
        string playerName = GetLocalPlayerName();

        // Gửi nội dung chat lên server (Netcode)
        SendChatMessageServerRpc(playerName, message);

        // Xóa nội dung ở ô input sau khi gửi
        chatInputField.text = "";
    }

    private IEnumerator SendJoinRoomMessageWhenReady()
    {
        if (hasSentJoinRoomMessage)
            yield break;

        hasSentJoinRoomMessage = true;

        yield return null;

        SendChatMessageServerRpc(GetLocalPlayerName(), JoinRoomMessage);
    }

    private string GetLocalPlayerName()
    {
        if (PlayerData.Instance != null && !string.IsNullOrWhiteSpace(PlayerData.Instance.PlayerName))
            return TrimToMaxLength(PlayerData.Instance.PlayerName.Trim(), MaxPlayerNameLength);

        return "Player";
    }

    // RequireOwnership = false cho phép bất kỳ client nào (kể cả không phải host) cũng gọi được hàm này lên server
    [ServerRpc(RequireOwnership = false)]
    private void RequestChatHistoryServerRpc(ServerRpcParams rpcParams = default)
    {
        ReceiveChatHistoryClientRpc(
            chatHistory.ToArray(),
            CreateTargetParams(rpcParams.Receive.SenderClientId));
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendChatMessageServerRpc(string playerName, string message, ServerRpcParams rpcParams = default)
    {
        // Lấy ID của client đã gửi tin nhắn
        ulong senderId = rpcParams.Receive.SenderClientId;
        string safePlayerName = TrimToMaxLength((playerName ?? string.Empty).Trim(), MaxPlayerNameLength);
        string safeMessage = TrimToMaxLength((message ?? string.Empty).Trim(), MaxMessageLength);

        if (string.IsNullOrWhiteSpace(safePlayerName))
            safePlayerName = "Player";

        if (string.IsNullOrWhiteSpace(safeMessage))
            return;

        ChatMessageEntry chatMessage = new ChatMessageEntry(safePlayerName, safeMessage, senderId);

        chatHistory.Add(chatMessage);
        if (chatHistory.Count > MaxChatHistoryMessages)
            chatHistory.RemoveAt(0);

        // Server nhận được message sẽ phát (broadcast) lại cho toàn bộ các Client kèm senderId
        ReceiveChatMessageClientRpc(safePlayerName, safeMessage, senderId);
    }

    private ClientRpcParams CreateTargetParams(ulong clientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        };
    }

    //[ClientRpc]
    //private void ReceiveChatMessageClientRpc(string playerName, string message, ulong senderClientId)
    //{
    //    if (chatHistoryText != null)
    //    {
    //        // Xác định xem người gửi có phải là mình (LocalClient) không
    //        bool isMe = senderClientId == NetworkManager.Singleton.LocalClientId;

    //        // Màu Xanh cho mình, màu Cam Đỏ cho đối phương
    //        string nameColor = isMe ? "#1D4ED8" : "#D97706"; 
    //        string msgColor = "#000000"; // Đen

    //        // Thêm nội dung chat có hỗ trợ Rich Text (Màu sắc, in đậm)
    //        chatHistoryText.text += $"<color={nameColor}><b>{playerName}</b></color> <color={msgColor}>:</color> <color={msgColor}>{message}</color>\n";
    //    }
    //}
    [ClientRpc]
    private void ReceiveChatMessageClientRpc(string playerName, string message, ulong senderClientId)
    {
        AppendChatMessage(playerName, message, senderClientId);
    }

    [ClientRpc]
    private void ReceiveChatHistoryClientRpc(ChatMessageEntry[] messages, ClientRpcParams rpcParams = default)
    {
        hasLoadedChatHistory = true;

        if (chatHistoryText != null)
        {
            chatHistoryText.text = "";

            foreach (ChatMessageEntry message in messages)
            {
                AppendChatMessage(
                    message.PlayerName.ToString(),
                    message.Message.ToString(),
                    message.SenderClientId);
            }
        }

        StartCoroutine(SendJoinRoomMessageWhenReady());
    }

    private static string TrimToMaxLength(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength);
    }

    private void AppendChatMessage(string playerName, string message, ulong senderClientId)
    {
        if (chatHistoryText != null)
        {
            bool isMe = senderClientId == NetworkManager.Singleton.LocalClientId;

            string displayName = isMe ? "Me" : playerName;

            string nameColor = isMe ? "#1D4ED8" : "#D97706";
            string msgColor = "#000000";

            chatHistoryText.text +=
                $"<color={nameColor}><b>{displayName}</b></color> " +
                $"<color={msgColor}>:</color> " +
                $"<color={msgColor}>{message}</color>\n";
        }
    }

    private struct ChatMessageEntry : INetworkSerializable
    {
        public FixedString64Bytes PlayerName;
        public FixedString512Bytes Message;
        public ulong SenderClientId;

        public ChatMessageEntry(string playerName, string message, ulong senderClientId)
        {
            PlayerName = new FixedString64Bytes(playerName);
            Message = new FixedString512Bytes(message);
            SenderClientId = senderClientId;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref Message);
            serializer.SerializeValue(ref SenderClientId);
        }
    }
}
