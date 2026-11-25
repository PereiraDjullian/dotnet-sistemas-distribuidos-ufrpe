# Sistemas Distribuídos: Berkeley e Relógios de Lamport

Esta solução reúne dois projetos de console em C# e .NET 8 que simulam processos distribuídos independentes:

- **Algoritmo de Berkeley:** um coordenador consulta quatro participantes e calcula ajustes para aproximar seus relógios simulados.
- **Relógios lógicos de Lamport:** quatro processos registram eventos locais, envios e recebimentos com contadores lógicos; P1 reúne os históricos e exibe uma ordenação global.

Os processos se comunicam por **sockets TCP** (`TcpListener` e `TcpClient`). As mensagens são serializadas com `System.Text.Json`, transmitidas em UTF-8 e delimitadas por quebra de linha. No Berkeley, a confirmação do ajuste é a linha de texto `ACK`, não uma mensagem JSON. Cada projeto define seu próprio protocolo e deve ser executado separadamente.

## Estrutura da solução

```text
SistemasDistribuidosUFRPE/
├── SistemasDistribuidosUFRPE.slnx
├── README.md
├── Algoritmo_Berkeley/
│   ├── Algoritmo_Berkeley.csproj
│   ├── Program.cs
│   ├── Models/
│   │   └── BerkeleyMessage.cs
│   ├── Network/
│   │   └── TcpMessenger.cs
│   └── Roles/
│       ├── CoordinatorNode.cs
│       └── ParticipantNode.cs
└── Relogios_Lamport/
	├── Relogios_Lamport.csproj
	├── Program.cs
	├── Model/
	│   ├── EventRecord.cs
	│   └── LamportMessage.cs
	├── Network/
	│   └── TcpLamportClient.cs
	└── Core/
		├── EventAnalyzer.cs
		└── LamportProcess.cs
```

### Responsabilidades

| Projeto | Arquivo ou classe | Responsabilidade |
|---|---|---|
| Berkeley | `Program.cs` | Lê o papel (`node` ou `coord`), a porta, o desvio inicial e, para o coordenador, os endereços dos participantes. |
| Berkeley | `Models/BerkeleyMessage.cs` | Define o envelope JSON com tipo, horário em ticks e ajuste em milissegundos. |
| Berkeley | `Network/TcpMessenger.cs` | Abre conexões TCP para consultar participantes e enviar ajustes; aguarda uma linha de confirmação após cada ajuste. |
| Berkeley | `Roles/ParticipantNode.cs` | Escuta em uma porta, responde à consulta de horário e aplica ajustes ao relógio simulado. |
| Berkeley | `Roles/CoordinatorNode.cs` | Consulta os participantes, calcula a média das diferenças, ajusta seu relógio e envia os ajustes individuais. |
| Lamport | `Program.cs` | Lê o ID, a porta e os endereços dos demais processos; inicia o servidor e executa o roteiro de eventos e mensagens. |
| Lamport | `Model/EventRecord.cs` | Representa um evento com ID do processo, timestamp, tipo e descrição. |
| Lamport | `Model/LamportMessage.cs` | Representa mensagens `MSG` e relatórios `REPORT`, incluindo o histórico quando necessário. |
| Lamport | `Network/TcpLamportClient.cs` | Serializa e envia uma mensagem por conexão TCP. |
| Lamport | `Core/LamportProcess.cs` | Mantém o relógio lógico e o histórico, recebe e envia mensagens e reúne relatórios em P1. |
| Lamport | `Core/EventAnalyzer.cs` | Ordena os eventos recebidos e apresenta a tabela, os empates e uma explicação da causalidade. |

## Pré-requisitos e compilação

- SDK **.NET 8** instalado; confira com `dotnet --list-sdks`.
- Portas locais 5001–5004 e 6001–6004 disponíveis. Os exemplos usam `127.0.0.1`, portanto todos os processos devem estar na mesma máquina.
- Os comandos abaixo são para **PowerShell**, na raiz `SistemasDistribuidosUFRPE`. No Visual Studio, salve eventuais alterações antes de compilar.

