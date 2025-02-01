using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using Sentinel.Interfaces.Providers;
using Sentinel.WpfExtras;

namespace Sentinel.Providers.DbMonitor;

/// <summary>
///   Interaction logic for FileMonitorProviderPage.xaml.
/// </summary>
public partial class DbMonitorProviderPage : IWizardPage, IDataErrorInfo
{
    private readonly ObservableCollection<IWizardPage> children = new ObservableCollection<IWizardPage>();

    private readonly ReadOnlyObservableCollection<IWizardPage> readonlyChildren;

    private bool warnFileNotFound;

    private bool isValid;

    public DbMonitorProviderPage()
    {
        InitializeComponent();

        DataContext = this;
        readonlyChildren = new ReadOnlyObservableCollection<IWizardPage>(children);

        PropertyChanged += PropertyChangedHandler;

        // Need a subsequent page to define columns.
        //AddChild(new MessageFormatPage());
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public string ConnectionString
    {
        get => _connectionString;

        set
        {
            if (_connectionString != value)
            {
                _connectionString = value;
                OnPropertyChanged(nameof(ConnectionString));
            }
        }
    }
    private string _connectionString = string.Empty;

    public string TableName
    {
        get => _tableName;

        set
        {
            if (_tableName != value)
            {
                _tableName = value;
                OnPropertyChanged(nameof(TableName));
            }
        }
    }
    private string _tableName = string.Empty;

    public bool WarnFileNotFound
    {
        get => warnFileNotFound;

        set
        {
            if (warnFileNotFound != value)
            {
                Trace.WriteLine("Setting WarnFileNotFound to " + value);
                warnFileNotFound = value;
                OnPropertyChanged(nameof(WarnFileNotFound));
            }
        }
    }

    public double Refresh
    {
        get => refresh;

        set
        {
            if (Math.Abs(refresh - value) > 0.01)
            {
                refresh = value;
                OnPropertyChanged(nameof(Refresh));
            }
        }
    }
    private double refresh;

    public int MaxRefresh => 25;

    public int MinRefresh => 5;

    public bool LoadExisting
    {
        get => loadExisting;

        set
        {
            if (loadExisting != value)
            {
                loadExisting = value;
                OnPropertyChanged(nameof(LoadExisting));
            }
        }
    }
    private bool loadExisting = true;

    public string Title => "Log file monitoring provider";

    public ReadOnlyObservableCollection<IWizardPage> Children => readonlyChildren;

    public Control PageContent => this;

    public string Description => "Configure Sentinel to monitor a db for log entries";

    public bool IsValid
    {
        get => isValid;

        private set
        {
            if (isValid != value)
            {
                isValid = value;
                OnPropertyChanged(nameof(IsValid));
            }
        }
    }

    /// <summary>
    ///   Gets an error message indicating what is wrong with this object.
    /// </summary>
    /// <returns>
    ///   An error message indicating what is wrong with this object. The default is an empty string ("").
    /// </returns>
    public string Error => this["FileName"];

    /// <summary>
    ///   Gets the error message for the property with the given name.
    /// </summary>
    /// <param name = "columnName">The name of the property whose error message to get.</param>
    /// <returns>The error message for the property. The default is an empty string ("").</returns>
    public string this[string columnName]
    {
        get
        {
            if (columnName != nameof(ConnectionString))
                return null;

            if (string.IsNullOrWhiteSpace(_connectionString))
                return "Connection string not specified";

            string reason;
            return ConnectionStringIsValid(ConnectionString, out reason) ? null : reason;
        }
    }

    public object Save(object saveData)
    {
        Debug.Assert(saveData != null, "Expecting a non-null instance of a class to save settings into");

        if (saveData is IProviderSettings settings)
        {
            if (settings is IDbMonitoringProviderSettings fileSettings)
            {
                fileSettings.Update(_connectionString, _tableName, (int)Refresh, LoadExisting);
                return fileSettings;
            }

            return new DbMonitoringProviderSettings(
                settings.Name,
                settings.Info,
                _connectionString,
                _tableName,
                (int)Refresh,
                LoadExisting);
        }

        return saveData;
    }

    public void AddChild(IWizardPage newItem)
    {
        children.Add(newItem);
        OnPropertyChanged(nameof(Children));
    }

    public void RemoveChild(IWizardPage item)
    {
        children.Remove(item);
        OnPropertyChanged(nameof(Children));
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        var handler = PropertyChanged;
        if (handler != null)
        {
            var e = new PropertyChangedEventArgs(propertyName);
            handler(this, e);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Refresh = MinRefresh;
    }

    private void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ConnectionString))
        {
            return;
        }

        try
        {
            IsValid = ConnectionStringIsValid(ConnectionString, out _);
        }
        catch (Exception)
        {
            // For exceptions, let the validation handler show the error.
            WarnFileNotFound = false;
            IsValid = false;
        }
    }

    private bool ConnectionStringIsValid(string connectionString, out string reason)
    {
        try
        {
            reason = null;
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            return true;
        }
        catch (ArgumentException)
        {
            reason = "The supplied connection string is invalid";
        }
        catch (InvalidOperationException)
        {
            reason = "Cannot open a connection without specifying a data source or server.";
        }
        return false;
    }
}