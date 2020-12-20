﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LogToText : MonoBehaviour
{
    private int numLogsToStore = 5;

    private static LogToText _instance;

    public static LogToText Instance
    {
        get { return _instance; }
    }

    struct Log
    {
        public string condition;
        public string stackTrace;
        public LogType type;

        public Log(string condition, string stackTrace, LogType type)
        {
            this.condition = condition;
            this.stackTrace = stackTrace;
            this.type = type;
        }
    }

    private Queue<Log> logs = new Queue<Log>();
    private string logPath;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            _instance = this;
        }

        if (string.IsNullOrEmpty(logPath))
        {
            deleteOldFiles();

            System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            int cur_time = (int) (System.DateTime.UtcNow - epochStart).TotalSeconds;

            string fileName = cur_time + ".log";

            logPath = Path.Combine(Application.persistentDataPath, fileName);
            StreamWriter sw = File.CreateText(logPath);
            sw.Close();
            print("Saving log to: " + logPath);
            
            OVRManager.HMDUnmounted += OVRManagerOnHMDUnmounted;
        }
    }

    void OnEnable()
    {
        Application.logMessageReceived += LogCallBack;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= LogCallBack;
    }

    void LogCallBack(string condition, string stackTrace, LogType type)
    {
        Log log;
        log.condition = condition;
        log.stackTrace = stackTrace;
        log.type = type;
        logs.Enqueue(log);
    }

    //deletes old logs so there is (numLogsToStore - 1) logs remaining
    //deletes old logs first based on unix timestamp name
    void deleteOldFiles()
    {
        var directoryInfo = new DirectoryInfo(Application.persistentDataPath);
        var filePaths = directoryInfo.GetFiles();
        SortedList<int, string> sortedLogs = new SortedList<int, string>();
        foreach (var filePath in filePaths)
        {
            string fileName = Path.GetFileName(filePath.ToString());
            int periodIndex = fileName.IndexOf('.');
            string name = fileName.Substring(0, periodIndex);
            string ext = fileName.Substring(periodIndex + 1);
            if (ext.CompareTo("log") == 0)
            {
                if (int.TryParse(name, out int nameNumber))
                {
                    sortedLogs.Add(nameNumber, filePath.ToString());
                }
            }
        }

        int numToDelete = sortedLogs.Count - (numLogsToStore - 1);
        for (int i = 0; i < numToDelete; i++)
        {
            File.Delete(sortedLogs.Values[i]);
        }
    }
    
    void appendLogQueueToFile()
    {
        if (logs.Count == 0)
            return;
        print("Updating log at: " + logPath);
        using (StreamWriter sw = File.AppendText(logPath))
        {
            while (logs.Count > 0)
            {
                Log log = logs.Dequeue();
                sw.WriteLine(log.type.ToString());
                sw.WriteLine(log.condition);
                sw.WriteLine(log.stackTrace);
            }
        }
    }

    private void OnApplicationQuit()
    {
        appendLogQueueToFile();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            appendLogQueueToFile();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            appendLogQueueToFile();
    }
    
    private void OVRManagerOnHMDUnmounted()
    {
        appendLogQueueToFile();
    }
}