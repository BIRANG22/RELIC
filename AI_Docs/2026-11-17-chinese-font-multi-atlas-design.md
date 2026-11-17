# 중국어 TMP 폰트 Multi Atlas 수정 설계

## 문제와 목표

`LocaleFontDatabase`의 간체·번체 연결은 정상이나, 실제 사용 폰트가 Dynamic 1024×1024 단일 아틀라스로 제한되어 아틀라스가 찬 뒤 추가 중국어 글리프가 사각형으로 출력된다. 실제 DB가 사용하는 두 폰트의 Multi Atlas를 활성화하고 DB 기반 회귀 테스트로 보호한다.

## 범위

- `zh-Hans`의 `ZiTiQuanWeiJunHei-W2-2 SDF`와 `zh-Hant`의 `LINESeedTW_TTF_Bd SDF`만 수정한다.
- 폰트 원본, 아틀라스 크기, 로케일 적용 코드, 번역 데이터는 변경하지 않는다.
- 기존 한국어·일본어 폰트 검사는 유지한다.

## 검토한 접근

1. **Multi Atlas 활성화 — 채택.** 현재 Dynamic 생성 흐름을 유지하면서 용량 제한만 제거한다.
2. 4096×4096 확대. 한계를 늦출 뿐이고 텍스처 메모리 부담이 커진다.
3. 전체 번역 Static 아틀라스화. 별도 문자 수집·재생성 파이프라인이 필요해 범위를 벗어난다.

## 검증

- 테스트는 DB에서 `zh-Hans`, `zh-Hant` 폰트를 해석해 Dynamic, Multi Atlas, 원본 폰트 및 대표 문자를 검사한다.
- YAML 정적 검사와 런타임/에디터 C# 컴파일을 수행한다.
- Unity 에디터 직접 제어 및 batchmode 테스트는 수행하지 않는다.

