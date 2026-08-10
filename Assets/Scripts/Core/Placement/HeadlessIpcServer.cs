using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

// TCP server that exposes RolloutEnvironment to an external process (Python training loop).
// Blocks the main thread intentionally — all Unity API calls stay on the main thread, and in
// -batchmode there is nothing else that needs Update(). One client at a time.
public class HeadlessIpcServer : MonoBehaviour
{
    [SerializeField] private RolloutEnvironment environment;
    [SerializeField] private int port = 9876;

    private bool started;

    private void Update()
    {
        if (started) return;
        started = true;
        RunServer();
    }

    private void RunServer()
    {
        // Command-line override: -port <N>
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-port" && int.TryParse(args[i + 1], out int parsed))
            {
                port = parsed;
                break;
            }
        }

        int w = environment.BoardWidth;
        int h = environment.BoardHeight;
        int boardSize = w * h;
        int maxCandidates = 4 * (w + 5);
        int maxResponseSize = 4 + 1 + boardSize + 4 + maxCandidates * (boardSize + 4);

        // Pre-allocated buffers
        byte[] boardBuffer = new byte[boardSize];
        byte[][] afterStateBuffers = new byte[maxCandidates][];
        for (int i = 0; i < maxCandidates; i++)
            afterStateBuffers[i] = new byte[boardSize];
        int[] linesClearedBuffer = new int[maxCandidates];
        byte[] responseBuffer = new byte[maxResponseSize];
        byte[] recvBuffer = new byte[1024];

        IReadOnlyList<PlacementCandidate> cachedCandidates = null;

        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Debug.Log($"[HeadlessIpcServer] Listening on port {port} (board {w}x{h})");

        try
        {
            TcpClient client = listener.AcceptTcpClient();
            client.NoDelay = true;
            NetworkStream stream = client.GetStream();
            Debug.Log("[HeadlessIpcServer] Client connected.");

            // Send HELLO
            responseBuffer[0] = PlacementProtocol.MSG_HELLO;
            PlacementProtocol.WriteInt32(responseBuffer, 1, w);
            PlacementProtocol.WriteInt32(responseBuffer, 5, h);
            PlacementProtocol.WriteFrame(stream, responseBuffer, 0, 9);

            // Message loop
            bool running = true;
            while (running)
            {
                int payloadLen;
                try
                {
                    payloadLen = PlacementProtocol.ReadFrame(stream, recvBuffer);
                }
                catch (EndOfStreamException)
                {
                    Debug.Log("[HeadlessIpcServer] Client disconnected (EOF).");
                    break;
                }
                catch (IOException)
                {
                    Debug.Log("[HeadlessIpcServer] Client disconnected (IO).");
                    break;
                }

                byte msgType = recvBuffer[0];

                switch (msgType)
                {
                    case PlacementProtocol.MSG_RESET:
                    {
                        int seed = PlacementProtocol.ReadInt32(recvBuffer, 1);
                        environment.Reset(seed);
                        environment.GetBoardOccupancy(boardBuffer);

                        responseBuffer[0] = PlacementProtocol.MSG_RESET;
                        Buffer.BlockCopy(boardBuffer, 0, responseBuffer, 1, boardSize);
                        PlacementProtocol.WriteFrame(stream, responseBuffer, 0, 1 + boardSize);
                        cachedCandidates = null;
                        break;
                    }

                    case PlacementProtocol.MSG_QUERY:
                    {
                        int n = environment.QueryAfterStates(afterStateBuffers, linesClearedBuffer);
                        cachedCandidates = environment.GetLegalPlacements();

                        int offset = 0;
                        responseBuffer[offset++] = PlacementProtocol.MSG_QUERY;
                        PlacementProtocol.WriteInt32(responseBuffer, offset, n);
                        offset += 4;

                        for (int i = 0; i < n; i++)
                        {
                            Buffer.BlockCopy(afterStateBuffers[i], 0, responseBuffer, offset, boardSize);
                            offset += boardSize;
                            PlacementProtocol.WriteInt32(responseBuffer, offset, linesClearedBuffer[i]);
                            offset += 4;
                        }

                        PlacementProtocol.WriteFrame(stream, responseBuffer, 0, offset);
                        break;
                    }

                    case PlacementProtocol.MSG_COMMIT:
                    {
                        int idx = PlacementProtocol.ReadInt32(recvBuffer, 1);
                        if (cachedCandidates == null || idx < 0 || idx >= cachedCandidates.Count)
                        {
                            Debug.LogError($"[HeadlessIpcServer] Invalid COMMIT index {idx}");
                            running = false;
                            break;
                        }

                        int linesCleared = environment.Step(cachedCandidates[idx]);
                        bool done = environment.IsDone;
                        environment.GetBoardOccupancy(boardBuffer);

                        int offset = 0;
                        responseBuffer[offset++] = PlacementProtocol.MSG_COMMIT;
                        PlacementProtocol.WriteInt32(responseBuffer, offset, linesCleared);
                        offset += 4;
                        responseBuffer[offset++] = done ? (byte)1 : (byte)0;
                        Buffer.BlockCopy(boardBuffer, 0, responseBuffer, offset, boardSize);
                        offset += boardSize;

                        PlacementProtocol.WriteFrame(stream, responseBuffer, 0, offset);
                        cachedCandidates = null;
                        break;
                    }

                    case PlacementProtocol.MSG_CLOSE:
                        Debug.Log("[HeadlessIpcServer] CLOSE received, shutting down.");
                        running = false;
                        break;

                    default:
                        Debug.LogWarning($"[HeadlessIpcServer] Unknown msg type 0x{msgType:X2}, ignoring.");
                        break;
                }
            }

            stream.Close();
            client.Close();
        }
        catch (SocketException ex)
        {
            Debug.LogError($"[HeadlessIpcServer] Socket error: {ex.Message}");
        }
        finally
        {
            listener.Stop();
        }

        Debug.Log("[HeadlessIpcServer] Server stopped. Quitting application.");
        Application.Quit();
    }
}
