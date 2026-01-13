namespace Sentinel.Extractors;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Input;

using Sentinel.Extractors.Gui;
using Sentinel.Extractors.Interfaces;
using Sentinel.Interfaces;
using Sentinel.Services;

using WpfExtras;

[DataContract]
public class ExtractingService<T> : ViewModelBase, IExtractingService<T>, IDefaultInitialisation
    where T : class, IExtractor
{
    private readonly CollectionChangeHelper<T> _collectionHelper = new CollectionChangeHelper<T>();

    private readonly AddExtractor _addExtractorService = new AddExtractor();

    private readonly EditExtractor _editExtractorService = new EditExtractor();

    private readonly RemoveExtractor _removeExtractorService = new RemoveExtractor();

    private int _selectedIndex = -1;

    public ObservableCollection<T> Extractors { get; set; } = [];

    public ObservableCollection<T> SearchExtractors { get; set; } = [];

    public ExtractingService()
    {
        Add = new DelegateCommand(AddExtractor);
        Edit = new DelegateCommand(EditExtractor, e => _selectedIndex != -1);
        Remove = new DelegateCommand(RemoveExtractor, e => _selectedIndex != -1);

        // Register self as an observer of the collection.
        _collectionHelper.OnPropertyChanged += CustomExtractorPropertyChanged;

        _collectionHelper.ManagerName = "ExtractingService";
        _collectionHelper.NameLookup += e => e.Name;

        Extractors.CollectionChanged += _collectionHelper.AttachDetach;
        SearchExtractors.CollectionChanged += _collectionHelper.AttachDetach;

        var searchExtractor = ServiceLocator.Instance.Get<ISearchExtractor>();

        if (searchExtractor != null)
        {
            SearchExtractors.Add(searchExtractor as T);
        }
        else
        {
            Debug.Fail("The search extractor is null.");
        }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public ICommand Add { get; private set; }

    // ReSharper disable once MemberCanBePrivate.Global
    public ICommand Edit { get; private set; }

    // ReSharper disable once MemberCanBePrivate.Global
    public ICommand Remove { get; private set; }

    // ReSharper disable once MemberCanBePrivate.Global
    public int SelectedIndex
    {
        get
        {
            return _selectedIndex;
        }

        set
        {
            if (value != _selectedIndex)
            {
                _selectedIndex = value;
                OnPropertyChanged(nameof(SelectedIndex));
            }
        }
    }

    public void Initialise()
    {
        // For adding standard extractors
    }

    public bool IsFiltered(ILogEntry entry)
    {
        return Extractors.Any(filter => filter.Enabled && filter.IsMatch(entry))
               || SearchExtractors.Any(filter => filter.Enabled && filter.IsMatch(entry));
    }

    private void AddExtractor(object obj)
    {
        _addExtractorService.Add();
    }

    private void CustomExtractorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is Extractor extractor)
        {
            Trace.WriteLine(
                $"ExtractingService saw some activity on {extractor.Name} (IsEnabled = {extractor.Enabled})");
        }

        OnPropertyChanged(string.Empty);
    }

    private void EditExtractor(object obj)
    {
        var extractor = Extractors.ElementAt(SelectedIndex);
        if (extractor != null)
        {
            _editExtractorService.Edit(extractor);
        }
    }

    private void RemoveExtractor(object obj)
    {
        var extractor = Extractors.ElementAt(SelectedIndex);
        _removeExtractorService.Remove(extractor);
    }
}