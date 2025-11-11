using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 제품에 붙이는 스크립트. CCTV가 감시하지 않을 때만 바코드를 조작할 수 있습니다.
/// 클릭하면 상세 패널이 열리고 드래그&드롭으로 바코드를 교체할 수 있습니다.
/// </summary>
public class ProductInteractable : MonoBehaviour
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

        // 버튼 클릭 이벤트 연결 (진열대 상품만, 복제본은 Button 없음)
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnProductClicked);
            Debug.Log($"[{productData.productName}] 버튼 이벤트 연결됨");
        }
        // else는 제거 - 복제본은 Button이 없는 게 정상
    }

    void OnMouseDown()
    {
        // UI 버튼으로 변경되어 이 메서드는 사용 안함
    }

    public void OnProductClicked()
    {
        Debug.Log($"[{productData.productName}] 버튼 클릭됨!");

        // CCTV 상관없이 항상 패널 열림 (빨간불일 때는 위험하지만 가능)
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
            Debug.LogError("ProductDetailPanel.Instance가 null입니다! 패널이 생성되지 않았거나 비활성화되어 있습니다.");
            return;
        }

        ProductDetailPanel.Instance.ShowProduct(this);
        Debug.Log($"[{productData.productName}] 제품 상세 패널 열림");
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
