using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 계산대 메인 컨트롤러 - 상태 관리 및 결제 흐름 제어
/// </summary>
public class CheckoutCounter : MonoBehaviour
{
    public static CheckoutCounter Instance { get; private set; }

    [Header("계산대 위치")]
    public Transform counterPosition;
    public float itemSpacing = 1.5f;

    [Header("UI")]
    public TextMeshProUGUI totalAmountText;
    public GameObject checkoutButton;

    // 서브 매니저들
    private CheckoutDisplayManager displayManager;
    private CheckoutItemManager itemManager;

    // 상태
    public bool isCustomerWaiting = false;
    private Customer currentCustomer = null;

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
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C) && isCustomerWaiting)
        {
            HandleCheckoutInput();
        }
    }

    void HandleCheckoutInput()
    {
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

        // 이익 계산
        int totalOriginalPrice = itemManager.GetTotalOriginalPrice();
        int profit = itemManager.GetTotalAmount() - totalOriginalPrice;

        // 지갑에 이익만큼 돈 추가
        if (POSSystem.Instance != null)
        {
            POSSystem.Instance.walletMoney += profit;
            POSSystem.Instance.UpdateWalletUI();

            Debug.Log($"[계산대] 이익 {profit}원을 지갑에 추가! (원가: {totalOriginalPrice}원, 받은금액: {itemManager.GetTotalAmount()}원)");
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
        displayManager.ClearAllDisplayedItems();
        itemManager.ClearAllItems();
        UpdateTotalDisplay();

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

        Debug.Log("[계산대] 손님 대기 중 - 손님이 가져온 상품을 계산대에 표시합니다!");

        // 손님이 선택한 상품들을 계산대에 표시
        foreach (var product in customer.selectedProducts)
        {
            displayManager.DisplayScannedItem(product);
        }

        Debug.Log($"[계산대] 손님이 가져온 상품 {customer.selectedProducts.Count}개 표시 완료! 스캐너로 스캔하세요!");

        // 스캐너 초기화
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

    // 구버전 호환용 (사용 안함)
    public void DisplayCustomerItems(List<ProductInteractable> products)
    {
        Debug.Log("[계산대] 손님이 도착했습니다. 스캐너로 상품을 스캔하세요!");
    }
}