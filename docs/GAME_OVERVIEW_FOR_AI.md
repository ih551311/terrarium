# 유리병 정원 (임시 제목) - 프로젝트 전체 정보

> 다른 AI(예: Gemini)에게 넘기려고 만든 문서. 이 프로젝트의 **현재 상태(구현된 것)**, 구조, 규칙, 앞으로 할 일, 작업 방법을 모두 적었다.
> 기준 시점: 2026-11-14, 마지막 푸시 커밋 `140734f` (그 뒤 코드 변경 없음).

---

## 1. 한 줄 요약

섬에서 재료(꽃, 이끼, 솔방울 등)를 채집하고, 집 작업대에서 **유리병 속에 재료를 자유롭게 놓아 테라리움을 꾸미는** 힐링 1인칭 3D 게임. 완성한 병은 집 선반에 전시한다. 전투, 시간 압박, 실패 없음. 재료를 우체통에 팔아 코인을 얻고, 코인으로 도구 강화나 장식 재료를 산다.

- 사용자는 **Unity 초보**이고 한국어를 쓴다. 답변은 짧고 쉽게.
- 엔진: **Unity 6000.3.23f1, URP**, 새 Input System, uGUI + TextMeshPro.
- 저장소: https://github.com/ih551311/terrarium (main 브랜치). 프로젝트 폴더 `C:\Users\ih551\terrariummake`.
- 기획서 원본: `GAME_DESIGN.md` (사용자 다운로드 폴더). 이 문서는 기획서와 **달라진 점**도 적었다(10장).

---

## 2. 현재 구현된 기능 (플레이 흐름)

1. **섬 걷기**: WASD 걷기(3.5m/s), 왼쪽 Shift 달리기(7m/s), 스페이스 점프(약 1.1m), 마우스 둘러보기. 섬 크기 약 300m. 지도 M(큰 지도, 클릭하면 그 자리로 순간이동, "집으로 돌아가기" 버튼), N(오른쪽 위 작은 지도 켜고 끄기).
2. **채집**
   - 식물, 이끼, 조개, 모래: 가까이 보고 **E 키**로 "채취/줍기". 얻으면 "고사리 +1" 알림. 가방(32칸, 칸당 최대 5개)에 들어감. 가방이 가득 차면 "가방이 가득 찼어요".
   - 나무: 보고 **좌클릭** → 도끼(자동 선택), 돌: 곡괭이. 기본 4번 치면 재료 1개. 캔 나무는 잎이 사라지고 줄기만 남음(돌은 색만 옅게), 다음 날 돌아옴. 희귀 재료(호박 조각, 수정 조각)가 가끔 나옴.
   - 달맞이꽃은 **밤에만** 나타나고 아침에 사라짐.
   - 채취한 자리는 오늘 비어 있고 **자고 일어나면** 다시 남. 집에 들어갔다 나와도 비어 있는 채로 유지(GameState가 기억).
3. **집**: 섬의 중세풍 집(Medieval Town Exteriors 팩의 Building_b)에 E로 들어감(씬 전환). 집 안에 작업대, 침대, 선반, 상자, 통, 책상 등.
4. **병 꾸미기(작업대 E)**
   - 처음엔 **"병을 만들어요" 화면**: 병 종류 4개(둥근, 긴, 넓은, 누운) 카드 + 흙의 양(적게/보통/많이). 카드를 누르면 병이 만들어지고, **병 종류와 흙의 양은 그 뒤로 바꿀 수 없음**. **병 가격**: 둥근 6, 긴 6, 넓은 10, 누운 14코인. 시작할 때 **공짜 병 1개**(GameState.FreeBottles, 저장됨)를 받고 먼저 쓰며, 다 쓰면 코인을 내고 만든다. 코인이 모자라면 병 만들기 화면에 안내가 뜸.
   - 꾸미는 화면: 병 그림(카메라가 정면 고정), 마우스로 끌면 **병 방향이 바뀜**(좌우=돌기, 위아래=앞뒤 기울기), T키로 원위치. 휠로 확대/축소.
   - 아래 재료 칸에서 재료를 고르고 병 안 흙 위를 **클릭하면 놓임**(놓을 자리 미리보기 반투명). 재료 위 휠: 돌리기, Q/R: 크기, 우클릭: 빼기. 병 하나에 최대 12개. 병 밖으로 못 나가게 안쪽 반지름 표로 검사. 바닥재는 흙 또는 모래(가방의 모래 1개 소모).
   - **완성** 버튼(두 번 눌러야 확정): 뚜껑이 닫히고 더 못 고침. 완성하면 병이 자동으로 **병 인벤토리**("내 병")에 들어가고 병 만들기 화면으로 돌아감. 같은 종류 병도 개수 제한 없이 또 만들 수 있음(비우기 없음). 완성할 때 **이름 짓기 상자**(BottleNameDialog, 최대 12자)가 먼저 뜨고, 비워 두면 "종류 #번호"가 이름이 됨. 이름은 `BottleRecord.name`에 저장되어 내 병·Tab 병 탭·선반에 표시됨.
   - 식물도 **자라지 않음**(성장 시스템은 사용자 결정으로 뺌). 놓은 즉시 다 자란 크기.
