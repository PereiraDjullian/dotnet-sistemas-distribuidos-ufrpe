using Relogios_Lamport.Model;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Relogios_Lamport.Network;

public static class TcpLamportClient
{
    public static async Task SendAsync(string ip, int port, LamportMessage message)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(ip, port);

        using NetworkStream stream = client.GetStream();
        using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        string json = JsonSerializer.Serialize(message);
        await writer.WriteLineAsync(json);
    }
}