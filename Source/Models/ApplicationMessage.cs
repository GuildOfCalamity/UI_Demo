using System;
using System.Text.Json.Serialization;

namespace UI_Demo;

public class ApplicationMessage : ICloneable
{
    public ModuleId Module { get; set; }
    public string? MessageText { get; set; }
    //[JsonIgnore]
    public Type? MessageType { get; set; }
    public object? MessagePayload { get; set; }
    public DateTime MessageTime { get; set; } = DateTime.Now;
    public object Clone() => this.MemberwiseClone();
    public override string ToString() => $"{Module} => {MessageText} => {MessageTime}";
}

