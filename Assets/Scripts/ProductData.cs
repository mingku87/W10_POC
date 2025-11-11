using UnityEngine;

/// <summary>
/// 제품 정보를 담는 ScriptableObject 또는 클래스
/// </summary>
[System.Serializable]
public class ProductData
{
    public string productName;     // 제품 이름
    public int originalPrice;      // 원래 가격
    public Sprite productSprite;   // 제품 이미지 (옵션)
    
    public ProductData(string name, int price)
    {
        this.productName = name;
        this.originalPrice = price;
    }
}
