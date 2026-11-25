using Algoritmo_Berkeley.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Algoritmo_Berkeley.Network
{
    public static class TcpMessenger
    {
        public static async Task<BerkeleyMessage> SendAndReceiveAsync(string ip, int port, BerkeleyMessage message)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(ip, port);

            using NetworkStream stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            string jsonOut = JsonSerializer.Serialize(message);
            await writer.WriteLineAsync(jsonOut);

            string? jsonIn = await reader.ReadLineAsync();
            return JsonSerializer.Deserialize<BerkeleyMessage>(jsonIn!)!;
        }

        public static async Task SendAndWaitAckAsync(string ip, int port, BerkeleyMessage message)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(ip, port);

            using NetworkStream stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            string jsonOut = JsonSerializer.Serialize(message);
            await writer.WriteLineAsync(jsonOut);

            // Para aguardar o participante confirmar que aplicou o ajuste antes de fechar o socket
            await reader.ReadLineAsync();
        }
    }
}