Compile os dois projetos:

```powershell
dotnet build .\Algoritmo_Berkeley\Algoritmo_Berkeley.csproj
dotnet build .\Relogios_Lamport\Relogios_Lamport.csproj
```

O padrão é a configuração `Debug`; os comandos de execução abaixo usam as DLLs geradas em `bin\Debug\net8.0`. Abra cada terminal na raiz da solução e mantenha-o aberto durante a respectiva simulação. Execute apenas um projeto de cada vez para facilitar a observação.

## 1. Algoritmo de Berkeley — cinco terminais

O coordenador solicita o horário de cada participante (`GET_TIME` / `TIME_REPLY`), calcula a diferença de cada relógio em relação ao seu e obtém a média aritmética incluindo sua própria diferença, zero. Ele soma a média ao próprio relógio e envia a cada participante `ADJUST`, com ajuste **média − diferença do participante**. Após aplicar o ajuste, cada participante envia `ACK`.

Os relógios são **valores simulados em memória**: cada processo captura `DateTime.Now` na inicialização e acrescenta o desvio informado. O valor não avança com o tempo entre consultas ou ajustes; não há alteração do relógio do sistema, correção de latência de rede ou exclusão de valores discrepantes. Assim, os horários iniciais também dependem do instante em que cada processo é iniciado.

### Executar

Inicie os quatro participantes, cada comando em um terminal PowerShell separado:

**Terminal 1 — participante na porta 5001, desvio +10 s:**

```powershell
dotnet .\Algoritmo_Berkeley\bin\Debug\net8.0\Algoritmo_Berkeley.dll node 5001 10
```

**Terminal 2 — participante na porta 5002, desvio −15 s:**

```powershell
dotnet .\Algoritmo_Berkeley\bin\Debug\net8.0\Algoritmo_Berkeley.dll node 5002 -15
```

**Terminal 3 — participante na porta 5003, desvio +25 s:**

```powershell
dotnet .\Algoritmo_Berkeley\bin\Debug\net8.0\Algoritmo_Berkeley.dll node 5003 25
```

**Terminal 4 — participante na porta 5004, desvio −5 s:**

```powershell
dotnet .\Algoritmo_Berkeley\bin\Debug\net8.0\Algoritmo_Berkeley.dll node 5004 -5
```

Depois que os quatro participantes exibirem `Iniciado`, execute no quinto terminal:

**Terminal 5 — coordenador, desvio 0 s:**

```powershell
dotnet .\Algoritmo_Berkeley\bin\Debug\net8.0\Algoritmo_Berkeley.dll coord 5000 0 '127.0.0.1:5001,127.0.0.1:5002,127.0.0.1:5003,127.0.0.1:5004'
```

Pressione **Enter no terminal do coordenador** para iniciar a rodada. Observe as diferenças, a média e o relógio antes/depois no coordenador; nos participantes, observe o ajuste e o relógio antes/depois. Ao final, o coordenador encerra e os participantes continuam aguardando conexões: use `Ctrl+C` em cada terminal para encerrá-los.

A porta `5000` é informada ao coordenador e exibida nos logs; nesta implementação ele atua como cliente e **não abre um servidor nessa porta**. As portas 5001–5004 são efetivamente escutadas pelos participantes.

## 2. Relógios lógicos de Lamport — quatro terminais

Cada processo inicia seu contador em zero. O relógio lógico não representa horário real:

1. **Evento local:** incrementa o contador antes de registrar o evento.
2. **Envio:** incrementa o contador e envia seu valor como timestamp na mensagem `MSG`.
3. **Recebimento:** atualiza o contador para `max(contador local, timestamp recebido) + 1` e registra o evento.

O roteiro do programa envia mensagens de P1 para P2, de P2 para P3 e P4, de P3 para P1 e de P4 para P2. Ao final, P2, P3 e P4 enviam relatórios `REPORT` com seus históricos para P1. P1 inclui seu próprio histórico e exibe a tabela consolidada.

