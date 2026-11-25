using Relogios_Lamport.Model;
using Relogios_Lamport.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Relogios_Lamport.Core
{
    public class LamportProcess
    {
        private readonly int _processId;
        private readonly int _port;
        private readonly Dictionary<int, (string Ip, int Port)> _peers;
        private readonly object _clockLock = new();

        private int _clock = 0;
        private readonly List<EventRecord> _localHistory = [];
        private readonly ConcurrentBag<EventRecord> _clusterEvents = [];
        private int _reportsReceived = 0;

        public LamportProcess(int processId, int port, Dictionary<int, (string Ip, int Port)> peers)
        {
            _processId = processId;
            _port = port;
            _peers = peers;
        }

        public void StartServer()
        {
            var listener = new TcpListener(IPAddress.Any, _port);
            listener.Start();
            _ = Task.Run(() => ListenLoopAsync(listener));
            Console.WriteLine($"[P{_processId} - Porta {_port}] Iniciado. Relógio Lógico = {_clock}");
        }

        // Regra 1: Antes de executar um evento local, incrementa o relógio lógico
        public void ExecuteLocalEvent(string description)
        {
            lock (_clockLock)
            {
                _clock++;
                var record = new EventRecord(_processId, _clock, "LOCAL", description);
                _localHistory.Add(record);
                Console.WriteLine($"[P{_processId} | L={_clock}] EVENTO LOCAL: {description}");
            }
        }

        // Regra 2: Antes de enviar mensagem, incrementa o relógio lógico e inclui o timestamp
        public async Task SendMessageAsync(int targetId, string content)
        {
            if (!_peers.TryGetValue(targetId, out var target)) return;

            int sendTimestamp;
            lock (_clockLock)
            {
                _clock++;
                sendTimestamp = _clock;
                var record = new EventRecord(_processId, sendTimestamp, "ENVIO", $"Para P{targetId}: '{content}'");
                _localHistory.Add(record);
                Console.WriteLine($"[P{_processId} | L={sendTimestamp}] ENVIO -> P{targetId}: '{content}'");
            }

            var message = new LamportMessage("MSG", _processId, sendTimestamp, content);
            await TcpLamportClient.SendAsync(target.Ip, target.Port, message);
        }

        public async Task SendHistoryToLeaderAsync(int leaderId)
        {
            if (!_peers.TryGetValue(leaderId, out var leader)) return;

            var report = new LamportMessage("REPORT", _processId, _clock, "Histórico final", _localHistory);
            await TcpLamportClient.SendAsync(leader.Ip, leader.Port, report);
        }

        public async Task<List<EventRecord>> WaitAllReportsAsync(int totalProcesses)
        {
            foreach (var ev in _localHistory)
                _clusterEvents.Add(ev);

            Interlocked.Increment(ref _reportsReceived);

            while (_reportsReceived < totalProcesses)
                await Task.Delay(200);

            return _clusterEvents.ToList();
        }

        private async Task ListenLoopAsync(TcpListener listener)
        {
            while (true)
            {
                try
                {
                    using TcpClient client = await listener.AcceptTcpClientAsync();
                    using NetworkStream stream = client.GetStream();
                    using var reader = new StreamReader(stream, Encoding.UTF8);

                    string? line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var message = JsonSerializer.Deserialize<LamportMessage>(line)!;

                    if (message.Type == "MSG")
                    {
                        HandleIncomingMessage(message);
                    }
                    else if (message.Type == "REPORT" && message.Events != null)
                    {
                        foreach (var ev in message.Events)
                            _clusterEvents.Add(ev);

                        Interlocked.Increment(ref _reportsReceived);
                    }
                }
                catch
                {
                    // Ignora encerramentos de conexão
                }
            }
        }

        // Regra 3: Cj = max(Cj, Tm) + 1
        private void HandleIncomingMessage(LamportMessage message)
        {
            lock (_clockLock)
            {
                int previousClock = _clock;
                _clock = Math.Max(_clock, message.Timestamp) + 1;

                var record = new EventRecord(
                    _processId,
                    _clock,
                    "RECEBIMENTO",
                    $"De P{message.SenderId} (Tm={message.Timestamp}): '{message.Content}'"
                );
                _localHistory.Add(record);

                Console.WriteLine($"[P{_processId} | L={previousClock} -> {_clock}] RECEBIDO de P{message.SenderId} (Tm={message.Timestamp}): '{message.Content}'");
            }
        }
    }
}
