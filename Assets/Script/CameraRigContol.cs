using UnityEngine;

public class CameraRigContol : MonoBehaviour
{
    [Header("旋转灵敏度")]
    [Tooltip("鼠标控制灵敏度")]
    public float sensitivity = 2f;

    [Header("水平旋转轴体，例如CameraRig")]
    [Tooltip("控制左右视角的旋转轴体")]
    public Transform horizontalPivot;

    [Header("垂直旋转轴体，例如PitchRig")]
    [Tooltip("控制上下视角的旋转轴体")]
    public Transform verticalPivot;

    [Header("垂直旋转限制")]
    [Tooltip("允许的最小仰角（向下看）")]
    public float minPitch = -45f;
    [Tooltip("允许的最大仰角（向上看）")]
    public float maxPitch = 45f;

    [Header("是否启用视角控制")]
    [Tooltip("启用后允许鼠标控制视角旋转")]
    public bool enableLook = true;

    private float pitch = 0f; // 当前仰角（垂直方向）

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // 锁定鼠标到屏幕中心
        Cursor.visible = false;                   // 隐藏鼠标光标
    }

    void Update()
    {
        if (!enableLook || horizontalPivot == null || verticalPivot == null) return;

        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch); // 使用可配置范围

        horizontalPivot.Rotate(Vector3.up * mouseX);
        verticalPivot.localEulerAngles = new Vector3(pitch, 0, 0);
    }
}