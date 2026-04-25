# Especificação de Produto - NameScan MVP

## Contexto

NameScan é um web app gratuito para verificar se um nickname, handle, nome de projeto ou nome de marca parece disponível em redes sociais, plataformas digitais e domínios.

O escopo inicial é Brasil primeiro. A interface, os textos, as mensagens de suporte e o fluxo de apoio voluntário devem ser em português, com o domínio `.com.br` tratado como alvo de primeira classe.

## Promessa Do Produto

O usuário digita um nome e vê, de forma progressiva, onde ele parece estar disponível, ocupado, inválido, inconclusivo ou com erro temporário.

O produto não deve prometer certeza absoluta. O valor do NameScan é ajudar a comparar plataformas rapidamente, com links diretos, níveis de confiança, observações curtas e sugestões alternativas simples.

## Público-Alvo Inicial

- Criadores brasileiros construindo uma marca pessoal.
- Pequenos negócios escolhendo nome para presença digital.
- Fundadores validando nome de startup, produto ou comunidade.
- Designers, social media e agências testando nomes para clientes.
- Desenvolvedores nomeando projetos, pacotes ou ferramentas.
- Gamers e streamers buscando consistência de nickname.

## Objetivos Do MVP

O MVP deve permitir que o usuário:

1. Digite um nickname ou handle de marca.
2. Inicie a verificação em uma tela principal.
3. Acompanhe os resultados chegando progressivamente.
4. Veja status, confiança, URL testada e link direto para cada plataforma.
5. Receba sugestões alternativas simples.
6. Copie um relatório em texto.
7. Leia um aviso claro sobre as limitações dos resultados.
8. Apoie o projeto via Apoia.se.

## Escopo De Plataformas Do MVP

Os alvos obrigatórios do MVP são:

- Instagram
- TikTok
- X
- YouTube
- GitHub
- Twitch
- `.com`
- `.com.br`

Reddit, Pinterest e Medium não são obrigatórios para o lançamento do MVP. Eles podem ser adicionados se o núcleo do produto ficar pronto antes do previsto, mas devem ser tratados como expansão pós-MVP.

## Status Dos Resultados

Cada resultado de plataforma deve usar um dos seguintes status:

- `Disponível`: o nome parece disponível com confiança suficiente.
- `Ocupado`: há sinal forte de que o perfil, handle ou domínio existe.
- `Inválido`: o nome não atende às regras daquela plataforma.
- `Inconclusivo`: o app não consegue determinar disponibilidade com confiança.
- `Erro`: houve falha técnica temporária.

Cada resultado deve conter:

- nome da plataforma;
- URL testada;
- status;
- nível de confiança: `Baixa`, `Média` ou `Alta`;
- observação curta opcional;
- link direto para conferência manual.

## Estratégia De Verificação

O NameScan usa uma estratégia híbrida de verificação.

Plataformas e domínios mais estáveis podem retornar `Disponível` ou `Ocupado` quando houver sinais públicos fortes. Plataformas ambíguas devem preferir `Inconclusivo` em vez de inventar certeza.

Cada verificador de plataforma deve:

- construir a URL pública ou alvo de consulta;
- validar regras específicas da plataforma quando isso for prático;
- fazer uma requisição HTTP pública ou consulta de domínio sem login;
- interpretar status codes, redirects e sinais mínimos de conteúdo;
- usar timeout individual;
- retornar `Inconclusivo` quando houver bloqueio, rate limit, redirect ambíguo ou conteúdo genérico.

O app não deve pedir credenciais de redes sociais, depender de scraping autenticado nem tentar contornar proteções agressivas das plataformas.

## Fluxo Principal

1. O usuário abre a página inicial.
2. O usuário digita um nickname, por exemplo `minhamarca`.
3. O usuário clica em `Verificar`.
4. O frontend valida o formato básico antes de abrir o stream.
5. O backend valida o nickname novamente.
6. A UI renderiza todos os alvos obrigatórios no estado `Verificando`.
7. O backend verifica as plataformas em paralelo.
8. Os resultados chegam via SSE e atualizam a interface uma plataforma por vez.
9. Quando o stream termina, a UI mostra sugestões e habilita a cópia do relatório.
10. O usuário pode abrir links diretos, copiar o relatório ou apoiar o projeto via Apoia.se.

## Experiência De Interface

A interface deve ser implementada em português com Blazor e componentes MudBlazor. Ela deve ser direta, responsiva e focada em utilidade.

Uso esperado de MudBlazor:

