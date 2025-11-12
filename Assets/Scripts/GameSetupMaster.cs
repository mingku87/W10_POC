using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI Auto Generator - TextMeshPro version
/// Right-click component in Inspector -> "Generate All UI"
/// </summary>
public class GameSetupMaster : MonoBehaviour
{
    [Header("Font Settings (TextMeshPro)")]
    [Tooltip("TextMeshPro Font Asset (leave empty for default)")]
    public TMP_FontAsset customFont;

    // Private 설정값 - Inspector에서 수정 불가, 코드 변경만 가능
    private int productCount = 5;
    private float productSpacing = 0f;
    private Vector3 startPosition = new Vector3(-8, 4f, 0);
    private GameObject moneyPrefabCache; // 거스름돈 prefab 캐시

    [ContextMenu("Generate All UI")]
    public void GenerateAllUI()
    {
#if UNITY_EDITOR
        Debug.Log("=== UI Generation Started ===");

        CreateCCTVSystem();
        CreateProducts(); // 상품 먼저 생성 (뒤쪽 레이어)
        GameObject barcodePanel = CreateBarcodeInventory(); // 바코드 패널 (중간 레이어)
        CreateProductDetailPanel(barcodePanel); // 디테일 패널 (앞쪽 레이어)

        // 거스름돈 prefab 미리 생성
        Canvas canvas = FindFirstObjectByType<Canvas>();
        moneyPrefabCache = CreateMoneyPrefab(canvas);

        CreateCheckoutCounter(); // 계산대 3D 오브젝트
        CreatePOSSystem(); // POS 시스템
        CreateCustomerSystem();

        Debug.Log("=== UI Generation Complete! Press Play to test ===");
#endif
    }

    void CreateCCTVSystem()
    {
#if UNITY_EDITOR
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("EventSystem 생성됨");
            }

            Debug.Log("Canvas and EventSystem created");
        }
        else
        {
            // Canvas는 있지만 EventSystem 확인
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("EventSystem 추가 생성됨");
            }

            // GraphicRaycaster 확인
            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log("GraphicRaycaster 추가됨");
            }
        }

        GameObject cctvManager = new GameObject("CCTVManager");
        GameObject lightObj = new GameObject("CCTVLight");
        lightObj.transform.SetParent(canvas.transform, false);

        Image image = lightObj.AddComponent<Image>();
        image.color = Color.green;

        RectTransform rectTransform = lightObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(1, 1);
        rectTransform.anchoredPosition = new Vector2(-50, -50);
        rectTransform.sizeDelta = new Vector2(30, 30);

        CCTVController controller = cctvManager.AddComponent<CCTVController>();
        controller.lightImage = image;
        controller.redColor = Color.red;
        controller.greenColor = Color.green;
        controller.watchDuration = 3f;
        controller.idleDuration = 5f;

        Debug.Log("1. CCTV System created");
#endif
    }

    GameObject CreateBarcodeInventory()
    {
#if UNITY_EDITOR
        Canvas canvas = FindFirstObjectByType<Canvas>();

        // 바코드 인벤토리 전용 Canvas (최상위)
        GameObject barcodeCanvas = new GameObject("BarcodeInventoryCanvas");
        Canvas barcodeCanvasComp = barcodeCanvas.AddComponent<Canvas>();
        barcodeCanvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
        barcodeCanvasComp.sortingOrder = 500; // 가장 최상위 레이어
        barcodeCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject panelObj = new GameObject("BarcodeInventoryPanel");
        panelObj.transform.SetParent(barcodeCanvas.transform, false);

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1); // 상단 중앙 기준으로 변경
        panelRect.anchorMax = new Vector2(0.5f, 1);
        panelRect.pivot = new Vector2(0.5f, 1);
        panelRect.anchoredPosition = new Vector2(0, -80); // 상단에서 80px 아래 (지갑 아래)
        panelRect.sizeDelta = new Vector2(800, 120);

        GridLayoutGroup grid = panelObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(70, 50);
        grid.spacing = new Vector2(10, 10);
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 10;

        GameObject barcodePrefab = CreateBarcodePrefab(canvas);

        GameObject inventoryManager = new GameObject("BarcodeInventoryManager");
        BarcodeInventory inventory = inventoryManager.AddComponent<BarcodeInventory>();
        inventory.barcodePrefab = barcodePrefab;
        inventory.barcodeContainer = panelObj.transform;
        inventory.gridLayout = grid;

        Debug.Log("2. Barcode Inventory created");

        return panelObj; // 패널 반환
#else
        return null;
