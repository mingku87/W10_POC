using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 손님을 일정 시간마다 소환하고 계산 처리하는 매니저
/// </summary>
public class CustomerManager : MonoBehaviour
{
    public static CustomerManager Instance { get; private set; }

    [Header("Customer Settings")]
    [Tooltip("손님 등장 간격 (초)")]
    public float spawnInterval = 15f;

    [Header("References")]
    public GameObject customerPrefab;

    [Header("손님 위치 설정")]
    [Tooltip("손님이 처음 생성되는 위치 (나갈 때도 여기로)")]
    public Transform spawnPosition;

    [Tooltip("손님이 걸어 들어올 위치 (쇼핑 위치)")]
    public Transform enterPosition;

    [Header("벨 설정")]
    [Tooltip("손님 입장 전에 보여줄 벨 오브젝트")]
    public GameObject bellObject;

    [Header("Customer Sprites")]
    [Tooltip("멀쩡한 손님 스프라이트")]
    public Sprite normalSprite;
    [Tooltip("취한 손님 스프라이트")]
    public Sprite drunkSprite;
    [Tooltip("핸드폰 보는 손님 스프라이트")]
    public Sprite onPhoneSprite;
    [Tooltip("화난 일반 손님 스프라이트")]
    public Sprite angryNormalSprite;
    [Tooltip("화난 취객 스프라이트")]
    public Sprite angryDrunkSprite;

    [Header("계산대 설정")]
    public Transform checkoutCounter; // 계산대 위치

    private List<Customer> waitingCustomers = new List<Customer>();
    public Customer currentCheckoutCustomer = null; // 현재 계산 대기 중인 손님 (public으로 변경)
    private bool isCustomerAtCheckout = false; // 계산대에 손님이 있는지 여부

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
        // 스프라이트 자동 로드 (Resources 폴더에서)
        LoadSpritesFromResources();

        // 벨 오브젝트 초기화 (처음엔 꺼둠)
        if (bellObject != null)
        {
            bellObject.SetActive(false);
        }