- `MudTextField` para o campo de nickname.
- `MudButton` para a ação principal de verificação.
- `MudTable` ou `MudList` para os resultados por plataforma.
- `MudChip` ou `MudBadge` para status e confiança.
- `MudProgressLinear` ou indicadores por plataforma durante a checagem.
- `MudAlert` para avisos de limitação, validação e erros gerais.
- `MudSnackbar` para feedback de relatório copiado.
- `MudCard` apenas para blocos repetidos ou áreas realmente enquadradas.

A UI deve cobrir estes estados:

- estado inicial;
- validando;
- verificando progressivamente;
- resultado parcial;
- resultado completo;
- nickname inválido;
- erro geral de stream;
- plataforma individual inconclusiva ou com erro.

A primeira tela deve conter:

- nome NameScan ou logo simples;
- campo principal de nickname;
- botão `Verificar`;
- aviso conciso de limitação dos resultados;
- área de resultados;
- área de sugestões;
- botão `Copiar relatório` após existir resultado;
- link discreto para Apoia.se.

## Arquitetura Backend

O backend deve usar C# com ASP.NET Core.

Estrutura recomendada:

```text
/NameScan
  /Components
    /Pages
      Home.razor
  /wwwroot
    /css
      app.css
    /js
      check-stream.js
  /Features
    /Checks
      CheckRequest.cs
      CheckResponse.cs
      PlatformCheckResult.cs
      CheckStatus.cs
      ConfidenceLevel.cs
      HandleCheckService.cs
      CheckStreamEvent.cs
    /Suggestions
      SuggestionService.cs
  /Platforms
    IPlatformChecker.cs
    InstagramChecker.cs
    TikTokChecker.cs
    XChecker.cs
    YouTubeChecker.cs
    GitHubChecker.cs
    TwitchChecker.cs
    DotComDomainChecker.cs
    DotComBrDomainChecker.cs
  /Validation
    NicknameValidator.cs
  Program.cs
```

Responsabilidades principais:

- `NicknameValidator`: faz a validação geral do nickname.
- `IPlatformChecker`: define o contrato dos verificadores.
- Classes de plataforma: implementam regras e consultas específicas.
- `HandleCheckService`: orquestra execução paralela, cache, timeout e streaming.
- `SuggestionService`: gera alternativas simples por regra.

## Contrato SSE

O MVP deve transmitir os resultados com Server-Sent Events.

Endpoint recomendado:

```http
GET /api/check/stream?nickname=minhamarca
```

O stream deve emitir:

```text
event: result
data: { "platform": "GitHub", "url": "https://github.com/minhamarca", "status": "Disponível", "confidence": "Alta", "note": "Perfil não encontrado" }

event: done
data: { "query": "minhamarca", "suggestions": ["useminhamarca", "minhamarcaapp"], "summary": { "available": 4, "occupied": 2, "inconclusive": 2 } }

event: error
data: { "message": "Não foi possível iniciar a verificação." }
```

O frontend deve manter falhas individuais isoladas. Uma plataforma com falha deve atualizar sua linha para `Erro` ou `Inconclusivo`; ela não deve cancelar toda a busca, exceto quando a requisição inicial for inválida ou o stream não puder começar.

## Contrato Interno Dos Verificadores

```csharp
public interface IPlatformChecker
{
    string Id { get; }
    string Name { get; }
    Task<PlatformCheckResult> CheckAsync(string nickname, CancellationToken cancellationToken);
}
```

```csharp
public sealed record PlatformCheckResult(
    string Platform,
    string Url,
    CheckStatus Status,
    ConfidenceLevel Confidence,
    string? Note
);
```

```csharp
public enum CheckStatus
{
    Available,
    Occupied,
    Invalid,
    Inconclusive,
    Error
}

public enum ConfidenceLevel
{
    Low,
    Medium,
    High
}
```

Os enums podem ser mantidos em inglês no código, mas a interface deve traduzir os rótulos para português.

## Validação

A validação geral deve:

- remover espaços no início e no fim;
- rejeitar busca vazia;
- limitar o tamanho entre 2 e 30 caracteres;
- aceitar letras, números, ponto, underline e hífen;
- explicar que cada plataforma pode ter regras próprias.

Validações específicas podem retornar `Inválido` para uma plataforma mesmo quando a entrada geral for aceita.

## Sugestões Alternativas

As sugestões devem ser geradas por regras simples no backend. IA não é necessária no MVP.

Para `minhamarca`, exemplos incluem:

- `minhamarcaapp`
- `useminhamarca`
- `minhamarcaoficial`
- `minhamarca_io`
- `getminhamarca`
- `minhamarcaHQ`
- `minhamarcaBrasil`

