using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class EGS_Dispatcher, that will execute code on the main thread.
/// </summary>
public class EGS_Dispatcher : MonoBehaviour
{
    #region Variables
    // Static instance of the dispatcher.
    private static EGS_Dispatcher _instance;

    // Queue that will store events to execute.
    private static readonly Queue<Action> eventQueue = new Queue<Action>();
    #endregion

    #region Unity Methods
    /// <summary>
    /// Method Awake, called on script load.
    /// </summary>
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(_instance);
        }
        else
        {
            Destroy(this);
        }
    }

    // Update is called once per frame
    void Update()
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
    public static void RunOnMainThread(Action action)
    {
        lock (eventQueue)
        {
            eventQueue.Enqueue(action);
        }
    }
    #endregion
}