5. **선반 전시**: 사용자가 **직접 골라서** 전시(자동 아님). 병 만들기 화면의 "내 병"에서 병 인벤토리의 병을 눌러 전시/내리기(선반 24칸, 완성한 병 6개마다 한 단=6칸씩 열림). 선반의 병을 보고 E → **자세히 보기**(BottleDetailPanel: 병을 크게 보여 주고 끌어서 돌려 볼 수 있음, T 처음 방향, 안에서 선반에 올리기/내리기). Tab 가방의 병 탭에서 병 칸을 눌러도 열림(집 안에서만, 병 프리팹을 작업대에서 찾음). 선반 병은 병 프리팹을 0.55배로 복사해 올리고 병 속이 그대로 보임.
6. **우체통(문 옆, E)**: 재료 보내기 → 다음 날 코인. 장식 16종 주문(코인) → 다음 날 도착. 도구 강화(도끼/곡괭이 비용 30/80/150, 괭이 20/60/120) 주문 → 다음 날 도착. 도착한 것은 우체통을 열어 "받기"로 가방/지갑에 들어옴.
7. **하루와 잠**: 하루 약 10분(GameState.DayLengthSeconds=600). 아침→낮→노을→밤. 밤 끝에서 시간이 멈춤. 집 안 침대 E로 자면 다음 날(시간 0, 채취 자리 복구, 우체통 물건 도착) + **자동 저장**.
8. **하늘**: AllSky 팩의 Cartoon Base BlueSky(낮)와 NightSky(밤) 큐브맵을 **직접 만든 셰이더 `Sky Blend`**로 섞음. SkyController가 시간에 맞춰 하늘, 해/달빛 방향과 색, 환경광, 안개를 바꿈.
9. **UI**: 가방(Tab), 오른쪽 위 코인 표시(항상, 작은 지도 왼쪽), 화면 아래 안내/알림 막대(어두운 반투명 바탕), 재료 **아이콘 32종**(3D 모델을 찍어 윤곽선 두른 로우폴리 그림)이 가방/재료 칸/우체통에 표시.
10. **저장/불러오기**: save.json(JSON). 게임을 켜면 자동으로 불러옴. 메뉴 "저장 지우기"로 새로 시작.

### 아직 없는 것
- 병 구매(가격 스위치는 있고 꺼져 있음)
- (완료) 선반 24칸 확장: 서쪽 벽의 긴 선반 4단 x 6칸. 완성한 병 6/12/18개마다 한 단씩 열림(BottleStore.ShelfCapacity)
- 도감, 시작 편지, 메뉴, 보관 상자(작업대 옆 상자), 효과음/음악(도구 치는 소리만 코드로 만든 임시음)
- 날씨(비), 비 온 다음 날 숲 버섯(버섯은 지금 항상 나옴), 바닷가에 떠밀려 오는 빈 병
- 구역 열기(바닷가, 꽃 언덕을 병 완성 개수로 여는 기획은 아직 안 함. 지금은 섬 전체가 열려 있음)
- 물주기/성장(뺐음), 물뿌리개(뺐음)
- 바다: 해안선 둘레(SeaCoast, Bitgem 물 셰이더)와 먼 바다(SeaFar, 단색 Lit, 40m 격자)로 나눔. 안개/하늘 지평선 색을 맞춰 낮·노을·밤·안개에도 이음새가 안 보임(SkyController + SkyBlend _Horizon*). 메뉴 `유리병 정원/바다 다시 만들기`.

---

## 3. 핵심 규칙과 숫자

| 항목 | 값 |
| --- | --- |
| 가방 | 32칸(8열 x 4행, 칸은 정사각형), 한 칸에 같은 재료 최대 5개 |
| 병 하나의 재료 | 최대 12개 |
| 병 크기 | 원본 모델 x2배(BottleScale=2) |
| 도구 강화 | 도끼, 곡괭이, 괭이 각각 단계 0~3. 처음 장비는 여러 번 쳐야 함. 타격 수 도끼/곡괭이 8/6/4/3, 괭이 6/4/3/2. 얻는 양 1/1/2/3, 희귀 확률 3/6/10/15%, 비용 도끼/곡괭이 -/30/80/150, 괭이 -/20/60/120 (ToolSettings 에셋, 메뉴 도구 수치 기본값으로). 도끼는 호박 조각, 곡괭이는 수정 조각, 괭이는 네잎클로버(풀)와 해바라기(꽃)가 희귀 재료 |
| 하루 길이 | 600초 |
| 밤 시작(달맞이꽃 등장) | 시간 0.78 이후 (하늘이 밤으로 섞이는 한가운데) |
| 재료 판매가 | 기본 재료 1~3코인, 희귀(호박, 수정) 20코인, 장식 구매가 6~40코인 (MaterialData의 sellPrice/buyPrice) |
| 선반 | 24칸(4단 x 6, 서쪽 벽을 따라 길이 3.1m), 병을 0.55배로 전시 |

