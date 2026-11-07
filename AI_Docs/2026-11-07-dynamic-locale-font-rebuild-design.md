# 로케일 폰트 Dynamic SDF 재구성 설계

## 목적

삭제되어 Missing 상태인 `LocaleFontDatabase` 폰트를 사용자가 제공한 원본 TTF와 TMP Font Asset으로 복구하되, 133.52 MiB의 `DungGeunMo SDF.asset`은 저장소에 다시 넣지 않는다.

## 선택한 접근

- 삭제 복원: 기존 GUID는 보존되지만 거대한 정적 아틀라스와 중복 폰트 구조도 함께 복원되어 제외한다.
- Git LFS: 업로드 제한은 피하지만 빌드 용량과 반복 변경 문제를 해결하지 못해 제외한다.
- Dynamic SDF 재구성: 원본 TTF를 보관하고 작은 초기 아틀라스를 런타임에 확장하므로 선택한다.

## 구조

- 원본 폰트: `Assets/Fonts/Source`
- TMP Font Asset: `Assets/Fonts/TMP`
- 제공된 Dynamic SDF 네 개는 기존 GUID를 유지한다.
- DungGeunMo는 제공된 정적 에셋의 폰트 메트릭과 GUID를 보존하되 글리프·문자·대형 아틀라스를 제거한 Dynamic SDF로 재구성한다.
- 기존 씬·프리팹이 직접 참조하는 삭제 전 DungGeunMo GUID는 같은 원본을 사용하는 작은 Dynamic 호환 에셋으로 복구한다.
- CJK Dynamic SDF는 4096 아틀라스와 Multi Atlas를 사용한다. 문자 범위가 작은 MASTOD 숫자 폰트는 제공된 1024 아틀라스를 유지한다.
- `LocaleFontDatabase`는 한국어/영어/스페인어에 DungGeunMo, 일본어에 MPLUSU, 중국어 간체에 LXGWWenKaiGB, 중국어 번체에 ZhiyiSans를 사용한다.
- 공통 숫자 폰트와 보존 소스 역할은 MASTOD로 연결한다.
- 더 이상 조회에 사용하지 않는 숨은 `roleFonts`의 Missing 참조는 비운다.

## 검증

- 모든 DB 폰트 GUID가 실제 `.meta`에 존재하는지 확인한다.
- 모든 TMP Font Asset의 Source Font GUID가 실제 TTF `.meta`에 존재하는지 확인한다.
- 모든 TMP Font Asset이 Dynamic/Multi Atlas인지, CJK 폰트가 4096이고 숫자 폰트가 1024인지 확인한다.
- 개별 파일이 GitHub 일반 Git 단일 파일 제한보다 작은지 확인한다.
- 런타임/에디터 C# 프로젝트를 컴파일한다.

## 비목표

- 씬·프리팹의 개별 TMP 참조를 일괄 교체하지 않는다. 기존 전역 로케일 폰트 적용기를 사용한다.
- Unity 에디터를 직접 제어하지 않는다.
- 폰트 라이선스나 번체 글리프의 시각적 품질은 자동 판정하지 않는다.

## 멀티플레이 영향

클라이언트의 표시용 폰트 에셋과 로케일 DB만 변경한다. 전투 Command, State, Result/Event 및 네트워크 동기화에는 영향이 없다.
