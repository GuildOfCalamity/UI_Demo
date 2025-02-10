using System;
using System.Collections.Concurrent;

namespace UI_Demo;

public class PubSubService<T>
{
    readonly ConcurrentQueue<T>? _messageQueue;
    static readonly Lazy<PubSubService<T>> _instance = new(() => new PubSubService<T>());
    
    /// <summary>
    /// Constructor will only be called when the Instance property is accessed.
    /// </summary>
    public static PubSubService<T> Instance => _instance.Value;

    /// <summary>
    /// Event triggered when a new message is received
    /// </summary>
    public event Action<T>? MessageReceived;

    /// <summary>
    /// Private constructor to prevent external instantiation.
    /// </summary>
    private PubSubService()
    {
        _messageQueue = new ConcurrentQueue<T>();
    }

    /// <summary>
    /// Sends a message to all subscribed consumers.
    /// </summary>
    public void SendMessage(T message)
    {
        if (message is null) { return; }
        _messageQueue?.Enqueue(message);
        MessageReceived?.Invoke(message);
    }

    /// <summary>
    /// Subscribes a consumer to listen for messages.
    /// </summary>
    public void Subscribe(Action<T> listener)
    {
        MessageReceived += listener;
    }

    /// <summary>
    /// Unsubscribes a consumer from message notifications.
    /// </summary>
    public void Unsubscribe(Action<T> listener)
    {
        MessageReceived -= listener;
    }
}

public class ApplicationMessage
{
    public ModuleId Module { get; set; }
    public string? MessageText { get; set; }
    public Type? MessageType { get; set; }
    public object? MessagePayload { get; set; }
    public DateTime MessageTime { get; set; } = DateTime.Now;
}

public enum ModuleId
{
    App = 0,
    MainWindow = 1,
    MainPage = 2,
    ControlsPage = 3,
}