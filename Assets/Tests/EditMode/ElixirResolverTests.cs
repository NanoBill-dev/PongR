using NUnit.Framework;
using PongRoyale.Core.Economy;
using PongRoyale.Core.Events;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Tests.EditMode
{
    /// <summary>
    /// O metronomo do elixir e as cargas de defesa.
    ///
    /// A barra e UMA SO, compartilhada, e nunca para. Isso elimina por construcao qualquer
    /// divergencia entre os dois clientes: nao existe cronometro individual que possa
    /// dessincronizar. O que varia por jogador e apenas se ele RECEBE a carga na batida.
    /// </summary>
    public sealed class ElixirResolverTests
    {
        private MatchState state;
        private MatchEventQueue events;

        [SetUp]
        public void SetUp()
        {
            state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);
            events = new MatchEventQueue();
        }

        private void Advance(float seconds)
        {
            // Conta em TICKS, como a simulacao: pedir "20 segundos" vira exatamente 1200
            // passos, e a batida cai sempre no mesmo lugar.
            int ticks = (int)System.Math.Round(seconds * MatchConstants.SimulationTicksPerSecond);
            for (int i = 0; i < ticks; i++)
            {
                ElixirResolver.Advance(state, events);
            }
        }

        private float CycleSeconds => state.Config.Elixir.CycleSeconds;

        private byte Charges(PlayerSlot slot) => state.GetPlayer(slot).DefenseCharges;

        [Test]
        public void MatchStartsWithNoCharges()
        {
            Assert.AreEqual(0, Charges(PlayerSlot.Bottom));
            Assert.AreEqual(0, Charges(PlayerSlot.Top));
            Assert.AreEqual(0, state.CompletedElixirCycles);
        }

        [Test]
        public void OneCycleGrantsOneChargeToBothPlayers()
        {
            // A barra e global: a batida vale para os dois ao mesmo tempo.
            Advance(CycleSeconds);

            Assert.AreEqual(1, Charges(PlayerSlot.Bottom));
            Assert.AreEqual(1, Charges(PlayerSlot.Top));
            Assert.AreEqual(1, state.CompletedElixirCycles);
        }

        [Test]
        public void ChargesStopAtTheConfiguredCeiling()
        {
            Advance(CycleSeconds * 6f);

            Assert.AreEqual(state.Config.Elixir.MaxDefenseCharges, Charges(PlayerSlot.Bottom));
        }

        [Test]
        public void CycleRemainderIsCarriedInsteadOfDiscarded()
        {
            // Zerar a barra na virada acumularia atraso ao longo da partida: seis ciclos
            // custariam mais que seis vezes a duracao de um.
            Advance(CycleSeconds * 3f);

            Assert.AreEqual(3, state.CompletedElixirCycles);
        }

        [Test]
        public void AbsorbingAHitSpendsACharge()
        {
            Advance(CycleSeconds * 2f);

            bool absorbed = ElixirResolver.TryAbsorbHit(state, PlayerSlot.Bottom, events);

            Assert.IsTrue(absorbed);
            Assert.AreEqual(1, Charges(PlayerSlot.Bottom));
        }

        [Test]
        public void WithoutChargesTheHitGoesThrough()
        {
            Assert.IsFalse(ElixirResolver.TryAbsorbHit(state, PlayerSlot.Bottom, events));
        }

        [Test]
        public void AbsorbingOnOneSideDoesNotSpendTheOthersCharge()
        {
            Advance(CycleSeconds);

            ElixirResolver.TryAbsorbHit(state, PlayerSlot.Bottom, events);

            Assert.AreEqual(0, Charges(PlayerSlot.Bottom));
            Assert.AreEqual(1, Charges(PlayerSlot.Top));
        }

        [Test]
        public void CleanStreakReachesTheRedemptionThreshold()
        {
            Advance(CycleSeconds * 3f);

            Assert.IsTrue(ElixirResolver.HasCleanStreakForRedemption(state, PlayerSlot.Bottom));
        }

        [Test]
        public void SpendingAChargeBreaksTheCleanStreak()
        {
            // A parte dura da redencao: qualquer acerto absorvido recomeca a contagem.
            Advance(CycleSeconds * 3f);
            ElixirResolver.TryAbsorbHit(state, PlayerSlot.Bottom, events);

            Assert.IsFalse(ElixirResolver.HasCleanStreakForRedemption(state, PlayerSlot.Bottom));
        }

        [Test]
        public void RecoveringTheChargeQuicklyDoesNotRecoverTheStreak()
        {
            // A sorte do metronomo ajuda a DEFESA, nunca a redencao. Perder uma carga
            // faltando pouco para a batida devolve o escudo, mas nao o progresso — senao
            // tomar acerto na hora certa sairia quase de graca.
            Advance(CycleSeconds * 3f);
            ElixirResolver.TryAbsorbHit(state, PlayerSlot.Bottom, events);
            Advance(CycleSeconds);

            Assert.AreEqual(3, Charges(PlayerSlot.Bottom), "A carga deveria ter voltado.");
            Assert.IsFalse(
                ElixirResolver.HasCleanStreakForRedemption(state, PlayerSlot.Bottom),
                "O progresso rumo a redencao nao pode voltar junto com a carga.");
        }

        [Test]
        public void TheStreakCanBeRebuiltFromScratch()
        {
            Advance(CycleSeconds * 3f);
            ElixirResolver.TryAbsorbHit(state, PlayerSlot.Bottom, events);
            Advance(CycleSeconds * 3f);

            Assert.IsTrue(ElixirResolver.HasCleanStreakForRedemption(state, PlayerSlot.Bottom));
        }

        [Test]
        public void BerserkPlayerLosesEverythingAndStopsReceiving()
        {
            Advance(CycleSeconds * 3f);

            ElixirResolver.EnterBerserk(state, PlayerSlot.Bottom, events);
            Advance(CycleSeconds * 2f);

            Assert.AreEqual(0, Charges(PlayerSlot.Bottom), "Berserk e ficar sem defesa nenhuma.");
            Assert.IsFalse(ElixirResolver.HasCleanStreakForRedemption(state, PlayerSlot.Bottom));
        }

        [Test]
        public void TheBarKeepsRunningForTheOtherPlayerDuringBerserk()
        {
            // A barra e global: o berserk de um nao congela o abastecimento do outro.
            ElixirResolver.EnterBerserk(state, PlayerSlot.Bottom, events);
            Advance(CycleSeconds * 2f);

            Assert.AreEqual(0, Charges(PlayerSlot.Bottom));
            Assert.AreEqual(2, Charges(PlayerSlot.Top));
            Assert.AreEqual(2, state.CompletedElixirCycles, "O metronomo global nao pode parar.");
        }

        [Test]
        public void ResumingChargingBringsThePlayerBack()
        {
            // Acontece quando o drop de redencao tambem e perdido: o jogador nao ficou com o
            // premio, entao recupera a geracao de defesa.
            ElixirResolver.EnterBerserk(state, PlayerSlot.Bottom, events);
            ElixirResolver.ResumeCharging(state, PlayerSlot.Bottom);

            Advance(CycleSeconds);

            Assert.AreEqual(1, Charges(PlayerSlot.Bottom));
        }

        [Test]
        public void CycleAndChargeChangesAreAnnounced()
        {
            Advance(CycleSeconds);

            bool cycleCompleted = false;
            bool chargeGained = false;

            for (int i = 0; i < events.Count; i++)
            {
                cycleCompleted |= events[i].Type == MatchEventType.ElixirCycleCompleted;
                chargeGained |= events[i].Type == MatchEventType.DefenseChargeGained;
            }

            Assert.IsTrue(cycleCompleted);
            Assert.IsTrue(chargeGained);
        }

        [Test]
        public void ElixirStateChangesTheHash()
        {
            ulong before = MatchStateHash.Compute(state);
            Advance(CycleSeconds);

            Assert.AreNotEqual(before, MatchStateHash.Compute(state));
        }
    }
}
