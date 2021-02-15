using System;
using System.IO;
using UnityEngine;
using TMPro;

/// <summary>
/// Class EGS_Log, that manages the EGS log.
/// </summary>
public class EGS_Log : MonoBehaviour
{
    #region Variables
    [Tooltip("Text where the log writes")]
    [SerializeField]
    private TextMeshProUGUI text_log;

    [Tooltip("DateTime to print log times")]
    private DateTime localTime;

    [Header("IO")]
    [Tooltip("StreamWriter to write log")]
    private StreamWriter streamWriter;

    #region Concurrency
    [Header("Concurrency")]
    [Tooltip("Lock object")]
    private object logLock = new object();
    #endregion
    #endregion

    #region Class Methods
    /// <summary>
    /// Method StartLog, that restarts the log.
    /// </summary>
    /// <param name="version">Server version</param>
    public void StartLog(string version)
    {
        // If logs directory doesn't exist, creates it.
        if (!Directory.Exists(Application.persistentDataPath + "/logs"))
            Directory.CreateDirectory(Application.persistentDataPath + "/logs");

        // Creates the log file.
        string dateString = GetActualDate();
        dateString = dateString.Replace('/', '-');
        dateString = dateString.Replace(' ', '_');
        dateString = dateString.Replace(':', '-');
        streamWriter = File.CreateText(Application.persistentDataPath +
        "/logs/log_" + dateString + ".txt");

        // Formats and logs the server start string.
        string stringToLog = "[" + GetActualDate() + "] " + "Started EasyGameServer with version " + version + ".";
        lock (logLock)
        {
            Debug.Log(stringToLog);
            text_log.text = stringToLog + "\n";
            streamWriter.WriteLine(stringToLog);
        }
    }

    /// <summary>
    /// Method Log, that adds the given string to the log.
    /// </summary>
    /// <param name="logString">String to add to the log</param>
    public void Log(string logString)
    {
        // Formats the string to log and logs it.
        string stringToLog = "[" + GetActualDate() + "] " + logString;
        lock (logLock)
        {
            Debug.Log(stringToLog);
            text_log.text += stringToLog + "\n";
            streamWriter.WriteLine(stringToLog);
        }
    }

    /// <summary>
    /// Method CloseLog, that closes the file where the log was writing.
    /// </summary>
    public void CloseLog()
    {
        Log("Easy Game Server closed");
        streamWriter.Close();
    }

    #region Private Methods
    /// <summary>
    /// Method GetActualDate, that returns the moment hour.
    /// </summary>
    /// <returns></returns>
    private string GetActualDate()
    {
        localTime = DateTime.Now;
        return localTime.ToString();
    }
    #endregion
    #endregion
}
