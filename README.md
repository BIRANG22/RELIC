# RELIC
게임 RELIC 개발협업 내부공유저장소

프로젝트 개요

장르: 로그라이크 기반 전술 RPG
핵심 특징:
-액션 예약 기반 턴제 전투
-판단과 관리 중심의 플레이
-플레이어는 전투원이 아닌 Handler(관리자) 역할

개발 환경
Engine: Unity 6
Version Control: Git + GitHub
협업 인원: 3인

폴더 구조 개요
⚠️ 각 역할은 자기 담당 폴더만 수정하는 것을 원칙으로 합니다.

Assets/

├─ _Project/

│  ├─ Core/

│  ├─ Combat/

│  ├─ Run/

│  ├─ Characters/

│  ├─ Stages/

│  ├─ UI/

│  └─ Data/

│

├─ Art/

├─ Audio/

└─ Plugins/

프로그래밍:
Assets/_Project/Core
Assets/_Project/Combat
Assets/_Project/Run
Assets/_Project/UI/Common

아트:
Assets/Art
Assets/_Project/Character 프리팹의 Visual 계층
Animator / Mesh / Material

기획:
Assets/_Project/Data

씬(Scene) 관리 규칙
동시에 같은 씬을 수정하지 않음

각자 다른 씬을 사용하면 서로에 씬 건드리지 않기
플밍은 .cs파일(스크립트)만 아트는 모델, 텍스처, 애니 등 만 기획은 데이터파일만 사용하기

Git 브랜치 규칙
main        : 항상 실행 가능한 상태
develop     : 통합 테스트
feature/*   : 개인 작업 브랜치

Git브랜치 규칙
main    출시할때 보일 게임에 상태
develop 개발자 전용 테스트 ex)치트가 존재하거나 테스트 모델 등이 들어간 상태
feature 개인 작업 브랜치 ex)현재 개인 상황이 저장되어 있는 상태

진행방법 : develop에서 개인 브랜치를 새로 생성하여 각자 업데이트하고 develop에 합치기
            이후 문제 없이 정리 된 상태에 develop을 메인에 push
