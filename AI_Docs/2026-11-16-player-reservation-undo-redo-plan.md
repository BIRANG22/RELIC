# 플레이어 예약 Undo/Redo 복구 구현 계획

## 목표

직접 취소를 Undo 가능한 `Delete` 작업으로 기록하고, 예약 추가·드래그 이동·취소를 여러 번 연속 Undo/Redo할 수 있게 한다.

## 작업 1: 회귀 테스트

- 직접 취소 후 Undo가 명령을 복원하는 테스트
- 직접 취소 후 Redo가 명령을 다시 삭제하는 테스트
- 여러 직접 취소가 역순으로 모두 Undo되고 정순으로 모두 Redo되는 테스트
- 여러 예약 추가와 여러 이동의 기존 다단계 테스트 유지

## 작업 2: Delete 히스토리 구현

- `PlayerReservationHistoryOperation.Delete` 추가
- 삭제 전 슬롯/순서를 보존하는 팩토리 추가
- 직접 취소 성공 후 Delete를 Undo 스택에 기록
- Delete Undo/Redo 전용 삽입·삭제 처리 추가
- 내부 처리 중 기존 히스토리 제거/Redo 초기화 방지

## 작업 3: 검증

- 원격 열람/예약 슬롯 잠금 제거
- Undo/Redo 네트워크 Command 및 호스트 소유권 검증 추가
- Snapshot 기반 Undo/Redo 가용 상태 동기화
- 안정적인 ReservationId 전달 및 스냅샷 이후 히스토리 재연결

## 작업 4: 검증

- 변경 테스트 소스 정적 확인
- `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj` 컴파일
- Unity 에디터 직접 실행 없이 코드·테스트·컴파일 결과 검토

## 검토 초점

- 삭제한 정확한 객체만 복원/재삭제하는가
- 같은 슬롯에서 원래 순서가 보존되는가
- 새 작업이 Redo 분기를 올바르게 무효화하는가
- Add/Move 기존 동작이 퇴행하지 않는가
