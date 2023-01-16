using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;


public class AudioManager : MonoBehaviour
{
    [SerializeField] AudioSource source;

    [SerializeField] List<AudioClipInfo> info;

    Dictionary<AudioType, List<AudioClip>> soundEffects = new Dictionary<AudioType, List<AudioClip>>();

    Random rnd = new Random();

    void Awake()
    {
        return;
        soundEffects = info.ToDictionary(x => x.Type, x => x.Clips);
    }

    public void PlaySFX(AudioType type)
    {
        return;
        var effects = soundEffects[type];
        var effect = effects[rnd.Next(0, effects.Count)];

        source.clip = effect;
        source.Play();
    }
}

[Serializable]
public class AudioClipInfo
{
    [SerializeField] string name;
    public AudioType Type;
    public List<AudioClip> Clips;
}

public enum AudioType
{
    none = 0,

    OnSelect = 1,
    OnAirstrike = 2,
}
