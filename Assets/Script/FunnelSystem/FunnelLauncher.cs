using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FunnelLauncher : MonoBehaviour
{
    [Header("浮游炮分组")]
    [Tooltip("左侧浮游炮列表")]
    public List<FunnelControl> leftFunnels = new List<FunnelControl>();

    [Tooltip("右侧浮游炮列表")]
    public List<FunnelControl> rightFunnels = new List<FunnelControl>();

    [Header("发射设置")]
    [Tooltip("发射操作的按键（短按：单边；长按：双边）")]
    public KeyCode launchKey = KeyCode.E;

    [Tooltip("判断为长按的持续时间（秒）")]
    public float longPressThreshold = 0.4f;

    [Tooltip("每个浮游炮之间的发射间隔（秒）")]
    public float intervalBetweenLaunches = 0.2f;

    [Tooltip("浮游炮回收后需要的充能时间（秒）")]
    public float rechargeDuration = 3f;

    // 内部状态
    private float pressTimer = 0f;

    private bool leftLaunched = false;
    private bool rightLaunched = false;

    private Dictionary<FunnelControl, float> funnelCooldownTimers = new();

    void Start()
    {
        BindDockEvents(leftFunnels);
        BindDockEvents(rightFunnels);
    }

    void Update()
    {
        HandleInput();
        UpdateCooldowns();
    }

    // 绑定浮游炮回收通知
    void BindDockEvents(List<FunnelControl> funnels)
    {
        foreach (var funnel in funnels)
        {
            if (funnel != null)
                funnel.onDocked += OnFunnelDocked;
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(launchKey))
        {
            pressTimer = 0f;
        }

        if (Input.GetKey(launchKey))
        {
            pressTimer += Time.deltaTime;
        }

        if (Input.GetKeyUp(launchKey))
        {
            bool isLongPress = pressTimer >= longPressThreshold;
            pressTimer = 0f;

            TryLaunch(isLongPress);
        }
    }

    void TryLaunch(bool isLongPress)
    {
        bool leftReady = CanLaunchGroup(leftFunnels) && !leftLaunched;
        bool rightReady = CanLaunchGroup(rightFunnels) && !rightLaunched;

        if (isLongPress && leftReady && rightReady)
        {
            StartCoroutine(LaunchGroup(leftFunnels));
            StartCoroutine(LaunchGroup(rightFunnels));
            leftLaunched = true;
            rightLaunched = true;
        }
        else if (leftReady)
        {
            StartCoroutine(LaunchGroup(leftFunnels));
            leftLaunched = true;
        }
        else if (rightReady)
        {
            StartCoroutine(LaunchGroup(rightFunnels));
            rightLaunched = true;
        }
    }

    IEnumerator LaunchGroup(List<FunnelControl> funnels)
    {
        foreach (var funnel in funnels)
        {
            if (funnel != null && !funnelCooldownTimers.ContainsKey(funnel) && funnel.currentState == FunnelControl.FunnelState.Docked)
            {
                funnelCooldownTimers[funnel] = rechargeDuration;
                funnel.ActivateFunnel();
                yield return new WaitForSeconds(intervalBetweenLaunches);
            }
        }
    }

    bool CanLaunchGroup(List<FunnelControl> funnels)
    {
        foreach (var funnel in funnels)
        {
            if (funnel != null && !funnelCooldownTimers.ContainsKey(funnel) && funnel.currentState == FunnelControl.FunnelState.Docked)
                return true;
        }
        return false;
    }

    void OnFunnelDocked(FunnelControl funnel)
    {
        if (!funnelCooldownTimers.ContainsKey(funnel))
        {
            funnelCooldownTimers[funnel] = rechargeDuration;
        }
    }

    void UpdateCooldowns()
    {
        List<FunnelControl> keys = new List<FunnelControl>(funnelCooldownTimers.Keys);
        foreach (var funnel in keys)
        {
            funnelCooldownTimers[funnel] -= Time.deltaTime;
            if (funnelCooldownTimers[funnel] <= 0f)
            {
                funnelCooldownTimers.Remove(funnel);
            }
        }

        if (IsLeftGroupReady() && IsRightGroupReady())
        {
            ResetLauncher();
        }
    }

    // 提供给 UI 查询接口
    public bool IsLeftGroupReady() => AreAllFunnelsReady(leftFunnels);
    public bool IsRightGroupReady() => AreAllFunnelsReady(rightFunnels);

    bool AreAllFunnelsReady(List<FunnelControl> group)
    {
        foreach (var f in group)
        {
            if (f == null || funnelCooldownTimers.ContainsKey(f) || f.currentState != FunnelControl.FunnelState.Docked)
                return false;
        }
        return true;
    }

    public float GetLeftGroupCooldownProgress() => GetGroupCooldownProgress(leftFunnels);
    public float GetRightGroupCooldownProgress() => GetGroupCooldownProgress(rightFunnels);

    float GetGroupCooldownProgress(List<FunnelControl> group)
    {
        float latestRemaining = 0f;

        foreach (var f in group)
        {
            if (funnelCooldownTimers.TryGetValue(f, out float remaining))
            {
                if (remaining > latestRemaining)
                    latestRemaining = remaining;
            }
        }

        return 1f - Mathf.Clamp01(latestRemaining / rechargeDuration);
    }

    public void ResetLauncher()
    {
        leftLaunched = false;
        rightLaunched = false;
    }
}