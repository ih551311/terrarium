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
## 눈 섬, 선착장, 눈 섬 재료 (구현됨)
- **눈 섬**(`SnowIsland` 씬, 메뉴 눈 섬 만들기 = `SnowIslandSetup`): 지름 약 190m. 가운데 눈 마을(집 3채, 우물, 눈사람, 울타리, 돌담, 팻말, 선착장까지 징검돌 길), 얼어붙은 연못, 북쪽 소나무 숲(190그루), 눈 더미 34개, 소품 나무. 지형(Terrain, 층 Snow/Ice/Path)과 바다(차가운 색, 이름 SeaCoast/SeaFar)는 코드로, 건물과 나무는 에셋 **Cozy Snowy Environment**(`Assets/HobiSoloved`, 유료라 **저장소에 올리지 않음**)의 프리팹으로 만든다. 에셋 재질은 기본 셰이더라 `SnowAssetSetup.ConvertMaterials`(메뉴 눈 에셋 재질 바꾸기)로 URP로 바꾼다. 처음 만들 때 Island 씬을 복사해서 시작한다(플레이어, 하늘, 조명). **눈 섬 씬은 에셋이 없으면 깨져 보인다.**
- **늘 눈**: `SceneClimate`가 씬이 열려 있는 동안 `Weather.Forced = Snow`로 붙잡아서 눈, 구름 하늘, 안개, 겨울 음악이 따라온다. 눈 섬에는 지도(M, N), 반딧불이가 없다(씬 이름이 Island일 때만). 발소리는 층 이름에 snow/ice가 있으면 부드러운 땅(Soil) 소리.
- **선착장**(`Dock`, `DockSetup`): 원래 섬 남쪽 바닷가(0, -107)에 있다. 처음엔 부서진 부두 조각과 팻말뿐이고, 바라보고 E로 **150코인**을 내고 고치면(`GameState.DockOpen`, 저장됨) 긴 부두와 배가 나타난다. 고친 뒤 E로 눈 섬으로 간다. 눈 섬 동쪽 선착장은 이미 열려 있어 E로 돌아온다. 도착 자리: Island `FromSnow`, SnowIsland `FromDock`. 값은 `DockSetup`의 `cost`(150), 메뉴 선착장 만들기로 원래 섬 선착장을 다시 만든다.
- **눈 섬 재료 11종**(`MaterialZone.Snow`, `ToolSettings.Roll`이 눈 섬 밖 나무와 돌에서는 안 나오게 거른다): 도끼 소나무에서 솔잎 가지, 눈 덮인 솔방울, 장작 / 곡괭이 눈 더미에서 얼음 조각, 고드름 / 줍기 호랑가시 열매, 서리 낀 잎, 눈덩이, 미니 눈사람, 겨울 방울, 벙어리장갑. 눈꽃도 눈 섬에 10송이 핀다(늘 눈이 와서 늘 보임). 모델은 `PlantModelMaker.BuildWinterModels`. 주운 자리 이름(id)은 눈 섬이면 씬 이름이 앞에 붙는다(`SceneKey`).
- 병 주문 편지에 눈 섬 편지 2개(눈 섬 재료 3개, 장작과 장갑 또는 방울) 추가.
- **아직 없는 겨울 소품 아이디어**: 미니 오두막, 겨울 랜턴, 눈 쌓인 팻말(우체통 상점용), 오로라 조각(희귀), 얼음 호수 낚시, 눈 섬 전용 병 종류, 눈 위 발소리 팩(MGWSoundDesign Snow and Grass).
## 눈 섬 갱신: 섬 모양, 선착장, 지도, 건물 재료 (구현됨)
- **섬 모양**: 해안선이 굽이치고(`CoastFactor`), 바다 위에 얼음덩이 22개가 떠 있다. **지형 높이 저장 오류 수정**: TerrainData를 메모리에서 높이와 칠을 모두 채운 뒤 **마지막에** `CreateAsset`으로 저장하고, 만든 뒤 `VerifyTerrainOnDisk`로 디스크에서 새로 읽어 확인한다(예전에는 껐다 켜면 지형이 바다 바닥으로 평평해졌다).
- **선착장 위치**: 두 섬 모두 물가(높이 0.25m)에 땅 쪽 끝을 두고 부두가 물속(-0.6m)까지 닿는다. 부두 윗면 0.35m. 원래 섬은 물가가 가장 가파른(부두가 짧은) 방향을 고른다(메뉴 선착장 만들기).
- **지도**: `SceneMapInfo`(씬마다 지도 그림, 가운데, 범위, 돌아가기 도착 지점)를 두면 지도(M)와 작은 지도(N)가 그것을 쓴다. 눈 섬은 `Assets/Data/SnowIslandMap.png`(눈 섬 만들기가 같이 만든다). 두 섬 모두 선착장을 파란 "배" 표시로 보여 주고, 눈 섬의 돌아가기 버튼은 "선착장으로 돌아가기"(`GameRoot.GoHome(spawnId)`). 지도는 씬 이름이 Island이거나 SceneMapInfo가 있는 씬에서만 열린다.
- **건물은 재료로**: 눈 섬에는 집과 우물을 놓지 않는다. 눈 오두막 1~3과 눈 우물(`snow_cottage1~3`, `snow_well`)은 병 속에 놓는 꾸밈 재료이고 **우체통 주문에서 산다**(30, 32, 35, 24코인). 모델은 눈 에셋 프리팹을 0.05배로 줄여 담은 프리팹 `Assets/Models/Deco/deco_snow_*.prefab`(`BuildingMaterialSetup`, 눈 에셋이 없으면 비어 보인다). 우체통 주문 칸은 20개(칸 높이 66)로 늘렸다. 재료 추가 순서: 모델, 재료 데이터 만들기, 재료 목록 만들기, 재료 아이콘 만들기, UI 만들기.
## 눈 바닥재, 눈사람 캐기, 우체통 메뉴 (구현됨)
- **바닥재 눈**: 모래처럼 `snow` 재료(Ground)를 눈 섬에서 줍고(14곳, 모델 없는 하얀 눈 더미), 병 꾸미기의 바닥재 버튼(흙, 모래, **눈**)으로 병 바닥에 깐다(가방의 눈 1개 소모, 바꾸면 돌려받음). 모습은 `SoilLook.BuildSnow`가 만든 눈 그림. `GroundSelector`에 snow 필드, UISetup 바닥재 상자 높이 290.
- **눈사람은 캐는 것**: 눈 섬 마을의 눈사람 6개를 곡괭이로 캐면 미니 눈사람이 나온다(`Harvestable.fixedMaterial`: 정해 둔 재료가 늘 나옴). 캔 눈사람은 푸르스름하게 바래고 다음 날 돌아온다. 미니 눈사람은 더 이상 바닥에 놓여 있지 않다.
- **벙어리장갑은 구매**: 우체통 상점(14코인)에서 산다. 눈 섬에서 줍지 않는다.
- **우체통 메뉴**(`MailboxPanel`, UISetup 우체통 블록): 열면 큰 버튼 네 개(**판매하기, 업그레이드, 구매하기, 받기**)가 있는 메뉴가 뜨고, 버튼을 누르면 그 화면만 따로 열린다. 각 화면 왼쪽 위의 "< 우체통 메뉴"로 돌아가고, Esc는 한 화면 뒤로(메뉴에서는 닫기). 판매하기는 8열 x 4행 가방 칸, 구매하기는 5열 격자(칸 25개까지), 받기는 큰 버튼과 도착한 것의 목록. 메뉴의 받기 버튼에는 도착한 것이 있으면 (!)가 뜬다. 상점 목록이 바뀌면 메뉴 UI 만들기를 다시 누른다.
## 꾸밈 소품, 동물, 병 속 효과 (구현됨)
- **병 속 효과**(`BottleEffect`): 우체통 구매하기에서 **작은 유리병 모양 효과 재료**를 사서 병에 놓으면(id가 `fx_`로 시작) 병 안쪽 넓은 부분을 입자가 채운다. `fx_snow` 눈 내리기(18코인), `fx_glitter` 반짝이 가루(15), `fx_petals` 꽃잎 날리기(15), `fx_bubbles` 비눗방울(12), `fx_rain` 보슬비(14). 입자는 병의 자식(로컬 공간, 병을 돌리면 같이 움직이고 선반에서 줄이면 같이 작아짐)이고 재료를 빼면 사라진다. 병 안쪽 범위는 `BottleContents.TryGetInterior`(안쪽 반지름 표에서 넓은 부분)로 구하고, 흙 표면 높이가 바뀌면 다시 맞춘다. 입자 그림은 코드로 만들고 재질은 항상 들어가는 Sprites/Default. 새 효과는 `BottleEffect.Presets`에 한 줄 + 모델 + 재료 표.
- **꾸밈 소품 15종**(`PropModelMaker`, 메뉴 꾸밈 소품 모델 만들기): 랜턴(밤에 빛남), 벤치, 나무 울타리, 돌다리, 작은 등대(밤에 빛남), 분수, 망원경, 깃발, 연, 돛단배, 버섯 집, 작은 새, 토끼, 고양이, 여우. 모두 우체통에서 산다(6~30코인).
- **우체통 구매하기 쪽 넘기기**: 상점 물건이 41개라 한 쪽 25칸, 이전/다음 버튼(`MailboxPanel.orderPage`). 병 주문 편지에 눈 내리기, 동물, 항구 편지 3개 추가.
## 눈 위 발소리, 오로라, 얼음 낚시 (구현됨)
- **눈 위 발소리**: 에셋 `Footstep(Snow and Grass)`(MGWSoundDesign, 에셋스토어 캐시에서 가져옴, 유료일 수 있어 **저장소에 올리지 않음**)의 한 걸음 소리 `Snow1 N-Audio` 30개를 `FootstepSet.snow`에 연결(메뉴 발소리 연결). 지형 층 이름에 snow나 ice가 있으면 `Surface.Snow`로 눈 소리를 낸다(눈 섬 전체). 눈 소리가 없으면 흙 소리.
- **오로라**(`AuroraSky`, 셰이더 `유리병 정원/Aurora`, 재질 `Resources/AuroraMat`): 눈 섬 밤하늘(시간 0.72~0.88에 서서히 나타남)에 북쪽으로 초록-청록-보라 빛의 커튼 두 겹이 일렁인다. 카메라를 따라다니는 반지름 600m 띠 메쉬(앞의 나무와 산은 가린다).
- **얼음 낚시**(`FishingSpot`): 눈 섬 얼어붙은 연못에 낚시 구멍 3곳(찌가 뜬다). 바라보고 E로 드리우면 3~8초 뒤 입질, 2.4초 안에 E로 당기면 물고기가 가방으로. 놓쳐도 불이익 없음. 물고기: 빙어(55%), 얼음 송어(30%), 금붕어(15%), 밤에만 12% 확률로 오로라 물고기(희귀, 밤에 빛남). 모두 병 속에 놓는 꾸밈 재료(바닥에 놓여 있지 않음). 모델은 `PropModelMaker.Fish`.
## 눈 섬 전용 병 (구현됨)
- **스노우볼**(`bottle4`, 18코인)과 **눈꽃 병**(`bottle5`, 22코인): 병 종류가 4개에서 6개가 됐다. 둘은 **선착장을 고쳐야 만들 수 있다**(`BottleChooser.IsUnlocked`: 번호 4 이상은 `GameState.DockOpen`이 있어야 함, 잠겼을 때 카드에 "선착장을 고치면 열려요"). 스노우볼은 둥근 구슬 모양에 눈 덮인 마개, 눈꽃 병은 열 면이 각진 결정 모양에 빨간 뾰족 마개.
- 유리 모델은 `BottleModelMaker`(메뉴 눈 섬 병 모델 만들기)가 회전체 껍데기로 만든다(바깥면, 안쪽면, 바닥, 입구 둘레, 안쪽 바닥 가운데 꼭짓점이 있어야 `JarSetup`이 안쪽 반지름 표를 읽는다). 새 병 종류를 더하려면 `JarSetup.Jars`에 한 줄, `BottleIconMaker.Ids`에 id, UISetup `ChoiceCount`와 typeIcons를 늘린다. 각진 병은 `Jar.fit`(안쪽 반지름에 곱함, 눈꽃 병 0.95)으로 평평한 면 안에 재료와 흙이 들어가게 한다. 순서: 병 모델, 장식 모델 가져오기 설정, 병 만들기, 병 그림 만들기, UI 만들기.
- **병 다시 만들기 주의**: `JarSetup.Build`는 흙 모양 파일을 지우지 않고 같은 파일에 덮어쓴다(`SaveMeshAsset`, GUID를 지켜 프리팹과의 연결이 끊기지 않게). 병을 다시 만든 뒤에는 병 프리팹을 강제로 다시 가져온(`AssetDatabase.ImportAsset(..., ForceUpdate)`) 다음 병 그림 만들기와 UI 만들기를 해야 한다(그렇지 않으면 메모리에 남은 옛 프리팹으로 그림이 흙 없이 찍힌다). Play 중에는 병 만들기를 하지 않는다.
## 병 버리기 · 병 종류 정리 (구현됨)
- **병 버리기**: 병 꾸미기 화면의 "완성" 아래 분홍 "병 버리기" 버튼(`BottlePanel.OnCancelClicked`). 두 번 눌러야 지워진다(첫 클릭은 확인 문구). 만들 때 낸 코인(`BottleRecord.paidCoins`) 또는 공짜 병(`usedFree`)을 돌려주고, 병 안 재료와 바닥재는 가방으로 돌려준다(가방이 차면 우체통 도착함). 저장 파일에도 `paidCoins`, `usedFree`가 들어간다.
- **병 종류**: 눈 구슬(snowglobe)은 없애고 **원기둥 병**(`cylinder`, 조금 넓은 원기둥, 10코인)으로 바꿨다. 키가 큰 병(`tall`)은 목록에서 뺐다(`BottleChooser.IsRetired`, 모델은 남아 있다). 가격: 6, 6, 10, 14, 10, 22.
- **흙 옆면**: `SoilLook.Styled`가 윗면은 평면 UV, 옆면은 (각도×반지름, 높이) UV로 다시 만든다(이음새 정점 복제).
## 날씨 재료 더 8종 (구현됨)
- 날씨마다 나타나는 재료가 8종 늘었다. 모두 섬에 6곳씩 놓여 있고 그 날씨에만 보인다(`Pickup.ConditionMet`). **비 온 날과 그다음 날(숲)**: 빗물 이끼(`rain_moss`), 푸른 버섯(`blue_mushroom`, 밤에 병 속에서 푸르게 빛남), 청개구리(`tree_frog`). **안개 낀 날(숲)**: 이슬 거미줄(`spider_web`). **바람 부는 날(꽃 언덕)**: 바람 깃털(`wind_feather`), 민들레 홀씨(`dandelion_puff`). **폭풍우 치는 날(바닷가)**: 번개 유리(`lightning_glass`, 밤에 은은히 빛남). **비가 그친 뒤 맑거나 흐린 날(바닷가)**: 무지개 조약돌(`rainbow_pebble`).
- `MaterialCondition`에 `WindDay`, `StormOnly`, `RainbowDay` 추가. `Weather.RainbowDay`(어제 비 + 오늘 맑음/흐림, F9로 고정했을 때는 맑음/흐림이면 true). 바람 날씨 확률을 8%에서 10%로, 맑음을 30%에서 28%로 바꿨다. 날씨 알림 문구(`WeatherEffects.Announce`)에 새 재료 힌트를 넣었다.
- 모델은 `PlantModelMaker.BuildMoreWeatherModels`(메뉴 날씨 재료 모델 더 만들기), 아이콘은 `IconBaker`가 3D 모델에서 찍는다. 순서: 모델 → 장식 모델 가져오기 설정 → 재료 데이터 만들기 → 재료 아이콘 만들기 → 재료 목록 만들기 → 섬에 새 재료 놓기(Play 중에는 하지 않는다).
## 사막 섬, 사막 재료 (구현됨)
- **사막 섬**(`DesertIsland` 씬, 메뉴 사막 섬 만들기 = `DesertIslandSetup`): 지름 약 210m의 모래 언덕 섬. 가운데 유목민 야영지(천막 4, 모닥불, 낙타 3), 서쪽 오아시스(연못과 야자나무 18그루), 북쪽 피라미드 언덕, 동쪽 선착장, 곳곳에 선인장 80그루, 바위 70개, 알로에와 마른 가지. 늘 맑다(`SceneClimate` Clear). 모래 언덕은 `Dune`(바람받이는 완만, 안쪽은 가파름), 지형 층은 Sand/RedSand/Path/OasisGrass(층 이름의 sand/path가 발소리를 정함). 에셋 **POLYDesert**(`Assets/Runemark Studio`, 선인장 3종, 알로에, 바위 8종 12개 프리팹)는 `DesertIslandSetup.ConvertMaterials`(메뉴 사막 에셋 재질 바꾸기)로 URP로 바꿔 쓰고, 야자나무, 천막, 낙타, 피라미드는 사막 재료 모델(`deco_*`)을 크게 키워서 쓴다(`Place`가 높이 미터로 크기를 정함).
- **사막 선착장**: 원래 섬 동북쪽 바닷가에 두 번째 선착장(`DesertDock`, 메뉴 사막 선착장 만들기 = `DockSetup.BuildDesertDock`). 눈 섬 선착장과 떨어진 물가를 고르고 250코인을 내고 고친다. `Dock`에 `dockId`(0 눈 섬, 1 사막 섬)와 `islandName`이 생겼고 열림 상태는 `GameState.DesertDockOpen`(저장 `desertDockOpen`)이다. 사막 섬에서 돌아올 때는 `Spawn_FromDesert` 자리에 선다. 눈 섬 선착장이 다시 만들어져도 사막 선착장은 따로 남는다.
- **사막 재료 14종**(`MaterialZone.Desert`, 사막 섬에서만 난다. 다른 섬의 도끼, 곡괭이에서는 안 나온다 - `ToolSettings.Roll`): 선인장(`cactus_piece`, 선인장을 도끼로), 대추야자(`date_fruit`, 야자나무를 도끼로) - 둘 다 나무마다 정해진 재료(`fixedMaterial`). 사암 조각, 사막 장미, 화석(희귀)은 바위를 곡괭이로. 선인장 꽃, 알로에, 마른 가지, 옛 도자기 조각(피라미드 근처), 풍뎅이(야영지에서 먼 곳)는 줍기. 낙타, 야자나무, 사막 천막, 작은 피라미드는 우체통 구매하기(26/18/20/30코인). 푸른 버섯과 번개 유리처럼 밤에 빛나는 재료는 아니다. 모델은 `PlantModelMaker.BuildDesertModels`(메뉴 사막 재료 모델 만들기).
- 순서: 사막 에셋 재질 바꾸기 → 사막 재료 모델 만들기 → 장식 모델 가져오기 설정 → 재료 데이터 만들기 → 도구 설정 만들기 → 재료 아이콘 만들기 → 재료 목록 만들기 → 사막 선착장 만들기 → 사막 섬 만들기 → UI 만들기(Play 중에는 하지 않는다). `IslandSetup`의 재료 놓기는 `MaterialZone.Desert` 재료를 원래 섬에 놓지 않는다.
- 아직 없는 것: 사막 전용 병 종류, 붉은 모래 바닥재.
## 선착장 하나로 합침, 사막 섬 충돌과 높이 고침 (구현됨)
- **선착장은 원래 섬에 하나**(`MainDock`, `Dock.hub`): E를 누르면 `DockPanel`(코드로 만든 창)이 열려 "어느 섬으로 갈까요?"를 고른다. 섬마다 따로 코인을 내고 열고(눈 섬 150, 사막 섬 250), 열린 섬은 "가기"로 이동한다. 새 섬을 더하려면 `DockPanel.Destinations`에 한 줄과 `GameState.IsDockOpen/OpenDock`에 열림 상태를 더한다. 사막 선착장(`DesertDock`)과 `DockSetup.BuildDesertDock`은 없앴다. 눈 섬과 사막 섬에서 돌아오면 원래 섬의 같은 선착장 앞(`Spawn_FromSnow`, `Spawn_FromDesert`)에 선다.
- **사막 섬 충돌**: 천막, 낙타, 피라미드, 바위는 보이지 않는 상자 대신 모양 그대로의 `MeshCollider`(`AddMeshCollider`)를 쓴다. 선인장은 크기에 맞춘 캡슐이다.
- **공중에 뜸 고침**: `DesertIslandSetup.Place`가 발밑 땅 여덟 곳 중 가장 낮은 곳에 맞춰 0.12m 묻는다. 모래 언덕 비탈에서도 한쪽이 뜨지 않는다.
## 시작 위치, 미쿠 NPC와 꾸밈 재료 (구현됨)
- **시작 위치**: 게임을 처음 켜면 `GameRoot.Start`가 `GoHome()`으로 집 앞(Spawn_FromHut)에서 시작한다(다른 씬에서 넘어온 것이 아닐 때만. 집 안에서 켠 경우에는 그 씬에 FromHut이 없어서 아무 일도 없다).
- **미쿠 NPC**(`MikuNPC`, 원래 섬 집 앞 시작 자리에서 오른쪽 2.5m, 앞으로 4m, 프리팹 `Assets/Prefabs/Miku.prefab`): 바라보고 E를 누르면 "안녕 나는 미쿠야"가 뜬다(`NpcTalk`, 대사는 lines에 줄을 더하면 누를 때마다 다음 줄). 7m 안에서는 플레이어를 천천히 바라본다. 모델은 `Assets/Models/Miku/MIKU.fbx`(받은 파일. 피부색과 흰색 재질밖에 없어서 `MikuSetup`이 부분별로 색 재질을 입힌다: Box001, d 피부, Box003, Box004, Cone001 머리, Box005 옷, Box008 입, Sphere002, Sphere003 눈). 모델은 -z를 보고 있어서 NPC는 180도 돌려 쓴다. 메뉴 미쿠 만들기(`MikuSetup.Build`)로 다시 만든다.
- **미쿠 꾸밈 재료**(`miku`, 우체통 구매하기 12코인, 되팔기 6코인): 키 8cm 모델 `Assets/Models/Deco/deco_miku.prefab`. 아이콘에서 얼굴이 보이게 -z를 본다. 순서: 미쿠 만들기 → 재료 데이터 만들기 → 재료 아이콘 만들기 → 재료 목록 만들기 → UI 만들기.
- **사막 섬 피라미드**: 작은 피라미드를 겹쳐 키우면 면이 맞닿은 곳에 어두운 쐐기가 보여서, 섬 풍경에는 단이 열두 개인 계단식 피라미드(deco_pyramid_steps, 키 16m)를 쓴다. 꾸밈 재료 피라미드(deco_pyramid)는 그대로다.

