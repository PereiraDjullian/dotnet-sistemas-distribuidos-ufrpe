using Relogios_Lamport.Core;

if (args.Length < 3)
{
    Console.WriteLine("Uso: Relogios_Lamport.exe <ID_PROCESSO> <PORTA> <ID1:IP:PORTA1,ID2:IP:PORTA2,...>");
    return;
}

int myId = int.Parse(args[0]);
int myPort = int.Parse(args[1]);
var peers = ParsePeers(args[2]);

var process = new LamportProcess(myId, myPort, peers);
process.StartServer();

Console.WriteLine("Pressione ENTER quando TODOS os 4 processos estiverem abertos...");
Console.ReadLine();

// Escalonamento leve por ID para intercalar mensagens na rede
await Task.Delay(myId * 300);

// 1. Evento local inicial em cada processo
process.ExecuteLocalEvent($"Inicialização de rotina em P{myId}");
await Task.Delay(400);

// 2. Troca de mensagens entre os 4 processos
switch (myId)
{
    case 1:
        await process.SendMessageAsync(2, "Pedido de consulta de saldo");
        await Task.Delay(500);
        process.ExecuteLocalEvent("Processamento interno pós-envio em P1");
        break;

    case 2:
        await process.SendMessageAsync(3, "Encaminhamento de transação");
        await Task.Delay(500);
        await process.SendMessageAsync(4, "Notificação de auditoria");
        break;

    case 3:
        process.ExecuteLocalEvent("Validação de regras de negócio em P3");
        await Task.Delay(400);
        await process.SendMessageAsync(1, "Confirmação de validação");
        break;

    case 4:
        process.ExecuteLocalEvent("Gravação de log local em P4");
        await Task.Delay(600);
        await process.SendMessageAsync(2, "ACK de auditoria");
        break;
}

// Aguarda todas as mensagens chegarem aos destinos
await Task.Delay(2500);
process.ExecuteLocalEvent($"Encerramento de operações em P{myId}");

// 3. Consolidação no Processo 1 para exibição da tabela ordenada
if (myId == 1)
{
    var allEvents = await process.WaitAllReportsAsync(totalProcesses: 4);
    EventAnalyzer.PrintGlobalOrder(allEvents);
}
else
{
    await process.SendHistoryToLeaderAsync(leaderId: 1);
    Console.WriteLine($"\n[P{myId}] Histórico enviado para P1. Verifique a tabela ordenada no terminal de P1.");
}

Console.WriteLine("\nPressione ENTER para encerrar...");
Console.ReadLine();

static Dictionary<int, (string Ip, int Port)> ParsePeers(string rawPeers)
{
    var result = new Dictionary<int, (string Ip, int Port)>();
    foreach (var entry in rawPeers.Split(',', StringSplitOptions.RemoveEmptyEntries))
    {
        var parts = entry.Split(':');
        result[int.Parse(parts[0])] = (parts[1], int.Parse(parts[2]));
    }
    return result;
}