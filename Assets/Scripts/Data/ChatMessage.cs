using System;

public enum ChatRole
{
    Player,
    AI,
    System
}

[Serializable]
public class ChatMessage
{
    public ChatRole Role { get; }
    public string Message { get; }
    public DateTime Time { get; }

    public ChatMessage(ChatRole role, string message)
    {
        Role = role;
        Message = message;
        Time = DateTime.Now;
    }
}
