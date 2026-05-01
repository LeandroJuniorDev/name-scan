# NameScan Home Redesign

## Objetivo

Refatorar a Home do app para reproduzir com alta fidelidade o estilo da referência visual fornecida, preservando integralmente o comportamento atual da busca, da validação, do streaming de resultados e da geração de sugestões.

## Escopo

- Manter a rota `/` e a lógica existente em `Home.razor`.
- Preservar o fluxo atual:
  - digitação do nickname
  - validação básica
  - início da verificação
  - recebimento progressivo dos resultados
  - exibição de sugestões
  - cópia do relatório
- Reestruturar a apresentação da Home para um layout escuro, centralizado e orientado a cards.
- Ajustar o layout global apenas no necessário para alinhar AppBar, fundo e área principal com a referência.

## Fora de Escopo

- Alterar regras de negócio de disponibilidade.
- Adicionar novas integrações de plataformas.
- Introduzir filtros funcionais para `DOMÍNIOS` e `SOCIAL`.
- Mudar contratos de JavaScript, endpoints, services ou testes de backend.

## Abordagem Recomendada

Usar `MudBlazor` como base estrutural e complementar a fidelidade visual com CSS customizado.

### Motivos

- Mantém consistência com o restante do app.
- Evita recriar componentes já existentes na biblioteca.
- Dá controle suficiente para aproximar tipografia, espaçamento, contraste, bordas e badges da referência.

## Estrutura da Tela

### Header

- AppBar escura, simples, com `NameScan` alinhado à esquerda.
- Remover o chip `MVP`.
- Eliminar elementos visuais que não existam na referência.

### Hero

- Bloco central com largura controlada.
- Headline em destaque, grande, centralizada e quebrada em duas linhas.
- Campo de busca horizontal com:
  - ícone de busca
  - placeholder no estilo da referência
  - botão de ação destacado
- Texto auxiliar abaixo do campo descrevendo as plataformas suportadas.

### Resultados

- Seção com título `Resultados em tempo real`.
- Divisor horizontal abaixo do cabeçalho da seção.
- Dois chips visuais no canto direito:
  - `DOMÍNIOS`
  - `SOCIAL`
- Apresentação em grid responsivo de cards.

### Card de Resultado

Cada item de `_results` deve virar um card com:

- ícone ou bloco visual à esquerda
- título principal derivado do resultado
- subtítulo com a plataforma ou descrição curta
- badge de status à direita

O card precisa acomodar estes estados visuais:

- disponível
- indisponível
- inconclusivo
- erro

Durante a execução do streaming, a área continua sendo atualizada em tempo real sem mudar a lógica atual.

## Mapeamento Visual dos Dados

- `Platform` alimenta o título ou subtítulo, dependendo do tipo de resultado.
- `Status` determina o badge, a cor e o texto exibido.
- `Note` aparece como informação secundária quando agregar contexto útil.
- `Url` continua disponível via link clicável no card.
- `_suggestions` permanece abaixo da grade em formato mais compatível com o novo layout.
- `CopyReportAsync` continua acessível, mas reposicionado para combinar com a nova composição.

## Responsividade

- Desktop: hero centralizado e grid com duas colunas.
- Tablet: grid com uma ou duas colunas conforme largura.
- Mobile: busca em coluna, botão com largura total e cards empilhados.

## Estilo

- Fundo global escuro.
- Superfícies com contraste sutil e bordas discretas.
- Tipografia forte no hero e mais contida nas informações secundárias.
- Espaçamento generoso entre seções.
- Chips e badges com aparência próxima à referência, sem perder legibilidade.

## Implementação Prevista

- Atualizar `Components/Layout/MainLayout.razor` para simplificar a AppBar.
- Ajustar `Components/Layout/MainLayout.razor.css` para o fundo e a área principal.
- Refatorar `Components/Pages/Home.razor` para a nova hierarquia visual.
- Expandir `wwwroot/app.css` com estilos específicos da Home.

## Testes e Verificação

- Executar os testes automatizados existentes.
- Validar manualmente:
  - estado inicial
  - validação sem nickname
  - carregamento com streaming
  - exibição de resultados
  - sugestões alternativas
  - cópia do relatório
  - responsividade básica

## Riscos Conhecidos

- A troca de tabela por cards exige remapeamento cuidadoso das informações para não reduzir clareza.
- A fidelidade visual depende de ajustes finos de CSS além da estrutura MudBlazor.
- O estado de carregamento “estilo mock” deve ser apenas visual e não pode interferir no streaming já implementado.
