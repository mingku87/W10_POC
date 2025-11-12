using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 개별 손님 AI - 3가지 타입 지원
/// 1. 멀쩡한 손님 (80%)
/// 2. 휴대폰 보는 손님 (멀쩡한 손님이 계산대에서 변함)
/// 3. 취한 손님 (20%)
/// </summary>
public class Customer : MonoBehaviour
{
    public enum CustomerType
    {
        Normal,      // 멀쩡한 손님
        OnPhone,     // 휴대폰 보는 손님
        Drunk        // 취한 손님
    }

    public CustomerManager manager; // 매니저 참조

    [Header("손님 타입")]
    public CustomerType customerType = CustomerType.Normal;
    public Image customerImage; // UI Image (SpriteRenderer 대신)



    [Header("상태")]
    public bool isWaiting = true;           // 쇼핑 중
    public bool readyForCheckout = false;   // 계산대 준비 완료
    public bool isOnPhone = false;          // 휴대폰 보는 중 (스캔 감지 불가)

    [Header("선택한 상품들")]
    public List<ProductInteractable> selectedProducts = new List<ProductInteractable>();

    [Header("시간 제한")]
    public Image timeGaugeImage;            // 시간 게이지 이미지
    public float checkoutTimeLimit = 30f;   // 계산 제한 시간 (일반 손님 기본값)
    private float remainingTime;            // 남은 시간
    private bool isTimerActive = false;     // 타이머 활성화 여부

    [Header("사기 한계")]
    public float fraudToleranceMin = 0.2f;  // 사기 한계 최소 (20%)
    public float fraudToleranceMax = 0.3f;  // 사기 한계 최대 (30%)
    private float currentFraudTolerance;    // 현재 손님의 사기 한계

    private float shoppingTime = 5f; // 쇼핑 시간
    private RectTransform rectTransform;
    private Vector2 checkoutPosition = new Vector2(100f, -200f); // UI 좌표

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        // UI Image 가져오기
        if (customerImage == null)
        {
            customerImage = GetComponent<Image>();
        }

        // 시간 게이지 이미지 초기화
        if (timeGaugeImage != null)
        {
            timeGaugeImage.fillAmount = 1f; // 처음엔 가득 참
            timeGaugeImage.gameObject.SetActive(false); // 계산대 도착 전까지 숨김
        }

        // 손님 타입에 따라 시간 제한 및 사기 한계 설정
        if (customerType == CustomerType.Drunk)
        {
            checkoutTimeLimit = Random.Range(50f, 70f); // 취객: 50~70초
            fraudToleranceMin = 0.7f; // 70%
            fraudToleranceMax = 0.8f; // 80%
        }
        else
        {
            checkoutTimeLimit = Random.Range(25f, 35f); // 일반 손님: 25~35초
            fraudToleranceMin = 0.2f; // 20%
            fraudToleranceMax = 0.3f; // 30%
        }

        // 현재 손님의 사기 한계를 랜덤하게 설정
        currentFraudTolerance = Random.Range(fraudToleranceMin, fraudToleranceMax);

        Debug.Log($"[손님] 타입: {customerType}, 시간제한: {checkoutTimeLimit:F1}초, 사기한계: {currentFraudTolerance:P0}");

