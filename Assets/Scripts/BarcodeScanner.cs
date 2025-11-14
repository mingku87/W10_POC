using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 스캔 존 - 상품을 여기에 Drop하면 스캔 처리
/// </summary>
public class BarcodeScanner : MonoBehaviour, IDropHandler
{
    public static BarcodeScanner Instance { get; private set; }

    [Header("스캔 설정")]
    public Color scanReadyColor = Color.green; // 기본 색
    public Color scanningColor = Color.yellow; // 스캔 중

    private Image scannerImage; // UI Image
    private RectTransform rectTransform;
    private HashSet<ProductInteractable> scannedProducts = new HashSet<ProductInteractable>();
    private HashSet<DraggableProduct> scannedProductInstances = new HashSet<DraggableProduct>(); // 실제 드래그 인스턴스 추적

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

        if (scannerImage != null)
        {
            scannerImage.color = scanReadyColor;
        }

        // Debug.Log("[스캔 존] 초기화 완료 - 상품을 여기로 드래그하여 스캔하세요!");
    }

    // IDropHandler 구현 - DraggableProduct에서 자체 처리하므로 여기선 비워둠
    public void OnDrop(PointerEventData eventData)
    {
        // DraggableProduct의 OnEndDrag에서 처리됨
    }

    /// <summary>
    /// 스캔 완료된 상품인지 확인
    /// </summary>
    public bool IsProductScanned(ProductInteractable product)
    {
        return scannedProducts.Contains(product);
    }

    /// <summary>
    /// 특정 DraggableProduct 인스턴스가 이미 스캔되었는지 확인
    /// </summary>
    public bool IsProductInstanceScanned(DraggableProduct productInstance)
    {
        return scannedProductInstances.Contains(productInstance);
    }

    /// <summary>
    /// 스캔 기록 추가
    /// </summary>
    public void AddScannedProduct(ProductInteractable product)
    {
        scannedProducts.Add(product);
    }

    /// <summary>
    /// DraggableProduct 인스턴스 스캔 기록 추가
    /// </summary>
    public void AddScannedProductInstance(DraggableProduct productInstance)
    {
        scannedProductInstances.Add(productInstance);
    }

    /// <summary>
    /// DraggableProduct 인스턴스 스캔 기록 제거
    /// </summary>
    public void RemoveScannedProductInstance(DraggableProduct productInstance)
    {
        scannedProductInstances.Remove(productInstance);
        Debug.Log($"[스캔 존] 상품 인스턴스 기록 제거됨");
    }

    /// <summary>
    /// 새 손님이 올 때 스캔 내역 초기화
    /// </summary>
    public void ResetScannedProducts()
    {
        scannedProducts.Clear();
        scannedProductInstances.Clear(); // 인스턴스 추적도 초기화

        // 스캔 존에 남아있는 모든 복사본 삭제
        foreach (Transform child in transform)
        {
            DraggableProduct product = child.GetComponent<DraggableProduct>();
            if (product != null && product.isClone)
            {
                Destroy(child.gameObject);
            }
        }

        // Debug.Log("[스캔 존] 스캔 내역 및 상품 초기화");
    }

    /// <summary>
    /// 스캔 효과
    /// </summary>
    public void FlashScanEffect()
    {
        if (scannerImage != null)
        {
            scannerImage.color = scanningColor;
            Invoke(nameof(ResetScannerColor), 0.3f);
        }
    }

    void ResetScannerColor()
    {
        if (scannerImage != null)
        {
            scannerImage.color = scanReadyColor;
        }
    }
}