---

## 4. 재료 32종 (MaterialData, `Assets/Data/Materials/*.asset`)

종류: Plant(식물), Decoration(장식), Ground(바닥재), Cover(바닥 덮기). 구역: Forest(작은 숲, 섬 북쪽), Beach(바닷가), FlowerHill(꽃 언덕), Anywhere, Shop(우체통 상점에서만).

- **숲**: 이끼(moss, Cover, 줍기), 고사리(fern, Plant, 줍기), 버섯(mushroom, 줍기), 나뭇가지(twig, 도끼), 솔방울(pinecone, 도끼)
- **섬의 풀과 꽃(괭이, Harvestable tool=Hoe)**: 섬의 풀 약 515개와 꽃 약 270개(꽃 언덕에 꽃 136송이를 더 심음)를 나무와 돌처럼 **괭이로 좌클릭**(2번 치기, 강화 없음)해서 캔다. 풀처럼 생긴 것(Plant01~03, 09, 12~16)에서는 **풀 재료**(`PlantGroup.Grass`: 이끼, 고사리, 클로버, 네잎클로버, 풀 다발, 강아지풀, 갈대, 밀 이삭, 나뭇잎, 단풍잎, 짙은 이끼, 도토리, 버섯), 꽃처럼 생긴 것(Plant04A, 05A/B, 06B, 07B)에서는 **꽃 재료**(`PlantGroup.Flower`: 흰·파란·분홍·보라 들꽃, 민들레, 튤립, 해바라기, 라벤더, 코스모스)가 구역에 맞게 무작위로 나옴(ToolSettings.RollHoe). 캔 풀은 바래 보이다가 다음 날 아침에 돌아옴. `AcquireMethod.Hoe` 재료는 섬 바닥에 따로 놓이지 않음. 모델은 코드로 만든 팔레트 로우폴리(`PlantModelMaker`, `ShellModelMaker`)이고 아이콘은 IconBaker가 모델을 찍어 만듦.
- **바닷가**: 조개껍데기(seashell, 줍기), 가리비·대합·고둥·모래 달러·뿔고둥(줍기), 모래(sand, Ground, 줍기), 조약돌(pebble, 곡괭이), 유목(driftwood, 도끼)
- **꽃 언덕**: 흰 들꽃(white_flower), 파란 들꽃(blue_flower), 클로버(clover), 달맞이꽃(evening_primrose, 밤만) — 모두 Plant, 줍기. 작은 바위(small_rock, 곡괭이)
- **어디서나(희귀)**: 호박 조각(amber, 도끼 희귀), 수정 조각(crystal, 곡괭이 희귀)
- **상점(우체통 주문) 16종**: 책, 책 더미, 우산, 캠프파이어, 텐트, 이정표, 종이배, 초, 찻잔, 바람종, 별 장식, 초승달, 돌탑, 사다리, 요정 문, 열기구

MaterialData 필드: id, displayName, kind, icon(스프라이트), zone, acquire(Hand/Axe/Pickaxe/Shop), condition(Always/NightOnly/AfterRain), isRare, sellPrice, buyPrice, placedPrefab(병 안 3D 모델), placeholderShape/Color/Size, pickupScale. 재료 추가는 `MaterialSetup.cs`의 표에 한 줄 + 메뉴 실행.

---

## 5. 폴더와 주요 스크립트

