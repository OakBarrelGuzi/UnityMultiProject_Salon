<div align="center">

![header](https://capsule-render.vercel.app/api?type=transparent&color=39FF14&height=150&section=header&text=Project_Salon&fontSize=50&animation=fadeIn&fontColor=39FF14&desc=KGA%20Team%20Pre-Contract%20Project%20Repository&descSize=25&descAlignY=75)

# 경일게임아카데미 사전협약 프로젝트

<p align="center">
  <img src="https://img.shields.io/badge/Unity-000000?style=for-the-badge&logo=unity&logoColor=white"/>
  <img src="https://img.shields.io/badge/Team_Project-FF4154?style=for-the-badge&logo=git&logoColor=white"/>
  <img src="https://img.shields.io/badge/Game_Development-4B32C3?style=for-the-badge&logo=gamemaker&logoColor=white"/>
</p>

## 📋 프로젝트 개요

<details>
<summary><b>📌 프로젝트 정보</b></summary>
<div align="center">

━━━━━━━━━━━━━━━━━━━━━━

### 🎮 게임 장르

#### • 멀티 소셜 게임

━━━━━━━━━━━━━━━━━━━━━━

### 👥 개발 인원

#### • 프로그래머

4명의 프로그래머가 개발 진행

#### • 기획자

4명의 기획자가 기획 진행

━━━━━━━━━━━━━━━━━━━━━━

</div>
</details>

## 🔧 개발 환경

<p align="center">
  <img src="https://img.shields.io/badge/Unity_2022.3.2f1-000000?style=for-the-badge&logo=unity&logoColor=white"/>
  <img src="https://img.shields.io/badge/Visual_Studio-5C2D91?style=for-the-badge&logo=v&logoColor=white"/>
  <img src="https://img.shields.io/badge/VS_Code-007ACC?style=for-the-badge&logo=v&logoColor=white"/>
  <img src="https://img.shields.io/badge/Git-F05032?style=for-the-badge&logo=git&logoColor=white"/>
  <img src="https://img.shields.io/badge/Fork-0052CC?style=for-the-badge&logo=gitkraken&logoColor=white"/>
</p>

## 📚 Rules & Guidelines

<details>
<summary><b>📁 에셋 관리 규칙</b></summary>
<div align="center">

### ⚙️ 에셋 관리 규칙

━━━━━━━━━━━━━━━━━━━━━━

#### • 외부 에셋 설치

구글 드라이브의 External 압축파일을 Asset 폴더 내 설치  
 에셋 스토어 패키지는 반드시 팀장과 상의 후 설치

━━━━━━━━━━━━━━━━━━━━━━

#### • 신규 에셋 추가

External 폴더에 임포트 후 압축하여 드라이브 업로드  
 파일명: `External_MMDD_HHMM` (예: External_1227_1800)  
 추가된 에셋 정보를 팀 디스코드에 공유

━━━━━━━━━━━━━━━━━━━━━━

#### • 에셋 네이밍 규칙

영문 사용 (한글 사용 금지)  
 띄어쓰기 대신 카멜케이스 사용  
 프리팹: `Pref_기능명`  
 머티리얼: `Mat_용도명`  
 텍스처: `Tex_용도명`

━━━━━━━━━━━━━━━━━━━━━━

</div>
</details>

<details>
<summary><b>📝 브랜치 규칙</b></summary>
<div align="center">

### 🌿 브랜치 관리

━━━━━━━━━━━━━━━━━━━━━━

#### • `main` 브랜치

팀장(최현성) 관리  
 안정적인 빌드 버전만 유지  
 직접 커밋 금지

━━━━━━━━━━━━━━━━━━━━━━

#### • `designers` 브랜치

기획팀 전용 작업 공간  
 기획 문서 및 리소스 관리  
 머지 시 반드시 Pull Request 사용

━━━━━━━━━━━━━━━━━━━━━━

#### • `Dev_'개인이름'` 브랜치

개발자 개인 작업 공간  
 작업 완료 후 main에 PR 요청

━━━━━━━━━━━━━━━━━━━━━━

### 🔄 Pull Request 규칙

#### • PR 생성 시 필수 정보

작업 내용 상세 기술  
 관련 이슈 번호 태그

━━━━━━━━━━━━━━━━━━━━━━

</div>
</details>

<details>
<summary><b>💬 커밋 컨벤션</b></summary>
<div align="center">

### 📝 커밋 메시지 구조

━━━━━━━━━━━━━━━━━━━━━━

#### • 기본 구조

**[Type]**
커밋 유형

**[Subject]**
커밋 제목

**[Body]**
커밋 내용 상세 설명
• 첫 번째 변경 사항
• 두 번째 변경 사항

**[Footer]**
이슈 번호 참조
• Closes/Fixes #123 (해당 이슈가 자동으로 종료됨)
• Related to #124, #125 (관련 이슈 링크만 걸림, 종료되지 않음)

━━━━━━━━━━━━━━━━━━━━━━

#### • 커밋 타입 종류

| 타입             | 설명                                              |
| ---------------- | ------------------------------------------------- |
| feat             | 새로운 기능 추가                                  |
| fix              | 버그 수정                                         |
| docs             | 문서 수정                                         |
| style            | 코드 포맷팅, 세미콜론 누락, 코드 변경이 없는 경우 |
| refactor         | 코드 리팩토링                                     |
| test             | 테스트 코드 추가                                  |
| chore            | 빌드 업무 수정, 패키지 매니저 수정 (잡일)                |
| design           | UI/UX 디자인 변경                                 |
| comment          | 필요한 주석 추가 및 변경                          |
| rename           | 파일 혹은 폴더명을 수정하거나 옮기는 작업         |
| remove           | 파일을 삭제하는 작업                              |
| !BREAKING CHANGE | 커다란 API 변경                                   |
| !HOTFIX          | 급하게 치명적인 버그를 고치는 경우                |

━━━━━━━━━━━━━━━━━━━━━━

#### • 커밋 메시지 예시

**[feat]**
실시간 채팅 시스템 구현

**[Body]**
• 1:1 채팅방 생성 및 관리 기능
• 이모티콘 시스템 통합
• 채팅 히스토리 저장 구현
• 실시간 메시지 알림 기능 추가

**[Footer]**
Closes #128
Related to #125, #126

━━━━━━━━━━━━━━━━━━━━━━

</div>
</details>

</div>
