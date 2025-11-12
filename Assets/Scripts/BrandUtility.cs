using UnityEngine;

/// <summary>
/// 브랜드 관련 유틸리티 클래스
/// (FakeProductManager 대체 - 간소화)
/// </summary>
public static class BrandUtility
{
    /// <summary>
    /// 손님이 가짜 제품을 발견할 확률 계산
    /// </summary>
    public static bool CustomerDetectsFake(ProductData product, Customer.CustomerType customerType)
    {
        if (!product.isFake) return false;

        float detectionChance = customerType switch
        {
            Customer.CustomerType.Normal => 0.7f,   // 일반 손님: 70% 발견
            Customer.CustomerType.OnPhone => 0.2f,  // 폰보는 손님: 20% 발견
            Customer.CustomerType.Drunk => 0.1f,    // 취한 손님: 10% 발견
            _ => 0.5f
        };

        bool detected = Random.value < detectionChance;

        if (detected)
        {
            Debug.Log($"[손님] 가짜 브랜드 발견! {product.productName} (발견 확률: {detectionChance:P0})");
        }

        return detected;
    }

    /// <summary>
    /// 가짜 제품 이익 계산
    /// </summary>
    public static int CalculateFakeProfit(ProductData product)
    {
        if (!product.isFake) return 0;

        // 하급 원가로 사서 상급 가격으로 판매
        int lowGradePrice = product.originalPrice; // 하급 원가
        int highGradePrice = Mathf.RoundToInt(product.originalPrice * 1.5f); // 상급 가격

        return highGradePrice - lowGradePrice;
    }
}