using System;
using System.IO;

// Binary framing protocol for Python ↔ Unity IPC over TCP.
// All integers are little-endian. Frame format: [4-byte LE payload_length][payload bytes].
public static class PlacementProtocol
{
    public const byte MSG_HELLO = 0;
    public const byte MSG_RESET = 1;
    public const byte MSG_QUERY = 2;
    public const byte MSG_COMMIT = 3;
    public const byte MSG_CLOSE = 4;

    static PlacementProtocol()
    {
        if (!BitConverter.IsLittleEndian)
            throw new PlatformNotSupportedException("PlacementProtocol requires a little-endian platform.");
    }

    public static void WriteInt32(byte[] buf, int offset, int value)
    {
        buf[offset]     = (byte)(value);
        buf[offset + 1] = (byte)(value >> 8);
        buf[offset + 2] = (byte)(value >> 16);
        buf[offset + 3] = (byte)(value >> 24);
    }

    public static int ReadInt32(byte[] buf, int offset)
    {
        return buf[offset]
             | (buf[offset + 1] << 8)
             | (buf[offset + 2] << 16)
             | (buf[offset + 3] << 24);
    }

    public static void WriteFrame(Stream stream, byte[] payload, int offset, int count)
    {
        byte[] header = new byte[4];
        WriteInt32(header, 0, count);
        stream.Write(header, 0, 4);
        stream.Write(payload, offset, count);
        stream.Flush();
    }

    // Reads one frame into buffer. Returns payload length.
    public static int ReadFrame(Stream stream, byte[] buffer)
    {
        byte[] header = new byte[4];
        ReadExact(stream, header, 0, 4);
        int length = ReadInt32(header, 0);
        if (length > buffer.Length)
            throw new InvalidOperationException($"Frame payload {length} exceeds buffer size {buffer.Length}.");
        ReadExact(stream, buffer, 0, length);
        return length;
    }

    public static void ReadExact(Stream stream, byte[] buffer, int offset, int count)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int bytesRead = stream.Read(buffer, offset + totalRead, count - totalRead);
            if (bytesRead == 0)
                throw new EndOfStreamException("Connection closed while reading.");
            totalRead += bytesRead;
        }
    }
}
