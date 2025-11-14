using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 제품 클릭 시 화면 중앙에 표시되는 상세 패널
/// 바코드를 드래그&드롭하여 교체할 수 있습니다
/// </summary>
public class ProductDetailPanel : MonoBehaviour
{
    public static ProductDetailPanel Instance { get; private set; }

    [Header("UI 요소")]
    public GameObject panelObject;       // 전체 패널
    public Image productImage;           // 제품 이미지
    public TextMeshProUGUI productNameText;         // 제품 이름
    public TextMeshProUGUI originalPriceText;       // 원래 가격 표시
    public TextMeshProUGUI currentPriceText;        // 현재 가격 표시
    public Button closeButton;           // 닫기 버튼
    public BarcodeDropZone dropZone;     // 바코드 드롭 영역
    public GameObject barcodeInventoryPanel; // 바코드 인벤토리 패널

    private ProductInteractable currentProduct;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Debug.Log("ProductDetailPanel Instance 생성됨");
        }
        else
        {
            Debug.LogWarning("ProductDetailPanel Instance 중복! 기존 것 유지");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        // Debug.Log("ProductDetailPanel 초기화 완료");
    }

    public void ShowProduct(ProductInteractable product)
    {
        Debug.Log($"ShowProduct 호출됨: {product.productData.productName}");

        currentProduct = product;

        if (panelObject != null)
        {
            panelObject.SetActive(true);
            Debug.Log("패널 활성화됨");
        }
        else
        {
            Debug.LogError("panelObject가 null입니다!");
        }

        // 바코드 인벤토리도 함께 표시
        if (barcodeInventoryPanel != null)
        {
            barcodeInventoryPanel.SetActive(true);
            Debug.Log("바코드 인벤토리 활성화됨");
        }        // 제품 정보 표시
        if (productNameText != null)
            productNameText.text = product.productData.productName;

        // 원래 가격 표시
        if (originalPriceText != null)
            originalPriceText.text = $"원래 가격: {product.productData.originalPrice}원";

        if (currentPriceText != null)
            currentPriceText.text = $"현재 가격: {product.GetCurrentPrice()}원";

        if (productImage != null && product.productData.productSprite != null)
            productImage.sprite = product.productData.productSprite;
    }

    public void ApplyBarcode(BarcodeData barcodeData)
    {
        if (currentProduct != null)
        {
            // CCTV가 빨간색(감시 중)일 때 바코드 교체하면 실수
            if (CCTVController.IsWatching)
            {
                Debug.LogWarning("[경고] CCTV가 지켜보고 있을 때 바코드를 교체했습니다!");

                if (MistakeManager.Instance != null)
                {
                    MistakeManager.Instance.AddMistake(
                        MistakeManager.MistakeType.BarcodeChangeCCTVDetected,
                        "CCTV 감시 중 바코드 교체"
                    );
                }
            }

            currentProduct.SetBarcode(barcodeData);

            // 가격 텍스트 업데이트
            if (currentPriceText != null)
                currentPriceText.text = $"현재 가격: {currentProduct.GetCurrentPrice()}원";

            Debug.Log($"바코드 교체 완료: {barcodeData.displayName}");
        }
    }

    public void ClosePanel()
    {
        HidePanel();

        // 바코드 인벤토리도 함께 숨김
        if (barcodeInventoryPanel != null)
            barcodeInventoryPanel.SetActive(false);

        currentProduct = null;
    }

    void HidePanel()
    {
        if (panelObject != null)
            panelObject.SetActive(false);
    }

    void Update()
    {
        // ESC 키로 패널 닫기
        if (Input.GetKeyDown(KeyCode.Escape) && panelObject != null && panelObject.activeSelf)
        {
            ClosePanel();
        }
    }
}
