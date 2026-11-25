using Relogios_Lamport.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Relogios_Lamport.Core
{
    public static class EventAnalyzer
    {
        public static void PrintGlobalOrder(List<EventRecord> events)
        {
            // Ordenação determinística: 1º Timestamp de Lamport, 2º ID do Processo como desempate
            var ordered = events
                .OrderBy(e => e.Timestamp)
                .ThenBy(e => e.ProcessId)
                .ToList();

            Console.WriteLine("\n=========================================================================");
            Console.WriteLine("  ORDENAÇÃO GLOBAL DOS EVENTOS (RELÓGIOS LÓGICOS DE LAMPORT)");
            Console.WriteLine("=========================================================================");
            Console.WriteLine($"{"Ordem",-6} | {"L(e)",-5} | {"Proc",-5} | {"Tipo",-12} | {"Descrição"}");
            Console.WriteLine(new string('-', 73));

            for (int i = 0; i < ordered.Count; i++)
            {
                var e = ordered[i];
                Console.WriteLine($"{i + 1,-6} | {e.Timestamp,-5} | P{e.ProcessId,-4} | {e.Type,-12} | {e.Description}");
            }

            PrintTieBreakers(ordered);
            PrintCausalityExplanation();
        }

        private static void PrintTieBreakers(List<EventRecord> ordered)
        {
            Console.WriteLine("\n--- CRITÉRIO DE DESEMPATE ---");
            var ties = ordered.GroupBy(e => e.Timestamp).Where(g => g.Count() > 1);

            foreach (var tie in ties)
            {
                string orderExplanation = string.Join(" < ", tie.Select(x => $"P{x.ProcessId}"));
                Console.WriteLine($"* Empate no Timestamp L={tie.Key}: resolvido pelo ID do processo ({orderExplanation}).");
            }
        }

        private static void PrintCausalityExplanation()
        {
            Console.WriteLine("\n--- ORDEM LÓGICA vs. CAUSALIDADE (HAPPENED-BEFORE: a -> b) ---");
            Console.WriteLine("1. Relação Causal (a -> b): Se o evento 'a' influencia 'b' (no mesmo processo ou");
            Console.WriteLine("   entre envio e recebimento de mensagem), então obrigatoriamente L(a) < L(b).");
            Console.WriteLine("2. Eventos Concorrentes (a || b): Eventos independentes em processos distintos");
            Console.WriteLine("   podem ter L(a) < L(b) ou empatar (ex.: eventos iniciais com L=1), mas não");
            Console.WriteLine("   têm relação de causa e efeito — a ordenação total é apenas uma convenção lógica.");
            Console.WriteLine("=========================================================================");
        }
    }
}
