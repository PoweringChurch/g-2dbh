using Godot;
using System;
using System.Collections.Generic;

public partial class MessageDisplay : Label
{
    private Dictionary<string, string> _messages = new();
    public int MessageCount => _messages.Count;
    public void SetMessage(string field, string message)
    {
        _messages[field] = message;
        RefreshMessage();
    }
    public void ClearMessage(string field)
    {
        _messages.Remove(field);
        RefreshMessage();
    }
    private void RefreshMessage() =>
        Text = string.Join("\n", _messages.Values);
}
