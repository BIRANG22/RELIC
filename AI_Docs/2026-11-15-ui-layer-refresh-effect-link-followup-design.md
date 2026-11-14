# UI 레이어·공명 새로고침·몬스터 고유 효과 후속 수정 설계

## 조사 결과

- 메뉴와 툴팁이 같은 Sorting Layer에서 `sortingOrder` 최댓값을 경쟁해 생성 순서에 따라 툴팁이 메뉴보다 앞에 나온다.
- 메뉴 블러 수집기는 일반 전투 패널과 설정 버튼만 수집하고, `TutorialMode`와 독립 튜토리얼 오버레이 루트는 수집하지 않는다.
- 튜토리얼 포커스는 이름이 `TooltipPanel` 또는 `TooltipUI`인 오브젝트만 전면 처리하여 실제 효과 툴팁 컴포넌트를 누락한다.
- 포커스가 시작될 때 한 번 전면 처리한 툴팁은 이후 툴팁 자체 정렬 코드가 값을 바꿔도 다시 강제되지 않는다.
- 공명 새로고침 버튼은 상태 갱신 중 Button의 `targetGraphic`까지 원본 흰색으로 복원하며, 클릭 후 Selectable의 `SelectedColor`가 다시 적용된다.
- 몬스터 고유 효과 설명은 `IsMonsterInnateTrait` 플래그가 전달된 경우에만 포매터를 거쳐 실제 몬스터 정보 패널 경로에서 누락될 수 있다.

## 권장 설계

1. 프로젝트 설정에 새 Sorting Layer를 추가하지 않는다. 메뉴가 열려 있는 동안 `Canvas.preWillRenderCanvases`에서 현재 최상위 Sorting Layer와 최대 Order를 MenuPanel에 적용하고, MenuRoot는 바로 아래에 둔다.
2. 메뉴 블러 대상에 활성 `TutorialMode`, `__TutorialFocusTooltipForeground`, `__RelicTutorialDialogueOverlay` 루트를 명시적으로 포함한다.
3. 튜토리얼 포커스 툴팁도 최종 렌더 직전에 암막과 같은 레이어의 더 높은 순서로 매 프레임 재적용하여 다른 UI 코드의 후속 정렬 변경을 막는다.
4. 새로고침 밝기 갱신에서 Button `targetGraphic`을 제외하고, `SelectedColor`를 `NormalColor`로 정규화한다. 클릭 종료 후 `EventSystem` 선택·Deselect 상태와 Target Graphic의 휴지 색상도 복원한다.
5. 상태효과 설명 전용 포매터를 몬스터 패널의 실제 `MonsterInfoStatusItemUI.Detail`과 상태 아이콘 툴팁 출력 경로에 플래그와 무관하게 적용한다. EffectDatabase와 GridEffectDatabase의 표시명을 찾아 상태효과 참조에는 링크와 밑줄, 그리드 효과 참조에는 밑줄을 적용한다.

## 검증 계획

- EditMode 테스트로 메뉴 레이어 적용/복원, 메뉴 블러용 튜토리얼 루트 식별, 튜토리얼 툴팁 식별, 새로고침 Target Graphic 제외 및 Selected 색 정규화, 플래그 없는 상태효과·그리드 효과 명칭 포매팅을 검증한다.
- Assembly-CSharp 및 Assembly-CSharp-Editor를 컴파일한다.
- Unity 에디터 화면에서의 실제 겹침·호버 색상은 직접 제어하지 않고 미검증 항목으로 남긴다.

## 멀티플레이 경계

UI 표시와 텍스트 포매팅만 변경한다. 전투 Command, 상태 변경, 결과 이벤트 및 랜덤 처리에는 영향이 없다.
