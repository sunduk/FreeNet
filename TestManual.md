# Test Manual

## Test Client

* Uses the test tool published on the cgcii website.
* Download page: [http://www.cgcii.co.kr/index.php?mid=board_eLHH13&document_srl=1936](http://www.cgcii.co.kr/index.php?mid=board_eLHH13&document_srl=1936)
* Test client download link: [http://www.cgcii.co.kr/?module=file&act=procFileDownload&file_srl=2976&sid=ed18a57f286b4fd7490ebd0fc2da9dcd&module_srl=1910](http://www.cgcii.co.kr/?module=file&act=procFileDownload&file_srl=2976&sid=ed18a57f286b4fd7490ebd0fc2da9dcd&module_srl=1910)
* Test server: [http://www.cgcii.co.kr/?module=file&act=procFileDownload&file_srl=2976&sid=ed18a57f286b4fd7490ebd0fc2da9dcd&module_srl=1910](http://www.cgcii.co.kr/?module=file&act=procFileDownload&file_srl=2976&sid=ed18a57f286b4fd7490ebd0fc2da9dcd&module_srl=1910)

---

## Running a Test

* Use the CSampleServer project included in the project.
  Modify the code for testing
  * Turn off heartbeat: The test client doesn't have a heartbeat feature, so you need to turn it off on the server.
    * Remove the comment on line 29 of CSampleServer/Program.cs to call CNetworkService.disable_heartbeat().
  * Enable echo server feature: Activate the echo server feature to send back the packets that the test client sends.
    * Remove the comments on lines 52 and 53 of CSampleServer/CGameUser.cs.

1. Run the test server and the test client.
2. Carry out the test in the order shown in the picture.

  <p align="center"><img src="https://github.com/sunduk/FreeNet/blob/master/test_result/testtool.png?raw=true" alt="Test"></p>

3. Test performance by increasing the Times entries (**Caution: if increased too much, your PC may crash!!**).
---

## Sample Game

---
<p align="center"><img src="https://github.com/sunduk/FreeNet/blob/master/viruswar/client/doc/screenshot.png?raw=true" alt="viruswar"></p>

* VirusWar is an online multiplayer board game sample developed using FreeNet and Unity.
