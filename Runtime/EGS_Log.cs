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

    #region Unity Methods
    private void Update()
    {
        text_log.UpdateMeshPadding();
    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method StartLog, that start the log.
    /// </summary>
    public void StartLog()
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

        // Erase all text that could been in the log.
        text_log.text = "";
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
            try
            {
                Debug.Log(stringToLog);
            }
            catch (UnityException)
            {
            }

            try
            {
                text_log.text += stringToLog + "\n";
            }
            catch (UnityException)
            {
            }
            
            streamWriter.WriteLine(nonRichStringToLog);
        }
    }

    /// <summary>
    /// Method LogWarning, that formats the given string to yellow as a warning.
    /// </summary>
    /// <param name="logString">String to add to the log as a warning</param>
    public void LogWarning(string logString)
    {
        // Format the string to log.
        string stringToLog = "[" + GetActualDate() + "] " + "<color=yellow>" + logString + "</color>";

        // Log the string.
        lock (logLock)
        {
            try
            {
                Debug.LogWarning(logString);
            }
            catch (UnityException)
            {
            }

            try
            {
                text_log.text += stringToLog + "\n";
            }
            catch (UnityException)
            {
            }

            streamWriter.WriteLine(logString);
        }
    }

    /// <summary>
    /// Method LogError, that formats the given string to yellow as an error.
    /// </summary>
    /// <param name="logString">String to add to the log as an error</param>
    public void LogError(string logString)
    {
        // Format the string to log.
        string stringToLog = "[" + GetActualDate() + "] " + "<color=red>" + logString + "</color>";

        // Log the string.
        lock (logLock)
        {
            try
            {
                Debug.LogError(logString);
            }
            catch (UnityException)
            {
            }

            try
            {
                text_log.text += stringToLog + "\n";
            }
            catch (UnityException)
            {
            }

            streamWriter.WriteLine(logString);
        }
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
