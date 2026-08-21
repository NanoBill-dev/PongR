# Pong Royale — Briefing completo do projeto

> Documento de contexto para discussao de design em conversa separada.
> Copie este arquivo inteiro para dar contexto completo do projeto.
> Atualizado em 2026-08-21.

---

## O que e o jogo

Pong competitivo 1v1 para celular, em retrato. Cada jogador defende tres torres com uma
raquete horizontal e ataca as tres do adversario rebatendo a bola. Mistura reflexo de Pong
com escolha estrategica de power-ups.

Principio central: **o elemento mais importante e sempre a BOLA**. Todo poder do jogo passa
por ela e pela raquete — voce destroi com a bola e coleta power-up com a raquete. Nao
existe recurso paralelo para gerenciar enquanto o jogo acontece.

Referencias de linguagem visual: Clash Royale, Brawl Stars, Rocket League Sideswipe.
Free-to-play, monetizacao apenas cosmetica, sem pay-to-win.

---

## Estado atual: o que JA EXISTE e funciona

O jogo esta jogavel contra um bot. 138 testes automatizados de regra e 18 de interface,
todos passando.

**Pronto:**
- Arena, fisica da bola, movimento da raquete, torres, dano, vitoria e derrota
- Bot com dificuldade ajustavel
- Interface: vida das torres com numero, numeros de dano flutuantes, relogio, tela de fim
- Controle por toque (arraste relativo)

**Nao existe ainda:** power-ups, elixir, selecao de deck, multiplayer online, arte
definitiva, som, menus, progressao.

---

## Geometria e numeros exatos

**Arena:** 10 x 18 unidades de mundo (retrato). Origem no centro. Paredes refletem a bola
nos quatro lados — a bola nunca sai da arena.

**Raquetes:** movem-se apenas no eixo X, em linhas fixas.
- Jogador de baixo: linha em y = -6,5
- Jogador de cima: linha em y = +6,5
- Largura 2,4 / espessura 0,4 / velocidade maxima 18 u/s

**Torres:** tres por lado, em linha.
- Linha das torres: y = -8,0 (baixo) e y = +8,0 (cima)
- Torre Rei ao centro (x = 0), meia-extensao 1,2 x 0,8, vida 5000
- Torres laterais em x = -3,2 e x = +3,2, meia-extensao 0,9 x 0,7, vida 2500

**Detalhe importante de geometria:** entre a linha da raquete e a linha das torres existe
um corredor de cerca de 0,5 unidade — do tamanho exato do diametro da bola. A bola PODE
passar pelos vaos entre as torres e ficar ricocheteando nessa area.

**Bola:**
- Velocidade inicial 8 u/s, maxima 25 u/s, ganho de +2% por rebatida
- Raio 0,25, dano base 250
- Angulo de saida vem do PONTO do impacto na raquete, ate 60 graus da normal
- Angulo minimo de 20 graus com a horizontal (impede vai-e-vem que nunca chega a raquete)
- Fisica propria, sem motor de fisica: colisao por varredura, passo fixo de 60 Hz

**Partida:** 120 segundos (provisorio).

---

## Mecanicas emergentes ja descobertas em playtest

Estas surgiram da fisica e foram mantidas de proposito. Qualquer power-up novo interage
com elas.

### A zona atras da raquete

A bola passa pelos vaos entre as torres e fica ricocheteando atras da raquete, entre a
raquete e as proprias torres. Para expulsa-la o jogador precisa levar a raquete para tras,
deixando a frente descoberta.

**Dano decrescente:** acertos consecutivos em torre, sem a bola voltar ao campo, causam
dano decrescente — 65% do anterior a cada acerto, com piso de 20%.

| Acerto | Dano |
|---|---|
| 1 | 250 |
| 2 | 162 |
| 3 | 106 |
| 4 | 69 |
| 5+ | 50 |

O PRIMEIRO acerto vale cheio: uma bola bem colocada pelo vao entre as torres e recompensada
por inteiro. O que decai e o pinball acidental.

### Transferencia de velocidade da raquete

Varrer a raquete no momento da rebatida transfere 35% da velocidade dela para a bola. Isso
permite ATACAR (empurrar a bola numa direcao) em vez de so devolver, e e a ferramenta para
arremessar para fora a bola que entrou atras da raquete.

### Consequencia estrategica das torres laterais

Destruir uma torre lateral hoje vale por tres motivos ao mesmo tempo:
1. Abre um corredor livre para a Torre Rei
2. Conta no criterio de desempate
3. Libera o power-up que o atacante escolheu (ver abaixo)

---

## Regras de fim de partida

- **Torre Rei destruida:** vitoria imediata. As duas no mesmo tick: empate.
- **Tempo esgotado, em cascata:**
  1. Vence quem tem MAIS torres de pe
  2. Empatado em torres, vence quem tem MAIS vida somada
  3. Identico: empate

---

## O sistema de power-ups (o que precisa ser preenchido)

**As cartas SAO os power-ups.** Nao existe deck de 8, mao de 4 nem custo de elixir.

### Como o jogador monta

O jogador escolhe **2 cartas de ATAQUE** e as ordena:

| Slot | Destino |
|---|---|
| 1 | Torre lateral ESQUERDA do adversario |
| 2 | Defesa PADRONIZADA (igual para todos, sem escolha) |
| 3 | Torre lateral DIREITA do adversario |