```
Assets/
  Scenes/            Island.unity(섬), HutInterior.unity(집 안)  — 둘 다 Build Settings에 있음
  Prefabs/           GameRoot.prefab(플레이어+UI+EventSystem, 모든 씬에 한 개씩), Bottles/Bottle_{round,tall,flask,side}.prefab
  Scripts/Core       GameRoot, GameState, SaveSystem, SaveData, SceneDoor, SpawnPoint, Bed, Mailbox, SkyController
  Scripts/Player     FirstPersonController, Interactor, ToolView
  Scripts/Items      Bag, MaterialData, MaterialDatabase, Pickup, Harvestable, ToolSettings, ToolSound
  Scripts/Terrarium  BottleContents, BottleStore, BottleStation, BottleViewer, PlacedMaterial, BottleShelf, ShelfBottle, BottleData
  Scripts/UI         BottlePanel, BottleChooser, BottleDecorator, MaterialStrip, GroundSelector, CompleteBottleButton, DragRotate,
                     InventoryPanel, MailboxPanel, MapPanel, MapClick, MiniMap, CoinHud, Hud, UIFocus
  Editor/            IslandSetup, InteriorSetup, UISetup, JarSetup, MaterialSetup, MapBake, IconBaker, BottleIconMaker, SkySetup,
                     ToolSetup, SaveMenu, CabinBuilder(미사용 잔재), BottleSetup, DecoImportSetup, PackMaterialSetup
  Shaders/           SkyBlend.shader (낮/밤 큐브맵 섞기)
  Resources/         ToolSettings.asset, MaterialDatabase.asset, HiddenLeaf.mat(캔 나무 잎 숨김용 투명 재질), Fonts/Cafe24DongdongLight SDF
  Data/              Materials/, Bottles/(병 데이터+흙 모양 메시), Icons/(재료 아이콘 PNG), BottleIcons/, IslandMap.png, Placeholder/
  Models/Deco/       jar_*.obj(유리병 모델, 사용자 제공 jars_no_soil), deco_*.obj(재료 3D 모델, 사용자 제공)
  ThirdParty/        LowPolyNaturePack(바위/풀/버섯), MedievalTownExteriors(집/소품), Donthrukat
  Low Poly Tree Mega Pack by MysticForge/  나무(소나무, 활엽수, 야자수)
  AllSkyFree/        하늘 큐브맵 2종만 남김
  Bitgem/            물 셰이더 팩(미사용, 미커밋)
docs/                ITEM_SOURCES.md(재료 얻는 곳 표), GAME_OVERVIEW_FOR_AI.md(이 문서)
```

### 구조의 핵심 개념
- **GameRoot**: 플레이어, UI Canvas, EventSystem을 담은 프리팹. `DontDestroyOnLoad`. 어느 씬에서 Play를 눌러도 시작되도록 모든 씬에 하나씩 두고, 중복은 스스로 사라짐. 씬 이동은 `GoTo(씬, 도착지점id)`.
- **GameState (정적)**: 날짜, 코인, 도구 단계, 우체통 상태(보낸 재료/주문/도착), 오늘 가져간 자리(taken), 하루 시간(TimeOfDay). `Sleep()`이 다음 날 처리(우체통 도착, 시간 0, 채취 자리 복구, `DayStarted` 이벤트).
- **BottleStore (정적)**: 병별 기록(작업대 병 bottle0~3 + 병 인벤토리의 완성한 병 `bottle0#3` 같은 고유 번호. BottleRecord: 종류(typeId), 만들어졌나, 흙의 양, 바닥재, 놓은 재료 목록, 완성 여부, 선반 칸). 씬을 옮겨도 병 속이 유지됨. `BottleContents.Awake`가 기록에서 복원.
- **BottleContents**: 병 하나의 속(흙, 재료, 놓을 자리 검사, 완성). 병마다 `bottleId`(bottle0~3)로 구분. 안쪽 반지름 표(profileRadii)로 "병 밖으로 안 나가게" 판정.
- **UIFocus**: 패널이 하나라도 열리면 커서를 풀고 걷기/상호작용을 멈추는 공통 규칙. 새 패널은 Open/Close에서 `UIFocus.Open/Close()`를 부를 것.
- **IInteractable**: `Prompt` 문자열 + `Interact(Interactor)`. Interactor가 카메라 앞 2.5m 광선(트리거 포함)으로 대상을 찾아 안내 글자를 보여 주고 E를 처리. 좌클릭은 `Harvestable`이면 도구 휘두르기.
- **편집기 설정 스크립트**: 씬/프리팹/UI를 **코드로 만들고 다시 만들 수 있음**(여러 번 눌러도 같은 결과). 메뉴 `유리병 정원 > ...`: 섬 만들기, 집 안 꾸미기, UI 만들기, 병 만들기, 섬 지도 만들기, 재료 아이콘 만들기, 병 그림 만들기, 하늘 만들기, 도구 설정 만들기, 재료 목록 만들기, 저장 지우기, 저장 폴더 열기. **UI를 바꾸려면 UISetup.cs를 고치고 "UI 만들기"를 실행**(GameRoot 프리팹의 UI를 지우고 다시 만듦). 씬 오브젝트는 해당 *Setup.cs를 고치고 메뉴 실행.

---

## 6. 섬과 집

