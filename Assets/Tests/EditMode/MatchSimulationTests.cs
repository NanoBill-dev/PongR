using NUnit.Framework;
using PongRoyale.Core.Combat;
using PongRoyale.Core.Events;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Tests.EditMode
{
    public sealed class MatchSimulationTests
    {
        private MatchSimulation simulation;
        private MatchCommandBuffer commands;

        [SetUp]
        public void SetUp()
        {
            simulation = new MatchSimulation(TestConfigs.Default(), PlayerSlot.Bottom);
            commands = new MatchCommandBuffer();
        }

        private void Tick(int ticks = 1)
        {
            for (int i = 0; i < ticks; i++)
            {
                simulation.Tick(commands);
                commands.Clear();
            }
        }

        [Test]
        public void BallDoesNotMoveDuringWarmUp()
        {
            var before = simulation.State.Balls[0].Position;

            Tick(60);

            Assert.AreEqual(before, simulation.State.Balls[0].Position);
            Assert.AreEqual(0f, simulation.State.ElapsedSeconds, 1e-4f, "O relogio nao anda no aquecimento.");
        }

        [Test]
        public void PaddlesAlreadyRespondDuringWarmUp()
        {
            // O jogador precisa poder se posicionar durante a contagem regressiva.
            commands.Add(MatchCommand.PaddleMove(0, PlayerSlot.Bottom, 2f));

            Tick(60);

            Assert.Greater(simulation.State.GetPaddle(PlayerSlot.Bottom).PositionX, 0.5f);
        }

        [Test]
        public void BeginStartsThePlayAndAnnouncesIt()
        {
            simulation.Begin();

            Assert.AreEqual(MatchPhase.Playing, simulation.State.Phase);
            Assert.AreEqual(1, simulation.Events.Count);
            Assert.AreEqual(MatchEventType.PhaseChanged, simulation.Events[0].Type);
        }

        [Test]
        public void BeginIsIgnoredAfterTheMatchHasStarted()
        {
            simulation.Begin();
            simulation.Events.Clear();

            simulation.Begin();

            Assert.AreEqual(0, simulation.Events.Count);
        }

        [Test]
        public void BallMovesOnceThePlayHasStarted()
        {
            simulation.Begin();

            Tick();

            Assert.AreNotEqual(0f, simulation.State.Balls[0].Position.Y);
            Assert.AreEqual(MatchConstants.FixedDeltaTime, simulation.State.ElapsedSeconds, 1e-6f);
        }

        [Test]
        public void CommandTakesEffectInTheSameTickItArrives()
        {
            // Se o comando so valesse no tick seguinte, o jogador sentiria um frame de
            // atraso na propria raquete — a pior latencia possivel num jogo de reflexo.
            simulation.Begin();
            commands.Add(MatchCommand.PaddleMove(0, PlayerSlot.Bottom, 3f));

            Tick();

            Assert.Greater(simulation.State.GetPaddle(PlayerSlot.Bottom).PositionX, 0f);
        }

        [Test]
        public void CommandFromAnImpossibleSlotIsIgnored()
        {
            // Um cliente adulterado pode mandar qualquer byte. Indexar o array com ele
            // derrubaria a simulacao, inclusive a do servidor.
            simulation.Begin();
            commands.Add(MatchCommand.PaddleMove(0, (PlayerSlot)7, 3f));

            Assert.DoesNotThrow(() => Tick());
            Assert.AreEqual(0f, simulation.State.GetPaddle(PlayerSlot.Bottom).PositionX, 1e-4f);
            Assert.AreEqual(0f, simulation.State.GetPaddle(PlayerSlot.Top).PositionX, 1e-4f);
        }

        [Test]
        public void PlayCardIsAcceptedButDoesNothingYet()
        {
            // FASE 2. Um comando que a simulacao ainda nao entende nao pode derrubar nada.
            simulation.Begin();
            commands.Add(MatchCommand.PlayCard(0, PlayerSlot.Bottom, 0, System.Numerics.Vector2.Zero));

            Assert.DoesNotThrow(() => Tick());
        }

        [Test]
        public void EventsSurviveAcrossTicksUntilTheConsumerClearsThem()
        {
            // Um frame pode rodar varios ticks. Se o Tick limpasse a fila, o jogador veria
            // uma torre cair sem som nem efeito.
            simulation.Begin();
            simulation.State.GetTower(PlayerSlot.Bottom, TowerKind.King).Health = 100f;
            simulation.State.GetPaddle(PlayerSlot.Bottom).PositionX = 4f;
            simulation.State.GetPaddle(PlayerSlot.Bottom).PreviousPositionX = 4f;

            Tick(200);

            Assert.Greater(simulation.Events.Count, 1, "Os eventos de varios ticks precisam coexistir.");
        }

        [Test]
        public void FinishedMatchStopsSimulating()
        {
            simulation.Begin();
            simulation.State.GetTower(PlayerSlot.Top, TowerKind.King).Health = 0f;
            Tick();

            Assert.IsTrue(simulation.IsFinished);

            var frozenPosition = simulation.State.Balls[0].Position;
            int frozenTick = simulation.State.Tick;

            Tick(60);

            Assert.AreEqual(frozenPosition, simulation.State.Balls[0].Position);
            Assert.AreEqual(frozenTick, simulation.State.Tick);
        }

        [Test]
        public void MatchEndsWhenTheClockRunsOut()
        {
            simulation.Begin();

            // Um tick a mais que a duracao regulamentar inteira.
            int totalTicks = (int)(TestConfigs.Default().Rules.MatchDurationSeconds
                                   * MatchConstants.SimulationTicksPerSecond) + 1;
            Tick(totalTicks);

            Assert.IsTrue(simulation.IsFinished);
            Assert.AreEqual(MatchPhase.Finished, simulation.State.Phase);
        }

        [Test]
        public void SameInputsProduceTheSameStateTwice()
        {
            // Determinismo dentro da plataforma. Sem isso nao existe replay, nem teste
            // reproduzivel, nem deteccao de divergencia com o servidor.
            ulong first = RunScenario(600);
            ulong second = RunScenario(600);

            Assert.AreEqual(first, second);
        }

        [Test]
        public void DifferentInputsProduceDifferentStates()
        {
            // Guarda contra um hash que ignora campos demais e passaria a validar nada.
            ulong straight = RunScenario(600);
            ulong wiggled = RunScenario(600, paddleAmplitude: 0f);

            Assert.AreNotEqual(straight, wiggled);
        }

        [Test]
        public void InactiveBallSlotsDoNotChangeTheHash()
        {
            var a = new MatchSimulation(TestConfigs.Default(), PlayerSlot.Bottom);
            var b = new MatchSimulation(TestConfigs.Default(), PlayerSlot.Bottom);

            // Lixo num slot reciclado nao pode influenciar o hash de um estado que e
            // logicamente identico — se influenciasse, geraria falso positivo de divergencia.
            b.State.Balls[3].Position = new System.Numerics.Vector2(123f, -456f);
            b.State.Balls[3].BaseSpeed = 999f;

            Assert.AreEqual(MatchStateHash.Compute(a.State), MatchStateHash.Compute(b.State));
        }

        [Test]
        public void GoldenScenarioStillProducesTheKnownState()
        {
            // TESTE DOURADO. Uma sequencia fixa de comandos precisa produzir sempre o mesmo
            // estado final. Se este teste falhar depois de uma refatoracao, o comportamento
            // do jogo MUDOU — verifique se a mudanca era intencional antes de atualizar o
            // numero abaixo. Nao atualize por reflexo: e justamente esse alarme que protege
            // a fisica de regressao silenciosa.
            // Atualizado em 2026-08-20 apos a correcao dos travamentos da bola: rebatida so
            // pela face da frente da raquete, separacao real no desencaixe e reflexao de
            // bola alem do limite da parede. A fisica mudou de PROPOSITO, entao o valor
            // dourado foi recalculado. O anterior era 12391916413434340699.
            const ulong ExpectedHash = 16652058934244109118UL;

            ulong actual = RunScenario(1800);

            Assert.AreEqual(ExpectedHash, actual);
        }

        /// <summary>
        /// Cenario reproduzivel: as duas raquetes varrem a arena num padrao triangular
        /// derivado do numero do tick. Sem aleatoriedade e sem trigonometria, para que o
        /// resultado dependa apenas da simulacao.
        /// </summary>
        private static ulong RunScenario(int ticks, float paddleAmplitude = 3f)
        {
            var run = new MatchSimulation(TestConfigs.Default(), PlayerSlot.Bottom);
            var buffer = new MatchCommandBuffer();
            run.Begin();

            for (int tick = 0; tick < ticks; tick++)
            {
                float wave = (tick % 120) / 60f - 1f;
                buffer.Add(MatchCommand.PaddleMove(tick, PlayerSlot.Bottom, wave * paddleAmplitude));
                buffer.Add(MatchCommand.PaddleMove(tick, PlayerSlot.Top, -wave * paddleAmplitude));

                run.Tick(buffer);
                buffer.Clear();
                run.Events.Clear();
            }

            return MatchStateHash.Compute(run.State);
        }
    }
}
