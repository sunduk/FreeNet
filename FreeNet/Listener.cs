using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace FreeNet;

/// <summary>
/// Listener class is responsible for accepting new client connections on a specified host and port. It uses
/// asynchronous socket operations to handle incoming connections efficiently. The class provides a callback mechanism
/// to notify when a new client has connected, allowing for separation of socket handling and content implementation.
/// </summary>
internal class Listener
{
    /// <summary>
    /// The callback on new client
    /// </summary>
    public NewClientHandler CallbackOnNewClient;

    /// <summary>
    /// EventArgs for asynchronous accept operations.
    /// </summary>
    private SocketAsyncEventArgs _acceptArgs;

    /// <summary>
    /// Event used to control the accept processing sequence.
    /// </summary>
    private AutoResetEvent _flowControlEvent;

    /// <summary>
    /// The listen socket
    /// </summary>
    private Socket _listenSocket;

    /// <summary>
    /// Initializes a new instance of the <see cref="Listener"/> class.
    /// </summary>
    public Listener() => CallbackOnNewClient = null;

    /// <summary>
    /// Callback invoked when a new client connection is accepted.
    /// </summary>
    /// <param name="client_socket">The client socket.</param>
    /// <param name="token">The token.</param>
    public delegate void NewClientHandler(Socket client_socket, object token);

    /// <summary>
    /// Starts the specified host.
    /// </summary>
    /// <param name="host">The host.</param>
    /// <param name="port">The port.</param>
    /// <param name="backlog">The backlog.</param>
    public void Start(string host, int port, int backlog)
    {
        _listenSocket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);

        var address = host == "0.0.0.0" ? IPAddress.Any : IPAddress.Parse(host);
        IPEndPoint endpoint = new(address, port);

        try
        {
            _listenSocket.Bind(endpoint);
            _listenSocket.Listen(backlog);

            _acceptArgs = new SocketAsyncEventArgs();
            _acceptArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);

            Thread listen_thread = new(DoListen);
            listen_thread.Start();
        }
        catch
        {
            // ignored
        }
    }

    /// <summary>
    /// Accepts clients in a loop. The flow is controlled through an event so the next accept runs only after the previous connection has been processed.
    /// </summary>
    private void DoListen()
    {
        _flowControlEvent = new AutoResetEvent(false);

        while (true)
        {
            // Reset to null so the SocketAsyncEventArgs can be reused.
            _acceptArgs.AcceptSocket = null;

            bool pending;
            try
            {
                // Call asynchronous accept to receive a client connection. Even though this is an asynchronous method,
                // it can complete synchronously, so the return value must be checked.
                pending = _listenSocket.AcceptAsync(_acceptArgs);
            }
            catch
            {
                // ignored
                continue;
            }

            // If it completes immediately, no event is raised, so call the callback directly when the return value is false.
            // If it is pending, wait for the asynchronous callback instead. http://msdn.microsoft.com/ko-kr/library/system.net.sockets.socket.acceptasync%28v=vs.110%29.aspx
            if (!pending)
            {
                OnAcceptCompleted(null, _acceptArgs);
            }

            // Once client connection handling is complete, wait for the event signal before continuing the loop.
            _ = _flowControlEvent.WaitOne();

            // *Tip: It does not have to be called strictly in WaitOne -> Set order. Even if the accept operation
            // completes so quickly that Set -> WaitOne happens first, the next accept call still proceeds correctly.
            // If the event is already signaled when WaitOne is called, the thread continues without blocking.
        }
    }

    /// <summary>
    /// Callback method for AcceptAsync.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">The EventArgs used when calling AcceptAsync.</param>
    private void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success)
        {
            // Store the newly accepted socket.
            var client_socket = e.AcceptSocket;
            client_socket.NoDelay = true;

            // This class is responsible only for accepting connections. It invokes the callback so that post-accept
            // client handling can be delegated externally. This separates socket handling from content implementation.
            // Content logic is more likely to change, while the socket accept path changes less often, so keeping
            // them separate is beneficial. It also keeps this class focused on listening-related code only.
            CallbackOnNewClient?.Invoke(client_socket, e.UserToken);

            // Accept the next connection.
            _ = _flowControlEvent.Set();

            return;
        }
        else
        {
            // TODO: Handle accept failure.
            Console.WriteLine($"Failed to accept client. {e.SocketError}"); // TODO: Assumes there is a console to write to. Consider using a logging framework instead.
        }

        // Accept the next connection.
        _ = _flowControlEvent.Set();
    }
}
