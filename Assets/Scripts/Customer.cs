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
/// 가짜 제품(isFake=true)은 주문하지 않음
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

    [Header("디버그 설정")]
    [Tooltip("디버그 모드: 모든 아이템을 1개씩 구매")]
    public bool isDebugMode = false;

    [Header("상태")]
    public bool isWaiting = true;           // 쇼핑 중
    public bool readyForCheckout = false;   // 계산대 준비 완료
    public bool isOnPhone = false;          // 휴대폰 보는 중 (스캔 감지 불가)

    [Header("선택한 상품들")]
    public List<ProductInteractable> selectedProducts = new List<ProductInteractable>();

    [Header("시간 제한")]
    public Image timeGaugeImage;            // 시간 게이지 이미지
    public float checkoutTimeLimit = 60f;   // 계산 제한 시간 (일반 손님 기본값)
    private float remainingTime;            // 남은 시간
    private bool isTimerActive = false;     // 타이머 활성화 여부

    [Header("사기 한계")]
    public float fraudToleranceMin = 0.2f;  // 사기 한계 최소 (20%)
    public float fraudToleranceMax = 0.3f;  // 사기 한계 최대 (30%)
    private float currentFraudTolerance;    // 현재 손님의 사기 한계

    [Header("수상함 감지")]
    private float suspicionTimePenalty = 20f; // 수상한 행동 시 시간 제한 감소량 (초)

    // 브랜드 변경 목격 시 대사 목록
    private static readonly string[] brandChangeDialogues = new string[]
    {
        "지금 뭐하시는 건가요?",
        "뭐 하는 짓이세요?",
        "어? 방금 뭐 하셨어요?",
        "야, 지금 나한테 사기치는 거야?",
        "뭔가 수상한데요?",
        "야! 뭐하는 거야!",
        "이거 바꾸셨죠?",
        "라벨 바꾸는거 봤는데요?"
    };

    // 중복 스캔 목격 시 대사 목록
    private static readonly string[] duplicateScanDialogues = new string[]
    {
        "그거 이미 찍으셨잖아요?",
        "아까 찍었던건데요?",
        "두 번 찍으시는거예요?",
        "이거 중복 아니에요?",
        "방금 찍었는데 또 찍어요?",
        "같은거 두번 찍으시네요?",
        "어? 이거 아까 찍었는데?",
        "왜 두 번 찍으세요?"
    };

    // 금액 초과 시 대사 목록
    private static readonly string[] overchargeDialogues = new string[]
    {
        "왜 이렇게 비싸??",
        "이거 금액 이상한데요?",
        "야! 너무 비싼거 아냐?",
        "이게 무슨 금액이야!",
        "사기치지 마세요!",
        "말도 안되는 금액인데?",
        "아니 이게 얼마야??",
        "장난해? 가격이 왜 이래?"
    };

    [Header("이동 위치")]
    private Vector2 spawnPos;               // 스폰 위치 (입장/퇴장 위치)
    private Vector2 enterPos;               // 입장 후 쇼핑 위치

    private float shoppingTime = 5f; // 쇼핑 시간
    private RectTransform rectTransform;

    /// <summary>
    /// 매니저에서 위치 정보를 설정할 때 호출
    /// </summary>
    public void SetPositions(Vector2 spawn, Vector2 enter)
    {
        spawnPos = spawn;
        enterPos = enter;
    }

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

        // ✨ 디버그 모드에서는 시간 제한과 사기 한계를 넉넉하게 설정
        if (isDebugMode)
        {
            checkoutTimeLimit = 120f; // 2분
            fraudToleranceMin = 0.95f; // 95%
            fraudToleranceMax = 0.99f; // 99%
            Debug.Log("[손님] 🔧 디버그 모드: 시간 제한 120초, 사기 한계 95~99%");
        }
        // 손님 타입에 따라 시간 제한 및 사기 한계 설정
        else if (customerType == CustomerType.Drunk)
        {
            checkoutTimeLimit = Random.Range(50f, 70f); // 취객: 50~70초
            fraudToleranceMin = 2.5f; // 250%
            fraudToleranceMax = 3.0f; // 300%
        }
        else
        {
            checkoutTimeLimit = Random.Range(40f, 50f); // 일반 손님: 40~50초
            fraudToleranceMin = 0.8f; // 80%
            fraudToleranceMax = 1.0f; // 100%
        }

        // 현재 손님의 사기 한계를 랜덤하게 설정
        currentFraudTolerance = Random.Range(fraudToleranceMin, fraudToleranceMax);

        //Debug.Log($"[손님] 타입: {customerType}, 시간제한: {checkoutTimeLimit:F1}초, 사기한계: {currentFraudTolerance:P0}");

        // 입장 애니메이션 시작
        StartCoroutine(EnterRoutine());
    }

    IEnumerator EnterRoutine()
    {
        // 스폰 위치에서 입장 위치로 이동
        //Debug.Log("[손님] 입장 중...");
        yield return StartCoroutine(MoveToPosition(enterPos, 2f));
        //Debug.Log("[손님] 입장 완료!");

        MistakeManager.Instance?.ClearText();


        // 타입에 따라 스프라이트 설정 (CustomerManager에서 이미 설정됨)
        StartCoroutine(ShoppingRoutine());
    }

    IEnumerator ShoppingRoutine()
    {
        //Debug.Log($"[손님] 입장! 타입: {customerType}");

        // 쇼핑 시간 대기 (1초로 단축)
        yield return new WaitForSeconds(1f);

        // 모든 상품을 확인하고 구매 결정
        SelectProducts();

        // ✨ 선택한 상품 목록 콘솔 출력
        PrintShoppingList();

        // 계산대 도착 (이동 없이 바로 처리)
        //Debug.Log($"[손님] 계산대 대기! 선택한 상품: {selectedProducts.Count}개");
        readyForCheckout = true;
        isWaiting = false;

        // 계산대에 손님 도착 알림
        if (CheckoutCounter.Instance != null)
        {
            CheckoutCounter.Instance.OnCustomerArrived(this);
            //Debug.Log("[손님] 계산대 대기 중 - 진열대에서 상품을 찾아 스캐너로 드래그하세요!");
        }

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


                // 1~2초간 휴대폰 봄
                yield return new WaitForSeconds(Random.Range(1f, 2f));

                // 휴대폰 그만 봄
                isOnPhone = false;
                customerType = CustomerType.Normal;

                if (manager != null && customerImage != null)
                {
                    customerImage.sprite = manager.normalSprite;
                }

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
                float fillRatio = remainingTime / checkoutTimeLimit;
                timeGaugeImage.fillAmount = fillRatio;

                // 게이지 색상 변경: 초록(100%) -> 노랑(50%) -> 빨강(0%)
                if (fillRatio > 0.5f)
                {
                    // 초록 -> 노랑 (100% ~ 50%)
                    timeGaugeImage.color = Color.Lerp(Color.yellow, Color.green, (fillRatio - 0.5f) * 2f);
                }
                else
                {
                    // 노랑 -> 빨강 (50% ~ 0%)
                    timeGaugeImage.color = Color.Lerp(Color.red, Color.yellow, fillRatio * 2f);
                }
            }

            yield return null;
        }

        // 시간 초과 시
        if (isTimerActive && remainingTime <= 0)
        {
            Debug.Log($"[손님 퇴장] ⏰ 시간 초과로 화나서 나감! (제한시간: {checkoutTimeLimit:F1}초 초과)");
            LeaveAngry("시간 초과");
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
        int overchargeAmount = scannedTotal - actualTotal;

        //Debug.Log($"[손님] 과금 체크 - 실제: {actualTotal}원, 스캔: {scannedTotal}원, 비율: {overchargeRatio:P1}, 한계: {currentFraudTolerance:P0}");

        // 사기 한계 초과 시
        if (overchargeRatio > currentFraudTolerance)
        {
            // 금액 초과 대사 표시
            string dialogue = overchargeDialogues[Random.Range(0, overchargeDialogues.Length)];
            if (MistakeManager.Instance != null)
            {
                MistakeManager.Instance.ShowCustomerDialogue(dialogue);
            }

            Debug.Log($"[손님 퇴장] 💰 금액 초과로 화나서 나감! (과금 {overchargeAmount}원 = {overchargeRatio:P1} > 한계 {currentFraudTolerance:P0})");
            LeaveAngry($"금액 초과 ({overchargeAmount}원 초과, {overchargeRatio:P1})");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 수상한 행동 감지 시 호출 (같은 상품 두 번 스캔, 가짜 상품 생성 등)
    /// 휴대폰 보는 중이 아닌 손님만 시간 제한 감소
    /// </summary>
    public void OnSuspiciousBehaviorDetected(string behaviorType)
    {
        if (!isTimerActive) return; // 타이머가 꺼져있으면 무시

        // 취객은 사기 행위를 감지하지 못함
        if (customerType == CustomerType.Drunk)
        {
            //Debug.Log("[손님] 🍺 취객이라 수상한 행동 감지 못함");
            return;
        }

        // 휴대폰 보는 중이면 감지하지 않음
        if (isOnPhone)
        {
            //Debug.Log("[손님] 📱 휴대폰 보는 중이라 수상한 행동 감지 못함");
            return;
        }

        // 상황에 맞는 대사 선택 및 표시
        ShowSuspiciousDialogue(behaviorType);

        // 수상함 감지 → 시간 제한 감소
        remainingTime -= suspicionTimePenalty;

        //Debug.Log($"[손님] 👀 수상한 행동 감지! ({behaviorType}) - 시간 제한 {suspicionTimePenalty}초 감소 (남은시간: {remainingTime:F1}초)");

        // 시간 게이지 업데이트
        if (timeGaugeImage != null && checkoutTimeLimit > 0)
        {
            float fillRatio = Mathf.Clamp01(remainingTime / checkoutTimeLimit);
            timeGaugeImage.fillAmount = fillRatio;

            // 게이지 색상 변경: 초록(100%) -> 노랑(50%) -> 빨강(0%)
            if (fillRatio > 0.5f)
            {
                // 초록 -> 노랑 (100% ~ 50%)
                timeGaugeImage.color = Color.Lerp(Color.yellow, Color.green, (fillRatio - 0.5f) * 2f);
            }
            else
            {
                // 노랑 -> 빨강 (50% ~ 0%)
                timeGaugeImage.color = Color.Lerp(Color.red, Color.yellow, fillRatio * 2f);
            }
        }

        // 시간이 0 이하로 떨어지면 즉시 화내고 나감
        if (remainingTime <= 0)
        {
            Debug.Log($"[손님 퇴장] ⏰ 수상한 행동으로 인한 시간 초과! ({behaviorType})");
            LeaveAngry($"수상한 행동 감지 후 시간 초과 ({behaviorType})");
        }
    }

    /// <summary>
    /// 수상한 행동 감지 시 상황에 맞는 랜덤 대사 표시
    /// </summary>
    void ShowSuspiciousDialogue(string behaviorType)
    {
        string dialogue = "";

        // 상황에 따라 다른 대사 목록 선택
        if (behaviorType.Contains("브랜드") || behaviorType.Contains("변경") || behaviorType.Contains("목격"))
        {
            dialogue = brandChangeDialogues[Random.Range(0, brandChangeDialogues.Length)];
        }
        else if (behaviorType.Contains("중복") || behaviorType.Contains("스캔") || behaviorType.Contains("두 번"))
        {
            dialogue = duplicateScanDialogues[Random.Range(0, duplicateScanDialogues.Length)];
        }
        else
        {
            // 기본 대사 (브랜드 변경 대사 사용)
            dialogue = brandChangeDialogues[Random.Range(0, brandChangeDialogues.Length)];
        }

        // MistakeManager를 통해 화면에 표시
        if (MistakeManager.Instance != null)
        {
            MistakeManager.Instance.ShowCustomerDialogue(dialogue);
        }
        else
        {
            Debug.LogError("[손님 대사] MistakeManager.Instance가 null입니다!");
        }
    }
    void LeaveAngry(string reason = "알 수 없음")
    {
        // 타이머 중지
        isTimerActive = false;
        readyForCheckout = false;

        // 화난 스프라이트로 변경
        ChangeToAngrySprite();

        // 실수 카운트 증가
        if (MistakeManager.Instance != null)
        {
            MistakeManager.Instance.AddMistake(
                MistakeManager.MistakeType.CustomerTimeout,
                reason
            );
            Debug.Log($"[손님 퇴장 이유] {reason} - 실수 카운트 증가!");
        }

        // UI 숨기기
        if (CustomerUI.Instance != null)
        {
            CustomerUI.Instance.HideUI();
        }

        // 퇴장 애니메이션 시작 (화났을 때도 걸어서 나감)
        //Debug.Log("[손님] 화나서 퇴장!");
        StartCoroutine(ExitRoutineAngry());
    }

    /// <summary>
    /// 화나서 퇴장하는 루틴 (매니저 및 계산대 정리 포함)
    /// </summary>
    IEnumerator ExitRoutineAngry()
    {
        // 스폰 위치로 이동
        //Debug.Log("[손님] 퇴장 중...");
        yield return StartCoroutine(MoveToPosition(spawnPos, 2f));
        //Debug.Log("[손님] 퇴장 완료!");

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

        // 오브젝트 삭제
        Destroy(gameObject);
    }

    /// <summary>
    /// 손님을 화난 스프라이트로 변경
    /// </summary>
    void ChangeToAngrySprite()
    {
        if (customerImage == null || manager == null) return;

        // 손님 타입에 따라 화난 스프라이트 설정
        if (customerType == Customer.CustomerType.Drunk)
        {
            if (manager.angryDrunkSprite != null)
            {
                customerImage.sprite = manager.angryDrunkSprite;
                //Debug.Log("[손님] 화난 취객 스프라이트로 변경!");
            }
        }
        else // Normal 또는 OnPhone
        {
            if (manager.angryNormalSprite != null)
            {
                customerImage.sprite = manager.angryNormalSprite;
                //Debug.Log("[손님] 화난 일반 손님 스프라이트로 변경!");
            }
        }
    }

    /// <summary>
    /// 상품 선택 로직 - 가짜 제품(isFake=true)은 선택하지 않음
    /// 최대 3종류의 상품만 선택하고, 각 종류당 1~5개까지 주문
    /// ✨ 디버그 모드: 모든 아이템을 1개씩 선택
    /// </summary>
    void SelectProducts()
    {
        ProductInteractable[] allProducts = FindObjectsByType<ProductInteractable>(FindObjectsSortMode.None);

        // 가짜 제품 제외한 실제 상품만 필터링
        List<ProductInteractable> validProducts = new List<ProductInteractable>();

        foreach (ProductInteractable product in allProducts)
        {
            // 가짜 제품은 무조건 제외
            if (product.productData.isFake)
            {
                //Debug.Log($"[손님] {product.productData.productName}은 가짜 제품이므로 선택하지 않음");
                continue;
            }

            // ✨ 디버그 모드: 모든 상품을 무조건 구매 후보에 추가
            if (isDebugMode)
            {
                validProducts.Add(product);
                //Debug.Log($"[손님] 🔧 디버그: {product.productData.productName} 자동 선택!");
                continue;
            }

            // 일반 모드: 가격에 따른 구매 확률 계산
            int originalPrice = product.productData.originalPrice;
            int currentPrice = product.GetCurrentPrice();

            // 가격 차이에 따른 구매 확률 계산
            float purchaseProbability = CalculatePurchaseProbability(originalPrice, currentPrice);

            // 확률에 따라 구매 결정
            if (Random.value <= purchaseProbability)
            {
                validProducts.Add(product);
                //Debug.Log($"[손님] {product.productData.productName} 구매 후보! (원가: {originalPrice}원, 현재: {currentPrice}원, 확률: {purchaseProbability:P0})");
            }
            else
            {
                //Debug.Log($"[손님] {product.productData.productName} 패스 (가격이 {currentPrice - originalPrice}원 비쌈, 확률: {purchaseProbability:P0})");
            }
        }

        // 구매할 상품이 없으면 종료
        if (validProducts.Count == 0)
        {
            //Debug.Log("[손님] 아무것도 안 샀어요... (가격이 다 비싸요!)");
            return;
        }

        // ✨ 디버그 모드: 모든 유효 상품을 1개씩 선택
        if (isDebugMode)
        {
            foreach (ProductInteractable product in validProducts)
            {
                selectedProducts.Add(product);
                //Debug.Log($"[손님] 🔧 디버그: {product.productData.productName} x 1개 선택!");
            }
            //Debug.Log($"[손님] 🔧 디버그 모드: 총 {validProducts.Count}종류, {selectedProducts.Count}개 상품 선택 완료!");
            return;
        }

        // 일반 모드: 최대 3종류까지만 선택
        int maxProductTypes = Mathf.Min(3, validProducts.Count);
        int selectedTypesCount = Random.Range(1, maxProductTypes + 1); // 1~3종류

        // 랜덤하게 상품 종류 선택
        List<ProductInteractable> shuffledProducts = new List<ProductInteractable>(validProducts);
        for (int i = 0; i < shuffledProducts.Count; i++)
        {
            int randomIndex = Random.Range(i, shuffledProducts.Count);
            var temp = shuffledProducts[i];
            shuffledProducts[i] = shuffledProducts[randomIndex];
            shuffledProducts[randomIndex] = temp;
        }

        // 선택된 종류의 상품들을 1~3개씩 추가 (확률: 1개 50%, 2개 35%, 3개 15%)
        for (int i = 0; i < selectedTypesCount; i++)
        {
            ProductInteractable selectedProduct = shuffledProducts[i];

            // 전체 상품 개수가 5개 미만일 때만 추가
            int remainingSlots = 5 - selectedProducts.Count;
            if (remainingSlots <= 0) break; // 이미 5개 이상이면 중단

            // 개수 결정 (남은 슬롯 범위 내에서 1~3개)
            int maxQuantity = Mathf.Min(3, remainingSlots);

            float randomValue = Random.value;
            int quantity;
            if (maxQuantity >= 3)
            {
                if (randomValue < 0.5f)
                {
                    quantity = 1; // 50% 확률
                }
                else if (randomValue < 0.85f)
                {
                    quantity = 2; // 35% 확률
                }
                else
                {
                    quantity = 3; // 15% 확률
                }
            }
            else if (maxQuantity == 2)
            {
                quantity = randomValue < 0.6f ? 1 : 2;
            }
            else
            {
                quantity = 1;
            }

            for (int j = 0; j < quantity; j++)
            {
                selectedProducts.Add(selectedProduct);
            }

            //Debug.Log($"[손님] {selectedProduct.productData.productName} x {quantity}개 선택!");
        }

        //Debug.Log($"[손님] 총 {selectedTypesCount}종류, {selectedProducts.Count}개 상품 선택 완료!");
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
        if (isDebugMode)
        {
            Debug.Log("🔧 디버그 모드: 모든 아이템 1개씩");
        }
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

    /// <summary>
    /// 지정된 위치로 부드럽게 이동
    /// </summary>
    IEnumerator MoveToPosition(Vector2 targetPos, float moveTime)
    {
        Vector2 startPos = rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveTime;
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPos;
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

        // 퇴장 애니메이션 시작 (HideUI는 코루틴 안에서 호출)
        StartCoroutine(ExitRoutine());

    }

    IEnumerator ExitRoutine()
    {
        // 스폰 위치로 이동
        yield return StartCoroutine(MoveToPosition(spawnPos, 2f));

        // UI 숨기기는 삭제 직전에 (이동 완료 후)
        if (CustomerUI.Instance != null)
        {
            CustomerUI.Instance.HideUI();
        }

        // 오브젝트 삭제
        Destroy(gameObject);
    }
}