#endif
    }

    GameObject CreateBarcodePrefab(Canvas canvas)
    {
#if UNITY_EDITOR
        string prefabPath = "Assets/Prefabs";
        if (!UnityEditor.AssetDatabase.IsValidFolder(prefabPath))
        {
            UnityEditor.AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        GameObject barcodeObj = new GameObject("BarcodePrefab");
        barcodeObj.transform.SetParent(canvas.transform, false);

        Image bgImage = barcodeObj.AddComponent<Image>();
        bgImage.color = new Color(1f, 0.9f, 0.7f, 1f);

        RectTransform rect = barcodeObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(70, 50);

        DraggableBarcode draggable = barcodeObj.AddComponent<DraggableBarcode>();
        barcodeObj.AddComponent<CanvasGroup>();

        GameObject textObj = new GameObject("PriceText");
        textObj.transform.SetParent(barcodeObj.transform, false);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "1000W";
        text.font = customFont;
        text.fontSize = 16;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        draggable.priceText = text;

        string fullPath = prefabPath + "/BarcodePrefab.prefab";
        GameObject prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(barcodeObj, fullPath);

        DestroyImmediate(barcodeObj);

        return prefab;
#else
        return null;
#endif
    }

    void CreateProductDetailPanel(GameObject barcodePanel)
    {
#if UNITY_EDITOR
        Canvas canvas = FindFirstObjectByType<Canvas>();

        // 빈 부모 오브젝트 (컴포넌트 담당, 항상 활성화)
        GameObject panelHolder = new GameObject("ProductDetailPanelHolder");
        panelHolder.transform.SetParent(canvas.transform, false);

        // 실제 패널 (검은 배경, 비활성화)
        GameObject mainPanel = new GameObject("ProductDetailPanel");
        mainPanel.transform.SetParent(panelHolder.transform, false);

        Image mainPanelImage = mainPanel.AddComponent<Image>();
        mainPanelImage.color = new Color(0, 0, 0, 0.78f);

        RectTransform mainRect = mainPanel.GetComponent<RectTransform>();
        mainRect.anchorMin = Vector2.zero;
        mainRect.anchorMax = Vector2.one;
        mainRect.offsetMin = Vector2.zero;
        mainRect.offsetMax = Vector2.zero;

        GameObject infoPanel = new GameObject("ProductInfoPanel");
        infoPanel.transform.SetParent(mainPanel.transform, false);

        Image infoPanelImage = infoPanel.AddComponent<Image>();
        infoPanelImage.color = Color.white;

        RectTransform infoRect = infoPanel.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0.5f, 0.5f);
        infoRect.anchorMax = new Vector2(0.5f, 0.5f);
        infoRect.pivot = new Vector2(0.5f, 0.5f);
        infoRect.anchoredPosition = Vector2.zero;
        infoRect.sizeDelta = new Vector2(600, 700); // 패널 크기 증가

        GameObject imageObj = new GameObject("ProductImage");
        imageObj.transform.SetParent(infoPanel.transform, false);
        Image productImage = imageObj.AddComponent<Image>();
        productImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);

        RectTransform imageRect = imageObj.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 1);
        imageRect.anchorMax = new Vector2(0.5f, 1);
        imageRect.pivot = new Vector2(0.5f, 1);
        imageRect.anchoredPosition = new Vector2(0, -10);
        imageRect.sizeDelta = new Vector2(500, 500); // 이미지 크기 500x500으로 크게 확대

        GameObject nameTextObj = new GameObject("ProductNameText");
        nameTextObj.transform.SetParent(infoPanel.transform, false);
        TextMeshProUGUI nameText = nameTextObj.AddComponent<TextMeshProUGUI>();
        nameText.text = "Product Name";
        nameText.font = customFont;
        nameText.fontSize = 28;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.black;

        RectTransform nameRect = nameTextObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 1);
        nameRect.anchorMax = new Vector2(0.5f, 1);
        nameRect.pivot = new Vector2(0.5f, 1);
        nameRect.anchoredPosition = new Vector2(0, -520); // 이미지 아래로 이동
        nameRect.sizeDelta = new Vector2(350, 40);

        // 원래 가격 텍스트 추가
        GameObject originalPriceTextObj = new GameObject("OriginalPriceText");
        originalPriceTextObj.transform.SetParent(infoPanel.transform, false);
        TextMeshProUGUI originalPriceText = originalPriceTextObj.AddComponent<TextMeshProUGUI>();
        originalPriceText.text = "Original Price: 1000";
        originalPriceText.font = customFont;
        originalPriceText.fontSize = 20;
        originalPriceText.alignment = TextAlignmentOptions.Center;
        originalPriceText.color = new Color(0.5f, 0.5f, 0.5f); // 회색

        RectTransform originalPriceRect = originalPriceTextObj.GetComponent<RectTransform>();
        originalPriceRect.anchorMin = new Vector2(0.5f, 1);
        originalPriceRect.anchorMax = new Vector2(0.5f, 1);
        originalPriceRect.pivot = new Vector2(0.5f, 1);
        originalPriceRect.anchoredPosition = new Vector2(0, -570); // 이름 아래로
        originalPriceRect.sizeDelta = new Vector2(350, 30);

        GameObject priceTextObj = new GameObject("CurrentPriceText");
        priceTextObj.transform.SetParent(infoPanel.transform, false);
        TextMeshProUGUI priceText = priceTextObj.AddComponent<TextMeshProUGUI>();
        priceText.text = "Current Price: 1000";
        priceText.font = customFont;
        priceText.fontSize = 22;
        priceText.alignment = TextAlignmentOptions.Center;
        priceText.color = Color.black;

        RectTransform priceRect = priceTextObj.GetComponent<RectTransform>();
        priceRect.anchorMin = new Vector2(0.5f, 1);
        priceRect.anchorMax = new Vector2(0.5f, 1);
        priceRect.pivot = new Vector2(0.5f, 1);
        priceRect.anchoredPosition = new Vector2(0, -610); // 원래 가격 아래로
        priceRect.sizeDelta = new Vector2(350, 30);

        GameObject dropZoneObj = new GameObject("BarcodeDropZone");
        dropZoneObj.transform.SetParent(infoPanel.transform, false);
        Image dropZoneImage = dropZoneObj.AddComponent<Image>();
        dropZoneImage.color = new Color(0.6f, 0.8f, 1f, 0.5f);

        RectTransform dropRect = dropZoneObj.GetComponent<RectTransform>();
        dropRect.anchorMin = new Vector2(0.5f, 0);
        dropRect.anchorMax = new Vector2(0.5f, 0);
        dropRect.pivot = new Vector2(0.5f, 0);
        dropRect.anchoredPosition = new Vector2(0, 80);
        dropRect.sizeDelta = new Vector2(300, 100);

        BarcodeDropZone dropZone = dropZoneObj.AddComponent<BarcodeDropZone>();

        GameObject hintTextObj = new GameObject("DropHintText");
        hintTextObj.transform.SetParent(dropZoneObj.transform, false);
        TextMeshProUGUI hintText = hintTextObj.AddComponent<TextMeshProUGUI>();
        hintText.text = "Drag barcode here";
        hintText.font = customFont;
        hintText.fontSize = 16;
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);

        RectTransform hintRect = hintTextObj.GetComponent<RectTransform>();
        hintRect.anchorMin = Vector2.zero;
        hintRect.anchorMax = Vector2.one;
        hintRect.offsetMin = Vector2.zero;
        hintRect.offsetMax = Vector2.zero;

        GameObject feedbackTextObj = new GameObject("FeedbackText");
        feedbackTextObj.transform.SetParent(infoPanel.transform, false);
        TextMeshProUGUI feedbackText = feedbackTextObj.AddComponent<TextMeshProUGUI>();
        feedbackText.text = "";
        feedbackText.font = customFont;
        feedbackText.fontSize = 20;
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.color = new Color(0, 0.7f, 0, 1);

        RectTransform feedbackRect = feedbackTextObj.GetComponent<RectTransform>();
        feedbackRect.anchorMin = new Vector2(0.5f, 0);
        feedbackRect.anchorMax = new Vector2(0.5f, 0);
        feedbackRect.pivot = new Vector2(0.5f, 0);
        feedbackRect.anchoredPosition = new Vector2(0, 20);
        feedbackRect.sizeDelta = new Vector2(350, 30);

        dropZone.feedbackText = feedbackText;

        GameObject buttonObj = new GameObject("CloseButton");
        buttonObj.transform.SetParent(infoPanel.transform, false);
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.8f, 0.3f, 0.3f, 1f);

        Button closeButton = buttonObj.AddComponent<Button>();

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1, 1);
        buttonRect.anchorMax = new Vector2(1, 1);
        buttonRect.pivot = new Vector2(1, 1);
        buttonRect.anchoredPosition = new Vector2(-10, -10);
        buttonRect.sizeDelta = new Vector2(80, 40);

        GameObject buttonTextObj = new GameObject("Text");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Close";
        buttonText.font = customFont;
        buttonText.fontSize = 18;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;

        RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        // 부모 오브젝트에 컴포넌트 추가
        ProductDetailPanel detailPanel = panelHolder.AddComponent<ProductDetailPanel>();
        detailPanel.panelObject = mainPanel;
        detailPanel.productImage = productImage;
        detailPanel.productNameText = nameText;
        detailPanel.originalPriceText = originalPriceText;
        detailPanel.currentPriceText = priceText;
        detailPanel.closeButton = closeButton;
        detailPanel.dropZone = dropZone;
        detailPanel.barcodeInventoryPanel = barcodePanel;

        // 부모는 활성화, 패널만 비활성화
        mainPanel.SetActive(false);
        barcodePanel.SetActive(false);

        Debug.Log("3. Product Detail Panel created (패널만 비활성화, Holder는 활성화)");
#endif
    }

    void CreateProducts()
    {
#if UNITY_EDITOR
        Canvas canvas = FindFirstObjectByType<Canvas>();

        string[] productNames = new string[] { "Snack", "Drink", "Ramen", "Fruit", "Bread" };
        int[] productPrices = new int[] { 1000, 1500, 800, 2000, 1200 };

        for (int i = 0; i < productCount && i < productNames.Length; i++)
        {
            CreateProductButton(canvas, productNames[i], productPrices[i], i);
        }

        Debug.Log("4. Products created");
#endif
    }

    void CreateProductButton(Canvas canvas, string productName, int price, int index)
    {
#if UNITY_EDITOR
        GameObject productBtn = new GameObject("Product_" + productName);
        productBtn.transform.SetParent(canvas.transform, false);

        Image btnImage = productBtn.AddComponent<Image>();
        btnImage.color = new Color(Random.Range(0.5f, 0.9f), Random.Range(0.5f, 0.9f), Random.Range(0.5f, 0.9f)); // 하얀색 방지 (0.9 이하)

        Button button = productBtn.AddComponent<Button>();

        RectTransform btnRect = productBtn.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0, 1);
        btnRect.anchorMax = new Vector2(0, 1);
        btnRect.pivot = new Vector2(0, 1);
        btnRect.anchoredPosition = new Vector2(20, -20 - (index * 120)); // 왼쪽 위에 세로 배치
        btnRect.sizeDelta = new Vector2(150, 100); // 큰 이미지 크기

        // 상품 이름 텍스트
        GameObject nameTextObj = new GameObject("NameText");
        nameTextObj.transform.SetParent(productBtn.transform, false);
        TextMeshProUGUI nameText = nameTextObj.AddComponent<TextMeshProUGUI>();
        nameText.text = productName;
        nameText.font = customFont;
        nameText.fontSize = 16; // 작은 폰트
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.black;

        RectTransform nameRect = nameTextObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0);
        nameRect.anchorMax = new Vector2(1, 0);
        nameRect.pivot = new Vector2(0.5f, 0);
        nameRect.anchoredPosition = new Vector2(0, 5);
        nameRect.sizeDelta = new Vector2(-10, 20);

        // 가격 텍스트
        GameObject priceTextObj = new GameObject("PriceText");
        priceTextObj.transform.SetParent(productBtn.transform, false);
        TextMeshProUGUI priceText = priceTextObj.AddComponent<TextMeshProUGUI>();
        priceText.text = price.ToString() + "원";
        priceText.font = customFont;
        priceText.fontSize = 14; // 더 작은 폰트
        priceText.alignment = TextAlignmentOptions.Center;
        priceText.color = new Color(0.2f, 0.2f, 0.2f);

        RectTransform priceRect = priceTextObj.GetComponent<RectTransform>();
        priceRect.anchorMin = new Vector2(0, 0);
        priceRect.anchorMax = new Vector2(1, 0);
        priceRect.pivot = new Vector2(0.5f, 0);
        priceRect.anchoredPosition = new Vector2(0, 30);
        priceRect.sizeDelta = new Vector2(-10, 20);

        ProductInteractable productScript = productBtn.AddComponent<ProductInteractable>();
        productScript.productData = new ProductData(productName, price);
        productScript.nameText = nameText;
        productScript.priceText = priceText;

        // 버튼 이벤트는 Start()에서 자동 연결됨

        Debug.Log($"Product Button 생성: {productName}, Position: {btnRect.anchoredPosition}");
