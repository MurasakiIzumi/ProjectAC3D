using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SS_GatlingTracerFX : MonoBehaviour
{
    [Tooltip("曳光最大距离")]
    public float tracerLength = 50f;

    [Tooltip("曳光存活时间")]
    public float tracerDuration = 0.06f;

    [Tooltip("曳光线方向偏移角度范围（°）")]
    public float tracerJitter = 1f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
    }

    public void PlayTracer(Vector3 direction, Vector3 origin)
    {
        direction = ApplyJitter(direction);

        Vector3 endPoint = origin + direction * tracerLength;

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);
        lineRenderer.enabled = true;

        CancelInvoke(nameof(HideTracer));
        Invoke(nameof(HideTracer), tracerDuration);
    }

    private Vector3 ApplyJitter(Vector3 dir)
    {
        Quaternion jitter = Quaternion.Euler(
            Random.Range(-tracerJitter, tracerJitter),
            Random.Range(-tracerJitter, tracerJitter),
            0f
        );
        return jitter * dir.normalized;
    }

    private void HideTracer()
    {
        lineRenderer.enabled = false;
    }
}