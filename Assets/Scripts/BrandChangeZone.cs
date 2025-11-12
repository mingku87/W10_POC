using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 브랜드 변경 존 - BrandChangeCover를 사용하여 수동으로 브랜드 변경
/// 자동 변경 기능 제거됨 (Unity 6.0 호환)
/// </summary>
public class BrandChangeZone : MonoBehaviour
{
    public static BrandChangeZone Instance { get; private set; }

    [Header("UI 표시")]
    public TextMeshProUGUI statusText; // "브랜드 변경 존" 같은 텍스트
    public Color normalColor = new Color(0.8f, 0.3f, 0.3f, 0.3f);      // 평소: 붉은빛
    public Color highlightColor = new Color(0.8f, 0.5f, 0.3f, 0.5f);   // 드래그 중: 주황빛
    public Color successColor = new Color(0.3f, 0.8f, 0.3f, 0.6f);     // 성공: 초록빛

    [Header("효과 설정")]
    public GameObject successEffectPrefab;  // 성공 효과 프리팹 (선택)
    public float effectDuration = 0.5f;     // 효과 지속 시간

    private Image backgroundImage;

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
        // UI 초기화
        backgroundImage = GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = gameObject.AddComponent<Image>();
        }
        backgroundImage.color = normalColor;

        UpdateStatusText("브랜드 변경 존\nBrandChangeCover를 사용하세요");

        Debug.Log("[브랜드 변경 존] 초기화 완료 (수동 모드)");
    }

    /// <summary>
    /// 상태 텍스트 업데이트
    /// </summary>
    public void UpdateStatusText(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }
    }

    /// <summary>
    /// 성공 효과 표시 - BrandChangeCover에서 호출
    /// </summary>
    /// <param name="worldPosition">효과를 표시할 월드 좌표</param>
    public void ShowSuccessEffect(Vector3 worldPosition)
    {
        // 배경 깜빡임
        FlashBackground(successColor, effectDuration);

        // 상태 텍스트 업데이트
        UpdateStatusText("✓ 브랜드 변경 성공!");
        Invoke(nameof(ResetStatusText), effectDuration + 0.5f);

        // 성공 효과 프리팹 생성 (선택적)
        if (successEffectPrefab != null)
        {
            GameObject effect = Instantiate(successEffectPrefab, transform);
            RectTransform effectRect = effect.GetComponent<RectTransform>();

            if (effectRect != null)
            {
                // 월드 좌표를 로컬 좌표로 변환
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    GetComponent<RectTransform>(),
                    RectTransformUtility.WorldToScreenPoint(null, worldPosition),
                    null,
                    out localPoint
                );
                effectRect.anchoredPosition = localPoint;
            }

            Destroy(effect, effectDuration);
        }

        Debug.Log($"[브랜드 변경 존] 성공 효과 표시");
    }

    /// <summary>
    /// 배경 깜빡임 효과
    /// </summary>
    void FlashBackground(Color targetColor, float duration)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = targetColor;
            Invoke(nameof(ResetBackgroundColor), duration);
        }
    }

    void ResetBackgroundColor()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
    }

    void ResetStatusText()
    {
        UpdateStatusText("브랜드 변경 존\nBrandChangeCover를 사용하세요");
    }

    // 마우스 오버 시 하이라이트
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = highlightColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
    }
}