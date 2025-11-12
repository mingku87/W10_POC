using UnityEngine;

/// <summary>
/// 브랜드 커버 정보를 담는 데이터 클래스
/// Unity 6.0 최신 버전 호환
/// </summary>
[System.Serializable]
public class BrandData
{
    [Header("브랜드 정보")]
    public ProductType targetProductType;  // 적용 대상 제품 타입
    public Sprite brandCoverSprite;        // 가짜 브랜드 커버 이미지
    public BrandGrade targetBrand = BrandGrade.High;  // 변경할 브랜드 등급 (기본: 상급)

    [Header("설명")]
    [TextArea(2, 4)]
    public string description;  // 브랜드 설명 (에디터용)

    /// <summary>
    /// 브랜드 데이터 생성자
    /// </summary>
    public BrandData(ProductType productType, Sprite sprite)
    {
        this.targetProductType = productType;
        this.brandCoverSprite = sprite;
        this.targetBrand = BrandGrade.High;
    }

    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    public override string ToString()
    {
        return $"BrandData: {targetProductType} → {targetBrand}등급";
    }
}