## 치비 NPC와 꾸밈 재료 (구현됨)
- **치비**(`ChibiNPC`, 집 문 바로 앞 왼쪽 x -1.3, z 11.6. 키 1.95m): 인사하는 자세의 SD 캐릭터. 말은 걸 수 없고(`NpcTalk` 없음) 충돌 캡슐만 있다. 우체통(왼쪽)과 가로등(오른쪽) 사이라서 겹치지 않게 이 자리로 정했다. 모델 `Assets/Models/Chibi/Chibi.fbx`는 뼈대가 있는 모델(SkinnedMeshRenderer 15개)이라 `ChibiSetup`이 자세 그대로 일반 메쉬로 굳힌 `Chibi_Baked.prefab`(메쉬는 `Baked/`)을 만들어 쓴다. 메쉬는 같은 파일 안에서 내용만 바꿔야 한다(지우고 새로 만들면 프리팹의 메쉬 연결이 끊겨 치비가 안 보인다). 굳힐 때 부품 크기는 `lossyScale`로 따로 줘야 한다(팔찌, 별, 리본이 틀어진 적이 있다). 모델은 +z를 본다. 메뉴 치비 만들기(`ChibiSetup.Build`)로 다시 만든다.
- **치비 꾸밈 재료**(`chibi`, 우체통 14코인, 되팔기 7코인): 키 9cm 모델 `Assets/Models/Deco/deco_chibi.prefab`. 순서는 미쿠와 같다(치비 만들기 → 재료 데이터 → 아이콘 → 재료 목록 → UI 만들기).
## 모래시계 병과 붉은 모래 바닥재 (구현됨, 사막 섬 전용)
- **모래시계 병**(`hourglass`, 병 번호 6, `bottle6`, 24코인): 아래 큰 방울, 잘록한 허리, 위 작은 방울, 좁은 목의 호리병 모양. 모델 `jar_hourglass`(+`_cork`)는 `BottleModelMaker.Hourglass`(메뉴 눈 섬 병 모델 만들기)가 만든다. **사막 섬 배를 열어야 만들 수 있다**(`BottleChooser.IsUnlocked(6)` = `GameState.DesertDockOpen`, 눈꽃 병 5번은 `DockOpen`). 병 종류를 더하려면 `JarSetup.Jars`, `BottleIconMaker.Ids`, UISetup `ChoiceCount`와 typeIcons(두 곳), `BottleChooser.prices`를 함께 늘린다. 재료는 흙이 있는 아래 방울에 놓는다.
- **붉은 모래 바닥재**(`red_sand`, `MaterialKind.Ground`, 구역 Desert): 사막 섬의 붉은 모래 지대에서 14곳, 더미 모양 임시 모델로 줍는다. 병 꾸미기의 바닥재 상자에 "붉은 모래 xN" 버튼(`GroundSelector.redSand`)이 있고, 그림은 `SoilLook.BuildRedSand`가 코드로 만든다(주황빛 붉은 바탕에 바람 물결). 순서: 눈 섬 병 모델 만들기 → 장식 모델 가져오기 설정 → 재료 데이터 만들기 → 병 만들기(JarSetup, 끝나면 병 프리팹 강제로 다시 가져오기) → 병 그림 만들기 → 재료 아이콘 만들기 → 재료 목록 만들기 → UI 만들기 → 사막 섬 만들기.
## 전체 점검 (2026-11-18)
- 재료 125종(아이디 중복, 아이콘, 모델, 이름, 상점 가격), 씬 4개(빠진 스크립트, 빈 재료 줍는 자리, 빈 메쉬, 빈 재질)를 검사했고 문제 없음. 섬 4곳을 배로 오가며 콘솔 오류와 경고 0개.
- 고친 것: 섬을 옮기면 날씨를 6초에 걸쳐 바꾸던 탓에 눈 섬에서 사막 섬으로 가면 눈이 계속 내렸다. 이제 씬이 바뀌면 날씨를 바로 바꾸고 남은 눈, 비 입자를 지운다(`WeatherEffects.sceneChanged`, 그때는 날씨 알림도 띄우지 않는다). 우체통 구매하기 화면에서 제목과 쪽 번호(1/2쪽)가 겹쳐서 쪽 번호를 "다음" 버튼 오른쪽으로 옮겼다.

