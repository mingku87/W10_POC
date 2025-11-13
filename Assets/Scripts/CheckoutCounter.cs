using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 계산대 메인 컨트롤러 - 상태 관리 및 결제 흐름 제어
/// ✅ Unity 6.0 호환 - ProductType + BrandGrade 기반 검증 (isFake 무시)
/// </summary>
public class CheckoutCounter : MonoBehaviour
{
    public static CheckoutCounter Instance { get; private set; }

    [Header("계산대 위치")]
    public Transform counterPosition;
    public float itemSpacing = 1.5f;

    [Header("UI")]
    public Button processPaymentButton; // 인스펙터에서 할당할 실제 UI 버튼

    [Header("손님 존 참조")]
    public CustomerZone customerZone; // CustomerZone 컴포넌트 참조

    // 서브 매니저들
    private CheckoutDisplayManager displayManager;
    public TextMeshProUGUI totalAmountText;
    private CheckoutItemManager itemManager;

    // 상태
    public bool isCustomerWaiting = false;
    public Customer currentCustomer = null;

    // 결제 상태
    public enum PaymentState
    {
        Scanning,
        WaitingPayment,
        WaitingChange,
        Complete
    }

    public PaymentState currentPaymentState = PaymentState.Scanning;
    public bool isCardPayment = false;
    public int customerPaidAmount = 0;

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

