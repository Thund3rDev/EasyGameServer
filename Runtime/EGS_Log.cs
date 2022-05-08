using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Class EGS_Log, that manages the Easy Game Server log.
/// </summary>
public class EGS_Log : MonoBehaviour
{
    #region Variables
    [Header("General Variables")]
    [Tooltip("Singleton")]
    public static EGS_Log instance;

    [Tooltip("DateTime to print log times")]
    private DateTime localTime;


    [Header("UI")]
    [Tooltip("Text where the log writes")]
    [SerializeField]
    private TextMeshProUGUI text_log;

    [Tooltip("Scrollbar from the log")]
    [SerializeField]
    private Scrollbar scrolbar;

    [Tooltip("Bool that indicates if scrollbar must be updated")]
    private bool scrollbarMustUpdate;

    [Tooltip("Counter to update the scrollbar")]
    private int scrollbarUpdateCounter = 0;

    [Tooltip("Bool that indicates if autoscroll is enabled")]
    private bool autoscroll = true;


    [Header("IO")]
    [Tooltip("StreamWriter to write log")]
    private StreamWriter streamWriter;


    [Header("Concurrency")]
    [Tooltip("Lock object")]
    private object logLock = new object();
    #endregion

    #region Unity Methods
    /// <summary>
    /// Method Awake, executed on script load.
    /// </summary>
    private void Awake()
    {
        // Create the singleton instance.
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    /// <summary>
    /// Method Update, that is called once per frame.
    /// </summary>
    private void Update()
    {
        // If autoscroll is enabled, check if scrollbar must update.
        if (autoscroll)
        {
            if (scrollbarMustUpdate)
            {
                if (scrollbarUpdateCounter > 2)
                {
                    scrolbar.value = 0;
                    scrollbarMustUpdate = false;
                    scrollbarUpdateCounter = 0;
                }
                else
                    scrollbarUpdateCounter++;
            }
        }
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
        string dateString = GetStartDateAndTime();
        streamWriter = File.CreateText(Application.persistentDataPath +
        "/logs/log_" + dateString + ".txt");

        // Erase all text that could been in the log.
        text_log.text = "";

        // Log that server started.
        Log("Started <color=green>EasyGameServer</color> with version <color=orange>" + EGS_Config.version + "</color>.", EGS_Control.EGS_DebugLevel.Minimal);
    }

    /// <summary>
    /// Method Log, that adds the given string to the log.
    /// </summary>
    /// <param name="logString">String to add to the log</param>
    public void Log(string logString, EGS_Control.EGS_DebugLevel debugLevel)
    {
        // Format the string to log.
        string stringToLog = "[" + GetCurrentDateTime() + "] " + logString;

        // Base string for the log.
        string nonRichStringToLog = stringToLog;

        // Check if has colors.
        if (stringToLog.Contains("<color"))
            nonRichStringToLog = Regex.Replace(stringToLog, "<.*?>", string.Empty);

        // Log the string.
        lock (logLock)
        {
            // To the server console.
            if (EGS_Config.DEBUG_MODE_CONSOLE >= debugLevel)
            {
                EGS_Dispatcher.RunOnMainThread(() => { 
                    Debug.Log(stringToLog);
                    text_log.text += stringToLog + "\n";
                });
            }

            // To the file.
            if (EGS_Config.DEBUG_MODE_FILE >= debugLevel)
                streamWriter.WriteLine(nonRichStringToLog);
        }

        // Update the scrollbar.
        if (EGS_Config.DEBUG_MODE_CONSOLE >= debugLevel)
            scrollbarMustUpdate = true;
    }

    /// <summary>
    /// Method LogWarning, that formats the given string to yellow as a warning.
    /// </summary>
    /// <param name="logString">String to add to the log as a warning</param>
    public void LogWarning(string logString, EGS_Control.EGS_DebugLevel debugLevel)
    {
        // Format the string to log.
        string stringToLog = "[" + GetCurrentDateTime() + "] " + "<color=yellow>" + logString + "</color>";

        // Log the string.
        lock (logLock)
        {
            // To the server console.
            if (EGS_Config.DEBUG_MODE_CONSOLE >= debugLevel)
            {
                EGS_Dispatcher.RunOnMainThread(() =>
                {
                    Debug.LogWarning(logString);
                    text_log.text += stringToLog + "\n";
                });
            }

            // To the file.
            if (EGS_Config.DEBUG_MODE_FILE >= debugLevel)
                streamWriter.WriteLine(logString);
        }

        // Update the scrollbar.
        if (EGS_Config.DEBUG_MODE_CONSOLE >= debugLevel)
            scrollbarMustUpdate = true;
    }

    /// <summary>
    /// Method LogError, that formats the given string to yellow as an error.
    /// </summary>
    /// <param name="logString">String to add to the log as an error</param>
    public void LogError(string logString, EGS_Control.EGS_DebugLevel debugLevel)
    {
        // Format the string to log.
        string stringToLog = "[" + GetCurrentDateTime() + "] " + "<color=red>" + logString + "</color>";

        // Log the string.
        lock (logLock)
        {
            // To the server console.
            if (EGS_Config.DEBUG_MODE_CONSOLE >= debugLevel)
            {
                EGS_Dispatcher.RunOnMainThread(() =>
                {
                    Debug.LogError(logString);
                    text_log.text += stringToLog + "\n";
                });
            }

            // To the file.
            if (EGS_Config.DEBUG_MODE_FILE >= debugLevel)
                streamWriter.WriteLine(logString);
        }

        // Update the scrollbar.
        if (EGS_Config.DEBUG_MODE_CONSOLE >= debugLevel)
            scrollbarMustUpdate = true;
    }

    /// <summary>
    /// Method CloseLog, that closes the file where the log was writing.
    /// </summary>
    public void CloseLog()
    {
        Log("<color=green>Easy Game Server</color> closed.", EGS_Control.EGS_DebugLevel.Minimal);
        streamWriter.Close();
    }

    /// <summary>
    /// Method UpdateAutoscroll, to check if execute autoscroll on new log.
    /// </summary>
    /// <param name="autoscrollValue">New autoscroll value</param>
    public void UpdateAutoscroll(bool autoscrollValue)
    {
        this.autoscroll = autoscrollValue;
    }

    #region Private Methods
    /// <summary>
    /// Method GetStartDateAndTime, that returns the date and time when the server starts.
    /// </summary>
    /// <returns></returns>
    private string GetStartDateAndTime()
    {
        localTime = DateTime.Now;
        return localTime.ToString("yyyy-MM-dd_HH-mm-ss");
    }

    /// <summary>
    /// Method GetCurrentDateTime, that returns the moment date and time.
    /// </summary>
    /// <returns></returns>
    private string GetCurrentDateTime()
    {
        localTime = DateTime.Now;
        return localTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }
    #endregion
    #endregion
}
