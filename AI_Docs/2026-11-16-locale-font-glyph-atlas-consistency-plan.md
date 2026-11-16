# 언어 폰트 글리프·Atlas 일관성 구현 계획

1. `탐사진행`, DB별 Multi Atlas, 번체 폰트, material 일관성에 대한 EditMode 회귀 테스트를 추가한다.
2. `LocaleTMPFontGuard`에 폰트와 material을 안전하게 함께 적용하는 공용 함수를 추가한다.
3. DB 폰트를 직접 대입하는 런타임 경로를 공용 함수로 교체한다.
4. DB용 동적 TMP Font Asset의 Multi Atlas를 활성화한다.
5. 번체 DB 폰트를 전체 문자열을 지원하는 `ZhiyiSans-Bold SDF`로 변경한다.
6. 코드 컴파일, 정적 데이터 검사, diff 검사를 수행한다.

Unity Editor 직접 제어 및 batchmode 테스트는 수행하지 않는다. Unity Test Runner 실행이 필요한 항목은 미검증으로 별도 보고한다.
