using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 계산대 UI 전담 매니저 - 상품 표시, 돈 표시, 결제 방식 텍스트 등
/// </summary>
public class CheckoutDisplayManager : MonoBehaviour
{
	private List<GameObject> displayedItems = new List<GameObject>();
	private Canvas counterItemsCanvas;
	private GameObject customerMoneyContainer;
	private TextMeshProUGUI paymentMethodText;

	public void Initialize(Transform counterPosition)
	{
		// 계산대 상품 전용 캔버스 생성 (높은 sortOrder)
		GameObject canvasObj = new GameObject("CounterItemsCanvas");
		counterItemsCanvas = canvasObj.AddComponent<Canvas>();
		counterItemsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
		counterItemsCanvas.sortingOrder = 100;
		canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

		Debug.Log("[DisplayManager] 초기화 완료");
	}

	public void DisplayScannedItem(ProductInteractable product)
	{
		// 계산대 위에 상품 복제본 생성
		GameObject itemObj = new GameObject($"CounterItem_{product.productData.productName}_{displayedItems.Count}");
		itemObj.transform.SetParent(counterItemsCanvas.transform, false);

		Image itemImage = itemObj.AddComponent<Image>();
		itemImage.color = product.GetComponent<Image>().color;

		RectTransform itemRect = itemObj.GetComponent<RectTransform>();
		int index = displayedItems.Count;

		// 손님 구역 (화면 하단 우측)에 상품 배치
		itemRect.anchorMin = new Vector2(1f, 0f);
		itemRect.anchorMax = new Vector2(1f, 0f);
		itemRect.pivot = new Vector2(0.5f, 0.5f);

		float xOffset = -350 - (index * 90);
		float yOffset = 230;

		itemRect.anchoredPosition = new Vector2(xOffset, yOffset);
		itemRect.sizeDelta = new Vector2(80, 80);

		// ProductInteractable 복사
		ProductInteractable clonedProduct = itemObj.AddComponent<ProductInteractable>();
		clonedProduct.productData = product.productData;

		BarcodeData originalBarcode = product.GetCurrentBarcode();
		clonedProduct.SetBarcode(new BarcodeData(originalBarcode.barcodeID, originalBarcode.price));

		// 가격 텍스트 추가
		GameObject priceTextObj = new GameObject("PriceText");
		priceTextObj.transform.SetParent(itemObj.transform, false);

		TextMeshProUGUI priceText = priceTextObj.AddComponent<TextMeshProUGUI>();

		GameSetupMaster setupMaster = FindFirstObjectByType<GameSetupMaster>();
		if (setupMaster != null && setupMaster.customFont != null)
		{
			priceText.font = setupMaster.customFont;
		}

		priceText.text = $"{originalBarcode.price}원";
		priceText.fontSize = 14;
		priceText.alignment = TextAlignmentOptions.Center;
		priceText.color = Color.white;
		priceText.fontStyle = FontStyles.Bold;

		RectTransform textRect = priceTextObj.GetComponent<RectTransform>();
		textRect.anchorMin = new Vector2(0, 0);
		textRect.anchorMax = new Vector2(1, 0);
		textRect.pivot = new Vector2(0.5f, 0);
		textRect.anchoredPosition = new Vector2(0, 5);
		textRect.sizeDelta = new Vector2(0, 20);

		// 상품 이름 텍스트 추가
		GameObject nameTextObj = new GameObject("NameText");
		nameTextObj.transform.SetParent(itemObj.transform, false);

		TextMeshProUGUI nameText = nameTextObj.AddComponent<TextMeshProUGUI>();
		if (setupMaster != null && setupMaster.customFont != null)
		{
			nameText.font = setupMaster.customFont;
		}

		nameText.text = product.productData.productName;
		nameText.fontSize = 12;
		nameText.alignment = TextAlignmentOptions.Center;
		nameText.color = new Color(1f, 1f, 0.8f);
		nameText.fontStyle = FontStyles.Normal;

		RectTransform nameTextRect = nameTextObj.GetComponent<RectTransform>();
		nameTextRect.anchorMin = new Vector2(0, 1);
		nameTextRect.anchorMax = new Vector2(1, 1);
		nameTextRect.pivot = new Vector2(0.5f, 1);
		nameTextRect.anchoredPosition = new Vector2(0, -5);
		nameTextRect.sizeDelta = new Vector2(0, 18);

		clonedProduct.priceText = priceText;

		displayedItems.Add(itemObj);

		Debug.Log($"[DisplayManager] 상품 복제 생성: {product.productData.productName}");
	}

