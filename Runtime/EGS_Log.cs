using System;
using System.IO;
using System.Text.RegularExpressions;
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
        // If logs directory doesn't exist, create it.
        if (!Directory.Exists(Application.persistentDataPath + "/logs"))
            Directory.CreateDirectory(Application.persistentDataPath + "/logs");

        // Create the log file.
        string dateString = GetActualDate();
        dateString = dateString.Replace('/', '-');
        dateString = dateString.Replace(' ', '_');
        dateString = dateString.Replace(':', '-');
        streamWriter = File.CreateText(Application.persistentDataPath +
        "/logs/log_" + dateString + ".txt");

        // Format the server start string.
        string stringToLog = "[" + GetActualDate() + "] " + "Started <color=green>EasyGameServer</color> with version <color=orange>" + version + "</color>.";

        // Base string for the log.
        string nonRichStringToLog = stringToLog;

        // Check if has colors.
        if (stringToLog.Contains("<color"))
            nonRichStringToLog = Regex.Replace(stringToLog, "<.*?>", string.Empty);

        // Log the server start string.
        lock (logLock)
        {
            Debug.Log(stringToLog);
            text_log.text = stringToLog + "\n";
            streamWriter.WriteLine(nonRichStringToLog);
        }
    }

    /// <summary>
    /// Method Log, that adds the given string to the log.
    /// </summary>
    /// <param name="logString">String to add to the log</param>
    public void Log(string logString)
    {
        // Format the string to log.
        string stringToLog = "[" + GetActualDate() + "] " + logString;

        // Base string for the log.
        string nonRichStringToLog = stringToLog;

        // Check if has colors.
        if (stringToLog.Contains("<color"))
            nonRichStringToLog = Regex.Replace(stringToLog, "<.*?>", string.Empty);

        // Log the string.
        lock (logLock)
        {
            Debug.Log(stringToLog);
            text_log.text += stringToLog + "\n";
            streamWriter.WriteLine(nonRichStringToLog);
        }
    }

    /// <summary>
    /// Method LogWarning, that formats the given string to yellow as a warning.
    /// </summary>
    /// <param name="logString">String to add to the log as a warning</param>
    public void LogWarning(string logString)
    {
        Log("<color=yellow>" + logString + "</color>");
    }

    /// <summary>
    /// Method LogError, that formats the given string to yellow as an error.
    /// </summary>
    /// <param name="logString">String to add to the log as an error</param>
    public void LogError(string logString)
    {
        Log("<color=red>" + logString + "</color>");
    }

    /// <summary>
    /// Method CloseLog, that closes the file where the log was writing.
    /// </summary>
    public void CloseLog()
    {
        Log("<color=green>Easy Game Server</color> closed.");
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
