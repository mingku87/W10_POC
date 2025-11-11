# 편의점 알바 게임 - CCTV & 드래그&드롭 바코드 교체 시스템

## 🎮 게임 컨셉

편의점 알바생이 되어 CCTV를 피해 물건의 바코드를 조작하여 돈을 빼돌리는 2D 게임

## 📋 현재 구현된 기능

- **CCTV 감시 시스템**: 빨간불(감시 중) / 녹색불(안전) 자동 토글
- **제품 관리**: 5개의 제품, 각각 이름과 가격 표시
- **바코드 인벤토리**: 10개의 재사용 가능한 바코드 (500원~10000원)
- **드래그&드롭 바코드 교체**:
  - CCTV 녹색불일 때만 제품 클릭 가능
  - 화면 중앙에 제품 상세 패널 표시
  - 바코드를 드래그하여 제품에 드롭하면 가격 교체
  - 바코드는 재사용 가능 (무한 사용)

---

## 🔧 Unity 씬 설정 방법 (완전 가이드)

### 1. CCTV UI 설정

1. **Canvas 생성**

   - Hierarchy → 우클릭 → UI → Canvas

2. **CCTV 라이트 Image 생성**

   - Canvas 우클릭 → UI → Image
   - 이름: `CCTVLight`
   - Rect Transform 설정:
     - Anchor: Top-Right
     - Position: X = -50, Y = -50
     - Width: 30, Height: 30

3. **CCTVController 스크립트 붙이기**
   - Hierarchy에서 빈 GameObject 생성: `CCTVManager`
   - `CCTVController.cs` 스크립트를 드래그하여 추가
   - Inspector에서:
     - `Light Image` 필드에 위에서 만든 `CCTVLight` Image를 드래그
     - `Watch Duration`: 3초 (빨간불 지속시간)
     - `Idle Duration`: 5초 (녹색불 지속시간)

---

### 2. 바코드 인벤토리 UI 설정

1. **바코드 컨테이너 패널 생성**

   - Canvas → 우클릭 → UI → Panel
   - 이름: `BarcodeInventoryPanel`
   - Rect Transform:
     - Anchor: Bottom (하단 중앙)
     - Width: 800, Height: 120
     - Position Y: 60

2. **Grid Layout 추가**

   - `BarcodeInventoryPanel` 선택
   - Add Component → Layout → Grid Layout Group
   - 설정:
     - Cell Size: X=70, Y=50
     - Spacing: X=10, Y=10
     - Start Axis: Horizontal
     - Child Alignment: Middle Center

3. **바코드 프리팹 생성**

   - Hierarchy → UI → Image (BarcodeInventoryPanel의 자식으로)
   - 이름: `BarcodePrefab`
   - 크기: 70x50
   - Add Component → `DraggableBarcode.cs`
   - Add Component → Canvas Group (자동 추가됨)
   - 자식으로 Text 추가:
     - 이름: `PriceText`
     - 텍스트: "1000원"
     - Font Size: 18, Alignment: Center
   - `BarcodePrefab`의 `DraggableBarcode` 스크립트에서:
     - `Price Text` 필드에 위의 Text 연결
   - **프리팹으로 저장**: `BarcodePrefab`을 Assets 폴더로 드래그
   - Hierarchy에서 `BarcodePrefab` 삭제

4. **BarcodeInventory 스크립트 설정**
   - Hierarchy에서 빈 GameObject 생성: `BarcodeInventoryManager`
   - `BarcodeInventory.cs` 스크립트 추가
   - Inspector에서:
     - `Barcode Prefab`: 위에서 만든 BarcodePrefab 드래그
     - `Barcode Container`: BarcodeInventoryPanel 드래그

---

### 3. 제품 상세 패널 UI 설정

1. **상세 패널 생성**

   - Canvas → 우클릭 → UI → Panel
   - 이름: `ProductDetailPanel`
   - Rect Transform: 전체 화면 (Anchor: Stretch, Offsets: 0)
   - Color: 반투명 검정 (R:0, G:0, B:0, A:200)

2. **제품 정보 패널 (중앙)**

   - `ProductDetailPanel` → 우클릭 → UI → Panel
   - 이름: `ProductInfoPanel`
   - Rect Transform:
     - Anchor: Center
     - Width: 400, Height: 500
     - Position: X=0, Y=0
   - Color: 흰색

