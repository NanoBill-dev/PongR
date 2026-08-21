using System.Numerics;
using NUnit.Framework;
using PongRoyale.Core.Ball;
using PongRoyale.Core.Simulation;
using PongRoyale.Gameplay.Input;

namespace PongRoyale.Tests.EditMode
{
    public sealed class AiPaddleBrainTests
    {
        private const float Tolerance = 1e-2f;
        private const float Step = MatchConstants.FixedDeltaTime;

        private MatchState state;

        /// <summary>Bot sem erro de mira, para os testes de geometria serem exatos.</summary>
        private static AiSettings PerfectAim => new AiSettings
        {
            ReactionSeconds = 0f,
            ErrorPerSpeedUnit = 0f,
            MaxError = 0f
        };

        [SetUp]
        public void SetUp()
        {
            state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);
            state.Balls[0].IsActive = false;
        }

        private void PlaceBall(Vector2 position, Vector2 direction, float speed)
        {
            state.Balls[0] = BallState.Create(position, direction, speed, 250f);
        }

        [Test]
        public void WithoutABallTheBotHoldsTheCenter()
        {
            // O centro e a posicao que cobre mais area, entao e o repouso correto.
            var brain = new AiPaddleBrain(seed: 1);

            float target = brain.Decide(state, PlayerSlot.Bottom, Step, PerfectAim);

            Assert.AreEqual(0f, target, Tolerance);
        }

        [Test]
        public void BotAimsAtWhereTheBallWillCrossItsLine()
        {
            // Bola descendo na diagonal a partir do centro. O bot precisa projetar o ponto
            // de cruzamento, nao seguir a posicao atual da bola.
            float lineY = state.GetPaddle(PlayerSlot.Bottom).LineY;
            PlaceBall(new Vector2(0f, 0f), new Vector2(1f, -1f), 10f);

            var brain = new AiPaddleBrain(seed: 1);
            float target = brain.Decide(state, PlayerSlot.Bottom, Step, PerfectAim);

            // Direcao 45 graus: o deslocamento em X iguala o deslocamento em Y.
            float expected = System.Math.Abs(lineY);

            Assert.AreEqual(expected, target, Tolerance);
        }

        [Test]
        public void BotIgnoresBallsThatAreMovingAway()
        {
            PlaceBall(new Vector2(3f, 0f), new Vector2(0f, 1f), 10f);

            var brain = new AiPaddleBrain(seed: 1);
            float target = brain.Decide(state, PlayerSlot.Bottom, Step, PerfectAim);

            Assert.AreEqual(0f, target, Tolerance, "Bola subindo nao ameaca a raquete de baixo.");
        }

        [Test]
        public void BotChasesTheBallThatArrivesFirst()
        {
            // Duas bolas descendo: uma perto e lenta, outra longe e rapida. A ameaca e a que
            // chega antes, nao a que esta mais perto.
            PlaceBall(new Vector2(-4f, -5f), new Vector2(0f, -1f), 1f);
            state.Balls[1] = BallState.Create(new Vector2(4f, 4f), new Vector2(0f, -1f), 40f, 250f);

            var brain = new AiPaddleBrain(seed: 1);
            float target = brain.Decide(state, PlayerSlot.Bottom, Step, PerfectAim);

            Assert.AreEqual(4f, target, Tolerance);
        }

        [Test]
        public void DecisionIsHeldUntilTheReactionTimeElapses()
        {
            // E isto que da ao bot um tempo de reacao: entre decisoes ele persegue um alvo
            // desatualizado, como um humano que leu a bola um instante atras.
            var settings = new AiSettings { ReactionSeconds = 0.2f, ErrorPerSpeedUnit = 0f, MaxError = 0f };
            PlaceBall(new Vector2(0f, 0f), new Vector2(1f, -1f), 10f);

            var brain = new AiPaddleBrain(seed: 1);
            float firstDecision = brain.Decide(state, PlayerSlot.Bottom, Step, settings);

            // A bola muda de rumo, mas o bot ainda nao "percebeu".
            state.Balls[0].Direction = new Vector2(-1f, -1f) / (float)System.Math.Sqrt(2.0);
            float heldDecision = brain.Decide(state, PlayerSlot.Bottom, Step, settings);

            Assert.AreEqual(firstDecision, heldDecision, 1e-5f);
        }

