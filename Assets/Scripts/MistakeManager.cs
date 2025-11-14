using UnityEngine;
using TMPro;

/// <summary>
/// 실수 관리 시스템 - 플레이어의 실수를 추적하고 게임오버를 처리
/// Unity 6.0 호환
/// </summary>
public class MistakeManager : MonoBehaviour
{
    public static MistakeManager Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI mistakeStackText; // 실수 스택 UI
    public TextMeshProUGUI mistakeAllText; // 실수 스택 UI
    public GameObject gameOverPanel; // 게임오버 패널

    [Header("Mistake Settings")]
    [SerializeField] private int maxMistakes = 3; // 최대 실수 허용 횟수
    [SerializeField] private int currentMistakeStack = 0; // 현재 실수 스택

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true; // 디버그 로그 표시 여부

    /// <summary>
    /// 실수 유형 정의
    /// </summary>
    public enum MistakeType
    {
        BrandChangeDetected,        // 브랜드 변경 중 손님에게 발각됨
        WrongProductInCheckout,     // 잘못된 상품이 계산대에 포함됨
        CustomerTimeout,            // 손님 대기 시간 초과
        FakeMoneyDetected,          // 가짜 돈이 손님에게 발각됨
        BarcodeChangeCCTVDetected,  // CCTV가 작동 중일 때 바코드 교체 발각
        ChangeAmountMistake         // 거스름돈 오류
    }

    void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 초기화
        currentMistakeStack = 0;
        UpdateMistakeUI();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (showDebugLogs)
        {
            Debug.Log("[MistakeManager] 실수 관리 시스템 초기화 완료");
        }
    }

    /// <summary>
    /// 실수 추가 (실수 유형 지정)
    /// </summary>
    /// <param name="mistakeType">발생한 실수의 유형</param>
    /// <param name="additionalInfo">추가 정보 (선택사항)</param>
    public void AddMistake(MistakeType mistakeType, string additionalInfo = "")
    {
        currentMistakeStack++;
        UpdateMistakeUI();

        // 실수 유형별 로그 메시지
        string mistakeMessage = GetMistakeMessage(mistakeType, additionalInfo);

        mistakeAllText.text = mistakeMessage;

        if (showDebugLogs)
        {
            Debug.LogWarning($"[MistakeManager] {mistakeMessage} (스택: {currentMistakeStack}/{maxMistakes})");
        }

        // 최대 실수 횟수에 도달하면 게임오버
        if (currentMistakeStack >= maxMistakes)
        {
            TriggerGameOver(mistakeType);
        }
    }

    /// <summary>
    /// 실수 제거 (특정 상황에서 실수 감소 가능하도록)
    /// </summary>
    /// <param name="amount">제거할 실수 개수</param>
    public void RemoveMistake(int amount = 1)
    {
        currentMistakeStack = Mathf.Max(0, currentMistakeStack - amount);
        UpdateMistakeUI();

        if (showDebugLogs)
        {
            Debug.Log($"[MistakeManager] 실수 {amount}개 감소 (현재: {currentMistakeStack}/{maxMistakes})");
        }
    }

    /// <summary>
    /// 실수 스택 초기화
    /// </summary>
    public void ResetMistakes()
    {
        currentMistakeStack = 0;
        UpdateMistakeUI();

        if (showDebugLogs)
        {
            Debug.Log("[MistakeManager] 실수 스택 초기화");
        }
    }

    /// <summary>
    /// 실수 UI 업데이트
    /// </summary>
    private void UpdateMistakeUI()
    {
        if (mistakeStackText != null)
        {
            mistakeStackText.text = $"실수: {currentMistakeStack}/{maxMistakes}";

            // 실수 횟수에 따른 색상 변경
            if (currentMistakeStack == 0)
            {
                mistakeStackText.color = Color.white;
            }
            else if (currentMistakeStack < maxMistakes)
            {
                mistakeStackText.color = Color.yellow;
            }
            else
            {
                mistakeStackText.color = Color.red;
            }
        }
    }

    /// <summary>
    /// 게임오버 처리
    /// </summary>
    /// <param name="finalMistakeType">게임오버를 유발한 실수 유형</param>
    private void TriggerGameOver(MistakeType finalMistakeType)
    {
        if (showDebugLogs)
        {
            Debug.LogError($"[MistakeManager] 게임 오버! 최종 실수: {GetMistakeMessage(finalMistakeType)}");
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            // 게임오버 패널에 최종 실수 유형 표시 (선택사항)
            TextMeshProUGUI gameOverText = gameOverPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (gameOverText != null)
            {
                gameOverText.text = $"게임 오버!\n\n최종 실수:\n{GetMistakeMessage(finalMistakeType)}";
            }
        }

        // 게임 일시정지
        Time.timeScale = 0f;
    }

    /// <summary>
    /// 실수 유형에 따른 메시지 반환
    /// </summary>
    private string GetMistakeMessage(MistakeType mistakeType, string additionalInfo = "")
    {
        string baseMessage = mistakeType switch
        {
            MistakeType.BrandChangeDetected => "손님이 브랜드 변경을 목격했습니다!",
            MistakeType.WrongProductInCheckout => "잘못된 상품이 계산대에 포함되었습니다!",
            MistakeType.CustomerTimeout => "손님 대기 시간이 초과되었습니다!",
            MistakeType.FakeMoneyDetected => "손님이 가짜 돈을 발견했습니다!",
            MistakeType.BarcodeChangeCCTVDetected => "CCTV가 바코드 교체를 감지했습니다!",
            MistakeType.ChangeAmountMistake => "거스름돈 계산에 오류가 발생했습니다!",
            _ => "알 수 없는 실수가 발생했습니다!"
        };

        if (!string.IsNullOrEmpty(additionalInfo))
        {
            return $"{baseMessage} ({additionalInfo})";
        }

        return baseMessage;
    }


    public void ClearText()
    {
        mistakeAllText.text = "";
    }

    /// <summary>
    /// 손님 대사 표시 (수상한 행동 감지 시)
    /// </summary>
    public void ShowCustomerDialogue(string dialogue)
    {
        if (mistakeAllText != null)
        {
            mistakeAllText.text = $"💬 {dialogue}";
            mistakeAllText.color = new Color(1f, 0.7f, 0.3f); // 주황색으로 강조
        }
        else
        {
            Debug.LogError("[MistakeManager] mistakeAllText가 null입니다!");
        }

        // 2초 후 자동으로 사라지도록
        StartCoroutine(ClearDialogueAfterDelay(2f));
    }
    System.Collections.IEnumerator ClearDialogueAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ClearText();

        // 색상 원래대로 복구
        if (mistakeAllText != null)
        {
            mistakeAllText.color = Color.white;
        }
    }

    /// <summary>
    /// 현재 실수 스택 반환 (읽기 전용)
    /// </summary>
    public int GetCurrentMistakeStack()
    {
        return currentMistakeStack;
    }

    /// <summary>
    /// 최대 실수 허용 횟수 반환 (읽기 전용)
    /// </summary>
    public int GetMaxMistakes()
    {
        return maxMistakes;
    }

    /// <summary>
    /// 실수 스택이 가득 찼는지 확인
    /// </summary>
    public bool IsMistakeStackFull()
    {
        return currentMistakeStack >= maxMistakes;
    }

    /// <summary>
    /// 게임오버 상태 확인
    /// </summary>
    public bool IsGameOver()
    {
        return gameOverPanel != null && gameOverPanel.activeSelf;
    }
}