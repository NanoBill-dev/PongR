# Pong Royale — Game Design

Documento vivo. Especifica as REGRAS do jogo, nao a implementacao — para arquitetura ver
`ARCHITECTURE.md`, para ordem de trabalho ver `ROADMAP.md`.

Ultima revisao: 2026-08-21, fechando o sistema de power-ups.

---

## 1. Visao geral

Pong competitivo 1v1 em retrato. Cada jogador defende tres torres com uma raquete e ataca
as tres do adversario rebatendo a bola.

O elemento central e sempre a BOLA. Todo poder do jogo passa por ela e pela raquete: voce
destroi com a bola e coleta com a raquete. Nao existe recurso paralelo para gerenciar
enquanto o jogo acontece.

---

## 2. Arena

Retrato, 10 x 18 unidades de mundo. Origem no centro.

- Raquetes se movem apenas no eixo X, em linhas fixas a 2,5 unidades da borda do seu lado.
- Tres torres por lado: Rei ao centro, duas laterais a 3,2 unidades do centro.
- Paredes refletem a bola nos quatro lados. A bola nunca sai da arena.

**Perspectiva:** cada jogador sempre se ve embaixo, online ou contra bot, como em Clash
Royale. A simulacao usa coordenadas absolutas (Bottom/Top) e a CAMERA espelha para quem
esta em cima. E decisao de apresentacao, nunca de regra.

---

## 3. Partida

| Regra | Valor |
|---|---|
| Duracao | 120 s (provisorio, a testar) |
| Vitoria imediata | Torre Rei destruida |
| Dois Reis no mesmo tick | Empate |

**Desempate ao fim do tempo, em cascata:**

1. Vence quem tem MAIS torres de pe.
2. Empatado em torres, vence quem tem MAIS vida somada nas torres.
3. Identico nos dois, empate — a tela oferece revanche.

O criterio conta as PROPRIAS torres vivas, nao "torres destruidas": se a bola de um jogador
derruba a torre dele mesmo, contar destruicoes daria o desempate ao lado errado.

---

## 4. Bola

| Parametro | Valor |
|---|---|
| Velocidade inicial | 8 u/s |
| Velocidade maxima | 25 u/s |
| Ganho por rebatida | +2% |
| Dano base | 250 |
| Raio | 0,25 |

**Angulo de saida** vem do ponto do impacto na raquete, ate 60 graus da normal. Acertar de
raspao manda para o lado, acertar no centro devolve reto. E a habilidade central do jogo.

**Varredura da raquete** transfere velocidade para a bola (35% por padrao). Rebater
varrendo empurra a bola: da para atacar, e nao so devolver. Tambem e a ferramenta para
expulsar a bola que entrou atras da raquete.

**Angulo minimo com a horizontal** de 20 graus impede a bola de entrar num vai-e-vem que
nunca chega a uma raquete.

### 4.1 Dano decrescente

Acertos consecutivos em torre, SEM a bola voltar ao campo entre as duas linhas de raquete,
causam dano decrescente: 65% do anterior a cada acerto, com piso de 20%.

| Acerto | Dano |
|---|---|
| 1 | 250 |
| 2 | 162 |
| 3 | 106 |
| 4 | 69 |
| 5+ | 50 (piso) |

**Por que:** uma bola pinballando atras da raquete derrubava uma torre em segundos por um
unico erro. O PRIMEIRO acerto continua valendo cheio, entao uma bola bem colocada pelo vao
entre as torres e recompensada por inteiro — o que decai e o acidente, nao a jogada.

O piso nao pode ser zero: dano zerado transformaria a area atras da propria raquete no
lugar mais seguro do jogo, e quem estivesse na frente no desempate poderia estagnar a
partida de proposito.

---

## 5. Power-ups

**As cartas SAO os power-ups.** Nao existe deck de 8, mao de 4 nem custo de elixir.

### 5.1 Deck de 3, ordenado

A ordem das cartas E a atribuicao:

| Slot | Tipo | Destino |
|---|---|---|
| 1 | Ataque | Torre lateral ESQUERDA do adversario |
| 2 | Defesa | Ciclo de elixir (nunca em torre) |
| 3 | Ataque | Torre lateral DIREITA do adversario |

