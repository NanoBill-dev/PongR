# Pong Royale — Sistema de cartas

Validacao do repositorio de 19 power-ups de ataque contra o codigo que existe.
Ultima revisao: 2026-08-21.

Para as REGRAS do jogo ver `GAME_DESIGN.md`. Para a arquitetura ver `ARCHITECTURE.md`.

---

## 1. Regra de posse — JA IMPLEMENTADA

O repositorio propoe uma peca de estado nova: o ultimo jogador cuja RAQUETE tocou a bola,
sem mudar em colisao com parede ou torre, comecando indefinido no saque.

**Isso ja existe desde o T9**, como `BallState.LastHitByPlayer`, e obedece exatamente essa
regra: e escrito somente na rebatida pela FACE FRONTAL da raquete, nao muda em parede nem
em torre, nao muda em contato pelas costas, e comeca em `NoPlayer`.

Convergencia por sorte: o campo foi criado para creditar dano em torre. Serve para posse
sem uma linha de alteracao.

**Consequencia de design:** um efeito "na bola" deixa de ser simetrico. Ele vale enquanto a
bola for sua, e rebater passa a ser, por si so, um ato defensivo.

---

## 2. A observacao que define a implementacao

**10 das 19 cartas sao apenas multiplicadores de configuracao.**

Turbina, Chumbo, Coice, Precisao, Estilingue, Lodo, Fundacao Rachada, Coroa Exposta, Vao
Aberto e Zona Quente nao fazem nada alem de mudar um numero que ja existe no `MatchConfig`.

Hoje os resolvers leem `state.Config.Paddle.MaxSpeed` diretamente. Implementar cada carta
como um `if` espalhado produziria 19 casos especiais — exatamente o que a secao 16 do
prompt mestre proibe.

**Solucao: uma CAMADA DE MODIFICADORES.** Os resolvers deixam de ler o config e passam a
perguntar o valor EFETIVO, ja com os efeitos ativos aplicados. Com isso essas 10 cartas
viram DADO PURO — qual campo, qual multiplicador, se afeta a si mesmo ou o adversario — e
somente 6 precisam de codigo proprio.

Regras da camada:

1. Efeitos MULTIPLICAM sobre a base, nunca substituem por valor absoluto. Ao expirar, o
   valor volta sozinho porque nada foi sobrescrito.
2. O clamp de angulo minimo de 20 graus e sempre a ULTIMA etapa, depois de qualquer
   curvatura ou aceleracao.
3. O teto de 8 bolas e rigido.
4. Combinacao mantem os dois efeitos integrais; a duracao curta e o unico preco.

---

## 3. Validacao carta a carta

| # | Carta | Veredito |
|---|---|---|
| 1 | Turbina | Depende do bloqueio A |
| 2 | Martelo | Compativel, mas reabre comportamento removido a pedido — ver 5.1 |
| 3 | Chumbo | Depende do bloqueio A |
| 4 | Ima do Rei | BLOQUEIO B — afeta o plano de netcode |
| 5 | Multibola | Cortada do v1 — ver 5.2 |
| 6 | Espectro | Compativel |
| 7 | Estilhaco | Compativel; metade dela ja e comportamento padrao — ver 5.3 |
| 8 | Corrosao | Compativel; exige estado novo por torre |
| 9 | Coice | Trivial: `PaddleConfig.SweepCarry` ja existe |
| 10 | Estilingue | Compativel |
| 11 | Precisao | Trivial: `MaxDeflectionFromNormalDegrees` ja existe |
| 12 | Efeito | BLOQUEIO B (mesma matematica do Ima) |
| 13 | Fundacao Rachada | Trivial |
| 14 | Coroa Exposta | Trivial |
| 15 | Sabotagem | Compativel, e mais barata do que a proposta imagina — ver 5.4 |
| 16 | Lodo | Compativel |
| 17 | Vao Aberto | A mais arriscada tecnicamente — ver 6 |
| 18 | Zona Quente | Depende do bloqueio A |
| 19 | Polvora | Compativel |

---

## 4. Bloqueios

### Bloqueio A — velocidade multiplicada ainda nao compoe corretamente

`BallState.SpeedMultiplier` existe desde o T9 mas vale sempre 1. Quando a transferencia de
velocidade da raquete foi implementada, ficou registrado no proprio codigo que a soma
misturaria espacos quando o multiplicador deixasse de ser 1: `ApplyOutgoingVelocity`
compoe a direcao vezes a VELOCIDADE BASE com a velocidade real da raquete.

Turbina, Chumbo e Zona Quente esbarram nisso.

**Correcao obrigatoria antes de qualquer carta de velocidade.** E pequena: compor tudo em
espaco de velocidade real e converter de volta para base ao final.

### Bloqueio B — curvatura quebra a extrapolacao da bola

O ADR-004 apoia todo o multiplayer em uma propriedade: a bola anda em LINHA RETA entre
colisoes, entao `posicao + velocidade * tempo` e exato, e nao aproximado. E isso que faz a
bola parecer local com 150 ms de latencia.

**Ima do Rei e Efeito introduzem aceleracao lateral.** A trajetoria deixa de ser reta e a
extrapolacao vira aproximacao.

Nao inviabiliza: o cliente pode aplicar a mesma aceleracao se souber que o efeito esta
ativo. Mas passa a exigir que o estado do efeito esteja sincronizado com precisao, e
qualquer divergencia vira bola desenhada no lugar errado.

