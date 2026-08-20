using System;
using System.Numerics;
using NUnit.Framework;
using PongRoyale.Core.Ball;
using PongRoyale.Core.Combat;
using PongRoyale.Core.Events;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Tests.EditMode
{
    public sealed class BallResolverTests
    {
        private const float Tolerance = 1e-3f;

        private MatchState state;
        private MatchEventQueue events;

        [SetUp]
        public void SetUp()
        {
            state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);
            events = new MatchEventQueue();
        }

        private void Step(int ticks = 1)
        {
            for (int i = 0; i < ticks; i++)
            {
                BallResolver.Advance(state, MatchConstants.FixedDeltaTime, events);
                state.Tick++;
            }
        }

        /// <summary>Tira as raquetes do caminho para testar a bola contra outras superficies.</summary>
        private void MovePaddlesAside()
        {
            state.GetPaddle(PlayerSlot.Bottom).PositionX = 4f;
            state.GetPaddle(PlayerSlot.Top).PositionX = 4f;
        }

        private void PlaceBall(Vector2 position, Vector2 direction, float speed)
        {
            state.Balls[0] = BallState.Create(position, direction, speed, state.Config.Ball.BaseDamage);
        }

        [Test]
        public void FreeFlightAdvancesExactlyBySpeedTimesTime()
        {
            PlaceBall(Vector2.Zero, new Vector2(0f, 1f), 8f);

            Step();

            Assert.AreEqual(8f * MatchConstants.FixedDeltaTime, state.Balls[0].Position.Y, Tolerance);
            Assert.AreEqual(0, events.Count, "Voo livre nao deveria gerar evento nenhum.");
        }

        [Test]
        public void SideWallSendsTheBallBackInside()
        {
            MovePaddlesAside();
            PlaceBall(new Vector2(4.6f, 0f), new Vector2(1f, 0.5f), 20f);

            Step(3);

            Assert.Less(state.Balls[0].Direction.X, 0f, "A bola precisa voltar para dentro.");
            Assert.LessOrEqual(
                Math.Abs(state.Balls[0].Position.X),
                state.Config.Arena.HalfWidth - state.Config.Ball.Radius + Tolerance);
        }

        [Test]
        public void PaddleSendsTheBallBackAndClaimsOwnership()
        {
            PlaceBall(new Vector2(0f, -6.0f), new Vector2(0f, -1f), 8f);

            Step(2);

            Assert.Greater(state.Balls[0].Direction.Y, 0f, "A raquete de baixo devolve a bola para cima.");
            Assert.AreEqual((sbyte)PlayerSlot.Bottom, state.Balls[0].LastHitByPlayer);
            Assert.Greater(state.Balls[0].CollisionSequence, 0);
        }

        [Test]
        public void EachPaddleHitAddsTwoPercentOfSpeed()
        {
            PlaceBall(new Vector2(0f, -6.0f), new Vector2(0f, -1f), 8f);

            Step(2);

            Assert.AreEqual(8f * 1.02f, state.Balls[0].BaseSpeed, Tolerance);
        }

        [Test]
        public void SpeedSaturatesAtTheConfiguredMaximum()
        {
            // Raquetes alinhadas no centro: a bola fica num vai-e-vem vertical e acumula
            // rebatidas ate bater no teto de velocidade.
            state.GetPaddle(PlayerSlot.Bottom).PositionX = 0f;
            state.GetPaddle(PlayerSlot.Top).PositionX = 0f;
            PlaceBall(Vector2.Zero, new Vector2(0f, -1f), 8f);

            Step(ticks: 12000);

            Assert.AreEqual(state.Config.Ball.MaxSpeed, state.Balls[0].BaseSpeed, Tolerance);
            Assert.LessOrEqual(state.Balls[0].BaseSpeed, state.Config.Ball.MaxSpeed + Tolerance);
        }

        [Test]
        public void HittingTheRightSideOfThePaddleSendsTheBallRight()
        {
            // A raquete esta deslocada para a esquerda, entao a bola toca na metade direita.
            state.GetPaddle(PlayerSlot.Bottom).PositionX = -0.8f;
            PlaceBall(new Vector2(0f, -6.0f), new Vector2(0f, -1f), 8f);

            Step(2);

            Assert.Greater(state.Balls[0].Direction.X, 0f);
        }

        [Test]
        public void HittingTheLeftSideOfThePaddleSendsTheBallLeft()
        {
            state.GetPaddle(PlayerSlot.Bottom).PositionX = 0.8f;
            PlaceBall(new Vector2(0f, -6.0f), new Vector2(0f, -1f), 8f);

            Step(2);

            Assert.Less(state.Balls[0].Direction.X, 0f);
        }

        [Test]
        public void CenterHitReturnsTheBallStraight()
        {
            state.GetPaddle(PlayerSlot.Bottom).PositionX = 0f;
            PlaceBall(new Vector2(0f, -6.0f), new Vector2(0f, -1f), 8f);

            Step(2);

            Assert.AreEqual(0f, state.Balls[0].Direction.X, Tolerance);
        }

        [Test]
        public void AbsurdSpeedStillDoesNotCrossThePaddle()
        {
            // 200 u/s e oito vezes o teto do jogo. Serve para provar que a solucao e por
            // varredura de verdade, e nao um teste de sobreposicao que so parece funcionar
            // porque as velocidades atuais sao baixas.
            state.GetPaddle(PlayerSlot.Bottom).PositionX = 0f;
            PlaceBall(new Vector2(0f, -6.0f), new Vector2(0f, -1f), 200f);

            Step();

            Assert.Greater(state.Balls[0].Direction.Y, 0f, "A bola atravessou a raquete.");
        }

        [Test]
        public void TowerTakesDamageAndTheBallBouncesOff()
        {
            MovePaddlesAside();
            float healthBefore = state.GetTower(PlayerSlot.Bottom, TowerKind.King).Health;
            PlaceBall(new Vector2(0f, -6.9f), new Vector2(0f, -1f), 8f);

            Step(3);

            float healthAfter = state.GetTower(PlayerSlot.Bottom, TowerKind.King).Health;

            Assert.AreEqual(healthBefore - state.Config.Ball.BaseDamage, healthAfter, Tolerance);
            Assert.Greater(state.Balls[0].Direction.Y, 0f, "A bola precisa quicar na torre.");
        }

        [Test]
        public void DestroyingATowerCreditsTheOpponent()
        {
            MovePaddlesAside();
            state.GetTower(PlayerSlot.Bottom, TowerKind.King).Health = 100f;
            PlaceBall(new Vector2(0f, -6.9f), new Vector2(0f, -1f), 8f);

            Step(3);

            Assert.IsFalse(state.IsKingAlive(PlayerSlot.Bottom));
            Assert.AreEqual(1, state.GetPlayer(PlayerSlot.Top).TowersDestroyed);
            Assert.IsTrue(HasEvent(MatchEventType.TowerDestroyed));
        }

        [Test]
        public void DestroyedTowerStopsBlockingAndStopsTakingDamage()
        {
            MovePaddlesAside();
            state.GetTower(PlayerSlot.Bottom, TowerKind.King).Health = 0f;
            PlaceBall(new Vector2(0f, -6.9f), new Vector2(0f, -1f), 8f);

            Step(3);

            Assert.IsFalse(HasEvent(MatchEventType.TowerDamaged), "Torre destruida nao pode continuar recebendo dano.");
        }

        [Test]
        public void BallNeverEscapesTheArena()
        {
            // Varre muitos angulos iniciais por muitos ticks. Uma bola que escapa significa
            // partida travada, e o bug so apareceria depois de horas de jogo real.
            var random = new Random(20260820);

            for (int run = 0; run < 40; run++)
            {
                state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);
                events = new MatchEventQueue();

                double angle = random.NextDouble() * Math.PI * 2.0;
                PlaceBall(
                    Vector2.Zero,
                    new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)),
                    state.Config.Ball.InitialSpeed);

                state.GetPaddle(PlayerSlot.Bottom).PositionX = (float)(random.NextDouble() * 6.0 - 3.0);
                state.GetPaddle(PlayerSlot.Top).PositionX = (float)(random.NextDouble() * 6.0 - 3.0);

                for (int tick = 0; tick < 600; tick++)
                {
                    BallResolver.Advance(state, MatchConstants.FixedDeltaTime, events);
                    state.Tick++;
                    events.Clear();

                    Assert.LessOrEqual(
                        Math.Abs(state.Balls[0].Position.X),
                        state.Config.Arena.HalfWidth,
                        $"Bola escapou pela lateral na execucao {run}, tick {tick}.");
                    Assert.LessOrEqual(
                        Math.Abs(state.Balls[0].Position.Y),
                        state.Config.Arena.HalfHeight,
                        $"Bola escapou pelo fundo na execucao {run}, tick {tick}.");
                }
            }
        }

        [Test]
        public void BallNeverExceedsTheSpeedCeilingDuringALongRally()
        {
            state.GetPaddle(PlayerSlot.Bottom).PositionX = 0f;
            state.GetPaddle(PlayerSlot.Top).PositionX = 0f;
            PlaceBall(Vector2.Zero, new Vector2(0.2f, -1f), 8f);

            for (int tick = 0; tick < 6000; tick++)
            {
                BallResolver.Advance(state, MatchConstants.FixedDeltaTime, events);
                state.Tick++;
                events.Clear();

                Assert.LessOrEqual(state.Balls[0].CurrentSpeed, state.Config.Ball.MaxSpeed + Tolerance);
            }
        }

        [Test]
        public void CornerDoesNotTrapTheBall()
        {
            // Bola mirando exatamente a quina inferior esquerda. O risco aqui e ficar presa
            // resolvendo a mesma colisao para sempre; o teto de iteracoes evita o travamento.
            MovePaddlesAside();
            PlaceBall(new Vector2(-4.6f, -8.6f), new Vector2(-1f, -1f), 20f);

            Step(120);

            Assert.LessOrEqual(Math.Abs(state.Balls[0].Position.X), state.Config.Arena.HalfWidth);
            Assert.LessOrEqual(Math.Abs(state.Balls[0].Position.Y), state.Config.Arena.HalfHeight);
        }

        [Test]
        public void InactiveBallsAreIgnored()
        {
            state.Balls[0].IsActive = false;
            Vector2 before = state.Balls[0].Position;

            Step(10);

            Assert.AreEqual(before, state.Balls[0].Position);
        }

        [Test]
        public void MultipleBallsAdvanceIndependently()
        {
            PlaceBall(Vector2.Zero, new Vector2(0f, 1f), 8f);
            state.Balls[1] = BallState.Create(Vector2.Zero, new Vector2(0f, -1f), 8f, 250f);

            Step();

            Assert.Greater(state.Balls[0].Position.Y, 0f);
            Assert.Less(state.Balls[1].Position.Y, 0f);
            Assert.AreEqual(2, state.CountActiveBalls());
        }

        private bool HasEvent(MatchEventType type)
        {
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].Type == type)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