Cartas de ataque so podem ocupar os slots 1 e 3.

**O slot 2 e PADRONIZADO: nao ha escolha de defesa.** Todo jogador entra com a mesma carta
defensiva, que anula um acerto em qualquer torre e acumula ate 3 cargas.

**Por que padronizar:** concentra toda a decisao e todo o segredo no ataque, e tira do MVP
o eixo mais dificil de balancear — cartas defensivas sao as que mais produzem partidas
travadas. A estrutura do slot 2 permanece, entao liberar variedade defensiva depois e
acrescentar opcoes, nao redesenhar.

Na pratica o jogador escolhe **2 cartas**: qual ataque na esquerda, qual na direita.

### 5.2 Informacao assimetrica

- Antes da partida, o adversario VE seu deck: sabe quais cartas voce trouxe.
- O adversario NAO VE a atribuicao: nao sabe qual ataque esta em qual torre.
- Durante a partida o deck nao aparece na tela.

Como defesa so pode ir no slot 2, o adversario identifica sua defesa de imediato. Todo o
segredo esta em qual ataque protege qual torre.

### 5.3 Aquisicao por drop

```
Voce destroi a torre lateral inimiga
        v
O power-up CAI da torre em direcao ao seu lado
        v
Ele atravessa a arena inteira, passando pela raquete do adversario
        v
   coletado pela sua raquete   |   interceptado pelo adversario   |   ninguem pegou
        v                      |            v                     |        v
   ativa por 5-7 s             |   voce perde, sem redencao       |   perdido, ELEGIVEL
                                                                        para redencao
```

- Somente a RAQUETE coleta. A bola atravessa o drop sem interagir.
- O drop e destruido ao alcancar a borda da ARENA do lado de quem coletaria.
  Criterio de MUNDO, nunca de tela: a area visivel varia por aparelho, e no online os dois
  jogadores discordariam sobre quando o item sumiu.

**Interceptacao: uma por jogador, por partida.** Gastou, acabou — o proximo drop passa
livre. Isso obriga a decidir sob pressao, sem saber o que esta caindo, e impede que um
jogador experiente zere a economia ofensiva do adversario.

**O drop de REDENCAO nao pode ser interceptado**, mesmo que o adversario ainda tenha a
interceptacao disponivel. A redencao foi comprada com 60 segundos de defesa impecavel e ja
custou as tres cargas; deixar que fosse roubada puniria duas vezes o mesmo jogador.

**O contrapeso que faz o sistema funcionar:** pegar o drop exige a raquete, que e a mesma
que esta defendendo. Destruir a torre nao da o premio; da uma ESCOLHA entre buscar o
power-up e manter a defesa. E o mesmo recurso disputado por dois objetivos.

### 5.4 Combinacao

Coletar um power-up com outro ainda ativo:

- Os efeitos SOMAM.
- A duracao total passa a ser 3,5 s, independentemente do que restava.

Cria decisao de ritmo: correr para a segunda torre e combinar forte e curto, ou espacar os
dois e ter dois periodos longos.

A carta de defesa NAO participa da combinacao. Ela e passiva com cargas, sem temporizador.

---

## 6. Ciclo de elixir e defesa

O elixir nao e moeda. E o motor da defesa e da redencao.

**A barra e um METRONOMO GLOBAL, nao um cronometro por jogador.** Um relogio unico, que
comeca junto e bate igual para os dois. O que difere e quem RECEBE a carga naquela batida.

- Ciclo de aproximadamente 20 s, compartilhado.
- A cada batida: +1 carga de defesa para cada jogador ativo, maximo 3.
- Cada carga anula UM acerto em qualquer torre e some.
- Gastar carga nao impede continuar acumulando ate 3.
- Jogador em modo berserk simplesmente nao recebe carga; a barra segue girando.

**Consequencia deliberada:** perder uma carga faltando pouco para a batida devolve ela
quase de graca; perder logo depois deixa o jogador 20 s exposto. E sorte real, e nao
injusta, porque ninguem escolhe quando toma o acerto.