- **섬(IslandSetup)**: 지형(Terrain, 절차적 높이/칠), 바다(해안 물 SeaCoast + 먼 바다 SeaFar), 중세 집 + 마당(우체통, 통, 상자, 우물, 수레, 가로등), 나무 348그루(숲 덩어리 노이즈로 소나무 숲/활엽수 숲/빈터, 겹침 방지, 야자수는 해변에 군집), 바위, 풀/버섯, 채취 자리(Pickup 81개), 해/하늘(SkyController). **길은 없음**(사용자 요청). 지형은 오두막 언덕(0,10)이 평평, 꽃 언덕(90,85) 반지름 45가 가장 높음, 북쪽(z>28)이 숲.
- **집 안(InteriorSetup)**: 6m x 6m 방, 회벽+돌 밑단+목조 뼈대 느낌, 창 3개, 침대(Bed), 작업대(Workbench+BottleStation), 선반(BottleShelf, 칸 6개), 책상, 촛불, 상자/통, 랜턴. 문은 남쪽(SceneDoor "밖으로 나가기").
- **지도(MapBake)**: 지형 계산으로 색을 칠하고(바다/모래/풀밭/숲/꽃 언덕, 구불구불한 경계, 음영), 실제 나무/바위 위치를 하나씩 그려 넣음. 섬을 다시 만들면 지도도 다시 만들 것.

---

## 7. 병(유리병) 상세

- 병 4종: 둥근(bottle0), 긴(bottle1), 넓은(bottle2), 누운(bottle3). 모델은 `Models/Deco/jar_*.obj`(속이 빈 유리, 코르크 `_cork.obj` 별도). 유리 재질 `JarGlass.mat`은 **Unlit 투명, 알파 0.2**(조명 반사로 하얗게 번쩍여서 속이 안 보이던 문제를 이렇게 해결. 병 속이 잘 보이게 0.3에서 낮춤. 더 낮추면 병 윤곽이 안 보임. 병 꾸미는 화면 배경은 짙은 청회색(JarSetup.BottleViewBackground), 카메라 시야각 50).
- 흙: 단계 3개(적게/보통/많이)의 메시를 JarSetup이 병마다 만들어 둠(`Data/Bottles/*_soil{0,1,2}.asset`). 서 있는 병은 안쪽 단면을 따라간 회전체(바닥 모서리 둥글게, 윗면 굴곡), 누운 병은 둥근 안쪽을 따라 차오르는 모양. **삼각형 앞면 방향이 중요**: 윗면은 +y, 바닥면은 -y를 보게 감아야 함(예전에 거꾸로여서 위에서 보면 흙 윗면이 안 보였음).
- 보는 법: 카메라는 병 옆 정면 고정(RenderTexture → RawImage). **BottleViewer**가 병만 돌림(병 가운데 기준, 카메라는 매번 원위치). 서 있는 병은 기본적으로 카메라 쪽으로 28도 기울여 흙 윗면이 보이게 함.
- 병이 커서 선반에는 0.55배, 누운 병은 90도 돌려 선반 안쪽을 향하게 둠.

---

## 8. 저장 파일

- 위치: Unity `Application.persistentDataPath` (Windows: `C:/Users/<이름>/AppData/LocalLow/DefaultCompany/terrarium11/save.json`).
- 내용(SaveData, 버전 2): 날짜, 코인, 도착 코인, 도구 단계, 우체통(보낸/주문/도착 재료 id, 주문/도착 도구 강화), 가방(id와 개수), 병들(bottleId, 만들었나, 완성, 흙의 양, 바닥재 id, 선반 칸, 놓은 재료 id/위치/각도/크기).
- 재료는 **id 문자열**로 저장하고 `MaterialDatabase` 에셋(Resources)으로 찾음. 재료를 새로 만들면 메뉴 "재료 목록 만들기"를 다시 눌러야 저장/불러오기가 인식함.
- 버전 1 저장은 선반 칸을 무시(모든 병을 내린 상태로 열림).
- 저장 시점은 **침대에서 잘 때만**. 불러오기는 게임 시작 시 자동(`RuntimeInitializeOnLoadMethod`).

---

## 9. 외부 에셋과 라이선스 메모

사용자가 에셋 스토어 등에서 가져와 넣은 것(저장소에도 커밋됨. 공개 저장소라 라이선스 재배포 이슈 가능, 사용자가 알고 올리기로 함):
- Low Poly Nature Pack, Medieval Town Exteriors(Lylek Games, URP), Low Poly Tree Mega Pack(MysticForge), AllSky Free(rpgwhitelock, 사용하는 2종만 남김), Donthrukat 팩, Cafe24 Dongdong Light 폰트, 재료 모델 `deco_*`와 병 모델 `jar_*`(사용자가 외부에서 생성해 제공한 zip).
- Bitgem URP Stylized Water Shader(Proto Series)는 `Assets/Bitgem/`에 있지만 **미사용, 미커밋**.
- 재료 아이콘과 병 그림은 3D 모델을 유니티에서 찍어 만든 **자체 생성물**.

---

## 10. 기획서(GAME_DESIGN.md)와 달라진 점 (기획서를 고쳐야 함)

