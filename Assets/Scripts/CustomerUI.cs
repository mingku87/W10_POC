using UnityEngine;
using TMPro; // TextMeshPro 네임스페이스 추가
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// 손님의 쇼핑 목록을 표시하는 UI 스크립트. (싱글톤)
/// Customer.cs에서 이 스크립트의 함수를 호출하여 제어합니다.
/// </summary>
public class CustomerUI : MonoBehaviour
{
    // ✨ [수정] 싱글톤 인스턴스
    public static CustomerUI Instance { get; private set; }

    // 인스펙터에서 쇼핑 목록을 표시할 TextMeshProUGUI 컴포넌트를 할당
    public TextMeshProUGUI shoppingListText;

    void Awake()
    {
        // ✨ [수정] 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("CustomerUI 인스턴스가 이미 존재합니다. 새로 생성된 인스턴스를 파괴합니다.");
            Destroy(gameObject);
            return; // 나머지 Awake 로직을 실행하지 않음
        }

        // UI가 비어 있는지 확인
        if (shoppingListText == null)
        {
            Debug.LogError("CustomerUI: shoppingListText가 할당되지 않았습니다!");
        }

        // 시작할 때 UI를 숨기지 않음 (항상 표시)
        // HideUI(); // 주석 처리 - 손님 캔버스가 자동으로 꺼지는 것 방지
    }

    /// <summary>
    /// 쇼핑 목록을 받아와 Text UI를 업데이트하고 UI를 활성화합니다.
    /// </summary>
    /// <param name="products">손님이 선택한 상품 리스트</param>
    public void UpdateShoppingList(List<ProductInteractable> products)
    {
        if (shoppingListText == null) return;

        // StringBuilder를 사용해 효율적으로 문자열 생성
        StringBuilder sb = new StringBuilder();

        if (products.Count == 0)
        {
            sb.AppendLine("🛒 쇼핑 목록: 없음");
        }
        else
        {
            // 상품 이름별로 그룹화 및 정렬 (Customer.cs의 PrintShoppingList 로직과 동일)
            var groupedProducts = products
                .GroupBy(p => p.productData.productName)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderBy(g => g.Name);

            sb.AppendLine($"🛒 쇼핑 목록 (총 {products.Count}개)");
            sb.AppendLine("─────────────────"); // UI에 맞게 구분선 길이 조절

            foreach (var item in groupedProducts)
            {
                sb.AppendLine($"• {item.Name} x {item.Count}");
            }
        }

        // TextMeshPro 텍스트 업데이트
        shoppingListText.text = sb.ToString();

        // UI (이 스크립트가 붙은 게임 오브젝트)를 활성화
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 텍스트를 비우고 UI를 비활성화합니다.
    /// </summary>
    public void HideUI()
    {
        if (shoppingListText != null)
        {
            shoppingListText.text = ""; // 텍스트 초기화
        }

        // UI 오브젝트 비활성화는 손님 오브젝트까지 비활성화될 수 있으므로 주석 처리
        // gameObject.SetActive(false);
    }
}