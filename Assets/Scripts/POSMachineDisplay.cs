using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 실제 편의점 포스기 화면 - 현재 금액 표시
/// </summary>
public class POSMachineDisplay : MonoBehaviour
{
    [Header("POS Screen")]
    public TextMeshProUGUI currentPriceText; // 현재 스캔 중인 금액
    public TextMeshProUGUI statusText; // 상태 표시
    public GameObject drawerPanel; // 현금 서랍 패널 (진짜 돈 - 현금통 버튼으로 토글)
    public Button cashDrawerButton; // 현금통 버튼 (인스펙터에서 할당)
    public Button posMenuButton; // POS 거래내역 버튼

    private int currentDisplayAmount = 0;

    void Start()
    {
        // 현금통 버튼 이벤트 연결
        if (cashDrawerButton != null)
        {
            cashDrawerButton.onClick.RemoveAllListeners();
            cashDrawerButton.onClick.AddListener(ToggleCashDrawer);
            Debug.Log("[POSMachineDisplay] 현금통 버튼 연결됨!");
        }
        else
        {
            Debug.LogWarning("[POSMachineDisplay] cashDrawerButton이 할당되지 않았습니다!");
        }

        // CheckoutCounter의 이벤트 구독
        CheckoutCounter counter = FindFirstObjectByType<CheckoutCounter>();
        if (counter != null)
        {
            // CheckoutCounter가 금액을 업데이트할 때마다 포스기 화면도 업데이트
            counter.onTotalChanged += UpdateDisplay;
        }

        // POS 메뉴 버튼 이벤트 연결
        if (posMenuButton != null)
        {
            posMenuButton.onClick.AddListener(() =>
            {
                if (POSSystem.Instance != null)
                {
                    POSSystem.Instance.TogglePOS();
                }
            });
        }

        UpdateDisplay(0);
    }

    void ToggleCashDrawer()
    {
        if (drawerPanel != null)
        {
            drawerPanel.SetActive(!drawerPanel.activeSelf);
            Debug.Log($"[POSMachineDisplay] 캐시 서랍 토글: {drawerPanel.activeSelf}");
        }
        else
        {
            Debug.LogWarning("[POSMachineDisplay] drawerPanel이 할당되지 않았습니다!");
        }
    }

    // OpenCashDrawer는 GameSetupMaster에서 직접 토글 방식으로 처리되므로 사용 안함
    /*
    void OpenCashDrawer()
    {
        Debug.Log("현금 서랍 열림!");

        if (drawerPanel != null)
        {
            drawerPanel.SetActive(true);
            Debug.Log("진짜 돈 서랍 활성화");

            // 진짜 돈 생성
            Transform realMoneyArea = drawerPanel.transform.Find("RealMoneyArea");
            if (realMoneyArea != null && moneyPrefab != null)
            {
                CreateMoneyInDrawer(realMoneyArea, moneyPrefab, false);
            }
        }

        // 가짜 돈통은 항상 표시되므로 여기서 처리하지 않음
    }
    */

    // CreateMoneyInDrawer 함수는 사용하지 않음 - 인스펙터에서 돈 UI 할당
    /*
    void CreateMoneyInDrawer(Transform parent, GameObject prefab, bool isFake)
    {
        Debug.Log($"CreateMoneyInDrawer 호출! isFake: {isFake}");

        // 기존 돈 삭제
        foreach (Transform child in parent)
        {
            if (child.GetComponent<DraggableMoney>() != null)
            {
                Destroy(child.gameObject);
            }
        }

        // 6종류 돈
        string[] moneyLabels = new string[] { "100원", "500원", "1000원", "5000원", "10000원", "50000원" };
        int[] moneyValues = new int[] { 100, 500, 1000, 5000, 10000, 50000 };

        for (int i = 0; i < moneyLabels.Length; i++)
        {
            GameObject moneyObj = Instantiate(prefab, parent);
            Debug.Log($"돈 생성: {moneyLabels[i]}");

            // 텍스트 제거 - 이미지에 금액 표시됨

            DraggableMoney draggable = moneyObj.GetComponent<DraggableMoney>();
            if (draggable != null)
            {
                draggable.moneyAmount = moneyValues[i];
                draggable.isFakeMoney = isFake;
            }

            // 색상은 하얀색 (스프라이트 원본 유지)
            Image bg = moneyObj.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = Color.white;
            }
        }
    }
    */

    public void UpdateDisplay(int amount)
    {
        currentDisplayAmount = amount;

        if (currentPriceText != null)
        {
            currentPriceText.text = $"{amount}원";
        }

        if (statusText != null)
        {
            // CheckoutCounter 상태 확인
            if (CheckoutCounter.Instance != null)
            {
                var state = CheckoutCounter.Instance.currentPaymentState;

                if (state == CheckoutCounter.PaymentState.WaitingPayment)
                {
                    if (CheckoutCounter.Instance.isCardPayment)
                    {
                        statusText.text = "카드 결제\n[ C키로 승인 ]";
                    }
                    else
                    {
                        statusText.text = "현금 결제\n돈을 받으세요";
                    }
                }
                else if (state == CheckoutCounter.PaymentState.WaitingChange)
                {
                    int change = CheckoutCounter.Instance.customerPaidAmount - amount;
                    statusText.text = $"거스름돈: {change}원\n[ C키로 완료 ]";
                }
                else if (amount > 0)
                {
                    statusText.text = "스캔 중...\n[ C키로 계산 ]";
                }
                else
                {
                    statusText.text = "대기 중";
                }
            }
            else
            {
                if (amount > 0)
                {
                    statusText.text = "스캔 중...\n[ C키로 계산 ]";
                }
                else
                {
                    statusText.text = "대기 중";
                }
            }
        }
    }

    public void ShowCheckoutComplete(int finalAmount)
    {
        if (statusText != null)
        {
            statusText.text = $"결제 완료: {finalAmount}원";
        }

        // 2초 후 초기화
        Invoke("ResetDisplay", 2f);
    }

    void ResetDisplay()
    {
        UpdateDisplay(0);
    }
}