## 섬 물체 크기 점검 (2026-11-18)
- 플레이어 키 1.8m 기준으로 섬 3곳의 물체 높이를 쟀다. 나무(4~12m), 바위, 선인장, 야자나무, 피라미드(16m), 집 마당 소품은 적당했다.
- 고친 것: 줍는 재료가 몇 cm라 바닥에서 안 보이던 것(모래 달러, 바다 유리 4cm, 풍뎅이 6cm, 청개구리 7cm 등)은 가장 긴 변이 최소 0.28m가 되게 키운다(`PickupSize`, 메뉴 줍는 재료 크기 맞추기. 눈 섬, 사막 섬 만들기와 섬에 새 재료 놓기가 끝에 부른다). 눈 섬 울타리 2.2m를 약 1.2m로(`SnowIslandSetup.FenceScale` 1.3), 사막 천막 4.2m를 3.2m로, 모닥불 1.1m를 0.7m로, 낙타 2.6m를 2.3m로 줄였다.

## 말투와 UI 다듬기 (2026-11-18)
- 화면에 나오는 한국어 문구 470개를 모아 읽고, 기계적으로 들리는 말을 고쳤다. 시작 편지는 먼저 살던 정원사가 직접 쓴 짧은 쪽지 말투로 바꿨다(줄표 "—"를 빼고, "서두를 건 하나도 없어" 같은 판에 박힌 위로를 줄이고, 선착장과 다른 섬 이야기를 넣었다). 주문 편지의 위로 문구("천천히 만들어 봐요", "서두르지 않아도 돼요")를 뺐고, 답장 몇 개를 사람 말투로 바꿨다. 우체통의 "업그레이드"는 "도구 강화"로, "(잠은 집 안의 침대에서 자요)" 반복은 뺐다. "병 인벤토리"는 "내 병", 가방 창 제목 "인벤토리"는 "내 물건". 잠긴 병 카드의 "배를 열면 열려요"(같은 말 반복)는 "눈 섬 배를 열면 만들 수 있어요".
- UI: 지도 설명이 줄바꿈되며 "켜/기" 로 끊기던 것을 짧게 고쳤고, 우체통 메뉴의 큰 버튼 네 개가 창 아래까지 차도록 키웠다.

