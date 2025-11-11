using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 드래그 가능한 바코드 UI 아이템
/// 인벤토리에서 제품으로 드래그하여 바코드를 교체합니다
/// </summary>
public class DraggableBarcode : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public BarcodeData barcodeData;
    public TextMeshProUGUI priceText;  // 바코드에 표시될 가격 텍스트
    public int simplePrice = 0; // 간단한 가격 바코드용 (3000원, 5000원 등)

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Transform originalParent;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvas = GetComponentInParent<Canvas>();
    }

    public void Initialize(BarcodeData data)
    {
        barcodeData = data;
        if (priceText != null)
            priceText.text = data.displayName;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;

        // 드래그 중에는 반투명하게
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        // 최상위로 이동 (다른 UI 위에 표시)
        transform.SetParent(canvas.transform);
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // 드롭 대상이 있는지 확인
        if (eventData.pointerEnter != null)
        {
            BarcodeDropZone dropZone = eventData.pointerEnter.GetComponent<BarcodeDropZone>();
            if (dropZone != null)
            {
                // 바코드 교체 성공
                dropZone.OnBarcodeDropped(this.barcodeData);

                // 원래 위치로 복귀
                transform.SetParent(originalParent);
                rectTransform.anchoredPosition = originalPosition;
                return;
            }
        }

        // 드롭 실패 - 원래 위치로 복귀
        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = originalPosition;
    }
}
