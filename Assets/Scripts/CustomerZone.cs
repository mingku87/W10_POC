using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 손님 존 - 스캔된 상품이 배치되는 영역
/// C키 결제 완료 시 모든 상품이 삭제됨
/// </summary>
public class CustomerZone : MonoBehaviour, IDropHandler
{
    public static CustomerZone Instance { get; private set; }

    [Header("배치 설정")]
    [Tooltip("자유 배치 vs Grid 정렬")]
    public bool useFreePositioning = true; // true: 자유 배치, false: Grid 정렬

    [Header("UI 표시")]
    public TextMeshProUGUI itemCountText; // "배치된 상품: 3개"
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
    public Color highlightColor = new Color(0.3f, 0.5f, 0.3f, 0.5f);

    [Header("배치 제한 (선택)")]
    public bool enableValidation = false; // 검증 활성화
    public int maxItems = 20; // 최대 배치 가능 개수

    private Image backgroundImage;
    private List<DraggableProduct> placedProducts = new List<DraggableProduct>();
    private GridLayoutGroup gridLayout; // Grid 모드용

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
        backgroundImage = GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = gameObject.AddComponent<Image>();
            backgroundImage.color = normalColor;
        }

        // Grid 모드면 GridLayoutGroup 추가
        if (!useFreePositioning)
        {
            gridLayout = GetComponent<GridLayoutGroup>();
            if (gridLayout == null)
            {
                gridLayout = gameObject.AddComponent<GridLayoutGroup>();
                gridLayout.cellSize = new Vector2(80, 80);
                gridLayout.spacing = new Vector2(10, 10);
                gridLayout.childAlignment = TextAnchor.UpperLeft;
            }
        }

        UpdateItemCountUI();

        Debug.Log($"[손님 존] 초기화 완료 - 배치 모드: {(useFreePositioning ? "자유 배치" : "Grid 정렬")}");
    }

    // IDropHandler 구현
    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject == null) return;

        DraggableProduct product = droppedObject.GetComponent<DraggableProduct>();
        if (product == null || !product.isClone) return;

        // 스캔 완료된 상품만 배치 가능 (한번이라도 스캔되었으면 OK)
        if (!product.hasBeenScanned)
        {
            Debug.LogWarning("[손님 존] 스캔되지 않은 상품은 배치할 수 없습니다!");
            return;
        }

        // 최대 개수 체크
        if (placedProducts.Count >= maxItems)
        {
            Debug.LogWarning($"[손님 존] 최대 {maxItems}개까지만 배치 가능합니다!");
            return;
        }

        // 검증 활성화 시 손님이 원하는 상품인지 체크
        if (enableValidation && !IsWantedProduct(product))
        {
            Debug.LogWarning("[손님 존] 손님이 원하지 않는 상품입니다!");
            return;
        }

        // 상품 배치
        PlaceProduct(product, eventData);
    }

    /// <summary>
    /// 상품을 손님 존에 배치
    /// </summary>
    void PlaceProduct(DraggableProduct product, PointerEventData eventData)
    {
        RectTransform productRect = product.GetComponent<RectTransform>();

        // 현재 월드 위치 저장 (드롭한 위치)
        Vector3 worldPosition = productRect.position;

        // 부모 설정
        product.transform.SetParent(transform);

        if (useFreePositioning)
        {
            // 자유 배치 - 드롭한 위치 그대로 유지
            // 부모가 바뀌었으므로 월드 위치를 로컬 위치로 변환
            productRect.position = worldPosition;
        }
        else
        {
            // Grid 정렬 - GridLayoutGroup이 자동 배치
            productRect.anchoredPosition = Vector2.zero;
        }

        // 리스트에 추가
        if (!placedProducts.Contains(product)) // [수정] 중복 추가 방지
        {
            placedProducts.Add(product);
        }

        // UI 업데이트
        UpdateItemCountUI();

        product.UpdateLastValidPlacement(transform, this);

        Debug.Log($"[손님 존] {product.productInteractable.productData.productName} 배치 완료! (총 {placedProducts.Count}개)");

        // 배치 효과
        FlashBackground();
    }

    /// <summary>
    /// 손님이 원하는 상품인지 확인 (검증 모드)
    /// </summary>
    bool IsWantedProduct(DraggableProduct product)
    {
        CustomerManager manager = FindFirstObjectByType<CustomerManager>();
        if (manager == null || manager.currentCheckoutCustomer == null)
            return true; // 손님 없으면 검증 안함

        Customer customer = manager.currentCheckoutCustomer;
        foreach (var wantedProduct in customer.selectedProducts)
        {
            if (wantedProduct.productData.productName == product.productInteractable.productData.productName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 배치된 모든 상품 삭제 (결제 완료 시 호출)
    /// </summary>
    public void ClearAllProducts()
    {
        foreach (var product in placedProducts)
        {
            if (product != null)
            {
                Destroy(product.gameObject);
            }
        }

        placedProducts.Clear();
        UpdateItemCountUI();

        Debug.Log("[손님 존] 모든 상품 삭제 완료!");
    }

    /// <summary>
    /// 손님 존에서 특정 상품을 제거합니다. (DraggableProduct가 호출)
    /// </summary>
    public void RemoveProduct(DraggableProduct product)
    {
        if (product != null && placedProducts.Contains(product))
        {
            placedProducts.Remove(product);
            UpdateItemCountUI();
            Debug.Log($"[손님 존] {product.productInteractable.productData.productName} 제거됨. (총 {placedProducts.Count}개)");
        }
    }



    /// <summary>
    /// 배치된 상품 개수 UI 업데이트
    /// </summary>
    void UpdateItemCountUI()
    {
        if (itemCountText != null)
        {
            itemCountText.text = $"배치된 상품: {placedProducts.Count}개";
        }
    }

    /// <summary>
    /// 배경 하이라이트 효과
    /// </summary>
    void FlashBackground()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = highlightColor;
            Invoke(nameof(ResetBackgroundColor), 0.3f);
        }
    }

    void ResetBackgroundColor()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
    }

    /// <summary>
    /// 배치된 상품 개수 반환
    /// </summary>
    public int GetPlacedItemCount()
    {
        return placedProducts.Count;
    }

    /// <summary>
    /// 특정 상품이 배치되었는지 확인
    /// </summary>
    public bool HasProduct(string productName)
    {
        foreach (var product in placedProducts)
        {
            if (product != null &&
                product.productInteractable.productData.productName == productName)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 배치된 상품 목록 반환
    /// </summary>
    public List<DraggableProduct> GetPlacedProducts()
    {
        return new List<DraggableProduct>(placedProducts);
    }

    // 마우스가 손님 존 위에 있을 때 하이라이트
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