## 지도 표시 그림, 재료 쪽지 로딩 화면 (구현됨)
- **지도 표시**: 빨간 네모와 흰 막대 대신 게임 아이콘처럼 짙은 갈색 윤곽선을 두른 그림을 쓴다(`MapMarkerMaker`, 메뉴 지도 표시 그림 만들기, `Assets/Data/MapMarkers`). 내 위치는 주황 핀(뾰족한 쪽이 바라보는 방향), 집은 빨간 지붕의 작은 집, 선착장은 돛단배(`MapPanel.dockSprite`, `MiniMap.dockSprite`). 큰 지도와 작은 지도 모두 같다.
- **로딩 화면**(`LoadingScreen`, 코드로 만듦): 집을 드나들거나 배로 섬을 옮길 때(`GameRoot.GoTo` → `Travel` 코루틴: 화면을 덮고, `LoadSceneAsync`, 최소 2.4초 보여 준 뒤 걷힘) 짙은 청회색 바탕에 종이 쪽지가 뜨고, 섬에서 얻는 재료 하나의 그림과 이름, 어디서 언제 어떻게 얻는지를 알려 준다(`TipOf`: 구역, 나오는 때, 얻는 방법으로 문장을 만들고 낚시, 눈사람, 선인장 같은 것은 `Special` 표). 날씨나 밤 조건이 있는 재료, 희귀 재료가 더 자주 나온다. 아래에 "눈 섬으로 가는 중..." 처럼 갈 곳. 가는 동안 걷기와 줍기는 멈춘다(`UIFocus`).
- **중간 알림 없앰**: 날씨가 바뀔 때 뜨던 "비가 와요. 숲에 버섯이 나요" 같은 알림 글자를 더는 띄우지 않는다(`WeatherEffects`, 재료 조건은 로딩 화면 쪽지가 알려 준다). 줍기, 캐기, 잠자기 같은 결과 알림은 그대로.

## 다시 점검 (2026-11-18 저녁)
- 로딩 쪽지 문장 80개를 다 읽고 틀린 것을 고쳤다: 얼음 조각, 고드름은 "돌"이 아니라 눈 더미에서, 솔잎 가지, 눈 덮인 솔방울, 장작은 눈 섬 소나무에서, 사막 장미와 사암은 바위에서, 눈꽃은 눈 섬에서도 핀다는 것을 `LoadingScreen.Special`에 적었다.
- 섬 3곳에서 떠 있는 물체, 물속 재료, 막힌 도착 자리를 검사했다. 원래 섬 절벽 3개의 아래가 바닷속에서 조금 떠 있지만 물에 가려 보이지 않아서 그대로 뒀다.
- 화면 글자 넘침 검사: 지도 설명 칸이 글보다 작아서 키웠다.
- 선착장 창(`DockPanel`)과 편지 창(`LetterPanel`)은 E로 여는데 E로도 닫혀서, 같은 프레임에 바로 닫힐 수 있었다. 연 프레임에는 닫기 키를 무시한다(`openedFrame`).