        // 타입에 따라 스프라이트 설정 (CustomerManager에서 이미 설정됨)
        StartCoroutine(ShoppingRoutine());
    }

    IEnumerator ShoppingRoutine()
    {
        Debug.Log($"[손님] 입장! 타입: {customerType}");

        // 쇼핑 시간 대기
        yield return new WaitForSeconds(shoppingTime);

        // 모든 상품을 확인하고 구매 결정
        SelectProducts();

        // ✨ 선택한 상품 목록 콘솔 출력
        PrintShoppingList();

        // 계산대로 이동
        yield return StartCoroutine(MoveToCheckout());

        Debug.Log($"[손님] 계산대 도착! 선택한 상품: {selectedProducts.Count}개");
        readyForCheckout = true;
        isWaiting = false;

        // 시간 제한 타이머 시작
        remainingTime = checkoutTimeLimit;
        isTimerActive = true;
        if (timeGaugeImage != null)
        {
            timeGaugeImage.gameObject.SetActive(true); // 게이지 표시
        }
        StartCoroutine(CheckoutTimerRoutine());

        // 멀쩡한 손님은 가끔 휴대폰을 봄
        if (customerType == CustomerType.Normal)
        {
            StartCoroutine(RandomlyUsePhone());
        }

        // 매니저에게 계산 대기 알림
        if (manager != null)
        {
            manager.OnCustomerReadyForCheckout(this);
        }

        // 계산 완료까지 대기 (매니저가 OnPaymentComplete 호출할 때까지)
    }

    IEnumerator RandomlyUsePhone()
    {
        // 계산대 대기 중 랜덤하게 휴대폰을 봄
        while (readyForCheckout)
        {
            yield return new WaitForSeconds(Random.Range(1f, 3f)); // 1~3초마다 체크

            if (readyForCheckout && Random.value < 0.7f) // 70% 확률
            {
                // 휴대폰 보기 시작
                isOnPhone = true;
                customerType = CustomerType.OnPhone;

                // 스프라이트 변경
                if (manager != null && customerImage != null)
                {
                    customerImage.sprite = manager.onPhoneSprite;
                }

                Debug.Log("[손님] 휴대폰 보는 중... (스캔 감지 안됨)");

                // 3~6초간 휴대폰 봄
                yield return new WaitForSeconds(Random.Range(3f, 6f));

                // 휴대폰 그만 봄
                isOnPhone = false;
                customerType = CustomerType.Normal;

                if (manager != null && customerImage != null)
                {
                    customerImage.sprite = manager.normalSprite;
                }

                Debug.Log("[손님] 휴대폰 그만 봄 (다시 정상)");
            }
        }
    }

    IEnumerator CheckoutTimerRoutine()
    {
        while (isTimerActive && remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;

            // 시간 게이지 업데이트 (1 = 가득참, 0 = 비어있음)
            if (timeGaugeImage != null)
            {
                timeGaugeImage.fillAmount = remainingTime / checkoutTimeLimit;
            }

            yield return null;
        }

        // 시간 초과 시
        if (isTimerActive && remainingTime <= 0)
        {
            Debug.Log($"[손님] 시간 초과! 화나서 나갑니다. (제한시간: {checkoutTimeLimit:F1}초)");
            LeaveAngry();
        }
    }

    /// <summary>
    /// 사기 한계를 초과했는지 체크 (매 스캔마다 호출)
    /// </summary>
    public bool CheckFraudLimit(int scannedTotal)
    {
        int actualTotal = GetTotalPrice();

        if (actualTotal == 0)
        {
            return true; // 상품이 없으면 체크 안함
        }

        // 과금 비율 계산
        float overchargeRatio = (float)(scannedTotal - actualTotal) / actualTotal;

        Debug.Log($"[손님] 과금 체크 - 실제: {actualTotal}원, 스캔: {scannedTotal}원, 비율: {overchargeRatio:P1}, 한계: {currentFraudTolerance:P0}");

        // 사기 한계 초과 시
        if (overchargeRatio > currentFraudTolerance)
        {
            Debug.Log($"[손님] 사기 한계 초과! 화나서 나갑니다. (과금 {overchargeRatio:P1} > 한계 {currentFraudTolerance:P0})");
            LeaveAngry();
            return false;
        }

        return true;
    }

    void LeaveAngry()
    {
        // 타이머 중지
        isTimerActive = false;
        readyForCheckout = false;

        // 실수 카운트 증가
        if (POSSystem.Instance != null)
        {
            POSSystem.Instance.AddMistake();
            Debug.Log("[손님] 손님이 화나서 나감 - 실수 카운트 증가!");
        }

        // 계산대 정리 요청
        if (CheckoutCounter.Instance != null)
        {
            CheckoutCounter.Instance.OnCustomerLeftAngry();
        }

        // 매니저에게 알림
        if (manager != null)
        {
            manager.OnCustomerLeftAngry(this);
        }

        // 퇴장
        Debug.Log("[손님] 화나서 퇴장!");
        Destroy(gameObject, 0.5f);
    }

    void SelectProducts()
    {
        ProductInteractable[] allProducts = FindObjectsByType<ProductInteractable>(FindObjectsSortMode.None);

        foreach (ProductInteractable product in allProducts)
        {
            int originalPrice = product.productData.originalPrice;
            int currentPrice = product.GetCurrentPrice();

            // 가격 차이에 따른 구매 확률 계산
            float purchaseProbability = CalculatePurchaseProbability(originalPrice, currentPrice);

            // 확률에 따라 구매 결정
            if (Random.value <= purchaseProbability)
            {
                selectedProducts.Add(product);
                Debug.Log($"[손님] {product.productData.productName} 선택! (원가: {originalPrice}원, 현재: {currentPrice}원, 확률: {purchaseProbability:P0})");
            }
            else
            {
                Debug.Log($"[손님] {product.productData.productName} 패스 (가격이 {currentPrice - originalPrice}원 비쌈, 확률: {purchaseProbability:P0})");
            }
        }

        if (selectedProducts.Count == 0)
        {
            Debug.Log("[손님] 아무것도 안 샀어요... (가격이 다 비싸요!)");
        }
    }

    /// <summary>
    /// 손님이 선택한 상품 목록을 콘솔에 명확하게 출력
    /// </summary>
    void PrintShoppingList()
    {
        if (selectedProducts.Count == 0)
        {
            Debug.Log("═══════════════════════════════════");
            Debug.Log("🛒 손님의 쇼핑 목록: 없음");
            Debug.Log("═══════════════════════════════════");
            // ✨ [수정] 싱글톤 인스턴스 사용
            if (CustomerUI.Instance != null)
            {
                CustomerUI.Instance.UpdateShoppingList(selectedProducts);
            }
            return;
        }

        // 상품 이름별로 그룹화하여 개수 세기
        var groupedProducts = selectedProducts
            .GroupBy(p => p.productData.productName)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderBy(g => g.Name);

        Debug.Log("═══════════════════════════════════");
        Debug.Log($"🛒 손님의 쇼핑 목록 (총 {selectedProducts.Count}개)");
        Debug.Log("───────────────────────────────────");

        foreach (var item in groupedProducts)
        {
            Debug.Log($"  • {item.Name} x {item.Count}");
        }

        Debug.Log("═══════════════════════════════════");
        Debug.Log("💡 진열대에서 해당 상품들을 찾아 스캐너로 드래그하세요!");
        Debug.Log("═══════════════════════════════════");


        // ✨ [수정] 싱글톤 인스턴스 사용
        if (CustomerUI.Instance != null)
        {
            // CustomerUI의 함수를 호출하여 UI를 업데이트
            CustomerUI.Instance.UpdateShoppingList(selectedProducts);
        }
        else
        {
            // UI가 할당되지 않았을 경우 경고
            Debug.LogWarning("CustomerUI.Instance가 존재하지 않습니다. 씬에 CustomerUI가 포함된 오브젝트가 있는지 확인하세요.");
        }
    }

    float CalculatePurchaseProbability(int originalPrice, int currentPrice)
    {
        // 기본 구매 확률 60%
        float baseProbability = 0.6f;

        if (currentPrice <= originalPrice)
        {
            // 원가 이하면 구매 확률 증가
            return Mathf.Min(baseProbability + 0.3f, 0.95f);
        }
        else
        {
            // 가격이 높을수록 구매 확률 감소
            int priceDiff = currentPrice - originalPrice;
            float priceRatio = (float)priceDiff / originalPrice;

            // 10% 비싸면 -10% 확률, 50% 비싸면 -30% 확률
            float penalty = priceRatio * 0.6f;
            float finalProbability = Mathf.Max(baseProbability - penalty, 0.05f);

            return finalProbability;
        }
    }

    IEnumerator MoveToCheckout()
    {
        Debug.Log("[손님] 계산대로 이동 중...");

        float moveTime = 2f;
        Vector2 startPos = rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveTime;
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, checkoutPosition, t);
            yield return null;
        }

        rectTransform.anchoredPosition = checkoutPosition;

        // 계산대에 손님 도착 알림
        if (CheckoutCounter.Instance != null)
        {
            CheckoutCounter.Instance.OnCustomerArrived(this);
            Debug.Log("[손님] 계산대 대기 중 - 진열대에서 상품을 찾아 스캐너로 드래그하세요!");
        }
    }

    public int GetTotalPrice()
    {
        int total = 0;
        foreach (var product in selectedProducts)
        {
            total += product.GetCurrentPrice();
        }
        return total;
    }

    public void Leave()
    {
        // 타이머 중지
        isTimerActive = false;

        Debug.Log("[손님] 퇴장합니다");
        if (CustomerUI.Instance != null)
        {
            CustomerUI.Instance.HideUI();
        }
        Destroy(gameObject, 0.5f);
    }
}