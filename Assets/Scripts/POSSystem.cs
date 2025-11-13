using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// POS 시스템 - 구매 내역, 환불, 매출 확인
/// ✅ Unity 6.0 호환 - 가짜 제품 탐지 제거 (손님이 못 알아채게 함)
/// ✅ MistakeManager로 실수 관리 기능 분리
/// </summary>
public class POSSystem : MonoBehaviour
{
    public static POSSystem Instance { get; private set; }

    [Header("UI References")]
    public GameObject posPanel;
    public Transform transactionContainer; // 거래 내역 리스트
    public TextMeshProUGUI totalSalesText; // 총 매출
    public TextMeshProUGUI totalProfitText; // 총 이익
    public Button clearButton; // 내역 초기화
    public Button toggleButton; // POS 토글 버튼
    public TextMeshProUGUI walletText; // 지갑 UI

    [Header("Stats")]
    public int totalSales = 0;
    public int totalProfit = 0;
    public int transactionCount = 0;
    public int walletMoney = 0; // 지갑 돈

    private List<GameObject> transactionItems = new List<GameObject>();
    private Dictionary<int, ItemRefundData> itemRefundDataMap = new Dictionary<int, ItemRefundData>(); // 상품별 환불 데이터
    private int itemRefundIDCounter = 0; // 상품별 고유 ID
    private TMP_FontAsset customFont; // 폰트 캐시

    // 개별 상품 환불 데이터
    private class ItemRefundData
    {
        public int itemID;
        public int transactionID;
        public ProductInteractable product;
        public int originalPrice;
        public int currentPrice;
        public int profit;
        public GameObject itemObj; // 상품 UI 오브젝트
        public bool isRefunded = false; // 환불 여부
    }

    // 거래별 헤더/합계 오브젝트 추적
    private class TransactionUIData
    {
        public int transactionID;
        public GameObject headerObj;
        public GameObject totalObj;
        public List<int> itemIDs = new List<int>(); // 이 거래에 속한 상품 ID들
    }

    private Dictionary<int, TransactionUIData> transactionUIMap = new Dictionary<int, TransactionUIData>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // customFont 가져오기
        GameSetupMaster setupMaster = FindFirstObjectByType<GameSetupMaster>();
        if (setupMaster != null)
        {
            customFont = setupMaster.customFont;
        }

        if (clearButton != null)
            clearButton.onClick.AddListener(ClearAllTransactions);

        if (toggleButton != null)
            toggleButton.onClick.AddListener(TogglePOS);

