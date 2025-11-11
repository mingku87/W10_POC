using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 거스름돈 드랍 존 (손님 구역에 배치)
/// </summary>
public class ChangeMoneyDropZone : MonoBehaviour, IDropHandler
{
    [Header("UI References")]
    public TextMeshProUGUI totalChangeText; // 총 거스름돈 표시

    private int totalChangeMoney = 0; // 현재 건넨 거스름돈 총액
    private int totalRealMoney = 0; // 진짜 돈만
    private int totalFakeMoney = 0; // 가짜 돈만
    private List<DraggableMoney> droppedMoneys = new List<DraggableMoney>(); // 드랍된 돈 리스트

    void Start()
    {
        UpdateDisplay();
    }

    // IDropHandler 인터페이스 구현
    public void OnDrop(PointerEventData eventData)
    {
        // DraggableMoney에서 이미 OnMoneyDropped를 호출하므로 여기서는 아무것도 안함
        // (중복 호출 방지)
    }

    public void OnMoneyDropped(DraggableMoney money)
    {
        totalChangeMoney += money.moneyAmount;
        droppedMoneys.Add(money);

        // 진짜/가짜 구분
        if (money.isFakeMoney)
        {
            totalFakeMoney += money.moneyAmount;
        }
        else
        {
            totalRealMoney += money.moneyAmount;
        }

        Debug.Log($"[거스름돈] {money.moneyAmount}원 놓음 (총: {totalChangeMoney}원, 진짜: {totalRealMoney}원, 가짜: {totalFakeMoney}원)");
        UpdateDisplay();
    }

    // 계산 완료 시 호출 - 모든 거스름돈 제거
    public void ClearAllMoney()
    {
        foreach (var money in droppedMoneys)
        {
            if (money != null)
            {
                Destroy(money.gameObject);
            }
        }
        droppedMoneys.Clear();
        totalChangeMoney = 0;
        totalRealMoney = 0;
        totalFakeMoney = 0;
        UpdateDisplay();
        Debug.Log("[거스름돈] 모든 거스름돈 제거됨 (계산 완료)");
    }

    public void ResetChange()
    {
        ClearAllMoney();
    }

    public int GetTotalChange()
    {
        return totalChangeMoney;
    }

    public int GetTotalRealMoney()
    {
        return totalRealMoney;
    }

    public int GetTotalFakeMoney()
    {
        return totalFakeMoney;
    }

    void UpdateDisplay()
    {
        if (totalChangeText != null)
        {
            // 필요한 거스름돈 계산
            int requiredChange = 0;
            if (CheckoutCounter.Instance != null && CheckoutCounter.Instance.isCustomerWaiting)
            {
                requiredChange = CheckoutCounter.Instance.customerPaidAmount - CheckoutCounter.Instance.GetTotalAmount();
            }

            // "현재/필요" 형식으로 표시
            if (requiredChange > 0)
            {
                totalChangeText.text = $"거스름돈: {totalChangeMoney}/{requiredChange}원";
            }
            else
            {
                totalChangeText.text = $"거스름돈: {totalChangeMoney}원";
            }
        }
    }
}
