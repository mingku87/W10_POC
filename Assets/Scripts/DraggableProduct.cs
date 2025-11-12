using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 진열대 상품을 드래그하여:
/// 1. 스캔 존에 Drop → 스캔 처리 + 계산대 금액 추가
/// 2. 손님 존으로 다시 드래그하여 배치
/// </summary>
public class DraggableProduct : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ProductInteractable productInteractable; // 연결된 상품 정보
    public bool isScanned = false; // 스캔 완료 여부
    public bool isClone = false; // 복사본 여부

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 lastValidPosition; // 마지막 유효 위치
    private Transform lastParent; // 마지막 부모

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

        // 복사본이면 드래그 시작
        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false;

        // 현재 위치 저장
        lastValidPosition = rectTransform.anchoredPosition;
        lastParent = transform.parent;

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
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 복사본이 아니면 아무것도 안함
        if (!isClone)
        {
            return;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Raycast로 Drop 대상 확인
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        bool validDrop = false;

        foreach (var result in results)
        {
            // 1. 스캔 존에 Drop (아직 스캔 안됐을 때만)
            if (!isScanned)
            {
                BarcodeScanner scanner = result.gameObject.GetComponent<BarcodeScanner>();
                if (scanner != null)
                {
                    Debug.Log($"[상품] 스캔 존에 Drop! {productInteractable.productData.productName}");
                    OnScanned();

                    // 스캔 존에 배치
                    transform.SetParent(scanner.transform);
                    rectTransform.anchoredPosition = Vector2.zero;
                    validDrop = true;
                    break;
                }
            }
            // 2. 손님 존에 Drop (스캔 완료 후에만)
            else
            {
                // CustomerZone 컴포넌트로 확인
                CustomerZone customerZone = result.gameObject.GetComponent<CustomerZone>();
                if (customerZone != null)
                {
                    Debug.Log($"[상품] 손님 존에 배치 시도! {productInteractable.productData.productName}");

                    // CustomerZone의 OnDrop이 배치를 처리함
                    validDrop = true;
                    break;
                }
            }
        }

        // 유효하지 않은 Drop → 마지막 위치로 복귀
        if (!validDrop)
        {
            Debug.Log($"[상품] 잘못된 위치에 Drop - 원위치로 복귀");

            if (isScanned)
            {
                // 스캔 완료 상태면 스캔 존으로 복귀
                BarcodeScanner scanner = FindFirstObjectByType<BarcodeScanner>();
                if (scanner != null)
                {
                    transform.SetParent(scanner.transform);
                    rectTransform.anchoredPosition = lastValidPosition;
                }
            }
            else
            {
                // 스캔 전이면 삭제 (원본은 진열대에 유지)
                Destroy(gameObject);
            }
        }
        else
        {
            // 유효한 Drop이면 현재 위치를 마지막 위치로 저장
            lastValidPosition = rectTransform.anchoredPosition;
            lastParent = transform.parent;
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
}