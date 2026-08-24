# Pong Royale — Roadmap

## FASE 0 — Setup (concluida, exceto T2b)
- [x] T1 Git + .gitignore + .gitattributes + LFS + UnityYAMLMerge
- [x] T2 ProjectSettings: nome, company, orientacao retrato fixa
- [x] T3 Limpeza de pacotes (20 removidos)
- [x] T4 Arvore Assets/_Project + Documentation
- [x] T5 9 Assembly Definitions (Core com noEngineReferences)
- [x] T7 Assets/Tests + primeiro teste EditMode verde
- [ ] T2b Android Build Support + bundle id + IL2CPP/ARM64 (depende do modulo no Hub)
- [x] T6 Cenas Bootstrap / MainMenu / Match + SceneLoader
- [x] T8 BalanceData com todos os numeros das secoes 11/13/14

## FASE 1 — Pong jogavel offline (CONCLUIDA)
- [x] T9  Core: MatchState, MatchCommand, MatchEvent, MatchStateFactory (MatchSnapshot adiado para a FASE 3: formato de rede sem rede e especulacao)
- [x] T10 Core: BallResolver + CollisionMath (varredura) + testes
- [x] T11 Core: PaddleResolver + limites + varredura relativa + testes
- [x] T12 Core: MatchOutcomeResolver (cascata de desempate) + testes
- [x] T13 Core: MatchSimulation.Tick() + teste de hash dourado
- [x] T14 Unity: MatchRunner, views, camera, arena com placeholders (+8 testes PlayMode)
- [x] T15 Input System (PointerPaddleInput unifica dedo e mouse) + AiPaddleInput

**Pronto:** da para jogar Pong contra a IA. 108 testes EditMode + 10 PlayMode.

## FASE 2 — Nucleo Pong Royale (em andamento)

REDESENHADA em 2026-08-21. O deck de 8 com mao de 4 e custo de elixir foi SUBSTITUIDO
pelo sistema de power-ups. Especificacao completa em GAME_DESIGN.md.

- [x] HUD v1: vida das torres com numero, dano flutuante, relogio, painel de resultado
- [x] Dano decrescente em acertos consecutivos de torre
- [x] Camada de efeitos com duracao (base comum de todo power-up)
- [x] Ciclo de elixir e cargas de defesa (metronomo global, contagem inteira)
- [x] Drop fisico: queda, coleta pela raquete, interceptacao unica
- [x] Visual do drop e barra de ciclo com cargas dos dois jogadores
- [ ] Combinacao de dois ataques (3,5 s)
- [ ] Redencao e modo berserk
- [ ] Selecao de deck de 3 antes da partida
- [x] Camada de modificadores: 5 cartas como dado puro (Fundacao Rachada, Coroa
      Exposta, Coice, Precisao, Lodo)
- [ ] Bloqueio A: composicao de velocidade, antes de Chumbo e Turbina
- [ ] Espectro e demais cartas com logica propria

**Pronto quando:** partida completa com power-ups, jogavel e divertida contra o bot.

## FASE 3 — Multiplayer
Spike NGO (2 dias) -> lobby -> spawn -> snapshot/extrapolacao -> resultado -> reconexao.

**Pronto quando:** dois celulares reais completam uma partida 1v1 em 4G.

## FASE 4-7
Progressao -> conteudo -> monetizacao -> polish. Ver secao 30 do prompt mestre.

## Decisoes em aberto
- D5 Backend de contas/progressao (UGS, PlayFab ou proprio) — decidir na FASE 4.
  No MVP: interface `IPlayerProfileService` com implementacao local em JSON.

---

## Playtest de 2026-08-24 — achados e plano

### O diagnostico principal

Dois sintomas relatados sao o MESMO problema:

- "Power-ups nao estao decidindo a partida; habilidade no Pong ja basta"
- "Nunca atingi a redencao"

**Causa raiz: o portao de aquisicao esta tarde demais.**

    torre lateral      2500 de vida
    dano por acerto     250
    acertos             10   <- para UM power-up nascer

