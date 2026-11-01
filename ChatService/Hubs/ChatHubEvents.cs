namespace ChatService.Hubs;

/// <summary>
/// Server → client event names from <c>contracts/signalr-hub.md</c>. These are
/// part of the v1 hub contract and may only change additively (Constitution V).
/// </summary>
public static class ChatHubEvents
{
    public const string MessageReceived = "MessageReceived";

    public const string TypingChanged = "TypingChanged";

    public const string PresenceChanged = "PresenceChanged";

    public const string ReadReceiptUpdated = "ReadReceiptUpdated";
}
