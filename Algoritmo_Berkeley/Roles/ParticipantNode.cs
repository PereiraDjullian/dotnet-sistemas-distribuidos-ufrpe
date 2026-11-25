using Algoritmo_Berkeley.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Algoritmo_Berkeley.Roles
{
    public class ParticipantNode
    {
        private readonly int _port;
        private DateTime _localClock;

        public ParticipantNode(int port, double initialOffsetSeconds)
        {
            _port = port;
            _localClock = DateTime.Now.AddSeconds(initialOffsetSeconds);
        }

        public async Task StartAsync()
        {
            var listener = new TcpListener(IPAddress.Any, _port);
            listener.Start();

            Console.WriteLine($"[PARTICIPANTE - Porta {_port}] Iniciado.");
            Console.WriteLine($"[PARTICIPANTE - Porta {_port}] Relógio INICIAL: {_localClock:HH:mm:ss.fff}\n");

            while (true)
            {
                try
                {
                    using TcpClient client = await listener.AcceptTcpClientAsync();
                    await HandleCoordinatorRequestAsync(client);
                }
                catch (IOException)
                {
                    // Ignora quedas repentinas de conexão
                }
            }
        }

        private async Task HandleCoordinatorRequestAsync(TcpClient client)
        {
            using NetworkStream stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            string? line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) return;

            var message = JsonSerializer.Deserialize<BerkeleyMessage>(line)!;

            if (message.Type == "GET_TIME")
            {
                Console.WriteLine($"[PARTICIPANTE - Porta {_port}] Consulta recebida do coordenador.");
                var reply = new BerkeleyMessage { Type = "TIME_REPLY", TimestampTicks = _localClock.Ticks };
                await writer.WriteLineAsync(JsonSerializer.Serialize(reply));
                Console.WriteLine($"[PARTICIPANTE - Porta {_port}] Enviado relógio atual: {_localClock:HH:mm:ss.fff}");
            }
            else if (message.Type == "ADJUST")
            {
                ApplyClockAdjustment(message.AdjustmentMs);
                await writer.WriteLineAsync("ACK");
            }
        }

        private void ApplyClockAdjustment(double offsetMs)
        {
            Console.WriteLine($"\n[PARTICIPANTE - Porta {_port}] Ajuste recebido: {offsetMs:+0.00;-0.00;0} ms");
            Console.WriteLine($"[PARTICIPANTE - Porta {_port}] Relógio ANTES do ajuste:  {_localClock:HH:mm:ss.fff}");

            _localClock = _localClock.AddMilliseconds(offsetMs);

            Console.WriteLine($"[PARTICIPANTE - Porta {_port}] Relógio DEPOIS do ajuste: {_localClock:HH:mm:ss.fff}");
            Console.WriteLine("------------------------------------------------------");
        }
    }
}
