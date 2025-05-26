using UnityEngine;

public class CameraRigContol : MonoBehaviour
{
    [Header("旋转灵敏度")]
    public float sensitivity = 2f; // 鼠标控制灵敏度

    [Header("水平旋转轴体，例如CameraRig")]
    public Transform horizontalPivot;

    [Header("垂直旋转轴体，例如实际Camera对象")]
    public Transform verticalPivot;

    [Header("是否启用视角控制")]
    public bool enableLook = true;

    private float pitch = 0f; // 当前仰角（垂直方向）

    void Update()
    {
        if (!enableLook || horizontalPivot == null || verticalPivot == null) return;

        // 鼠标输入
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        // 旋转角度更新
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -45f, 45f); // 限制仰角（避免翻转）

        // 应用旋转
        horizontalPivot.Rotate(Vector3.up * mouseX);
        verticalPivot.localEulerAngles = new Vector3(pitch, 0, 0);
    }
}