Dez acertos limpos em torre quase nao acontecem numa partida de 120 s. Quando
acontecem, o jogador JA esta ganhando: o power-up vira trofeu de quem venceu, dura
6 s e nao muda nada.

A redencao morre pelo mesmo motivo. Ela exige um drop PERDIDO; se drops quase nao
nascem, nao ha o que redimir. Os 60 s de ciclos limpos nem chegam a ser o gargalo.

**Nao e falta de tempo nem cartas fracas. E a economia trancada atras de um evento
raro.**

### Correcao proposta

Alvo: o primeiro drop deve aparecer por volta dos 30-40 s. Supondo que um bom
jogador vaze cerca de uma bola a cada 10 s, isso significa 3 a 4 acertos.

    hoje                        10 acertos para o 1o drop
    drop em 50% e em 0%          5 acertos
    + vida da lateral 2500->1500 3 acertos  <- alvo

Recomendado: **drops em marcos de dano (50% e 0%) E vida da lateral para 1500**.
O BallResolver ja detecta dano em torre; falta comparar a vida antes e depois com
o limiar. A Torre Rei segue em 5000 como condicao de vitoria dificil.

Aumentar a duracao da partida foi DESCARTADO por ora: mascararia o problema. So
reavaliar depois que os drops chegarem cedo.

### Bug conhecido, introduzido no commit da arte

As barras aparecem pretas porque a MOLDURA foi desenhada por cima do
preenchimento (ordem 99 contra 91) e o interior dela e opaco. Moldura vai para
baixo do preenchimento.

### Ordem de trabalho

1. **Correcoes visuais baratas** (quase tudo dado ou ferramenta de cena)
   - [ ] Moldura abaixo do preenchimento (bug acima)
   - [ ] Barra de vida ABAIXO de cada torre; hoje aponta para o centro e cobre a raquete
   - [ ] Barra de elixir borda a borda (largura 8 -> 10)
   - [ ] Ordem de camadas: bola e raquete estao em 20 e a HUD em 90-100, entao a
         bola passa POR BAIXO da barra central. Precisam ficar acima
   - [ ] Fundo mais claro para dar contraste aos elementos

2. **Indicador de power-up** — o mais valioso da lista
   - [ ] Icone do efeito ativo perto da raquete, com tempo restante
   - [ ] O drop caindo precisa mostrar QUAL carta e; hoje e um circulo generico
   - Sem isso o sistema e invisivel, e sistema invisivel nao influencia decisao.
     Provavelmente contribui para a sensacao do achado principal.

3. **Drop em marcos de dano** — ataca a causa raiz

4. **Tamanho das torres** — resolve dois problemas juntos: sprites pequenos demais
   e o achatamento de 31% (a arte e mais alta que larga, a caixa e mais larga que
   alta). Custo: vaos entre torres diminuem, muda como a bola passa.

5. **Bot com decisoes** — hoje so persegue a bola. Precisa de prioridade por tick:
   - drop caindo para mim e alcancavel antes da bola chegar? busca
   - drop inimigo cruzando minha linha e ainda tenho interceptacao? rouba
   - senao, persegue a bola
   O interessante e que e a MESMA decisao que o humano enfrenta, o que faz do bot
   um sparring de verdade. Botao de dificuldade: chance de sequer considerar o drop.
   Sem isso nao da para testar interceptacao, porque o adversario nunca a usa.

6. **Reavaliar duracao da partida** — com dados, depois do item 3

### Adiados, com motivo

- **Hitbox arredondada na raquete**: exige varredura circulo-contra-capsula,
  mexendo no codigo mais testado do projeto, e o ganho e so nas pontas. Mitigacao
  barata: estreitar a colisao ~0,1 para bater com a parte reta do sprite.
- **Ranking e trofeus**: FASE 4. `TrophyConfig` ja existe com +30/-25/0 esperando.
