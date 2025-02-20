using System;
using System.Text.Json.Serialization;

namespace UI_Demo;

public class ApplicationMessage
{
    public ModuleId Module { get; set; }
    public string? MessageText { get; set; }
    //[JsonIgnore]
    public Type? MessageType { get; set; }
    public object? MessagePayload { get; set; }
    public DateTime MessageTime { get; set; } = DateTime.Now;
    public override string ToString() => $"{Module} => {MessageText} => {MessageTime}";
}

