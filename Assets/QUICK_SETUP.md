# 빠른 설정 가이드 (Quick Setup)

이 문서는 Unity 씬 설정을 빠르게 하기 위한 요약본입니다.
자세한 설명은 `README_CCTV_SETUP.md`를 참고하세요.

## 📦 생성된 스크립트 파일들

1. `CCTVController.cs` - CCTV 감시 시스템
2. `ProductInteractable.cs` - 제품 클릭 및 바코드 관리
3. `ProductData.cs` - 제품 정보 데이터 클래스
4. `BarcodeData.cs` - 바코드 정보 데이터 클래스
5. `BarcodeInventory.cs` - 10개 바코드 인벤토리 관리
6. `DraggableBarcode.cs` - 드래그 가능한 바코드 UI
7. `BarcodeDropZone.cs` - 바코드 드롭 영역
8. `ProductDetailPanel.cs` - 제품 상세 패널 UI

## ⚡ 빠른 설정 체크리스트

### Phase 1: CCTV 설정

- [ ] Canvas 생성
- [ ] Canvas에 Image 추가 → 이름: CCTVLight (오른쪽 위 배치)
- [ ] 빈 GameObject 생성 → CCTVController 스크립트 추가
- [ ] CCTVController에 CCTVLight 연결

### Phase 2: 바코드 인벤토리 설정

- [ ] Canvas에 Panel 추가 → 이름: BarcodeInventoryPanel (하단 배치)
- [ ] BarcodeInventoryPanel에 Grid Layout Group 추가
- [ ] UI Image 생성 → 이름: BarcodePrefab (크기: 70x50)
  - [ ] DraggableBarcode 스크립트 추가
  - [ ] 자식 Text 추가 (PriceText)
  - [ ] Assets 폴더에 프리팹으로 저장
- [ ] 빈 GameObject 생성 → BarcodeInventory 스크립트 추가
- [ ] BarcodeInventory에 BarcodePrefab과 BarcodeInventoryPanel 연결

### Phase 3: 제품 상세 패널 설정

- [ ] Canvas에 Panel 추가 → 이름: ProductDetailPanel (전체 화면, 반투명)
  - [ ] 자식 Panel 추가 → ProductInfoPanel (중앙, 400x500)
    - [ ] Image: ProductImage (300x300)
    - [ ] Text: ProductNameText
    - [ ] Text: CurrentPriceText
    - [ ] Image: BarcodeDropZone (하단, 300x100, 파란색)
      - [ ] BarcodeDropZone 스크립트 추가
      - [ ] 자식 Text: DropHintText
    - [ ] Text: FeedbackText (하단)
    - [ ] Button: CloseButton (우상단)
- [ ] ProductDetailPanel에 ProductDetailPanel 스크립트 추가
- [ ] 모든 UI 요소 연결

### Phase 4: 제품 5개 생성

각 제품마다:

- [ ] 2D Sprite 생성
- [ ] Box Collider 2D 추가
- [ ] World Space Canvas 자식으로 추가
  - [ ] Text: NameText (제품 이름)
  - [ ] Text: PriceText (가격)
- [ ] ProductInteractable 스크립트 추가
- [ ] ProductData 설정 (이름, 원래 가격)
- [ ] Text 필드 연결

**5개 제품 예시:**

1. 과자 - 1000원
2. 음료 - 1500원
3. 라면 - 800원
4. 과일 - 2000원
5. 빵 - 1200원

## 🎮 테스트 실행

1. Play 모드 실행 (Ctrl+P)
2. CCTV 녹색불일 때 제품 클릭
3. 중앙 패널에서 바코드 드래그&드롭
4. 가격 변경 확인
5. ESC로 패널 닫기

## 🎨 UI 계층 구조 요약

```
Canvas
├── CCTVLight (Image)
├── BarcodeInventoryPanel (Panel)
│   └── [BarcodePrefab x 10] (런타임 생성)
└── ProductDetailPanel (Panel)
    └── ProductInfoPanel (Panel)
        ├── ProductImage (Image)
        ├── ProductNameText (Text)
        ├── CurrentPriceText (Text)
        ├── BarcodeDropZone (Image + Script)
        │   └── DropHintText (Text)
        ├── FeedbackText (Text)
        └── CloseButton (Button)

CCTVManager (GameObject)
└── CCTVController.cs

BarcodeInventoryManager (GameObject)
└── BarcodeInventory.cs

Product_Snack1~5 (Sprite)
├── Box Collider 2D
├── ProductInteractable.cs
└── Canvas (World Space)
    ├── NameText (Text)
    └── PriceText (Text)
```

## 🔧 자주 하는 실수

1. **EventSystem 없음** → Canvas 생성 시 자동 생성되지만, 없으면 수동 추가
2. **프리팹 저장 안 함** → BarcodePrefab을 반드시 Assets 폴더로 드래그하여 프리팹 저장
3. **Collider 크기** → 제품 스프라이트 크기에 맞게 Box Collider 2D 조정
4. **World Space Canvas Scale** → 너무 크면 안 보이므로 Scale 조정 (예: 0.01)
5. **UI 연결 누락** → Inspector에서 모든 필드가 연결되었는지 확인

## 💡 팁

- **빠른 복제**: 첫 제품 완벽하게 만든 후 Ctrl+D로 복제하여 나머지 4개 생성
- **프리팹 활용**: 제품을 프리팹으로 만들면 나중에 수정이 쉬움
- **Console 확인**: 항상 Console 창을 열어두고 로그 확인
- **씬 저장**: 작업 중간중간 Ctrl+S로 씬 저장

---

자세한 설명과 문제 해결은 `README_CCTV_SETUP.md` 참고!
