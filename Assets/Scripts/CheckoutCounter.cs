using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 계산대 - 스캐너로 스캔한 상품 누적, "계산하기" 버튼으로 완료
/// </summary>
public class CheckoutCounter : MonoBehaviour
{
    public static CheckoutCounter Instance { get; private set; }

    [Header("계산대 위치")]
    public Transform counterPosition; // 계산대 중앙 위치
    public float itemSpacing = 1.5f; // 상품 간 간격

    [Header("UI")]
    public TextMeshProUGUI totalAmountText; // 총 금액 표시 (UI 텍스트)
    public GameObject checkoutButton; // "계산하기" 버튼 (3D 오브젝트)

    private List<ProductInteractable> scannedItems = new List<ProductInteractable>();
    private List<GameObject> displayedItems = new List<GameObject>();
    private int totalAmount = 0;
    public bool isCustomerWaiting = false; // public으로 변경 (스캐너에서 확인)
    private Customer currentCustomer = null;
    private Canvas counterItemsCanvas; // 계산대 상품 전용 캔버스

    // 결제 상태
    public enum PaymentState
    {
        Scanning,        // 스캔 중
        WaitingPayment,  // 결제 대기 (카드/현금 선택됨)
        WaitingChange,   // 거스름돈 대기 (현금 결제)
        Complete         // 완료
    }

    public PaymentState currentPaymentState = PaymentState.Scanning;
    public bool isCardPayment = false; // 카드 결제 여부
    public int customerPaidAmount = 0; // 손님이 낸 금액
    public GameObject customerMoneyContainer; // 손님이 낸 돈 표시 컨테이너
    public TextMeshProUGUI paymentMethodText; // 결제 방식 표시 텍스트

    // 포스기 화면 업데이트용 이벤트
    public System.Action<int> onTotalChanged;

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

