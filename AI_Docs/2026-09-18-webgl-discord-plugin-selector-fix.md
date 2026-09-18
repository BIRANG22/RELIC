# WebGL Discord 플러그인 선택기 수정

## 원인

`DiscordPluginSelector`는 모든 플랫폼의 빌드 전·후처리에서 Debug/Release 네이티브 플러그인 상태를 바꾸고 `.meta`를 `SaveAndReimport()`한다. WebGL은 Discord 네이티브 DLL을 사용하지 않지만, 빌드 후처리에서 Windows DLL 메타 파일을 다시 저장하려다 실패하여 Player 생성 완료 후 빌드 전체가 Failed가 되었다.

## 수정

WebGL 빌드일 때 `OnPreprocessBuild`와 `OnPostprocessBuild`를 즉시 반환한다. WebGL 대상 DLL은 원래 지원 대상이 아니므로 플러그인 상태 전환이 필요 없다.

## 범위와 안전성

- Windows, Linux, macOS, Android, iOS의 기존 Debug/Release 선택 동작은 유지한다.
- Discord 런타임·게임플레이·멀티플레이 로직을 변경하지 않는다.
- 패키지의 로컬 임베디드 소스만 수정한다.

## 검증

- WebGL 가드가 두 빌드 콜백 모두에 존재하는지 EditMode 회귀 테스트로 확인한다.
- Unity Editor에서 WebGL Build & Run을 다시 실행해 Discord DLL 메타 저장 오류가 재발하지 않는지 확인한다.