기획서는 사용자가 직접 갱신한다(AI가 임의로 고치지 말 것). 달라진 점:
1. 달리기(Shift)와 점프(Space) 추가, 지도(M)와 작은 지도(N), 지도 클릭 순간이동 추가.
2. 병 꾸미기는 **패널(팝업)** 에서 함. 병은 4종(누운 병 추가), 병을 만들 때 종류와 흙의 양을 정하고 바꿀 수 없음. 병은 나중에 구매 예정.
3. 병은 마우스로 **자유 방향**으로 돌림(좌우+위아래), 시점 고정.
4. **식물 성장/물주기/물뿌리개 삭제**. 식물은 놓은 그대로.
5. 선반에는 **사용자가 골라 전시**.
6. 코인은 **화면에 항상 표시**(기획서는 우체통을 열 때만).
7. 재료 32종(장식 16종은 우체통 구매), 재료마다 얻는 방법(줍기/도끼/곡괭이/상점)과 구역.
8. 섬 크기 약 300m, 길 없음, 씬 분리(Island/HutInterior).
9. 하늘: 아침/낮/노을/밤 4단계를 큐브맵 2장 섞기로 구현.
10. 달맞이꽃은 밤(시간 0.78 이후)에만 줍기 가능.
11. 캔 나무는 "색이 옅어짐"이 아니라 **잎이 사라지고 줄기만 남음**.
12. 하늘/지형/에셋은 임시이고, 기획서의 "구역 열기, 도감, 시작 편지, 날씨"는 미구현.

---

## 11. 앞으로 할 일 (우선순위 제안)

1. 병 구매(가격 켜기). (병 인벤토리 구조는 완료)
2. 구역 열기(바닷가/꽃 언덕을 완성한 병 개수로 열기, 첫 구역만 열고 시작) + 바닷가 빈 병.
3. 도감(재료 모으기 기록), 시작 편지, 메뉴(저장/종료).
4. 비 오는 날과 비 온 다음 날 버섯, 날씨.
5. 보관 상자(작업대 옆).
6. 효과음/음악, 도구 모델, 알림 연출.
7. 바다: 완료(해안 물 셰이더 + 먼 바다 단색, 지평선 이음새 처리).
8. 콘솔 오류 하나: `BatchDrawCommand ... invalid Batch` (원인 미확인, 게임 동작엔 영향 없어 보임). URP 설정 `PC_RPAsset`의 GPU Resident Drawer가 켜진 것과 관련 의심.

---

## 12. 작업 방법과 주의사항 (다음 AI가 꼭 지킬 것)

**사용자 규칙**
- 답변은 **한국어, 짧고 쉽게**. 확인하지 못한 것은 "확인하지 못했다"고 말한다.
- **커밋 메시지에 `Co-Authored-By`/"Generated with Claude Code"를 넣지 않는다.** 단계별로 커밋하고, 사용자가 **"알아서 계속 푸시해"라고 했으므로 커밋 후 `git push origin main`까지** 한다.
- git은 PATH에 없다. Fork 프로그램의 git: `C:\Users\ih551\AppData\Local\Fork\gitInstance\2.50.1\cmd\git.exe`. PowerShell에서 한글 커밋 메시지는 따옴표 문제가 생기니 **메시지를 UTF-8 파일로 쓰고 `git commit -F`**.
- 사용자가 에디터에서 **직접 Play 중일 수 있다**. Play를 멋대로 끄거나 시험 때문에 저장 파일을 지우지 말 것(저장 파일 `save.json`은 사용자의 진행). 자기 시험용으로 만든 저장만 지운다.
- Unity가 자동으로 다시 저장하는 파일(폰트 SDF, SampleSceneProfile, SeaWater 등)은 변경이 생길 수 있음. 쓰는 파일이면 커밋한다.

