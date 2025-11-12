using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 제품에 붙이는 스크립트. 
/// - 드래그: 스캐너로 가져가서 스캔
/// - 우클릭: 바코드 교체 패널 열기
/// - 브랜드 변경 존에서 가짜 상품 생성 가능
/// </summary>
public class ProductInteractable : MonoBehaviour, IPointerClickHandler
{
    [Header("제품 정보")]
    public ProductData productData = new ProductData("과자", 1000);

    [Header("UI 텍스트")]
    public TextMeshProUGUI nameText;   // 제품 이름 표시
    public TextMeshProUGUI priceText;  // 현재 가격 표시

    [Header("시각 효과")]
    public Image productImage;         // 제품 이미지

    private BarcodeData currentBarcode;

    void Start()
    {
        InitializeAsNewProduct();
    }

    /// <summary>
    /// 새로운 상품으로 초기화 (BrandChangeZone에서 생성 시에도 호출)
    /// </summary>
    public void InitializeAsNewProduct()
    {
        // 브랜드 등급을 고려한 가격 설정
        int initialPrice = productData.GetAdjustedPrice();
        currentBarcode = new BarcodeData("ORIGINAL", initialPrice);

        UpdateUI();

        // 가짜 상품이면 시각 효과 추가
        if (productData.isFake)
        {
            ApplyFakeVisualEffect();
        }

        Debug.Log($"[{productData.productName}] 초기화 완료 - 브랜드: {productData.currentBrand.ToKoreanName()}, 가격: {initialPrice}원, 가짜: {productData.isFake}");
    }

    /// <summary>
    /// 가짜 상품 시각 효과 적용
    /// </summary>
    void ApplyFakeVisualEffect()
    {
        if (productImage == null)
        {
            productImage = GetComponent<Image>();
        }

        if (productImage != null)
        {
            // 금색 테두리 효과
            productImage.color = new Color(1f, 0.9f, 0.6f);
        }
    }

    // IPointerClickHandler 구현 - 우클릭 감지
    public void OnPointerClick(PointerEventData eventData)
    {
        // 우클릭인 경우에만 바코드 교체 패널 열기
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightClick();
        }
    }

    void OnRightClick()
    {
        Debug.Log($"[{productData.productName}] 우클릭 - 바코드 교체 패널 열기");

        // CCTV 경고 (CCTVController가 있는 경우)
        if (FindFirstObjectByType<CCTVController>() != null)
        {
            if (CCTVController.IsWatching)
            {
                Debug.LogWarning($"⚠️ 위험! CCTV가 감시 중입니다! 바코드 교체 시 걸릴 수 있습니다!");
            }
        }

        // 상세 패널 열기
        OpenDetailPanel();
    }

    private void OpenDetailPanel()
    {
        if (ProductDetailPanel.Instance == null)
        {
            Debug.LogError("ProductDetailPanel.Instance가 null입니다!");
            return;
        }

        ProductDetailPanel.Instance.ShowProduct(this);
        Debug.Log($"[{productData.productName}] 바코드 교체 패널 열림");
    }

    public void SetBarcode(BarcodeData newBarcode)
    {
        currentBarcode = newBarcode;
        UpdateUI();
        Debug.Log($"[{productData.productName}] 바코드 교체: {productData.originalPrice}원 → {newBarcode.price}원");
    }

    public int GetCurrentPrice()
    {
        return currentBarcode.price;
    }

    public BarcodeData GetCurrentBarcode()
    {
        return currentBarcode;
    }

    /// <summary>
    /// 브랜드 변경 시 호출 - 가격 업데이트
    /// (현재는 InitializeAsNewProduct에서 처리하므로 사용 안함)
    /// </summary>
    public void UpdateBrandUI()
    {
        // 브랜드 등급에 따른 새로운 가격 계산
        int newPrice = productData.GetAdjustedPrice();

        // 바코드 가격 업데이트
        currentBarcode = new BarcodeData("ORIGINAL", newPrice);

        // UI 업데이트
        UpdateUI();

        // 시각적 피드백 (가짜 상품은 색상 변경)
        if (productData.isFake)
        {
            ApplyFakeVisualEffect();
        }

        Debug.Log($"[{productData.productName}] 브랜드 UI 업데이트 완료 - 새 가격: {newPrice}원");
    }

    void UpdateUI()
    {
        if (nameText != null)
            nameText.text = productData.productName;

        if (priceText != null)
        {
            priceText.text = $"{currentBarcode.price}원";

            // 가짜 상품이면 가격에 특수 표시
            if (productData.isFake)
            {
                priceText.text += " ★"; // 별 표시로 힌트
                priceText.color = productData.currentBrand.ToColor(); // 금색
            }
            else
            {
                priceText.color = Color.white; // 일반 색상
            }
        }
    }

    // Inspector에서 값 변경 시 UI 업데이트
    void OnValidate()
    {
        if (Application.isPlaying && currentBarcode != null)
            UpdateUI();
    }
}