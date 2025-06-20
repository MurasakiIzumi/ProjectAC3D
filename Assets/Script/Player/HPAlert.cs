using System.Collections.Generic;
using UnityEngine;

public class HPAlert : MonoBehaviour
{
    [Tooltip("警报音效播放器（请勾选 Loop，取消 PlayOnAwake）")]
    public AudioSource alertAudio;

    private HashSet<HPStatus> activeAlertParts = new HashSet<HPStatus>();

    public void RegisterCritical(HPStatus part)
    {
        if (activeAlertParts.Add(part))
        {
            if (!alertAudio.isPlaying)
                alertAudio.Play();
        }
    }

    public void UnregisterCritical(HPStatus part)
    {
        if (activeAlertParts.Remove(part))
        {
            if (activeAlertParts.Count == 0 && alertAudio.isPlaying)
                alertAudio.Stop();
        }
    }
}
