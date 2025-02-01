using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using log4net;
using Microsoft.Data.SqlClient;
using Sentinel.Interfaces;
using Sentinel.Interfaces.Providers;

namespace Sentinel.Providers.DbMonitor;

public class DbMonitoringProvider : ILogProvider, IDisposable
{
    private static readonly ILog Log = LogManager.GetLogger(nameof(DbMonitoringProvider));

    private readonly bool loadExistingContent;

    private readonly Queue<ILogEntry> pendingQueue = new Queue<ILogEntry>();

    private readonly PeriodicTimer _refreshTimer;

    private long _lastIdRead;

    private string _tableName;

    public static IProviderRegistrationRecord ProviderRegistrationInformation { get; } =
        new ProviderRegistrationInformation(new ProviderInfo());

    public string ConnectionString { get; }

    public IProviderInfo Information { get; }

    public IProviderSettings ProviderSettings { get; }

    public ILogger Logger { get; set; }

    public string Name { get; set; }

    public bool IsActive => _workerTask is { IsCompleted: false };

    private BackgroundWorker PurgeWorker { get; set; } = new BackgroundWorker { WorkerReportsProgress = true };

    private Task _workerTask;

    private readonly CancellationTokenSource _workerCancellation = new CancellationTokenSource();

    public DbMonitoringProvider(IProviderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var dbSettings = settings as IDbMonitoringProviderSettings;
        var message = "The DbMonitoringProvider class expects configuration information "
                      + "to be of IDbMonitoringProviderSettings type";

        Debug.Assert(dbSettings != null, message);

        ProviderSettings = dbSettings;
        ConnectionString = dbSettings.ConnectionString;
        Information = settings.Info;
        _refreshTimer = new PeriodicTimer(TimeSpan.FromSeconds(dbSettings.RefreshInSeconds));
        loadExistingContent = dbSettings.LoadExistingContent;
        _tableName = dbSettings.TableName;

        // Chain up callbacks to the workers.
        PurgeWorker.DoWork += PurgeWorkerDoWork;
        PurgeWorker.WorkerSupportsCancellation = true;
    }

    public void Start()
    {
        Debug.Assert(!string.IsNullOrEmpty(ConnectionString), "Filename not specified");
        Debug.Assert(Logger != null, "No logger has been registered, this is required before starting a provider");

        lock (pendingQueue)
        {
            Log.DebugFormat(CultureInfo.InvariantCulture, "Starting of monitoring db");
        }

        _workerTask = MonitorLogs(_workerCancellation.Token);
        PurgeWorker.RunWorkerAsync();
    }

    public void Close()
    {
        _workerCancellation.Cancel();
        PurgeWorker.CancelAsync();
    }

    public void Pause()
    {
        if (_workerTask != null)
        {
            // TODO: need a better pause mechanism...
            Close();
        }
    }

    private void PurgeWorkerDoWork(object sender, DoWorkEventArgs e)
    {
        while (!e.Cancel)
        {
            // Go to sleep.
            Thread.Sleep(_refreshTimer.Period);

            lock (pendingQueue)
            {
                if (pendingQueue.Any())
                {
                    Log.DebugFormat(CultureInfo.InvariantCulture, "Adding a batch of {0} entries to the logger",
                        pendingQueue.Count);
                    Logger.AddBatch(pendingQueue);
                    Trace.WriteLine("Done adding the batch");
                }
            }
        }
    }

    private async Task MonitorLogs(CancellationToken cancellationToken)
    {
        try
        {
            await MonitorLogsInternal(cancellationToken);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error in DbMonitorProvider: {ex}",
                "Error in DbMonitorProvider",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task LoadId(CancellationToken cancellationToken)
    {
        await using var sqlConn = new SqlConnection(ConnectionString);
        await sqlConn.OpenAsync(cancellationToken);

        var sqlCommand = sqlConn.CreateCommand();
        if (!loadExistingContent)
        {
            sqlCommand.CommandText = $"select max(id) from {_tableName}";
            var maxId = await sqlCommand.ExecuteScalarAsync(cancellationToken);
            _lastIdRead = maxId is DBNull ? 0 : Convert.ToInt64(maxId);
        }
        else
        {
            sqlCommand.CommandText = $"select min(t.id) from (select top 1000 id from {_tableName} order by id desc) t";
            var maxId = await sqlCommand.ExecuteScalarAsync(cancellationToken);
            _lastIdRead = maxId is DBNull ? 0 : Convert.ToInt64(maxId);
        }
    }

    private async Task MonitorLogsInternal(CancellationToken cancellationToken)
    {
        await LoadId(cancellationToken);
        await ReadLogs(cancellationToken);
        
        while (!cancellationToken.IsCancellationRequested
               && await _refreshTimer.WaitForNextTickAsync(cancellationToken))
        {
            await ReadLogs(cancellationToken);
        }
    }

    private async Task ReadLogs(CancellationToken cancellationToken)
    {
        await using var sqlConn = new SqlConnection(ConnectionString);
        await sqlConn.OpenAsync(cancellationToken);
        
        var sqlCommand = sqlConn.CreateCommand();
        sqlCommand.CommandText = $"""
                                  select * from {_tableName} 
                                  where id > {_lastIdRead}
                                  order by id
                                  """;

        await using var reader = await sqlCommand.ExecuteReaderAsync(cancellationToken);
        int idIndex = reader.GetOrdinal("Id");
        var dbIndices = new DbIndices(idIndex, 
            reader.GetOrdinal("Date"),
            reader.GetOrdinal("Logger"),
            reader.GetOrdinal("Level"),
            reader.GetOrdinal("Message"),
            reader.GetOrdinal("Exception"));
        while (await reader.ReadAsync(cancellationToken))
        {
            var entry = ReadEntry(reader, dbIndices);
            _lastIdRead = Math.Max(_lastIdRead, reader.GetInt32(idIndex));
            lock (pendingQueue)
                pendingQueue.Enqueue(entry);
        }
    }

    private ILogEntry ReadEntry(SqlDataReader reader, DbIndices dbIndices)
    {
        var entry = new LogEntry
        {
            DateTime = reader.GetDateTime(dbIndices.Date),
            Source = reader.GetString(dbIndices.Logger),
            Type = reader.GetString(dbIndices.Level),
            Description = reader.GetString(dbIndices.Message),
            MetaData = new Dictionary<string, object>(),
        };
        
        if (!reader.IsDBNull(dbIndices.Exception))
        {
            var exc = reader.GetString(dbIndices.Exception);
            if (!string.IsNullOrWhiteSpace(exc))
                entry.MetaData["Exception"] = exc;
        }

        return entry;
    }

    private record DbIndices(int Id, int Date, int Logger, int Level, int Message, int Exception);

    protected virtual void Dispose(bool disposing)
    {
        PurgeWorker?.Dispose();
        PurgeWorker = null;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private class ProviderInfo : IProviderInfo
    {
        public Guid Identifier => new Guid("2CF1F456-447E-4F35-A8B4-6E0F8BF6BD5A");

        public string Name => "Db Monitoring Provider";

        public string Description => "Monitors a database for new log entries.";
    }
}