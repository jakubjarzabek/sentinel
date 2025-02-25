using System.Net;
using System.Net.Sockets;
using System.Text;
using log4net;
using Newtonsoft.Json.Linq;
using Sentinel.Interfaces;
using Sentinel.Interfaces.Providers;

namespace Sentinel.Providers.MSBuild;

public class MsBuildProvider : INetworkProvider
{
    public static readonly IProviderRegistrationRecord ProviderRegistrationRecord =
        new ProviderRegistrationInformation(new ProviderInfo());

    private const int PumpFrequency = 100;

    private static readonly ILog Log = LogManager.GetLogger(typeof(MsBuildProvider));

    private readonly Queue<string> pendingQueue = new Queue<string>();

    private CancellationTokenSource cancellationTokenSource;

    private Task listenerTask;

    public MsBuildProvider(IProviderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Settings = settings as IMsBuildListenerSettings;
        ArgumentNullException.ThrowIfNull(Settings);

        ProviderSettings = settings;
    }

    public IProviderInfo Information { get; private set; }

    public IProviderSettings ProviderSettings { get; private set; }

    public ILogger Logger { get; set; }

    public string Name { get; set; }

    public bool IsActive => listenerTask != null && listenerTask.Status == TaskStatus.Running;

    public int Port { get; private set; }

    protected IMsBuildListenerSettings Settings { get; set; }

    public void Start()
    {
        Log.Debug("Start requested");

        if (listenerTask == null || listenerTask.IsCompleted)
        {
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            listenerTask = Task.Factory.StartNew(SocketListener, token);
            Task.Factory.StartNew(MessagePump, token);
        }
        else
        {
            Log.Warn("UDP listener task is already active and can not be started again.");
        }
    }

    public void Pause()
    {
        Log.Debug("Pause requested");
        if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
        {
            Log.Debug("Cancellation token triggered");
            cancellationTokenSource.Cancel();
        }
    }

    public void Close()
    {
        Log.Debug("Close requested");
        if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
        {
            Log.Debug("Cancellation token triggered");
            cancellationTokenSource.Cancel();
        }
    }

    private void SocketListener()
    {
        Log.Debug("SocketListener started");

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var endPoint = new IPEndPoint(IPAddress.Any, 9123);

            using (var listener = new UdpClient(endPoint))
            {
                var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                listener.Client.ReceiveTimeout = 1000;

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        var bytes = listener.Receive(ref remoteEndPoint);

                        if (Log.IsDebugEnabled)
                            Log.DebugFormat("Received {0} bytes from {1}", bytes.Length, remoteEndPoint.Address);

                        var message = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                        lock (pendingQueue)
                        {
                            pendingQueue.Enqueue(message);
                        }
                    }
                    catch (SocketException socketException)
                    {
                        if (socketException.SocketErrorCode != SocketError.TimedOut)
                        {
                            Log.Error("SocketException", socketException);
                            Log.Debug($"SocketException.SocketErrorCode = {socketException.SocketErrorCode}");

                            // Break out of the 'using socket' loop and try to establish a new socket.
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error("UdpClient Exception", e);
                    }
                }
            }
        }

        Log.Debug("SocketListener completed");
    }

    private void MessagePump()
    {
        Log.Debug("MessagePump started");

        var processedQueue = new Queue<ILogEntry>();

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            Thread.Sleep(PumpFrequency);

            try
            {
                if (Logger != null)
                {
                    lock (pendingQueue)
                    {
                        while (pendingQueue.Count > 0)
                        {
                            var message = pendingQueue.Dequeue();

                            // TODO: validate
                            if (IsValidMessage(message))
                            {
                                var deserializeMessage = DeserializeMessage(message);

                                if (deserializeMessage != null)
                                {
                                    processedQueue.Enqueue(deserializeMessage);
                                }
                            }
                        }
                    }

                    if (processedQueue.Any())
                    {
                        Logger.AddBatch(processedQueue);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("MessagePump Exception", e);
            }
            finally
            {
                processedQueue.Clear();
            }
        }

        Log.Debug("MessagePump completed");
    }

    private ILogEntry DeserializeMessage(string message)
    {
        try
        {
            var json = JToken.Parse(message);

            var jsonObject = json as JObject;

            if (jsonObject != null && jsonObject.Children().Count() == 1)
            {
                var property = jsonObject.Children().First() as JProperty;

                if (property == null)
                {
                    Log.Error("First item in JObject should be a property");
                }
                else
                {
                    var msbuildEventType = property.Name;
                    var content = property.Value as JObject;

                    if (string.IsNullOrWhiteSpace(msbuildEventType) || content == null)
                    {
                        Log.ErrorFormat(
                            "Expected payload to consist of a property corresponding to the MSBuild event type name, "
                            + "and a value which is the serialized object corresponding to the type.");
                    }
                    else
                    {
                        return new LogEntry(msbuildEventType, content);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.Error("Deserialization exception trying to turn the JSON content into a LogMessage", e);
        }

        return null;
    }

    private bool IsValidMessage(string message)
    {
        // TODO: validation logic required.
        return true;
    }
}