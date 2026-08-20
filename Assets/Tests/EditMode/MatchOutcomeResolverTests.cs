using NUnit.Framework;
using PongRoyale.Core.Combat;
using PongRoyale.Core.Events;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Tests.EditMode
{
    public sealed class MatchOutcomeResolverTests
    {
        private MatchState state;
        private MatchEventQueue events;

        [SetUp]
        public void SetUp()
        {
            state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);
            state.Phase = MatchPhase.Playing;
            events = new MatchEventQueue();
        }

        private void DestroyTower(PlayerSlot slot, TowerKind kind)
        {
            state.GetTower(slot, kind).Health = 0f;
        }

        private void RunOutTheClock()
        {
            state.ElapsedSeconds = state.Config.Rules.MatchDurationSeconds;
        }

        [Test]
        public void MatchStaysUndecidedWhileBothKingsStand()
        {
            MatchOutcomeResolver.Evaluate(state, events);

            Assert.IsFalse(state.Result.IsDecided);
            Assert.AreEqual(MatchPhase.Playing, state.Phase);
            Assert.AreEqual(0, events.Count);
        }

        [Test]
        public void DestroyingTheKingEndsTheMatchImmediately()
        {
            // Sem esperar o relogio: derrubar o Rei e vitoria na hora.
            DestroyTower(PlayerSlot.Top, TowerKind.King);

            MatchOutcomeResolver.Evaluate(state, events);

            Assert.AreEqual(MatchOutcome.Victory, state.Result.Outcome);
            Assert.AreEqual((sbyte)PlayerSlot.Bottom, state.Result.WinnerSlot);
            Assert.AreEqual(MatchEndReason.KingTowerDestroyed, state.Result.Reason);
            Assert.AreEqual(MatchPhase.Finished, state.Phase);
        }

        [Test]
        public void LosingYourOwnKingHandsTheWinToTheOpponent()
        {
            DestroyTower(PlayerSlot.Bottom, TowerKind.King);

            MatchOutcomeResolver.Evaluate(state, events);

            Assert.AreEqual((sbyte)PlayerSlot.Top, state.Result.WinnerSlot);
        }

        [Test]
        public void BothKingsFallingInTheSameTickIsADraw()
        {
            // Possivel com Multibola: duas bolas resolvem no mesmo tick.
            DestroyTower(PlayerSlot.Bottom, TowerKind.King);
            DestroyTower(PlayerSlot.Top, TowerKind.King);

            MatchOutcomeResolver.Evaluate(state, events);

            Assert.AreEqual(MatchOutcome.Draw, state.Result.Outcome);
            Assert.AreEqual(MatchResult.NoWinner, state.Result.WinnerSlot);
        }

        [Test]
        public void EndingTheMatchEmitsPhaseAndResultEvents()
        {
            DestroyTower(PlayerSlot.Top, TowerKind.King);

            MatchOutcomeResolver.Evaluate(state, events);

            Assert.AreEqual(2, events.Count);
            Assert.AreEqual(MatchEventType.PhaseChanged, events[0].Type);
            Assert.AreEqual(MatchEventType.MatchEnded, events[1].Type);
        }

        [Test]
        public void ResultIsFinalAndLaterEvaluationsChangeNothing()
        {
            // Na FASE 3 o servidor pode avaliar o mesmo estado mais de uma vez.
            DestroyTower(PlayerSlot.Top, TowerKind.King);
            MatchOutcomeResolver.Evaluate(state, events);
            events.Clear();

            DestroyTower(PlayerSlot.Bottom, TowerKind.King);
            MatchOutcomeResolver.Evaluate(state, events);

            Assert.AreEqual((sbyte)PlayerSlot.Bottom, state.Result.WinnerSlot, "O resultado ja estava decidido.");
            Assert.AreEqual(0, events.Count, "Partida encerrada nao pode emitir evento de novo.");
        }

        [Test]
        public void TimeRunningOutWithMoreTowersStandingWins()
        {
            DestroyTower(PlayerSlot.Top, TowerKind.LeftGuard);
            RunOutTheClock();

            MatchOutcomeResolver.Evaluate(state, events);

            Assert.AreEqual((sbyte)PlayerSlot.Bottom, state.Result.WinnerSlot);
            Assert.AreEqual(MatchEndReason.TiebreakResolved, state.Result.Reason);
        }

        [Test]
        public void TowerCountBeatsHealthEvenWhenTheOtherSideIsBattered()
        {
            // Um lado perdeu uma torre; o outro esta com todas de pe porem quase zeradas.
            // Contagem de torres vem primeiro na cascata, entao ele vence assim mesmo.
            DestroyTower(PlayerSlot.Top, TowerKind.LeftGuard);
            state.GetTower(PlayerSlot.Bottom, TowerKind.King).Health = 1f;
            state.GetTower(PlayerSlot.Bottom, TowerKind.LeftGuard).Health = 1f;
            state.GetTower(PlayerSlot.Bottom, TowerKind.RightGuard).Health = 1f;
            RunOutTheClock();

            MatchOutcomeResolver.Evaluate(state, events);

            Assert.AreEqual((sbyte)PlayerSlot.Bottom, state.Result.WinnerSlot);
        }

        [Test]
        public void EqualTowersFallBackToTotalHealth()
        {
            state.GetTower(PlayerSlot.Top, TowerKind.King).Health = 1000f;
            RunOutTheClock();

            MatchOutcomeResolver.Evaluate(state, events);

            Assert.AreEqual((sbyte)PlayerSlot.Bottom, state.Result.WinnerSlot);
            Assert.AreEqual(MatchEndReason.TiebreakResolved, state.Result.Reason);
        }

        [Test]
        public void HealthTiebreakAlsoAppliesWhenBothLostTheSameNumberOfTowers()
        {
            // A generalizacao: nao e so "todas de pe". Dois a dois tambem cai na vida.
            DestroyTower(PlayerSlot.Bottom, TowerKind.LeftGuard);
            DestroyTower(PlayerSlot.Top, TowerKind.RightGuard);
            state.GetTower(PlayerSlot.Bottom, TowerKind.King).Health = 4000f;
            RunOutTheClock();

            MatchOutcomeResolver.Evaluate(state, events);

            Assert.AreEqual((sbyte)PlayerSlot.Top, state.Result.WinnerSlot);
        }

        [Test]
        public void IdenticalSidesEndInADraw()
        {
            RunOutTheClock();

            MatchOutcomeResolver.Evaluate(state, events);

            Assert.AreEqual(MatchOutcome.Draw, state.Result.Outcome);
            Assert.AreEqual(MatchEndReason.TimeExpired, state.Result.Reason);
            Assert.AreEqual(MatchResult.NoWinner, state.Result.WinnerSlot);
        }

        [Test]
        public void NegligibleHealthDifferenceIsStillADraw()
        {
            // Vida sai de subtracoes sucessivas em float. Uma diferenca na ultima casa
            // decimal nao pode decidir uma partida.
            state.GetTower(PlayerSlot.Top, TowerKind.King).Health -= 1e-5f;
            RunOutTheClock();

            MatchOutcomeResolver.Evaluate(state, events);

            Assert.AreEqual(MatchOutcome.Draw, state.Result.Outcome);
        }

        [Test]
        public void ClockStillTickingLeavesTheMatchOpen()
        {
            state.ElapsedSeconds = state.Config.Rules.MatchDurationSeconds - 0.1f;
            DestroyTower(PlayerSlot.Top, TowerKind.LeftGuard);

            MatchOutcomeResolver.Evaluate(state, events);

            Assert.IsFalse(state.Result.IsDecided, "Vantagem em torres so decide quando o tempo acaba.");
        }

        [Test]
        public void OwnGoalDoesNotWinTheTiebreak()
        {
            // A bola do jogador de baixo derruba a PROPRIA torre. O contador de destruicoes
            // credita o adversario, mas o desempate olha torres de pe — e quem perdeu a
            // torre foi ele mesmo.
            DestroyTower(PlayerSlot.Bottom, TowerKind.LeftGuard);
            state.GetPlayer(PlayerSlot.Top).TowersDestroyed = 1;
            RunOutTheClock();

            MatchOutcomeResolver.Evaluate(state, events);

            Assert.AreEqual((sbyte)PlayerSlot.Top, state.Result.WinnerSlot);
        }
    }
}
