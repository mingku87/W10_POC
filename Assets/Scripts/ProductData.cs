using UnityEngine;

/// <summary>
/// 제품 정보를 담는 데이터 클래스
/// Unity 6.0 최신 버전 호환
/// </summary>
[System.Serializable]
public class ProductData
{
    [Header("기본 정보")]
    public string productName;          // 제품 이름
    public int originalPrice;           // 원래 가격
    public Sprite productSprite;        // 제품 이미지 (옵션)

    [Header("제품 분류")]
    public ProductType productType = ProductType.None;
    public BrandGrade currentBrand = BrandGrade.Low;  // 기본값: 하급

    [Header("가짜 제품 정보")]
    public bool isFake = false;         // 가짜 제품 여부
    public BrandGrade originalBrand = BrandGrade.Low;  // 원래 브랜드 (가짜인 경우에만 의미 있음)

    // 생성자들
    public ProductData(string name, int price)
    {
        this.productName = name;
        this.originalPrice = price;
        this.productType = ProductType.None;
        this.currentBrand = BrandGrade.Low;  // 기본값: 하급
        this.isFake = false;
    }

    public ProductData(string name, int price, ProductType type, BrandGrade brand)
    {
        this.productName = name;
        this.originalPrice = price;
        this.productType = type;
        this.currentBrand = brand;
        this.originalBrand = brand;
        this.isFake = false;
    }

    /// <summary>
    /// 하급 브랜드를 상급(가짜)으로 변환 - BrandChangeZone에서 호출
    /// </summary>
    public bool TryConvertToFakeHigh()
    {
        // 통조림만 변경 가능
        if (productType != ProductType.CannedPork)
        {
            Debug.LogWarning($"[브랜드 변경 실패] {productName}은(는) 통조림이 아닙니다!");
            return false;
        }

        // 이미 가짜면 변경 불가
        if (isFake)
        {
            Debug.LogWarning($"[브랜드 변경 실패] {productName}은(는) 이미 가짜 브랜드입니다!");
            return false;
        }

        // 이미 상급이면 변경 불가
        if (currentBrand == BrandGrade.High)
        {
            Debug.LogWarning($"[브랜드 변경 실패] {productName}은(는) 이미 상급 브랜드입니다!");
            return false;
        }

        // 하급 → 상급(가짜)으로 변경
        originalBrand = currentBrand;  // Low 저장
        currentBrand = BrandGrade.High;
        isFake = true;

        Debug.Log($"[브랜드 변경 성공] {productName}: 하급 → 상급(가짜) 변환 완료!");
        return true;
    }

    /// <summary>
    /// 진짜 제품으로 복원
    /// </summary>
    public void RestoreToOriginal()
    {
        currentBrand = originalBrand;
        isFake = false;
    }

    /// <summary>
    /// 브랜드 등급에 따른 가격 배율 반환
    /// </summary>
    public float GetBrandPriceMultiplier()
    {
        return currentBrand switch
        {
            BrandGrade.High => 1.5f,    // 상급: +50%
            BrandGrade.Low => 1.0f,     // 하급: 기본가
            _ => 1.0f
        };
    }

    /// <summary>
    /// 현재 브랜드 등급을 고려한 가격 반환
    /// </summary>
    public int GetAdjustedPrice()
    {
        return Mathf.RoundToInt(originalPrice * GetBrandPriceMultiplier());
    }

    /// <summary>
    /// 제품 정보를 디버그 문자열로 반환
    /// </summary>
    public override string ToString()
    {
        string fakeStatus = isFake ? $" [가짜: {originalBrand}→{currentBrand}]" : "";
        return $"{productName} ({productType}) - {currentBrand}등급 - {originalPrice}원{fakeStatus}";
    }
}

/// <summary>
/// 제품 타입 분류
/// </summary>
public enum ProductType
{
    None,           // 미분류
    CannedPork,     // 통조림 (돼지고기)
    Pantry,         // 식료품 저장실 품목
    PackedLunch     // 도시락
}

/// <summary>
/// 브랜드 등급 (상/하 2단계)
/// </summary>
public enum BrandGrade
{
    Low,      // 하급 브랜드 (저가형)
    High      // 상급 브랜드 (고가형)
}

/// <summary>
/// 브랜드 등급 확장 메서드
/// </summary>
public static class BrandGradeExtensions
{
    /// <summary>
    /// 한글 이름 반환
    /// </summary>
    public static string ToKoreanName(this BrandGrade grade)
    {
        return grade switch
        {
            BrandGrade.High => "상급",
            BrandGrade.Low => "하급",
            _ => "알 수 없음"
        };
    }

    /// <summary>
    /// 색상 코드 반환 (UI 표시용)
    /// </summary>
    public static Color ToColor(this BrandGrade grade)
    {
        return grade switch
        {
            BrandGrade.High => new Color(1f, 0.84f, 0f),      // 금색
            BrandGrade.Low => new Color(0.8f, 0.5f, 0.2f),    // 구리색
            _ => Color.white
        };
    }
}