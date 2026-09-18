# WebGL 메모리 및 VFX/SFX 빌드 최적화 설계

## 목표

WebGL Player가 초기 로딩 중 WebAssembly 메모리 부족으로 중단되는 문제를 완화한다. WebGL 최대 메모리를 4GB로 상향하고, 실제 게임에서 사용하지 않는 VFX/SFX가 Player 또는 초기 씬에 포함되는 경로를 제거한다.

## 관찰 결과

- WebGL 최대 메모리는 2GB이며, Player는 `abort("OOM")`로 종료된다.
- 최근 Player 통계의 비압축 씬 합계는 5.34GB이고, Bootstrap만 3.46GB다.
- 프로젝트에는 대형 WAV, VFX 텍스처, 패키지 데모 리소스가 다수 존재한다.
- Vefects 원본 폴더의 많은 에셋은 데모/미사용 후보지만, 실제 프리팹에서 참조되는 효과도 있으므로 삭제하지 않는다.

## 설계

1. WebGL 설정
   - 최대 메모리를 4096MB로 설정한다.
   - 초기 메모리는 512MB로 올려 반복적인 작은 메모리 증가를 줄인다.

2. 포함 대상 분석
   - Build Report 및 씬 직렬화 의존성을 사용해 Bootstrap과 Build Settings 씬에서 참조되는 VFX/SFX를 추적한다.
   - `Resources` 경로와 Addressables 엔트리도 별도로 검사한다.

3. VFX/SFX 축소
   - 게임 외부 참조가 없는 Vefects 데모 프리팹·머티리얼·텍스처는 WebGL 빌드 대상에서 제외한다.
   - 실제 사용 VFX의 WebGL 텍스처만 플랫폼 오버라이드로 압축/최대 크기를 낮춘다.
   - 실제 사용 BGM/SFX 중 대형 PCM WAV는 WebGL 플랫폼에서 Vorbis 압축과 적절한 로드 타입을 사용한다.
   - 씬 시작에 필요 없는 대형 리소스는 Addressables의 지연 로드 후보로 문서화하며, 참조 구조가 확인된 항목만 이동한다.

## 안전성

- 원본 VFX/SFX 파일을 삭제하지 않는다.
- 씬/프리팹의 직접 참조를 끊기 전에 의존성 검사를 수행한다.
- 게임플레이 전투 상태와 멀티플레이 동기화에는 변경하지 않는다.

## 검증

- WebGL 설정 및 제외 목록을 검사하는 EditMode 테스트를 추가한다.
- WebGL Build & Run 후 Editor.log에서 OOM 및 Addressables 오류 여부를 확인한다.
