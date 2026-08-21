using System.Collections;
using NUnit.Framework;
using PongRoyale.Core.Simulation;
using PongRoyale.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PongRoyale.Tests.PlayMode
{
    /// <summary>
    /// Testes de fiacao da cena Match. O Core ja tem 99 testes provando que a REGRA esta
    /// certa; estes provam que a cena esta LIGADA — referencias atribuidas, views seguindo o
    /// estado, camera enquadrando a arena.
    ///
    /// E o tipo de falha que teste de EditMode nao pega e que so apareceria com o jogo na
    /// tela: um campo do Inspector vazio nao quebra compilacao, quebra a partida.
    /// </summary>
    public sealed class MatchScenePlayModeTests
    {
        private const float Tolerance = 1e-3f;

        [UnitySetUp]
        public IEnumerator LoadMatchScene()
        {
            yield return SceneManager.LoadSceneAsync("Match", LoadSceneMode.Single);
        }

        private static MatchRunner FindRunner()
        {
            var runner = Object.FindFirstObjectByType<MatchRunner>();
            Assert.IsNotNull(runner, "A cena Match precisa ter um MatchRunner.");
            return runner;
        }

        [UnityTest]
        public IEnumerator RunnerStartsTheSimulationWithBalanceDataAttached()
        {
            MatchRunner runner = FindRunner();
            yield return null;

            Assert.IsTrue(runner.IsReady, "O BalanceData nao foi atribuido no Inspector.");
            Assert.AreEqual(MatchPhase.Playing, runner.Simulation.State.Phase);
        }

        [UnityTest]
        public IEnumerator BallActuallyMovesOverTime()
        {
            MatchRunner runner = FindRunner();
            yield return null;

            float startY = runner.Simulation.State.Balls[0].Position.Y;

            yield return new WaitForSeconds(0.5f);

            Assert.AreNotEqual(startY, runner.Simulation.State.Balls[0].Position.Y);
            Assert.Greater(runner.Simulation.State.Tick, 0, "O acumulador nao esta chamando Tick.");
        }

        [UnityTest]
        public IEnumerator BallViewFollowsTheSimulatedBall()
        {
            MatchRunner runner = FindRunner();
            yield return new WaitForSeconds(0.3f);

            var ballObject = GameObject.Find("Ball_0");
            Assert.IsNotNull(ballObject, "A cena precisa ter o objeto Ball_0.");

            var ball = runner.Simulation.State.Balls[0];

            Assert.AreEqual(ball.Position.X, ballObject.transform.position.x, Tolerance);
            Assert.AreEqual(ball.Position.Y, ballObject.transform.position.y, Tolerance);
        }

        [UnityTest]
        public IEnumerator BallIsDrawnAtItsRealSize()
        {
            // Se o sprite nao medir o diametro correto, a bola parece quicar antes de
            // encostar: a matematica esta certa e o desenho e que engana.
            MatchRunner runner = FindRunner();
            yield return null;

            var ballObject = GameObject.Find("Ball_0");
            float expectedDiameter = runner.Config.Ball.Radius * 2f;

            Assert.AreEqual(expectedDiameter, ballObject.transform.localScale.x, Tolerance);
        }

        [UnityTest]
        public IEnumerator ExtraBallsExistButStayHidden()
        {
            // Ja existem 8 views para a Multibola da FASE 2 nao precisar de Instantiate no
            // meio da partida (secao 35).
            yield return null;

            var firstBall = GameObject.Find("Ball_0");
            var spareBall = GameObject.Find("Ball_7");

            Assert.IsNotNull(spareBall, "As bolas extras precisam existir desde o inicio.");
            Assert.IsTrue(firstBall.GetComponent<SpriteRenderer>().enabled);
            Assert.IsFalse(spareBall.GetComponent<SpriteRenderer>().enabled, "Bola inativa nao pode aparecer.");
        }

        [UnityTest]
        public IEnumerator PaddleViewFollowsItsOwnSide()
        {
            MatchRunner runner = FindRunner();
            yield return null;

            var bottom = GameObject.Find("Paddle_Bottom");
            var top = GameObject.Find("Paddle_Top");

            Assert.IsNotNull(bottom);
            Assert.IsNotNull(top);
            Assert.Less(bottom.transform.position.y, 0f);
            Assert.Greater(top.transform.position.y, 0f);
            Assert.AreEqual(runner.Config.Paddle.Width, bottom.transform.localScale.x, Tolerance);
        }

        [UnityTest]
        public IEnumerator TowersSitWhereTheSimulationPutsThem()
        {
            MatchRunner runner = FindRunner();
            yield return null;

            var king = GameObject.Find("Tower_Bottom_King");
            Assert.IsNotNull(king);

            var state = runner.Simulation.State.GetTower(PlayerSlot.Bottom, Core.Combat.TowerKind.King);

            Assert.AreEqual(state.Position.X, king.transform.position.x, Tolerance);
            Assert.AreEqual(state.Position.Y, king.transform.position.y, Tolerance);
        }

        [UnityTest]
        public IEnumerator BotChasesTheBallOnItsOwnSide()
        {
            // Prova a corrente inteira do input: cerebro do bot -> PaddleController ->
            // MatchCommand -> fila do runner -> PaddleResolver -> view. Qualquer elo
            // desligado no Inspector deixa a raquete parada em zero.
            MatchRunner runner = FindRunner();
            yield return null;

            runner.Simulation.State.Balls[0] = Core.Ball.BallState.Create(
                new System.Numerics.Vector2(0f, 0f),
                new System.Numerics.Vector2(0.7f, 0.7f),
                speed: 10f,
                damage: 250f);

            yield return new WaitForSeconds(0.6f);

            Assert.Greater(
                runner.Simulation.State.GetPaddle(PlayerSlot.Top).PositionX,
                0.2f,
                "O bot deveria ter perseguido a bola que sobe pela direita.");
        }

        [UnityTest]
        public IEnumerator PlayerPaddleStaysPutWithoutInput()
        {
            // Sem dedo na tela nenhum comando e emitido, entao a raquete do jogador nao pode
            // sair sozinha do lugar.
            MatchRunner runner = FindRunner();

            yield return new WaitForSeconds(0.4f);

            Assert.AreEqual(0f, runner.Simulation.State.GetPaddle(PlayerSlot.Bottom).PositionX, Tolerance);
        }

        [UnityTest]
        public IEnumerator CameraFramesTheWholeArena()
        {
            MatchRunner runner = FindRunner();
            yield return null;

            Camera camera = Camera.main;
            Assert.IsNotNull(camera, "A cena precisa de uma camera marcada como MainCamera.");
            Assert.IsTrue(camera.orthographic);

            float visibleHalfHeight = camera.orthographicSize;
            float visibleHalfWidth = camera.orthographicSize * camera.aspect;

            Assert.GreaterOrEqual(
                visibleHalfHeight + Tolerance,
                runner.Config.Arena.HalfHeight,
                "A arena esta sendo cortada em cima e embaixo.");
            Assert.GreaterOrEqual(
                visibleHalfWidth + Tolerance,
                runner.Config.Arena.HalfWidth,
                "A arena esta sendo cortada nas laterais.");
        }
    }
}
