using Algoritmo_Berkeley.Models;
using Algoritmo_Berkeley.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algoritmo_Berkeley.Roles
{
    public class CoordinatorNode
    {
        private readonly int _port;
        private readonly string[] _peers;
        private DateTime _localClock;

        public CoordinatorNode(int port, double initialOffsetSeconds, string[] peers)
        {
            _port = port;
            _localClock = DateTime.Now.AddSeconds(initialOffsetSeconds);
            _peers = peers;
        }

        public async Task ExecuteSynchronizationAsync()
        {
            Console.WriteLine($"[COORDENADOR - Porta {_port}] Iniciado.");
            Console.WriteLine($"[COORDENADOR - Porta {_port}] Relógio INICIAL: {_localClock:HH:mm:ss.fff}");
            Console.WriteLine("Pressione ENTER quando todos os 4 participantes estiverem rodando...");
            Console.ReadLine();

            // Passo 1: Coletar horários dos participantes e calcular diferenças em relação ao coordenador
            var peerDifferences = await CollectTimeDifferencesAsync();

            // Passo 2: Calcular a média incluindo o próprio coordenador (diferença 0)
            double averageDiffMs = CalculateBerkeleyAverage(peerDifferences.Values);
            Console.WriteLine($"\n[COORDENADOR] Média calculada das diferenças: {averageDiffMs:+0.00;-0.00;0} ms");

            // Passo 3: Ajustar o relógio do próprio coordenador
            Console.WriteLine($"[COORDENADOR] Relógio ANTES:  {_localClock:HH:mm:ss.fff}");
            _localClock = _localClock.AddMilliseconds(averageDiffMs);
            Console.WriteLine($"[COORDENADOR] Relógio DEPOIS: {_localClock:HH:mm:ss.fff}\n");

            // Passo 4: Enviar o ajuste individual para cada participante
            await SendAdjustmentsToPeersAsync(peerDifferences, averageDiffMs);

            Console.WriteLine("\n[COORDENADOR] Sincronização concluída!");
        }

        private async Task<Dictionary<string, double>> CollectTimeDifferencesAsync()
        {
            var differences = new Dictionary<string, double>();

            foreach (var peer in _peers)
            {
                var (ip, peerPort) = ParseEndpoint(peer);
                var request = new BerkeleyMessage { Type = "GET_TIME" };

                BerkeleyMessage reply = await TcpMessenger.SendAndReceiveAsync(ip, peerPort, request);
                DateTime peerClock = new DateTime(reply.TimestampTicks);

                double diffMs = (peerClock - _localClock).TotalMilliseconds;
                differences[peer] = diffMs;

                Console.WriteLine($"[COORDENADOR] {peer} -> Relógio: {peerClock:HH:mm:ss.fff} | Diferença: {diffMs:+0.00;-0.00;0} ms");
            }

            return differences;
        }

        private static double CalculateBerkeleyAverage(IEnumerable<double> peerDifferences)
        {
            var allDiffs = new List<double> { 0.0 }; // 0.0 = diferença do próprio coordenador
            allDiffs.AddRange(peerDifferences);
            return allDiffs.Average();
        }

        private async Task SendAdjustmentsToPeersAsync(Dictionary<string, double> peerDifferences, double averageDiffMs)
        {
            foreach (var (peer, peerDiff) in peerDifferences)
            {
                var (ip, peerPort) = ParseEndpoint(peer);
                double adjustmentForPeer = averageDiffMs - peerDiff;

                var adjustMsg = new BerkeleyMessage { Type = "ADJUST", AdjustmentMs = adjustmentForPeer };
                await TcpMessenger.SendAndWaitAckAsync(ip, peerPort, adjustMsg);

                Console.WriteLine($"[COORDENADOR] Enviado ajuste de {adjustmentForPeer:+0.00;-0.00;0} ms para {peer}");
            }
        }

        private static (string Ip, int Port) ParseEndpoint(string endpoint)
        {
            var parts = endpoint.Split(':');
            return (parts[0], int.Parse(parts[1]));
        }
    }
}
