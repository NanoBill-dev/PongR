# Pong Royale — Arquitetura

Documento vivo. Registra as decisoes estruturais e o porque delas.
Ultima revisao: 2026-08-20 (FASE 0).

## Visao geral das camadas

```
PongRoyale.Core          C# puro, sem UnityEngine. Toda a REGRA do jogo.
  ^ MatchCommand           entra: intencao do jogador
  v MatchSnapshot          sai: estado + fila de MatchEvent
PongRoyale.Gameplay      Views, input sources, mapeamento ScriptableObject -> Core, pooling
PongRoyale.Presentation  HUD, VFX, SFX, camera. Le snapshot/eventos, nunca escreve regra
PongRoyale.Networking    Transporta MatchCommand e MatchSnapshot. Nao conhece regra
PongRoyale.Services      Matchmaking, perfil, analytics (interfaces + fakes locais)
PongRoyale.App           Composition root. Unica assembly que conhece todas as outras
PongRoyale.Editor        Ferramentas de autoria (Editor-only)
```

Dependencias sao impostas pelo compilador via Assembly Definitions. `PongRoyale.Core`
tem `noEngineReferences: true`, o que torna **impossivel** chamar `Time.deltaTime`,
`Debug.Log` ou `Random.value` dentro da regra de jogo.

## ADR-001 — Simulacao em C# puro, fora de MonoBehaviour

**Decisao:** toda a regra vive em `MatchSimulation.Tick(commands, dt)`, sem MonoBehaviour.

**Por que:** o mesmo codigo roda no servidor headless, no cliente (predicao) e em testes
EditMode, sem duplicacao. Testar elixir, dano e rotacao de deck vira teste unitario de
milissegundos. Trocar a stack de rede toca uma unica assembly.

**Custo aceito:** e preciso escrever a ponte Core -> GameObject manualmente.

## ADR-002 — Fisica da bola custom, sem Rigidbody2D

**Decisao:** simulacao cinematica com passo fixo de 1/60s e colisao por varredura
(swept circle vs segmentos). Colliders da engine, se existirem, servem apenas para
queries de mira de carta — nunca para decidir resultado competitivo.

**Por que:**
1. A 25 u/s a bola anda ~0,42 u por tick: risco de tunneling atraves de raquetes finas.
2. Pong bom nao usa reflexao fisica. O angulo de saida vem do offset do impacto na
   raquete — isso e a skill do jogo. Fisica realista da controle pobre.
3. `Rigidbody2D` nao e deterministico nem serializavel: inviabiliza replay, teste
   reproduzivel e validacao server-side.

**Alvo de determinismo:** reproduzivel na mesma plataforma (suficiente para testes e
replays). Determinismo bit-exact entre ARM e x86 NAO e requisito, porque o modelo e
server-authoritative.

## ADR-003 — Arena em retrato, raquete horizontal

**Decisao:** celular em pe. Raquete do jogador na base movendo no eixo X, adversario no
topo, torres (Rei ao centro + 2 laterais) nas duas extremidades. Arena 10 x 18 unidades,
camera ortografica, 1 unidade = 1 metro.

**Por que:** ergonomia de uma mao so e leitura visual coerente com o layout de torres.
Em retrato com raquetes laterais a arena ficaria estreita demais e os angulos de
rebatida degenerados.

**Atencao:** isto SOBRESCREVE a secao 13 do prompt mestre, que pedia arraste vertical.

## ADR-004 — Netcode: NGO + Relay no MVP, servidor dedicado depois

**Decisao:** Netcode for GameObjects 2.x + Unity Relay/Lobby, host sendo um dos jogadores.
Simulacao 60 Hz, snapshot 20 Hz, input 30 Hz.

**Predicao:** o cliente prediz apenas a propria raquete (1 eixo, reconciliacao trivial).
A bola e **extrapolada**, nao interpolada: o snapshot carrega posicao, velocidade e um
`CollisionSequence` (ushort) incrementado a cada colisao. Entre colisoes o movimento e
retilineo uniforme, entao `pos + vel * dt` e exato. Quando `CollisionSequence` muda, o
cliente corrige suavemente em ~80 ms. E isso que faz a bola parecer local a 150 ms de RTT.

