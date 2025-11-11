using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 바코드를 드롭할 수 있는 영역 (제품 상세 패널에 있음)
/// </summary>
public class BarcodeDropZone : MonoBehaviour, IDropHandler
{
    public TextMeshProUGUI feedbackText;  // 드롭 결과를 보여주는 텍스트
    private ProductDetailPanel detailPanel;

    void Start()
    {
        detailPanel = GetComponentInParent<ProductDetailPanel>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        DraggableBarcode barcode = eventData.pointerDrag?.GetComponent<DraggableBarcode>();
        if (barcode != null)
        {
            OnBarcodeDropped(barcode.barcodeData);
        }
    }

    public void OnBarcodeDropped(BarcodeData barcodeData)
    {
        if (detailPanel != null)
        {
            detailPanel.ApplyBarcode(barcodeData);
        }

        if (feedbackText != null)
        {
            feedbackText.text = $"바코드 교체 완료!";
            Invoke(nameof(ClearFeedback), 1.5f);
        }
    }

    void ClearFeedback()
    {
        if (feedbackText != null)
            feedbackText.text = "";
    }
}