### Executar

Abra quatro terminais PowerShell na raiz e execute um comando em cada um. O terceiro argumento lista os **outros três processos** no formato `ID:IP:PORTA`:

**Terminal 1 — P1, líder de consolidação, porta 6001:**

```powershell
dotnet .\Relogios_Lamport\bin\Debug\net8.0\Relogios_Lamport.dll 1 6001 '2:127.0.0.1:6002,3:127.0.0.1:6003,4:127.0.0.1:6004'
```

**Terminal 2 — P2, porta 6002:**

```powershell
dotnet .\Relogios_Lamport\bin\Debug\net8.0\Relogios_Lamport.dll 2 6002 '1:127.0.0.1:6001,3:127.0.0.1:6003,4:127.0.0.1:6004'
```

**Terminal 3 — P3, porta 6003:**

```powershell
dotnet .\Relogios_Lamport\bin\Debug\net8.0\Relogios_Lamport.dll 3 6003 '1:127.0.0.1:6001,2:127.0.0.1:6002,4:127.0.0.1:6004'
```

**Terminal 4 — P4, porta 6004:**

```powershell
dotnet .\Relogios_Lamport\bin\Debug\net8.0\Relogios_Lamport.dll 4 6004 '1:127.0.0.1:6001,2:127.0.0.1:6002,3:127.0.0.1:6003'
```

1. Espere os quatro terminais mostrarem que os processos foram iniciados.
2. Pressione **Enter em cada terminal, em rápida sequência**, para liberar o roteiro. O início não é uma barreira automática: os processos começam individualmente ao receber Enter, e a simulação usa esperas fixas; demoras ou atrasos de rede podem afetar a coleta dos históricos.
3. Aguarde os relatórios e a tabela global no terminal de P1. Os demais terminais informam que enviaram seu histórico para P1.
4. Quando a tabela aparecer, pressione **Enter em cada terminal** para encerrar. Mantenha P1 aberto até receber os relatórios; se um relatório não chegar, P1 continuará aguardando.

### Desempate e causalidade

A tabela de `EventAnalyzer` ordena primeiro pelo **timestamp lógico crescente** e, em caso de empate, pelo **ID crescente do processo**: P1 antes de P2, P2 antes de P3 e P3 antes de P4. Por exemplo, `(L=3, P1)` aparece antes de `(L=3, P2)`. Esse desempate estabelece uma ordem total determinística para a apresentação; ele não significa que um evento causou o outro.

A relação *happened-before* (`a → b`) é definida pela ordem dos eventos no mesmo processo, pelo envio de uma mensagem antes de seu recebimento e pela transitividade dessas relações. As regras do relógio garantem que **se `a → b`, então `L(a) < L(b)`**. A recíproca não é necessariamente verdadeira: `L(a) < L(b)` não prova que `a` causou `b`. Eventos em processos diferentes sem caminho causal são concorrentes e podem receber timestamps iguais ou diferentes; apenas os timestamps de Lamport não bastam para identificar toda concorrência.

## Evidências de execução

Espaço reservado para acrescentar capturas ou links reais após executar os projetos:

- **Berkeley:** captura dos cinco terminais, mostrando os relógios antes/depois, a média e os ajustes.
- <img width="2246" height="1239" alt="image" src="https://github.com/user-attachments/assets/fe0a2b81-0dd1-486f-a7b6-6348d2667619" />
- <img width="2258" height="1225" alt="image" src="https://github.com/user-attachments/assets/31c3b747-c2b8-4779-8c82-3eda68c6ed05" />


- **Lamport:** captura dos quatro terminais e da tabela global em P1, incluindo empates e a explicação de causalidade.
- <img width="2325" height="1200" alt="image" src="https://github.com/user-attachments/assets/eec2cda5-c60e-42ad-a465-000a7f12ba53" />

