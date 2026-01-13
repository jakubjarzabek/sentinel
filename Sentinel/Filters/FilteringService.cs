namespace Sentinel.Filters;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Input;

using Sentinel.Filters.Gui;
using Sentinel.Filters.Interfaces;
using Sentinel.Interfaces;
using Sentinel.Services;

using WpfExtras;

[DataContract]
public class FilteringService<T> : ViewModelBase, IFilteringService<T>, IDefaultInitialisation
    where T : class, IFilter
{
    private readonly CollectionChangeHelper<T> _collectionHelper = new CollectionChangeHelper<T>();

    private readonly IAddFilterService _addFilterService = new AddFilter();

    private readonly IEditFilterService _editFilterService = new EditFilter();

    private readonly IRemoveFilterService _removeFilterService = new RemoveFilter();

    private int selectedIndex = -1;

    public FilteringService()
    {
        Add = new DelegateCommand(AddFilter);
        Edit = new DelegateCommand(EditFilter, e => selectedIndex != -1);
        Remove = new DelegateCommand(RemoveFilter, e => selectedIndex != -1);

        Filters = new ObservableCollection<T>();
        SearchFilters = new ObservableCollection<T>();

        // Register self as an observer of the collection.
        _collectionHelper.OnPropertyChanged += CustomFilterPropertyChanged;

        _collectionHelper.ManagerName = "FilteringService";
        _collectionHelper.NameLookup += e => e.Name;

        Filters.CollectionChanged += _collectionHelper.AttachDetach;
        SearchFilters.CollectionChanged += _collectionHelper.AttachDetach;

        var searchFilter = ServiceLocator.Instance.Get<ISearchFilter>();

        if (searchFilter != null)
        {
            SearchFilters.Add(searchFilter as T);
        }
        else
        {
            Debug.Fail("The search filter is null.");
        }
    }

    public ICommand Add { get; private set; }

    public ICommand Edit { get; private set; }

    public ICommand Remove { get; private set; }

    public int SelectedIndex
    {
        get => selectedIndex;

        set
        {
            if (value != selectedIndex)
            {
                selectedIndex = value;
                OnPropertyChanged(nameof(SelectedIndex));
            }
        }
    }

    public ObservableCollection<T> Filters { get; set; }

    public ObservableCollection<T> SearchFilters { get; set; }

    public void Initialise()
    {
        // Add the standard debugging filters
        Filters.Add(new StandardFilter("Trace", LogEntryFields.Type, "TRACE") as T);
        Filters.Add(new StandardFilter("Debug", LogEntryFields.Type, "DEBUG") as T);
        Filters.Add(new StandardFilter("Info", LogEntryFields.Type, "INFO") as T);
        Filters.Add(new StandardFilter("Warn", LogEntryFields.Type, "WARN") as T);
        Filters.Add(new StandardFilter("Error", LogEntryFields.Type, "ERROR") as T);
        Filters.Add(new StandardFilter("Fatal", LogEntryFields.Type, "FATAL") as T);
    }

    public bool IsMatch(ILogEntry entry)
    {
        var activeFilters = Filters.Concat(SearchFilters).Where(f => f.Enabled).ToList();
        return activeFilters.Count == 0 || activeFilters.Any(filter => filter.IsMatch(entry));
    }

    private void AddFilter(object obj)
    {
        _addFilterService.Add();
    }

    private void CustomFilterPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        var filter = sender as Filter;
        if (filter != null)
        {
            Trace.WriteLine(
                $"FilteringService saw some activity on {filter.Name} (IsEnabled = {filter.Enabled})");
        }

        OnPropertyChanged(string.Empty);
    }

    private void EditFilter(object obj)
    {
        var filter = Filters.ElementAt(SelectedIndex);
        if (filter != null)
        {
            _editFilterService.Edit(filter);
        }
    }

    private void RemoveFilter(object obj)
    {
        var filter = Filters.ElementAt(SelectedIndex);
        _removeFilterService.Remove(filter);
    }
}