        UpdateUI();
        posPanel.SetActive(false); // 처음엔 숨김
    }

    public void TogglePOS()
    {
        posPanel.SetActive(!posPanel.activeSelf);
    }

    public void AddTransaction(List<ProductInteractable> products)
    {
        if (products.Count == 0) return;

        transactionCount++;
        int currentTransactionID = transactionCount;

        int transactionTotal = 0;
        int transactionProfit = 0;

        // ✅ 가짜 제품 탐지 제거 - 손님이 못 알아채게 함
        // 가짜 제품도 정상적으로 POS에 기록됨

        TransactionUIData uiData = new TransactionUIData
        {
            transactionID = currentTransactionID
        };

        // 거래 헤더 (거래 번호만 표시)
        GameObject headerObj = new GameObject($"Transaction_{currentTransactionID}");
        headerObj.transform.SetParent(transactionContainer, false);

        TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
        if (customFont != null) headerText.font = customFont;

        // ✅ 경고 표시 제거
        headerText.text = $"━ 거래 #{currentTransactionID} ━";
        headerText.fontSize = 16;
        headerText.alignment = TextAlignmentOptions.Center;
        headerText.color = new Color(0.3f, 0.3f, 0.8f);
        headerText.fontStyle = FontStyles.Bold;

        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.sizeDelta = new Vector2(280, 30);

        uiData.headerObj = headerObj;
        transactionItems.Add(headerObj);

        // 각 상품마다 환불 버튼 포함한 UI 생성
        foreach (var product in products)
        {
            int current = product.GetCurrentPrice();

            // ProductDataManager를 통해 실제 원가 계산
            int original = ProductDataManager.Instance.CalculateRealCost(product.productData);

            int itemProfit = current - original;

            transactionTotal += current;
            transactionProfit += itemProfit;

            // 상품별 고유 ID 생성
            itemRefundIDCounter++;
            int itemID = itemRefundIDCounter;

            // 상품 환불 데이터 생성
            ItemRefundData refundData = new ItemRefundData
            {
                itemID = itemID,
                transactionID = currentTransactionID,
                product = product,
                originalPrice = original,
                currentPrice = current,
                profit = itemProfit
            };

            // 상품 + 환불 버튼 컨테이너 (가로 배치)
            GameObject itemContainer = new GameObject($"ItemContainer_{itemID}");
            itemContainer.transform.SetParent(transactionContainer, false);

            HorizontalLayoutGroup itemLayout = itemContainer.AddComponent<HorizontalLayoutGroup>();
            itemLayout.childAlignment = TextAnchor.MiddleLeft;
            itemLayout.childControlWidth = false;
            itemLayout.childControlHeight = false;
            itemLayout.spacing = 5;
            itemLayout.padding = new RectOffset(10, 5, 0, 0);

            RectTransform itemContainerRect = itemContainer.GetComponent<RectTransform>();
            itemContainerRect.sizeDelta = new Vector2(280, 25);

            // 상품 정보 텍스트
            GameObject itemTextObj = new GameObject($"ItemText_{itemID}");
            itemTextObj.transform.SetParent(itemContainer.transform, false);

            TextMeshProUGUI itemText = itemTextObj.AddComponent<TextMeshProUGUI>();
            if (customFont != null) itemText.font = customFont;
            string profitSign = itemProfit >= 0 ? "+" : "";
            Color profitColor = itemProfit >= 0 ? new Color(0, 0.7f, 0) : Color.red;

            // ✅ 가짜 제품 표시 제거 - 일반 제품처럼 보이게 함
            itemText.text = $"{product.productData.productName}: {current}원 <color=#{ColorUtility.ToHtmlStringRGB(profitColor)}>({profitSign}{itemProfit})</color>";
            itemText.fontSize = 13;
            itemText.alignment = TextAlignmentOptions.Left;
            itemText.color = Color.white; // 모든 제품 동일한 색상

            RectTransform itemTextRect = itemTextObj.GetComponent<RectTransform>();
            itemTextRect.sizeDelta = new Vector2(180, 25);

            // 개별 환불 버튼
            GameObject refundBtnObj = new GameObject($"RefundBtn_{itemID}");
            refundBtnObj.transform.SetParent(itemContainer.transform, false);

            Image refundBtnImage = refundBtnObj.AddComponent<Image>();
            refundBtnImage.color = new Color(0.8f, 0.3f, 0.3f, 1f); // 빨간색

            Button refundButton = refundBtnObj.AddComponent<Button>();
            int capturedItemID = itemID; // 클로저용
            refundButton.onClick.AddListener(() => RefundItem(capturedItemID));

            RectTransform refundRect = refundBtnObj.GetComponent<RectTransform>();
            refundRect.sizeDelta = new Vector2(60, 20);

            GameObject refundTextObj = new GameObject("Text");
            refundTextObj.transform.SetParent(refundBtnObj.transform, false);
            TextMeshProUGUI refundText = refundTextObj.AddComponent<TextMeshProUGUI>();
            if (customFont != null) refundText.font = customFont;
            refundText.text = "환불";
            refundText.fontSize = 12;
            refundText.alignment = TextAlignmentOptions.Center;
            refundText.color = Color.white;

            RectTransform refundTextRect = refundTextObj.GetComponent<RectTransform>();
            refundTextRect.anchorMin = Vector2.zero;
            refundTextRect.anchorMax = Vector2.one;
            refundTextRect.offsetMin = Vector2.zero;
            refundTextRect.offsetMax = Vector2.zero;

            // 데이터 저장
            refundData.itemObj = itemContainer;
            itemRefundDataMap[itemID] = refundData;
            uiData.itemIDs.Add(itemID);
            transactionItems.Add(itemContainer);
        }

        // 거래 합계
        GameObject totalObj = new GameObject($"Total_{currentTransactionID}");
        totalObj.transform.SetParent(transactionContainer, false);

        TextMeshProUGUI totalText = totalObj.AddComponent<TextMeshProUGUI>();
        if (customFont != null) totalText.font = customFont;
        string profitSign2 = transactionProfit >= 0 ? "+" : "";
        totalText.text = $"합계: {transactionTotal}원 (이익: {profitSign2}{transactionProfit}원)";
        totalText.fontSize = 15;
        totalText.alignment = TextAlignmentOptions.Center;
        totalText.color = new Color(1f, 1f, 0.5f);
        totalText.fontStyle = FontStyles.Bold;

        RectTransform totalRect = totalObj.GetComponent<RectTransform>();
        totalRect.sizeDelta = new Vector2(280, 25);

        uiData.totalObj = totalObj;
        transactionUIMap[currentTransactionID] = uiData;
        transactionItems.Add(totalObj);

        // 전체 통계 업데이트
        totalSales += transactionTotal;
        totalProfit += transactionProfit;

        UpdateUI();

        Debug.Log($"[POS] 거래 #{currentTransactionID} 기록: {transactionTotal}원 (이익: {profitSign2}{transactionProfit}원), 상품 {products.Count}개");
    }

    void UpdateUI()
    {
        if (totalSalesText != null)
            totalSalesText.text = $"총 매출: {totalSales}원";

        if (totalProfitText != null)
        {
            Color profitColor = totalProfit >= 0 ? new Color(0, 0.8f, 0) : Color.red;
            string sign = totalProfit >= 0 ? "+" : "";
            totalProfitText.text = $"총 이익: {sign}{totalProfit}원";
            totalProfitText.color = profitColor;
        }

        UpdateWalletUI();
    }

    public void UpdateWalletUI()
    {
        if (walletText != null)
        {
            walletText.text = $"지갑: {walletMoney}원";
        }
    }

    // 개별 상품 환불 처리
    void RefundItem(int itemID)
    {
        if (!itemRefundDataMap.ContainsKey(itemID))
        {
            Debug.LogWarning($"[POS] 상품 ID {itemID}를 찾을 수 없습니다!");
            return;
        }

        ItemRefundData refundData = itemRefundDataMap[itemID];

        if (refundData.isRefunded)
        {
            Debug.LogWarning($"[POS] 이미 환불된 상품입니다!");
            return;
        }

        // 환불 시 지갑에 환불금(현재가격) 추가 (손님에게 돈을 돌려받음)
        walletMoney += refundData.currentPrice;

        // 전체 통계에서 차감
        totalSales -= refundData.currentPrice;
        totalProfit -= refundData.profit;

        // UI 오브젝트 삭제
        if (refundData.itemObj != null)
        {
            Destroy(refundData.itemObj);
            transactionItems.Remove(refundData.itemObj);
        }

        // 환불 처리 표시
        refundData.isRefunded = true;

        // 거래의 합계 업데이트
        UpdateTransactionTotal(refundData.transactionID);

        UpdateUI();

        Debug.Log($"[POS] 상품 '{refundData.product.productData.productName}' 환불 완료! 지갑에 {refundData.currentPrice}원 추가 (총 {walletMoney}원)");
    }

    // 특정 거래의 합계 업데이트 (일부 상품만 환불된 경우)
    void UpdateTransactionTotal(int transactionID)
    {
        if (!transactionUIMap.ContainsKey(transactionID))
            return;

        TransactionUIData uiData = transactionUIMap[transactionID];

        // 남은 상품들의 합계 계산
        int remainingTotal = 0;
        int remainingProfit = 0;
        int remainingCount = 0;

        foreach (int itemID in uiData.itemIDs)
        {
            if (itemRefundDataMap.ContainsKey(itemID))
            {
                ItemRefundData itemData = itemRefundDataMap[itemID];
                if (!itemData.isRefunded)
                {
                    remainingTotal += itemData.currentPrice;
                    remainingProfit += itemData.profit;
                    remainingCount++;
                }
            }
        }

        // 합계 텍스트 업데이트 또는 삭제
        if (uiData.totalObj != null)
        {
            if (remainingCount == 0)
            {
                // 모든 상품이 환불됨 - 헤더와 합계도 삭제
                Destroy(uiData.headerObj);
                Destroy(uiData.totalObj);
                transactionItems.Remove(uiData.headerObj);
                transactionItems.Remove(uiData.totalObj);
                transactionUIMap.Remove(transactionID);
            }
            else
            {
                // 일부만 환불됨 - 합계 업데이트
                TextMeshProUGUI totalText = uiData.totalObj.GetComponent<TextMeshProUGUI>();
                if (totalText != null)
                {
                    string profitSign = remainingProfit >= 0 ? "+" : "";
                    totalText.text = $"합계: {remainingTotal}원 (이익: {profitSign}{remainingProfit}원)";
                }
            }
        }
    }

    void ClearAllTransactions()
    {
        // 모든 UI 오브젝트 삭제
        foreach (var item in transactionItems)
        {
            if (item != null) Destroy(item);
        }

        transactionItems.Clear();
        itemRefundDataMap.Clear();
        transactionUIMap.Clear();
        itemRefundIDCounter = 0;
        totalSales = 0;
        totalProfit = 0;
        transactionCount = 0;

        UpdateUI();

        Debug.Log("[POS] 모든 거래 내역 초기화");
    }
}