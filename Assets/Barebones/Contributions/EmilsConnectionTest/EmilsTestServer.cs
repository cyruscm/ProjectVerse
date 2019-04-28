using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using Barebones.Networking;
using UnityEngine;

/// <summary>
///     Big thanks to Emil Rainero for contributing this script!
/// </summary>
public class EmilsTestServer : MonoBehaviour {
    private const int _messageOpCode = 0;
    private List<IPeer> _clients;

    private IServerSocket _server;
    public int port = 777;
    public bool sendMessages;
    public bool startServer;
    public bool useWs = true;

    // Use this for initialization
    private void Start() {
        ParseCommandLineArguments();

        if (startServer) StartServer();
    }

    private void ParseCommandLineArguments() {
        startServer = Msf.Args.IsProvided("-startServer") ? true : startServer;
        useWs = Msf.Args.IsProvided("-useWs") ? true : useWs;
        useWs = !Msf.Args.IsProvided("-useUnet") ? false : useWs;
        sendMessages = Msf.Args.IsProvided("-sendMessages") ? true : sendMessages;

        if (Msf.Args.IsProvided("-port")) port = Msf.Args.ExtractValueInt("-port");

#if UNITY_EDITOR
        startServer = true;
#endif
    }

    private void StartServer() {
        _clients = new List<IPeer>();

        if (useWs)
            _server = new ServerSocketWs();
        else
            _server = new ServerSocketUnet();

        _server.Connected += ClientConnected;
        _server.Disconnected += ClientDisconnected;

        // Start listening
        Debug.Log("Server: listening on port " + port);
        _server.Listen(port);

        if (sendMessages) StartCoroutine(SendMessages(1f));
    }

    private void ClientConnected(IPeer client) {
        Debug.Log("Server: client connected, Id: " + client.Id);

        _clients.Add(client);
    }

    private void ClientDisconnected(IPeer client) {
        Debug.Log("Server: client disconnected, Id: " + client.Id);

        _clients.Remove(client);
    }

    private IEnumerator SendMessages(float delay) {
        var count = 0;
        while (true) {
            yield return new WaitForSeconds(delay);

            if (_clients.Count > 0) {
                count++;
                var str = "Message " + count;
                Debug.Log(string.Format("Server: Sending message \"{0}\" to {1} client(s)", str, _clients.Count));

                BroadcastMessage(MessageHelper.Create(_messageOpCode, str));
            }
        }
    }

    private void BroadcastMessage(IMessage msg) {
        foreach (var client in _clients) client.SendMessage(msg, DeliveryMethod.Reliable);
    }
}