**Ciclos limpos** e um contador separado das cargas: sobe a cada batida recebida e ZERA
sempre que uma carga e gasta. Chegar a 3 ciclos limpos significa 60 s sem tomar um acerto
sequer, e e o que a redencao exige.

A sorte da batida ajuda a DEFESA, nunca a redencao: recuperar a carga rapido devolve o
escudo, nao o progresso. Do contrario, tomar acerto na hora certa sairia quase de graca.

---

## 7. Redencao

> **Dispara no instante em que as duas condicoes forem verdadeiras ao mesmo tempo:**
> **(a)** 3 cargas limpas
> **(b)** existe um drop PERDIDO POR VOCE pendente

Nao importa qual vem primeiro. Perdeu o drop aos 30 s e completa a terceira carga limpa aos
60 s: dispara aos 60 s. Ja estava com 3 limpas e erra a coleta: dispara na hora.

**Ao disparar:**

1. O power-up perdido cai de novo. Se os DOIS foram perdidos, sorteia 50/50 entre as duas
   cartas de ataque escolhidas.
2. As 3 cargas de defesa sao CONSUMIDAS.
3. O elixir para.

O jogador fica em **modo berserk**: so ataque, sem defesa. O preco e imediato e visivel —
os tres diamantes apagam no instante em que o drop reaparece.

**Estados do elixir:**

A barra global nunca para. O que muda e se o jogador RECEBE a carga da batida:

| Situacao do jogador | Recebe carga? |
|---|---|
| Acumulando | sim |
| 3 cargas limpas, nada perdido pendente | ja esta no teto; volta a receber ao gastar |
| 3 cargas limpas, perda foi por INTERCEPTACAO | idem; nao ha redencao para esse drop |
| Redencao disparou e o drop foi COLETADO | NAO, ate o fim da partida (berserk) |
| Redencao disparou e o drop foi PERDIDO | sim, volta a receber; sem nova redencao |

A ultima linha e o contrapeso: recebeu o premio, paga o preco integral; nao recebeu, nao e
punido duas vezes — recupera a geracao de defesa, so nao tem segunda chance no ataque.

**A redencao acontece no maximo uma vez por partida**, em qualquer cenario.

---

## 8. HUD

```
+---------------------+
|  T     T     T      |  torres do adversario
|      =====          |  raquete dele
|                     |
|         o           |  bola
|      <> <> <>       |  cargas DELE
| ===================  |  BARRA DE CICLO (divisao dos lados)
|      <> <> <>       |  cargas SUAS
|                     |
|      =====          |  sua raquete
|  T     T     T      |  suas torres
+---------------------+
```

- Vida de cada torre com numero e barra, ancorada no mundo acima dela.
- Numero de dano flutuante no impacto, mostrando o valor REALMENTE aplicado.
- Relogio da partida acima da arena.
- Barra de ciclo na divisao dos lados: UMA barra, compartilhada, porque o metronomo e
  global. Nao ha divergencia possivel entre os dois jogadores.
- Indicadores de carga dos DOIS jogadores, um conjunto de cada lado da barra. Ver as cargas
  do adversario muda como voce ataca — e ver as tres dele acesas avisa que ele esta a um
  passo da redencao.

---

## 9. Riscos conhecidos

| Risco | Situacao |
|---|---|
| Bola de neve por acumulo de recompensa na torre lateral | MITIGADO: coletar exige a raquete que defende |
| Legibilidade da redencao | ABERTO: condicao complexa, precisa de indicador claro |
| Berserk punitivo demais em partida de 120 s | ABERTO: so playtest decide |
| Poucos power-ups por partida (max 2 de ataque) | ABERTO: cada um precisa ser impactante sem decidir a partida sozinho |
| Precisao sob latencia na FASE 3 | ABERTO: coletar drop com 150 ms de atraso e mais dificil |

---

## 10. Em aberto

- Efeitos concretos de cada power-up (em amadurecimento).
- Duracao da partida: 120 s e provisorio, a validar jogando.
- Velocidade de queda do drop.
- Repositorio de cartas de ATAQUE: quantas existem e quais, para a escolha de 2 ser
  interessante. As cartas defensivas da secao 17 do prompt mestre (Escudo, Muro,
  Congelamento) ficam sem uso enquanto a defesa for padronizada.
