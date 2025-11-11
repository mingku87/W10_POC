# UI 자동 생성 가이드 (TextMeshPro 버전)

## 🚀 빠른 사용법 (3단계)

### 1단계: GameObject 생성

Hierarchy에서 우클릭 → Create Empty → 이름: `GameSetup`

### 2단계: 스크립트 추가

`GameSetup` 선택 → Inspector → Add Component → `GameSetupMaster` 검색 → 추가

### 3단계: UI 자동 생성

Inspector에서 `GameSetupMaster` 컴포넌트 우클릭 → **"전체 UI 한번에 자동 생성"** 클릭

**⚠️ 중요:** TextMeshPro를 사용하므로 첫 실행 시 TMP Importer가 나타나면 **"Import TMP Essentials"** 클릭하세요!

## ✅ 생성되는 것들

실행하면 **자동으로** 다음이 생성됩니다:

1. ✅ **Canvas + EventSystem** (없으면 생성)
2. ✅ **CCTV 시스템** (오른쪽 위 빨간불/녹색불)
3. ✅ **바코드 인벤토리** (하단 10개 바코드)
4. ✅ **제품 상세 패널** (중앙 팝업)
5. ✅ **제품 5개** (Snack, Drink, Ramen, Fruit, Bread)
6. ✅ **바코드 프리팹** (Assets/Prefabs/BarcodePrefab.prefab)

## 🎮 바로 플레이 테스트

UI 생성 후:

1. **Play 버튼** (Ctrl+P) 클릭
2. **녹색불일 때** 제품 클릭
3. 중앙 패널에서 **바코드 드래그&드롭**
4. 가격 변경 확인!

## ⚙️ 커스터마이징 (선택사항)

Inspector에서 `GameSetupMaster` 설정을 변경할 수 있습니다:

- **Product Count**: 제품 개수 (기본 5개)
- **Product Spacing**: 제품 간격 (기본 2)
- **Start Position**: 첫 제품 위치 (기본 -4, 0, 0)
- **Custom Font**: TextMeshPro 폰트 에셋 (비워두면 TMP 기본 폰트 사용)

### TextMeshPro 폰트 설정 방법:

1. Window → TextMeshPro → Font Asset Creator 열기
2. Source Font File에 TTF/OTF 폰트 드래그
3. "Generate Font Atlas" 클릭
4. "Save" 클릭하여 TMP 폰트 에셋 저장
5. Inspector의 `GameSetupMaster` → `Custom Font` 필드에 생성된 TMP 폰트 에셋 드래그
6. "전체 UI 한번에 자동 생성" 클릭

설정 변경 후 다시 "전체 UI 한번에 자동 생성" 실행하면 새로운 설정으로 생성됩니다.

## 🔄 다시 생성하기

UI를 지우고 다시 생성하려면:

1. Hierarchy에서 생성된 오브젝트 삭제:
   - `Canvas` (및 자식들)
   - `CCTVManager`
   - `BarcodeInventoryManager`
   - `Product_*` (모든 제품)
2. "전체 UI 한번에 자동 생성" 다시 클릭

## 📝 주의사항

- **Edit 모드**에서 실행 (플레이 모드 아님)
- 이미 Canvas가 있으면 재사용
- Console 창에서 생성 진행 상황 확인
- 에러 발생 시 Console 확인

## 🐛 문제 해결

**"전체 UI 한번에 자동 생성" 메뉴가 안 보여요**

- GameSetupMaster 스크립트가 제대로 컴파일되었는지 확인
- Inspector에서 컴포넌트 헤더를 **우클릭**해야 함

**UI가 생성되지 않아요**

- Console 창 확인 (Window → General → Console)
- 에러 메시지가 있는지 확인

**바코드 프리팹 생성 실패**

- Assets 폴더에 쓰기 권한이 있는지 확인

---

**완료! 이제 플레이 모드로 게임을 테스트해보세요!** 🎮
