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

## FASE 2 — Nucleo Pong Royale
Elixir, deck 8 / mao 4, 8 cartas, HUD, VFX/SFX basicos.
Ordem das cartas, da mais simples para a mais dificil:
Muro -> Escudo -> Congelamento -> Turbina -> Canhao -> Ima -> Portal -> Multibola.

**Pronto quando:** partida completa com cartas, divertida contra bot.

## FASE 3 — Multiplayer
Spike NGO (2 dias) -> lobby -> spawn -> snapshot/extrapolacao -> resultado -> reconexao.

**Pronto quando:** dois celulares reais completam uma partida 1v1 em 4G.

## FASE 4-7
Progressao -> conteudo -> monetizacao -> polish. Ver secao 30 do prompt mestre.

## Decisoes em aberto
- D5 Backend de contas/progressao (UGS, PlayFab ou proprio) — decidir na FASE 4.
  No MVP: interface `IPlayerProfileService` com implementacao local em JSON.