	public void ShowPaymentMethod(string method)
	{
		if (paymentMethodText == null)
		{
			GameObject textObj = new GameObject("PaymentMethodText");
			textObj.transform.SetParent(counterItemsCanvas.transform, false);

			paymentMethodText = textObj.AddComponent<TextMeshProUGUI>();

			GameSetupMaster setupMaster = FindFirstObjectByType<GameSetupMaster>();
			if (setupMaster != null && setupMaster.customFont != null)
			{
				paymentMethodText.font = setupMaster.customFont;
			}

			paymentMethodText.fontSize = 32;
			paymentMethodText.alignment = TextAlignmentOptions.Center;
			paymentMethodText.color = new Color(1f, 0.9f, 0.2f);
			paymentMethodText.fontStyle = FontStyles.Bold;

			RectTransform textRect = textObj.GetComponent<RectTransform>();
			textRect.anchorMin = new Vector2(0f, 0f);
			textRect.anchorMax = new Vector2(0f, 0f);
			textRect.pivot = new Vector2(0.5f, 0.5f);
			textRect.anchoredPosition = new Vector2(100, 280);
			textRect.sizeDelta = new Vector2(300, 50);
		}

		paymentMethodText.text = method;
		paymentMethodText.gameObject.SetActive(true);
	}

	public int SpawnCustomerMoney(int totalAmount)
	{
		int[] possibleAmounts = CalculatePossiblePayments(totalAmount);
		int paidAmount = possibleAmounts[Random.Range(0, possibleAmounts.Length)];

		Debug.Log($"[DisplayManager] 손님이 {paidAmount}원을 냅니다. 거스름돈: {paidAmount - totalAmount}원");

		// 손님 돈 컨테이너 생성
		if (customerMoneyContainer == null)
		{
			customerMoneyContainer = new GameObject("CustomerMoneyContainer");
			customerMoneyContainer.transform.SetParent(counterItemsCanvas.transform, false);

			RectTransform containerRect = customerMoneyContainer.AddComponent<RectTransform>();
			containerRect.anchorMin = new Vector2(0f, 0f);
			containerRect.anchorMax = new Vector2(0f, 0f);
			containerRect.pivot = new Vector2(0.5f, 0.5f);
			containerRect.anchoredPosition = new Vector2(200, 150);
			containerRect.sizeDelta = new Vector2(300, 150);
		}

		// 기존 돈 제거
		foreach (Transform child in customerMoneyContainer.transform)
		{
			Destroy(child.gameObject);
		}

		// 돈 배치
		List<int> breakdown = BreakdownMoney(paidAmount);
		float xOffset = 0f;

		foreach (int value in breakdown)
		{
			GameObject moneyObj = new GameObject($"CustomerMoney_{value}");
			moneyObj.transform.SetParent(customerMoneyContainer.transform, false);

			Image moneyImage = moneyObj.AddComponent<Image>();
			moneyImage.sprite = Resources.Load<Sprite>($"Sprites/Money/{value}");
			moneyImage.color = Color.white;

			RectTransform moneyRect = moneyObj.GetComponent<RectTransform>();
			moneyRect.anchorMin = new Vector2(0f, 0.5f);
			moneyRect.anchorMax = new Vector2(0f, 0.5f);
			moneyRect.pivot = new Vector2(0.5f, 0.5f);
			moneyRect.anchoredPosition = new Vector2(xOffset, 0);
			moneyRect.sizeDelta = new Vector2(60, 60);

			xOffset += 70f;
		}

		return paidAmount;
	}

	int[] CalculatePossiblePayments(int amount)
	{
		List<int> possiblePayments = new List<int>();

		if (amount <= 5000)
		{
			possiblePayments.Add(5000);
			possiblePayments.Add(10000);
		}
		else if (amount <= 10000)
		{
			possiblePayments.Add(10000);
			possiblePayments.Add(15000);
			possiblePayments.Add(20000);
		}
		else if (amount <= 20000)
		{
			possiblePayments.Add(20000);
			possiblePayments.Add(30000);
			possiblePayments.Add(50000);
		}
		else if (amount <= 50000)
		{
			possiblePayments.Add(50000);
			possiblePayments.Add(60000);
			possiblePayments.Add(100000);
		}
		else
		{
			possiblePayments.Add(100000);
			possiblePayments.Add(150000);
		}

		return possiblePayments.ToArray();
	}

	List<int> BreakdownMoney(int amount)
	{
		List<int> breakdown = new List<int>();
		int[] denominations = { 50000, 10000, 5000, 1000 };

		foreach (int denom in denominations)
		{
			while (amount >= denom)
			{
				breakdown.Add(denom);
				amount -= denom;
			}
		}

		return breakdown;
	}

	public void ClearAllDisplayedItems()
	{
		foreach (var item in displayedItems)
		{
			if (item != null)
				Destroy(item);
		}

		displayedItems.Clear();
		Debug.Log("[DisplayManager] 모든 표시 상품 제거");
	}

	public void ClearPaymentUI()
	{
		// 손님이 낸 돈 제거
		if (customerMoneyContainer != null)
		{
			foreach (Transform child in customerMoneyContainer.transform)
			{
				Destroy(child.gameObject);
			}
		}

		// 결제 방식 텍스트 숨김
		if (paymentMethodText != null)
		{
			paymentMethodText.gameObject.SetActive(false);
		}
	}
}