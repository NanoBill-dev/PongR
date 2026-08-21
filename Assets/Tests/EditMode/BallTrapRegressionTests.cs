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
    /// Regressoes dos travamentos reportados jogando em 2026-08-20.
    ///
    /// Os tres sintomas relatados — bola grudada na raquete, trava ao arrastar a raquete
    /// junto da parede, e velocidade disparando "do nada" — vinham da mesma causa: a bola
    /// chegando por TRAS da raquete recebia a deflexao de defesa, que a empurrava para
    /// dentro da propria raquete. Presa, ela colidia dezenas de vezes por segundo e cada
    /// colisao somava os 2% de ganho por rebatida.
    /// </summary>
    public sealed class BallTrapRegressionTests
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

        private void SetPaddleX(PlayerSlot slot, float x)
        {
            ref PaddleState paddle = ref state.GetPaddle(slot);
            paddle.PositionX = x;
            paddle.PreviousPositionX = x;
            paddle.TargetX = x;
        }

        [Test]
        public void BallBehindThePaddleFindsItsWayBackIntoPlay()
        {
            // Cenario real: a bola passou pelo vao entre a Torre Rei e a lateral esquerda e
            // esta no corredor atras da raquete. Ela precisa conseguir voltar ao jogo,
            // contornando a raquete pela lateral, em vez de ficar quicando ali para sempre.
            //
            // x = -1.75 fica no vao: a Torre Rei vai ate -1.2 e a lateral comeca em -2.3.
            const float GapX = -1.75f;
            SetPaddleX(PlayerSlot.Bottom, GapX);
            float lineY = state.GetPaddle(PlayerSlot.Bottom).LineY;

            state.Balls[0] = BallState.Create(
                new Vector2(GapX, lineY - 0.45f), new Vector2(0.6f, 0.8f), 10f, 250f);

            bool returnedToPlay = false;
            for (int i = 0; i < 180 && !returnedToPlay; i++)
            {
                Step(1);
                returnedToPlay = state.Balls[0].Position.Y > lineY;
            }

            Assert.IsTrue(returnedToPlay, "A bola ficou presa no corredor atras da raquete.");
        }

        [Test]
        public void BackFaceContactDoesNotStealTheSpeedBonus()
        {
            // Bater nas costas da raquete nao e defesa: nao pode render os 2% de ganho.
            SetPaddleX(PlayerSlot.Bottom, 0f);
            float lineY = state.GetPaddle(PlayerSlot.Bottom).LineY;

            state.Balls[0] = BallState.Create(
                new Vector2(0f, lineY - 0.6f), new Vector2(0f, 1f), 8f, 250f);

            Step(30);

            Assert.AreEqual(8f, state.Balls[0].BaseSpeed, 1e-3f);
        }

        [Test]
        public void BackFaceContactDoesNotChangeBallOwnership()
        {
            SetPaddleX(PlayerSlot.Bottom, 0f);
            float lineY = state.GetPaddle(PlayerSlot.Bottom).LineY;

            state.Balls[0] = BallState.Create(
                new Vector2(0f, lineY - 0.6f), new Vector2(0f, 1f), 8f, 250f);

            Step(30);

            Assert.AreEqual(
                BallState.NoPlayer,
                state.Balls[0].LastHitByPlayer,
                "Encostar nas costas da raquete nao credita a rebatida a ninguem.");
        }

        [Test]
        public void SpeedCannotRunAwayWhileTheBallLingersNearThePaddle()
        {
            // O sintoma reportado: a bola ficava rapida de uma hora para outra. Com 8 u/s
            // iniciais, dois segundos de jogo cabem poucas rebatidas legitimas.
            SetPaddleX(PlayerSlot.Bottom, 0f);
            float lineY = state.GetPaddle(PlayerSlot.Bottom).LineY;

            state.Balls[0] = BallState.Create(
                new Vector2(0f, lineY + 0.3f), new Vector2(0f, -1f), 8f, 250f);

            Step(120);

            Assert.Less(
                state.Balls[0].BaseSpeed,
                10f,
                "A velocidade disparou: a bola esta colidindo repetidamente com a raquete.");
        }

        [Test]
        public void PaddleSweepingIntoTheBallPushesItAllTheWayOut()
        {
            // A raquete anda para dentro da bola. Uma folga simbolica deixaria a bola ainda
            // sobreposta e ela voltaria a colidir no tick seguinte, indefinidamente.
            float lineY = state.GetPaddle(PlayerSlot.Bottom).LineY;
            state.Balls[0] = BallState.Create(new Vector2(0f, lineY), new Vector2(0f, -1f), 8f, 250f);

            ref PaddleState paddle = ref state.GetPaddle(PlayerSlot.Bottom);
            paddle.PreviousPositionX = -2f;
            paddle.PositionX = 0f;

            Step(1);

            // O desencaixe pode sair por qualquer eixo — empurrar de lado e tao valido
            // quanto empurrar para cima. O que importa e nao restar sobreposicao.
            Assert.IsFalse(
                IsOverlappingPaddle(PlayerSlot.Bottom),
                "A bola continuou dentro da raquete depois da resolucao.");
        }

        [Test]
        public void BallPinnedBetweenPaddleAndSideWallLeavesImmediately()
        {
            // Relatado no playtest: com a raquete encostada na parede, a bola grudava por
            // alguns instantes antes de sair. A separacao lateral jogava a bola para fora da
            // arena e o clamp de seguranca a devolvia para dentro da raquete — as duas
            // garantias brigando. Aqui a saida tem que ser pelo eixo vertical.
            float limitX = state.Config.Arena.HalfWidth - state.Config.Paddle.HalfWidth;
            SetPaddleX(PlayerSlot.Bottom, limitX);

            float lineY = state.GetPaddle(PlayerSlot.Bottom).LineY;
            state.Balls[0] = BallState.Create(
                new Vector2(limitX + 0.9f, lineY), new Vector2(0f, -1f), 10f, 250f);

            Step(1);

            Assert.IsFalse(
                IsOverlappingPaddle(PlayerSlot.Bottom),
                "A bola continuou presa entre a raquete e a parede.");
            Assert.LessOrEqual(
                Math.Abs(state.Balls[0].Position.X),
                state.Config.Arena.HalfWidth - state.Config.Ball.Radius + 1e-3f,
                "A separacao empurrou a bola para fora da arena.");
        }

        [Test]
        public void BallStaysInsideTheArenaWhilePinnedAgainstEitherWall()
        {
            // Os dois lados, por varios ticks: nem sobreposicao nem fuga pela parede.
            foreach (float side in new[] { -1f, 1f })
            {
                state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);
                events = new MatchEventQueue();

                float limitX = side * (state.Config.Arena.HalfWidth - state.Config.Paddle.HalfWidth);
                SetPaddleX(PlayerSlot.Bottom, limitX);

                float lineY = state.GetPaddle(PlayerSlot.Bottom).LineY;
                state.Balls[0] = BallState.Create(
                    new Vector2(limitX + side * 0.9f, lineY + 0.1f), new Vector2(side, -1f), 14f, 250f);

                for (int tick = 0; tick < 60; tick++)
                {
                    Step(1);

                    Assert.LessOrEqual(
                        Math.Abs(state.Balls[0].Position.X),
                        state.Config.Arena.HalfWidth,
                        $"Bola saiu da arena no lado {side}, tick {tick}.");
                }

                Assert.IsFalse(
                    IsOverlappingPaddle(PlayerSlot.Bottom),
                    $"Bola terminou dentro da raquete no lado {side}.");
            }
        }

        private bool IsOverlappingPaddle(PlayerSlot slot)
        {
            PaddleState paddle = state.GetPaddle(slot);
            float radius = state.Config.Ball.Radius;

            float horizontalGap = Math.Abs(state.Balls[0].Position.X - paddle.PositionX);
            float verticalGap = Math.Abs(state.Balls[0].Position.Y - paddle.LineY);

            return horizontalGap < state.Config.Paddle.HalfWidth + radius
                   && verticalGap < state.Config.Paddle.Thickness * 0.5f + radius;
        }

        [Test]
        public void BallCornoredBetweenPaddleAndWallGetsFree()
        {
            // Exatamente o cenario relatado: arrastar a raquete contra a parede com a bola
            // no meio. A bola precisa sair, nao vibrar presa no canto.
            float lineY = state.GetPaddle(PlayerSlot.Bottom).LineY;
            float wallX = state.Config.Arena.HalfWidth - state.Config.Ball.Radius;

            state.Balls[0] = BallState.Create(
                new Vector2(wallX - 0.05f, lineY + 0.2f), new Vector2(1f, -1f), 12f, 250f);

            ref PaddleState paddle = ref state.GetPaddle(PlayerSlot.Bottom);
            paddle.PreviousPositionX = 2f;
            paddle.PositionX = 3.8f;
            paddle.TargetX = 3.8f;

            Step(90);

            // Escapar do canto significa voltar ao campo, acima da linha da raquete —
            // e nao ficar vibrando entre a raquete e a parede.
            Assert.Greater(
                state.Balls[0].Position.Y,
                lineY,
                "A bola nao escapou do canto entre a raquete e a parede.");
            Assert.Less(
                state.Balls[0].BaseSpeed,
                16f,
                "A velocidade cresceu demais: sinal de colisoes repetidas no canto.");
        }

        [Test]
        public void BallNudgedOutsideTheWallIsBroughtBackAndReflected()
        {
            // Bug irmao: com a bola alem do limite, a varredura calculava tempo negativo e
            // descartava a colisao. A bola era reposicionada todo tick sem nunca refletir —
            // grudada na parede.
            float wallX = state.Config.Arena.HalfWidth - state.Config.Ball.Radius;

            state.Balls[0] = BallState.Create(
                new Vector2(wallX + 0.2f, 0f), new Vector2(1f, 0.2f), 10f, 250f);

            Step(2);

            Assert.Less(state.Balls[0].Direction.X, 0f, "A bola precisa refletir de volta para dentro.");
            Assert.LessOrEqual(state.Balls[0].Position.X, wallX + 1e-3f);
        }

        [Test]
        public void FrontFaceStillBehavesLikeADefence()
        {
            // Guarda contra a correcao ter ido longe demais: a defesa normal precisa
            // continuar rendendo ganho de velocidade e posse da bola.
            SetPaddleX(PlayerSlot.Bottom, 0f);
            float lineY = state.GetPaddle(PlayerSlot.Bottom).LineY;

            state.Balls[0] = BallState.Create(
                new Vector2(0f, lineY + 0.6f), new Vector2(0f, -1f), 8f, 250f);

            Step(10);

            Assert.Greater(state.Balls[0].Direction.Y, 0f);
            Assert.AreEqual(8f * 1.02f, state.Balls[0].BaseSpeed, 1e-3f);
            Assert.AreEqual((sbyte)PlayerSlot.Bottom, state.Balls[0].LastHitByPlayer);
        }
    }
}
