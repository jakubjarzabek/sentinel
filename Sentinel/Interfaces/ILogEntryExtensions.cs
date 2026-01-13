namespace Sentinel.Interfaces;

public static class ILogEntryExtensions
{
    public static string GetField(this ILogEntry logEntry, LogEntryFields field)
    {
        string target;
        
        switch (field)
        {
            case LogEntryFields.None:
                target = string.Empty;
                break;
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
            case LogEntryFields.Classification:
            case LogEntryFields.Host:
            default:
                target = string.Empty;
                break;
        }

        return target;
    }
}