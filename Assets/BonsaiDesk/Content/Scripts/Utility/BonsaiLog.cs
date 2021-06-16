using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BonsaiLog
{
    public static void Log(string msg, string title = "BonsaiLog", string color = "orange")
    {
        LogMessage(Debug.Log, msg, title, color);
    }

    public static void LogWarning(string msg, string title = "BonsaiLog", string color = "orange")
    {
        LogMessage(Debug.LogWarning, msg, title, color);
    }

    public static void LogError(string msg, string title = "BonsaiLog", string color = "orange")
    {
        LogMessage(Debug.LogError, msg, title, color);
    }
    
    private delegate void UnityLog(object msg);
    
    private static void LogMessage(UnityLog log, string msg, string title, string color)
    {
        log($"<color={color}>{title}: </color>: {msg}");
    }
}