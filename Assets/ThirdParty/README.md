# ThirdParty (남이 만든 에셋)

직접 만든 것이 아닌 에셋을 이 폴더에 모아 둔다. 우리가 만든 파일(Scripts, Editor, Data, Scenes 등)과 섞지 않는다.

| 폴더 | 내용 | 쓰는 곳 |
| --- | --- | --- |
| `LowPolyNaturePack` | 에셋 스토어의 자연 에셋(오두막 키트, 나무, 풀, 바위, 구름 등) | 섬과 집 안 (`IslandSetup`, `CabinBuilder`, `InteriorSetup`) |
| `Donthrukat` | 에셋 스토어의 근접 무기 모음 | 아직 안 씀 (도끼, 곡괭이를 만들 때) |

- 에셋 스토어 에셋은 라이선스상 공개 저장소에 그대로 올리면 안 될 수 있다. 저장소를 공개하기 전에 확인한다.
- 팩의 재질은 기존(Built-in) 셰이더라서, 처음 가져온 뒤 메뉴 `유리병 정원 > 에셋 팩 재질을 URP로 바꾸기`를 한 번 눌러야 한다.
- 이 폴더의 경로를 바꾸면 `IslandSetup`, `CabinBuilder`, `InteriorSetup`, `PackMaterialSetup`의 경로 상수도 같이 고친다.
