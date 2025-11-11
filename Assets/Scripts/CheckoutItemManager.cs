using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 계산대 상품 데이터 관리 - 스캔된 상품 리스트, 총액 계산
/// </summary>
public class CheckoutItemManager : MonoBehaviour
{
    private List<ProductInteractable> scannedItems = new List<ProductInteractable>();
    private int totalAmount = 0;

    public void AddItem(ProductInteractable product)
    {
        scannedItems.Add(product);
        totalAmount += product.GetCurrentPrice();

        Debug.Log($"[ItemManager] 상품 추가: {product.productData.productName} ({product.GetCurrentPrice()}원) - 총액: {totalAmount}원");
    }

    public void AddSimplePrice(int price)
    {
        totalAmount += price;
        Debug.Log($"[ItemManager] 간단 가격 추가: {price}원 - 총액: {totalAmount}원");
    }

    public void ClearAllItems()
    {
        scannedItems.Clear();
        totalAmount = 0;
        Debug.Log("[ItemManager] 모든 상품 데이터 초기화");
    }

    public int GetTotalAmount()
    {
        return totalAmount;
    }

    public int GetScannedItemCount()
    {
        return scannedItems.Count;
    }

    public List<ProductInteractable> GetScannedItems()
    {
        return new List<ProductInteractable>(scannedItems); // 복사본 반환
    }

    public int GetTotalOriginalPrice()
    {
        int total = 0;
        foreach (var product in scannedItems)
        {
            total += product.productData.originalPrice;
        }
        return total;
    }
}