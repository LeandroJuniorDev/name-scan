# Especificação De Design - Adoção Do .NET Aspire No NameScan

## Contexto

O `NameScan` está atualmente estruturado como um único web app em ASP.NET Core + Blazor, com endpoint SSE para streaming progressivo dos resultados, uso de `HttpClientFactory`, `IMemoryCache`, MudBlazor e suíte de testes separada.

Esse estado representa o MVP funcional do produto. Antes de qualquer mudança estrutural ligada a `.NET Aspire`, o histórico do repositório deve marcar explicitamente esse ponto como o marco do MVP.

## Objetivo Desta Mudança

Adicionar `.NET Aspire` ao projeto com uma abordagem intermediária que:

- melhore a experiência de desenvolvimento local;
- organize observabilidade, configuração e health checks;
- deixe um caminho claro para publicação e hospedagem;
- preserve a simplicidade arquitetural do MVP;
- funcione como ambiente de estudo e experimentação controlada do ecossistema Aspire.

Esta mudança não tem como objetivo transformar o NameScan em uma arquitetura de múltiplos serviços neste momento.

## Princípios De Design

- O `NameScan` continua sendo a aplicação de produto principal.
- A experiência do usuário final não deve mudar por causa da adoção do Aspire.
- O fluxo principal do MVP deve continuar simples, rápido e em português.
- Adoção de Aspire deve priorizar valor operacional real, não complexidade especulativa.
- A solução deve continuar executável de forma previsível tanto no fluxo tradicional quanto no fluxo orquestrado por Aspire, quando isso for útil.
- O rollout deve ser incremental e reversível por etapas.

## Escopo

### Incluído

- criação de um projeto `NameScan.AppHost`;
- criação de um projeto `NameScan.ServiceDefaults`;
- integração do `NameScan` com os defaults compartilhados do Aspire;
- configuração de telemetria, logging estruturado, health checks e defaults operacionais;
- trilha inicial para publicação e hospedagem compatível com Aspire;
- documentação de execução local e de responsabilidades dos novos projetos;
- marcação do estado pré-Aspire do repositório com a tag `v1.0.0-mvp`.

### Não Incluído

- decomposição do produto em microserviços;
- extração de domínio para serviços separados sem necessidade clara;
- criação de banco de dados, filas, cache distribuído ou infraestrutura adicional apenas para “demonstrar” o Aspire;
- mudança do fluxo de produto, da copy da UI ou do comportamento funcional do SSE por razões arquiteturais.

## Estado Atual A Ser Preservado

O repositório hoje tem estas características centrais:

- aplicação web única no projeto `NameScan`;
- streaming progressivo via `/api/check/stream`;
- lógica de domínio organizada por `Features`, `Platforms` e `Validation`;
- UI principal em `Home.razor`;
- projeto de testes em `NameScan.Tests`.

Esses elementos continuam sendo a espinha dorsal do MVP. O Aspire entra como camada de composição e operação, não como reescrita do produto.

## Abordagem Escolhida

A adoção seguirá a abordagem recomendada de “camada de composição sobre o app atual”, com um toque mínimo de infraestrutura compartilhada:

- `NameScan` permanece como web app principal;
- `NameScan.AppHost` passa a ser o ponto preferencial de execução local com Aspire;
- `NameScan.ServiceDefaults` centraliza defaults transversais;
- qualquer extração adicional deve ficar restrita a preocupações operacionais claramente compartilhadas.

Essa abordagem equilibra aprendizado, utilidade prática e baixo risco de regressão.

## Estrutura Proposta Da Solução

```text
/NameScan.sln
/NameScan
/NameScan.AppHost
/NameScan.ServiceDefaults
/NameScan.Tests
/docs/superpowers/specs
/docs/superpowers/plans
```

### Responsabilidades De `NameScan`

O projeto `NameScan` continua responsável por:

- interface Blazor e MudBlazor;
- endpoint SSE `/api/check/stream`;
- validação de nickname;
- orquestração de checks;
- checkers de plataformas e domínios;
- sugestões e formatação de relatório.

O `Program.cs` deste projeto deve ser simplificado para consumir os defaults compartilhados do Aspire sem absorver excesso de configuração operacional.

### Responsabilidades De `NameScan.AppHost`

