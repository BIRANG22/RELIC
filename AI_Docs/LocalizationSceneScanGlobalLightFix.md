# 로컬리제이션 씬 검사 Global Light 충돌 수정

## 원인

로컬리제이션 전체검사는 정적 TMP의 실제 텍스트와 연결 키를 교차검증하기 위해 미로드 씬을 Additive 모드로 열었다. 현재 씬과 검사 대상 씬이 각각 동일한 Sorting Layer 및 Blend Style의 Global Light 2D를 포함하면, 검사 중 두 조명이 동시에 활성화되어 URP 2D Renderer가 중복 Global Light 오류를 출력했다.

각 씬에는 Global Light가 하나씩만 존재하므로 씬 데이터의 중복 문제가 아니라 에디터 검사 과정의 씬 격리 문제다.

## 수정 설계

- 이미 로드된 씬은 현재 씬 인스턴스를 그대로 검사한다.
- 로드되지 않은 씬은 `EditorSceneManager.OpenPreviewScene`으로 격리해서 연다.
- 검사 완료 또는 예외 발생 시 `EditorSceneManager.ClosePreviewScene`으로 반드시 닫는다.
- TMP 컴포넌트, 현재 원문 및 로컬리제이션 키 교차검증 방식은 유지한다.
- 일반 Additive 로드가 다시 도입되지 않도록 EditMode 회귀 테스트를 유지한다.

## 영향 범위

에디터 로컬리제이션 전체검사에만 영향을 준다. 씬, 조명 설정, 런타임 전투 로직 및 로컬리제이션 데이터는 변경하지 않는다.
