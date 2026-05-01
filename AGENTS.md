# AGENTS.md

Este arquivo orienta agentes que trabalhem neste repositório. O objetivo é reduzir contexto perdido, evitar regressões de produto e manter consistência com o fluxo que já usamos até aqui.

## Visão Geral

- Projeto: `NameScan`
- Tipo: web app em ASP.NET Core + Blazor
- Stack principal: `.NET 10`, `Blazor Web App`, `MudBlazor`, `xUnit`, `bUnit`
- Idioma do produto: `português do Brasil`
- Foco do MVP: Brasil primeiro, com `.com.br` tratado como alvo de primeira classe

O produto verifica se um nickname, handle ou nome de marca parece estar disponível em plataformas digitais e domínios. O app não promete certeza absoluta. Quando houver ambiguidade, prefira `Inconclusive` a inventar um resultado definitivo.

## Estado Atual Do Repositório

Considere o estado atual da branch antes de agir. Este repositório acabou de receber atualização de branch e o `main` já inclui trabalho mais recente que o contexto anterior de alguns agentes pode não refletir.

No momento em que este arquivo foi escrito:

- `main` está alinhada com `origin/main`
- O histórico já contém merge de trabalho até a etapa de observabilidade e verificação do MVP
- Não assuma que contexto antigo de branches `feat/namescan-task-*` ainda representa o estado mais novo do projeto

Antes de editar qualquer coisa:

1. Rode `git status --short`
2. Rode `git log --oneline --decorate -n 12`
3. Confirme que sua mudança faz sentido em cima do estado atual

Nunca reverta mudanças do usuário sem instrução explícita.

## Objetivo Do Produto

O NameScan deve permitir que o usuário:

- Digite um nickname ou nome de marca
- Dispare uma verificação única
- Veja resultados chegando progressivamente
- Compare disponibilidade estimada em plataformas e domínios
- Receba sugestões alternativas simples
- Copie um relatório textual

O tom do produto deve ser útil, direto e honesto sobre limitações. A experiência atual foi pensada para ser simples, rápida e em português.

## Escopo Atual Do MVP

Os alvos obrigatórios atuais são:

- `Instagram`
- `TikTok`
- `X`
- `YouTube`
- `GitHub`
- `Twitch`
- `.com`
- `.com.br`

Se você adicionar novas plataformas, trate isso como expansão de escopo. Atualize testes, documentação e, se necessário, o spec/plano em `docs/superpowers/`.

## Arquitetura Atual

### Aplicação

- `NameScan/Program.cs`
  - registra serviços
  - configura MudBlazor
  - expõe o endpoint SSE em `/api/check/stream`
  - mapeia os componentes Blazor

- `NameScan/Components/Pages/Home.razor`
  - tela principal
  - fluxo de busca
  - integração com JavaScript para `EventSource`
  - renderização progressiva dos resultados
  - cópia do relatório

- `NameScan/wwwroot/js/check-stream.js`
  - bridge entre `EventSource` e o componente Blazor

### Domínio

- `NameScan/Validation/`
  - validação e normalização de nickname

- `NameScan/Features/Checks/`
  - orquestração das verificações
  - modelo de evento de stream
  - resumo final
  - enums de status e confiança

- `NameScan/Features/Suggestions/`
  - geração de sugestões simples e determinísticas

- `NameScan/Features/Reporting/`
  - formatação do relatório textual

- `NameScan/Platforms/`
  - contrato de verificadores
  - registry dos alvos
  - verificadores HTTP e de domínio

### Testes

- `NameScan.Tests/Validation/`
- `NameScan.Tests/Features/`
- `NameScan.Tests/Platforms/`
- `NameScan.Tests/Web/`

Use a suíte existente como fonte de verdade para comportamento esperado.

## Comportamentos E Combinados Importantes

### Produto e UX

- Toda copy visível ao usuário deve permanecer em português.
- O produto é Brasil-first. Não “anglicize” a UI sem necessidade explícita.
- O app deve comunicar limitação e estimativa, não certeza absoluta.
- Quando um verificador não consegue concluir com confiança, prefira `Inconclusive`.
- Falha de uma plataforma não deve derrubar a verificação inteira.

### Backend

- `NicknameValidator` é a fonte central para validar input do usuário.
- `HandleCheckService` faz a orquestração paralela e o streaming progressivo.
- Resultados devem ser emitidos conforme chegam, não apenas no final.
- A ordenação final das plataformas deve continuar estável e previsível.
- Timeouts e falhas temporárias devem isolar o erro por plataforma.
- O cache deve evitar repetir resultados estáveis, mas não deve persistir resultados transitórios de erro ou inconclusão.

### Plataformas

