using System.Diagnostics;

namespace ICE.Utilities;

internal static class IceLogging
{
    private static string _lastVerboseMessage, _lastDebugMessage, _lastInfoMessage = string.Empty;

    private static string GetCallerPrefix()
    {
        var stackFrame = new StackFrame(3);
        var method = stackFrame.GetMethod();
        var className = method?.DeclaringType?.Name;
        var methodName = method?.Name;

        if (className != null && methodName != null)
        {
            return $"[{className}.{methodName}]";
        }
        else if (className != null)
        {
            return $"[{className}]";
        }
        else if (methodName != null)
        {
            return $"[{methodName}]";
        }
        return string.Empty;
    }

    private static string FormatMessage(string message, string prefix = null)
    {
        var callerPrefix = prefix ?? GetCallerPrefix();
        return $"{callerPrefix} {message}";
    }

    public static void Verbose(string message, string prefix = null)
    {
        var formattedMessage = FormatMessage(message, prefix);
        if (formattedMessage == _lastVerboseMessage) return;
        PluginLog.Verbose(formattedMessage);
        _lastVerboseMessage = formattedMessage;
    }

    public static void Debug(string message, string prefix = null)
    {
        var formattedMessage = FormatMessage(message, prefix);
        if (formattedMessage == _lastDebugMessage) return;
        PluginLog.Debug(formattedMessage);
        _lastDebugMessage = formattedMessage;
    }

    public static void Info(string message, string prefix = null)
    {
        var formattedMessage = FormatMessage(message, prefix);
        if (formattedMessage == _lastInfoMessage) return;
        PluginLog.Information(formattedMessage);
        _lastInfoMessage = formattedMessage;
    }

    public static void Warning(string message, string prefix = null)
    {
        var formattedMessage = FormatMessage(message, prefix);
        PluginLog.Warning(formattedMessage);
    }

    public static void Error(string message, string prefix = null)
    {
        var formattedMessage = FormatMessage(message, prefix);
        PluginLog.Error(formattedMessage);
    }

    public static void Fatal(string message, string prefix = null)
    {
        var formattedMessage = FormatMessage(message, prefix);
        PluginLog.Fatal(formattedMessage);
    }
}