## 완성 한 번에, 완성한 병 지우기 (구현됨)
- **완성 버튼**은 이제 한 번만 누르면 바로 완성되고 이름 짓는 상자가 뜬다(`CompleteBottleButton`, 예전에는 3초 안에 두 번).
- **병 지우기**: 병 자세히 보기(Tab 가방의 병 탭에서 병을 누르거나 선반의 병에서 E)에 분홍 "병 지우기" 버튼이 있다(`BottleDetailPanel.OnDeleteClicked`). 처음 누르면 "정말 지울까요? 한 번 더 누르면 지워져요"로 바뀌고, 한 번 더 누르면 `BottleStore.Remove`로 지운다(선반에 있었으면 선반에서도 사라짐). 재료나 코인은 돌려주지 않고, 지운 뒤 바로 저장한다.

## 도구를 쥔 손 (구현됨)
- 도끼, 곡괭이, 괭이를 휘두를 때 자루 아래쪽을 쥔 오른손이 함께 보인다(`ToolView.AddHand`, 기본 도형으로 만든 주먹, 손가락 마디 네 개, 엄지, 손목, 흰 소맷부리, 연두빛 스웨터 소매). 도구의 자식이라 같이 휘둘러진다. 손이 화면 아래에 묻히지 않게 도구 보기 자리를 조금 올렸다(y -0.3 → -0.24).

## 1인칭 도구와 손 다시 만듦 (구현됨)
- **모델**: 도끼, 곡괭이, 괭이와 그것을 쥔 오른손을 다른 물체와 같은 팔레트 로우폴리로 만든다(`ToolModelMaker`, 메뉴 도구와 손 모델 만들기). 메쉬는 `Assets/Models/Tools/*.asset`(같은 파일 안에서 내용만 바꿈), 묶음은 `Assets/Resources/ToolRig.prefab`(Hand, Axe/Pickaxe/Hoe 각각 Handle과 Head). 자루가 +Y, 손이 쥔 자리가 원점, 날은 앞(+Z). 손은 손가락 네 개가 자루를 감아 카메라 쪽으로 돌아오고 엄지가 그 위를 덮으며, 손등은 오른쪽, 손목에서 흰 소맷부리와 연두 스웨터 소매가 오른쪽 아래로 빠진다. 도구 머리는 흰색이라 강화 단계 색을 곱한다.
- **움직임**(`ToolView`): 캘 수 있는 것을 바라보면 그 도구를 들어 올리고(`Interactor`가 `SetAim`), 시선을 돌리면 0.7초 뒤 내려놓는다. 다른 도구로 바뀌면 내렸다가 바꿔 든다. 좌클릭하면 화면 밖 어깨를 축으로 들었다가(0.1초) 내려치고(0.07초) 잠깐 떨린 뒤(0.06초) 되돌아온다(0.24초). 연달아 눌러도 지금 자세에서 이어진다. 들고 있으면 숨쉬듯 오르내리고, 걸으면 흔들리고, 시선을 돌리면 살짝 늦게 따라온다. 자세 값은 `GripPos`, `GripEuler`, `ModelScale`, 휘두르기 각도는 `RaiseShoulderDeg` 등.
- **맞는 순간**: `Harvestable.Hit`은 소리와 나무 흔들림을 `ToolView.ImpactDelay`(0.17초) 뒤, 도구가 맞는 순간에 낸다.

---

## 메인 화면 (2026-11-18)

- 게임을 켜면 `Assets/Scenes/MainMenu.unity`가 먼저 나온다 (빌드 설정 0번). 화면은 전부 `Assets/Scripts/UI/MainMenu.cs`가 코드로 만든다(UIKit). 씬을 다시 만들려면 메뉴 "유리병 정원 > 메인 화면 만들기"(`Assets/Editor/MainMenuSetup.cs`).
- 버튼 세 개: **시작하기**(로딩 화면을 거쳐 Island로 간다. 저장이 있으면 GameRoot가 저절로 이어서 불러온다), **도움말**(조작키와 게임 방법), **설정**(소리 크기, 마우스 감도, 배경 음악, 화면 모드).
- 설정은 PlayerPrefs에 기억한다: `master_volume`(일시정지 메뉴와 같은 값), `look_sensitivity`(배율, FirstPersonController가 곱한다), `music_on`(GameAudio.MusicOn). 화면 모드는 기억하지 않는다.
- 배경 병 그림(유리, 흙, 뚜껑)과 반딧불이는 파일 없이 코드로 그린다. 병 속 재료 그림은 열 때마다 MaterialDatabase에서 다섯 개를 무작위로 고른다.
- 게임 제목 글자는 `MainMenu.GameTitle` 한 곳에 있다(지금은 임시 제목 "유리병 정원"). 글꼴에 가운뎃점(·)이 없어서 UI 글자에 쓰지 않는다.
- 아직 없는 것: 게임 안에서 메인 화면으로 돌아가는 버튼(GameRoot가 씬을 넘어 남아 있어서 돌아가려면 GameRoot를 지우고 상태를 되돌리는 처리가 필요하다).
- 게임 안 Esc 메뉴에 "메인 화면으로" 버튼이 있다(두 번 눌러야 한다). GameRoot.ReturnToMenu가 저장하고, 뿌리 오브젝트를 지운 뒤 MainMenu 씬으로 간다. SaveSystem.Save가 방금 저장한 내용을 기억해서 다시 시작해도 가방이 맞게 채워진다.

- **UI는 전부 실제 캔버스 오브젝트다 (2026-11-18).** 예전에는 UIKit으로 켜질 때 코드로 그렸지만 지금은 에디터에서 보이고 고칠 수 있다. 메인 화면은 MainMenu 씬 안의 `MainMenuCanvas`, 나머지(일시정지 메뉴, 로딩 화면, 편지 답장, 선착장 창)는 `Assets/Resources/UI/*.prefab`이다. 이 스크립트들은 구워 둔 버튼과 글자를 `[SerializeField]`로 받아 쓰고 버튼 연결만 한다. 화면 모양을 코드 기준으로 되돌리거나 다시 만들려면 메뉴 "유리병 정원 > 화면 캔버스 다시 만들기"(프리팹 네 개)와 "메인 화면 만들기"(씬 + 그림 `Assets/UI/MainMenu/*.png`)를 누른다(`Assets/Editor/UIBake.cs`). 손으로 고친 것은 다시 만들면 덮어써진다. 가방, 작업대 같은 나머지 화면은 원래부터 UISetup이 만든 캔버스다.
- **공중에 놓는 재료 (2026-11-18).** 반딧불이, 바람 깃털, 민들레 홀씨, 별 장식, 초승달, 연, 작은 새, 열기구는 병 안에서 흙에 닿지 않고 공중에 놓을 수 있다 (MaterialData.CanFloat, 목록은 FloatingIds). 식물, 돌, 집 같은 것은 그대로 흙 위에만 놓인다. 놓는 자리는 병 가운데를 지나고 병 카메라를 마주 보는 면에서 클릭한 곳이라 누른 높이 그대로 뜬다 (BottleContents.TryFindFloatingPlacement). 목록에 더하려면 FloatingIds에 id를 넣는다.

- **글자와 화면 손질 (2026-11-18).** 받침에 맞는 조사는 Kor.Eul/Iga/Eun으로 붙인다(을(를) 같은 표기를 쓰지 않는다). 설명하듯 뜨는 알림은 줄이고, 희귀한 것은 종소리로 알린다. 편지 20통은 사람마다 말투를 다르게 썼다(BottleRequests). 네모난 바탕은 UIRound가 둥근 모서리로 바꾼다(UISetup, UIBake가 끝에 부른다). TMP 설정은 한글을 단어 단위로 줄바꿈한다(TMP Settings.asset). 글꼴에 가운뎃점(·)과 화살표가 없어서 UI 글자에 쓰지 않는다.

