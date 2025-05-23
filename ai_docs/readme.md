# EasyETL Project Documentation

Este documento fornece uma visão geral do projeto EasyETL, com base na estrutura de arquivos e pastas do repositório.

## Visão Geral do Projeto

O EasyETL parece ser uma biblioteca .NET projetada para facilitar processos de Extract, Transform, Load (ETL). Ele fornece funcionalidades para extrair dados de diversas fontes, transformá-los e carregá-los em diferentes destinos.

## Estrutura do Repositório

O repositório está organizado da seguinte forma:

- **`.github/`**: Contém templates para issues (relatos de bugs, solicitações de features).
  - `ISSUE_TEMPLATE/`
    - `bug_report.md`
    - `feature_request.md`
- **`ai_docs/`**: Contém esta documentação gerada por IA.
  - `readme.md`
- **`examples/`**: Fornece exemplos práticos de como utilizar a biblioteca EasyETL.
  - `ExtractCsvToSQLiteTable.cs`: Exemplo de extração de CSV para uma tabela SQLite.
  - `ExtractCsvToSqlTable.cs`: Exemplo de extração de CSV para uma tabela SQL.
  - `ExtractParquetToSql.cs`: Exemplo de extração de Parquet para SQL.
  - `ExtractSQLToJsonL.cs`: Exemplo de extração de SQL para JSONL.
  - `ExtractSQLiteToJsonL.cs`: Exemplo de extração de SQLite para JSONL.
  - `Playground.csproj`: Arquivo de projeto para os exemplos.
  - `Program.cs`: Ponto de entrada para execução dos exemplos.
  - `ReadFileProgress.cs`: Exemplo de como acompanhar o progresso da leitura de arquivos.
- **`src/`**: Contém o código fonte principal da biblioteca.
  - `Library/`
    - `EasyEtl.cs`: Provavelmente o arquivo principal da biblioteca ou um ponto de entrada para suas funcionalidades.
    - `Extractors/`: Componentes responsáveis pela extração de dados.
    - `Infra/`: Código de infraestrutura, como classes de progresso (`EtlDataProgress.cs`) e eventos (`EasyEtlNotificationEventArgs.cs`).
    - `Library.csproj`: Arquivo de projeto da biblioteca.
    - `Loaders/`: Componentes responsáveis pelo carregamento de dados.
    - `Transformers/`: Componentes responsáveis pela transformação de dados.
- **`tests/`**: Contém os testes do projeto.
  - `Benchmark/`: Testes de performance.
    - `CsvToSqlBenchmark.cs`: Benchmark específico para a operação de CSV para SQL.
  - `UnitTests/`: Testes unitários para garantir a corretude dos componentes.
    - `Configs/`: Configurações para testes, incluindo arquivos de recurso como `StaticFiles.resx`.
    - `Resources/`: Recursos para testes, como o `default_reader_config.txt` que define a configuração de um leitor de dados.
- `.gitignore`: Especifica arquivos e pastas a serem ignorados pelo Git.
- `EasyETL.sln`: Arquivo de solução do Visual Studio para o projeto.

## Principais Componentes e Funcionalidades (Detalhado)

A exploração do código em `src/Library/` revelou os seguintes detalhes sobre os componentes principais:

- **`EasyEtl.cs`**: Funciona como o orquestrador central do processo ETL. Ele gerencia o fluxo de dados entre os componentes de extração, transformação e carga utilizando `System.Threading.Channels` para uma comunicação assíncrona eficiente. A classe expõe eventos cruciais para o monitoramento do processo:
  - `OnChange`: Notifica sobre o progresso das operações.
  - `OnComplete`: Indica a conclusão bem-sucedida do processo ETL.
  - `OnError`: Sinaliza a ocorrência de erros durante a execução.
  Uma característica notável é sua flexibilidade, permitindo a execução do pipeline ETL mesmo sem um transformador de dados explicitamente fornecido; nesse caso, um `BypassDataTransformer` é utilizado por padrão.

- **Interfaces Fundamentais**:
  - **`IDataExtractor`**: Define o contrato para todos os componentes responsáveis pela extração de dados. Sua principal responsabilidade é ler dados de uma fonte e emitir cada registro (geralmente como um `Dictionary<string, object?>`). A interface inclui:
    - Eventos: `OnRead` (para cada leitura), `OnFinish` (ao concluir a extração), `OnError` (para erros de extração).
    - Propriedades de Progresso: `TotalLines`, `LineNumber`, `BytesRead`, `PercentRead`, `FileSize`.
    - Método Principal: `Extract(RowAction processRow)`.
  - **`IDataLoader`**: Estabelece o contrato para os componentes de carregamento de dados. Estes componentes recebem os dados (já transformados) e os persistem em um destino. A interface inclui:
    - Eventos: `OnWrite` (para cada escrita), `OnFinish` (ao concluir o carregamento), `OnError` (para erros de carregamento).
    - Propriedades de Progresso: `CurrentLine`, `TotalLines`, `PercentWritten`.
    - Método Principal: `Task Load(IAsyncEnumerable<Dictionary<string, object?>> data, CancellationToken cancellationToken)`.
  - **`IDataTransformer`**: Especifica o contrato para os componentes de transformação de dados. São responsáveis por receber os dados extraídos, aplicar as lógicas de transformação necessárias e emitir os dados resultantes. A interface inclui:
    - Eventos: `OnTransform` (para cada transformação), `OnFinish` (ao concluir a transformação), `OnError` (para erros de transformação).
    - Propriedades de Progresso: `IngestedLines`, `TransformedLines`, `ExcludedByFilter`, `PercentDone`, `TotalLines`.
    - Métodos Principais: `IAsyncEnumerable<Dictionary<string, object?>> Transform(IAsyncEnumerable<Dictionary<string, object?>> data, CancellationToken cancellationToken)` e `List<Dictionary<string, object?>> ApplyTransformations(Dictionary<string, object?> item)`.

