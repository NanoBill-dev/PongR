using System;
using System.Numerics;
using NUnit.Framework;
using PongRoyale.Core.Ball;
using PongRoyale.Core.Combat;
using PongRoyale.Core.Events;
using PongRoyale.Core.Paddle;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Tests.EditMode
{
    /// <summary>
    /// Decaimento do dano em acertos consecutivos de torre.
    ///
    /// Motivo: uma bola pinballando atras da raquete derrubava uma torre lateral em
    /// segundos, por um unico erro do jogador. Punicao desproporcional e geradora de bola
    /// de neve. O primeiro acerto continua valendo cheio — o que perde forca e o acidente,
    /// nao a jogada bem colocada.
    /// </summary>
    public sealed class TowerDamageDecayTests
    {
        private MatchState state;
        private MatchEventQueue events;

        [SetUp]
        public void SetUp()
        {
            state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);
            events = new MatchEventQueue();

            // Raquetes fora do caminho: aqui interessa a bola contra as torres.
            SetPaddleX(PlayerSlot.Bottom, 4f);
            SetPaddleX(PlayerSlot.Top, 4f);
        }

        private void SetPaddleX(PlayerSlot slot, float x)
        {
            ref PaddleState paddle = ref state.GetPaddle(slot);
            paddle.PositionX = x;
            paddle.PreviousPositionX = x;
            paddle.TargetX = x;
        }

        private void Step(int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                BallResolver.Advance(state, MatchConstants.FixedDeltaTime, events);
                state.Tick++;
            }
        }

        /// <summary>Dano de um unico acerto, dado um numero de acertos consecutivos previos.</summary>
        private float DamageAfterConsecutiveHits(byte previousHits)
        {
            state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);
            events = new MatchEventQueue();
            SetPaddleX(PlayerSlot.Bottom, 4f);

            float healthBefore = state.GetTower(PlayerSlot.Bottom, TowerKind.King).Health;

            state.Balls[0] = BallState.Create(new Vector2(0f, -6.9f), new Vector2(0f, -1f), 8f, 250f);
            state.Balls[0].ConsecutiveTowerHits = previousHits;

            Step(3);

            return healthBefore - state.GetTower(PlayerSlot.Bottom, TowerKind.King).Health;
        }

        [Test]
        public void FirstHitDealsFullDamage()
        {
            // A jogada boa e recompensada por inteiro. So o pinball decai.
            Assert.AreEqual(250f, DamageAfterConsecutiveHits(0), 1e-2f);
        }

        [Test]
        public void EachConsecutiveHitDealsLess()
        {
            float first = DamageAfterConsecutiveHits(0);
            float second = DamageAfterConsecutiveHits(1);
            float third = DamageAfterConsecutiveHits(2);

            Assert.Less(second, first);
            Assert.Less(third, second);
        }

        [Test]
        public void DecayFollowsTheConfiguredRate()
        {
            // 65% do anterior a cada acerto.
            Assert.AreEqual(250f * 0.65f, DamageAfterConsecutiveHits(1), 1e-2f);
            Assert.AreEqual(250f * 0.65f * 0.65f, DamageAfterConsecutiveHits(2), 1e-2f);
        }

        [Test]
        public void DamageNeverFallsBelowTheFloor()
        {
            // Sem piso, a area atras da propria raquete viraria o lugar mais seguro do jogo
            // e daria para estagnar a partida de proposito.
            float expectedFloor = 250f * 0.2f;

            Assert.AreEqual(expectedFloor, DamageAfterConsecutiveHits(20), 1e-2f);
            Assert.AreEqual(expectedFloor, DamageAfterConsecutiveHits(200), 1e-2f);
        }

        [Test]
        public void ReturningToPlayRestoresFullDamage()
        {
            // A bola volta ao campo entre as duas linhas de raquete: o proximo ataque
            // comeca do zero.
            state.Balls[0] = BallState.Create(new Vector2(0f, 0f), new Vector2(0f, 1f), 8f, 250f);
            state.Balls[0].ConsecutiveTowerHits = 5;

            Step(1);

            Assert.AreEqual(0, state.Balls[0].ConsecutiveTowerHits);
        }

        [Test]
        public void DecayPersistsWhileTheBallStaysBehindThePaddleLine()
        {
            // Enquanto nao voltar ao campo, o contador nao pode zerar sozinho.
            float lineY = state.GetPaddle(PlayerSlot.Bottom).LineY;

            state.Balls[0] = BallState.Create(
                new Vector2(-1.75f, lineY - 0.4f), new Vector2(0f, -1f), 8f, 250f);
            state.Balls[0].ConsecutiveTowerHits = 3;

            Step(1);

            Assert.AreEqual(3, state.Balls[0].ConsecutiveTowerHits);
        }

        [Test]
        public void TrappedBallNoLongerMeltsAGuardTower()
        {
            // O cenario reportado: bola presa atras da raquete castigando uma torre lateral.
            // Antes, 10 acertos derrubavam os 2500 de vida. Agora precisa de muito mais.
            state.GetTower(PlayerSlot.Bottom, TowerKind.LeftGuard).Health = 2500f;

            float total = 0f;
            for (byte hit = 0; hit < 10; hit++)
            {
                total += DamageAfterConsecutiveHits(hit);
            }

            Assert.Less(total, 1200f, "Dez acertos seguidos ainda derrubariam a torre.");
            Assert.Greater(total, 500f, "O decaimento ficou punitivo demais: a bola virou inofensiva.");
        }

        [Test]
        public void EachBallCarriesItsOwnDecay()
        {
            // Importa para a Multibola da FASE 2: o castigo de uma bola nao pode aliviar o
            // dano da outra.
            //
            // As raquetes vao para a ponta esquerda porque em x = 4 elas cobririam a torre
            // lateral direita, e a bola ficaria quicando no corredor entre as duas — o
            // proprio pinball que este sistema existe para conter, que aqui so poluiria a
            // medicao.
            SetPaddleX(PlayerSlot.Bottom, -3.8f);
            SetPaddleX(PlayerSlot.Top, -3.8f);

            state.Balls[0] = BallState.Create(new Vector2(0f, -6.9f), new Vector2(0f, -1f), 8f, 250f);
            state.Balls[0].ConsecutiveTowerHits = 4;

            // A torre lateral e mais baixa que a Rei (meia-altura 0.7 contra 0.8), entao a
            // bola precisa comecar mais perto para as duas colisoes caberem na mesma janela.
            state.Balls[1] = BallState.Create(new Vector2(3.2f, -7.0f), new Vector2(0f, -1f), 8f, 250f);

            float kingBefore = state.GetTower(PlayerSlot.Bottom, TowerKind.King).Health;
            float guardBefore = state.GetTower(PlayerSlot.Bottom, TowerKind.RightGuard).Health;

            Step(6);

            float kingDamage = kingBefore - state.GetTower(PlayerSlot.Bottom, TowerKind.King).Health;
            float guardDamage = guardBefore - state.GetTower(PlayerSlot.Bottom, TowerKind.RightGuard).Health;

            Assert.Less(kingDamage, guardDamage, "A bola castigada deveria doer menos que a bola nova.");
            Assert.AreEqual(250f, guardDamage, 1e-2f);
        }

        [Test]
        public void DamageEventReportsTheValueActuallyApplied()
        {
            // A HUD vai ler este evento para mostrar o numero do dano. Se ele reportasse o
            // dano base, o jogador veria 250 enquanto a torre perde 106.
            state.Balls[0] = BallState.Create(new Vector2(0f, -6.9f), new Vector2(0f, -1f), 8f, 250f);
            state.Balls[0].ConsecutiveTowerHits = 2;

            float healthBefore = state.GetTower(PlayerSlot.Bottom, TowerKind.King).Health;
            Step(3);
            float applied = healthBefore - state.GetTower(PlayerSlot.Bottom, TowerKind.King).Health;

            float reported = 0f;
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].Type == MatchEventType.TowerDamaged)
                {
                    reported = events[i].Value;
                }
            }

            Assert.AreEqual(applied, reported, 1e-2f);
        }
    }
}