## 겹침과 병 속 위치 고치기 (2026-11-19)
- **OverlapFixer** (Assets/Editor/OverlapFixer.cs, 메뉴: 유리병 정원 > 겹친 물건 고치기 / 모든 섬 겹침 고치기): 열린 섬 씬에서 나무, 바위, 눈 더미, 소품, 재료가 눈사람, 울타리, 집 같은 것과 겹치거나 같은 자리에 쌓인 것을 가까운 빈 자리로 옮긴다. 마을(눈사람, 울타리, 돌담, 팻말, 징검돌)은 그대로 두고 나머지가 비켜 선다. 나무끼리는 줄기만 떨어지면 된다(잎은 겹쳐도 됨). Plants(풀, 꽃) 아래 콜라이더는 장애물로 보지 않는다. 눈 섬 만들기(SnowIslandSetup.Build) 끝에서도 자동으로 부른다.
- **병 꾸미기 시점 버그**: BottleViewer.ResetOrientation이 카메라 오른쪽 축으로 +tilt를 돌려서 병 위쪽이 카메라에서 멀어졌다(주석은 반대로 적혀 있었음). 흙 윗면이 등을 돌려 옆면만 보이고, 흙 위 재료의 아랫부분이 앞쪽 흙에 가렸다. -tilt로 고쳤다.
- **재료를 놓는 높이**: BottleContents.CreateVisual의 "바닥이 표면에 닿게 올리기"가 세계 좌표 상자를 썼다. 병이 기울면 기운 모델의 상자가 더 커서 엉뚱하게 떴다. 이제 재료 자신의 좌표(LocalBounds)로 잰다. 클릭 상자도 같은 방식.
- **흙 윗면 판판하게**: 흙 모양(메시) 윗면이 약 ±5mm 울퉁불퉁하고 가장자리가 1cm쯤 솟아 있었다. SoilLook.Flat이 재료를 놓는 높이(surfaceY)로 편다. 흙의 결은 법선 그림이 맡는다.

## 걷기와 행동의 손맛 (2026-11-20)
- FirstPersonController.LateUpdate: 걸음마다 카메라가 위아래로 출렁이고(걸음 거리에 비례), 옆으로 걸으면 살짝 기울고, 서 있으면 숨쉬듯 오르내리고, 착지하면 용수철처럼 내려앉았다 돌아오고, 달리면 시야(FOV)가 +7도 넓어진다. FirstPersonController.Nod(m)로 다른 곳에서도 시선을 끄덕일 수 있다. (발소리는 원래 FootstepPlayer가 낸다)
- HitFx(Assets/Scripts/Core/HitFx.cs): 빛무리 그림(FireflyBug.GlowSprite)을 여러 개 뿌리는 효과. Sparkle은 재료를 주울 때(Pickup), Chips는 나무, 돌, 풀을 칠 때(Harvestable.Impact, 마지막 타격은 더 세게). 입자 시스템이나 재질 에셋이 필요 없다.

## 집은 섬 씬 안의 방, 문은 직접 연다 (2026-11-20)
- **집 안은 더 이상 별도 씬(HutInterior)이 아니다.** 섬(Island) 씬의 집(Cabin = Building_b, 속이 빈 껍데기) 속에 방(`HouseInterior`)을 짓는다. 옛 HutInterior 씬과 빌드 목록 항목은 지웠다 (필요하면 git 기록에서 복구).
- **열리는 문**: HouseDoorBuilder(메뉴: 집 문 만들기)가 Building_b 메시를 꼭짓점 번호로 이어진 덩어리로 나눠, 문 자리 상자 안에 통째로 들어가는 덩어리(널빤지, 쇠 띠, 손잡이)를 문짝으로 뗀다. 문틀과 벽은 남는다. 문짝은 `HouseDoor`(오른쪽 돌쩌귀를 축으로 안쪽 100도, 0.7초)가 되고, 바라보고 E로 열고 E로 닫는다. 열려 있는 동안 문짝 충돌은 꺼지고 바라보는 상자(DoorTrigger)는 켜 둔다. 문 한가운데 서 있으면 닫히지 않는다. 삐걱 소리는 GameAudio.Creak(Sfx.DoorOpen, DoorClose).
- **방**: 바깥 집(Building_b)은 직사각형이 아니라 T자다: 문이 있는 가운데가 앞으로 튀어나온 현관 앞방(안쪽 폭 2.8m, 깊이 3.2m)과 그 뒤 몸채(큰 방, 10.9m x 5.9m). InteriorSetup(메뉴: 집 안 꾸미기)이 그 모양대로 방 두 개를 짓는다. (처음에 직사각형으로 지어서 벽이 집 밖으로 나오고 집 앞 벽의 창틀과 상자 조각이 방에 남았던 것을 고쳤다) 뿌리 = 큰 방 바닥 한가운데, 높이는 문턱(집 밑동 + 0.43). 현관 앞방 남쪽 안벽은 문 두께 바로 뒤. 작업대(Workbench, 병 포함)는 섬에 있는 것을 큰 방 북쪽 벽 앞에 옮겨 놓고 상판을 꾸몄다. 모든 좌표는 뿌리 기준이다. 집 안쪽 치수는 껍데기 메시의 평면(벽)을 재서 구했다: 몸채 x -5.91~5.72, z 17.16~23.63, 현관 앞방 x -1.61~1.61, z 14.02~17.2 (세계 좌표).
- **집 안 판정**: IndoorZone(방 뿌리에 붙음, 상자 두 개: 큰 방과 현관 앞방). 플레이어가 상자 안이면 `IndoorZone.Inside`. 비와 눈이 숨고(WeatherEffects.Outdoors), 방 소리가 나고(GameAudio), 발소리가 나무 바닥이 되고(FootstepPlayer), 환경광이 따뜻하게 어두워진다(들어가고 나올 때 부드럽게).
- 눈 섬과 사막 섬을 만들 때는 이 방(HouseInterior)과 문(HouseDoor)을 지운다. 섬을 처음부터 다시 만들면(IslandSetup) 집 문 만들기, 집 안 꾸미기를 차례로 다시 눌러야 한다.

## 식물 크기 맞추기 (2026-11-20)
- 에셋 팩의 풀, 꽃, 버섯이 사람(1.8m)보다 크게 들어와 있었다 (버섯 평균 1.5~2.0m, 큰 풀과 꽃 평균 1.9m, 데이지 잎 너비 4m 이상). SizeBalancer(메뉴: 유리병 정원 > 모든 섬 식물 크기 맞추기)가 종류(이름에서 끝 번호를 뗀 것)마다 목표 평균 높이(버섯 0.45m, 큰 풀과 꽃 0.8m, 작은 풀 0.3~0.55m, 사막 알로에 0.5m와 마른 가지 0.65m)에 맞춰 같은 배율을 곱한다. 땅에 닿은 높이는 지킨다. 목표 기준이라 여러 번 눌러도 같은 결과다. 섬을 다시 만들면 다시 눌러야 한다.
- 크기를 바꾸지 않은 것: 줍는 재료(0.1~0.4m), 나무(4~13m), 바위(1~3m), 눈 섬 마을 물건(울타리 1.2m, 눈사람 1.8m)은 어울려서 그대로 뒀다.

## 가방 칸 늘리기와 스크롤 (2026-11-20)
- 가방 칸 수 32 -> 96 (한 칸에 같은 재료 5개까지는 그대로). UISetup.SlotCount, Bag.slotCount.
- 칸 수에 묶인 화면 세 곳을 스크롤 영역(UISetup.MakeScroll: ScrollRect + RectMask2D + 오른쪽 스크롤 막대)으로 바꿨다: 가방 창(Tab, 8열, 약 다섯 줄이 보임), 병 꾸미기 아래 재료 칸(16열, 두 줄이 보임), 우체통 판매 화면(8열, 네 줄이 보임). 마우스 휠로 내려 본다. 가방의 병 탭은 칸 수 32(BottleCellCount)에 쪽 넘기기라 그대로다.
- UI 바꾼 뒤에는 메뉴 유리병 정원 > UI 만들기를 눌러 GameRoot 프리팹을 다시 만든다.

## 병 주문 편지 늘리기 (2026-11-20)
- 편지 20통 -> 62통. 사막, 눈 섬, 날씨(번개 유리, 무지개 조약돌, 바람 깃털, 안개), 물고기, 동물, 바닥재, 재료 종류 수 같은 다양한 조건. 편지 번호는 저장에 쓰이니 앞의 순서를 바꾸지 말고 뒤에만 덧붙인다.
- 최근 온 12통은 다시 오지 않는다 (BottleRequests.Roll).
- 6일(GameState.RequestExpireDays) 동안 못 들어준 편지는 다른 편지로 바뀐다. 구하기 어려운 재료가 필요한 편지에 막혀 새 편지가 영영 안 오던 문제를 고쳤다. SaveData.requestAge에 저장한다.