**Risco aceito no MVP:** host tem autoridade, logo cheat e viavel. Aceitavel enquanto nao
houver ranked real. Migrar para servidor dedicado e trocar quem chama `Tick()`, nao rewrite.

**Plano B:** Photon Fusion 2, se um spike de 2 dias no inicio da FASE 3 mostrar que NGO
nao atende.

## ADR-005 — ScriptableObject e camada de autoria, nao de runtime

```
CardDefinitionSO   (Unity, autoria no Inspector)
   -> CardConfig       (struct imutavel do Core, serializavel na rede)
   -> CardEffectRuntime (estado mutavel: duracao restante, alvo)
```

**Por que:** o Core nao pode referenciar `ScriptableObject` (nao tem UnityEngine), e SO
com estado mutavel vaza entre partidas no Editor e quebra no servidor.

**Regras:** toda carta tem um `ushort CardId` estavel — a rede trafega o id, nunca o nome.
`BalanceDataSO` centraliza velocidade, dano, custos, regen de elixir e trofeus.
Nenhum numero de gameplay hardcoded em script.

## ADR-006 — System.Numerics.Vector2 no Core, structs mutaveis em array

**Decisao:** o Core usa `System.Numerics.Vector2` como tipo de vetor, e o estado de
simulacao (bola, raquete, torre) sao structs MUTAVEIS guardados em arrays de tamanho fixo,
com campos publicos.

**Por que o vetor:** o Core nao pode referenciar UnityEngine (ADR-001), e escrever um
`Vec2` proprio seria reinventar matematica ja testada. Verificado em batchmode: resolve
normalmente numa assembly com `noEngineReferences: true`. A conversao para
`UnityEngine.Vector2` fica numa extension method na fronteira, em Gameplay.

**Por que struct mutavel:** `state.Balls[i].Position = x` escreve no lugar, sem alocacao
por tick. NUNCA guardar esses structs em `List<T>`: o indexador de List devolve uma copia
e a escrita seria perdida em silencio. Pela mesma razao `MatchState.GetTower` devolve
`ref` — ha um teste dedicado a isso, porque a regressao seria invisivel ao compilador.

**Por que campos publicos:** isto e dado de simulacao, nao objeto de dominio. O
comportamento mora nos resolvers. Propriedades custariam chamadas por tick sem proteger
nada, ja que quem escreve e sempre o proprio Core.

**Nota de nomenclatura:** existe o namespace `PongRoyale.Core.Ball`, entao nenhum tipo do
Core pode se chamar `Ball`. Vale o mesmo para `Paddle`.

## ADR-007 — Desempate em cascata, sem prorrogacao

**Decisao (do game designer, 2026-08-20):** a partida termina assim:

1. Torre Rei destruida encerra imediatamente. As duas no mesmo tick (possivel com
   Multibola) e empate.
2. Tempo esgotado, em cascata: mais torres de pe vence; empatado em torres, mais vida
   somada vence; identico nos dois, empate e a tela oferece revanche.

**Consequencia:** nao existe prorrogacao. `MatchPhase.Overtime`, `OvertimeDurationSeconds`
e o enum `TiebreakRule` foram REMOVIDOS por virarem codigo morto. Se prorrogacao voltar um
dia, volta como fase nova, nao como configuracao orfa.

**Detalhe que parece igual mas nao e:** o criterio conta as PROPRIAS torres vivas, nao
"torres que o jogador destruiu". Quando a bola de um jogador derruba a torre dele mesmo, o
contador de destruicoes credita o adversario e daria o desempate ao lado errado. O que
esta de pe nao mente.

**Generalizacao aplicada:** o criterio de vida vale para qualquer empate em contagem de
torres (3x3, 2x2, 1x1), nao apenas com todas de pe.

## ADR-008 — Passo fixo por contrato e ordem das operacoes no tick

**Decisao:** `MatchSimulation.Tick()` NAO aceita deltaTime. O passo e sempre
`MatchConstants.FixedDeltaTime`. Quem chama acumula o tempo real e chama Tick quantas
vezes couber no frame.

