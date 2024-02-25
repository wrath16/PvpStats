using PvpStats.Types.Player;
using System;

namespace PvpStats.Types;
public class ChatMessage {
    public int Channel { get; set; }
    public string Message { get; set; }
    public DateTime Time { get; set; }
    public PlayerAlias? Source { get; set; }

    public ChatMessage(string message) {
        Message = message;
    }
}
