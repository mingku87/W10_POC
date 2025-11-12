using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ProductDataManager의 데이터를 기반으로 UI 버튼을 동적 생성
/// 이미지처럼 스크롤 가능한 제품 목록을 만듦
/// 가짜 제품(isFake=true)은 UI에 표시하지 않음
/// Unity 6.0 최신 버전 호환
/// </summary>
public class ProductUISpawner : MonoBehaviour
{
    [Header("UI 부모 오브젝트")]
    [SerializeField] private Transform contentParent; // ScrollView의 Content

    [Header("필터 설정")]
    [SerializeField] private ProductType filterType = ProductType.None; // None이면 전체 표시
    [SerializeField] private bool showOnlyFake = false; // 가짜 제품만 표시

    [Header("레이아웃 설정")]
    [SerializeField] private float spacing = 10f; // 버튼 간 간격

    private List<GameObject> spawnedButtons = new List<GameObject>();

    private void Start()
    {
        // 약간의 지연을 주고 생성 (Manager 초기화 대기)
        Invoke(nameof(SpawnAllProducts), 0.1f);
    }

    /// <summary>
    /// 모든 제품 UI 생성
    /// </summary>
    public void SpawnAllProducts()
    {
        if (ProductDataManager.Instance == null)
        {
            Debug.LogError("[ProductUISpawner] ProductDataManager가 없습니다!");
            return;
        }

        if (contentParent == null)
        {
            Debug.LogError("[ProductUISpawner] Content Parent가 할당되지 않았습니다!");
            return;
        }

        // 기존 버튼 제거
        ClearAllButtons();

        // 제품 목록 가져오기
        List<ProductData> products = GetFilteredProducts();

        // UI 생성
        foreach (var product in products)
        {
            SpawnProductButton(product);
        }

        Debug.Log($"[ProductUISpawner] {products.Count}개 제품 UI 생성 완료");
    }

    /// <summary>
    /// 필터링된 제품 목록 가져오기
    /// </summary>
    private List<ProductData> GetFilteredProducts()
    {
        List<ProductData> products;

        // 가짜 제품만 필터
        if (showOnlyFake)
        {
            products = ProductDataManager.Instance.GetFakeProducts();
        }
        // 타입 필터
        else if (filterType != ProductType.None)
        {
            products = ProductDataManager.Instance.GetProductsByType(filterType);
            // 가짜 제품 제외
            products.RemoveAll(p => p.isFake);
        }
        // 전체 (가짜 제외)
        else
        {
            products = ProductDataManager.Instance.GetRealProducts();
        }

        return products;
    }

    /// <summary>
    /// 개별 제품 버튼 생성
    /// </summary>
    private void SpawnProductButton(ProductData productData)
    {
        GameObject prefab = ProductDataManager.Instance.GetProductButtonPrefab();
        if (prefab == null)
        {
            Debug.LogError("[ProductUISpawner] 프리팹이 설정되지 않았습니다!");
            return;
        }

        // 프리팹 인스턴스화
        GameObject buttonObj = Instantiate(prefab, contentParent);

        // ProductInteractable 설정
        ProductInteractable interactable = buttonObj.GetComponent<ProductInteractable>();
        if (interactable != null)
        {
            // ProductData 복사본 할당 (원본 데이터 보호)
            interactable.productData = ProductDataManager.Instance.CloneProductData(productData.productName);

            // 초기화
            interactable.InitializeAsNewProduct();
        }
        else
        {
            Debug.LogWarning($"[ProductUISpawner] {buttonObj.name}에 ProductInteractable 컴포넌트가 없습니다!");
        }

        // 버튼 클릭 이벤트 추가 (드래그 가능한 제품 생성)
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnProductButtonClicked(productData));
        }

        spawnedButtons.Add(buttonObj);
    }

    /// <summary>
    /// 제품 버튼 클릭 시 - 드래그 가능한 제품 생성
    /// </summary>
    private void OnProductButtonClicked(ProductData productData)
    {
        Debug.Log($"[ProductUISpawner] {productData.productName} 버튼 클릭됨!");

        // 여기서 드래그 가능한 제품을 생성하거나
        // 다른 매니저에게 알림을 보낼 수 있습니다

        // 예시: 인벤토리에 추가
        // InventoryManager.Instance.AddProduct(productData);
    }

    /// <summary>
    /// 모든 버튼 제거
    /// </summary>
    public void ClearAllButtons()
    {
        foreach (var button in spawnedButtons)
        {
            if (button != null)
                Destroy(button);
        }
        spawnedButtons.Clear();
    }

    /// <summary>
    /// 필터 변경 후 다시 생성
    /// </summary>
    public void SetFilter(ProductType type, bool onlyFake = false)
    {
        filterType = type;
        showOnlyFake = onlyFake;
        SpawnAllProducts();
    }

    /// <summary>
    /// 통조림만 표시
    /// </summary>
    [ContextMenu("통조림만 표시")]
    public void ShowOnlyCannedProducts()
    {
        SetFilter(ProductType.CannedPork, false);
    }

    /// <summary>
    /// 가짜 제품만 표시
    /// </summary>
    [ContextMenu("가짜 제품만 표시")]
    public void ShowOnlyFakeProducts()
    {
        SetFilter(ProductType.None, true);
    }

    /// <summary>
    /// 전체 제품 표시 (가짜 제외)
    /// </summary>
    [ContextMenu("전체 제품 표시")]
    public void ShowAllProducts()
    {
        SetFilter(ProductType.None, false);
    }
}