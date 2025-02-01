using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

using Sentinel.Highlighters.Interfaces;
using Sentinel.Interfaces;

using Sentinel.WpfExtras;

namespace Sentinel.Highlighters;

[DataContract]
public class Highlighter : ViewModelBase, IHighlighter
{
    private bool _enabled = true;

    private LogEntryFields _field;

    private string _name;

    private IHighlighterStyle _style;

    private string _pattern;

    private Regex _regex;

    private Func<string, string, bool> _matcher;

    public Highlighter()
    {
        Pattern = string.Empty;
        Enabled = false;

        PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(Field) || e.PropertyName == nameof(Mode) || e.PropertyName == nameof(Pattern))
            {
                if (Mode == MatchMode.RegularExpression && Pattern != null)
                {
                    _regex = new Regex(Pattern);
                    SetupMatcher();
                }

                OnPropertyChanged(nameof(Description));
            }
        };

        SetupMatcher();
    }

    protected Highlighter(string name, bool enabled, LogEntryFields field, MatchMode mode, string pattern, IHighlighterStyle style)
    {
        Name = name;
        Enabled = enabled;
        Field = field;
        Mode = mode;
        Pattern = pattern;
        Style = style;
        _regex = new Regex(pattern);

        PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(Field) || e.PropertyName == nameof(Mode) ||
                e.PropertyName == nameof(Pattern))
            {
                if (Mode == MatchMode.RegularExpression && Pattern != null)
                {
                    _regex = new Regex(Pattern);
                    SetupMatcher();
                }

                OnPropertyChanged(nameof(Description));
            }
        };
        SetupMatcher();
    }

    private void SetupMatcher()
    {
        if (_mode == MatchMode.Exact)
            _matcher = (target, pattern) => string.Equals(target, pattern, StringComparison.OrdinalIgnoreCase);
        if (_mode == MatchMode.Contains)
            _matcher = (target, pattern) =>  target.Contains(Pattern,  StringComparison.OrdinalIgnoreCase);
        if (_mode == MatchMode.RegularExpression)
            if (_regex != null)
                _matcher = (target, pattern) => _regex.IsMatch(target);
            else
                _matcher = (target, pattern) => false;
    }

    public string Name
    {
        get => _name;

        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public bool Enabled
    {
        get
        {
            return _enabled;
        }

        set
        {
            if (_enabled != value)
            {
                _enabled = value;
                OnPropertyChanged(nameof(Enabled));
            }
        }
    }

    public LogEntryFields Field
    {
        get
        {
            return _field;
        }

        set
        {
            _field = value;
            OnPropertyChanged(nameof(Field));
        }
    }

    public string HighlighterType => "Basic Highlighter";

    public MatchMode Mode
    {
        get => _mode;

        set
        {
            if (_mode != value)
            {
                _mode = value;
                OnPropertyChanged(nameof(Mode));
                SetupMatcher();
            }
        }
    }
    private MatchMode _mode;

    public string Description
    {
        get
        {
            string modeDescription = "Exact";
            switch (Mode)
            {
                case MatchMode.RegularExpression:
                    modeDescription = "RegEx";
                    break;
                case MatchMode.Contains:
                    modeDescription = "Case sensitive";
                    break;
                case MatchMode.CaseInsensitive:
                    modeDescription = "Case insensitive";
                    break;
            }

            return $"{modeDescription} match of {Pattern} in the {Field} field";
        }
    }

    public string Pattern
    {
        get => _pattern;

        set
        {
            if (_pattern != value)
            {
                _pattern = value;
                OnPropertyChanged(nameof(Pattern));
            }
        }
    }

    public IHighlighterStyle Style
    {
        get => _style;

        set
        {
            if (_style != value)
            {
                _style = value;
                OnPropertyChanged(nameof(Style));
            }
        }
    }

    public bool IsMatch(ILogEntry logEntry)
    {
        Debug.Assert(logEntry != null, "logEntry can not be null.");

        if (logEntry == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(Pattern))
        {
            return false;
        }

        var target = GetTarget(logEntry);

        return _matcher(target, _pattern);
    }

    private string GetTarget(ILogEntry logEntry)
    {
        string target;

        switch (Field)
        {
            case LogEntryFields.Type:
                target = logEntry.Type;
                break;
            case LogEntryFields.System:
                target = logEntry.System;
                break;
            case LogEntryFields.Thread:
                target = logEntry.Thread;
                break;
            case LogEntryFields.Source:
                target = logEntry.Source;
                break;
            case LogEntryFields.Description:
                target = logEntry.Description;
                break;
            ////case LogEntryField.Classification:
            ////case LogEntryField.None:
            ////case LogEntryField.Host:
            default:
                target = string.Empty;
                break;
        }

        return target;
    }
}