**기술 주의**
- **PowerShell 5.1에서 한글 파일을 `Get-Content -Raw`로 읽고 쓰면 글자가 깨진다.** 한글이 든 파일 수정은 편집 도구(Edit/Write)나 `-Encoding UTF8` 명시, 또는 .NET `ReadAllText/WriteAllText(UTF8)`를 쓸 것. (한 번 5개 파일이 깨져 git으로 복구한 적 있음)
- Unity 조작은 **Unity CLI**로 한다: `unity command script eval --file x.cs`(C#을 에디터에서 실행), `unity recompile`, `unity status --until-ready`, `unity command editor.playmode start/stop`, `unity command capture game_view --output-path x.png`, `unity command console get --level error`. 키보드/마우스 입력은 에디터에서 안정적으로 안 들어가므로(키 시뮬레이션이 안 먹음) **코드로 직접 호출해서 검증**하고, 마우스로 직접 해 보진 못했다고 사용자에게 알린다.
- 셰이더가 처음 컴파일되는 동안 화면이 비거나 하늘색 덩어리로 나올 수 있다. 몇 초 기다렸다 다시 캡처.
- 편집 모드에서 `Destroy` 대신 `DestroyImmediate`. 씬을 `OpenScene`하면 메모리의 ScriptableObject 참조가 풀리니 다시 불러올 것. `AddComponent` 후 `MarkSceneDirty`.
- 새 파일을 추가하면 Unity가 `.meta`를 만든다. 둘 다 커밋.
- 새 재료를 만들면 `MaterialSetup`에 추가 → 메뉴 실행 → "재료 목록 만들기" → "재료 아이콘 만들기" → "UI 만들기"(주문 목록) 순서.
- 모델을 바꾸면 "병 만들기"(JarSetup) → "병 그림 만들기" → "UI 만들기".
- 섬을 다시 만들면 "섬 지도 만들기"도 다시. 씬 파일이 크므로 불필요한 재생성은 피한다.

**게임 디자인 원칙 (기획서의 정신)**: 코인은 점수가 아니다(없어도 끝까지 즐길 수 있음). 시들거나 사라지는 것, 시간 제한, 불이익이 없다. 가방이 꽉 차도 불이익 없음. 잠은 사용자가 스스로 자야 다음 날이 된다(밤에도 억지로 재우지 않음). 힐링, 아기자기한 로우폴리 분위기를 유지한다.

## 분위기: 편지, 날씨, 소리 (구현됨)
- **시작 편지**: 처음 시작하고 2.5초 뒤 LetterPanel이 저절로 열려 섬에서 하는 일을 알려 줌(E/Esc/스페이스/닫기로 닫음, 읽음은 GameState.LetterRead로 저장). 집 책상 위 편지(DeskLetter)를 보고 E로 다시 읽음.
- **날씨(8종)**: 맑음, 흐림, 바람, 이슬비, 비, 폭풍우(천둥번개), 안개, 눈. **아침마다 무작위로 뽑음**(`Weather.Roll`, 첫날은 맑음, 확률 30/14/8/11/15/6/9/7%, 같은 날씨 사흘 연속 없음). 오늘과 어제 날씨는 `GameState.WeatherToday/WeatherYesterday`에 저장됨(날씨가 저장되지 않은 예전 저장은 켤 때 무작위로 뽑음). 모습과 소리는 에셋 **URP Dynamic Weather System**(`Assets/DynamicWeatherSystem`, 유료 에셋이라 저장소에 올리지 않음)의 비, 눈, 바람, 번개, 소리 모듈을 `WeatherEffects`가 런타임에 꾸려서 씀. 에셋의 조명, 안개, 하늘 모듈은 쓰지 않고 `GameWeatherModule`이 섞인 값(어두운 정도, 하늘 색과 밝기, 안개, 비, 눈, 바람)을 정적 값으로 내놓으면 `SkyController`가 낮밤 위에 덧씌움(흐린 날은 하늘 셰이더 `SkyBlend`의 `_Overcast`로 잿빛 구름 하늘을 덮어서 짙은 남색이 되지 않게 함). 날씨 상태 파일은 `Assets/Resources/Weather/WS_(종류).asset`(메뉴 유리병 정원 > 날씨 상태 만들기). 집 안에서는 비와 눈 입자를 숨기고 날씨 소리를 먹먹하게(저역 통과) 낮춤. 이슬비, 비, 폭풍우인 날과 그다음 날(`Weather.MushroomDay`)에만 버섯(`AfterRain`) 픽업이 나타남. 비 오는 날에는 `RainGround`가 플레이어 둘레 땅에서 작은 물방울이 톡 튀게 하고, 땅(Terrain)이 천천히 젖어 살짝 윤이 나게 함(Terrain.SetSplatMaterialPropertyBlock의 _Smoothness). 그친 뒤 약 50초에 걸쳐 마름. F9 키로 날씨를 하나씩 바꿔 볼 수 있음(개발용). 날씨가 바뀌면 6초에 걸쳐 부드럽게 변하고 알림 메시지가 뜸.
- **소리**: 소리 파일 없이 코드로 만든 소리(GameAudio). 낮/밤 배경 음악(시간에 따라 섞임, P 키로 켜기/끄기), 파도(바닷가에 가까울수록 큼), 바람, 낮의 새 소리, 밤의 풀벌레, 빗소리, 집 안 방 소리, 발소리, 효과음(줍기, 놓기, 빼기, 완성 종소리, 코인, 종이, 버튼). 진짜 소리 파일을 구하면 GameAudio의 clip만 바꿔 끼우면 됨.

- **병 속 흙과 모래의 겉모습**: SoilLook이 코드로 흙 그림(갈색 얼룩, 알갱이, 작은 자갈)과 울퉁불퉁한 법선 그림, 모래 그림을 만들어 병 속 흙(BottleContents.groundRenderer)에 입힘. 흙 메시는 쓸 때 복사해서 UV와 접선을 붙임(원래 메시 에셋은 그대로). 윤기 없는 재질.

- **음악과 발소리는 사신 에셋 사용** (유료라 저장소에 올리지 않음): 배경 음악은 Dreamcore Music Vol. 1의 LOOP 곡 8개를 MusicPlayer가 때와 날씨에 맞게 고름(눈=Winter, 폭풍우=Purgatory/Oddity, 비=Fragments/Tone/Tone 2, 안개/흐림=Oddity/Tone/Tone 2, 밤=Lost/Tone 2/Fragments, 낮=Ice Cream/Tone 2/Tone. 6초 섞어 넘어가고 한 곡은 약 2분 반 뒤 같은 모음의 다른 곡으로). 곡 연결은 MusicSet(Resources, 메뉴 배경 음악 연결). 발소리는 Footsteps Sounds - Volume 02를 FootstepPlayer가 걷는 거리마다 내고, 서 있는 땅(Terrain 재질 Grass/Sand/Soil, 집 안은 나무)에 맞는 소리를 고름. 비 오는 날은 젖은 땅 소리가 섞임. 연결은 FootstepSet(Resources, 메뉴 발소리 연결). 에셋이 없으면 GameAudio가 코드로 만든 음악과 발소리를 씀.

## 자동 저장, 메뉴, 병 주문 편지, 날씨 재료, 반딧불이, 창가 전시 (구현됨)
- **자동 저장**: 침대에서 잘 때뿐 아니라 게임을 끌 때(OnApplicationQuit), 앱이 멈출 때, 집을 드나들 때, 2분마다 저절로 저장한다(`GameRoot.SaveNow`). 저장 파일을 읽지 못했으면(`SaveSystem.LoadFailed`) 덮어쓰지 않는다. 하루 시간(`timeOfDay`)과 오늘 가져간 자리(`taken`)도 저장해서, 껐다 켜도 자리가 다시 채워지지 않는다.
- **Esc 메뉴**(`PauseMenu`, 코드로 만듦): 이어서 하기, 지금 저장하기, 소리 크기(PlayerPrefs `master_volume`), 음악 켜기/끄기, 게임 끝내기(두 번 눌러야 끝남). 열려 있는 동안 `Time.timeScale=0`. 다른 패널이 열려 있거나 방금 닫혔으면 열리지 않는다(`UIFocus.LastCloseFrame`). 코드로 UI를 만들 때는 `UIKit`(종이색 패널, 버튼, 슬라이더) 도우미를 쓴다.
- **병 주문 편지**(`BottleRequests`, `RequestPanel`): 둘째 날부터, 완성한 병이 1개 이상이면 아침마다 약 65% 확률로 우체통에 편지가 온다(`GameState.RollRequest`, 한 번에 하나, 기한과 실패 없음). 편지는 15가지(바닷가, 숲, 꽃밭, 밤, 가득 찬 병, 모래, 캠핑, 단순한 병, 차와 책, 비, 눈꽃, 반딧불이, 바다 유리, 요정, 열기구). 우체통에서 E를 누르면 편지 화면이 먼저 열리고, 조건에 맞는 병이 있으면 두 번 눌러 보낸다(병은 `BottleStore.Remove`로 사라짐). 답례(코인과 재료)는 우체통에 도착(`GameState.CompleteRequest`). 저장: `requestId/requestSeen/lastRequestId/requestsDone`.
- **날씨 재료**(`MaterialCondition.SnowDay/FogDay/AfterStorm` 추가): 눈꽃(눈 오는 날, 꽃 언덕), 안개꽃(안개 낀 날, 꽃 언덕), 이슬방울(비 온 날과 다음 날, 꽃 언덕), 바다 유리(폭풍우 날과 다음 날, 바닷가). 각 6곳씩 섬에 놓여 있고 그 날씨에만 보인다(`Pickup.ConditionMet`, `Weather.StormDay`). 모델은 `PlantModelMaker.BuildWeatherModels`(메뉴 날씨 재료 모델 만들기).
- **반딧불이**(`FireflySwarm`, `FireflyBug`): 섬에서 밤(시간 0.74 이후)에 비, 폭풍우, 눈이 아닐 때 36마리가 집 둘레, 숲, 꽃 언덕 위를 떠다닌다. E로 잡으면 재료 `firefly`(오늘 밤 사라지고 다음 날 다시 나옴). 빛무리는 코드로 만든 스프라이트.
- **밤에 빛나는 병 속 재료**(`NightGlow`): 초, 별 장식, 초승달, 달맞이꽃, 반딧불이, 캠프파이어, 요정 문, 눈꽃은 밤이 되면 병 속에서 은은히 빛나고, 병 하나에 작은 불빛이 하나 생긴다.
- **창가와 책상 전시**: 선반 24칸 말고 책상 위 1곳, 북쪽 창턱 2곳, 동쪽 창턱 2곳이 더 있다(칸 번호 24~28, `BottleStore.ExtraSlotStart/ExtraSlotCount`). 선반이 다 안 열려도 처음부터 쓸 수 있고, 선반이 차면 이쪽으로 자동 배정된다. 집 안 가구를 다시 만들려면 메뉴 선반 다시 만들기.