using NUnit.Framework;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Tests.EditMode
{
    /// <summary>
    /// Valida as propriedades derivadas do MatchConfig. Sao pequenas, mas erram calado:
    /// um DoubleElixirStartTime errado so apareceria como bug de ritmo de partida.
    /// </summary>
    public sealed class MatchConfigTests
    {
        [Test]
        public void DoubleElixirStartsOneMinuteBeforeTheEnd()
        {
            var rules = new MatchRulesConfig(
                matchDurationSeconds: 180f,
                doubleElixirLastSeconds: 60f);

            Assert.AreEqual(120f, rules.DoubleElixirStartTime, 1e-4f);
        }

        [Test]
        public void ArenaExposesHalfExtents()
        {
            var arena = new ArenaConfig(width: 10f, height: 18f, paddleLineOffsetFromEdge: 2.5f);

            Assert.AreEqual(5f, arena.HalfWidth, 1e-4f);
            Assert.AreEqual(9f, arena.HalfHeight, 1e-4f);
        }

        [Test]
        public void PaddleExposesHalfWidth()
        {
            var paddle = new PaddleConfig(
                width: 2.4f,
                thickness: 0.4f,
                maxSpeed: 18f,
                smoothingTime: 0.05f,
                dragSensitivity: 1f,
                sweepCarry: 0.35f);

            Assert.AreEqual(1.2f, paddle.HalfWidth, 1e-4f);
        }
    }
}