O projeto `NameScan.AppHost` será responsável por:

- orquestrar a execução local do `NameScan`;
- concentrar a experiência de subir o sistema com Aspire;
- expor dashboard e visão integrada de observabilidade;
- preparar a composição do ambiente para publicação futura;
- servir como ponto controlado para estudar capacidades do Aspire sem contaminar a lógica de domínio.

O `AppHost` não deve passar a conter regras de negócio do NameScan.

### Responsabilidades De `NameScan.ServiceDefaults`

O projeto `NameScan.ServiceDefaults` será responsável por:

- registrar OpenTelemetry;
- padronizar logging estruturado;
- registrar health checks;
- aplicar defaults de service discovery quando relevantes;
- consolidar configuração operacional compartilhada;
- oferecer extensões reutilizáveis para o `Program.cs` do `NameScan`.

Esse projeto deve permanecer enxuto e focado em infraestrutura transversal.

## Observabilidade E Operação

O primeiro ganho concreto esperado com Aspire deve ser observabilidade melhor do fluxo já existente.

### Telemetria

Devem ser priorizados sinais que ajudem a entender o comportamento real do MVP:

- duração total de uma verificação;
- tempo por plataforma/domínio;
- quantidade de resultados `Available`, `Occupied`, `Invalid`, `Inconclusive` e `Error`;
- falhas técnicas por checker;
- erros de bootstrap do stream;
- volume de execuções iniciadas e concluídas.

### Logging

Os logs devem permitir correlacionar:

- início de uma verificação;
- resultados emitidos progressivamente;
- conclusão do stream;
- falhas isoladas por plataforma;
- eventos de interface já existentes que façam sentido manter.

O objetivo é melhorar diagnóstico, não gerar ruído excessivo.

### Health Checks

Health checks devem cobrir o que realmente reflete prontidão do app:

- disponibilidade da aplicação web;
- readiness básica do pipeline e dos serviços registrados;
- ausência de dependência obrigatória fictícia.

Como o app ainda não depende de banco ou fila, os health checks devem permanecer simples.

## Relação Com O Fluxo Atual De SSE

O endpoint `/api/check/stream` continua parte central do produto.

A adoção do Aspire não deve:

- alterar o contrato funcional esperado pelo frontend;
- remover o comportamento progressivo do stream;
- concentrar todos os resultados apenas no final;
- acoplar o fluxo do usuário a novas dependências externas.

O que pode mudar é a instrumentação ao redor do fluxo:

- métricas de duração;
- traces de execução;
- logs com correlação;
- configuração mais clara por ambiente.

## Configuração E Ambientes

O design deve permitir que:

- o projeto continue simples de rodar em desenvolvimento;
- o fluxo com Aspire seja o caminho principal recomendado;
- o `NameScan` ainda possa ser executado isoladamente quando útil para desenvolvimento focado;
- a configuração de ambiente seja explícita e previsível.

Se houver variáveis ou configurações operacionais novas, elas devem ser documentadas com clareza e com defaults seguros para o contexto atual do MVP.

## Estratégia De Publicação E Hospedagem

Esta mudança deve deixar o projeto “deploy-ready” no sentido de arquitetura e configuração, mesmo sem exigir um destino de infraestrutura específico desde já.

Isso significa:

- preparar o caminho para publicação via ecossistema Aspire;
- organizar configuração e dependências para ambientes além do local;
- evitar decisões que amarrem o app a uma hospedagem única prematuramente;
- documentar quais partes são obrigatórias para publicação e quais são opcionais para estudo local.

O foco desta etapa é preparar a trilha, não maximizar cobertura de provedores.

## Estratégia De Aprendizado E Experimentação

Como parte da motivação é estudar o Aspire, o design deve reservar espaço para exploração controlada.

Essa exploração deve ocorrer em camadas:

1. composição local e dashboard;
2. telemetry, logging e health checks;
3. configuração de publicação;
4. experimentos adicionais apenas se trouxerem aprendizado claro sem inflar o produto.

Recursos do Aspire que não entreguem valor imediato ou aprendizado objetivo devem ficar fora do escopo inicial.

## Estratégia Para Marcar O MVP Atual

Antes da introdução dos projetos do Aspire, o estado atual da `main` deve ser marcado com uma tag anotada:

