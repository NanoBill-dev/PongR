using System;
using System.Numerics;
using NUnit.Framework;
using PongRoyale.Core.Ball;
using PongRoyale.Core.Events;
using PongRoyale.Core.Paddle;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Tests.EditMode
{
    /// <summary>
    /// Transferencia de velocidade da raquete para a bola.
    ///
    /// Sem ela, a raquete e uma parede: o angulo de saida depende so de ONDE a bola tocou.
    /// Com ela, tambem depende de COMO o jogador estava se movendo no impacto — da para
    /// empurrar a bola no ataque, e da para expulsar a bola que entrou atras da raquete
    /// varrendo com forca, em vez de cutuca-la ate ela sair sozinha.
    /// </summary>
    public sealed class PaddleSweepCarryTests
    {
        private MatchState state;
        private MatchEventQueue events;

        [SetUp]
        public void SetUp()
        {
            state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);
            events = new MatchEventQueue();
        }

        private void Step(int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                BallResolver.Advance(state, MatchConstants.FixedDeltaTime, events);
                state.Tick++;
                events.Clear();
            }
        }

        /// <summary>
        /// Posiciona a raquete parada, porem declarando uma velocidade. Isola o termo de
        /// transferencia: sem deslocamento real, a unica diferenca entre os casos e a
        /// velocidade declarada, e nao o ponto de contato.
        /// </summary>
        private void SetPaddle(float x, float velocityX)
        {
            ref PaddleState paddle = ref state.GetPaddle(PlayerSlot.Bottom);
            paddle.PositionX = x;
            paddle.PreviousPositionX = x;
            paddle.TargetX = x;
            paddle.VelocityX = velocityX;
        }

        private float BounceDirectionX(float paddleVelocityX)
        {
            state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);
            events = new MatchEventQueue();

            SetPaddle(0f, paddleVelocityX);
            float lineY = state.GetPaddle(PlayerSlot.Bottom).LineY;

            state.Balls[0] = BallState.Create(
                new Vector2(0f, lineY + 0.6f), new Vector2(0f, -1f), 8f, 250f);

            Step(10);
            return state.Balls[0].Direction.X;
        }

        [Test]
        public void StandingStillKeepsTheClassicPongBounce()
        {
            // Raquete parada, impacto no centro: a bola volta reta, como sempre voltou.
            Assert.AreEqual(0f, BounceDirectionX(paddleVelocityX: 0f), 1e-3f);
        }

        [Test]
        public void SweepingRightSendsTheBallRight()
        {
            Assert.Greater(BounceDirectionX(paddleVelocityX: 18f), 0.1f);
        }

        [Test]
        public void SweepingLeftSendsTheBallLeft()
        {
            Assert.Less(BounceDirectionX(paddleVelocityX: -18f), -0.1f);
        }

        [Test]
        public void FasterSweepBendsTheBallMore()
        {
            // A intensidade importa: e isso que transforma a varredura em habilidade em vez
            // de um interruptor de liga-desliga.
            float gentle = BounceDirectionX(paddleVelocityX: 6f);
            float hard = BounceDirectionX(paddleVelocityX: 18f);

            Assert.Greater(hard, gentle);
        }

        [Test]
        public void SweepingStillRespectsTheSpeedCeiling()
        {
            // Varrer nao pode virar atalho para furar o teto de velocidade do jogo.
            SetPaddle(0f, velocityX: 500f);
            float lineY = state.GetPaddle(PlayerSlot.Bottom).LineY;

            state.Balls[0] = BallState.Create(
                new Vector2(0f, lineY + 0.6f), new Vector2(0f, -1f), 24f, 250f);

            Step(10);

            Assert.LessOrEqual(
                state.Balls[0].BaseSpeed,
                state.Config.Ball.MaxSpeed + 1e-3f);
        }

        [Test]
        public void SweepingNeverFlattensTheBallIntoAHorizontalDrift()
        {
            // Uma varredura violenta poderia deixar a bola quase horizontal, condenada a
            // nunca mais chegar a uma raquete. O angulo minimo continua valendo.
            SetPaddle(0f, velocityX: 500f);
            float lineY = state.GetPaddle(PlayerSlot.Bottom).LineY;

            state.Balls[0] = BallState.Create(
                new Vector2(0f, lineY + 0.6f), new Vector2(0f, -1f), 8f, 250f);

            Step(10);

            float minimumVertical = (float)Math.Sin(
                state.Config.Ball.MinAngleFromHorizontalDegrees * Math.PI / 180.0);

            Assert.GreaterOrEqual(Math.Abs(state.Balls[0].Direction.Y), minimumVertical - 1e-3f);
        }

        [Test]
        public void SweepingClearsTheBallTrappedBehindThePaddle()
        {
            // O uso que motivou a mecanica: a bola entrou atras da raquete e o jogador varre
            // para expulsa-la. Com a varredura ela precisa ganhar deslocamento lateral bem
            // maior do que ganharia so encostando.
            float clearedWithSweep = ClearingDisplacement(paddleVelocityX: 18f);
            float clearedWithoutSweep = ClearingDisplacement(paddleVelocityX: 0f);

            Assert.Greater(clearedWithSweep, clearedWithoutSweep + 0.5f);
        }

        private float ClearingDisplacement(float paddleVelocityX)
        {
            state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);
            events = new MatchEventQueue();

            SetPaddle(0f, paddleVelocityX);
            float lineY = state.GetPaddle(PlayerSlot.Bottom).LineY;

            // Bola logo abaixo da raquete, subindo contra as costas dela.
            state.Balls[0] = BallState.Create(
                new Vector2(0f, lineY - 0.45f), new Vector2(0f, 1f), 8f, 250f);

            Step(20);
            return Math.Abs(state.Balls[0].Position.X);
        }

        [Test]
        public void BackFaceSweepStillDoesNotCountAsADefence()
        {
            // Varrer para limpar continua nao sendo defesa: sem posse da bola.
            SetPaddle(0f, velocityX: 18f);
            float lineY = state.GetPaddle(PlayerSlot.Bottom).LineY;

            state.Balls[0] = BallState.Create(
                new Vector2(0f, lineY - 0.45f), new Vector2(0f, 1f), 8f, 250f);

            Step(20);

            Assert.AreEqual(BallState.NoPlayer, state.Balls[0].LastHitByPlayer);
        }
    }
}