#endif
    }

    void CreateCheckoutCounter()
    {
#if UNITY_EDITOR
        Canvas canvas = FindFirstObjectByType<Canvas>();

        // 계산대 전용 Canvas (가장 낮은 레이어 - 모든 UI 뒤에)
        GameObject counterCanvas = new GameObject("CheckoutCounterCanvas");
        Canvas counterCanvasComp = counterCanvas.AddComponent<Canvas>();
        counterCanvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
        counterCanvasComp.sortingOrder = -100; // 가장 낮은 레이어 (모든 UI 뒤)
        counterCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // === 1. 계산대 메인 (아래쪽 전체, 1인칭 시점) ===
        GameObject counter3D = new GameObject("CheckoutCounter");
        counter3D.transform.SetParent(counterCanvas.transform, false);

        Image counterImage = counter3D.AddComponent<Image>();
        counterImage.color = new Color(0.6f, 0.4f, 0.2f); // 갈색 계산대

        RectTransform counterRect = counter3D.GetComponent<RectTransform>();
        counterRect.anchorMin = new Vector2(0.5f, 0);
        counterRect.anchorMax = new Vector2(0.5f, 0);
        counterRect.pivot = new Vector2(0.5f, 0);
        counterRect.anchoredPosition = new Vector2(0, 0); // 화면 최하단에 딱 붙임
        counterRect.sizeDelta = new Vector2(1400, 250); // 높이 약간 줄임 (300 → 250)

        // === 2. 왼쪽 영역 (포스기/스캐너 공간) - 어두운 갈색 ===
        GameObject leftArea = new GameObject("LeftArea_POSZone");
        leftArea.transform.SetParent(counter3D.transform, false);

        Image leftImage = leftArea.AddComponent<Image>();
        leftImage.color = new Color(0.5f, 0.3f, 0.15f); // 진한 갈색

        RectTransform leftRect = leftArea.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0, 1);
        leftRect.pivot = new Vector2(0, 0.5f);
        leftRect.anchoredPosition = new Vector2(10, 0);
        leftRect.sizeDelta = new Vector2(550, -20); // 왼쪽 절반

        // === 3. 오른쪽 영역 (손님 상품 공간) - 밝은 회색 ===
        GameObject rightArea = new GameObject("RightArea_CustomerZone");
        rightArea.transform.SetParent(counter3D.transform, false);

        Image rightImage = rightArea.AddComponent<Image>();
        rightImage.color = new Color(0.75f, 0.75f, 0.7f); // 밝은 회색

        RectTransform rightRect = rightArea.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(1, 0);
        rightRect.anchorMax = new Vector2(1, 1);
        rightRect.pivot = new Vector2(1, 0.5f);
        rightRect.anchoredPosition = new Vector2(-10, 0);
        rightRect.sizeDelta = new Vector2(550, -20); // 오른쪽 절반

        // 오른쪽 라벨
        GameObject rightLabelObj = new GameObject("RightLabel");
        rightLabelObj.transform.SetParent(rightArea.transform, false);

        TextMeshProUGUI rightLabel = rightLabelObj.AddComponent<TextMeshProUGUI>();
        rightLabel.text = "[ 손님 구역 ]";
        rightLabel.font = customFont;
        rightLabel.fontSize = 20;
        rightLabel.alignment = TextAlignmentOptions.Center;
        rightLabel.color = new Color(0.3f, 0.3f, 0.3f);
        rightLabel.fontStyle = FontStyles.Bold;

        RectTransform rightLabelRect = rightLabelObj.GetComponent<RectTransform>();
        rightLabelRect.anchorMin = new Vector2(0.5f, 1);
        rightLabelRect.anchorMax = new Vector2(0.5f, 1);
        rightLabelRect.pivot = new Vector2(0.5f, 1);
        rightLabelRect.anchoredPosition = new Vector2(0, -10);
        rightLabelRect.sizeDelta = new Vector2(300, 30);

        // === 거스름돈 드랍 존 (손님 구역 아래쪽) - 별도 Canvas로 최상위 배치 ===
        GameObject changeDropCanvas = new GameObject("ChangeDropCanvas");
        Canvas changeCanvas = changeDropCanvas.AddComponent<Canvas>();
        changeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        changeCanvas.sortingOrder = 250; // 계산대보다 높고 DragCanvas보다 낮음
        changeDropCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject changeDropZone = new GameObject("ChangeMoneyDropZone");
        changeDropZone.transform.SetParent(changeDropCanvas.transform, false);

        Image changeZoneBg = changeDropZone.AddComponent<Image>();
        changeZoneBg.color = new Color(0.3f, 0.6f, 0.9f, 0.4f); // 반투명 파란색

        RectTransform changeZoneRect = changeDropZone.GetComponent<RectTransform>();
        // 화면 절대 좌표로 배치 (손님 구역 아래쪽)
        changeZoneRect.anchorMin = new Vector2(0.5f, 0);
        changeZoneRect.anchorMax = new Vector2(0.5f, 0);
        changeZoneRect.pivot = new Vector2(0.5f, 0);
        changeZoneRect.anchoredPosition = new Vector2(350, 10); // 손님 구역 가운데 (화면 오른쪽)
        changeZoneRect.sizeDelta = new Vector2(500, 80); // 충분히 큰 크기

        ChangeMoneyDropZone dropZone = changeDropZone.AddComponent<ChangeMoneyDropZone>();

        // 거스름돈을 가로로 나열하기 위한 레이아웃
        HorizontalLayoutGroup changeLayout = changeDropZone.AddComponent<HorizontalLayoutGroup>();
        changeLayout.spacing = 5;
        changeLayout.childAlignment = TextAnchor.MiddleCenter;
        changeLayout.childControlWidth = false;
        changeLayout.childControlHeight = false;
        changeLayout.childForceExpandWidth = false;
        changeLayout.childForceExpandHeight = false;
        changeLayout.padding = new RectOffset(10, 10, 10, 10);

        // 거스름돈 표시 텍스트 (드랍 존 위쪽으로 분리)
        GameObject changeTotalObj = new GameObject("ChangeTotalText");
        changeTotalObj.transform.SetParent(changeDropCanvas.transform, false); // Canvas 직접 자식

        TextMeshProUGUI changeTotalText = changeTotalObj.AddComponent<TextMeshProUGUI>();
        changeTotalText.text = "거스름돈: 0원";
        changeTotalText.font = customFont;
        changeTotalText.fontSize = 22;
        changeTotalText.alignment = TextAlignmentOptions.Center;
        changeTotalText.color = new Color(0.1f, 0.3f, 0.8f);
        changeTotalText.fontStyle = FontStyles.Bold;

        RectTransform changeTotalRect = changeTotalObj.GetComponent<RectTransform>();
        changeTotalRect.anchorMin = new Vector2(0.5f, 0);
        changeTotalRect.anchorMax = new Vector2(0.5f, 0);
        changeTotalRect.pivot = new Vector2(0.5f, 0);
        changeTotalRect.anchoredPosition = new Vector2(350, 100); // 드랍존 위쪽 (손님 구역)
        changeTotalRect.sizeDelta = new Vector2(500, 30); // 드랍존과 같은 너비

        dropZone.totalChangeText = changeTotalText;

        Debug.Log("거스름돈 드랍 존 생성됨 (손님 구역)");

        // === 4. 중앙 구분선 ===
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(counter3D.transform, false);

        Image dividerImage = divider.AddComponent<Image>();
        dividerImage.color = new Color(0.3f, 0.2f, 0.1f); // 어두운 선

        RectTransform dividerRect = divider.GetComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0.5f, 0);
        dividerRect.anchorMax = new Vector2(0.5f, 1);
        dividerRect.pivot = new Vector2(0.5f, 0.5f);
        dividerRect.anchoredPosition = new Vector2(0, 0);
        dividerRect.sizeDelta = new Vector2(5, -20);

        // === 5. CheckoutCounter 컴포넌트용 홀더 ===
        GameObject counterHolder = new GameObject("CheckoutCounterHolder");
        CheckoutCounter counter = counterHolder.AddComponent<CheckoutCounter>();
        counter.counterPosition = rightArea.transform; // 오른쪽 영역이 상품 배치 공간
        counter.itemSpacing = 1.2f;

        // === 6. 바코드 스캐너 (최상위 캔버스에 배치 - 모든 UI보다 앞에) ===
        GameObject scannerCanvas = new GameObject("ScannerCanvas");
        Canvas scanCanvas = scannerCanvas.AddComponent<Canvas>();
        scanCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        scanCanvas.sortingOrder = 600; // 바코드 인벤토리(500)보다 높음
        scannerCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject scanner = new GameObject("BarcodeScanner");
        scanner.transform.SetParent(scannerCanvas.transform, false);

        Image scannerImage = scanner.AddComponent<Image>();
        scannerImage.color = Color.gray;

        RectTransform scannerRect = scanner.GetComponent<RectTransform>();
        // 화면 기준 절대 좌표로 배치 (계산대 왼쪽 영역)
        scannerRect.anchorMin = new Vector2(0f, 0f);
        scannerRect.anchorMax = new Vector2(0f, 0f);
        scannerRect.pivot = new Vector2(0.5f, 0.5f);
        scannerRect.anchoredPosition = new Vector2(270, 200); // 화면 좌측 하단에서 적당한 위치
        scannerRect.sizeDelta = new Vector2(70, 70); // 더 크게

        BarcodeScanner scannerScript = scanner.AddComponent<BarcodeScanner>();
        /*        scannerScript.scanRange = 100f; // 범위 증가
                scannerScript.scanReadyColor = Color.green;
                scannerScript.scanNotReadyColor = Color.gray;*/

        // 스캐너 안내 텍스트
        GameObject scannerLabelObj = new GameObject("ScannerLabel");
        scannerLabelObj.transform.SetParent(scanner.transform, false);

        TextMeshProUGUI scannerLabel = scannerLabelObj.AddComponent<TextMeshProUGUI>();
        scannerLabel.text = "SCAN";
        scannerLabel.font = customFont;
        scannerLabel.fontSize = 20;
        scannerLabel.alignment = TextAlignmentOptions.Center;
        scannerLabel.color = Color.cyan;
        scannerLabel.fontStyle = FontStyles.Bold;

        RectTransform labelRect = scannerLabelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        // === 8. 실제 편의점 포스기 (왼쪽 영역 오른쪽에 배치) ===
        CreateRealPOSMachine(leftArea.transform);

        Debug.Log("6. Checkout Counter created (스캐너 최상위 배치)");
