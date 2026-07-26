# Test Manual - 테스트 매뉴얼

## Test Client - 테스트 클라이언트

* Uses the test tool published on the cgcii website. - cgcii홈페이지에 공개된 테스트툴을 활용.
* Download page - 다운로드 페이지: [http://www.cgcii.co.kr/index.php?mid=board_eLHH13&document_srl=1936](http://www.cgcii.co.kr/index.php?mid=board_eLHH13&document_srl=1936)
* Test client download link - 테스트 클라이언트 다운로드 링크: [http://www.cgcii.co.kr/?module=file&act=procFileDownload&file_srl=2976&sid=ed18a57f286b4fd7490ebd0fc2da9dcd&module_srl=1910](http://www.cgcii.co.kr/?module=file&act=procFileDownload&file_srl=2976&sid=ed18a57f286b4fd7490ebd0fc2da9dcd&module_srl=1910)
* Test server - 테스트 서버: [http://www.cgcii.co.kr/?module=file&act=procFileDownload&file_srl=2976&sid=ed18a57f286b4fd7490ebd0fc2da9dcd&module_srl=1910](http://www.cgcii.co.kr/?module=file&act=procFileDownload&file_srl=2976&sid=ed18a57f286b4fd7490ebd0fc2da9dcd&module_srl=1910)

---

## Running a Test - 테스트 진행

* Use the CSampleServer project included in the Te* project. - 프로젝트에 포함되어 있는 CSampleServer프로젝트를 사용.
  Modify the code for testing - 테스트를 위한 코드 수정
  * Turn off heartbeat: The test client doesn't have a heartbeat feature, so you need to turn it off on the server. - 하트비트 끄기 : 테스트 클라이언트에는 하트비트 기능이 없으므로 서버에서 꺼줘야한다
    * Remove the comment on line 29 of CSampleServer/Program.cs to call CNetworkService.disable_heartbeat(). - CSampleServer/Program.cs 29번째줄의 주석을 제거하여 CNetworkService.disable_heartbeat()를 호출한다.
  * Enable echo server feature: Activate the echo server feature to send back the packets that the test client sends. - 에코 서버 기능 활성화 : 테스트 클라이언트에서 보내는 패킷을 그대로 돌려주는 에코서버 기능을 활성화 한다
    * Remove the comments on lines 52 and 53 of CSampleServer/CGameUser.cs. - CSampleServer/CGameUser.cs 52,53번째줄의 주석을 제거한다.

1. Run the test server and the test client. - 테스트 서버와 테스트 클라이언트를 실행한다.
2. Carry out the test in the order shown in the picture. - 그림에 나온 순서대로 테스트를 진행한다.

  <p align="center"><img src="https://github.com/sunduk/FreeNet/blob/master/test_result/testtool.png?raw=true" alt="Test"></p>

3. Test performance by increasing the Times entries (**Caution: if increased too much, your PC may crash!!**). - Times 항목을 늘려가며 성능을 테스트한다(**주의:과도하게늘릴경우 PC가 따운될수있음!!**).

---

## Sample Game

---
<p align="center"><img src="https://github.com/sunduk/FreeNet/blob/master/viruswar/client/doc/screenshot.png?raw=true" alt="viruswar"></p>

* FreeNet라이브러리를 활용하여 Unity로 만든 온라인 멀티플레이 보드 게임 세균전.
* The VirusWar that online multiplay board game sample developed using FreeNet and Unity.
