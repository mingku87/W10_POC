using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 바코드 스캐너 - 드래그해서 상품에 가져다 대고 스캔 (UI 기반)
/// </summary>
public class BarcodeScanner : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static BarcodeScanner Instance { get; private set; }

    [Header("스캔 설정")]
    public float scanRange = 200f; // 스캔 가능 거리 (UI 픽셀)
    public Color scanReadyColor = Color.green; // 스캔 가능할 때 색
    public Color scanNotReadyColor = Color.gray; // 스캔 불가능할 때 색

    private Image scannerImage; // UI Image
    private RectTransform rectTransform;
    private Canvas canvas;
    private HashSet<ProductInteractable> scannedProducts = new HashSet<ProductInteractable>();

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
        scannerImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        if (scannerImage != null)
        {
            scannerImage.color = scanNotReadyColor;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 드래그 시작
    }

    public void OnDrag(PointerEventData eventData)
    {
        // UI 드래그 (마우스 따라가기)
        if (rectTransform != null)
        {
            rectTransform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 드래그 종료
    }

    void Update()
    {
        // 스페이스바로 스캔
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryScanNearbyProduct();
        }

        // 가까운 상품 감지해서 색상 변경
        UpdateScannerColor();
    }

    void UpdateScannerColor()
    {
        ProductInteractable nearestProduct = FindNearestProduct();

        if (scannerImage != null)
        {
            if (nearestProduct != null)
            {
                scannerImage.color = scanReadyColor; // 스캔 가능 (중복 허용)
            }
            else
            {
                scannerImage.color = scanNotReadyColor; // 스캔 불가
            }
        }
    }

    ProductInteractable FindNearestProduct()
    {
        ProductInteractable[] allProducts = FindObjectsByType<ProductInteractable>(FindObjectsSortMode.None);
        ProductInteractable nearest = null;
        float minDistance = scanRange;

        foreach (var product in allProducts)
        {
            // UI RectTransform 거리 계산 (스크린 좌표 기준)
            RectTransform productRect = product.GetComponent<RectTransform>();
            if (productRect != null && rectTransform != null)
            {
                // 스크린 좌표로 거리 계산
                float distance = Vector2.Distance(rectTransform.position, productRect.position);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = product;
                }
            }
        }

        return nearest;
    }

    void TryScanNearbyProduct()
    {
        ProductInteractable nearestProduct = FindNearestProduct();

        if (nearestProduct != null)
        {
            ScanProduct(nearestProduct); // 중복 스캔 허용
        }
        else
        {
            // 상품이 없으면 간단한 가격 바코드 확인
            DraggableBarcode nearestBarcode = FindNearestSimpleBarcode();
            if (nearestBarcode != null && nearestBarcode.simplePrice > 0)
            {
                ScanSimpleBarcode(nearestBarcode);
            }
            else
            {
                Debug.Log("[스캐너] 스캔 가능한 상품이 범위 내에 없습니다!");
            }
        }
    }

    DraggableBarcode FindNearestSimpleBarcode()
    {
        DraggableBarcode[] allBarcodes = FindObjectsByType<DraggableBarcode>(FindObjectsSortMode.None);
        DraggableBarcode nearest = null;
        float minDistance = scanRange;

        foreach (var barcode in allBarcodes)
        {
            if (barcode.simplePrice <= 0) continue; // 간단 가격 바코드만

            RectTransform barcodeRect = barcode.GetComponent<RectTransform>();
            if (barcodeRect != null && rectTransform != null)
            {
                float distance = Vector2.Distance(rectTransform.position, barcodeRect.position);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = barcode;
                }
            }
        }

        return nearest;
    }

    void ScanSimpleBarcode(DraggableBarcode barcode)
    {
        // 계산대에 손님이 없으면 스캔 불가
        if (CheckoutCounter.Instance == null || !CheckoutCounter.Instance.isCustomerWaiting)
        {
            Debug.LogWarning("[스캐너] 손님이 없어서 스캔할 수 없습니다!");
            return;
        }

        // 손님 타입 확인 (가짜 바코드는 취객이 아니고 휴대폰 안 볼 때 실수)
        CustomerManager manager = FindFirstObjectByType<CustomerManager>();
        if (manager != null && manager.currentCheckoutCustomer != null)
        {
            Customer customer = manager.currentCheckoutCustomer;

            // 멀쩡한 손님이고 휴대폰도 안 보고 있을 때 가짜 바코드 스캔하면 실수
            if (customer.customerType != Customer.CustomerType.Drunk && !customer.isOnPhone)
            {
                Debug.LogWarning("[경고] 멀쩡한 손님에게 가짜 바코드를 스캔했습니다!");

                if (POSSystem.Instance != null)
                {
                    POSSystem.Instance.AddMistake();
                }
            }
        }

        // 계산대에 간단 가격만 추가
        if (CheckoutCounter.Instance != null)
        {
            CheckoutCounter.Instance.AddSimplePrice(barcode.simplePrice);
        }

        Debug.Log($"[스캐너] 간단 바코드 스캔: {barcode.simplePrice}원");
    }

    void ScanProduct(ProductInteractable product)
    {
        // 계산대에 손님이 없으면 스캔 불가
        if (CheckoutCounter.Instance == null || !CheckoutCounter.Instance.isCustomerWaiting)
        {
            Debug.LogWarning("[스캐너] 손님이 없어서 스캔할 수 없습니다!");
            return;
        }

        int currentPrice = product.GetCurrentPrice();

        // 중복 스캔 체크 (멀쩡한 손님이고 휴대폰 안 볼 때)
        CustomerManager manager = FindFirstObjectByType<CustomerManager>();
        if (manager != null && manager.currentCheckoutCustomer != null)
        {
            Customer customer = manager.currentCheckoutCustomer;

            // 멀쩡한 손님이고 휴대폰도 안 보고 있을 때
            if (customer.customerType != Customer.CustomerType.Drunk && !customer.isOnPhone)
            {
                // 이미 스캔한 상품인지 체크
                if (scannedProducts.Contains(product))
                {
                    Debug.LogWarning("[경고] 이미 스캔한 상품을 다시 스캔했습니다!");

                    if (POSSystem.Instance != null)
                    {
                        POSSystem.Instance.AddMistake();
                    }
                }
            }
        }

        // 스캔 기록 추가
        scannedProducts.Add(product);

        // 계산대에 금액 추가
        if (CheckoutCounter.Instance != null)
        {
            CheckoutCounter.Instance.AddScannedItem(product);
        }

        Debug.Log($"[스캐너] 스캔 완료: {product.productData.productName} - {currentPrice}원");

        // 효과음이나 피드백 추가 가능
    }

    // 새 손님이 올 때 스캔 내역 초기화
    public void ResetScannedProducts()
    {
        scannedProducts.Clear();
        Debug.Log("[스캐너] 스캔 내역 초기화");
    }
}
