# NameScan

NameScan é um web app gratuito para verificar se um nickname, handle ou nome de marca parece disponível em redes sociais, plataformas digitais e domínios.

O escopo inicial é Brasil primeiro. O MVP usa C# com ASP.NET Core, Blazor, MudBlazor e Server-Sent Events para mostrar resultados progressivamente.

## MVP

Alvos obrigatórios:

- Instagram
- TikTok
- X
- YouTube
- GitHub
- Twitch
- `.com`
- `.com.br`

Recursos principais:

- verificação progressiva via SSE;
- status e confiança por plataforma;
- sugestões alternativas simples;
- relatório copiável;
- aviso de limitação dos resultados;
- apoio voluntário via Apoia.se.

## Documentação

- Especificação de produto: `docs/superpowers/specs/2026-04-25-namescan-mvp-design.md`
- Plano de implementação: `docs/superpowers/plans/2026-04-25-namescan-mvp-implementation.md`
- Especificação de adoção do Aspire: `docs/superpowers/specs/2026-05-02-aspire-adoption-design.md`
- Plano de adoção do Aspire: `docs/superpowers/plans/2026-05-02-aspire-adoption-implementation.md`

## Execução

Fluxo recomendado com Aspire:

```bash
dotnet run --project NameScan.AppHost
```

Fluxo direto do app web:

```bash
dotnet run --project NameScan
```

## Observabilidade Com Aspire

Ao subir o projeto por `NameScan.AppHost`, o dashboard do Aspire passa a concentrar:

- métricas do runtime ASP.NET Core;
- health checks de desenvolvimento em `/health` e `/alive`;
- telemetria do fluxo de verificação do NameScan;
- telemetria do ciclo de vida do stream SSE em `/api/check/stream`.

Os sinais de domínio atuais incluem:

- início e conclusão de verificações;
- duração total da verificação;
- duração por plataforma;
- resultados por status e confiança;
- início e conclusão do stream SSE.

## Marco Do MVP

O estado funcional anterior à adoção do `.NET Aspire` foi marcado com a tag anotada `v1.0.0-mvp`.

## Hospedagem E Deploy

Nesta etapa, o projeto está preparado para rodar localmente com Aspire e para evoluir a configuração operacional sem mudar a arquitetura do MVP.

Compromissos atuais:

- `NameScan` continua sendo o único app de produto;
- `NameScan.AppHost` é o ponto preferencial para execução local com observabilidade;
- `NameScan` ainda pode ser executado isoladamente para desenvolvimento focado;
- não há dependências obrigatórias novas de banco, fila ou cache distribuído.

O caminho de deploy futuro deve preservar esse desenho simples e tratar recursos extras como decisões explícitas, não como requisito artificial da adoção do Aspire.