#endif
    }

    void CreateRealPOSMachine(Transform parent)
    {
#if UNITY_EDITOR
        Canvas canvas = FindFirstObjectByType<Canvas>();

        // 포스기 본체 (검은색 틀)
        GameObject posMachine = new GameObject("POSMachine");
        posMachine.transform.SetParent(parent, false);

        Image posFrame = posMachine.AddComponent<Image>();
        posFrame.color = new Color(0.15f, 0.15f, 0.15f, 1f); // 검은색 틀

        RectTransform posRect = posMachine.GetComponent<RectTransform>();
        posRect.anchorMin = new Vector2(0.65f, 1); // 위쪽 기준으로 변경
        posRect.anchorMax = new Vector2(0.65f, 1);
        posRect.pivot = new Vector2(0.5f, 1);
        posRect.anchoredPosition = new Vector2(-200, 200); // 화면 최상단에 딱 붙임
        posRect.sizeDelta = new Vector2(240, 260); // 조금 더 크게

        // 포스기 화면 (LCD)
        GameObject posScreen = new GameObject("POSScreen");
        posScreen.transform.SetParent(posMachine.transform, false);

        Image screenBg = posScreen.AddComponent<Image>();
        screenBg.color = new Color(0.2f, 0.3f, 0.25f, 1f); // 어두운 LCD 녹색

        RectTransform screenRect = posScreen.GetComponent<RectTransform>();
        screenRect.anchorMin = new Vector2(0.5f, 1);
        screenRect.anchorMax = new Vector2(0.5f, 1);
        screenRect.pivot = new Vector2(0.5f, 1);
        screenRect.anchoredPosition = new Vector2(0, -10);
        screenRect.sizeDelta = new Vector2(200, 150); // 큰 화면

        // 화면 테두리
        GameObject screenBorder = new GameObject("Border");
        screenBorder.transform.SetParent(posScreen.transform, false);

        Image borderImg = screenBorder.AddComponent<Image>();
        borderImg.color = new Color(0.1f, 0.1f, 0.1f, 1f);

        RectTransform borderRect = screenBorder.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-2, -2);
        borderRect.offsetMax = new Vector2(2, 2);

        // 현재 스캔 중인 금액 표시 (LCD 화면)
        GameObject currentPriceObj = new GameObject("CurrentPriceDisplay");
        currentPriceObj.transform.SetParent(posScreen.transform, false);

        TextMeshProUGUI currentPriceText = currentPriceObj.AddComponent<TextMeshProUGUI>();
        currentPriceText.text = "0원";
        currentPriceText.font = customFont;
        currentPriceText.fontSize = 48; // 매우 큰 폰트
        currentPriceText.alignment = TextAlignmentOptions.Center;
        currentPriceText.color = new Color(0.3f, 1f, 0.3f, 1f); // 밝은 녹색 LCD
        currentPriceText.fontStyle = FontStyles.Bold;

        RectTransform currentPriceRect = currentPriceObj.GetComponent<RectTransform>();
        currentPriceRect.anchorMin = new Vector2(0, 0.5f);
        currentPriceRect.anchorMax = new Vector2(1, 1);
        currentPriceRect.pivot = new Vector2(0.5f, 0.5f);
        currentPriceRect.anchoredPosition = Vector2.zero;
        currentPriceRect.sizeDelta = Vector2.zero;

        // 상태 표시 (하단)
        GameObject statusObj = new GameObject("StatusDisplay");
        statusObj.transform.SetParent(posScreen.transform, false);

        TextMeshProUGUI statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "대기 중";
        statusText.font = customFont;
        statusText.fontSize = 14;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = new Color(0.5f, 0.8f, 0.5f, 1f); // 중간 녹색
        statusText.fontStyle = FontStyles.Normal;

        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0);
        statusRect.anchorMax = new Vector2(1, 0.4f);
        statusRect.pivot = new Vector2(0.5f, 0.5f);
        statusRect.anchoredPosition = Vector2.zero;
        statusRect.sizeDelta = Vector2.zero;

        // 현금 서랍 (하단)
        GameObject cashDrawer = new GameObject("CashDrawer");
        cashDrawer.transform.SetParent(posMachine.transform, false);

        Image drawerBg = cashDrawer.AddComponent<Image>();
        drawerBg.color = new Color(0.25f, 0.25f, 0.25f, 1f); // 회색 서랍

        Button drawerButton = cashDrawer.AddComponent<Button>();

        RectTransform drawerRect = cashDrawer.GetComponent<RectTransform>();
        drawerRect.anchorMin = new Vector2(0, 0);
        drawerRect.anchorMax = new Vector2(1, 0);
        drawerRect.pivot = new Vector2(0.5f, 0);
        drawerRect.anchoredPosition = new Vector2(0, 5);
        drawerRect.sizeDelta = new Vector2(-10, 60);

        // 서랍 라벨
        GameObject drawerLabelObj = new GameObject("DrawerLabel");
        drawerLabelObj.transform.SetParent(cashDrawer.transform, false);

        TextMeshProUGUI drawerLabel = drawerLabelObj.AddComponent<TextMeshProUGUI>();
        drawerLabel.text = "[ 현금 서랍 ]";
        drawerLabel.font = customFont;
        drawerLabel.fontSize = 14;
        drawerLabel.alignment = TextAlignmentOptions.Center;
        drawerLabel.color = new Color(0.7f, 0.7f, 0.7f);
        drawerLabel.fontStyle = FontStyles.Bold;

        RectTransform drawerLabelRect = drawerLabelObj.GetComponent<RectTransform>();
        drawerLabelRect.anchorMin = Vector2.zero;
        drawerLabelRect.anchorMax = Vector2.one;
        drawerLabelRect.offsetMin = Vector2.zero;
        drawerLabelRect.offsetMax = Vector2.zero;

        // ========== 진짜 돈 서랍 (포스기 바로 아래) - 최상위 레이어로 ==========
        // FIX: 별도 Canvas로 최상위 배치 (다른 UI에 안 가림)
        GameObject realDrawerCanvas = new GameObject("RealMoneyDrawerCanvas");
        Canvas realDrawerCanvasComp = realDrawerCanvas.AddComponent<Canvas>();
        realDrawerCanvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
        realDrawerCanvasComp.sortingOrder = 350; // DragCanvas(300)보다 높음
        realDrawerCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject drawerPanel = new GameObject("RealMoneyDrawerPanel");
        drawerPanel.transform.SetParent(realDrawerCanvas.transform, false); // Canvas 자식으로

        Image drawerPanelBg = drawerPanel.AddComponent<Image>();
        drawerPanelBg.color = new Color(0.8f, 0.7f, 0.5f, 1f); // 금속 색

        RectTransform drawerPanelRect = drawerPanel.GetComponent<RectTransform>();
        drawerPanelRect.anchorMin = new Vector2(0f, 0f); // 화면 기준 절대 좌표
        drawerPanelRect.anchorMax = new Vector2(0f, 0f);
        drawerPanelRect.pivot = new Vector2(0f, 0f);
        drawerPanelRect.anchoredPosition = new Vector2(200, 50); // 화면 왼쪽 하단
        drawerPanelRect.sizeDelta = new Vector2(150, 240); // 가짜돈통과 같은 크기

        drawerPanel.SetActive(false); // 초기에는 숨김

        // 진짜 돈 서랍 타이틀
        GameObject realTitleObj = new GameObject("RealMoneyTitle");
        realTitleObj.transform.SetParent(drawerPanel.transform, false);

        TextMeshProUGUI realTitle = realTitleObj.AddComponent<TextMeshProUGUI>();
        realTitle.text = "[ 진짜 돈 ]";
        realTitle.font = customFont;
        realTitle.fontSize = 18;
        realTitle.alignment = TextAlignmentOptions.Center;
        realTitle.color = new Color(0.2f, 0.6f, 0.2f);
        realTitle.fontStyle = FontStyles.Bold;

        RectTransform realTitleRect = realTitleObj.GetComponent<RectTransform>();
        realTitleRect.anchorMin = new Vector2(0, 1);
        realTitleRect.anchorMax = new Vector2(1, 1);
        realTitleRect.pivot = new Vector2(0.5f, 1);
        realTitleRect.anchoredPosition = new Vector2(0, -10);
        realTitleRect.sizeDelta = new Vector2(-20, 25);

        // 진짜 돈 영역
        GameObject realMoneyArea = new GameObject("RealMoneyArea");
        realMoneyArea.transform.SetParent(drawerPanel.transform, false);

        Image realAreaBg = realMoneyArea.AddComponent<Image>();
        realAreaBg.color = new Color(0.9f, 0.85f, 0.7f, 1f); // 밝은 금속색

        RectTransform realAreaRect = realMoneyArea.GetComponent<RectTransform>();
        realAreaRect.anchorMin = new Vector2(0, 0); // 하단 기준
        realAreaRect.anchorMax = new Vector2(1, 0);
        realAreaRect.pivot = new Vector2(0.5f, 0);
        realAreaRect.anchoredPosition = new Vector2(0, 5); // 하단에서 5px 위
        realAreaRect.sizeDelta = new Vector2(-20, 200); // 가짜돈통과 동일

        // 진짜 돈 그리드 (6칸: 3행 2열)
        GridLayoutGroup realGrid = realMoneyArea.AddComponent<GridLayoutGroup>();
        realGrid.cellSize = new Vector2(60, 60); // 돈 크기에 맞춤
        realGrid.spacing = new Vector2(5, 5);
        realGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
        realGrid.childAlignment = TextAnchor.MiddleCenter;
        realGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        realGrid.constraintCount = 2; // 2열 고정 (3행으로 배치됨)
        realGrid.padding = new RectOffset(10, 10, 10, 10);

        // 돈 UI는 인스펙터에서 할당하므로 코드에서 생성하지 않음

        // 진짜 돈 서랍 처음에는 닫힌 상태
        drawerPanel.SetActive(false);

        // 닫기 버튼 제거 - 현금통 버튼으로 토글

        // ========== 가짜 돈 서랍 (포스기 근처에 항상 표시) ==========
        // 가짜 돈통 전용 Canvas (바코드 인벤토리 아래 레이어)
        GameObject fakeDrawerCanvas = new GameObject("FakeMoneyDrawerCanvas");
        Canvas fakeDrawerCanvasComp = fakeDrawerCanvas.AddComponent<Canvas>();
        fakeDrawerCanvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
        fakeDrawerCanvasComp.sortingOrder = 450; // 바코드 인벤토리(500)보다 아래
        fakeDrawerCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject fakeDrawerPanel = new GameObject("FakeMoneyDrawerPanel");
        fakeDrawerPanel.transform.SetParent(fakeDrawerCanvas.transform, false);

        Image fakeDrawerBg = fakeDrawerPanel.AddComponent<Image>();
        fakeDrawerBg.color = new Color(0.7f, 0.6f, 0.5f, 1f); // 약간 다른 색

        RectTransform fakeDrawerRect = fakeDrawerPanel.GetComponent<RectTransform>();
        fakeDrawerRect.anchorMin = new Vector2(0f, 0f); // 바코드 스캐너 근처
        fakeDrawerRect.anchorMax = new Vector2(0f, 0f);
        fakeDrawerRect.pivot = new Vector2(0f, 0f); // 왼쪽 하단 피벗
        fakeDrawerRect.anchoredPosition = new Vector2(650, 30); // 스캐너(270, 200) 오른쪽
        fakeDrawerRect.sizeDelta = new Vector2(150, 240); // 원래 크기 유지

        // 항상 표시 (SetActive(false) 제거)

        // 가짜 돈 서랍 타이틀
        GameObject fakeTitleObj = new GameObject("FakeMoneyTitle");
        fakeTitleObj.transform.SetParent(fakeDrawerPanel.transform, false);

        TextMeshProUGUI fakeTitle = fakeTitleObj.AddComponent<TextMeshProUGUI>();
        fakeTitle.text = "[ 가짜 돈 ]";
        fakeTitle.font = customFont;
        fakeTitle.fontSize = 18;
        fakeTitle.alignment = TextAlignmentOptions.Center;
        fakeTitle.color = new Color(0.8f, 0.3f, 0.3f);
        fakeTitle.fontStyle = FontStyles.Bold;

        RectTransform fakeTitleRect = fakeTitleObj.GetComponent<RectTransform>();
        fakeTitleRect.anchorMin = new Vector2(0, 1);
        fakeTitleRect.anchorMax = new Vector2(1, 1);
        fakeTitleRect.pivot = new Vector2(0.5f, 1);
        fakeTitleRect.anchoredPosition = new Vector2(0, -10);
        fakeTitleRect.sizeDelta = new Vector2(-20, 25);

        // 가짜 돈 영역
        GameObject fakeMoneyArea = new GameObject("FakeMoneyArea");
        fakeMoneyArea.transform.SetParent(fakeDrawerPanel.transform, false);

        Image fakeAreaBg = fakeMoneyArea.AddComponent<Image>();
        fakeAreaBg.color = new Color(0.85f, 0.75f, 0.65f, 1f);

        RectTransform fakeAreaRect = fakeMoneyArea.GetComponent<RectTransform>();
        fakeAreaRect.anchorMin = new Vector2(0, 0); // 하단 기준으로 변경
        fakeAreaRect.anchorMax = new Vector2(1, 0); // 하단 기준
        fakeAreaRect.pivot = new Vector2(0.5f, 0); // 하단 피벗
        fakeAreaRect.anchoredPosition = new Vector2(0, 5); // 하단에서 5px 위
        fakeAreaRect.sizeDelta = new Vector2(-20, 200); // 높이 200 (60x60 돈 3행 + 여백)

        // 가짜 돈 그리드
        GridLayoutGroup fakeGrid = fakeMoneyArea.AddComponent<GridLayoutGroup>();
        fakeGrid.cellSize = new Vector2(60, 60); // 돈 크기에 맞춤
        fakeGrid.spacing = new Vector2(5, 5);
        fakeGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
        fakeGrid.childAlignment = TextAnchor.MiddleCenter;
        fakeGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        fakeGrid.constraintCount = 2;
        fakeGrid.padding = new RectOffset(10, 10, 10, 10);

        // 가짜 돈 서랍은 닫기 버튼 없음 (항상 표시)

        // === 3000원, 5000원 바코드 (가짜돈통 옆에 배치) ===
        GameObject extraBarcodeCanvas = new GameObject("ExtraBarcodeCanvas");
        Canvas extraCanvas = extraBarcodeCanvas.AddComponent<Canvas>();
        extraCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        extraCanvas.sortingOrder = 150; // 중간 레이어
        extraBarcodeCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // 3000원 바코드
        GameObject barcode3000 = new GameObject("Barcode_3000");
        barcode3000.transform.SetParent(extraBarcodeCanvas.transform, false);

        Image barcode3000Bg = barcode3000.AddComponent<Image>();
        barcode3000Bg.color = new Color(1f, 0.9f, 0.7f, 1f); // 연한 노란색

        RectTransform barcode3000Rect = barcode3000.GetComponent<RectTransform>();
        barcode3000Rect.anchorMin = new Vector2(0f, 0f);
        barcode3000Rect.anchorMax = new Vector2(0f, 0f);
        barcode3000Rect.pivot = new Vector2(0f, 0f);
        barcode3000Rect.anchoredPosition = new Vector2(820, 180); // 가짜돈통(650) 오른쪽
        barcode3000Rect.sizeDelta = new Vector2(70, 50);

        GameObject barcode3000TextObj = new GameObject("Text");
        barcode3000TextObj.transform.SetParent(barcode3000.transform, false);

        TextMeshProUGUI barcode3000Text = barcode3000TextObj.AddComponent<TextMeshProUGUI>();
        barcode3000Text.text = "3000W";
        barcode3000Text.font = customFont;
        barcode3000Text.fontSize = 16;
        barcode3000Text.alignment = TextAlignmentOptions.Center;
        barcode3000Text.color = Color.black;

        RectTransform barcode3000TextRect = barcode3000TextObj.GetComponent<RectTransform>();
        barcode3000TextRect.anchorMin = Vector2.zero;
        barcode3000TextRect.anchorMax = Vector2.one;
        barcode3000TextRect.offsetMin = Vector2.zero;
        barcode3000TextRect.offsetMax = Vector2.zero;

        DraggableBarcode draggable3000 = barcode3000.AddComponent<DraggableBarcode>();
        barcode3000.AddComponent<CanvasGroup>();
        draggable3000.priceText = barcode3000Text;
        draggable3000.simplePrice = 3000; // 간단 가격 설정

        // 5000원 바코드
        GameObject barcode5000 = new GameObject("Barcode_5000");
        barcode5000.transform.SetParent(extraBarcodeCanvas.transform, false);

        Image barcode5000Bg = barcode5000.AddComponent<Image>();
        barcode5000Bg.color = new Color(1f, 0.9f, 0.7f, 1f);

        RectTransform barcode5000Rect = barcode5000.GetComponent<RectTransform>();
        barcode5000Rect.anchorMin = new Vector2(0f, 0f);
        barcode5000Rect.anchorMax = new Vector2(0f, 0f);
        barcode5000Rect.pivot = new Vector2(0f, 0f);
        barcode5000Rect.anchoredPosition = new Vector2(820, 120); // 3000원 아래
        barcode5000Rect.sizeDelta = new Vector2(70, 50);

        GameObject barcode5000TextObj = new GameObject("Text");
        barcode5000TextObj.transform.SetParent(barcode5000.transform, false);

        TextMeshProUGUI barcode5000Text = barcode5000TextObj.AddComponent<TextMeshProUGUI>();
        barcode5000Text.text = "5000W";
        barcode5000Text.font = customFont;
        barcode5000Text.fontSize = 16;
        barcode5000Text.alignment = TextAlignmentOptions.Center;
        barcode5000Text.color = Color.black;

        RectTransform barcode5000TextRect = barcode5000TextObj.GetComponent<RectTransform>();
        barcode5000TextRect.anchorMin = Vector2.zero;
        barcode5000TextRect.anchorMax = Vector2.one;
        barcode5000TextRect.offsetMin = Vector2.zero;
        barcode5000TextRect.offsetMax = Vector2.zero;

        DraggableBarcode draggable5000 = barcode5000.AddComponent<DraggableBarcode>();
        barcode5000.AddComponent<CanvasGroup>();
        draggable5000.priceText = barcode5000Text;
        draggable5000.simplePrice = 5000; // 간단 가격 설정

        Debug.Log("3000원, 5000원 바코드 생성됨 (가짜돈통 옆)");

        // 버튼 이벤트 연결
        drawerButton.onClick.RemoveAllListeners(); // 기존 리스너 제거
        drawerButton.onClick.AddListener(() =>
        {
            // 단순 토글 (보이기/숨기기만)
            bool isOpen = drawerPanel.activeSelf;

            if (isOpen)
            {
                drawerPanel.SetActive(false);
                Debug.Log($"[현금 서랍] 닫힘!");
            }
            else
            {
                drawerPanel.SetActive(true);
                Debug.Log($"[현금 서랍] 열림!");
            }
        });

        // 가짜 돈은 인스펙터에서 할당하므로 코드에서 생성하지 않음
        // CreateMoneyInDrawer(fakeMoneyArea.transform, moneyPrefabCache, true);

        // === POS 거래내역 버튼 (포스기 옆에 부착) ===
        GameObject posMenuButton = new GameObject("POSMenuButton");
        posMenuButton.transform.SetParent(posMachine.transform, false);

        Image posMenuBg = posMenuButton.AddComponent<Image>();
        posMenuBg.color = new Color(0.2f, 0.2f, 0.2f, 1f); // 검은 버튼

        Button posMenuBtn = posMenuButton.AddComponent<Button>();

        RectTransform posMenuRect = posMenuButton.GetComponent<RectTransform>();
        posMenuRect.anchorMin = new Vector2(1, 1);
        posMenuRect.anchorMax = new Vector2(1, 1);
        posMenuRect.pivot = new Vector2(0, 1);
        posMenuRect.anchoredPosition = new Vector2(10, 0); // 포스기 오른쪽
        posMenuRect.sizeDelta = new Vector2(70, 100); // 세로로 긴 버튼

        GameObject posMenuTextObj = new GameObject("Text");
        posMenuTextObj.transform.SetParent(posMenuButton.transform, false);

        TextMeshProUGUI posMenuText = posMenuTextObj.AddComponent<TextMeshProUGUI>();
        posMenuText.text = "거래\n내역";
        posMenuText.font = customFont;
        posMenuText.fontSize = 16;
        posMenuText.alignment = TextAlignmentOptions.Center;
        posMenuText.color = Color.white;
        posMenuText.fontStyle = FontStyles.Bold;

        RectTransform posMenuTextRect = posMenuTextObj.GetComponent<RectTransform>();
        posMenuTextRect.anchorMin = Vector2.zero;
        posMenuTextRect.anchorMax = Vector2.one;
        posMenuTextRect.offsetMin = Vector2.zero;
        posMenuTextRect.offsetMax = Vector2.zero;

        // POSMachineDisplay 컴포넌트 추가
        POSMachineDisplay posDisplay = posMachine.AddComponent<POSMachineDisplay>();
        posDisplay.currentPriceText = currentPriceText;
        posDisplay.statusText = statusText;
        posDisplay.drawerPanel = drawerPanel; // 진짜 돈 서랍만 할당 (현금통 버튼으로 토글)
        posDisplay.posMenuButton = posMenuBtn;

        Debug.Log("Real POS Machine created with LCD, real money drawer, and fake money drawer");
