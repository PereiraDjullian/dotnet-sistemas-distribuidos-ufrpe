using Algoritmo_Berkeley.Roles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


   if (args.Length < 3)
{
    Console.WriteLine("Uso:");
    Console.WriteLine("  Participante: Algoritmo_Berkeley.exe node <PORTA> <DESVIO_SEGUNDOS>");
    Console.WriteLine("  Coordenador:  Algoritmo_Berkeley.exe coord <PORTA> <DESVIO_SEGUNDOS> <IP:PORTA1,IP:PORTA2,...>");
    return;
}

string role = args[0].ToLower();
int port = int.Parse(args[1]);
double initialOffsetSeconds = double.Parse(args[2]);

if (role == "node")
{
    var participant = new ParticipantNode(port, initialOffsetSeconds);
    await participant.StartAsync();
}
else if (role == "coord")
{
    string[] peers = args[3].Split(',', StringSplitOptions.RemoveEmptyEntries);
    var coordinator = new CoordinatorNode(port, initialOffsetSeconds, peers);
    await coordinator.ExecuteSynchronizationAsync();
}