        // 서브 매니저 초기화
        displayManager = gameObject.AddComponent<CheckoutDisplayManager>();
        itemManager = gameObject.AddComponent<CheckoutItemManager>();
    }

    void Start()
    {
        displayManager.Initialize(counterPosition);
        UpdateTotalDisplay();

        // CustomerZone 자동 찾기
        if (customerZone == null)
        {
            customerZone = CustomerZone.Instance;
            if (customerZone == null)
            {
                Debug.LogWarning("[계산대] CustomerZone을 찾을 수 없습니다! Inspector에서 설정하세요.");
            }
        }

        // 버튼 리스너 등록 및 초기 비활성화
        if (processPaymentButton != null)
        {
            processPaymentButton.onClick.AddListener(HandleCheckoutInput);
            processPaymentButton.interactable = false; // 처음에는 비활성화
        }
        else
        {
            Debug.LogWarning("[계산대] processPaymentButton이 Inspector에서 할당되지 않았습니다!");
        }
    }

    void Update()
    {
        // C키로 계산 처리
        if (Input.GetKeyDown(KeyCode.C) && isCustomerWaiting)
        {
            HandleCheckoutInput();
        }
    }

    void HandleCheckoutInput()
    {
        // 버튼이 활성화되었더라도, 안전을 위해 한 번 더 체크
        if (!isCustomerWaiting) return;

        if (currentPaymentState == PaymentState.Scanning)
        {
            StartPayment();
        }
        else if (currentPaymentState == PaymentState.WaitingPayment)
        {
            if (isCardPayment)
            {
                ProcessCheckout();
            }
        }
        else if (currentPaymentState == PaymentState.WaitingChange)
        {
            ValidateChangeAndComplete();
        }
    }

    void StartPayment()
    {
        if (itemManager.GetScannedItemCount() == 0)
        {
            Debug.LogWarning("[계산대] 스캔한 상품이 없습니다!");
            return;
        }

        // ✅ C키 눌렀을 때 사기 한계 체크
        if (currentCustomer != null)
        {
            if (!currentCustomer.CheckFraudLimit(itemManager.GetTotalAmount()))
            {
                // 손님이 화나서 나감 (Customer.LeaveAngry에서 처리)
                return;
            }
        }

        // 카드/현금 랜덤 선택 (50%)
        isCardPayment = Random.value < 0.5f;
        currentPaymentState = PaymentState.WaitingPayment;

        if (isCardPayment)
        {
            Debug.Log("[계산대] 손님이 카드로 결제합니다. C키를 한번 더 눌러 완료하세요.");
            displayManager.ShowPaymentMethod("카드");
        }
        else
        {
            Debug.Log("[계산대] 손님이 현금으로 결제합니다. 돈을 받고 거스름돈을 준비하세요.");
            customerPaidAmount = displayManager.SpawnCustomerMoney(itemManager.GetTotalAmount());
            displayManager.ShowPaymentMethod($"현금 {customerPaidAmount}원");
            currentPaymentState = PaymentState.WaitingChange;
        }

        UpdateTotalDisplay();
    }

    void ValidateChangeAndComplete()
    {
        ChangeMoneyDropZone changeZone = FindFirstObjectByType<ChangeMoneyDropZone>();
        if (changeZone == null)
        {
            Debug.LogWarning("[계산대] 거스름돈 영역을 찾을 수 없습니다!");
            return;
        }

        int expectedChange = customerPaidAmount - itemManager.GetTotalAmount();
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

        bool hasMistake = false;

        // 멀쩡한 손님에게 금액 틀리게 주면 실수
        int totalGivenChange = actualChange + fakeMoney;
        if (!isCustomerDrunk && totalGivenChange != expectedChange)
        {
            Debug.LogWarning("[경고] 거스름돈 금액이 틀렸습니다!");
            hasMistake = true;
        }
        if (hasMistake && MistakeManager.Instance != null)
        {
            MistakeManager.Instance.AddMistake(
                MistakeManager.MistakeType.ChangeAmountMistake,
                $"거스름돈 오류: 예상 {totalGivenChange}원"
            );
        }

        // 현금 결제 총 이득 계산 및 지갑에 추가 (실수 여부와 관계없이 이득은 챙김)
        if (POSSystem.Instance != null)
        {
            // 1. 가짜 돈 이득 (거스름돈으로 가짜 돈 준 금액)
            int fakeMoneyProfit = fakeMoney;

            // 2. 사기 이득 (여러번 스캔해서 얻은 이득)
            int totalOriginalPrice = itemManager.GetTotalOriginalPrice();
            int scanProfit = itemManager.GetTotalAmount() - totalOriginalPrice;

            // 3. 총 이득 = 가짜 돈 + 사기 이득
            int totalProfit = fakeMoneyProfit + scanProfit;

            if (totalProfit > 0)
            {
                POSSystem.Instance.walletMoney += totalProfit;
                POSSystem.Instance.UpdateWalletUI();
                Debug.Log($"[계산대 - 현금결제] 총 이득 {totalProfit}원을 지갑에 추가! (가짜돈: {fakeMoneyProfit}원, 스캔사기: {scanProfit}원)");
            }
        }

        ProcessCheckout();
    }

    void ProcessCheckout()
    {
        if (itemManager.GetScannedItemCount() == 0)
        {
            Debug.LogWarning("[계산대] 스캔한 상품이 없습니다!");
            return;
        }

        Debug.Log($"[계산대] 계산 처리 중... 총 {itemManager.GetScannedItemCount()}개 상품, {itemManager.GetTotalAmount()}원");

        // ✅ ProductType + BrandGrade 기반으로 검증 (isFake 무시)
        bool hasWrongProduct = false;
        if (currentCustomer != null)
        {
            List<ProductInteractable> scannedItems = itemManager.GetScannedItems();

            // 손님이 원하는 상품 타입/등급별 개수 카운트
            Dictionary<string, int> wantedProducts = new Dictionary<string, int>();
            foreach (var wantedProduct in currentCustomer.selectedProducts)
            {
                string key = $"{wantedProduct.productData.productType}_{wantedProduct.productData.currentBrand}";
                if (wantedProducts.ContainsKey(key))
                {
                    wantedProducts[key]++;
                }
                else
                {
                    wantedProducts[key] = 1;
                }
            }

            // 스캔된 상품 타입/등급별 개수 카운트
            Dictionary<string, int> scannedProducts = new Dictionary<string, int>();
            foreach (var scannedItem in scannedItems)
            {
                string key = $"{scannedItem.productData.productType}_{scannedItem.productData.currentBrand}";
                if (scannedProducts.ContainsKey(key))
                {
                    scannedProducts[key]++;
                }
                else
                {
                    scannedProducts[key] = 1;
                }
            }

            // 검증: 각 타입/등급별로 개수가 맞는지 확인
            Debug.Log("═══════════════════════════════════");
            Debug.Log("[계산 검증] ProductType + BrandGrade 기반 검증 시작");
            Debug.Log("───────────────────────────────────");

            foreach (var wanted in wantedProducts)
            {
                string typeGrade = wanted.Key;
                int wantedCount = wanted.Value;
                int scannedCount = scannedProducts.ContainsKey(typeGrade) ? scannedProducts[typeGrade] : 0;

                Debug.Log($"  • {typeGrade}: 필요 {wantedCount}개, 스캔 {scannedCount}개");

                if (scannedCount < wantedCount)
                {
                    hasWrongProduct = true;
                    Debug.LogWarning($"[검증 실패] {typeGrade} 부족! (필요: {wantedCount}, 스캔: {scannedCount})");
                }
            }

            // 추가로 스캔된 상품이 있는지 확인
            foreach (var scanned in scannedProducts)
            {
                string typeGrade = scanned.Key;
                int scannedCount = scanned.Value;
                int wantedCount = wantedProducts.ContainsKey(typeGrade) ? wantedProducts[typeGrade] : 0;

                if (scannedCount > wantedCount)
                {
                    hasWrongProduct = true;
                    Debug.LogWarning($"[검증 실패] {typeGrade} 초과! (필요: {wantedCount}, 스캔: {scannedCount})");
                }
            }

            Debug.Log("═══════════════════════════════════");

            // 잘못된 상품이 있으면 실수 카운트 1회
            if (hasWrongProduct && MistakeManager.Instance != null)
            {
                MistakeManager.Instance.AddMistake(
                    MistakeManager.MistakeType.WrongProductInCheckout,
                    "계산대에 잘못된 상품 포함"
                );
                Debug.LogWarning("[계산 검증] 잘못된 상품이 포함되어 실수 카운트 +1");
            }
            else
            {
                Debug.Log("[계산 검증] ✅ 모든 상품이 정확합니다! (가짜 여부는 무시됨)");
            }
        }

        // 카드 결제인 경우만 여기서 이익 추가 (현금 결제는 ValidateChangeAndComplete에서 이미 처리됨)
        if (isCardPayment && POSSystem.Instance != null)
        {
            // 이익 계산
            int totalOriginalPrice = itemManager.GetTotalOriginalPrice();
            int profit = itemManager.GetTotalAmount() - totalOriginalPrice;

            POSSystem.Instance.walletMoney += profit;
            POSSystem.Instance.UpdateWalletUI();

            Debug.Log($"[계산대 - 카드결제] 사기 이익 {profit}원을 바로 지갑에 추가! (원가: {totalOriginalPrice}원, 받은금액: {itemManager.GetTotalAmount()}원)");
        }

        // 포스기 화면에 결제 완료 표시
        POSMachineDisplay posDisplay = FindFirstObjectByType<POSMachineDisplay>();
        if (posDisplay != null)
        {
            posDisplay.ShowCheckoutComplete(itemManager.GetTotalAmount());
        }

        // POS 시스템에 기록
        if (POSSystem.Instance != null)
        {
            POSSystem.Instance.AddTransaction(itemManager.GetScannedItems());
        }

        // 거스름돈 드랍 존 초기화
        ChangeMoneyDropZone changeZone = FindFirstObjectByType<ChangeMoneyDropZone>();
        if (changeZone != null)
        {
            changeZone.ClearAllMoney();
        }

        // 손님이 낸 돈 제거 및 결제 방식 텍스트 숨김
        displayManager.ClearPaymentUI();

        // 상태 초기화
        currentPaymentState = PaymentState.Scanning;
        isCardPayment = false;
        customerPaidAmount = 0;

        // 손님 존의 모든 상품 복사본 삭제
        ClearCustomerZone();

        // 계산대 정리
        ClearCounter();

        // CustomerManager에게 완료 알림
        if (CustomerManager.Instance != null)
        {
            CustomerManager.Instance.OnPaymentComplete();
        }

        isCustomerWaiting = false;
        currentCustomer = null;

        // 버튼 비활성화
        if (processPaymentButton != null)
        {
            processPaymentButton.interactable = false;
        }
    }

    /// <summary>
    /// 손님 존의 모든 상품 복사본 삭제
    /// </summary>
    void ClearCustomerZone()
    {
        if (customerZone == null)
        {
            Debug.LogWarning("[계산대] CustomerZone이 설정되지 않았습니다!");
            return;
        }

        // CustomerZone에게 삭제 요청
        customerZone.ClearAllProducts();
    }

    void ClearCounter()
    {
        displayManager.ClearAllDisplayedItems();
        itemManager.ClearAllItems();
        UpdateTotalDisplay();

        // 스캔 존 초기화
        if (BarcodeScanner.Instance != null)
        {
            BarcodeScanner.Instance.ResetScannedProducts();
        }

        Debug.Log("[계산대] 계산대 정리 완료");
    }

    void UpdateTotalDisplay()
    {
        onTotalChanged?.Invoke(itemManager.GetTotalAmount());
    }

    // === 외부에서 호출하는 메서드들 ===

    public void OnCustomerArrived(Customer customer)
    {
        currentCustomer = customer;
        isCustomerWaiting = true;

        // 버튼 활성화
        if (processPaymentButton != null)
        {
            processPaymentButton.interactable = true;
        }

        Debug.Log("[계산대] 손님 대기 중!");
        Debug.Log("═══════════════════════════════════");
        Debug.Log("📋 게임 플레이:");
        Debug.Log("  1. 진열대 상품을 스캔 존으로 드래그");
        Debug.Log("  2. 스캔된 상품을 손님 존으로 드래그");
        Debug.Log("  3. 모든 상품 스캔 완료 후 C키로 결제");
        Debug.Log("═══════════════════════════════════");

        // 스캔 존 초기화
        if (BarcodeScanner.Instance != null)
        {
            BarcodeScanner.Instance.ResetScannedProducts();
        }
    }

    public void AddScannedItem(ProductInteractable product)
    {
        if (!isCustomerWaiting)
        {
            Debug.LogWarning("[계산대] 아직 손님이 없습니다!");
            return;
        }

        itemManager.AddItem(product);
        UpdateTotalDisplay();

        Debug.Log($"[계산대] {product.productData.productName} 스캔 - 현재 총액: {itemManager.GetTotalAmount()}원");
    }

    public void AddSimplePrice(int price)
    {
        if (!isCustomerWaiting)
        {
            Debug.LogWarning("[계산대] 아직 손님이 없습니다!");
            return;
        }

        itemManager.AddSimplePrice(price);
        UpdateTotalDisplay();

        Debug.Log($"[계산대] 간단 바코드 스캔 - {price}원 추가, 현재 총액: {itemManager.GetTotalAmount()}원");
    }

    public int GetTotalAmount()
    {
        return itemManager.GetTotalAmount();
    }

    /// <summary>
    /// 손님이 화나서 나갔을 때 호출 (시간 초과 또는 사기 한계 초과)
    /// </summary>
    public void OnCustomerLeftAngry()
    {
        Debug.Log("[계산대] 손님이 화나서 나갔습니다! 계산대를 정리합니다.");

        // 손님 존의 상품들 정리
        ClearCustomerZone();

        // 계산대 정리
        ClearCounter();

        // 상태 초기화
        currentPaymentState = PaymentState.Scanning;
        isCardPayment = false;
        customerPaidAmount = 0;
        isCustomerWaiting = false;
        currentCustomer = null;

        // 결제 UI 정리
        displayManager.ClearPaymentUI();

        // 버튼 비활성화
        if (processPaymentButton != null)
        {
            processPaymentButton.interactable = false;
        }
    }

    // 구버전 호환용 (사용 안함)
    public void DisplayCustomerItems(List<ProductInteractable> products)
    {
        Debug.Log("[계산대] 손님이 도착했습니다.");
    }
}