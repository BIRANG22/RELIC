# ExplorationResultPanel 캐릭터 경험치 영역 및 프레임 확장 설계

## 의도

`ExplorationReportRow_0~2`에서 레벨 텍스트와 경험치 바를 기존 캐릭터 정보와 비슷한 가로폭으로 정돈하고, 여유 공간을 둔 사각 라인 프레임 안에 모두 포함한다.

## 현재 상태

- 캐릭터 정보의 기본 가로폭은 250이다.
- 경험치 바는 폭 250이지만 레벨 텍스트가 바 왼쪽으로 크게 돌출되어 있다.
- 현재 프레임은 Row보다 사방 10px만 커서 경험치 영역의 하단과 돌출된 레벨 텍스트를 감싸지 못한다.
- Row 중심 간 간격은 325이므로 프레임 폭을 과도하게 늘리면 인접 캐릭터 프레임과 겹친다.

## 선택한 설계

- 사용자가 Unity에서 직접 조정한 `ExplorationReportExpSlider_2`를 기준 레이아웃으로 사용한다.
- `_2`의 Fill 축소·이동 값과 `LevelUp` 위치를 `_0`, `_1`에 동일하게 복제한다.
- 레벨 텍스트를 Slider 왼쪽 음수 좌표에 두지 않고 `_2`와 동일한 양수 x 위치에 둔다.
- 경험치 수치 텍스트도 축소된 바 안에 유지한다.
- 각 `ExplorationReportRow_0~2` 자체의 최종 크기는 310x460으로 확장한다.
- Row를 아래로 20px 이동하고 기존 자식의 RectTransform을 상쇄 보정해 화면상 위치는 유지한다. 경험치 관련 RectTransform은 새 Row 내부 하단에 배치한다.
- `ExplorationReportRowFrame_0~2`는 확장된 Row 전체에 맞춰 Stretch한다.
- 결과적으로 세 캐릭터의 경험치 Fill 길이와 레벨 위치가 사용자가 조정한 `_2`와 동일해진다.
- 프레임 간에는 약 15px 간격이 남는다.

## 정확한 RectTransform 값

- `ExplorationReportExpSlider_0~2`: `anchoredPosition=(0,0)`, `sizeDelta=(0,32)`
- 각 `Fill`: `localScale=(0.72196245,1,1)`, `anchoredPosition=(31,0)`
- 각 `LevelUp`: `anchoredPosition=(37,-12)`, `sizeDelta=(60,30)`
- 각 `ExperienceText`: `anchoredPosition=(100,-12)`, `sizeDelta=(30,24)`
- `ExplorationReportRow_0~2`: 기존 x 유지, `anchoredPosition.y=-20`, `sizeDelta=(310,460)`
- `ExplorationReportRowFrame_0~2`: Stretch, `anchoredPosition=(0,0)`, `sizeDelta=(0,0)`

세 슬라이더의 내부 레이아웃은 `ExplorationReportExpSlider_2`의 저장값을 기준으로 동일하게 유지한다.

## 범위

- 결과 화면의 고정 UI 배치만 변경한다.
- 레벨/경험치 계산이나 표시 데이터는 변경하지 않는다.
- 실제 프레임 Sprite 연결은 포함하지 않는다.
