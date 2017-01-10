using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Keypad : MonoBehaviour
{
    [Serializable]
    public class CompleteEvent : UnityEvent<string>
    {

    }

    public int step = 1;
    public string empty = "_";
    public Text text;
    public CompleteEvent onComplete;
    private int __count;

    public void Add(int number)
    {
        string text = this.text == null ? null : this.text.text;
        if (string.IsNullOrEmpty(text))
            return;

        int index = __count * step, count = text.Length;
        if (index >= count)
            return;

        text = text.Remove(index, 1);
        text = text.Insert(index, number.ToString());
        this.text.text = text;

        ++__count;
        if(__count * step >= count)
        {
            if (onComplete != null)
                onComplete.Invoke(text.Replace(empty, string.Empty));
        }
    }

    public void Remove()
    {
        if (__count < 1)
            return;

        string text = this.text == null ? null : this.text.text;
        if (string.IsNullOrEmpty(text))
            return;

        --__count;

        int index = __count * step;
        text = text.Remove(index, 1);
        this.text.text = text.Insert(index, empty);
    }

    public void Reset()
    {
        string text = this.text == null ? null : this.text.text;
        if (string.IsNullOrEmpty(text))
            return;
        
        int index;
        for (int i = 0; i < __count; ++i)
        {
            index = i * step;
            text = text.Remove(index, 1);
            text = text.Insert(index, empty);
        }
        
        this.text.text = text;

        __count = 0;
    }
}
