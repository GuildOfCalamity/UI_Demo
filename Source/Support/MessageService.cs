using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace UI_Demo;

#region [Non-Generic]
/// <summary>
///   Allows multiple consumers to read messages asynchronously.
/// </summary>
/// <remarks>
///   This is first come first serve, so whomever reads the message first will get it.
/// </remarks>
public class MessageService
{
    readonly int _maxMessages = 1000;
    readonly Channel<string> _messageChannel;
    
    // Constructor will only be called when the Instance property is accessed.
    static readonly Lazy<MessageService> _instance = new Lazy<MessageService>(() => new MessageService());
    public static MessageService Instance => _instance.Value;

    /// <summary>
    /// Private constructor to prevent external instantiation.
    /// </summary>
    private MessageService()
    {
        
        var options = new BoundedChannelOptions(_maxMessages)
        {
            //AllowSynchronousContinuations = false, // async only
            FullMode = BoundedChannelFullMode.DropOldest // remove oldest message when full
        };

        //_messageChannel = Channel.CreateUnbounded<string>();
        _messageChannel = Channel.CreateBounded<string>(options);
    }

    /// <summary>
    /// Writes a message to the channel.
    /// </summary>
    public async Task SendMessageAsync(string message, CancellationToken token = default)
    {
        await _messageChannel.Writer.WriteAsync(message, token);
    }

    /// <summary>
    /// Allows multiple consumers to read messages asynchronously.
    /// </summary>
    public ChannelReader<string> GetMessageReader()
    {
        return _messageChannel.Reader;
    }

    /// <summary>
    /// Marks the channel as completed, signaling no more messages will be written.
    /// </summary>
    public void Complete()
    {
        _messageChannel.Writer.Complete();
    }

    /// <summary>
    /// Return the maximum amount of messages that the channel will allow.
    /// </summary>
    public int GetMaxmimumLimit() => _maxMessages;
}
#endregion

#region [Generic]
/// <summary>
///   Allows multiple consumers to read messages asynchronously (generic version).
/// </summary>
/// <remarks>
///   This is first come first serve, so whomever reads the message first will get it.
/// </remarks>
public class MessageService<T>
{
    readonly int _maxMessages = 1000;
    readonly Channel<T> _messageChannel;

    // Constructor will only be called when the Instance property is accessed.
    static readonly Lazy<MessageService<T>> _instance = new(() => new MessageService<T>());
    public static MessageService<T> Instance => _instance.Value;

    /// <summary>
    /// Private constructor to prevent external instantiation.
    /// </summary>
    private MessageService()
    {
        var options = new BoundedChannelOptions(_maxMessages)
        { 
            //AllowSynchronousContinuations = false, // async only
            FullMode = BoundedChannelFullMode.DropOldest // remove oldest message when full
        };

        //_messageChannel = Channel.CreateUnbounded<T>(); // Supports any T-type messages
        _messageChannel = Channel.CreateBounded<T>(options); // Supports any T-type messages
    }

    /// <summary>
    /// Sends a message to the channel.
    /// </summary>
    public async Task SendMessageAsync(T message, CancellationToken token = default)
    {
        await _messageChannel.Writer.WriteAsync(message, token);
    }

    /// <summary>
    /// Gets a ChannelReader<T> so consumers can listen to messages.
    /// </summary>
    public ChannelReader<T> GetMessageReader()
    {
        return _messageChannel.Reader;
    }

    /// <summary>
    /// Completes the message channel, signaling that no more messages will be written.
    /// </summary>
    public void Complete()
    {
        _messageChannel.Writer.Complete();
    }

    /// <summary>
    /// Return the maximum amount of messages that the channel will allow.
    /// </summary>
    public int GetMaxmimumLimit() => _maxMessages;

}
#endregion
