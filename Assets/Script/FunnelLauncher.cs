using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FunnelLauncher : MonoBehaviour
{
    [Header("浮游炮列表")]
    public List<FunnelControl> funnels = new List<FunnelControl>();

    [Header("发射设置")]
    public KeyCode launchKey = KeyCode.E;       // 发射触发键
    public float intervalBetweenLaunches = 0.3f; // 每个浮游炮之间的延迟

    private bool isLaunching = false;

    void Update()
    {
        if (Input.GetKeyDown(launchKey) && !isLaunching)
        {
            StartCoroutine(SequentialLaunch());
        }
    }

    IEnumerator SequentialLaunch()
    {
        isLaunching = true;

        foreach (var funnel in funnels)
        {
            if (funnel != null && funnel.currentState == FunnelControl.FunnelState.Docked)
            {
                funnel.ActivateFunnel();
                yield return new WaitForSeconds(intervalBetweenLaunches);
            }
        }

        isLaunching = false;
    }
}
