# Discord 플러그인 빌드 후처리 오류 수정

## 원인

`DiscordPluginSelector.OnPostprocessBuild`가 빌드 후처리 콜백이 끝나기 전에 Debug 플러그인 설정을 복구하면서 `PluginImporter.SaveAndReimport()`를 즉시 호출한다. 이 시점에는 Unity 빌드 파이프라인이 해당 플러그인 메타 파일을 사용 중일 수 있어 `Cannot open ...meta for write`와 `Failed to write meta file`이 발생한다.

## 수정

- 빌드 산출물의 Krisp 모델 복사는 기존 시점에 유지한다.
- 에디터용 Debug 플러그인 설정 복구만 `EditorApplication.delayCall`로 지연한다.
- 여러 후처리 호출이 겹쳐도 복구 콜백은 한 번만 예약한다.
- 다음 에디터 로드 시의 초기화 경로는 유지한다.

## 검증

- Discord Editor 어셈블리 컴파일.
- 후처리 경로에 즉시 `SetPluginConfig(true)` 호출이 남아 있지 않은지 코드 검토.
- 실제 Player Build는 열린 Unity 에디터에서 사용자가 빌드 대상과 출력 경로를 선택해야 하므로 별도 확인한다.
