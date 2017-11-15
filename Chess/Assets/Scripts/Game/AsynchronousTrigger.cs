using System;
using UnityEngine;
using UnityEngine.Events;

public class AsynchronousTrigger : MonoBehaviour
{
    [Serializable]
    public class Event : UnityEvent<string>
    {

    }

    public Event onPlay;
    public UnityEvent onStop;

    private string __name;

    public void Play(string name)
    {
        if (__name != name)
        {
            __name = name;

            CancelInvoke("__Stop");
        }

        if (onPlay != null)
            onPlay.Invoke(name);
    }

    public void Stop(string name, float time)
    {
        if (__name != name)
            return;

        Invoke("__Stop", time);
    }

    private void __Stop()
    {
        if (onStop != null)
            onStop.Invoke();
    }
}