#endif
    }

    GameObject CreateMoneyPrefab(Canvas canvas)
    {
#if UNITY_EDITOR
        string prefabPath = "Assets/Prefabs";

        // Money Prefab (드래그 가능한 돈) - 텍스트 없음
        GameObject moneyObj = new GameObject("MoneyPrefab");
        moneyObj.transform.SetParent(canvas.transform, false);

        Image moneyBg = moneyObj.AddComponent<Image>();
        moneyBg.color = Color.white; // 하얀색 (스프라이트 원본 색상 유지)
        moneyBg.preserveAspect = true; // 이미지 비율 유지

        RectTransform moneyRect = moneyObj.GetComponent<RectTransform>();
        moneyRect.sizeDelta = new Vector2(60, 60); // 크기 60x60

        // 텍스트 제거 - 이미지에 금액이 표시되므로

        // Draggable 컴포넌트 추가
        DraggableMoney draggable = moneyObj.AddComponent<DraggableMoney>();
        moneyObj.AddComponent<CanvasGroup>();

        string fullPath = prefabPath + "/MoneyPrefab.prefab";
        GameObject prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(moneyObj, fullPath);

        DestroyImmediate(moneyObj);

        return prefab;
#else
        return null;
#endif
    }

    // CreateMoneyInDrawer 함수는 사용하지 않음 - 인스펙터에서 돈 UI 할당
    /*
    void CreateMoneyInDrawer(Transform parent, GameObject moneyPrefab, bool isFake)
    {
        Debug.Log($"CreateMoneyInDrawer 호출됨! isFake: {isFake}, Parent: {parent.name}");

        // 기존 돈 삭제 (중복 생성 방지)
        foreach (Transform child in parent)
        {
            if (child.GetComponent<DraggableMoney>() != null)
            {
                Destroy(child.gameObject);
            }
        }

        if (moneyPrefab == null)
        {
            Debug.LogError("Money Prefab이 NULL입니다!");
            return;
        }

        // 6종류 돈: 100, 500, 1000, 5000, 10000, 50000
        string[] moneyLabels = new string[] { "100원", "500원", "1000원", "5000원", "10000원", "50000원" };
        int[] moneyValues = new int[] { 100, 500, 1000, 5000, 10000, 50000 };

        for (int i = 0; i < moneyLabels.Length; i++)
        {
            GameObject moneyObj = Instantiate(moneyPrefab, parent);
            Debug.Log($"돈 생성: {moneyLabels[i]}");

            // 텍스트 제거 - 이미지에 금액 표시됨

            DraggableMoney draggable = moneyObj.GetComponent<DraggableMoney>();
            if (draggable != null)
            {
                draggable.moneyAmount = moneyValues[i];
                draggable.isFakeMoney = isFake;
            }

            // 색상은 하얀색으로 (스프라이트 원본 색상 유지)
            Image bg = moneyObj.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = Color.white; // 모든 돈 하얀색
            }
        }
    }
    */

    void CreatePOSSystem()
    {
#if UNITY_EDITOR
        Canvas canvas = FindFirstObjectByType<Canvas>();

        // POS 홀더
        GameObject posHolder = new GameObject("POSSystemHolder");
        posHolder.transform.SetParent(canvas.transform, false);

        // POS 패널 (화면 왼쪽 하단 - 계산대 POS 구역 위)
        GameObject posPanel = new GameObject("POSPanel");
        posPanel.transform.SetParent(posHolder.transform, false);

        Image panelImage = posPanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f); // 검은색

        RectTransform panelRect = posPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 기준
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0, 0); // 화면 정중앙
        panelRect.sizeDelta = new Vector2(450, 550); // 조금 더 크게

        // 제목
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(posPanel.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "[ POS 시스템 ]";
        titleText.font = customFont;
        titleText.fontSize = 24;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1);
        titleRect.anchorMax = new Vector2(0.5f, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -10);
        titleRect.sizeDelta = new Vector2(330, 30);

        // 통계 패널
        GameObject statsPanel = new GameObject("StatsPanel");
        statsPanel.transform.SetParent(posPanel.transform, false);

        RectTransform statsRect = statsPanel.AddComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0, 1);
        statsRect.anchorMax = new Vector2(1, 1);
        statsRect.pivot = new Vector2(0.5f, 1);
        statsRect.anchoredPosition = new Vector2(0, -50);
        statsRect.sizeDelta = new Vector2(-20, 80);

        // 총 매출
        GameObject salesObj = new GameObject("TotalSales");
        salesObj.transform.SetParent(statsPanel.transform, false);
        TextMeshProUGUI salesText = salesObj.AddComponent<TextMeshProUGUI>();
        salesText.text = "총 매출: 0원";
        salesText.font = customFont;
        salesText.fontSize = 18;
        salesText.alignment = TextAlignmentOptions.Left;
        salesText.color = Color.cyan;

        RectTransform salesRect = salesObj.GetComponent<RectTransform>();
        salesRect.anchorMin = new Vector2(0, 1);
        salesRect.anchorMax = new Vector2(1, 1);
        salesRect.pivot = new Vector2(0, 1);
        salesRect.anchoredPosition = new Vector2(10, -5);
        salesRect.sizeDelta = new Vector2(-20, 30);

        // 총 이익
        GameObject profitObj = new GameObject("TotalProfit");
        profitObj.transform.SetParent(statsPanel.transform, false);
        TextMeshProUGUI profitText = profitObj.AddComponent<TextMeshProUGUI>();
        profitText.text = "총 이익: +0원";
        profitText.font = customFont;
        profitText.fontSize = 18;
        profitText.alignment = TextAlignmentOptions.Left;
        profitText.color = Color.green;

        RectTransform profitRect = profitObj.GetComponent<RectTransform>();
        profitRect.anchorMin = new Vector2(0, 1);
        profitRect.anchorMax = new Vector2(1, 1);
        profitRect.pivot = new Vector2(0, 1);
        profitRect.anchoredPosition = new Vector2(10, -40);
        profitRect.sizeDelta = new Vector2(-20, 30);

        // 거래 내역 스크롤뷰
        GameObject scrollView = new GameObject("TransactionScrollView");
        scrollView.transform.SetParent(posPanel.transform, false);

        Image scrollBg = scrollView.AddComponent<Image>();
        scrollBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        RectTransform scrollRect = scrollView.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.pivot = new Vector2(0.5f, 1);
        scrollRect.anchoredPosition = new Vector2(0, -140);
        scrollRect.sizeDelta = new Vector2(-20, -200);

        ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
        scroll.vertical = true;
        scroll.horizontal = false;

        // 거래 내역 컨테이너
        GameObject content = new GameObject("Content");
        content.transform.SetParent(scrollView.transform, false);

        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 2;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(5, 5, 5, 5);

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;

        // 초기화 버튼
        GameObject clearBtn = new GameObject("ClearButton");
        clearBtn.transform.SetParent(posPanel.transform, false);

        Image clearBtnImage = clearBtn.AddComponent<Image>();
        clearBtnImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);

        Button clearButton = clearBtn.AddComponent<Button>();

        RectTransform clearRect = clearBtn.GetComponent<RectTransform>();
        clearRect.anchorMin = new Vector2(0.5f, 0);
        clearRect.anchorMax = new Vector2(0.5f, 0);
        clearRect.pivot = new Vector2(0.5f, 0);
        clearRect.anchoredPosition = new Vector2(0, 10);
        clearRect.sizeDelta = new Vector2(150, 35);

        GameObject clearTextObj = new GameObject("Text");
        clearTextObj.transform.SetParent(clearBtn.transform, false);
        TextMeshProUGUI clearText = clearTextObj.AddComponent<TextMeshProUGUI>();
        clearText.text = "내역 초기화";
        clearText.font = customFont;
        clearText.fontSize = 16;
        clearText.alignment = TextAlignmentOptions.Center;
        clearText.color = Color.white;

        RectTransform clearTextRect = clearTextObj.GetComponent<RectTransform>();
        clearTextRect.anchorMin = Vector2.zero;
        clearTextRect.anchorMax = Vector2.one;
        clearTextRect.offsetMin = Vector2.zero;
        clearTextRect.offsetMax = Vector2.zero;

        // POSSystem 컴포넌트
        POSSystem pos = posHolder.AddComponent<POSSystem>();
        pos.posPanel = posPanel;
        pos.transactionContainer = content.transform;
        pos.totalSalesText = salesText;
        pos.totalProfitText = profitText;
        pos.clearButton = clearButton;

        // 처음엔 숨김
        posPanel.SetActive(false);

        // 지갑 UI (상단 중앙으로 이동, 크게)
        GameObject walletObj = new GameObject("WalletText");
        walletObj.transform.SetParent(canvas.transform, false);

        TextMeshProUGUI walletText = walletObj.AddComponent<TextMeshProUGUI>();
        walletText.text = "지갑: 0원";
        walletText.font = customFont;
        walletText.fontSize = 36; // 폰트 크기 증가 (24 → 36)
        walletText.alignment = TextAlignmentOptions.Center;
        walletText.color = new Color(1f, 0.9f, 0.2f); // 금색
        walletText.fontStyle = FontStyles.Bold;

        RectTransform walletRect = walletObj.GetComponent<RectTransform>();
        walletRect.anchorMin = new Vector2(0.5f, 1); // 상단 중앙
        walletRect.anchorMax = new Vector2(0.5f, 1);
        walletRect.pivot = new Vector2(0.5f, 1);
        walletRect.anchoredPosition = new Vector2(0, -20); // 상단에서 20px 아래
        walletRect.sizeDelta = new Vector2(300, 50); // 크기 증가

        pos.walletText = walletText;

        // 실수 스택 UI (지갑 바로 아래)
        GameObject mistakeObj = new GameObject("MistakeStackText");
        mistakeObj.transform.SetParent(canvas.transform, false);

        TextMeshProUGUI mistakeText = mistakeObj.AddComponent<TextMeshProUGUI>();
        mistakeText.text = "실수: 0/3";
        mistakeText.font = customFont;
        mistakeText.fontSize = 28;
        mistakeText.alignment = TextAlignmentOptions.Center;
        mistakeText.color = new Color(1f, 0.3f, 0.3f); // 빨간색
        mistakeText.fontStyle = FontStyles.Bold;

        RectTransform mistakeRect = mistakeObj.GetComponent<RectTransform>();
        mistakeRect.anchorMin = new Vector2(0.5f, 1);
        mistakeRect.anchorMax = new Vector2(0.5f, 1);
        mistakeRect.pivot = new Vector2(0.5f, 1);
        mistakeRect.anchoredPosition = new Vector2(0, -75); // 지갑 아래
        mistakeRect.sizeDelta = new Vector2(200, 40);

        pos.mistakeStackText = mistakeText;

        // 게임오버 패널 (최상위 레이어)
        GameObject gameOverCanvas = new GameObject("GameOverCanvas");
        Canvas gameOverCanvasComp = gameOverCanvas.AddComponent<Canvas>();
        gameOverCanvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
        gameOverCanvasComp.sortingOrder = 1000; // 최상위 레이어
        gameOverCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(gameOverCanvas.transform, false);

        Image gameOverBg = gameOverPanel.AddComponent<Image>();
        gameOverBg.color = new Color(0, 0, 0, 0.9f); // 거의 검은색

        RectTransform gameOverRect = gameOverPanel.GetComponent<RectTransform>();
        gameOverRect.anchorMin = Vector2.zero;
        gameOverRect.anchorMax = Vector2.one;
        gameOverRect.offsetMin = Vector2.zero;
        gameOverRect.offsetMax = Vector2.zero;

        // 게임오버 텍스트
        GameObject gameOverTextObj = new GameObject("GameOverText");
        gameOverTextObj.transform.SetParent(gameOverPanel.transform, false);

        TextMeshProUGUI gameOverText = gameOverTextObj.AddComponent<TextMeshProUGUI>();
        gameOverText.text = "GAME OVER\n\n실수를 너무 많이 했습니다!";
        gameOverText.font = customFont;
        gameOverText.fontSize = 60;
        gameOverText.alignment = TextAlignmentOptions.Center;
        gameOverText.color = Color.red;
        gameOverText.fontStyle = FontStyles.Bold;

        RectTransform gameOverTextRect = gameOverTextObj.GetComponent<RectTransform>();
        gameOverTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        gameOverTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        gameOverTextRect.pivot = new Vector2(0.5f, 0.5f);
        gameOverTextRect.anchoredPosition = Vector2.zero;
        gameOverTextRect.sizeDelta = new Vector2(800, 300);

        pos.gameOverPanel = gameOverPanel;

        Debug.Log("7. POS System created (P키로 토글, 지갑 UI, 실수 스택, 게임오버 패널 추가)");
