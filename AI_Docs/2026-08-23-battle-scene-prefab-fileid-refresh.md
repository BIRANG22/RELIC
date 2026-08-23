# Battle Scene Prefab FileID Refresh

## 조사 결과

로비에서 배틀로 진입할 때 표시되는 `Assertion failed on expression: 'fidA != fidB'`는 `Battle.unity` 로드 중 `BattleSceneController.Start()` 이전에 발생한다. 전투 진입 서비스나 버튼 코드의 런타임 예외가 아니라 Unity 씬/프리팹 직렬화 단계의 fileID 충돌 계열 Assertion으로 판단한다.

의심 대상은 `Battle.unity` 하단의 prefab instance다.

- `Assets/Project/PrefabsR/MenuPanel.prefab`
- `Assets/Project/PrefabsR/RestRoom/ShopPanel.prefab`

특히 `ShopPanel` 인스턴스는 두 개가 배치되어 있었다. 하나는 씬 로컬 stripped object ID가 `9103000000000000001` 범위로 고정되어 있고, 다른 컴포넌트가 이 stripped GameObject를 참조한다. 다른 하나는 더 직접적으로 prefab instance와 stripped object의 씬 로컬 ID가 원본 prefab 내부 fileID인 `1273546924`, `1273546925`, `1273546928`과 같아서 `fidA != fidB` 충돌 조건에 부합한다.

## 권장 설계

Unity 에디터에서 해당 프리팹을 새 인스턴스로 교체했을 때와 같은 효과를 최소 변경으로 만든다.

- 의심 prefab instance의 씬 로컬 fileID를 새 고유 범위로 재배정한다.
- stripped object를 참조하는 기존 필드는 새 fileID로 함께 갱신한다.
- 프리팹 원본 GUID와 override 값은 유지하여 UI 배치와 기능 변경을 피한다.
- `m_Modifications.target.fileID`는 원본 prefab 내부 ID를 가리켜야 하므로 변경하지 않는다.

## 검증 계획

- `Battle.unity` YAML anchor 중복 여부를 확인한다.
- 의심 구간에 기존 `910300000000000000*`, `7850000000000000002` ID가 남지 않았는지 확인한다.
- Unity 에디터에서 로비에서 배틀로 진입해 Assertion 재발 여부를 확인한다.
