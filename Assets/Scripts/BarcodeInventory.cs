using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 플레이어가 가지고 있는 바코드 인벤토리 관리
/// 10개의 재사용 가능한 바코드를 관리합니다
/// </summary>
public class BarcodeInventory : MonoBehaviour
{
    public static BarcodeInventory Instance { get; private set; }

    [Header("바코드 프리팹")]
    public GameObject barcodePrefab;  // DraggableBarcode가 붙은 프리팹

    [Header("UI 설정")]
    public Transform barcodeContainer;  // 바코드들이 배치될 부모 오브젝트
    public GridLayoutGroup gridLayout;   // Grid Layout Group (옵션)

    [Header("바코드 데이터")]
    public List<BarcodeData> availableBarcodes = new List<BarcodeData>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        InitializeBarcodes();
        CreateBarcodeUI();
    }

    void InitializeBarcodes()
    {
        // 기본 10개 바코드 생성 (다양한 가격대)
        availableBarcodes.Clear();
        availableBarcodes.Add(new BarcodeData("BC001", 500));
        availableBarcodes.Add(new BarcodeData("BC002", 1000));
        availableBarcodes.Add(new BarcodeData("BC003", 1500));
        availableBarcodes.Add(new BarcodeData("BC004", 2000));
        availableBarcodes.Add(new BarcodeData("BC005", 2500));
        availableBarcodes.Add(new BarcodeData("BC006", 3000));
        availableBarcodes.Add(new BarcodeData("BC007", 4000));
        availableBarcodes.Add(new BarcodeData("BC008", 5000));
        availableBarcodes.Add(new BarcodeData("BC009", 7000));
        availableBarcodes.Add(new BarcodeData("BC010", 10000));
    }

    void CreateBarcodeUI()
    {
        if (barcodePrefab == null || barcodeContainer == null)
        {
            Debug.LogWarning("BarcodeInventory: 프리팹이나 컨테이너가 설정되지 않았습니다.");
            return;
        }

        // 기존 바코드 UI 제거
        foreach (Transform child in barcodeContainer)
        {
            Destroy(child.gameObject);
        }

        // 각 바코드에 대한 UI 생성
        foreach (BarcodeData data in availableBarcodes)
        {
            GameObject barcodeObj = Instantiate(barcodePrefab, barcodeContainer);
            DraggableBarcode draggable = barcodeObj.GetComponent<DraggableBarcode>();
            if (draggable != null)
            {
                draggable.Initialize(data);
            }
        }
    }

    // 바코드를 사용해도 다시 쓸 수 있으므로 제거하지 않음
    // 필요시 특정 바코드를 일시적으로 비활성화하는 기능 추가 가능
}