        StartCoroutine(CustomerSpawnRoutine());
    }

    IEnumerator CustomerSpawnRoutine()
    {
        // 첫 손님 등장 전 대기 시간 증가
        //yield return new WaitForSeconds(10f); // 10초 대기
        StartCoroutine(SpawnCustomerWithBell());

        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // 계산대에 손님이 있으면 새 손님 입장 안 함
            if (!isCustomerAtCheckout)
            {
                StartCoroutine(SpawnCustomerWithBell());
            }
            else
            {
                Debug.Log("[매니저] 계산대에 손님이 있어서 입장 대기 중...");
            }
        }
    }

    IEnumerator SpawnCustomerWithBell()
    {
        // 1. 벨 울리기 (1초간 표시)
        if (bellObject != null)
        {
            bellObject.SetActive(true);
            Debug.Log("[매니저] 띵동~ 손님이 곧 입장합니다!");
            yield return new WaitForSeconds(1f);
            bellObject.SetActive(false);
        }

        // 2. 1초 후 손님 생성 및 입장
        yield return new WaitForSeconds(1f);
        SpawnCustomer();
    }

    void SpawnCustomer()
    {
        if (customerPrefab != null)
        {
            // 스폰 위치와 입장 위치 확인
            if (spawnPosition == null || enterPosition == null)
            {
                Debug.LogError("[매니저] 스폰 위치 또는 입장 위치가 설정되지 않았습니다!");
                return;
            }

            // 스폰 위치의 부모(Canvas 등) 가져오기
            Transform parentTransform = spawnPosition.parent;
            if (parentTransform == null)
            {
                Debug.LogError("[매니저] 스폰 위치에 부모가 없습니다!");
                return;
            }

            // 프리팹 인스턴스 생성 (스폰 위치의 부모 하위에 생성)
            GameObject customerObj = Instantiate(customerPrefab, parentTransform);

            // 프리팹에서 Customer 컴포넌트 가져오기 (없으면 자동 추가)
            Customer customer = customerObj.GetComponent<Customer>();
            if (customer == null)
            {
                Debug.LogWarning("[매니저] Customer 프리팹에 Customer 스크립트가 없어서 자동으로 추가합니다.");
                customer = customerObj.AddComponent<Customer>();
            }

            // 매니저 참조 전달
            customer.manager = this;

            // 손님 타입 랜덤 결정 (80% 멀쩡한 손님, 20% 취객)
            float randomValue = Random.value;
            if (randomValue < 0.2f) // 20% 확률로 취객
            {
                customer.customerType = Customer.CustomerType.Drunk;
            }
            else
            {
                customer.customerType = Customer.CustomerType.Normal;
            }

            // RectTransform 설정 (스폰 위치에 생성)
            RectTransform customerRect = customerObj.GetComponent<RectTransform>();
            RectTransform spawnRect = spawnPosition.GetComponent<RectTransform>();
            RectTransform enterRect = enterPosition.GetComponent<RectTransform>();

            if (customerRect != null && spawnRect != null && enterRect != null)
            {
                // 스폰 위치로 초기 배치
                customerRect.anchoredPosition = spawnRect.anchoredPosition;

                // 입장 위치 정보 전달
                customer.SetPositions(spawnRect.anchoredPosition, enterRect.anchoredPosition);
            }

            // 타입에 따라 스프라이트 설정
            if (customer.customerImage != null)
            {
                if (customer.customerType == Customer.CustomerType.Normal)
                {
                    customer.customerImage.sprite = normalSprite;
                }
                else if (customer.customerType == Customer.CustomerType.Drunk)
                {
                    customer.customerImage.sprite = drunkSprite;
                }
                else if (customer.customerType == Customer.CustomerType.OnPhone)
                {
                    customer.customerImage.sprite = onPhoneSprite;
                }
            }

            waitingCustomers.Add(customer);

            Debug.Log($"[매니저] 손님 입장! 타입: {customer.customerType} (대기 중인 손님: {waitingCustomers.Count}명)");
        }
    }

    // 손님이 계산대에 도착했을 때 호출
    public void OnCustomerReadyForCheckout(Customer customer)
    {
        if (currentCheckoutCustomer == null)
        {
            currentCheckoutCustomer = customer;
            isCustomerAtCheckout = true; // 계산대 차지
            Debug.Log("[매니저] 손님이 계산 대기 중 - 스캐너로 스캔 → C키로 계산");
        }
    }

    // 플레이어가 계산 완료했을 때 호출
    public void OnPaymentComplete()
    {
        if (currentCheckoutCustomer != null)
        {
            Debug.Log("[매니저] 계산 완료 - 손님 퇴장 처리");

            waitingCustomers.Remove(currentCheckoutCustomer);
            currentCheckoutCustomer.Leave();
            currentCheckoutCustomer = null;
            isCustomerAtCheckout = false; // 계산대 비움 - 다음 손님 입장 가능
        }
    }

    /// <summary>
    /// 손님이 화나서 나갔을 때 호출 (시간 초과 또는 사기 한계 초과)
    /// </summary>
    public void OnCustomerLeftAngry(Customer customer)
    {
        Debug.Log("[매니저] 손님이 화나서 나갔습니다!");

        waitingCustomers.Remove(customer);

        if (currentCheckoutCustomer == customer)
        {
            currentCheckoutCustomer = null;
            isCustomerAtCheckout = false; // 계산대 비움 - 다음 손님 입장 가능
        }
    }

    /// <summary>
    /// Resources 폴더에서 손님 스프라이트 자동 로드
    /// Resources/Sprites/Customers/ 폴더에 normal.png, drunk.png, onphone.png 넣어두면 자동 로드
    /// </summary>
    void LoadSpritesFromResources()
    {
        if (normalSprite == null)
        {
            normalSprite = Resources.Load<Sprite>("Sprites/Customers/normal");
            if (normalSprite != null) Debug.Log("Normal sprite loaded from Resources");
        }

        if (drunkSprite == null)
        {
            drunkSprite = Resources.Load<Sprite>("Sprites/Customers/drunk");
            if (drunkSprite != null) Debug.Log("Drunk sprite loaded from Resources");
        }

        if (onPhoneSprite == null)
        {
            onPhoneSprite = Resources.Load<Sprite>("Sprites/Customers/onphone");
            if (onPhoneSprite != null) Debug.Log("OnPhone sprite loaded from Resources");
        }

        if (angryNormalSprite == null)
        {
            angryNormalSprite = Resources.Load<Sprite>("Sprites/Customers/angry_normal");
            if (angryNormalSprite != null) Debug.Log("Angry Normal sprite loaded from Resources");
        }

        if (angryDrunkSprite == null)
        {
            angryDrunkSprite = Resources.Load<Sprite>("Sprites/Customers/angry_drunk");
            if (angryDrunkSprite != null) Debug.Log("Angry Drunk sprite loaded from Resources");
        }
    }
}
