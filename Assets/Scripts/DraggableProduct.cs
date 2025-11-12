using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 진열대 상품을 드래그하여:
/// 1. 스캔 존 위에 일정 시간 머물면 자동 스캔
/// 2. 손님 존으로 다시 드래그하여 배치
/// </summary>
public class DraggableProduct : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    [Header("자동 스캔 설정")]
    public float autoScanDelay = 0.1f; // 스캔 존 위에 머물러야 하는 시간 (초)

    public ProductInteractable productInteractable; // 연결된 상품 정보
    public bool isScanned = false; // 스캔 완료 여부
    public bool isClone = false; // 복사본 여부

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 lastValidPosition; // 마지막 유효 위치
    private Transform lastParent; // 마지막 부모
    private CustomerZone startZone = null;

    // 자동 스캔 관련
    private bool isOverScanner = false; // 현재 스캔 존 위에 있는지
    private float scanTimer = 0f; // 스캔 존 위에 머문 시간
    private bool isDragging = false; // 현재 드래그 중인지

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvas = GetComponentInParent<Canvas>();

        // ProductInteractable 자동 연결
        if (productInteractable == null)
        {
            productInteractable = GetComponent<ProductInteractable>();
        }
    }

    void Start()
    {
        // 초기 위치 저장
        lastValidPosition = rectTransform.anchoredPosition;
        lastParent = transform.parent;
    }

    void Update()
    {
        // 드래그 중이고, 스캔되지 않은 상태이며, 스캔 존 위에 있을 때만 타이머 증가
        if (isDragging && !isScanned && isOverScanner)
        {
            scanTimer += Time.deltaTime;

            // 일정 시간 이상 머물면 자동 스캔
            if (scanTimer >= autoScanDelay)
            {
                AutoScan();
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 손님이 없으면 드래그 불가
        if (CheckoutCounter.Instance == null || !CheckoutCounter.Instance.isCustomerWaiting)
        {
            Debug.LogWarning("[상품] 손님이 없어서 상품을 드래그할 수 없습니다!");
            eventData.pointerDrag = null;
            return;
        }

        // 복사본이 아니면 (진열대 원본) → 복사본 생성
        if (!isClone)
        {
            CreateDragClone(eventData);
            return;
        }

        // --- 여기서부터 복사본 드래그 시작 ---
        isDragging = true;
        scanTimer = 0f;
        isOverScanner = false;

        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false;

        // 현재 위치와 부모(lastParent) 저장
        lastValidPosition = rectTransform.anchoredPosition;
        lastParent = transform.parent;

        // 드래그 시작 시, 내가 CustomerZone에 있었는지 확인
        if (lastParent != null)
        {
            startZone = lastParent.GetComponent<CustomerZone>();
        }
        else
        {
            startZone = null;
        }

        // 최상위로 이동 (다른 UI 위에 표시)
        Canvas topCanvas = FindOrCreateDragCanvas();
        transform.SetParent(topCanvas.transform, true);
    }

    void CreateDragClone(PointerEventData eventData)
    {
        Canvas dragCanvas = FindOrCreateDragCanvas();

        // 복사본 생성
        GameObject dragClone = Instantiate(gameObject, dragCanvas.transform);
        dragClone.name = gameObject.name + "_Clone";

        DraggableProduct cloneDraggable = dragClone.GetComponent<DraggableProduct>();
        cloneDraggable.isClone = true;
        cloneDraggable.isScanned = false;
        cloneDraggable.productInteractable = this.productInteractable; // 원본 참조

        // Canvas 참조 설정
        cloneDraggable.canvas = dragCanvas;
        cloneDraggable.rectTransform = dragClone.GetComponent<RectTransform>();
        cloneDraggable.canvasGroup = dragClone.GetComponent<CanvasGroup>();

        // 복사본 위치를 원본과 동일하게
        RectTransform cloneRect = dragClone.GetComponent<RectTransform>();
        cloneRect.position = rectTransform.position;
        cloneRect.sizeDelta = rectTransform.sizeDelta;

        // 드래그 시작
        cloneDraggable.isDragging = true;
        cloneDraggable.scanTimer = 0f;
        cloneDraggable.isOverScanner = false;
        cloneDraggable.lastValidPosition = cloneRect.anchoredPosition;
        cloneDraggable.lastParent = dragCanvas.transform;
        cloneDraggable.canvasGroup.alpha = 0.7f;
        cloneDraggable.canvasGroup.blocksRaycasts = false;

        // Button 비활성화 (드래그 중 클릭 방지)
        Button cloneBtn = dragClone.GetComponent<Button>();
        if (cloneBtn != null)
        {
            cloneBtn.enabled = false;
        }

        Debug.Log($"[상품] {productInteractable.productData.productName} 복사본 생성!");

        // 원본 이벤트 취소하고 복사본에게 드래그 이벤트 넘김
        eventData.pointerDrag = dragClone;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isClone && rectTransform != null && canvas != null)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;

            // 드래그 중 스캔 존 위에 있는지 체크
            CheckIfOverScanner(eventData);
        }
    }

    /// <summary>
    /// 현재 드래그 중인 상품이 스캔 존 위에 있는지 체크
    /// </summary>
    void CheckIfOverScanner(PointerEventData eventData)
    {
        if (isScanned) return; // 이미 스캔된 상품은 체크하지 않음

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        bool foundScanner = false;
        foreach (var result in results)
        {
            BarcodeScanner scanner = result.gameObject.GetComponent<BarcodeScanner>();
            if (scanner != null)
            {
                foundScanner = true;
                break;
            }
        }

        // 스캔 존에 진입하거나 벗어났을 때 처리
        if (foundScanner && !isOverScanner)
        {
            // 스캔 존 진입
            isOverScanner = true;
            scanTimer = 0f;
            Debug.Log($"[상품] 스캔 존 진입: {productInteractable.productData.productName}");

            // 스캔 존 시각 효과
            if (BarcodeScanner.Instance != null)
            {
                BarcodeScanner.Instance.FlashScanEffect();
            }
        }
        else if (!foundScanner && isOverScanner)
        {
            // 스캔 존 이탈
            isOverScanner = false;
            scanTimer = 0f;
            Debug.Log($"[상품] 스캔 존 이탈: {productInteractable.productData.productName}");
        }
    }

    /// <summary>
    /// 자동 스캔 처리 - 스캔만 하고 드래그는 계속 유지
    /// </summary>
    void AutoScan()
    {
        if (isScanned) return;

        Debug.Log($"[상품] 자동 스캔 시작! {productInteractable.productData.productName}");
        OnScanned(); // 스캔 처리

        // OnScanned()에 의해 객체가 파괴되었는지 확인
        if (this == null || gameObject == null)
        {
            return;
        }

        // 스캔 성공 시 - 드래그는 계속 유지, 스캔 존을 마지막 유효 위치로 설정
        if (isScanned && BarcodeScanner.Instance != null)
        {
            // 스캔 존을 마지막 유효 위치로 기억 (드래그 끝날 때 여기로 돌아감)
            lastParent = BarcodeScanner.Instance.transform;
            lastValidPosition = Vector2.zero;

            // 스캔 존 효과
            BarcodeScanner.Instance.FlashScanEffect();

            // 타이머 리셋 (중복 스캔 방지)
            scanTimer = 0f;
            isOverScanner = false;

            Debug.Log($"[상품] 스캔 완료! 계속 드래그 가능");
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 복사본이 아니면 아무것도 안함
        if (!isClone)
        {
            return;
        }

        isDragging = false;
        isOverScanner = false;
        scanTimer = 0f;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Raycast로 Drop 대상 확인
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        CustomerZone endZone = null;
        bool validDropTargetFound = false;

        // 1. 드롭된 위치에서 유효한 타겟(손님 존) 찾기
        foreach (var result in results)
        {
            // 스캔 후이고, 손님 존을 찾았을 때
            if (isScanned)
            {
                endZone = result.gameObject.GetComponent<CustomerZone>();
                if (endZone != null)
                {
                    validDropTargetFound = true;
                    break;
                }
            }
        }

        // 2. 시작 존에서 제거 처리
        if (startZone != null && endZone != startZone)
        {
            // 시작 존의 리스트에서 이 상품을 제거
            startZone.RemoveProduct(this);
        }

        // 3. 유효한 타겟에 드롭한 경우 처리
        if (validDropTargetFound)
        {
            if (endZone != null)
            {
                // 손님 존 로직
                Debug.Log($"[상품] 손님 존에 배치 시도! {productInteractable.productData.productName}");
                // 실제 배치는 CustomerZone.OnDrop에서 처리할 것입니다.
                // CustomerZone.PlaceProduct 에서 UpdateLastValidPlacement()가 호출되어야 합니다.
            }
        }
        // 4. 유효하지 않은 곳에 드롭한 경우 (허공)
        else
        {
            Debug.Log($"[상품] 잘못된 위치에 Drop - 원위치로 복귀");

            // 스캔 전 상품을 허공에 버리면 파괴
            if (!isScanned)
            {
                Destroy(gameObject);
            }
            // 스캔 후 상품을 허공에 버리면 마지막 유효 위치로 복귀
            else
            {
                // 스캔 존에 배치
                transform.SetParent(lastParent);
                rectTransform.anchoredPosition = lastValidPosition;

                Debug.Log($"[상품] 스캔 존으로 복귀");
            }
        }
    }

    /// <summary>
    /// 스캔 처리 - 계산대에 금액 추가
    /// </summary>
    void OnScanned()
    {
        if (isScanned)
        {
            Debug.LogWarning("[상품] 이미 스캔된 상품입니다!");
            return;
        }

        // 손님이 원하는 상품인지 확인
        CustomerManager manager = FindFirstObjectByType<CustomerManager>();
        if (manager != null && manager.currentCheckoutCustomer != null)
        {
            Customer customer = manager.currentCheckoutCustomer;

            // 손님이 원하는 상품 목록에 있는지 확인
            bool isWantedProduct = false;
            ProductInteractable matchedProduct = null;

            foreach (var wantedProduct in customer.selectedProducts)
            {
                if (wantedProduct.productData.productName == productInteractable.productData.productName)
                {
                    isWantedProduct = true;
                    matchedProduct = wantedProduct;
                    break;
                }
            }

            // 잘못된 상품 스캔
            if (!isWantedProduct)
            {
                Debug.LogWarning($"[경고] 손님이 원하지 않는 상품! (스캔: {productInteractable.productData.productName})");

                if (POSSystem.Instance != null)
                {
                    POSSystem.Instance.AddMistake();
                }

                // 스캔 실패 - 복사본 삭제
                Destroy(gameObject);
                return;
            }

            // 중복 스캔 체크 (멀쩡한 손님만)
            if (customer.customerType != Customer.CustomerType.Drunk && !customer.isOnPhone)
            {
                if (BarcodeScanner.Instance != null &&
                    BarcodeScanner.Instance.IsProductScanned(matchedProduct))
                {
                    Debug.LogWarning("[경고] 이미 스캔한 상품을 다시 스캔했습니다!");

                    if (POSSystem.Instance != null)
                    {
                        POSSystem.Instance.AddMistake();
                    }

                    // 중복 스캔 - 복사본 삭제
                    Destroy(gameObject);
                    return;
                }

                // 스캔 기록 추가
                if (BarcodeScanner.Instance != null)
                {
                    BarcodeScanner.Instance.AddScannedProduct(matchedProduct);
                }
            }
        }

        // 계산대에 금액 추가
        if (CheckoutCounter.Instance != null)
        {
            CheckoutCounter.Instance.AddScannedItem(productInteractable);
        }

        isScanned = true;

        // 시각적 피드백 (스캔 완료 표시)
        Image img = GetComponent<Image>();
        if (img != null)
        {
            img.color = new Color(0.8f, 1f, 0.8f); // 연한 초록색
        }

        Debug.Log($"[스캔 완료] {productInteractable.productData.productName} - {productInteractable.GetCurrentPrice()}원");
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
    /// 아이템이 스캐너나 손님 존에 성공적으로 배치되었을 때 호출됩니다.
    /// (CustomerZone.PlaceProduct 또는 DraggableProduct.OnEndDrag 에서 호출)
    /// </summary>
    public void UpdateLastValidPlacement(Transform newParent, CustomerZone newZone)
    {
        lastParent = newParent;
        lastValidPosition = rectTransform.anchoredPosition;
        startZone = newZone; // 손님 존이 아니면 null이 됨
    }
}