A carta de defesa e fixa: anula um acerto em qualquer torre, acumula ate 3 cargas.

### Como se adquire um power-up de ataque

```
Voce destroi a torre lateral inimiga
        v
O power-up CAI da torre em direcao ao seu lado, atravessando a arena inteira
        v
Ele passa pela raquete do ADVERSARIO no caminho
        v
   voce coleta          |  adversario intercepta   |  ninguem pega
   com a raquete        |  (uma vez por partida)   |
        v               |          v               |       v
   ativa por 5-7 s      |     voce perde           |  elegivel para redencao
```

- Somente a RAQUETE coleta; a bola atravessa sem interagir
- **Maximo de 2 power-ups de ataque por jogador por partida** (uma por torre lateral)
- A interceptacao e unica por partida: gastou, o proximo drop passa livre

**O contrapeso central:** coletar exige a raquete, que e a mesma que esta defendendo.
Destruir a torre nao da o premio — da uma ESCOLHA entre buscar o power-up e manter a
defesa.

### Informacao assimetrica

O adversario VE quais cartas voce trouxe antes da partida, mas NAO sabe qual esta em qual
torre. Durante a partida o deck nao aparece.

### Combinacao

Coletar um power-up com outro ainda ativo: os efeitos SOMAM, mas a duracao total passa a
ser apenas **3,5 segundos**.

Isso cria decisao de ritmo: correr para a segunda torre e combinar forte e curto, ou
espacar e ter dois periodos longos.

### Ciclo de elixir e redencao

O elixir nao e moeda: e um **metronomo global** de ~20 s, compartilhado pelos dois
jogadores. A cada batida, cada jogador recebe +1 carga de defesa (maximo 3).

**Redencao:** se o jogador tiver 3 cargas limpas (60 s sem tomar nenhum acerto) E tiver
perdido um drop por nao ter conseguido coleta-lo, o power-up perdido CAI DE NOVO. Isso
consome as tres cargas e para o elixir dele: ele entra em **modo berserk**, so ataque, sem
defesa pelo resto da partida.

Acontece no maximo uma vez por partida. O drop de redencao nao pode ser interceptado.

---

## O que precisa ser projetado

**Quais power-ups de ATAQUE devem existir no repositorio?**

Restricoes que qualquer proposta precisa respeitar:

1. **Sao de ataque.** A defesa e padronizada e nao entra na escolha.
2. **Duram 5-7 segundos** (ou 3,5 s quando combinados).
3. **Sao raros:** no maximo 2 por jogador por partida.
4. **A bola e COMPARTILHADA.** Um efeito que muda a bola afeta os dois jogadores. Um
   power-up que acelera a bola ajuda voce enquanto ataca e te atrapalha quando ela volta.
   Isso precisa ser resolvido em cada carta: ou o efeito e assimetrico por natureza, ou a
   carta assume conscientemente o risco de dois gumes.
5. **Precisam somar bem entre si**, porque a combinacao de dois e uma mecanica central.
6. **Nao podem quebrar a fisica** nem gerar estados travados (bola presa, partida sem fim).
7. **Precisam ter counterplay:** o defensor tem que poder fazer alguma coisa.
8. **Precisam funcionar sob latencia** (multiplayer online planejado) e em celular fraco.

**Ideias iniciais do prompt original**, ainda nao validadas nem classificadas:
Canhao (atira na bola mudando a direcao), Turbina (+40% de velocidade), Ima (atrai a bola
para um lado), Multibola (cria 3 bolas), Portal (teletransporta a bola), mais tres
defensivas que ficaram sem uso com a padronizacao da defesa: Escudo, Muro, Congelamento.

**Quantidade:** o repositorio precisa ser grande o bastante para a escolha de 2 ser
interessante, e servir de eixo de progressao (desbloquear cartas novas). Nao ha numero
definido.

---

## Criterios de avaliacao de qualquer mecanica nova

Do documento de design do projeto, toda mecanica deve responder:

1. Cria decisoes interessantes?
2. Recompensa habilidade?
3. Pode ser explorada de forma degenerada?
4. Cria situacoes frustrantes?
5. Tem counterplay?
6. Quebra a fisica da bola?
7. Cria vantagem excessiva?
8. Funciona no multiplayer?
9. Funciona em aparelho fraco?

E dois principios que valem acima de tudo:

- O jogo NAO pode virar "Clash Royale com Pong". Sistemas demais competindo com a bola
  descaracterizam o jogo.
- Nenhuma carta pode permitir vencer sem habilidade mecanica. As cartas ampliam as
  possibilidades estrategicas sem eliminar o reflexo.

---

## Restricoes tecnicas relevantes para o design

- A simulacao roda em passo fixo de 60 Hz e e determinística: nada de aleatoriedade fora de
  uma semente guardada no estado.
- Teto rigido de 8 bolas simultaneas (Multibola precisa caber nisso).
- Toda regra roda sem interface, para o servidor poder validar. Efeitos nao podem depender
  de animacao ou de leitura de tela.
- Efeitos sao aplicados a bola, a raquete, as torres ou a arena. Qualquer coisa fora desses
  quatro alvos exige sistema novo.
- O jogo e mobile: entrada e um dedo arrastando. Power-ups nao podem exigir input adicional
  complexo (mirar, escolher alvo, dois toques simultaneos).
