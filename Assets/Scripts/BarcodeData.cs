using UnityEngine;

/// <summary>
/// 바코드 데이터를 담는 클래스
/// </summary>
[System.Serializable]
public class BarcodeData
{
    public string barcodeID;      // 바코드 고유 ID (예: "BC001")
    public int price;              // 판매가
    public int realCost;           // 실제 원가
    public string displayName;     // 표시 이름 (예: "1000원")

    // 기본 생성자 (판매가 = 원가)
    public BarcodeData(string id, int price)
    {
        this.barcodeID = id;
        this.price = price;
        this.realCost = price;  // 기본적으로 원가 = 판매가
        this.displayName = $"{price}원";
    }

    // 원가와 판매가가 다른 경우 (가짜 제품용)
    public BarcodeData(string id, int price, int realCost)
    {
        this.barcodeID = id;
        this.price = price;
        this.realCost = realCost;
        this.displayName = $"{price}원";
    }
}
