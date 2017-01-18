using System;
using UnityEngine;

public class Dice : MonoBehaviour
{
    [Serializable]
    public struct Rotation
    {
        public int x;
        public int y;
        public int z;

        public Rotation(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3 toEluarAngles()
        {
            return new Vector3(x * 90.0f, y * 90.0f, z * 90.0f);
        }
    }

    [Serializable]
    public struct Trigger
    {
        public string name;
        public Rotation rotation;
    }

    public Animator animator;
    public Transform instance;
    public Rotation[] rotations;
    public Trigger[] triggers;

    public void Random()
    {
        if (instance != null)
            instance.localEulerAngles = new Rotation(UnityEngine.Random.Range(0, 3), UnityEngine.Random.Range(0, 3), UnityEngine.Random.Range(0, 3)).toEluarAngles();
    }

    public void Play(int point)
    {
        if (point < 0)
            return;

        int numTriggers = triggers == null ? 0 : triggers.Length;
        if (numTriggers < 1)
            return;

        int numRotations = rotations == null ? 0 : rotations.Length;
        if (point >= numRotations)
            return;

        Trigger trigger = triggers[UnityEngine.Random.Range(0, numTriggers - 1)];

        if(instance != null)
            instance.localEulerAngles = rotations[point].toEluarAngles() + trigger.rotation.toEluarAngles();

        if (animator != null)
            animator.SetTrigger(trigger.name);
    }
}
