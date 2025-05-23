# Manual de Atualização de Dependências

## Passo 1: Atualizar a Versão do .NET para 9

1. Abra um terminal no diretório do projeto.
2. Execute o comando para atualizar o arquivo de projeto para a versão 9 do .NET:
   ```sh
   dotnet new --force -n MyProject -f net9.0
   ```
3. Atualize o arquivo `.csproj` para usar o SDK do .NET 9:
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>net9.0</TargetFramework>
       <ImplicitUsings>enable</ImplicitUsings>
       <Nullable>enable</Nullable>
     </PropertyGroup>
   </Project>
   ```

## Passo 2: Atualizar os Pacotes NuGet

1. Execute o comando para atualizar todos os pacotes NuGet para as versões mais recentes:
   ```sh
   dotnet list package --outdated
   ```
2. Atualize os pacotes individualmente:
   ```sh
   dotnet add package <package_name> --version <latest_version>
   ```
3. Verifique as atualizações e faça os ajustes necessários no código, se necessário.

## Passo 3: Usar Context7 para Buscar Pacotes Atuais

1. Execute o comando para buscar pacotes atuais usando context7:
   ```sh
   context7 search <package_name>
   ```
2. Atualize os pacotes conforme necessário.

## Passo 4: Testar o Projeto

1. Execute os testes para garantir que tudo está funcionando corretamente:
   ```sh
   dotnet test
   ```
2. Corrija quaisquer erros ou problemas que surgirem durante os testes.

## Passo 5: Comitar as Alterações

1. Adicione as alterações ao repositório:
   ```sh
   git add .
   ```
2. Comite as alterações com uma mensagem descritiva:
   ```sh
   git commit -m "Atualizado para .NET 9 e pacotes NuGet mais recentes"
   ```
3. Faça o push das alterações para o repositório remoto:
   ```sh
   git push origin main
