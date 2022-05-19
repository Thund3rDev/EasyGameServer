using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class MainThreadDispatcher, that will execute code on the main thread.
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    #region Variables
    [Header("Dispatcher")]
    [Tooltip("Static instance of the dispatcher")]
    private static MainThreadDispatcher instance;

    [Tooltip("Queue that will store events to execute")]
    private static readonly Queue<Action> eventQueue = new Queue<Action>();
    #endregion

    #region Unity Methods
    /// <summary>
    /// Method Awake, called on script load.
    /// </summary>
    void Awake()
    {
        // Instantiate the singleton.
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(instance);
        }
        else
        {
            Destroy(this);
        }
    }

    /// <summary>
    /// Method Update, that is called once per frame.
    /// </summary>
    private void Update()
    {
        // If there are events left, invoke them.
        lock (eventQueue)
        {
            while (eventQueue.Count > 0)
            {
                eventQueue.Dequeue().Invoke();
            }
        }
    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method RunOnMainThread, to execute code from threads on the main application thread.
    /// </summary>
    /// <param name="action">Action to execute</param>
    public static void RunOnMainThread(Action action)
    {
        lock (eventQueue)
        {
            eventQueue.Enqueue(action);
        }
    }
    #endregion
}