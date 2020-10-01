FreeNet
=========
* C# Network library. Asynchronous. TCP. GameServer.
* Email me if you have any questions : lee.seokhyun@gmail.com

Version
----------
* v0.1.1 Apply .Net Core
* v0.1.0 Heartbeat
* v0.0.1

프로젝트 정보
----------
* C# 비동기 네트워크 라이브러리.
* 게임 서버에서 사용할 수 있는 TCP기반의 socket server.
* .Net Core 사용
* Unity 연동 가능 (.NetFramework버전으로 바꿔서 빌드해야 연동 가능함)

Project info
----------
* C# Asynchronous network library.
* TCP socket server that can be used in game server.
* .Net Core
* Available in unity3d (need to use .NetFramework instead of .NetCore).

테스트 클라이언트
----------
* cgcii홈페이지에 공개된 테스트툴을 활용.
* 다운로드 페이지 : [http://www.cgcii.co.kr/index.php?mid=board_eLHH13&document_srl=1936](http://www.cgcii.co.kr/index.php?mid=board_eLHH13&document_srl=1936)
* 테스트 클라이언트 다운로드 링크 : [http://www.cgcii.co.kr/?module=file&act=procFileDownload&file_srl=2976&sid=ed18a57f286b4fd7490ebd0fc2da9dcd&module_srl=1910](http://www.cgcii.co.kr/?module=file&act=procFileDownload&file_srl=2976&sid=ed18a57f286b4fd7490ebd0fc2da9dcd&module_srl=1910)

테스트 서버
----------
* 프로젝트에 포함되어 있는 CSampleServer프로젝트를 사용.

테스트를 위한 코드 수정
----------
* 하트비트 끄기 : 테스트 클라이언트에는 하트비트 기능이 없으므로 서버에서 꺼줘야한다
  * CSampleServer/Program.cs 29번째줄의 주석을 제거하여 CNetworkService.disable_heartbeat()를 호출한다.
* 에코 서버 기능 활성화 : 테스트 클라이언트에서 보내는 패킷을 그대로 돌려주는 에코서버 기능을 활성화 한다
  * CSampleServer/CGameUser.cs 52,53번째줄의 주석을 제거한다.

테스트 진행
----------
1. 테스트 서버와 테스트 클라이언트를 실행한다.
2. 그림에 나온 순서대로 테스트를 진행한다.
* ![Test](https://github.com/sunduk/FreeNet/blob/master/test_result/testtool.png?raw=true)
3. Times 항목을 늘려가며 성능을 테스트한다(**주의:과도하게늘릴경우 PC가 따운될수있음!!**).

Sample Game
----------
![viruswar](https://github.com/sunduk/FreeNet/blob/master/viruswar/client/doc/screenshot.png?raw=true)
* FreeNet라이브러리를 활용하여 Unity로 만든 온라인 멀티플레이 보드 게임 세균전.
* The VirusWar that online multiplay board game sample developed using FreeNet and Unity.

아키텍처 및 구조   
----------
![structure](https://github.com/sunduk/FreeNet/blob/master/documents/struct.png?raw=true)
![class structure](https://github.com/sunduk/FreeNet/blob/master/documents/class_struct.png?raw=true)
![worker](https://github.com/sunduk/FreeNet/blob/master/documents/worker_thread.png?raw=true)
![logic](https://github.com/sunduk/FreeNet/blob/master/documents/logic_thread.png?raw=true)
![send](https://github.com/sunduk/FreeNet/blob/master/documents/send.png?raw=true)

Structure
----------
![structure](https://github.com/sunduk/FreeNet/blob/master/documents/struct_en.png?raw=true)
![class structure](https://github.com/sunduk/FreeNet/blob/master/documents/class_struct_en.png?raw=true)
![worker](https://github.com/sunduk/FreeNet/blob/master/documents/worker_thread_en.png?raw=true)
![logic](https://github.com/sunduk/FreeNet/blob/master/documents/logic_thread_en.png?raw=true)
![send](https://github.com/sunduk/FreeNet/blob/master/documents/send_en.png?raw=true)


라이선스
----------
* 소스코드는 상업적, 비상업적 어느 용도이든 자유롭게 사용 가능 합니다.

License
----------
* All source codes are free to use commercially also
