using UnityEngine;

/// <summary>
/// 바코드 데이터를 담는 클래스
/// </summary>
[System.Serializable]
public class BarcodeData
{
    public string barcodeID;      // 바코드 고유 ID (예: "BC001")
    public int price;              // 가격
    public string displayName;     // 표시 이름 (예: "1000원")

    public BarcodeData(string id, int price)
    {
        this.barcodeID = id;
        this.price = price;
        this.displayName = $"{price}원";
    }
}
