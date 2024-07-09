FreeNet
=========
FreeNet is a lightweight asynchronous network library.

Structure
=========
* Transmission Method
  * Asynchronous accept.
  * Asynchronous receive and send.
* Pooling
  * SocketAsyncEventArgs pooling management.
  * Receive buffer pooling management.
* Performance Optimization
  * Aggregate BufferList for batch sending.
  * ![send](https://github.com/sunduk/FreeNet/blob/master/documents/send_en.png?raw=true)
  * Use of double buffering queues.
* Thread Model
  * IO thread packet processing method.
  * Single logic thread packet processing method.
* Additional Features
  * Heartbeat functionality.
