using System;
using NUnit.Framework;
using PongRoyale.Core.Paddle;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Tests.EditMode
{
    public sealed class PaddleResolverTests
    {
        private const float Tolerance = 1e-4f;

        private MatchState state;

        [SetUp]
        public void SetUp()
        {
            state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);
        }

        private void Step(int ticks = 1)
        {
            for (int i = 0; i < ticks; i++)
            {
                PaddleResolver.Advance(state, MatchConstants.FixedDeltaTime);
            }
        }

        private float LegalLimit =>
            state.Config.Arena.HalfWidth - state.Config.Paddle.HalfWidth;

        [Test]
        public void RequestedTargetIsClampedToTheLegalRange()
        {
            PaddleResolver.SetTarget(ref state.GetPaddle(PlayerSlot.Bottom), 999f, state.Config);

            Assert.AreEqual(LegalLimit, state.GetPaddle(PlayerSlot.Bottom).TargetX, Tolerance);
        }

        [Test]
        public void WholePaddleFitsInsideTheArena()
        {
            // O limite e o centro da arena menos a MEIA-LARGURA da raquete: encostar o
            // centro na parede deixaria metade da raquete para fora.
            PaddleResolver.SetTarget(ref state.GetPaddle(PlayerSlot.Bottom), 999f, state.Config);
            Step(300);

            float rightEdge = state.GetPaddle(PlayerSlot.Bottom).PositionX + state.Config.Paddle.HalfWidth;

            Assert.LessOrEqual(rightEdge, state.Config.Arena.HalfWidth + Tolerance);
        }

        [Test]
        public void ASingleTickCannotTeleportAcrossTheArena()
        {
            // Protecao central da secao 20. Mesmo com um comando pedindo a outra ponta da
            // arena, um tick so pode render MaxSpeed * deltaTime.
            ref PaddleState paddle = ref state.GetPaddle(PlayerSlot.Bottom);
            PaddleResolver.SetTarget(ref paddle, LegalLimit, state.Config);
            float before = paddle.PositionX;

            Step();

            float travelled = Math.Abs(state.GetPaddle(PlayerSlot.Bottom).PositionX - before);
            float budget = state.Config.Paddle.MaxSpeed * MatchConstants.FixedDeltaTime;

            Assert.LessOrEqual(travelled, budget + Tolerance);
        }

        [Test]
        public void SpeedNeverExceedsTheConfiguredMaximum()
        {
            var random = new Random(20260820);
            float budget = state.Config.Paddle.MaxSpeed + Tolerance;

            for (int tick = 0; tick < 2000; tick++)
            {
                // Um cliente adulterado poderia mandar exatamente isto: alvos absurdos,
                // trocando de lado a cada tick.
                float hostileTarget = (float)(random.NextDouble() * 2000.0 - 1000.0);
                PaddleResolver.SetTarget(ref state.GetPaddle(PlayerSlot.Bottom), hostileTarget, state.Config);

                Step();

                Assert.LessOrEqual(
                    Math.Abs(state.GetPaddle(PlayerSlot.Bottom).VelocityX),
                    budget,
                    $"Velocidade impossivel no tick {tick}.");
                Assert.LessOrEqual(
                    Math.Abs(state.GetPaddle(PlayerSlot.Bottom).PositionX),
                    LegalLimit + Tolerance,
                    $"Raquete fora da arena no tick {tick}.");
            }
        }

        [Test]
        public void PaddleEventuallyReachesTheTarget()
        {
            PaddleResolver.SetTarget(ref state.GetPaddle(PlayerSlot.Bottom), 2f, state.Config);

            Step(120);

            Assert.AreEqual(2f, state.GetPaddle(PlayerSlot.Bottom).PositionX, 1e-2f);
        }

        [Test]
        public void VelocityMatchesTheDisplacementItProduced()
        {
            PaddleResolver.SetTarget(ref state.GetPaddle(PlayerSlot.Bottom), 3f, state.Config);
            float before = state.GetPaddle(PlayerSlot.Bottom).PositionX;

            Step();

            PaddleState paddle = state.GetPaddle(PlayerSlot.Bottom);
            float expected = (paddle.PositionX - before) / MatchConstants.FixedDeltaTime;

            Assert.AreEqual(expected, paddle.VelocityX, Tolerance);
        }

        [Test]
        public void PreviousPositionHoldsWhereThePaddleStartedTheTick()
        {
            // A varredura da bola depende deste valor. Se ele parar de ser atualizado, a
            // colisao passa a mirar uma raquete fantasma na posicao do tick anterior.
            PaddleResolver.SetTarget(ref state.GetPaddle(PlayerSlot.Bottom), 3f, state.Config);
            float before = state.GetPaddle(PlayerSlot.Bottom).PositionX;

            Step();

            Assert.AreEqual(before, state.GetPaddle(PlayerSlot.Bottom).PreviousPositionX, Tolerance);
        }

        [Test]
        public void StandingStillProducesNoVelocity()
        {
            Step(10);

            Assert.AreEqual(0f, state.GetPaddle(PlayerSlot.Bottom).VelocityX, Tolerance);
            Assert.AreEqual(0f, state.GetPaddle(PlayerSlot.Bottom).PositionX, Tolerance);
        }

        [Test]
        public void BothPaddlesMoveIndependently()
        {
            PaddleResolver.SetTarget(ref state.GetPaddle(PlayerSlot.Bottom), 2f, state.Config);
            PaddleResolver.SetTarget(ref state.GetPaddle(PlayerSlot.Top), -2f, state.Config);

            Step(120);

            Assert.AreEqual(2f, state.GetPaddle(PlayerSlot.Bottom).PositionX, 1e-2f);
            Assert.AreEqual(-2f, state.GetPaddle(PlayerSlot.Top).PositionX, 1e-2f);
        }

        [Test]
        public void PaddlesKeepTheirOwnLine()
        {
            PaddleResolver.SetTarget(ref state.GetPaddle(PlayerSlot.Bottom), 3f, state.Config);
            Step(60);

            Assert.Less(state.GetPaddle(PlayerSlot.Bottom).LineY, 0f);
            Assert.Greater(state.GetPaddle(PlayerSlot.Top).LineY, 0f);
        }
    }
}
