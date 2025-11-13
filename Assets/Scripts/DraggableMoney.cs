using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 드래그 가능한 거스름돈
/// </summary>
public class DraggableMoney : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int moneyAmount = 1000; // 돈 금액
    public bool isFakeMoney = false; // 가짜 돈 여부
    public bool isClone = false; // 복사본인지 여부
    public bool isPlacedInDropZone = false; // 거스름돈 창구에 이미 배치되었는지

    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private GameObject dragClone; // 드래그 중인 복사본
    private ChangeMoneyDropZone currentDropZone; // 현재 속한 드랍존

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        // 스프라이트 자동 로드 (Resources/Sprites/Money/ 폴더에서)
        LoadMoneySprite();
    }

    /// <summary>
    /// Resources 폴더에서 돈 스프라이트 자동 로드
    /// Resources/Sprites/Money/ 폴더에:
    /// - 진짜 돈: 100.png, 500.png, 1000.png, 5000.png, 10000.png, 50000.png
    /// - 가짜 돈: 100fake.png, 500fake.png, 1000fake.png, 5000fake.png, 10000fake.png, 50000fake.png
    /// </summary>
    void LoadMoneySprite()
    {
        Image img = GetComponent<Image>();
        if (img != null && img.sprite == null)
        {
            // 가짜 돈이면 "fake" 접미사 추가
            string spriteName = moneyAmount.ToString();
            if (isFakeMoney)
            {
                spriteName += "fake";
            }

            Sprite loadedSprite = Resources.Load<Sprite>("Sprites/Money/" + spriteName);

            if (loadedSprite != null)
            {
                img.sprite = loadedSprite;
                Debug.Log($"Money sprite loaded: {spriteName}");
            }
            else
            {
                // 스프라이트 없으면 기본 색상 유지
                Debug.Log($"Money sprite not found: Sprites/Money/{spriteName} (using default color)");
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 이미 배치된 거스름돈을 다시 드래그하는 경우
        if (isPlacedInDropZone && currentDropZone != null)
        {
            Debug.Log($"[거스름돈 제거 시작] {moneyAmount}원을 드래그하여 제거합니다.");
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
            return;
        }

        // 원본은 그대로 두고 복사본 생성
        if (!isClone)
        {
            // FIX: 드래그용 최상위 Canvas 찾기 또는 생성
            Canvas dragCanvas = FindOrCreateDragCanvas();

            dragClone = Instantiate(gameObject, dragCanvas.transform);
            dragClone.name = gameObject.name + "_DragClone";

            DraggableMoney cloneDraggable = dragClone.GetComponent<DraggableMoney>();
            cloneDraggable.isClone = true;
            cloneDraggable.moneyAmount = moneyAmount;
            cloneDraggable.isFakeMoney = isFakeMoney;

            // 복사본의 Canvas 참조 수동 설정 (Awake 대신)
            cloneDraggable.canvas = dragCanvas;
            cloneDraggable.rectTransform = dragClone.GetComponent<RectTransform>();
            cloneDraggable.canvasGroup = dragClone.GetComponent<CanvasGroup>();

            // 복사본 스프라이트 복사 (이미지가 보이도록)
            Image originalImg = GetComponent<Image>();
            Image cloneImg = dragClone.GetComponent<Image>();
            if (originalImg != null && cloneImg != null)
            {
                if (originalImg.sprite != null)
                {
                    cloneImg.sprite = originalImg.sprite;
                }
                cloneImg.color = originalImg.color; // 색상도 복사

                // FIX: 스프라이트 없으면 빨간색으로 명확하게 보이도록
                if (cloneImg.sprite == null)
                {
                    cloneImg.color = Color.red;
                    Debug.LogWarning($"[돈 드래그] 스프라이트 없음! 빨간색으로 표시");
                }
            }

            // FIX: 복사본 위치를 원본 스크린 좌표로 (world position 사용)
            RectTransform cloneRect = dragClone.GetComponent<RectTransform>();
            cloneRect.position = rectTransform.position; // 스크린 좌표 복사
            cloneRect.sizeDelta = rectTransform.sizeDelta; // 크기도 복사

            // 복사본 드래그 시작
            cloneDraggable.originalPosition = cloneRect.anchoredPosition;
            cloneDraggable.originalParent = dragCanvas.transform;
            cloneDraggable.canvasGroup.alpha = 0.8f; // 더 진하게 (잘 보이도록)
            cloneDraggable.canvasGroup.blocksRaycasts = false;

            Debug.Log($"[돈 드래그 시작] {moneyAmount}원 클론 생성! DragCanvas 자식으로 생성됨. 위치: {cloneRect.position}, 색: {cloneImg.color}");

            // 원본 이벤트 취소하고 복사본에게 드래그 이벤트 넘김
            eventData.pointerDrag = dragClone;
            return; // 원본은 여기서 끝
        }

        // 복사본이면 그냥 드래그 시작 (이미 DragCanvas 자식)
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        canvasGroup.alpha = 0.8f;
        canvasGroup.blocksRaycasts = false;

        Debug.Log($"[복사본 드래그] 위치: {rectTransform.position}, Parent: {transform.parent.name}");
    }    /// <summary>
         /// 드래그용 최상위 Canvas 찾기 또는 생성
         /// </summary>
    Canvas FindOrCreateDragCanvas()
    {
        // 기존 DragCanvas 찾기
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in allCanvases)
        {
            if (c.name == "DragCanvas" && c.sortingOrder == 300)
            {
                return c;
            }
        }

        // 없으면 생성
        GameObject dragCanvasObj = new GameObject("DragCanvas");
        Canvas dragCanvas = dragCanvasObj.AddComponent<Canvas>();
        dragCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dragCanvas.sortingOrder = 300; // 모든 UI보다 높음 (ScannerCanvas=200보다 높음)
        dragCanvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        Debug.Log("[드래그 캔버스] 최상위 DragCanvas 생성 (sortingOrder=300)");
        return dragCanvas;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 배치된 거스름돈을 드래그 중이거나 복사본을 드래그 중
        if ((isPlacedInDropZone || isClone) && rectTransform != null && canvas != null)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 배치된 거스름돈을 드래그해서 밖으로 빼는 경우
        if (isPlacedInDropZone)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            // Raycast로 여전히 ChangeMoneyDropZone 위에 있는지 확인
            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            bool stillInDropZone = false;
            foreach (var result in raycastResults)
            {
                if (result.gameObject.GetComponent<ChangeMoneyDropZone>() != null)
                {
                    stillInDropZone = true;
                    break;
                }
            }

            // 드랍존 밖으로 나갔으면 삭제하고 금액 차감
            if (!stillInDropZone)
            {
                Debug.Log($"[거스름돈 제거] {moneyAmount}원을 드랍존 밖으로 드래그 - 삭제 및 금액 차감");

                if (currentDropZone != null)
                {
                    currentDropZone.RemoveMoney(this);
                }

                Destroy(gameObject);
                return;
            }
            else
            {
                Debug.Log($"[거스름돈] 여전히 드랍존 안에 있음 - 유지");
                return;
            }
        }

        // 복사본이 아니면 아무것도 안함 (원본은 움직이지 않음)
        if (!isClone)
        {
            return;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        Debug.Log($"[드래그 끝] 마우스 위치에서 Raycast 실행 중...");

        // Raycast로 드랍 존 확인
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        Debug.Log($"[Raycast] 결과 {results.Count}개 발견:");
        foreach (var result in results)
        {
            Debug.Log($"  - {result.gameObject.name} (레이어: {result.gameObject.layer})");

            ChangeMoneyDropZone dropZone = result.gameObject.GetComponent<ChangeMoneyDropZone>();
            if (dropZone != null)
            {
                Debug.Log($"[드랍 성공!] ChangeMoneyDropZone 찾음!");

                // 손님이 없으면 드랍 불가
                if (CheckoutCounter.Instance == null || !CheckoutCounter.Instance.isCustomerWaiting)
                {
                    Debug.LogWarning("[거스름돈] 손님이 없어서 거스름돈을 올릴 수 없습니다!");
                    Destroy(gameObject);
                    return;
                }

                // 가짜 돈을 올릴 때 손님이 보고 있으면 실수 (취객은 못 봄)
                if (isFakeMoney)
                {
                    CustomerManager manager = FindFirstObjectByType<CustomerManager>();
                    if (manager != null && manager.currentCheckoutCustomer != null)
                    {
                        Customer customer = manager.currentCheckoutCustomer;

                        // 취객이 아니고 휴대폰을 안 보고 있으면 실수
                        if (customer.customerType != Customer.CustomerType.Drunk && !customer.isOnPhone)
                        {
                            Debug.LogWarning("[경고] 손님이 가짜 돈을 발견했습니다!");

                            if (POSSystem.Instance != null)
                            {
                                POSSystem.Instance.AddMistake();
                            }
                        }
                    }
                }

                // 거스름돈 드랍 성공
                dropZone.OnMoneyDropped(this);

                // 드랍존 참조 저장 (나중에 제거할 때 사용)
                currentDropZone = dropZone;
                isPlacedInDropZone = true;

                // FIX: ChangeMoneyDropZone 자식으로 이동 (ChangeDropCanvas 안으로)
                transform.SetParent(dropZone.transform);

                // FIX: 드랍존 안에서 최상위로 (다른 UI에 가려지지 않도록)
                transform.SetAsLastSibling();

                // 드랍존 안에 보이도록 배치 (HorizontalLayout이 자동 배치함)
                rectTransform.anchoredPosition = new Vector2(0, 0);
                rectTransform.localScale = Vector3.one;

                Debug.Log($"[돈] 거스름돈 영역에 {moneyAmount}원 드랍 완료! Parent: {transform.parent.name}, 위치: {rectTransform.position}");
                return;
            }
        }

        // 드랍 실패 - 복사본 삭제
        Debug.Log($"[돈] 드랍 실패 - ChangeMoneyDropZone 못 찾음. 복사본 삭제.");
        Destroy(gameObject);
    }
}