#endif
    }

    void CreateCustomerSystem()
    {
#if UNITY_EDITOR
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas를 찾을 수 없습니다!");
            return;
        }

        // Customer Prefab 생성 (UI로 변경)
        GameObject customerPrefab = new GameObject("CustomerPrefab");
        customerPrefab.transform.SetParent(canvas.transform, false);

        // UI Image 사용 (SpriteRenderer 대신)
        UnityEngine.UI.Image customerImage = customerPrefab.AddComponent<UnityEngine.UI.Image>();
        customerImage.sprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        customerImage.color = new Color(1f, 0.8f, 0.6f); // 살색
        customerImage.raycastTarget = false; // 클릭 차단 안함

        RectTransform customerRect = customerPrefab.GetComponent<RectTransform>();
        customerRect.sizeDelta = new Vector2(100, 140); // 크기 증가 (가로 100, 세로 140)

        // Prefab으로 저장
        string prefabPath = "Assets/Prefabs";
        if (!UnityEditor.AssetDatabase.IsValidFolder(prefabPath))
        {
            UnityEditor.AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        string fullPath = prefabPath + "/CustomerPrefab.prefab";
        GameObject savedPrefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(customerPrefab, fullPath);
        DestroyImmediate(customerPrefab);

        // CustomerManager 생성
        GameObject managerObj = new GameObject("CustomerManager");
        CustomerManager manager = managerObj.AddComponent<CustomerManager>();
        manager.customerPrefab = savedPrefab;
        manager.spawnInterval = 15f; // 15초마다 손님 입장

        // 위치는 이제 Transform으로 설정해야 하므로 주석 처리
        // Unity 인스펙터에서 spawnPosition, enterPosition, bellObject를 수동으로 설정해야 합니다
        // manager.spawnPosition = 손님 스폰 위치 Transform 할당 필요
        // manager.enterPosition = 손님 입장 위치 Transform 할당 필요
        // manager.bellObject = 벨 GameObject 할당 필요

        Debug.Log("5. Customer System created (UI 기반, 작은 크기)");
        Debug.LogWarning("[설정 필요] CustomerManager의 spawnPosition, enterPosition, bellObject를 인스펙터에서 설정하세요!");
#endif
    }
}