**Custo dessas duas cartas e FASE 3, nao FASE 2.** A conta so chega no multiplayer.

### Bloqueio C — Multibola

Ver 5.2.

---

## 5. Ressalvas por carta

### 5.1 Martelo reabre o que foi removido a pedido

O dano decrescente foi implementado porque uma bola pinballando atras da raquete derretia
uma torre em segundos. O Martelo desliga esse decaimento por ate 3 acertos por posse — 750
de dano contra uma torre lateral de 2500.

**Nao e contradicao, e a diferenca entre bug e mecanica:** antes era acidente sem escolha,
agora e carta que o adversario escolheu trazer, com limite e com counterplay (levar a
raquete para tras e varrer a bola para fora).

Registrado para que seja decisao consciente e nao surpresa em playtest.

### 5.2 Multibola cortada do v1

Fatores que se somam:

- Maior custo em celular fraco (ate 3 bolas ativas por jogador)
- Maior dificuldade sob latencia
- Mais dificil de balancear
- Unica que exige regra especial de deck

**Sobre fazer os clones afetarem somente o adversario:** resolve o descontrole, mas remove
o contrapeso inteiro — ela vira a unica carta sem risco proprio, e portanto a melhor de
qualquer deck.

Cortar nao fecha porta: o teto de 8 bolas ja esta no codigo. Ela volta quando o sistema
estiver provado.

### 5.3 Estilhaco: metade ja e o padrao

A proposta diz que a bola "atravessa o espaco da torre destruida em vez de refletir". Isso
JA acontece desde o T10: torre destruida deixa de colidir.

A carta se reduz ao respingo de 400 na torre adjacente. Continua boa, so nao e tao especial
quanto parece.

### 5.4 Sabotagem sai mais barata do que a proposta supoe

A proposta sugere congelar o metronomo do adversario e avisa que, se apertar, basta cortar
essa parte.

Nao precisa cortar. `PlayerState.ReceivesCharges` ja existe, criado para o modo berserk.
"Congelar o elixir dele" e simplesmente nao abastece-lo durante o efeito, SEM TOCAR na
barra global — o que preserva a decisao de a barra ser compartilhada e indivergivel.

Efeito colateral justo: sem receber batidas, a contagem de ciclos limpos dele tambem nao
avanca, atrasando a redencao.

---

## 6. O problema da geometria (Vao Aberto)

O Vao Aberto encolhe as torres laterais do adversario de 0,9 para 0,65 de meia-largura. A
torre da direita, que ocupa de x = 2,3 a x = 4,1, passa a ocupar de 2,55 a 3,85.

A faixa entre 2,3 e 2,55 fica VAZIA e a bola pode entrar ali. Quando o efeito acaba, a
torre volta ao tamanho original — e se a bola estiver naquela faixa, ela fica DENTRO de um
objeto solido.

E a mesma familia do travamento reportado em playtest nas raquetes, que foi resolvido com
`SeparateFromPaddles`. As torres nao tem equivalente.

**Encaminhamento escolhido:** adiar o retorno da geometria enquanto houver bola na faixa
afetada, com limite de seguranca — se demorar demais, empurra a bola e restaura.

Alternativas descartadas: restaurar gradualmente empurrando a bola, ou deixar o vao aberto
ate o fim da partida.

Precisa de teste dedicado forcando o cenario.

---

## 7. Progressao

Progressao e meta-jogo, fora da partida: quais cartas o jogador ja DESBLOQUEOU para poder
escolher (secoes 23 e 30 do prompt mestre, FASE 4). **Nao afeta a simulacao.**

Ordem sugerida de desbloqueio: comecar pelas cartas de RAQUETE (Coice, Estilingue,
Precisao, Efeito), porque elas ensinam a varredura — a habilidade central do jogo. Quem
aprende a varrer joga melhor em tudo; quem comeca com Vao Aberto ganha vantagem sem
aprender nada.

Nao e urgente. Para testar agora, todas ficam disponiveis.

---

## 8. Decisoes tomadas

- **Nao e permitido levar a mesma carta nos dois slots de ataque.** Regra global, e nao
  excecao da Multibola.
- **Multibola fora do v1.**
- **Portal, Canhao, Escudo, Muro e Congelamento descartados** como cartas de ataque.
  Portal quebra predicao sob latencia e e candidato a estado travado. Canhao exige mira,
  violando a entrada de um dedo. Os outros tres sao defensivos, e a defesa foi padronizada.

---

## 9. Conjunto v1 e ordem de implementacao

**Camada de modificadores primeiro.** Depois:

| Ordem | Carta | Por que nesta posicao |
|---|---|---|
| 1 | Fundacao Rachada | Multiplicador de dano puro; prova a camada inteira |
| 2 | Coice | Uma constante, e amplifica a habilidade que ja existe |
| 3 | Precisao | Um angulo |
| 4 | Chumbo | Exige resolver o bloqueio A |
| 5 | Turbina | Mesma base do Chumbo |
| 6 | Espectro | Primeira com logica propria |

Seis cartas, quatro tipos de alvo, zero risco de netcode. Se essas seis forem divertidas, o
sistema esta provado e as outras treze sao preenchimento.

Ficam para depois: tudo que depende do bloqueio B (Ima do Rei, Efeito), a Multibola, e o
Vao Aberto — este por ultimo, quando o resto estiver estavel.
