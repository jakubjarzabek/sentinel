namespace Sentinel.Filters;

using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

using Sentinel.Filters.Interfaces;
using Sentinel.Interfaces;

using WpfExtras;

[DataContract]
public class Filter : ViewModelBase, IFilter
{
    /// <summary>
    /// Is the filter enabled?  If so, it will remove anything matching from the output.
    /// </summary>
    private bool enabled;

    private string name;

    private string pattern;

    private LogEntryFields field;

    private MatchMode mode = MatchMode.Exact;

    private Regex _regex;

    public Filter()
    {
        PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(Field) || e.PropertyName == nameof(Mode) || e.PropertyName == nameof(Pattern))
            {
                if (Mode == MatchMode.RegularExpression && Pattern != null)
                {
                    _regex = new Regex(Pattern);
                }

                OnPropertyChanged(nameof(Description));
            }
        };
    }

    public Filter(string name, LogEntryFields field, string pattern)
    {
        Name = name;
        Pattern = pattern;
        Field = field;
        _regex = new Regex(pattern);

        PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(Field) || e.PropertyName == nameof(Mode) || e.PropertyName == nameof(Pattern))
            {
                if (Mode == MatchMode.RegularExpression && Pattern != null)
                {
                    _regex = new Regex(Pattern);
                }

                OnPropertyChanged(nameof(Description));
            }
        };
    }

    public string Name
    {
        get
        {
            return name;
        }

        set
        {
            if (name != value)
            {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the filter is enabled.
    /// </summary>
    public bool Enabled
    {
        get
        {
            return enabled;
        }

        set
        {
            if (value != enabled)
            {
                enabled = value;
                OnPropertyChanged(nameof(Enabled));
            }
        }
    }

    public string Pattern
    {
        get
        {
            return pattern;
        }

        set
        {
            if (pattern != value)
            {
                pattern = value;
                OnPropertyChanged(nameof(Pattern));
            }
        }
    }

    public LogEntryFields Field
    {
        get
        {
            return field;
        }

        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(Field));
            }
        }
    }

    public MatchMode Mode
    {
        get
        {
            return mode;
        }

        set
        {
            if (mode != value)
            {
                mode = value;
                OnPropertyChanged(nameof(Mode));
            }
        }
    }

    public string Description
    {
        get
        {
            var modeDescription = Mode switch
            {
                MatchMode.RegularExpression => "RegEx",
                MatchMode.Contains => "Case sensitive",
                MatchMode.CaseInsensitive => "Case insensitive",
                _ => "Exact"
            };

            return $"{modeDescription} match of {Pattern} in the {Field} field";
        }
    }

    public bool IsMatch(ILogEntry logEntry)
    {
        ArgumentNullException.ThrowIfNull(logEntry);

        if (string.IsNullOrWhiteSpace(Pattern))
            return true;

        var target = logEntry.GetField(Field);

        return Mode switch
        {
            MatchMode.Exact => string.Equals(target, Pattern, StringComparison.OrdinalIgnoreCase),
            MatchMode.Contains => target.Contains(Pattern),
            MatchMode.CaseInsensitive => target.Contains(Pattern, StringComparison.CurrentCultureIgnoreCase),
            MatchMode.RegularExpression => _regex != null && _regex.IsMatch(target),
            _ => true,
        };
    }

#if DEBUG
    public override string ToString()
    {
        return Description;
    }
#endif
}