        // 계산대 상품 전용 캔버스 생성 (높은 sortOrder)
        GameObject canvasObj = new GameObject("CounterItemsCanvas");
        counterItemsCanvas = canvasObj.AddComponent<Canvas>();
        counterItemsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        counterItemsCanvas.sortingOrder = 100; // 최상위
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    }

    void Start()
    {
        UpdateTotalDisplay();
    }

    void Update()
    {
        // 계산하기 버튼 클릭 (3D 오브젝트이므로 레이캐스트 필요)
        // 여기서는 간단하게 C키로 처리
        if (Input.GetKeyDown(KeyCode.C) && isCustomerWaiting)
        {
            HandleCheckoutInput();
        }
    }

    void HandleCheckoutInput()
    {
        if (currentPaymentState == PaymentState.Scanning)
        {
            // 첫 C키: 결제 방식 선택
            StartPayment();
        }
        else if (currentPaymentState == PaymentState.WaitingPayment)
        {
            // 카드 결제일 경우 바로 완료
            if (isCardPayment)
            {
                ProcessCheckout();
            }
        }
        else if (currentPaymentState == PaymentState.WaitingChange)
        {
            // 거스름돈 확인 후 완료
            ValidateChangeAndComplete();
        }
    }

    void StartPayment()
    {
        if (scannedItems.Count == 0)
        {
            Debug.LogWarning("[계산대] 스캔한 상품이 없습니다!");
            return;
        }

        // 카드/현금 랜덤 선택 (50%)
        isCardPayment = Random.value < 0.5f;
        currentPaymentState = PaymentState.WaitingPayment;

        if (isCardPayment)
        {
            Debug.Log("[계산대] 손님이 카드로 결제합니다. C키를 한번 더 눌러 완료하세요.");
            ShowPaymentMethod("카드");
        }
        else
        {
            Debug.Log("[계산대] 손님이 현금으로 결제합니다. 돈을 받고 거스름돈을 준비하세요.");
            SpawnCustomerMoney();
            ShowPaymentMethod($"현금 {customerPaidAmount}원");
            currentPaymentState = PaymentState.WaitingChange;
        }

        // 포스기 화면 업데이트
        UpdateTotalDisplay();
    }

    void ShowPaymentMethod(string method)
    {
        // 결제 방식 텍스트 생성 (손님 위쪽)
        if (paymentMethodText == null)
        {
            GameObject textObj = new GameObject("PaymentMethodText");
            textObj.transform.SetParent(counterItemsCanvas.transform, false);

            paymentMethodText = textObj.AddComponent<TextMeshProUGUI>();

            // 폰트 가져오기
            GameSetupMaster setupMaster = FindFirstObjectByType<GameSetupMaster>();
            if (setupMaster != null && setupMaster.customFont != null)
            {
                paymentMethodText.font = setupMaster.customFont;
            }

            paymentMethodText.fontSize = 32;
            paymentMethodText.alignment = TextAlignmentOptions.Center;
            paymentMethodText.color = new Color(1f, 0.9f, 0.2f); // 금색
            paymentMethodText.fontStyle = FontStyles.Bold;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(0f, 0f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(100, 280); // 손님 위쪽
            textRect.sizeDelta = new Vector2(300, 50);
        }

        paymentMethodText.text = method;
        paymentMethodText.gameObject.SetActive(true);
    }

    // 스캐너가 상품을 스캔했을 때 호출
    public void AddScannedItem(ProductInteractable product)
    {
        if (!isCustomerWaiting)
        {
            Debug.LogWarning("[계산대] 아직 손님이 없습니다!");
            return;
        }

        // ✅ 상품 생성하지 않고 가격만 추가
        scannedItems.Add(product);
        totalAmount += product.GetCurrentPrice();

        UpdateTotalDisplay();

        Debug.Log($"[계산대] {product.productData.productName} 스캔 - 현재 총액: {totalAmount}원");
    }

    // 간단한 가격 바코드 스캔 (3000원, 5000원)
    public void AddSimplePrice(int price)
    {
        if (!isCustomerWaiting)
        {
            Debug.LogWarning("[계산대] 아직 손님이 없습니다!");
            return;
        }

        totalAmount += price;
        UpdateTotalDisplay();

        Debug.Log($"[계산대] 간단 바코드 스캔 - {price}원 추가, 현재 총액: {totalAmount}원");
    }

    void DisplayScannedItem(ProductInteractable product)
    {
        // 계산대 위에 상품 복제본 생성 (전용 캔버스 사용)
        GameObject itemObj = new GameObject($"CounterItem_{product.productData.productName}_{displayedItems.Count}");
        itemObj.transform.SetParent(counterItemsCanvas.transform, false);

        Image itemImage = itemObj.AddComponent<Image>();
        itemImage.color = product.GetComponent<Image>().color; // 원본 색상 복사

        RectTransform itemRect = itemObj.GetComponent<RectTransform>();

        int index = displayedItems.Count;

        // 손님 구역 (화면 하단 우측)에 상품 배치 - 거스름돈 라벨 위
        itemRect.anchorMin = new Vector2(1f, 0f); // 우측 하단 기준
        itemRect.anchorMax = new Vector2(1f, 0f);
        itemRect.pivot = new Vector2(0.5f, 0.5f);

        // 우측 하단 손님 구역 안에 배치 (거스름돈 영역 바로 위)
        float xOffset = -350 - (index * 90); // 우측에서 조금 안쪽, 왼쪽으로 나열
        float yOffset = 230; // 거스름돈 드랍존(60+80=140) 위쪽

        itemRect.anchoredPosition = new Vector2(xOffset, yOffset);
        itemRect.sizeDelta = new Vector2(80, 80);

        // ✅ 복제본에 ProductInteractable 복사 (스캔 가능하도록)
        ProductInteractable clonedProduct = itemObj.AddComponent<ProductInteractable>();
        clonedProduct.productData = product.productData; // 원본 데이터 복사

        // 원본 바코드 정보도 복사
        BarcodeData originalBarcode = product.GetCurrentBarcode();
        clonedProduct.SetBarcode(new BarcodeData(originalBarcode.barcodeID, originalBarcode.price));

        // ✅ 가격 텍스트 추가 (폰트 적용 필요)
        GameObject priceTextObj = new GameObject("PriceText");
        priceTextObj.transform.SetParent(itemObj.transform, false);

        TextMeshProUGUI priceText = priceTextObj.AddComponent<TextMeshProUGUI>();

        // customFont 가져오기 (GameSetupMaster에서)
        GameSetupMaster setupMaster = FindFirstObjectByType<GameSetupMaster>();
        if (setupMaster != null && setupMaster.customFont != null)
        {
            priceText.font = setupMaster.customFont;
        }
        else
        {
            // customFont가 없으면 Resources에서 로드
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/NanumGothic-Regular SDF");
            if (font != null)
            {
                priceText.font = font;
            }
        }

        priceText.text = $"{originalBarcode.price}원";
        priceText.fontSize = 14;
        priceText.alignment = TextAlignmentOptions.Center;
        priceText.color = Color.white;
        priceText.fontStyle = FontStyles.Bold;

        RectTransform textRect = priceTextObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 0);
        textRect.pivot = new Vector2(0.5f, 0);
        textRect.anchoredPosition = new Vector2(0, 5);
        textRect.sizeDelta = new Vector2(0, 20);

        // ✅ 상품 이름 텍스트 추가 (위쪽)
        GameObject nameTextObj = new GameObject("NameText");
        nameTextObj.transform.SetParent(itemObj.transform, false);

        TextMeshProUGUI nameText = nameTextObj.AddComponent<TextMeshProUGUI>();
        if (setupMaster != null && setupMaster.customFont != null)
        {
            nameText.font = setupMaster.customFont;
        }
        else
        {
            // customFont가 없으면 Resources에서 로드
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/NanumGothic-Regular SDF");
            if (font != null)
            {
                nameText.font = font;
            }
        }

        nameText.text = product.productData.productName;
        nameText.fontSize = 12;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = new Color(1f, 1f, 0.8f); // 연한 노란색
        nameText.fontStyle = FontStyles.Normal;

        RectTransform nameTextRect = nameTextObj.GetComponent<RectTransform>();
        nameTextRect.anchorMin = new Vector2(0, 1);
        nameTextRect.anchorMax = new Vector2(1, 1);
        nameTextRect.pivot = new Vector2(0.5f, 1);
        nameTextRect.anchoredPosition = new Vector2(0, -5);
        nameTextRect.sizeDelta = new Vector2(0, 18);

        clonedProduct.priceText = priceText; // ProductInteractable에 연결

        displayedItems.Add(itemObj);

        Debug.Log($"[계산대] 상품 복제 생성: {product.productData.productName} (총 {displayedItems.Count}개, 현재가: {originalBarcode.price}원)");
    }

    void UpdateTotalDisplay()
    {
        // 포스기 화면 업데이트만 (텍스트는 포스기 LCD에 표시됨)
        onTotalChanged?.Invoke(totalAmount);
    }

    // 손님이 계산대에 도착했을 때
    public void OnCustomerArrived(Customer customer)
    {
        currentCustomer = customer;
        isCustomerWaiting = true;

        Debug.Log("[계산대] 손님 대기 중 - 손님이 가져온 상품을 계산대에 표시합니다!");

        // 손님이 선택한 상품들을 계산대에 표시만 (가격은 스캔할 때 추가)
        foreach (var product in customer.selectedProducts)
        {
            DisplayScannedItem(product); // 그냥 시각적으로만 표시
        }

        Debug.Log($"[계산대] 손님이 가져온 상품 {customer.selectedProducts.Count}개 표시 완료! 스캐너로 스캔하세요!");

        // 스캐너 초기화
        if (BarcodeScanner.Instance != null)
        {
            BarcodeScanner.Instance.ResetScannedProducts();
        }
    }

    void SpawnCustomerMoney()
    {
        // 손님이 낼 금액 결정 (총액보다 크거나 같게)
        int[] possibleAmounts = CalculatePossiblePayments(totalAmount);
        customerPaidAmount = possibleAmounts[Random.Range(0, possibleAmounts.Length)];

        Debug.Log($"[계산대] 손님이 {customerPaidAmount}원을 냅니다. 거스름돈: {customerPaidAmount - totalAmount}원");

        // 손님 돈 컨테이너 생성
        if (customerMoneyContainer == null)
        {
            customerMoneyContainer = new GameObject("CustomerMoneyContainer");
            customerMoneyContainer.transform.SetParent(counterItemsCanvas.transform, false);

            RectTransform containerRect = customerMoneyContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0f, 0f);
            containerRect.anchorMax = new Vector2(0f, 0f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = new Vector2(200, 150); // 손님 구역
            containerRect.sizeDelta = new Vector2(300, 150);
        }

        // 기존 돈 제거
        foreach (Transform child in customerMoneyContainer.transform)
        {
            Destroy(child.gameObject);
        }

        // 돈 스프라이트 로드
        Sprite[] moneySprites = new Sprite[6];
        int[] moneyValues = { 1000, 5000, 10000, 50000 };

        // Resources에서 스프라이트 로드
        for (int i = 0; i < 4; i++)
        {
            moneySprites[i] = Resources.Load<Sprite>($"Sprites/Money/{moneyValues[i]}");
        }

        // 돈 배치
        List<int> breakdown = BreakdownMoney(customerPaidAmount);
        float xOffset = 0f;

        foreach (int value in breakdown)
        {
            GameObject moneyObj = new GameObject($"CustomerMoney_{value}");
            moneyObj.transform.SetParent(customerMoneyContainer.transform, false);

            Image moneyImage = moneyObj.AddComponent<Image>();
            moneyImage.sprite = Resources.Load<Sprite>($"Sprites/Money/{value}");
            moneyImage.color = Color.white;

            RectTransform moneyRect = moneyObj.GetComponent<RectTransform>();
            moneyRect.anchorMin = new Vector2(0f, 0.5f);
            moneyRect.anchorMax = new Vector2(0f, 0.5f);
            moneyRect.pivot = new Vector2(0.5f, 0.5f);
            moneyRect.anchoredPosition = new Vector2(xOffset, 0);
            moneyRect.sizeDelta = new Vector2(60, 60);

            xOffset += 70f;
        }
    }

    int[] CalculatePossiblePayments(int amount)
    {
        // 총액에 따라 가능한 결제 금액들
        List<int> possiblePayments = new List<int>();

        if (amount <= 5000)
        {
            possiblePayments.Add(5000);
            possiblePayments.Add(10000);
        }
        else if (amount <= 10000)
        {
            possiblePayments.Add(10000);
            possiblePayments.Add(15000); // 10000 + 5000
            possiblePayments.Add(20000); // 10000 + 10000
        }
        else if (amount <= 20000)
        {
            possiblePayments.Add(20000);
            possiblePayments.Add(30000);
            possiblePayments.Add(50000);
        }
        else if (amount <= 50000)
        {
            possiblePayments.Add(50000);
            possiblePayments.Add(60000);
            possiblePayments.Add(100000);
        }
        else
        {
            possiblePayments.Add(100000);
            possiblePayments.Add(150000);
        }

        return possiblePayments.ToArray();
    }

    List<int> BreakdownMoney(int amount)
    {
        // 금액을 지폐로 분해
        List<int> breakdown = new List<int>();
        int[] denominations = { 50000, 10000, 5000, 1000 };

        foreach (int denom in denominations)
        {
            while (amount >= denom)
            {
                breakdown.Add(denom);
                amount -= denom;
            }
        }

        return breakdown;
    }

    void ValidateChangeAndComplete()
    {
        // 거스름돈 영역 확인
        ChangeMoneyDropZone changeZone = FindFirstObjectByType<ChangeMoneyDropZone>();
        if (changeZone == null)
        {
            Debug.LogWarning("[계산대] 거스름돈 영역을 찾을 수 없습니다!");
            return;
        }

        int expectedChange = customerPaidAmount - totalAmount;
        int actualChange = changeZone.GetTotalRealMoney();
        int fakeMoney = changeZone.GetTotalFakeMoney();

        Debug.Log($"[계산대] 예상 거스름돈: {expectedChange}원, 실제: {actualChange}원, 가짜: {fakeMoney}원");

        // 손님 타입 확인
        CustomerManager manager = FindFirstObjectByType<CustomerManager>();
        bool isCustomerDrunk = false;
        bool isCustomerOnPhone = false;

        if (manager != null && manager.currentCheckoutCustomer != null)
        {
            Customer customer = manager.currentCheckoutCustomer;
            isCustomerDrunk = customer.customerType == Customer.CustomerType.Drunk;
            isCustomerOnPhone = customer.isOnPhone;
        }

        // 실수 체크 (올릴 때 이미 체크했으므로 여기서는 금액만 체크)
        bool hasMistake = false;

        // 멀쩡한 손님에게 금액 틀리게 주면 실수 (진짜 돈 + 가짜 돈 합계로 체크)
        int totalGivenChange = actualChange + fakeMoney;
        if (!isCustomerDrunk && totalGivenChange != expectedChange)
        {
            Debug.LogWarning("[경고] 거스름돈 금액이 틀렸습니다!");
            hasMistake = true;
        }

        if (hasMistake && POSSystem.Instance != null)
        {
            POSSystem.Instance.AddMistake();
        }

        // 가짜 돈을 성공적으로 건네면 지갑에 추가
        if (!hasMistake && fakeMoney > 0)
        {
            if (POSSystem.Instance != null)
            {
                POSSystem.Instance.walletMoney += fakeMoney;
                POSSystem.Instance.UpdateWalletUI();
                Debug.Log($"[계산대] 가짜 돈 {fakeMoney}원을 성공적으로 건넸습니다! 지갑에 추가!");
            }
        }

        // 계산 완료
        ProcessCheckout();
    }

    void ProcessCheckout()
    {
        if (scannedItems.Count == 0)
        {
            Debug.LogWarning("[계산대] 스캔한 상품이 없습니다!");
            return;
        }

        Debug.Log($"[계산대] 계산 처리 중... 총 {scannedItems.Count}개 상품, {totalAmount}원");

        // 손님이 가져온 상품의 총 원가 계산
        int totalOriginalPrice = 0;
        foreach (var product in scannedItems)
        {
            totalOriginalPrice += product.productData.originalPrice;
        }

        // 이익 = 받은 금액 - 원가
        int profit = totalAmount - totalOriginalPrice;

        // 지갑에 이익만큼 돈 추가
        if (POSSystem.Instance != null)
        {
            POSSystem.Instance.walletMoney += profit;
            POSSystem.Instance.UpdateWalletUI();

            Debug.Log($"[계산대] 이익 {profit}원을 지갑에 추가! (원가: {totalOriginalPrice}원, 받은금액: {totalAmount}원)");
        }

        // 포스기 화면에 결제 완료 표시
        POSMachineDisplay posDisplay = FindFirstObjectByType<POSMachineDisplay>();
        if (posDisplay != null)
        {
            posDisplay.ShowCheckoutComplete(totalAmount);
        }

        // POS 시스템에 기록
        if (POSSystem.Instance != null)
        {
            POSSystem.Instance.AddTransaction(scannedItems);
        }

        // 거스름돈 드랍 존 초기화 (계산 완료 시 돈 제거)
        ChangeMoneyDropZone changeZone = FindFirstObjectByType<ChangeMoneyDropZone>();
        if (changeZone != null)
        {
            changeZone.ClearAllMoney();
        }

        // 손님이 낸 돈 제거
        if (customerMoneyContainer != null)
        {
            foreach (Transform child in customerMoneyContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }

        // 결제 방식 텍스트 숨김
        if (paymentMethodText != null)
        {
            paymentMethodText.gameObject.SetActive(false);
        }

        // 상태 초기화
        currentPaymentState = PaymentState.Scanning;
        isCardPayment = false;
        customerPaidAmount = 0;

        // 계산대 정리
        ClearCounter();

        // CustomerManager에게 완료 알림
        if (CustomerManager.Instance != null)
        {
            CustomerManager.Instance.OnPaymentComplete();
        }

        isCustomerWaiting = false;
        currentCustomer = null;
    }

    void ClearCounter()
    {
        // 복제된 상품들 삭제
        foreach (var item in displayedItems)
        {
            if (item != null)
                Destroy(item);
        }

        displayedItems.Clear();
        scannedItems.Clear();
        totalAmount = 0;

        UpdateTotalDisplay();

        Debug.Log("[계산대] 계산대 정리 완료 - 복제 상품 삭제됨");
    }

    public int GetTotalAmount()
    {
        return totalAmount;
    }

    // 손님이 계산대에 도착하기 전 (구버전 호환)
    public void DisplayCustomerItems(List<ProductInteractable> products)
    {
        // 이제 사용 안함 - 스캐너로 직접 스캔
        Debug.Log("[계산대] 손님이 도착했습니다. 스캐너로 상품을 스캔하세요!");
    }
}
