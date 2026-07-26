# FreeNet

FreeNet is a lightweight asynchronous C# network library.

---

## Project Info - 프로젝트 정보

* C# asynchronous network library. - C# 비동기 네트워크 라이브러리.
* TCP socket server that can be used in game servers. - 게임 서버에서 사용할 수 있는 TCP기반의 socket server.
* Uses .NET 10 - 사용
* Can be integrated with Unity if built with .NetFramework - Unity 연동 가능 (.NetFramework버전으로 바꿔서 빌드해야 연동 가능함)

---

## Contact

* Email me if you have any questions : lee.seokhyun@gmail.com

---

## Version

* v0.1.2 Upgrade to .NET 10
* v0.1.1 Apply .Net Core
* v0.1.0 Heartbeat
* v0.0.1

### Semantic Versioning

* FreeNet follows [SemVer](https://semver.org/) (`MAJOR.MINOR.PATCH`).
* Build outputs automatically include generated semantic versions:
  * `Release` builds: `MAJOR.MINOR.PATCH`
  * non-`Release` builds: `MAJOR.MINOR.PATCH-dev.<UTC timestamp>`

---

## License - 라이선스

The source code can be freely used for both commercial and non-commercial purposes. - 소스코드는 상업적, 비상업적 어느 용도이든 자유롭게 사용 가능 합니다.

---

## Structure - 아키텍처 및 구조

* Transmission Method
  * Asynchronous accept.
  * Asynchronous receive and send.
* Pooling
  * SocketAsyncEventArgs pooling management.
  * Receive buffer pooling management.
* Performance Optimization
  * Aggregate BufferList for batch sending.
  * <p align="center"><img src="https://github.com/sunduk/FreeNet/blob/master/documents/send_en.png?raw=true" alt="send"></p>
  * Use of double buffering queues.
* Thread Model
  * IO thread packet processing method.
  * Single logic thread packet processing method.
* Additional Features
  * Heartbeat functionality.

<p align="center"><img src="https://github.com/sunduk/FreeNet/blob/master/documents/struct.png?raw=true" alt="structure"></p>
<p align="center"><img src="https://github.com/sunduk/FreeNet/blob/master/documents/class_struct.png?raw=true" alt="class structure"></p>
<p align="center"><img src="https://github.com/sunduk/FreeNet/blob/master/documents/worker_thread.png?raw=true" alt="worker"></p>
<p align="center"><img src="https://github.com/sunduk/FreeNet/blob/master/documents/logic_thread.png?raw=true" alt="logic"></p>
<p align="center"><img src="https://github.com/sunduk/FreeNet/blob/master/documents/send.png?raw=true" alt="send"></p>

---