- `v1.0.0-mvp`

### Motivo Da Tag

A tag serve para:

- registrar formalmente o ponto exato do MVP antes da evolução arquitetural;
- facilitar comparação entre o estado pré-Aspire e pós-Aspire;
- apoiar rollback conceitual, documentação e comunicação do projeto;
- servir como base para uma release opcional no GitHub.

### Forma Recomendada

A recomendação é usar uma tag anotada, não apenas uma lightweight tag.

Exemplo esperado:

```bash
git tag -a v1.0.0-mvp -m "MVP baseline before .NET Aspire adoption"
```

Opcionalmente, o repositório também pode publicar uma GitHub Release apontando para essa tag com notas curtas dizendo que ela representa o MVP funcional pré-Aspire.

## Rollout Incremental

### Etapa 1: Marcação Do Marco Atual

- confirmar que `main` está no ponto desejado;
- criar a tag anotada `v1.0.0-mvp`;
- opcionalmente publicar release correspondente.

### Etapa 2: Introdução Da Estrutura Aspire

- adicionar `NameScan.AppHost`;
- adicionar `NameScan.ServiceDefaults`;
- incluir os novos projetos na solução;
- manter o comportamento funcional do `NameScan` inalterado.

### Etapa 3: Integração Operacional

- migrar o `Program.cs` do `NameScan` para consumir defaults compartilhados;
- adicionar health checks;
- adicionar telemetry e logging estruturado;
- configurar execução preferencial via `AppHost`.

### Etapa 4: Documentação E Validação

- documentar como rodar com e sem Aspire;
- documentar implicações para deploy;
- validar que o streaming SSE e a suíte de testes seguem estáveis.

## Critérios De Sucesso

A mudança será considerada bem-sucedida se:

- o comportamento do MVP permanecer intacto para o usuário final;
- a solução ganhar observabilidade útil em desenvolvimento;
- o caminho de configuração e publicação ficar mais organizado;
- o `NameScan` continuar simples de entender como aplicação de produto;
- o Aspire entrar como facilitador, não como fonte de complexidade desnecessária;
- a tag `v1.0.0-mvp` registrar claramente o marco anterior à mudança.

## Riscos E Mitigações

### Risco: Complexidade Operacional Prematura

Adicionar Aspire pode tornar a solução mais difícil de abordar para quem só quer trabalhar no produto.

Mitigação:

- manter `NameScan` com responsabilidade clara;
- limitar `ServiceDefaults` ao essencial;
- documentar o fluxo mínimo de execução.

### Risco: Abstrações Compartilhadas Em Excesso

Extrair infraestrutura cedo demais pode gerar camadas que o produto ainda não precisa.

Mitigação:

- só extrair o que for transversal e comprovadamente útil;
- evitar criar projetos extras além de `AppHost` e `ServiceDefaults` nesta fase.

### Risco: Estudo Virar Escopo Paralelo

A motivação de aprender Aspire pode expandir demais o escopo do trabalho.

Mitigação:

- tratar aprendizado como objetivo secundário subordinado ao valor para o projeto;
- aprovar experimentos adicionais separadamente.

## Estratégia De Testes

A introdução do Aspire deve preservar a suíte existente como fonte principal de regressão funcional.

Expectativas:

- testes de domínio e SSE continuam válidos;
- novos testes podem cobrir integração mínima de startup/configuração quando fizer sentido;
- nenhuma mudança arquitetural deve reduzir a confiabilidade da validação atual do MVP.

## Fora De Escopo Futuro Imediato

Os itens abaixo podem ser revisitados depois, mas não entram nesta etapa:

- separar checkers em worker dedicado;
- distribuir checks em filas;
- persistir histórico de consultas;
- adicionar banco só para “aproveitar” o Aspire;
- decompor frontend e backend em aplicações independentes sem necessidade de produto.

## Resultado Esperado

Ao final desta iniciativa, o repositório terá:

- um marco formal do MVP anterior à mudança via tag `v1.0.0-mvp`;
- uma solução compatível com o fluxo recomendado do Aspire;
- melhor observabilidade e organização operacional;
- um caminho mais claro para publicação;
- preservação da simplicidade do NameScan como produto Brasil-first em português.
