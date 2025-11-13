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

    public void RemoveItem(ProductInteractable product)
    {
        // 리스트에서 첫 번째로 일치하는 상품만 제거
        if (scannedItems.Remove(product))
        {
            totalAmount -= product.GetCurrentPrice();
            Debug.Log($"[ItemManager] 상품 제거: {product.productData.productName} ({product.GetCurrentPrice()}원) - 총액: {totalAmount}원");
        }
        else
        {
            Debug.LogWarning($"[ItemManager] 제거 실패: {product.productData.productName}을 찾을 수 없습니다.");
        }
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
            // BarcodeData에서 실제 원가 가져오기
            int originalPrice = product.GetRealCost();
            total += originalPrice;
        }
        return total;
    }

    /// <summary>
    /// 가짜 라벨로 인한 이익 계산
    /// (가짜 상품의 스캔 가격 - 원래 가격)
    /// </summary>
    public int GetFakeLabelProfit()
    {
        int profit = 0;
        foreach (var product in scannedItems)
        {
            // 가짜 상품만 계산
            if (product.productData.isFake)
            {
                // 가짜로 받은 가격 (상급 가격)
                int fakePrice = product.GetCurrentPrice();

                // 실제 원가 (하급 가격)
                int realCost = product.GetRealCost();

                // 차액
                int itemProfit = fakePrice - realCost;
                profit += itemProfit;

                Debug.Log($"[가짜 라벨 이익] {product.productData.productName}: {fakePrice}원 - {realCost}원 = {itemProfit}원");
            }
        }
        return profit;
    }
}