using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 제품에 붙이는 스크립트. 
/// - 드래그: 스캐너로 가져가서 스캔
/// - 우클릭: 바코드 교체 패널 열기
/// </summary>
public class ProductInteractable : MonoBehaviour, IPointerClickHandler
{
    [Header("제품 정보")]
    public ProductData productData = new ProductData("과자", 1000);

    [Header("UI 텍스트")]
    public TextMeshProUGUI nameText;   // 제품 이름 표시
    public TextMeshProUGUI priceText;  // 현재 가격 표시

    private BarcodeData currentBarcode;

    void Start()
    {
        // 기본 바코드 설정 (원래 가격)
        currentBarcode = new BarcodeData("ORIGINAL", productData.originalPrice);
        UpdateUI();

        Debug.Log($"[{productData.productName}] 초기화 완료 - 드래그: 스캔, 우클릭: 바코드 교체");
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

        // CCTV 경고
        if (CCTVController.IsWatching)
        {
            Debug.LogWarning($"⚠️ 위험! CCTV가 감시 중입니다! 바코드 교체 시 걸릴 수 있습니다!");
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

    void UpdateUI()
    {
        if (nameText != null)
            nameText.text = productData.productName;

        if (priceText != null)
            priceText.text = $"{currentBarcode.price}원";
    }

    // Inspector에서 값 변경 시 UI 업데이트
    void OnValidate()
    {
        if (Application.isPlaying && currentBarcode != null)
            UpdateUI();
    }
}