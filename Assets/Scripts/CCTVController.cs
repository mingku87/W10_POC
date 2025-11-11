using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple CCTV controller that toggles between watching (red) and idle (green).
/// Place on a GameObject in the scene and assign a UI Image (top-right) to show the light.
/// Use CCTVController.IsWatching from other scripts to know whether the player is being watched.
/// </summary>
public class CCTVController : MonoBehaviour
{
    public Image lightImage; // assign in inspector (e.g. an Image in top-right canvas)
    public Color redColor = Color.red;
    public Color greenColor = Color.green;

    [Tooltip("How long the CCTV stays watching (red)")]
    public float watchDuration = 3f;
    [Tooltip("How long the CCTV stays idle (green)")]
    public float idleDuration = 5f;

    private bool isWatching;
    private float timer;

    public static CCTVController Instance { get; private set; }

    public static bool IsWatching => Instance != null && Instance.isWatching;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        SetWatching(false);
        timer = idleDuration;
        UpdateLight();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            // toggle state
            SetWatching(!isWatching);
            timer = isWatching ? watchDuration : idleDuration;
        }
        UpdateLight();
    }

    private void SetWatching(bool watching)
    {
        isWatching = watching;
    }

    private void UpdateLight()
    {
        if (lightImage != null)
        {
            lightImage.color = isWatching ? redColor : greenColor;
        }
    }
}
