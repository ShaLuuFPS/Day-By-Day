using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Orbit")]
    public float distance = 8f;
    public float height = 0.8f;
    public float shoulderOffset = 0.7f;
    [Range(0.1f, 5f)] public float sensitivity = 1f;
    public float minDistance = 3f;
    public float maxDistance = 15f;
    public float minPitch = -85f;
    public float maxPitch = 85f;

    [Header("Crosshair")]
    public Color crosshairColor = Color.white;
    public float crosshairSize = 8f;
    public float crosshairThickness = 2f;
    public float crosshairGap = 4f;

    private float currentYaw = 0f;
    private float currentPitch = 5f;
    

    void Start()
    {
        sensitivity = PlayerPrefs.GetFloat("CameraSensitivity", 1f);
        if (target != null) currentYaw = target.eulerAngles.y;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (target != null)
        {
            Vector3 rightDir = Quaternion.Euler(0, currentYaw, 0) * Vector3.right;
            Vector3 orbitCenter = target.position + rightDir * shoulderOffset + Vector3.up * height;
            Vector3 backDir = Quaternion.Euler(0, currentYaw, 0) * Vector3.back;
            transform.position = orbitCenter + Quaternion.AngleAxis(currentPitch, rightDir) * (backDir * distance);
            transform.LookAt(orbitCenter + Vector3.up * 1.2f);
            
        }

        CreateCrosshair();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            else
            { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }
        if (GameStateManager.IsInputFrozen)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            return;
        }
        if (Cursor.lockState != CursorLockMode.Locked)
        { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    }

    void LateUpdate()
    {
        if (target == null) return;
        if (GameStateManager.IsInputFrozen) return;

        if (Mouse.current != null)
        {
            currentYaw += Mouse.current.delta.x.ReadValue() * sensitivity * 3f;
            currentPitch -= Mouse.current.delta.y.ReadValue() * sensitivity * 3f;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
            distance -= Mouse.current.scroll.y.ReadValue() * 2f;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        Vector3 rightDir = Quaternion.Euler(0, currentYaw, 0) * Vector3.right;
        Vector3 orbitCenter = target.position + rightDir * shoulderOffset + Vector3.up * height;
        Vector3 backDir = Quaternion.Euler(0, currentYaw, 0) * Vector3.back;
        Vector3 camPos = orbitCenter + Quaternion.AngleAxis(currentPitch, rightDir) * (backDir * distance);

        transform.position = camPos;
        // 直接由 yaw/pitch 构造旋转，避开 LookAt 在极端俯仰角时的万向节翻转
        transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
    }

    void CreateCrosshair()
    {
        Transform existing = transform.Find("CrosshairCanvas");
        if (existing != null) Destroy(existing.gameObject);

        GameObject canvasGo = new GameObject("CrosshairCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform);
        canvasGo.transform.localPosition = Vector3.zero;
        canvasGo.transform.localRotation = Quaternion.identity;

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        CreateCrosshairPiece(canvasGo, "Dot", new Vector2(0.5f, 0.5f), new Vector2(crosshairThickness, crosshairThickness));
        CreateCrosshairPiece(canvasGo, "Top", new Vector2(0.5f, 0.5f + (crosshairGap + crosshairSize) / 1080f), new Vector2(crosshairThickness, crosshairSize));
        CreateCrosshairPiece(canvasGo, "Bottom", new Vector2(0.5f, 0.5f - (crosshairGap + crosshairSize) / 1080f), new Vector2(crosshairThickness, crosshairSize));
        CreateCrosshairPiece(canvasGo, "Left", new Vector2(0.5f - (crosshairGap + crosshairSize) / 1920f, 0.5f), new Vector2(crosshairSize, crosshairThickness));
        CreateCrosshairPiece(canvasGo, "Right", new Vector2(0.5f + (crosshairGap + crosshairSize) / 1920f, 0.5f), new Vector2(crosshairSize, crosshairThickness));
    }

    void CreateCrosshairPiece(GameObject parent, string name, Vector2 anchor, Vector2 size)
    {
        GameObject piece = new GameObject(name, typeof(RectTransform), typeof(Image));
        piece.transform.SetParent(parent.transform, false);
        RectTransform rt = piece.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.sizeDelta = size; rt.anchoredPosition = Vector2.zero;
        piece.GetComponent<Image>().color = crosshairColor;
    }
}
