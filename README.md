FreeNet
=========
**FreeNet** is a lightweight asynchronous network library.

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
    * ![image](https://github.com/sunduk/FreeNet/assets/438767/8ec79c1a-2230-4d5d-82d9-cb7ccb2d4957)
  * Use of double buffering queues.
* Thread Model
  * IO thread packet processing method.
  * Single logic thread packet processing method.
* Additional Features
  * Heartbeat functionality.
