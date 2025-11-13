using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 제품 데이터를 관리하는 싱글톤 매니저
/// Unity 6.0 최신 버전 호환
/// </summary>
public class ProductDataManager : MonoBehaviour
{
    private static ProductDataManager _instance;
    public static ProductDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ProductDataManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("ProductDataManager");
                    _instance = go.AddComponent<ProductDataManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("제품 데이터 목록")]
    [SerializeField] private List<ProductData> productList = new List<ProductData>();

    [Header("브랜드 커버 목록")]
    [SerializeField] private List<BrandData> brandList = new List<BrandData>();

    [Header("UI 프리팹")]
    [SerializeField] private GameObject productButtonPrefab; // 제품 버튼 프리팹

    private Dictionary<string, ProductData> productDict;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeProductDictionary();
    }

    /// <summary>
    /// 제품 딕셔너리 초기화
    /// </summary>
    private void InitializeProductDictionary()
    {
        productDict = new Dictionary<string, ProductData>();
        foreach (var product in productList)
        {
            if (!string.IsNullOrEmpty(product.productName) && !productDict.ContainsKey(product.productName))
            {
                productDict.Add(product.productName, product);
            }
            else
            {
                Debug.LogWarning($"[ProductDataManager] 중복된 제품명 또는 빈 이름: {product.productName}");
            }
        }

        Debug.Log($"[ProductDataManager] 초기화 완료 - 총 {productDict.Count}개 제품 등록");
    }

    /// <summary>
    /// 제품명으로 데이터 가져오기
    /// </summary>
    public ProductData GetProductData(string productName)
    {
        if (productDict.TryGetValue(productName, out ProductData data))
        {
            return data;
        }

        Debug.LogWarning($"[ProductDataManager] 제품을 찾을 수 없음: {productName}");
        return null;
    }

    /// <summary>
    /// 제품 존재 여부 확인
    /// </summary>
    public bool HasProduct(string productName)
    {
        return productDict.ContainsKey(productName);
    }

    /// <summary>
    /// 런타임에 제품 추가
    /// </summary>
    public void AddProduct(ProductData product)
    {
        if (string.IsNullOrEmpty(product.productName))
        {
            Debug.LogError("[ProductDataManager] 제품명이 비어있습니다!");
            return;
        }

        if (!productDict.ContainsKey(product.productName))
        {
            productList.Add(product);
            productDict.Add(product.productName, product);
            Debug.Log($"[ProductDataManager] 제품 추가: {product.productName}");
        }
        else
        {
            Debug.LogWarning($"[ProductDataManager] 이미 존재하는 제품: {product.productName}");
        }
    }

    /// <summary>
    /// 모든 제품 목록 가져오기 (복사본 반환)
    /// </summary>
    public List<ProductData> GetAllProducts()
    {
        return new List<ProductData>(productList);
    }

    /// <summary>
    /// 특정 타입의 제품만 가져오기
    /// </summary>
    public List<ProductData> GetProductsByType(ProductType type)
    {
        List<ProductData> filtered = new List<ProductData>();
        foreach (var product in productList)
        {
            if (product.productType == type)
            {
                filtered.Add(product);
            }
        }
        return filtered;
    }

    /// <summary>
    /// 통조림 제품만 가져오기 (브랜드 변경 가능한 제품)
    /// </summary>
    public List<ProductData> GetCannedProducts()
    {
        return GetProductsByType(ProductType.CannedPork);
    }

    /// <summary>
    /// 가짜 제품만 가져오기
    /// </summary>
    public List<ProductData> GetFakeProducts()
    {
        List<ProductData> fakes = new List<ProductData>();
        foreach (var product in productList)
        {
            if (product.isFake)
            {
                fakes.Add(product);
            }
        }
        return fakes;
    }

    /// <summary>
    /// 실제 제품만 가져오기 (가짜 제외)
    /// </summary>
    public List<ProductData> GetRealProducts()
    {
        List<ProductData> realProducts = new List<ProductData>();
        foreach (var product in productList)
        {
            if (!product.isFake)
            {
                realProducts.Add(product);
            }
        }
        return realProducts;
    }

    /// <summary>
    /// 가짜 제품 찾기 - ProductType과 BrandGrade가 일치하고 isFake가 true인 제품 반환
    /// </summary>
    /// <param name="productType">제품 타입</param>
    /// <param name="targetBrand">목표 브랜드 등급</param>
    /// <returns>조건에 맞는 가짜 제품 데이터, 없으면 null</returns>
    public ProductData FindFakeProduct(ProductType productType, BrandGrade targetBrand)
    {
        foreach (var product in productList)
        {
            // ProductType이 일치하고, isFake가 true이며, currentBrand가 목표 브랜드와 일치
            if (product.productType == productType &&
                product.isFake &&
                product.currentBrand == targetBrand)
            {
                Debug.Log($"[ProductDataManager] 가짜 제품 발견: {product.productName} (Type: {productType}, Brand: {targetBrand})");
                return product;
            }
        }

        Debug.LogWarning($"[ProductDataManager] 가짜 제품을 찾을 수 없음 (Type: {productType}, Brand: {targetBrand})");
        return null;
    }

    /// <summary>
    /// 모든 브랜드 데이터 가져오기
    /// </summary>
    public List<BrandData> GetAllBrands()
    {
        return new List<BrandData>(brandList);
    }

    /// <summary>
    /// 특정 ProductType에 해당하는 BrandData 가져오기
    /// </summary>
    public BrandData GetBrandDataByType(ProductType type)
    {
        foreach (var brand in brandList)
        {
            if (brand.targetProductType == type)
            {
                return brand;
            }
        }

        Debug.LogWarning($"[ProductDataManager] BrandData를 찾을 수 없음: {type}");
        return null;
    }

    /// <summary>
    /// UI 프리팹 가져오기
    /// </summary>
    public GameObject GetProductButtonPrefab()
    {
        return productButtonPrefab;
    }

    /// <summary>
    /// 제품 데이터 복제본 생성 (가짜 제품 생성용)
    /// </summary>
    public ProductData CloneProductData(string productName)
    {
        ProductData original = GetProductData(productName);
        if (original == null) return null;

        // 깊은 복사
        ProductData clone = new ProductData(
            original.productName,
            original.originalPrice,
            original.productType,
            original.currentBrand
        )
        {
            productSprite = original.productSprite,
            isFake = original.isFake,
            originalBrand = original.originalBrand
        };

        return clone;
    }

    /// <summary>
    /// 제품의 실제 원가를 계산
    /// - 진짜 제품: originalPrice 그대로 반환
    /// - 가짜 제품: originalBrand와 currentBrand를 비교하여 실제 원가 계산
    /// </summary>
    public int CalculateRealCost(ProductData product)
    {
        if (product == null)
        {
            Debug.LogError("[ProductDataManager] CalculateRealCost: product가 null입니다!");
            return 0;
        }

        // 진짜 제품인 경우 originalPrice 그대로 반환
        if (!product.isFake)
        {
            return product.originalPrice;
        }

        // 가짜 제품인 경우 실제 원가 계산
        // originalBrand가 Low면 배율 1.0, High면 1.5
        float originalMultiplier = product.originalBrand == BrandGrade.Low ? 1.0f : 1.5f;
        float currentMultiplier = product.currentBrand == BrandGrade.Low ? 1.0f : 1.5f;

        // 실제 원가 = 현재 originalPrice / 현재 배율 * 원래 배율
        int realCost = Mathf.RoundToInt(product.originalPrice / currentMultiplier * originalMultiplier);

        Debug.Log($"[ProductDataManager] 실제 원가 계산: {product.productName}");
        Debug.Log($"  - Product Type: {product.productType}");
        Debug.Log($"  - 원래 브랜드: {product.originalBrand} (배율 {originalMultiplier})");
        Debug.Log($"  - 현재 브랜드: {product.currentBrand} (배율 {currentMultiplier})");
        Debug.Log($"  - 표시된 originalPrice: {product.originalPrice}원");
        Debug.Log($"  - 계산된 실제 원가: {realCost}원");

        return realCost;
    }

}