As sugestões devem aparecer depois da conclusão da busca. Elas não precisam ser verificadas automaticamente no MVP.

## Relatório Copiável

O usuário deve poder copiar um relatório em texto simples.

Exemplo:

```text
Resultado para: minhamarca

Instagram: Ocupado
TikTok: Disponível
X: Ocupado
YouTube: Inconclusivo
GitHub: Disponível
Twitch: Disponível
.com: Ocupado
.com.br: Disponível
```

Depois da cópia, a UI deve mostrar um feedback curto com `MudSnackbar`.

## Apoio Voluntário

O app será gratuito. O MVP deve incluir um link discreto, mas visível, para o Apoia.se.

Texto sugerido:

```text
Este projeto é gratuito. Se ele te ajudou, considere apoiar pelo Apoia.se.
```

O MVP não deve incluir pagamentos obrigatórios, planos pagos, criação de conta ou processamento de pagamento dentro do app.

## Requisitos Não Funcionais

### Performance

- O primeiro resultado deve aparecer assim que estiver disponível.
- Cada plataforma deve ter timeout individual.
- A verificação completa deve normalmente terminar em 5 a 10 segundos.

### Confiabilidade

- Preferir `Inconclusivo` em vez de falsa certeza.
- Falha em uma plataforma não deve quebrar os outros resultados.
- A UI deve continuar utilizável se o stream terminar cedo ou uma plataforma expirar.

### Cache

- Cachear resultados por nickname e plataforma.
- Usar `IMemoryCache` no MVP.
- Usar TTL entre 1 e 6 horas.
- Manter a arquitetura compatível com Redis no futuro.

### Privacidade E Segurança

- Não pedir credenciais de redes sociais.
- Não armazenar dados pessoais desnecessários.
- Não prometer reserva de nomes.
- Exibir aviso claro de limitação.

Texto sugerido:

```text
Os resultados são estimativas baseadas em verificações públicas. Confirme manualmente antes de tomar uma decisão final.
```

## Métricas

O MVP pode acompanhar métricas por logs estruturados:

- buscas por dia;
- contagem de resultados por status;
- taxa de erro por plataforma;
- tempo médio de resposta por plataforma;
- eventos de relatório copiado;
- cliques em links de resultado;
- cliques no link do Apoia.se.

Os logs devem evitar armazenamento de dados pessoais desnecessários.

## Critérios De Aceite

O MVP estará pronto quando:

- o usuário conseguir buscar um nickname na tela principal;
- todos os 8 alvos obrigatórios forem verificados;
- os resultados chegarem progressivamente via SSE;
- cada resultado mostrar status, confiança, URL testada, observação quando necessário e link direto;
- nickname inválido for tratado claramente;
- timeout ou falha de plataforma não quebrar a página inteira;
- sugestões simples aparecerem ao final;
- o usuário conseguir copiar um relatório em texto;
- a página exibir aviso de limitação;
- a página incluir link de apoio via Apoia.se;
- a interface funcionar bem em desktop e mobile;
- o backend estiver implementado em C# com ASP.NET Core;
- o frontend usar Blazor com MudBlazor.

## Fora Do Escopo Do MVP

- login de usuário;
- histórico permanente de buscas;
- monitoramento recorrente;
- alertas quando um nome ficar disponível;
- API pública;
- dashboard administrativo;
- análise de branding com IA;
- extensão de navegador;
- pagamento obrigatório;
- planos pagos;
- scraping agressivo;
- verificações autenticadas em plataformas.

## Estratégia De Testes

A implementação deve incluir:

- testes unitários do `NicknameValidator`;
- testes unitários do `SuggestionService`;
- testes dos mapeamentos de status e confiança nos verificadores;
- testes do `HandleCheckService` provando independência entre plataformas e tratamento de timeout;
- teste do endpoint SSE validando eventos `result`, `done` e `error`;
- teste de UI ou componente para busca, atualização progressiva, entrada inválida e cópia de relatório.

## Roadmap

### Versão 1.1

- seleção manual de plataformas;
- verificadores para Reddit, Pinterest e Medium;
- mais extensões de domínio;
- controles melhores de cache.

### Versão 1.2

- histórico local no navegador;
- comparação entre vários nicknames;
- página compartilhável de resultado;
- pontuação de consistência do nome.

### Versão 1.3

- alertas opcionais por e-mail;
- verificações de pacotes npm, PyPI e NuGet;
- integração mais rica com Apoia.se.

### Futuro

- API pública com limites justos;
- extensão de navegador;
- análise de branding;
- monitoramento recorrente.