3. **제품 이미지**

   - `ProductInfoPanel` → 우클릭 → UI → Image
   - 이름: `ProductImage`
   - Rect Transform:
     - Anchor: Top Center
     - Width: 300, Height: 300
     - Position: X=0, Y=-160

4. **제품 이름 텍스트**

   - `ProductInfoPanel` → 우클릭 → UI → Text
   - 이름: `ProductNameText`
   - Text: "제품 이름"
   - Font Size: 28
   - Rect Transform:
     - Anchor: Top Center
     - Position: X=0, Y=-30

5. **현재 가격 텍스트**

   - `ProductInfoPanel` → 우클릭 → UI → Text
   - 이름: `CurrentPriceText`
   - Text: "현재 가격: 1000원"
   - Font Size: 22
   - Rect Transform:
     - Anchor: Top Center
     - Position: X=0, Y=-80

6. **바코드 드롭 영역**

   - `ProductInfoPanel` → 우클릭 → UI → Image
   - 이름: `BarcodeDropZone`
   - Rect Transform:
     - Anchor: Bottom Center
     - Width: 300, Height: 100
     - Position: X=0, Y=80
   - Color: 연한 파랑 (R:150, G:200, B:255)
   - Add Component → `BarcodeDropZone.cs`
   - 자식으로 Text 추가:
     - 이름: `DropHintText`
     - Text: "여기에 바코드를 드래그하세요"
     - Alignment: Center

7. **피드백 텍스트**

   - `ProductInfoPanel` → 우클릭 → UI → Text
   - 이름: `FeedbackText`
   - Text: "" (비워둠)
   - Font Size: 20, Color: 초록색
   - Rect Transform:
     - Anchor: Bottom Center
     - Position: X=0, Y=20

8. **닫기 버튼**

   - `ProductInfoPanel` → 우클릭 → UI → Button
   - 이름: `CloseButton`
   - Rect Transform:
     - Anchor: Top Right
     - Width: 80, Height: 40
     - Position: X=-50, Y=-20
   - Button의 Text: "닫기" (X)

9. **ProductDetailPanel 스크립트 설정**
   - `ProductDetailPanel` GameObject 선택
   - Add Component → `ProductDetailPanel.cs`
   - Inspector에서 연결:
     - `Panel Object`: ProductDetailPanel (자기 자신)
     - `Product Image`: ProductImage
     - `Product Name Text`: ProductNameText
     - `Current Price Text`: CurrentPriceText
     - `Close Button`: CloseButton
     - `Drop Zone`: BarcodeDropZone
   - `BarcodeDropZone` 스크립트에서:
     - `Feedback Text`: FeedbackText 연결

---

### 4. 제품(Product) 설정 (5개 생성)

1. **첫 번째 제품 생성**

   - Hierarchy → 2D Object → Sprite
   - 이름: `Product_Snack1`
   - Sprite: Unity 기본 스프라이트 또는 커스텀 이미지
   - Position: X=-4, Y=0

2. **Collider2D 추가**

   - Add Component → Physics 2D → Box Collider 2D
   - Collider 크기를 스프라이트에 맞게 조정

3. **제품 이름/가격 텍스트 추가**

   - `Product_Snack1` → 우클릭 → UI → Canvas
   - Canvas 설정:
     - Render Mode: World Space
     - Width: 200, Height: 100
   - Canvas → 우클릭 → UI → Text
   - 이름: `NameText`
   - Text: "과자"
   - Font Size: 16
   - Position: Y=20

   - Canvas → 우클릭 → UI → Text
   - 이름: `PriceText`
   - Text: "1000원"
   - Font Size: 14
   - Position: Y=0

4. **ProductInteractable 스크립트 추가**

   - `Product_Snack1` 선택
   - Add Component → `ProductInteractable.cs`
   - Inspector에서:
     - `Product Data`:
       - Product Name: "과자"
       - Original Price: 1000
     - `Name Text`: NameText 드래그
     - `Price Text`: PriceText 드래그

5. **4개 제품 더 생성**
   - `Product_Snack1` 선택 후 Ctrl+D로 복제
   - 각각 다른 위치에 배치 (X: -4, -2, 0, 2, 4)
   - Inspector에서 제품 정보 변경:
     - Product_Snack2: "음료", 1500원
     - Product_Snack3: "라면", 800원
     - Product_Snack4: "과일", 2000원
     - Product_Snack5: "빵", 1200원

