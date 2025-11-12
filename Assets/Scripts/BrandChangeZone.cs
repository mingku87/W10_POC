using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 브랜드 변경 존 - 하급 브랜드 복사본을 받아서 상급(가짜) 상품 생성
/// Unity 6.0 호환
/// </summary>
public class BrandChangeZone : MonoBehaviour, IDropHandler
{
    public static BrandChangeZone Instance { get; private set; }

    [Header("UI 표시")]
    public TextMeshProUGUI statusText; // "브랜드 변경 준비" 같은 텍스트
    public Color normalColor = new Color(0.8f, 0.3f, 0.3f, 0.3f);      // 평소: 붉은빛
    public Color highlightColor = new Color(0.8f, 0.5f, 0.3f, 0.5f);   // 드래그 중: 주황빛
    public Color successColor = new Color(0.3f, 0.8f, 0.3f, 0.6f);     // 성공: 초록빛

    [Header("변경 설정")]
    public float changeDelay = 0.5f; // 브랜드 변경에 걸리는 시간 (초)
    public Vector2 fakeProductSpawnOffset = Vector2.zero; // 생성 위치 오프셋

    [Header("가짜 상품 프리팹")]
    public GameObject fakeProductPrefab; // 가짜 상품 UI 프리팹 (옵션)

    private Image backgroundImage;
    private bool isProcessing = false; // 변경 처리 중인지

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

        UpdateStatusText("브랜드 변경 존\n하급 복사본을 드래그하세요");

