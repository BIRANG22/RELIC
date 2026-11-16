# 언어 폰트 글리프·Atlas 일관성 설계

## 확인된 원인

- `ContinueButton/Text`의 `탐사진행`은 하나의 TMP 텍스트이며 rich text의 `<font>` 태그로 분리되지 않는다.
- 한국어 DB 폰트인 `Pretendard-Bold SDF`에는 `탐`, `사`만 생성되어 있고 `진`, `행`은 없다.
- 원본 OTF에는 네 글자가 모두 있지만 TMP Font Asset의 1024 atlas가 포화되어 있고 Multi Atlas가 꺼져 있어 누락 글리프를 동적으로 추가하지 못한다.
- 누락 글리프는 TMP 기본 fallback인 DungGeunMo로 표시되어 한 단어 안에서 두 폰트가 섞인다.
- 일본어·중국어·스페인어 DB 폰트도 작은 초기 문자표와 비활성 Multi Atlas 설정 때문에 같은 문제가 다수 존재한다.
- 일부 TMP는 `font`와 `fontSharedMaterial`의 atlas texture가 달라, 폰트 교체 후에도 이전 material이 남을 수 있다.
- 번체 DB의 LINE Seed 원본 폰트는 현재 번체 문자열에 필요한 문자 중 일부를 지원하지 않는다. `ZhiyiSans-Bold` 원본은 현재 번체 문자열을 모두 지원한다.

## 권장 설계

1. DB에서 사용하는 동적 TMP Font Asset의 Multi Atlas를 활성화한다.
2. 번체 기본 폰트를 `ZhiyiSans-Bold SDF`로 교체한다.
3. DB 폰트를 적용하는 공용 함수를 만들고 다음을 원자적으로 처리한다.
   - 대상 TMP의 `font` 교체
   - 현재 material의 atlas texture가 새 폰트와 호환되지 않을 때만 새 폰트의 기본 material로 교체
   - 호환되는 material preset은 유지
4. DB 폰트를 직접 쓰는 모든 경로가 공용 함수를 사용하도록 통합한다.
5. 실제 문자열·폰트 설정·material 호환성을 EditMode 테스트로 고정한다.

## 범위

- 한국어, 영어/스페인어, 일본어, 중국어 간체/번체 DB 폰트
- 전역 적용기, 개별 지역화 컴포넌트, 드롭다운, 로비 퀘스트, 블러 복제 경로
- Font Asset 설정 및 LocaleFontDatabase 참조

## 비범위

- UI 디자인이나 폰트 크기/간격 변경
- TMP 기본 fallback 체계 전체 제거
- 전투 로직 변경