## 풀과 꽃의 막는 충돌 없애기 (2026-11-20)
- 풀, 꽃, 버섯, 알로에 815개가 막는 메시 콜라이더를 3개씩(약 3260개) 달고 있어서 풀밭을 걸으면 자꾸 걸렸고 물리 계산도 무거웠다. SizeBalancer(식물 크기 맞추기)가 막는 충돌을 없애고, 캘 수 있는 것에는 바라볼 때 잡히는 트리거 상자만 남긴다. Interactor는 트리거도 맞추므로 캐기 안내는 그대로 뜬다. 나무와 바위는 그대로 막는다.

## 바다로 걸어 나가지 못하게 (2026-11-20)
- 바다로 걸어 들어가면 수심이 깊어져도(바다 밑 -7m) 한없이 걸어서 섬 밖으로 사라질 수 있었다. FirstPersonController.LimitSea: 지형이 -0.9m(바다 높이 0 기준 가슴 깊이)보다 깊어지는 쪽으로는 걸어 들어가지 못하고, 벽처럼 미끄러진다. 선착장, 얼음, 배처럼 지형이 아닌 곳 위에서는 제한하지 않는다. 이미 깊은 곳에 있으면 얕은 쪽으로는 갈 수 있다.

## NPC 대화 (2026-11-20)
- 미쿠는 "안녕 나는 미쿠야" 한 줄만 반복했다. NpcDialogue(Assets/Scripts/Player)가 날씨, 시간(밤, 아침), 우체통과 편지, 병 개수, 선착장 진행 상황에 맞는 대사를 섞어 말하고, 최근 6개는 다시 하지 않는다. 미쿠는 밝은 반말로 말한다. 치비는 말하지 않는다(말 거는 기능 없음, 사용자 요청).
- 대사를 늘릴 때는 NpcDialogue.MikuLines에 contextual(상황 맞음, 더 자주)과 generic(아무 때나)로 덧붙인다. 글꼴에 없는 글자(· →)는 쓰지 않는다.

## 저장 파일 안전하게 (2026-11-20)
- 저장 파일을 바로 덮어써서 쓰는 도중 꺼지면 깨질 수 있었다. 이제 임시 파일(save.json.tmp)에 다 쓰고, 이전 저장을 save.json.bak으로 남긴 뒤 바꾼다.
- 저장 파일이 깨져 못 읽으면 바로 전 저장(.bak)으로 이어 간다. 둘 다 못 읽으면 깨진 파일을 지우지 않고 save.json.broken으로 옮겨 두고 새로 시작한다. (예전에는 읽지 못한 파일이 있으면 영영 저장이 안 됐다.)

## 화면 중복 열기 방지 (2026-11-20)
- 지도(MapPanel), 가방(InventoryPanel), 일시정지(PauseMenu)의 Open에 "이미 열려 있으면 다시 열지 않는다" 가드를 넣었다. 두 번 열리면 UIFocus 카운트가 올라가서 닫은 뒤에도 조작이 잠긴 채(커서가 안 잠김) 남을 수 있었다. 키 조작에서는 막혀 있었지만 다른 곳에서 부르면 생길 수 있는 문제였다.
- 시험 결과: 모든 화면을 두 번 열고 두 번 닫아도 UIFocus가 0으로 돌아온다. 섬 사이 이동(눈 섬, 사막 섬 왕복 3번)은 GameRoot 1개, 소리 듣는 귀 1개, 이벤트 시스템 1개로 안정적이다. 실제 게임 파일(Windows 빌드, 305MB)은 오류 0으로 만들어지고 메인 화면을 25초 돌려도 오류가 없다.

- **눈 발소리 에셋 삭제**: 라이선스를 확인할 수 없어 Assets/Footstep(Snow and Grass)를 지웠다. FootstepSet.snow는 비어 있고 FootstepPlayer가 눈/얼음 위에서 흙 소리를 쓴다. FootstepSetup도 눈 소리 연결을 뺐다. (문서 245줄 근처의 눈 발소리 설명은 옛 내용). 눈 에셋 Cozy Snowy Environment와 Footsteps Volume 02는 무료 + 표준 에셋 스토어 약관으로 확인함.

- **눈 발소리(새로 만듦)**: 사신 Footsteps Volume 02의 Footstep_Sand_*를 0.9배속으로 늘이고 7kHz 저역 통과로 둔하게 가공해 Assets/Audio/SnowSteps/Snow_01~08.wav로 두고 FootstepSetup(발소리 연결)이 FootstepSet.snow에 연결한다. 도구는 EarShot(MIT, 설치 위치 C:\Users\ih551\tools\earshot, 게임 폴더 밖). EarShot은 눈 소리를 구별하는 어휘가 없어 '눈'을 확인해 주지 못한다(실제 모래 발소리도 weak). 코드로 합성한 소리는 스네어/하이햇으로 들린다고 나와 버렸다. **직접 귀로 확인 필요**. 크레딧: The Sound Guild(Footsteps Sounds Vol.02), EarShot(MIT).

- **효과음 교체(EarShot 점검 결과)**: 게임 안에서 코드로 만든 효과음을 뽑아 EarShot rank로 보니 파도만 '그럴듯'했고 종/동전/문/새/귀뚜라미/발소리는 의도한 소리로 인식되지 않았다(EarShot도 완벽하지 않아 참고용). 소리 모음에서 후보를 만들어 비교해 점수가 높은 것으로 Assets/Resources/Sfx/에 두었다: Chime=fit/bell/small_bell, Coin=pro_notify/ding_2296, Click=click/tink, Pickup=pop/0, Place=fit/land.light/finger_wood, Paper=swipe/paper (최대 음량 0.7로 맞춤). GameAudio.Awake가 이 파일이 있으면 코드 소리 대신 쓰고 없으면 코드 소리로 돌아간다. 문 소리, Remove, 새, 귀뚜라미, 바람, 비, 방 소리는 아직 코드 소리 그대로(EarShot 소리 모음에는 문 삐걱, 새, 귀뚜라미가 없다). **귀로 확인 필요**. 크레딧: EarShot(MIT, Leon Lin).

- **소리 2차 교체**: EarShot rank 결과로 고름. 도구로 칠 때 소리 ToolSound는 Footstep_Deep_01~03(사신 팩의 둔한 발소리, 후보 중 '둔한 쿵' 점수 최고)을 Resources/Sfx/ToolHit1~3.wav로 두고 무작위로 낸다(없으면 코드 소리). 바람 WindLoop.wav는 날씨 에셋(DynamicWeatherSystem) wind_loop를 32kHz 모노로 줄인 것(점수가 코드 바람보다 높음). Remove=pop/2, DoorOpen=click/thock(걸쇠 소리), DoorClose=knock/wood(나무 쿵)로 교체. 빗소리는 원래 날씨 에셋(AudioModule)이 내서 GameAudio의 비 소리는 꺼져 있다. **그대로 둔 것**: 새, 귀뚜라미, 방 소리(후보가 없거나 EarShot이 맞다고 보지 않음), 문 삐걱 소리(후보 없음). 귀로 확인 필요.

- **잠자기 연출 (마인크레프트식)**: 침대 E → SleepSequence(Assets/Scripts/Core, 화면은 코드로 만들고 씬을 넘어 남김). 1.6초 동안 어두워지며 부드러운 종소리 → 검은 화면에서 '쿨쿨...'(2.6초, 중간에 GameState.Sleep()과 저장) → 아침 종소리와 함께 2.4초 동안 밝아지고 가운데에 'N일째 아침 / 오늘은 (날씨)'가 떴다 사라짐 → 우체통 도착 알림. 연출 중에는 UIFocus로 조작이 멈추고, 중간에 오류가 나도 finally에서 화면과 조작을 되돌린다. Bed는 이제 SleepSequence.Begin만 부른다. Play에서 3단계(쿨쿨 화면, 아침 글자 밝아짐)를 캡처로 확인함. 소리(종소리)는 귀로 확인 필요. **귀뚜라미 소리는 삭제함** (GameAudio에서 소스와 CricketData 제거).