---

### 5. 카메라 설정

- Main Camera 선택
- Projection: Orthographic
- Size: 5 (화면에 맞게 조정)
- Position: X=0, Y=0, Z=-10

---

## 🎯 테스트 방법

1. **플레이 모드 실행** (단축키: Ctrl+P)

2. **CCTV 라이트 확인**

   - 오른쪽 위에 빨간불/녹색불이 번갈아 켜지는지 확인
   - 녹색불: 안전 (제품 클릭 가능)
   - 빨간불: 위험 (클릭 불가)

3. **바코드 인벤토리 확인**

   - 화면 하단에 10개의 바코드가 표시되는지 확인
   - 각 바코드마다 다른 가격 표시 (500원~10000원)

4. **제품 정보 확인**

   - 5개 제품 각각 아래에 이름과 가격이 표시되는지 확인

5. **드래그&드롭 바코드 교체 테스트**

   - **녹색불일 때**:

     1. 제품 클릭 → 중앙에 제품 상세 패널이 열림
     2. 하단 바코드 중 하나를 마우스로 드래그
     3. 중앙 패널의 파란색 드롭 영역에 드롭
     4. "바코드 교체 완료!" 메시지 확인
     5. 현재 가격이 변경되었는지 확인
     6. ESC 또는 닫기 버튼으로 패널 닫기

   - **빨간불일 때**:
     - 제품 클릭해도 패널이 열리지 않음
     - Console에 "위험해! CCTV가 감시하고 있어요" 메시지

6. **바코드 재사용 확인**

   - 같은 바코드를 여러 제품에 반복 사용 가능
   - 바코드가 사라지지 않고 계속 인벤토리에 남아있음

7. **Console 확인** (Window → General → Console)
   - 모든 행동에 대한 로그 확인

---

## 📝 다음 단계 구현 예정

- [x] 드래그&드롭 바코드 교체 시스템
- [x] 제품 이름/가격 표시
- [x] 재사용 가능한 바코드 인벤토리
- [ ] 계산대 시스템 (손님, 바코드 스캔)
- [ ] 중복 결제 기능
- [ ] 환불 시스템
- [ ] 거스름돈 사기 시스템
- [ ] 돈 누적 UI
- [ ] 게임오버 조건 (들키면 해고)
- [ ] 시각적 피드백 개선 (애니메이션, 파티클)

---

## 🐛 문제 해결

**제품을 클릭해도 패널이 안 열려요**

- Collider2D가 제품에 추가되어 있는지 확인
- ProductDetailPanel.Instance가 null이 아닌지 확인 (패널에 스크립트 붙었는지)
- CCTV가 녹색불인지 확인 (빨간불일 때는 안 열림)
- Game View에서 클릭하고 있는지 확인 (Scene View에서는 안됨)

**바코드를 드래그할 수 없어요**

- BarcodePrefab에 Canvas Group이 추가되어 있는지 확인
- DraggableBarcode 스크립트가 제대로 붙어있는지 확인
- EventSystem이 Scene에 있는지 확인 (Canvas 생성 시 자동 생성)

**바코드를 드롭해도 가격이 안 바뀌어요**

- BarcodeDropZone 스크립트가 드롭 영역에 붙어있는지 확인
- ProductDetailPanel의 모든 필드가 제대로 연결되어 있는지 확인
- Console에서 에러 메시지 확인

**바코드 인벤토리에 바코드가 안 보여요**

- BarcodeInventory 스크립트의 필드가 모두 연결되어 있는지 확인
- BarcodePrefab이 프리팹으로 제대로 저장되었는지 확인
- Play 모드에서 Console에 에러가 없는지 확인

**텍스트가 제품 아래에 안 보여요**

- World Space Canvas가 제품의 자식으로 있는지 확인
- Canvas의 Scale이 너무 작지 않은지 확인 (예: 0.01로 설정)
- ProductInteractable의 Text 필드가 연결되어 있는지 확인

**스크립트 에러가 나요**

- Unity에서 자동 컴파일이 끝날 때까지 기다리세요
- Console 창에서 에러 메시지 확인
- 모든 스크립트 파일이 Assets/Scripts 폴더에 있는지 확인
