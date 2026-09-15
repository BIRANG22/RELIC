# 동적 메뉴·확인창 텍스트 로컬라이제이션 정리

## 원인

`MenuPanel/quit_Text`와 `Check/Slogan`은 상황에 따라 런타임 코드가 내용을 작성하는 동적 텍스트다. 그러나 두 프리팹에는 `LocalizedTMPText`도 함께 연결되어 있어, 활성화 시 정적 로컬라이즈 값과 런타임 값이 같은 TMP 텍스트를 순서에 따라 덮어썼다. 그 결과 한 프레임 동안 이전 또는 고정 문구가 보였다.

## 결정

두 텍스트에 `LocalizationIgnore`를 지정하고 정적 `LocalizedTMPText` 컴포넌트를 제거한다. 각 화면의 기존 런타임 소유자는 유지한다.

- `UIManager`: 로비/전투 상태에 따른 `quit_Text` 값.
- `BootstrapConfirmDialogUI`: 확인창의 상황별 `Slogan` 메시지와 버튼 값.

## 회귀 방지

EditMode 테스트에서 두 프리팹의 해당 텍스트가 `LocalizationIgnore`를 보유하며 `LocalizedTMPText`를 보유하지 않음을 검증한다.