        Debug.Log("[브랜드 변경 존] 초기화 완료");
    }

    // IDropHandler 구현
    public void OnDrop(PointerEventData eventData)
    {
        if (isProcessing)
        {
            Debug.LogWarning("[브랜드 변경 존] 이미 변경 처리 중입니다!");
            return;
        }

        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject == null) return;

        // DraggableProduct 확인
        DraggableProduct draggable = droppedObject.GetComponent<DraggableProduct>();
        if (draggable == null)
        {
            Debug.LogWarning("[브랜드 변경 존] DraggableProduct가 아닙니다!");
            return;
        }

        // 복사본만 변경 가능
        if (!draggable.isClone)
        {
            Debug.LogWarning("[브랜드 변경 존] 원본은 브랜드를 변경할 수 없습니다! 드래그해서 생성된 복사본을 사용하세요.");
            UpdateStatusText("❌ 원본은 변경 불가!\n복사본을 드래그하세요");
            FlashBackground(Color.red, 0.5f);
            return;
        }

        ProductInteractable product = draggable.productInteractable;
        if (product == null || product.productData == null)
        {
            Debug.LogWarning("[브랜드 변경 존] 상품 정보가 없습니다!");
            return;
        }

        // 브랜드 변경 가능 여부 체크
        if (!CanConvertToFake(product.productData))
        {
            return;
        }

        // 가짜 상품 생성
        CreateFakeProduct(draggable, eventData);
    }

    /// <summary>
    /// 브랜드 변경 가능 여부 체크
    /// </summary>
    bool CanConvertToFake(ProductData productData)
    {
        // 통조림만 변경 가능
        if (productData.productType != ProductType.CannedPork)
        {
            Debug.LogWarning($"[브랜드 변경 실패] {productData.productName}은(는) 통조림이 아닙니다!");
            UpdateStatusText($"❌ 통조림만 변경 가능합니다");
            FlashBackground(Color.red, 0.5f);
            return false;
        }

        // 이미 가짜면 변경 불가
        if (productData.isFake)
        {
            Debug.LogWarning($"[브랜드 변경 실패] {productData.productName}은(는) 이미 가짜 브랜드입니다!");
            UpdateStatusText($"❌ 이미 가짜 브랜드입니다");
            FlashBackground(Color.red, 0.5f);
            return false;
        }

        // 이미 상급이면 변경 불가
        if (productData.currentBrand == BrandGrade.High)
        {
            Debug.LogWarning($"[브랜드 변경 실패] {productData.productName}은(는) 이미 상급 브랜드입니다!");
            UpdateStatusText($"❌ 이미 상급 브랜드입니다");
            FlashBackground(Color.red, 0.5f);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 가짜 상품 생성
    /// </summary>
    void CreateFakeProduct(DraggableProduct originalDraggable, PointerEventData eventData)
    {
        isProcessing = true;

        // 1. 드롭 위치 계산 (Zone 내부 로컬 좌표)
        Vector2 dropLocalPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out dropLocalPosition
        );

        // 2. 원본 복사본의 ProductData 복사 (참조가 아닌 진짜 복사)
        ProductInteractable originalProduct = originalDraggable.productInteractable;
        ProductData fakeData = CopyAndConvertToFake(originalProduct.productData);

        // 3. 원본 복사본 삭제
        Destroy(originalDraggable.gameObject);

        // 4. 가짜 상품 오브젝트 생성
        GameObject fakeProductObj;

        if (fakeProductPrefab != null)
        {
            // 프리팹이 있으면 프리팹 사용
            fakeProductObj = Instantiate(fakeProductPrefab, transform);
        }
        else
        {
            // 프리팹이 없으면 원본 GameObject 복제
            fakeProductObj = Instantiate(originalProduct.gameObject, transform);
        }

        // 5. 가짜 상품 위치 설정 (드롭한 위치에 정확히 배치)
        RectTransform fakeRect = fakeProductObj.GetComponent<RectTransform>();
        if (fakeRect != null)
        {
            fakeRect.anchoredPosition = dropLocalPosition; // ✅ 드롭 위치 사용
        }

        // 5. ProductInteractable 설정
        ProductInteractable fakeProduct = fakeProductObj.GetComponent<ProductInteractable>();
        if (fakeProduct == null)
        {
            fakeProduct = fakeProductObj.AddComponent<ProductInteractable>();
        }
        fakeProduct.productData = fakeData;
        fakeProduct.InitializeAsNewProduct(); // UI 초기화

        // 6. DraggableProduct 설정
        DraggableProduct fakeDraggable = fakeProductObj.GetComponent<DraggableProduct>();
        if (fakeDraggable == null)
        {
            fakeDraggable = fakeProductObj.AddComponent<DraggableProduct>();
        }
        fakeDraggable.isClone = true; // 복사본으로 설정
        fakeDraggable.isScanned = false; // 스캔 전 상태
        fakeDraggable.productInteractable = fakeProduct;

        // 7. 시각 효과
        UpdateStatusText($"✅ 가짜 브랜드 생성!\n{fakeData.productName}\n하급 → 상급(가짜)");
        FlashBackground(successColor, changeDelay);

        Debug.Log($"[브랜드 변경 성공] {fakeData.productName} - 가짜 상품 생성 완료! 새 가격: {fakeData.GetAdjustedPrice()}원");

        // 8. 처리 완료
        Invoke(nameof(ResetZone), changeDelay + 0.5f);
    }

    /// <summary>
    /// ProductData 복사 및 가짜로 변환
    /// </summary>
    ProductData CopyAndConvertToFake(ProductData original)
    {
        // ✅ 원본 ProductData를 복사 (깊은 복사)
        ProductData fakeData = new ProductData(
            original.productName,
            original.originalPrice,
            original.productType,
            original.currentBrand // 현재는 Low
        );

        // Sprite도 복사 (있으면)
        fakeData.productSprite = original.productSprite;

        // ✅ 하급 → 상급(가짜)로 변환
        fakeData.TryConvertToFakeHigh();
        // 결과: currentBrand = High, isFake = true, originalBrand = Low

        return fakeData;
    }

    /// <summary>
    /// 존 초기화
    /// </summary>
    void ResetZone()
    {
        isProcessing = false;
        backgroundImage.color = normalColor;
        UpdateStatusText("브랜드 변경 존\n하급 복사본을 드래그하세요");
    }

    /// <summary>
    /// 상태 텍스트 업데이트
    /// </summary>
    void UpdateStatusText(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }
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

    // 마우스 오버 시 하이라이트
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isProcessing && backgroundImage != null)
        {
            backgroundImage.color = highlightColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isProcessing && backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
    }
}