- **Componentes de Infraestrutura**:
  - **`EtlDataProgress.cs`**: Uma classe de dados que encapsula o estado do progresso de uma etapa específica do ETL (extração, transformação ou carga) ou do processo global. Contém informações como `CurrentLine`, `TotalLines`, `PercentComplete`, `Status` (definido pelo enum `EtlStatus`: Running, Completed, Failed), `Speed` e `EstimatedTimeToEnd`. O enum `EtlType` (Extract, Transform, Load, Global) é usado para categorizar o progresso.
  - **`EasyEtlTelemetry.cs`**: Responsável por monitorar e reportar o progresso e o status de cada estágio do processo ETL. Esta classe se inscreve nos eventos emitidos pelos componentes `IDataExtractor`, `IDataTransformer` e `IDataLoader`. Ao receber essas notificações, atualiza o `EtlDataProgress` correspondente a cada etapa e, em seguida, dispara seus próprios eventos consolidados (`OnChange`, `OnError`) para o consumidor da biblioteca `EasyEtl`.

- **Implementações de Transformadores**:
  - **`BypassDataTransformer.cs`**: Uma implementação da interface `IDataTransformer` que atua como um "passa-através". Ele não realiza nenhuma modificação nos dados, simplesmente os encaminha da entrada para a saída. É útil em cenários onde os dados já estão no formato desejado e nenhuma transformação é necessária.
  - **`DynamicDataTransformer.cs`**: Uma implementação mais complexa de `IDataTransformer` que permite aplicar transformações de forma dinâmica. As transformações são definidas através de um objeto `TransformationConfig`. Esta configuração permite especificar filtros condicionais e uma série de ações de mapeamento de campos, onde tanto as condições quanto os valores dos campos podem ser avaliados dinamicamente em tempo de execução.

## Casos de Uso e Comportamentos Esperados (Baseado em Testes)

A análise dos testes unitários e de integração revelou os seguintes cenários de uso e comportamentos chave da biblioteca EasyETL:

- **Extração de Dados (CSV):**
  - Leitura básica de arquivos CSV, validando a contagem de linhas.
  - Desempenho adequado na leitura de grandes volumes de dados (e.g., 100.000 linhas em menos de 1 segundo).
  - Tratamento robusto de erros, incluindo:
    - `FormatException` para dados malformados no CSV.
    - `FileNotFoundException` para arquivos de origem não encontrados.
    - `ArgumentNullException` se a configuração do extrator for nula.
  - As propriedades públicas do extrator (como `LineNumber`, `FileSize`, `BytesRead`, `PercentRead`) refletem corretamente o estado da extração após a conclusão.
  - Capacidade de parsear diversos tipos de dados de strings CSV para tipos .NET correspondentes (e.g., `int`, `double`, `DateTime`, `bool`, `Guid`).

- **Carregamento de Dados (CSV):**
  - Emissão correta dos eventos `OnWrite` (durante o processo) e `OnFinish` (ao final) para acompanhamento do progresso.
  - A frequência do evento `OnWrite` é configurável através de `RaiseChangeEventAfer`.
  - O evento `OnError` é disparado em caso de falhas, como tentativa de escrita em um caminho inválido ou ao encontrar tipos de dados não suportados para serialização em CSV.

- **Transformação de Dados (Dinâmica):**
  - Aplicação de transformações condicionais: as regras de transformação são executadas somente se a condição especificada para um registro for atendida.
  - Capacidade de copiar dinamicamente valores de campos de origem para campos de destino.
  - Suporte à aplicação de múltiplas ações de transformação em uma única linha de dados (e.g., modificar um valor e, em seguida, duplicar a linha com outra modificação).
  - Se nenhuma transformação for explicitamente configurada, os dados passam pelo transformador inalterados.
  - Notificação de progresso através do evento `OnTransform` durante o processo de transformação.

- **Pipeline ETL Completo (Integração):**
  - Demonstração de um fluxo ETL completo: extração de dados de um CSV, aplicação de transformações dinâmicas e carregamento dos dados resultantes em formato JSON.
  - Verificação da correta emissão dos eventos `OnFinish` para cada etapa (extração, transformação, carga), com validação das contagens de linhas e percentuais de progresso.
  - Confirmação de que o evento `OnComplete` do `EasyEtl` é disparado ao final de um processo bem-sucedido.
  - Utilização de arquivos de configuração externos (JSON) para definir o comportamento dos extratores, transformadores e carregadores, mostrando a flexibilidade da biblioteca.

## Análise de Dependências (src/Library/Library.csproj)

Esta seção detalha as dependências diretas do projeto principal `Library.csproj`, suas versões atuais conforme declaradas no arquivo de projeto e as versões mais recentes identificadas.

| Dependências                        | Versão Atual | Versão Mais Recente | Observações                               |
|-------------------------------------|--------------|---------------------|-------------------------------------------|
| Ardalis.GuardClauses                | 4.0.1        | 4.5.0               |                                           |
| JsonStreamer.NewtonsoftJson.Client  | 2.3.2        | 2.3.2               | Nenhuma versão mais recente encontrada.   |
| Microsoft.Data.Sqlite               | 7.0.5        | 9.0.5               |                                           |
| Microsoft.Data.Sqlite.Core          | 7.0.5        | 9.0.5               |                                           |
| Newtonsoft.Json                     | 13.0.3       | 13.0.3              | Já está na versão mais recente.           |

---

*Este README foi gerado por IA com base na análise da estrutura do projeto.*