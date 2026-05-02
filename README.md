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

## Marco Do MVP

O estado funcional anterior à adoção do `.NET Aspire` foi marcado com a tag anotada `v1.0.0-mvp`.