        [Test]
        public void BotUpdatesOnceTheReactionTimeHasPassed()
        {
            var settings = new AiSettings { ReactionSeconds = 0.1f, ErrorPerSpeedUnit = 0f, MaxError = 0f };
            PlaceBall(new Vector2(0f, 0f), new Vector2(1f, -1f), 10f);

            var brain = new AiPaddleBrain(seed: 1);
            float firstDecision = brain.Decide(state, PlayerSlot.Bottom, Step, settings);

            state.Balls[0].Direction = new Vector2(-1f, -1f) / (float)System.Math.Sqrt(2.0);
            float updatedDecision = brain.Decide(state, PlayerSlot.Bottom, 0.2f, settings);

            Assert.AreNotEqual(firstDecision, updatedDecision);
            Assert.Less(updatedDecision, 0f, "Depois de reagir, o bot persegue o novo rumo.");
        }

        [Test]
        public void AimingErrorStaysWithinTheConfiguredCeiling()
        {
            var settings = new AiSettings { ReactionSeconds = 0f, ErrorPerSpeedUnit = 10f, MaxError = 1.5f };
            float lineY = state.GetPaddle(PlayerSlot.Bottom).LineY;

            var brain = new AiPaddleBrain(seed: 7);

            for (int i = 0; i < 200; i++)
            {
                PlaceBall(new Vector2(0f, 0f), new Vector2(1f, -1f), 25f);
                float target = brain.Decide(state, PlayerSlot.Bottom, Step, settings);
                float perfect = System.Math.Abs(lineY);

                Assert.LessOrEqual(
                    System.Math.Abs(target - perfect),
                    settings.MaxError + Tolerance,
                    "Um erro alem do teto tornaria o bot inutil numa bola rapida.");
            }
        }

        [Test]
        public void FasterBallsProduceLooserAim()
        {
            // O bot falha mais conforme o rali esquenta, que e quando o humano tambem falha.
            var settings = new AiSettings { ReactionSeconds = 0f, ErrorPerSpeedUnit = 0.05f, MaxError = 10f };
            float lineY = System.Math.Abs(state.GetPaddle(PlayerSlot.Bottom).LineY);

            float slowSpread = MeasureSpread(speed: 8f, settings, lineY);
            float fastSpread = MeasureSpread(speed: 25f, settings, lineY);

            Assert.Greater(fastSpread, slowSpread);
        }

        [Test]
        public void SameSeedProducesTheSameBehaviour()
        {
            var settings = new AiSettings { ReactionSeconds = 0f, ErrorPerSpeedUnit = 0.05f, MaxError = 2f };

            float first = RunSequence(seed: 42, settings);
            float second = RunSequence(seed: 42, settings);

            Assert.AreEqual(first, second, 1e-6f, "Semente fixa precisa dar comportamento reproduzivel.");
        }

        private float MeasureSpread(float speed, AiSettings settings, float perfectTarget)
        {
            var brain = new AiPaddleBrain(seed: 99);
            float worst = 0f;

            for (int i = 0; i < 200; i++)
            {
                PlaceBall(new Vector2(0f, 0f), new Vector2(1f, -1f), speed);
                float target = brain.Decide(state, PlayerSlot.Bottom, Step, settings);
                worst = System.Math.Max(worst, System.Math.Abs(target - perfectTarget));
            }

            return worst;
        }

        private float RunSequence(int seed, AiSettings settings)
        {
            var brain = new AiPaddleBrain(seed);
            float last = 0f;

            for (int i = 0; i < 50; i++)
            {
                PlaceBall(new Vector2(0f, 0f), new Vector2(1f, -1f), 12f);
                last = brain.Decide(state, PlayerSlot.Bottom, Step, settings);
            }

            return last;
        }
    }
}
