using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 드래그 가능한 브랜드 커버
/// ChangeBrandZone 위의 ProductInteractable에 호버하면 0.1초 후 브랜드 변경
/// Unity 6.0 최신 버전 호환
/// </summary>
public class BrandChangeCover : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("브랜드 데이터")]
    public BrandData brandData;  // 이 커버가 적용할 브랜드 정보

    [Header("변경 설정")]
    private float hoverDelay = 0.02f;  // 호버 시 브랜드 변경까지의 대기 시간

    [Header("드래그 설정")]
    public bool isDraggable = true;  // 드래그 가능 여부

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Vector3 originalWorldPosition;  // 월드 좌표 저장
    private Transform originalParent;
    private LayoutElement layoutElement;  // Layout 제어용

    // 호버 감지
    private bool isDragging = false;
    private ProductInteractable hoveredProduct = null;
    private float hoverTimer = 0f;
    private bool isProcessing = false;  // 브랜드 변경 처리 중

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvas = GetComponentInParent<Canvas>();

        // LayoutElement 가져오기 (HorizontalLayoutGroup 사용 시 필요)
        layoutElement = GetComponent<LayoutElement>();

        // 이미지 컴포넌트가 없으면 추가
        Image img = GetComponent<Image>();
        if (img == null)
        {
            img = gameObject.AddComponent<Image>();
        }

        // 브랜드 데이터의 스프라이트 적용
        if (brandData != null && brandData.brandCoverSprite != null)
        {
            img.sprite = brandData.brandCoverSprite;
        }
    }

    void Start()
    {
        // 초기 위치와 부모 저장 (로컬 좌표와 월드 좌표 모두)
        originalPosition = rectTransform.anchoredPosition;
        originalWorldPosition = rectTransform.position;  // 월드 좌표 저장
        originalParent = transform.parent;
    }

    void Update()
    {
        // 드래그 중이고, 제품 위에 호버 중일 때
        if (isDragging && hoveredProduct != null && !isProcessing)
        {
            hoverTimer += Time.deltaTime;

            // 일정 시간 이상 호버하면 브랜드 변경
            if (hoverTimer >= hoverDelay)
            {
                StartCoroutine(ChangeBrand());
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isDraggable) return;

        isDragging = true;
        hoverTimer = 0f;
        hoveredProduct = null;

        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false;

        // LayoutElement가 있으면 비활성화 (HorizontalLayoutGroup 영향 제거)
        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = true;
        }

        // 최상위 캔버스로 이동
        Canvas topCanvas = FindOrCreateDragCanvas();
        transform.SetParent(topCanvas.transform, true);

        Debug.Log($"[브랜드 커버] 드래그 시작: {brandData.targetProductType}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggable || !isDragging) return;

        // 위치 업데이트
        if (rectTransform != null && canvas != null)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }

        // 현재 마우스 아래의 오브젝트들 확인
        CheckHoveredProduct(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggable) return;

        isDragging = false;
        hoverTimer = 0f;
        hoveredProduct = null;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // 부모 복귀 (로컬 좌표 유지)
        transform.SetParent(originalParent, false);

        // LayoutElement 재활성화
        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = false;
        }

        // LayoutGroup에게 위치를 맡김 - 수동 위치 설정 제거!

        Debug.Log($"[브랜드 커버] 드래그 종료");
    }

    /// <summary>
    /// 현재 마우스 아래에 ProductInteractable이 있는지 확인
    /// </summary>
    void CheckHoveredProduct(PointerEventData eventData)
    {
        if (isProcessing) return;  // 이미 처리 중이면 무시

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        ProductInteractable foundProduct = null;
        BrandChangeZone foundZone = null;

        // Raycast 결과에서 BrandChangeZone과 ProductInteractable 찾기
        foreach (var result in results)
        {
            // BrandChangeZone 확인
            if (foundZone == null)
            {
                foundZone = result.gameObject.GetComponent<BrandChangeZone>();
            }

            // ProductInteractable 확인
            if (foundProduct == null)
            {
                foundProduct = result.gameObject.GetComponent<ProductInteractable>();
            }

            // 둘 다 찾았으면 종료
            if (foundZone != null && foundProduct != null)
            {
                break;
            }
        }

        // BrandChangeZone 위에 있는 ProductInteractable만 유효
        if (foundZone != null && foundProduct != null)
        {
            // 새로운 제품 위에 호버 시작
            if (hoveredProduct != foundProduct)
            {
                hoveredProduct = foundProduct;
                hoverTimer = 0f;
                Debug.Log($"[브랜드 커버] 제품 위 호버 시작: {foundProduct.productData.productName}");
            }
        }
        else
        {
            // 호버 해제
            if (hoveredProduct != null)
            {
                Debug.Log($"[브랜드 커버] 호버 해제");
                hoveredProduct = null;
                hoverTimer = 0f;
            }
        }
    }

    /// <summary>
    /// 브랜드 변경 처리
    /// </summary>
    IEnumerator ChangeBrand()
    {
        if (hoveredProduct == null || isProcessing)
            yield break;

        isProcessing = true;

        ProductData productData = hoveredProduct.productData;

        // 유효성 검사
        if (productData == null)
        {
            Debug.LogWarning("[브랜드 커버] ProductData가 없습니다!");
            isProcessing = false;
            yield break;
        }

        // 제품 타입이 일치하는지 확인
        if (productData.productType != brandData.targetProductType)
        {
            Debug.LogWarning($"[브랜드 커버] 제품 타입 불일치! 커버: {brandData.targetProductType}, 제품: {productData.productType}");
            isProcessing = false;
            yield break;
        }

        // 이미 가짜 제품인지 확인
        if (productData.isFake)
        {
            Debug.LogWarning($"[브랜드 커버] 이미 가짜 제품입니다: {productData.productName}");
            isProcessing = false;
            yield break;
        }

        // 이미 상급 브랜드인지 확인
        if (productData.currentBrand == brandData.targetBrand)
        {
            Debug.LogWarning($"[브랜드 커버] 이미 목표 브랜드입니다: {productData.productName}");
            isProcessing = false;
            yield break;
        }

        // ProductDataManager에서 가짜 제품 데이터 찾기
        ProductData fakeProductData = ProductDataManager.Instance.FindFakeProduct(
            productData.productType,
            brandData.targetBrand
        );

        if (fakeProductData == null)
        {
            Debug.LogError($"[브랜드 커버] 가짜 제품 데이터를 찾을 수 없습니다! Type: {productData.productType}, Brand: {brandData.targetBrand}");
            isProcessing = false;
            yield break;
        }

        // 브랜드 변경 애니메이션 (선택적)
        float elapsed = 0f;
        Color originalColor = canvasGroup.GetComponent<Image>().color;

        while (elapsed < hoverDelay)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / hoverDelay;
            // 깜빡임 효과
            canvasGroup.alpha = 0.5f + Mathf.Sin(t * Mathf.PI * 4) * 0.3f;
            yield return null;
        }

        canvasGroup.alpha = 0.7f;




        if (hoveredProduct == null)
        {
            Debug.LogWarning("[브랜드 커버] 변경 처리 직전에 호버가 해제되었습니다. 작업을 중단합니다.");
            isProcessing = false;

            yield break;
        }


        // ✨ Customer의 isOnPhone 상태 체크
        Customer currentCustomer = FindCurrentCustomer();
        if (currentCustomer != null)
        {
            if (currentCustomer.isOnPhone)
            {
                // 휴대폰 보는 중이면 실수 감지 안됨
                Debug.Log($"[브랜드 커버] ✅ 손님이 휴대폰을 보는 중! 브랜드 변경 성공!");
            }
            else
            {
                // 휴대폰 안 보고 있으면 실수 카운트 증가
                Debug.Log($"[브랜드 커버] ⚠️ 손님이 보고 있음! 실수 카운트 증가!");

                // POSSystem의 실수 카운트 증가
                if (MistakeManager.Instance != null)
                {
                    MistakeManager.Instance.AddMistake(
                        MistakeManager.MistakeType.BrandChangeDetected,
                        "손님이 브랜드 변경 목격"
                    );
                }
            }
        }

        // 원본 제품의 실제 원가 저장 (하급 브랜드 가격)
        int originalRealCost = hoveredProduct.productData.originalPrice;

        // 가짜 제품의 판매가 (상급 브랜드 가격)
        int fakePrice = fakeProductData.originalPrice;

        // ProductData는 원본 그대로 유지하고, BarcodeData만 변경
        hoveredProduct.SetBarcode(new BarcodeData("FAKE", fakePrice, originalRealCost));

        // 시각 효과를 위해 ProductData의 isFake만 true로 설정
        hoveredProduct.productData.isFake = true;
        hoveredProduct.productData.originalBrand = hoveredProduct.productData.currentBrand;  // 원래 브랜드 저장
        hoveredProduct.productData.currentBrand = brandData.targetBrand;  // 현재 브랜드 변경

        // UI 갱신
        hoveredProduct.UpdateUI();

        Debug.Log($"[브랜드 커버] 브랜드 변경 완료!");
        Debug.Log($"  - 제품: {hoveredProduct.productData.productName}");
        Debug.Log($"  - 원래 브랜드: {hoveredProduct.productData.originalBrand} (실제 원가: {originalRealCost}원)");
        Debug.Log($"  - 가짜 브랜드: {hoveredProduct.productData.currentBrand} (판매가: {fakePrice}원)");
        Debug.Log($"  - 이익: {fakePrice - originalRealCost}원");

        // 성공 효과
        if (BrandChangeZone.Instance != null)
        {
            BrandChangeZone.Instance.ShowSuccessEffect(hoveredProduct.transform.position);
        }

        // 커버 사용 후 원위치로 복귀
        isDragging = false;
        hoveredProduct = null;
        hoverTimer = 0f;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // 원위치로 복귀 (월드 좌표 복원)
        transform.SetParent(originalParent, false);  // 월드 좌표 유지하면서 부모 설정

        // LayoutElement가 있으면 재활성화
        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = false;
        }

        isProcessing = false;

        Debug.Log($"[브랜드 커버] 사용 완료, 원위치 복귀");
    }

    /// <summary>
    /// 드래그용 최상위 Canvas 찾기 또는 생성
    /// </summary>
    Canvas FindOrCreateDragCanvas()
    {
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in allCanvases)
        {
            if (c.name == "DragCanvas" && c.sortingOrder == 300)
            {
                return c;
            }
        }

        GameObject dragCanvasObj = new GameObject("DragCanvas");
        Canvas dragCanvas = dragCanvasObj.AddComponent<Canvas>();
        dragCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dragCanvas.sortingOrder = 300;
        dragCanvasObj.AddComponent<GraphicRaycaster>();

        return dragCanvas;
    }

    /// <summary>
    /// 현재 계산대에 있는 Customer 찾기
    /// </summary>
    Customer FindCurrentCustomer()
    {
        // CheckoutCounter의 currentCustomer 가져오기
        if (CheckoutCounter.Instance != null)
        {
            return CheckoutCounter.Instance.currentCustomer;
        }

        return null;
    }

    /// <summary>
    /// 브랜드 데이터 설정 (런타임)
    /// </summary>
    public void SetBrandData(BrandData data)
    {
        brandData = data;

        // 이미지 업데이트
        Image img = GetComponent<Image>();
        if (img != null && data != null && data.brandCoverSprite != null)
        {
            img.sprite = data.brandCoverSprite;
        }
    }
}