**Por que:** aceitar deltaTime variavel faria o resultado depender do frame rate do
aparelho, quebrando replay, teste reproduzivel e sincronizacao com o servidor de uma vez
so. Nao aceitar o parametro torna o erro impossivel, em vez de apenas desaconselhado.

**Ordem dentro do tick, e o motivo de cada posicao:**

1. Comandos — o input deste tick vale neste tick. Um tick de atraso na propria raquete e
   a latencia que mais se sente num jogo de reflexo.
2. Raquetes — antes da bola, para PreviousPositionX estar correto e a varredura relativa
   funcionar.
3. Bolas — varrem contra as raquetes ja atualizadas.
4. Relogio — ANTES de avaliar. Avaliar antes de somar o tempo faria a partida terminar um
   tick depois do apito.
5. Resultado — por ultimo, com o dano e o tempo deste tick ja contabilizados.

**A simulacao nunca limpa a fila de eventos.** Quem consome chama Clear, uma vez por frame,
depois de desenhar. Se o Tick limpasse, um frame que rodasse varios ticks perderia os
eventos de todos menos o ultimo, e o jogador veria uma torre cair sem som nem efeito.

**Teste dourado:** `MatchStateHash` resume o estado via FNV-1a sobre os BITS dos floats.
Uma sequencia fixa de 1800 ticks precisa produzir sempre o mesmo hash. Quando esse teste
falhar, o COMPORTAMENTO mudou: confirme que a mudanca era intencional antes de atualizar o
numero. O mesmo hash serve para detectar divergencia entre cliente e servidor na FASE 3.

## ADR-009 — Ferramentas de Editor: I/O primeiro, referencias depois

**Armadilha encontrada em 2026-08-20, custou uma cena silenciosamente quebrada:**
`AssetDatabase.LoadAssetAtPath` devolve uma referencia que MORRE na proxima importacao de
asset. `AssetDatabase.Refresh`, `SaveAndReimport`, `PrefabUtility.SaveAsPrefabAsset` e ate
`EditorSceneManager.OpenScene` importam.

Como o operador `==` do Unity trata objeto destruido como `null`, atribuir uma referencia
morta a um campo grava NULL — sem excecao, sem aviso no console, e passando em todo teste
de EditMode. O sintoma so aparece com o jogo rodando.

**Regra para toda ferramenta de Editor deste projeto:**

1. Fazer TODO o I/O de asset primeiro (criar arquivos, importar, salvar prefabs).
2. So entao carregar as referencias que serao usadas.
3. Montar a cena por ultimo, sem novas importacoes no meio.
4. VALIDAR o proprio resultado antes de salvar, e recusar salvar se algo ficou nulo.

O passo 4 e o que transforma essa classe de falha em erro barulhento. Uma ferramenta que
loga sucesso e entrega cena quebrada e pior que uma que falha.

**Corolario de testes:** os 99 testes de EditMode passaram o tempo todo com a cena
quebrada. Fiacao de cena so e verificavel em PlayMode — por isso a suite PlayMode existe e
checa referencias atribuidas, views seguindo o estado e enquadramento da camera.

## Convencoes

- Namespace raiz `PongRoyale`, espelhando a pasta (`PongRoyale.Core.Simulation`).
- Regra competitiva NUNCA em MonoBehaviour.
- Comunicacao Core -> Presentation por fila de eventos por tick, nunca chamada direta.
- Constante estrutural (tick rate) fica em `MatchConstants`. Numero de gameplay fica em SO.

## Versionamento

- Unity fixado em `6000.5.9f1` (unica versao instalada). Reavaliar migracao para LTS
  antes da FASE 3, quando o custo ainda e aceitavel.
- Git LFS cobre binarios pesados de autoria (psd, tga, wav, fbx...). `.png`/`.jpg` ficam
  fora do LFS de proposito: sprites 2D sao pequenos e queimariam a cota gratuita do
  GitHub. Migrar depois e possivel com `git lfs migrate import --include="*.png"`
  (reescreve historico — trivial em repo solo, doloroso em equipe).