- **출시 준비 4가지 (2026-11-20)**: (1) PerformanceDefaults: 켜질 때 수직 동기화 1(품질 설정이 0이어도). (2) CreditsPanel: 크레딧 화면(코드로 만듦, 스크롤). 메인 화면 왼쪽 아래 작은 '크레딧' 버튼과 일시정지 메뉴 맨 아래 버튼으로 연다. 글자는 CreditsPanel.Text에서 고친다. **'만든 사람' 자리에 제작자 이름을 아직 안 적었다.** (3) 에셋 라이선스 확인(스토어 페이지로): Cozy Snowy(HobiSoLoved, 무료, EULA), Low Poly Nature Pack(BD Studios, 무료), Low Poly Tree Mega Pack - 1000 Trees(MysticForge, 무료), Medieval Town Exteriors(Lylek, 무료), AllSky Free(rpgwhitelock, 무료), URP Dynamic Weather System(CreaTuMundo, 무료), Footsteps Vol.02(The Sound Guild, 무료) 모두 표준 에셋 스토어 약관. Poly Desert(Runemark)는 itch.io에서 CC0, 스토어 목록의 판매자는 확인 못 함. Dreamcore Music(juanjo_sound)은 GameDev Market 무료 팩이고 '상업 사용, 무제한 프로젝트, 로열티 무료'이며 크레딧 요청이 있다. Cafe24 동동 글꼴은 상업 이용 가능으로 알려졌으나 게임 임베딩 문구는 공식 확인 못 함. (4) Bed: TimeOfDay 0.6 이후(Bed.SleepFrom)에만 잘 수 있다. 낮에는 안내 '아직 졸리지 않아요'. 숫자를 바꾸면 시간이 달라진다.

- **게임 이름: Mini Terra** (2026-11-20 확정). 제품 이름(PlayerSettings.productName) Mini Terra, 메인 화면 제목 Title 글자 Mini Terra. 한글 부제는 아직 없음(후보: 초미니 테라리움 꾸미기). **저장 위치가 바뀌었다**: 제품 이름이 폴더 이름이라 세이브는 %USERPROFILE%/AppData/LocalLow/DefaultCompany/Mini Terra/ 이다. 이전 폴더 .../terrarium11/의 save* 파일을 복사해 옮겼다(옛 폴더는 그대로 남아 있음). 소리 크기, 감도 같은 PlayerPrefs(레지스트리)는 제품 이름에 묶여 있어서 처음 상태로 돌아간다. 회사 이름은 아직 DefaultCompany. 에디터 메뉴 이름 '유리병 정원/...'은 개발 도구라 그대로 둠. 크레딧 화면 제목 등에는 아직 게임 이름이 안 들어 있음.

- **회사 이름과 부제 확정 (2026-11-20)**: 회사 이름(PlayerSettings.companyName) CheeseBugi(스토브 스튜디오 이름과 같게). 한글 부제 미니 테라리움 꾸미기(메인 화면 Subtitle 글자, 크레딧 맨 위). 크레딧의 '기획, 제작'은 CheeseBugi. **저장 위치가 또 바뀜**: %USERPROFILE%/AppData/LocalLow/CheeseBugi/Mini Terra/ (이전 폴더 DefaultCompany/Mini Terra, DefaultCompany/terrarium11은 남아 있음, 내용 같음 확인함). PlayerPrefs(소리 크기, 감도)는 다시 초기값.

- **미쿠 → 햄부기 (2026-11-20)**: 화면에 보이는 이름만 바꿨다. NPC 이름·첫 대사·NpcDialogue 대사('안녕 나는 햄부기야'), 꾸밈 재료 표시 이름(miku.asset의 displayName), 크레딧, 에디터 메뉴(유리병 정원 > 햄부기 만들기)와 안내 글. **내부 이름은 그대로**: 재료 id miku(저장 파일이 쓴다), MIKU.fbx, Miku.prefab, MikuNPC, MikuSetup, NpcPersona.Miku. Island.unity의 NPC는 프리팹 값을 따라가서 씬은 다시 저장하지 않았다(다시 저장하면 8000줄이 바뀐다).

- **이끼 바닥재 moss_ground (2026-11-20)**: 바닥재 5번째(흙, 모래, 눈, 붉은 모래, 이끼 바닥). **우체통에서 8코인에 산다**(zone=Shop, cquire=Shop, 되팔기 3). 그림은 SoilLook.BuildMoss(짙고 옅은 초록 덩어리, 이끼 방울, 새순 점)로 코드가 만든다. 재료 데이터는 BottleSetup.EnsureMossGroundAsset, 값은 MaterialSetup.Build 끝에서 정한다. GroundSelector에 '이끼 바닥 x0' 버튼(UISetup, 바닥재 판 높이 450)이 생겼다. 순서: 재료 데이터 만들기 → 재료 아이콘 만들기 → 재료 목록 만들기 → UI 만들기(끝나면 아이콘 중 바뀐 다른 png는 git으로 되돌릴 것, 모든 아이콘이 다시 구워져서 4개가 달라진다). Play에서 병에 깔고 흙으로 되돌리면 가방 개수가 맞게 오르내리는 것을 확인함. 이미 있는 장식 이끼 moss, moss_dark와는 다른 재료다.

- **새 꾸밈 재료 25종 (2026-11-20)**: 재료 126종 → 151종. 발견: Assets/Models/Deco에 재료로 연결 안 된 모델이 이미 있었다(연못, 풍차, 가로등, 그루터기, 불가사리, 산호, 다리, 나비 2, 소라, 집, 무당벌레, 달팽이, 검은·흰 조약돌). 그것을 먼저 쓰고, 없는 10종만 ExtraPropModels.cs(PropModelMaker.BuildMore, 메뉴 새 꾸밈 소품 모델 만들기, 파일이 있으면 덮어쓰지 않음)로 만들었다: 새집, 허수아비, 그네, 미니 우체통, 해변 파라솔, 오아시스, 장작더미, 정원 요정, 통나무, 터키석. **얻는 곳**: 우체통 구매 13종(풍차 32, 연못 22, 허수아비 22, 작은 집 24, 새집 16, 그네 20, 미니 우체통 12, 가로등 18, 해변 파라솔 16, 오아시스 26, 장작더미 10, 정원 요정 18, 나무 다리 18) / 숲: 그루터기, 통나무(도끼로 나무를, 솔방울·나뭇가지와 같이 나옴), 무당벌레(줍기), 달팽이(비 온 뒤 줍기) / 바닷가: 불가사리, 산호, 소라(줍기), 검은 조약돌, 흰 조약돌(곡괭이) / 꽃 언덕: 푸른 나비, 분홍 나비(줍기) / 사막: 터키석(바위를 곡괭이로). 가로등과 작은 집은 밤에 빛난다(NightGlow). 섬에는 줍는 재료 42곳(7종 x 6)을 IslandSetup.AddMissingPickups로 놓았다(Island.unity가 6500줄 늘어남). 순서: 새 꾸밈 소품 모델 만들기 → 장식 모델 가져오기 설정 → 재료 데이터 만들기 → 도구 설정 만들기 → 재료 아이콘 만들기 → 재료 목록 만들기 → UI 만들기(우체통 구매 목록 orderables가 거기서 구워진다. 안 하면 새 상점 재료가 안 보인다) → 섬에 새 재료 놓기. Play에서 확인: 25종 모두 아이콘·모델 있음, 도끼 숲에서 그루터기·통나무가 25%씩 나옴, 사막 곡괭이에서 터키석, 우체통 구매 61종(3쪽), 구매 주문 동작. **얻는 곳 검토 결과**: 대체로 어울림. 어색한 것은 숲에서 괭이로 캐는 도토리와 나뭇잎·버섯(바닥에 떨어진 것이라 줍는 쪽이 어울림, 바꾸려면 섬 장면에 줍는 재료를 놓아야 해서 그대로 둠), 사막 소품(낙타, 천막, 피라미드)이 전부 구매 전용이라 사막 섬에서 얻는 것이 터키석뿐이라는 점.
