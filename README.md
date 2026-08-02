# FreeNet

FreeNet is a lightweight asynchronous C# network library.

---

## Project Info

* C# asynchronous network library.
* TCP socket server that can be used in game servers.
* Uses .NET 10
* Can be integrated with Unity if built with .NetFramework

---

## Fork Changes

* Added character movement packets and support for .NET 10
* Unity project repository: [https://github.com/NyanReal/unitymobclient](https://github.com/NyanReal/unitymobclient)

---

## Contact

* Email me if you have any questions : lee.seokhyun@gmail.com

---

## Version

* **v0.2.0** - System.IO.Pipelines modernization
  * Added modern async I/O via `UserToken.cs` for improved performance and resource efficiency
  * Added `StartPipelinesAsync(CancellationToken)` and pipeline-backed `Send(...)` processing
  * Backward compatible; existing SAEA-based code remains functional and unchanged
  * See [PIPELINES_MODERNIZATION.md](documents/PIPELINES_MODERNIZATION.md) for migration details
  * Released with MIT License

* **v0.1.2** - Upgrade to .NET 10
  * Updated all projects to target .NET 10 runtime
  * Enhanced async/await patterns and modern C# language features

* **v0.1.1** - Apply .NET Core
  * Migrated from .NET Framework to .NET Core

* **v0.1.0** - Heartbeat support
  * Added heartbeat mechanism for connection health monitoring

* **v0.0.1** - Initial Release
  * Initial async TCP socket server implementation with SAEA pooling

### Semantic Versioning

* FreeNet follows [SemVer](https://semver.org/) (`MAJOR.MINOR.PATCH`).
* Build outputs automatically include generated semantic versions:
  * `Release` builds: `MAJOR.MINOR.PATCH`
  * non-`Release` builds: `MAJOR.MINOR.PATCH-dev.<UTC timestamp>`

---

## License

FreeNet is released under the [MIT License](LICENSE). The source code can be freely used for both commercial and non-commercial purposes.

See the [LICENSE](LICENSE) file for the full text of the license.

---

## Structure

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
