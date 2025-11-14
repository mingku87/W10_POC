using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 사기 모드 컨트롤러 - T키로 활성화/비활성화
/// 사기 모드 중에는 시야가 좁아지고 마우스를 따라다님
/// 라벨 조작과 중복 스캔은 사기 모드에서만 가능
/// </summary>
public class FraudModeController : MonoBehaviour
{
    public static FraudModeController Instance { get; private set; }

    [Header("사기 모드 상태")]
    public bool isFraudModeActive = false;

    [Header("시야 제한 설정")]
    [Tooltip("시야 범위 반경 (픽셀)")]
    public float visionRadius = 200f;

    [Tooltip("시야 외곽 페이드 크기")]
    public float fadeSize = 120f;

    [Header("UI 요소")]
    private GameObject visionOverlay;
    private GameObject visionHole;
    private RectTransform holeTransform;
    private Image holeImage;
    private TextMeshProUGUI modeText;

    [Header("디버그")]
    public bool showDebugInfo = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        CreateVisionOverlay();
        SetFraudMode(false); // 시작 시 비활성화
    }

    void Update()
    {
        // T키로 사기 모드 토글
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("[사기 모드] T키 눌림 감지!");
            ToggleFraudMode();
        }

        // 사기 모드 활성화 중이면 마우스 위치 추적
        if (isFraudModeActive && visionOverlay != null && visionOverlay.activeSelf)
        {
            UpdateVisionPosition();
            // 최상위 유지
            visionOverlay.transform.SetAsLastSibling();
        }
    }

    void CreateVisionOverlay()
    {
        // 독립적인 오버레이용 Canvas 생성 (기존 Canvas에 붙이지 않음)
        GameObject canvasObj = new GameObject("FraudModeCanvas");
        Canvas overlayCanvas = canvasObj.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 1500; // 최상위 레이어

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Debug.Log("[사기 모드] 독립 Canvas 생성 완료");

        // 메인 오버레이 GameObject 생성
        visionOverlay = new GameObject("FraudModeVisionOverlay");
        visionOverlay.transform.SetParent(canvasObj.transform, false);

        RectTransform overlayRect = visionOverlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        // CanvasGroup으로 레이캐스트 차단 안함
        CanvasGroup canvasGroup = visionOverlay.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;

        // 검은색 오버레이 (구멍 뚫린 텍스처 사용)
        visionHole = new GameObject("VisionOverlay");
        visionHole.transform.SetParent(visionOverlay.transform, false);

        holeTransform = visionHole.AddComponent<RectTransform>();
        holeTransform.anchorMin = Vector2.zero;
        holeTransform.anchorMax = Vector2.one;
        holeTransform.offsetMin = Vector2.zero;
        holeTransform.offsetMax = Vector2.zero;

        holeImage = visionHole.AddComponent<Image>();
        holeImage.raycastTarget = false;

        // 초기 텍스처 생성 (화면 중앙에 구멍)
        UpdateVisionTexture(new Vector2(Screen.width / 2, Screen.height / 2));

        // Debug.Log("[사기 모드] 시야 제한 오버레이 생성 완료");

        // 안내 텍스트 생성
        CreateModeText(visionOverlay.transform);

        visionOverlay.SetActive(false);

        // Debug.Log("[사기 모드] Vision Overlay 생성 완료");
    }

    void CreateModeText(Transform parent)
    {
        GameObject textObj = new GameObject("ModeText");
        textObj.transform.SetParent(parent, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 1f);
        textRect.anchorMax = new Vector2(0.5f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = new Vector2(0, -50);
        textRect.sizeDelta = new Vector2(600, 100);

        modeText = textObj.AddComponent<TextMeshProUGUI>();
        modeText.text = "🎭 사기 모드 활성화\n시야가 제한됩니다. 조심하세요!\n(T키를 눌러 해제)";
        modeText.fontSize = 24;
        modeText.fontStyle = FontStyles.Bold;
        modeText.alignment = TextAlignmentOptions.Center;
        modeText.color = new Color(1f, 0.3f, 0.3f); // 빨간색
        modeText.raycastTarget = false;

        // 외곽선 추가
        var outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, 2);
    }

    void CreateVisionHole(Transform parent)
    {
        // 이제 사용하지 않음
    }

    void UpdateVisionTexture(Vector2 mousePosition)
    {
        if (holeImage == null) return;

        // 화면 비율에 맞는 텍스처 크기 설정
        int width = 512;
        int height = Mathf.RoundToInt(width * ((float)Screen.height / Screen.width));

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        // 마우스 위치를 텍스처 좌표로 변환
        float centerX = (mousePosition.x / Screen.width) * width;
        float centerY = (mousePosition.y / Screen.height) * height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // 이제 정확한 원형 계산
                float dx = x - centerX;
                float dy = y - centerY;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // 픽셀 크기로 조정
                float pixelRadius = (visionRadius / Screen.width) * width;
                float pixelFade = (fadeSize / Screen.width) * width;

                Color color;
                if (dist <= pixelRadius)
                {
                    // 중심부는 투명 (보이는 부분)
                    color = new Color(0, 0, 0, 0);
                }
                else if (dist <= pixelRadius + pixelFade)
                {
                    // 페이드 영역
                    float t = (dist - pixelRadius) / pixelFade;
                    float alpha = Mathf.SmoothStep(0f, 1f, t);
                    color = new Color(0, 0, 0, alpha);
                }
                else
                {
                    // 외곽은 검은색
                    color = new Color(0, 0, 0, 1);
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();

        // 스프라이트 생성 및 적용
        if (holeImage.sprite != null)
        {
            Destroy(holeImage.sprite.texture);
            Destroy(holeImage.sprite);
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f)
        );
        holeImage.sprite = sprite;
        holeImage.color = Color.white;
    }

    void UpdateVisionPosition()
    {
        if (holeImage == null) return;

        // 마우스 위치로 텍스처 업데이트
        UpdateVisionTexture(Input.mousePosition);
    }

    public void ToggleFraudMode()
    {
        SetFraudMode(!isFraudModeActive);
    }

    public void SetFraudMode(bool active)
    {
        isFraudModeActive = active;

        // Debug.Log($"[사기 모드] SetFraudMode 호출됨: {active}, visionOverlay: {visionOverlay != null}");

        if (visionOverlay != null)
        {
            visionOverlay.SetActive(active);
            // Debug.Log($"[사기 모드] visionOverlay.SetActive({active}) 실행됨");
        }
        else
        {
            // Debug.LogError("[사기 모드] visionOverlay가 null입니다!");
        }

        if (active)
        {
            // Debug.Log("🎭 [사기 모드] 활성화! 시야가 제한됩니다. 조심하세요!");
        }
        else
        {
            // Debug.Log("✅ [사기 모드] 비활성화! 정상 시야로 복귀합니다.");
        }
    }

    public bool CanPerformFraud()
    {
        return isFraudModeActive;
    }
}
