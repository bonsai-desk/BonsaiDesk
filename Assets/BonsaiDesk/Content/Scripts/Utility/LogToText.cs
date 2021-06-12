using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class LogToText : MonoBehaviour
{
    private const int NumLogsToStore = 5;
    private const int MaxCharsPerFile = 25000000;

    private static LogToText _instance;
    public static LogToText Instance => _instance;

    private string _logPath;
    private StreamWriter _streamWriter;
    private int _numCharsWritten;
    private int _flushedFrame;

    private bool _showedMessageStack;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        if (!string.IsNullOrEmpty(_logPath))
        {
            return;
        }

        try
        {
            DeleteOldFiles();

            var unixTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            var fileName = unixTimestamp + ".log";

            _logPath = Path.Combine(Application.persistentDataPath, fileName);
            var fs = File.Create(_logPath);
            fs.Close();

            OVRManager.HMDUnmounted += OVRManagerOnHMDUnmounted;

            _streamWriter = new StreamWriter(_logPath, true);
        }
        catch (Exception e)
        {
            Debug.LogError("LogToText Awake init error: " + e);
        }
    }

    private void Update()
    {
        _streamWriter?.Flush(); //flush stream once per frame to prevent buffer from growing too large
    }

    private void OnEnable()
    {
        Application.logMessageReceived += LogCallBack;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= LogCallBack;
    }

    private void LogCallBack(string condition, string stackTrace, LogType type)
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        if (!_showedMessageStack && type == LogType.Error && MessageStack.Singleton)
        {
            _showedMessageStack = true;
            MessageStack.Singleton.AddMessage(condition, MessageStack.MessageType.Bad, 10f);
        }
#endif

        if (_numCharsWritten >= MaxCharsPerFile)
        {
            return;
        }

        try
        {
            if (_streamWriter == null)
            {
                _streamWriter = new StreamWriter(_logPath, true);
            }

            string typeString = type.ToString();
            const string atTimeString = " at time: ";
            string timeString = Time.realtimeSinceStartup.ToString(CultureInfo.InvariantCulture);

            _streamWriter.Write(typeString);
            _numCharsWritten += typeString.Length;
            _streamWriter.Write(atTimeString);
            _numCharsWritten += atTimeString.Length;
            _streamWriter.WriteLine(timeString);
            _numCharsWritten += timeString.Length + 1;
            _streamWriter.WriteLine(condition);
            _numCharsWritten += condition.Length + 1;
            _streamWriter.WriteLine(stackTrace);
            _numCharsWritten += stackTrace.Length + 1;

            if (_numCharsWritten >= MaxCharsPerFile)
            {
                _streamWriter.WriteLine("\n\n\n_numCharsWritten >= MaxCharsPerFile. No more logs will be written.");
            }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _streamWriter.Flush(); //in development build, flush after every log to ensure logs are always up to date in case of a crash
#endif
        }
        catch
        {
            //fail silently to prevent any recursive logs
        }
    }

    //deletes old logs so there is (numLogsToStore - 1) logs remaining
    //deletes old logs first based on unix timestamp name
    private void DeleteOldFiles()
    {
        var directoryInfo = new DirectoryInfo(Application.persistentDataPath);
        var filePaths = directoryInfo.GetFiles();
        SortedList<int, string> sortedLogs = new SortedList<int, string>();
        foreach (var filePath in filePaths)
        {
            string fileName = Path.GetFileName(filePath.ToString());
            int periodIndex = fileName.IndexOf('.');
            if (periodIndex > 0)
            {
                string justName = fileName.Substring(0, periodIndex);
                string ext = fileName.Substring(periodIndex + 1);
                if (ext == "log")
                {
                    if (int.TryParse(justName, out int nameNumber))
                    {
                        if (!sortedLogs.ContainsKey(nameNumber))
                        {
                            sortedLogs.Add(nameNumber, filePath.ToString());
                        }
                        else
                        {
                            Debug.LogError("Sorted logs already contains key: " + nameNumber);
                        }
                    }
                }
            }
        }

        int numToDelete = sortedLogs.Count - (NumLogsToStore - 1);
        for (int i = 0; i < numToDelete; i++)
        {
            File.Delete(sortedLogs.Values[i]);
        }
    }

    private void OnApplicationQuit()
    {
        CleanupStreamWriter();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            CleanupStreamWriter();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            CleanupStreamWriter();
        }
    }

    private void OVRManagerOnHMDUnmounted()
    {
        CleanupStreamWriter();
    }

    private void CleanupStreamWriter()
    {
        if (_streamWriter == null)
        {
            return;
        }

        _streamWriter.Close();
        _streamWriter = null;
    }
}