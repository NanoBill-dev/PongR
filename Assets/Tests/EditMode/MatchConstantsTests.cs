using NUnit.Framework;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Tests.EditMode
{
    /// <summary>
    /// Primeiro teste do projeto. Alem de validar as constantes, prova que o
    /// pipeline de testes enxerga PongRoyale.Core sem entrar em Play Mode.
    /// </summary>
    public sealed class MatchConstantsTests
    {
        [Test]
        public void FixedDeltaTime_MatchesTickRate()
        {
            Assert.AreEqual(1f / 60f, MatchConstants.FixedDeltaTime, 1e-6f);
        }

        [Test]
        public void HandIsSmallerThanDeck()
        {
            Assert.Less(MatchConstants.HandSize, MatchConstants.DeckSize);
        }

        [Test]
        public void SnapshotRateDividesSimulationRate()
        {
            Assert.AreEqual(0, MatchConstants.SimulationTicksPerSecond % MatchConstants.SnapshotsPerSecond);
        }
    }
}