- Verificadores devem usar apenas sinais públicos.
- Não adicione scraping autenticado.
- Não dependa de login, cookies de usuário ou bypass de proteção agressiva.
- Se um comportamento externo estiver ambíguo, use observação curta e `Inconclusive`.

### Frontend

- Preserve a experiência simples da `Home.razor`.
- Mudanças de UI devem continuar responsivas e objetivas.
- O app usa `MudBlazor`; siga os padrões já presentes antes de introduzir outra abordagem.
- Não complique a tela principal sem necessidade clara de produto.

### Tradução Entre Código e UI

- Enums podem continuar em inglês no código.
- Rótulos exibidos ao usuário devem continuar em português.
- Reaproveite `ReportFormatter` para tradução de status/confiança quando fizer sentido.

## Ferramentas E Fluxo Que Estamos Usando

Este projeto já vem sendo implementado com disciplina baseada em Superpowers. Siga isso para manter continuidade.

### Superpowers

Ferramentas e skills relevantes para este repositório:

- `superpowers:using-superpowers`
  - ponto de partida para checar skills aplicáveis

- `superpowers:brainstorming`
  - use antes de trabalho criativo, mudança de comportamento, nova feature ou alteração relevante de UX
  - gera alinhamento antes de sair implementando

- `superpowers:writing-plans`
  - use quando houver spec ou requisitos para trabalho multi-etapas
  - os planos ficam em `docs/superpowers/plans/`

- `superpowers:test-driven-development`
  - preferido para features e correções com comportamento verificável

- `superpowers:systematic-debugging`
  - use para investigar bugs, falhas de testes ou comportamento inesperado

- `superpowers:verification-before-completion`
  - use antes de declarar a tarefa como pronta

- `superpowers:requesting-code-review`
  - útil ao concluir mudanças maiores

### Documentação De Processo Já Existente

- Spec atual:
  - `docs/superpowers/specs/2026-04-25-namescan-mvp-design.md`

- Plano atual:
  - `docs/superpowers/plans/2026-04-25-namescan-mvp-implementation.md`

Se a mudança alterar escopo de produto, fluxo principal, arquitetura ou compromissos do MVP, atualize a documentação correspondente em vez de deixar o código divergir silenciosamente.

## Convenções De Implementação

- Prefira mudanças pequenas e localizadas.
- Siga a organização atual por responsabilidade.
- Evite criar arquivos genéricos ou “utilitários” sem necessidade real.
- Mantenha nomes claros e próximos da linguagem do domínio.
- Preserve `nullable` habilitado e trate warnings com seriedade.
- Não introduza complexidade especulativa.
- Em caso de dúvida, escolha a solução mais simples que preserve testabilidade.

## Convenções De Teste

Antes de concluir trabalho, rode o menor conjunto de testes que prova sua mudança e, quando a alteração for ampla, rode a suíte completa.

Comandos úteis:

```bash
dotnet build NameScan.sln
dotnet test NameScan.sln
dotnet test NameScan.sln --filter NicknameValidatorTests
dotnet test NameScan.sln --filter HandleCheckServiceTests
dotnet test NameScan.sln --filter HomePageTests
dotnet test NameScan.sln --filter CheckStreamEndpointTests
```

Expectativas:

- Mudanças em validação: atualize `NameScan.Tests/Validation/`
- Mudanças em orquestração, cache ou timeout: atualize `NameScan.Tests/Features/Checks/`
- Mudanças em verificadores: atualize `NameScan.Tests/Platforms/`
- Mudanças na página inicial ou fluxo de stream: atualize `NameScan.Tests/Web/`

## Convenções De Git

- Verifique sempre se o worktree está limpo antes de grandes mudanças.
- Não sobrescreva trabalho alheio.
- Siga a convenção de commits já visível no histórico:
  - `feat: ...`
  - `fix: ...`
  - `test: ...`
  - `build: ...`
  - `chore: ...`
- O histórico existente usa branches como `feat/...` e `docs/...`; se precisar criar branch e não houver instrução diferente, prefira seguir a convenção já adotada no repositório.

## Checklist Antes De Encerrar Uma Tarefa

1. A mudança respeita o estado atual da branch?
2. A UI continua em português?
3. O comportamento mantém a honestidade do produto sobre incerteza?
4. O menor conjunto de testes relevante foi executado?
5. Documentação e testes foram atualizados quando necessário?
6. `git status --short` está coerente com o que foi alterado?

## Resumo Para Agentes

Se você esquecer todo o resto, lembre disto:

- Este é um MVP Brasil-first em português
- Simplicidade e honestidade importam mais que “esperteza”
- Prefira `Inconclusive` a um falso positivo
- Preserve streaming progressivo e isolamento de falhas por plataforma
- Use os testes existentes para guiar mudanças
- Siga o fluxo com Superpowers em vez de improvisar processo
