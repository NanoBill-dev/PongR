using System;
using PongRoyale.Core.Events;

namespace PongRoyale.Core.Simulation
{
    /// <summary>
    /// Decide quando a partida acaba e quem venceu.
    ///
    /// Regra completa:
    ///
    ///   1. Torre Rei destruida encerra na hora, sem esperar o relogio. Se as duas caem no
    ///      mesmo tick (possivel com Multibola), e empate.
    ///   2. Tempo esgotado, em cascata:
    ///      a. vence quem tem MAIS torres de pe;
    ///      b. empatado em torres, vence quem tem MAIS vida somada nas torres;
    ///      c. identico nos dois criterios, empate — a tela de fim oferece revanche.
    ///
    /// O resultado e final: uma vez decidido, chamadas seguintes nao mudam nada. Isso
    /// importa porque na FASE 3 o servidor pode avaliar o mesmo estado mais de uma vez.
    /// </summary>
    public static class MatchOutcomeResolver
    {
        /// <summary>
        /// Folga na comparacao de vida. Vida vem de subtracoes sucessivas de dano em float:
        /// dois jogadores que tomaram exatamente o mesmo castigo podem diferir na ultima
        /// casa decimal, e isso nao pode virar vitoria.
        /// </summary>
        private const float HealthComparisonTolerance = 1e-3f;

        public static void Evaluate(MatchState state, MatchEventQueue events)
        {
            if (state.Result.IsDecided)
            {
                return;
            }

            if (TryResolveByKingTower(state, out MatchResult kingResult))
            {
                Finish(state, kingResult, events);
                return;
            }

            if (state.ElapsedSeconds >= state.Config.Rules.MatchDurationSeconds)
            {
                Finish(state, ResolveByTiebreak(state), events);
            }
        }

        private static bool TryResolveByKingTower(MatchState state, out MatchResult result)
        {
            bool bottomAlive = state.IsKingAlive(PlayerSlot.Bottom);
            bool topAlive = state.IsKingAlive(PlayerSlot.Top);

            if (bottomAlive && topAlive)
            {
                result = MatchResult.Undecided;
                return false;
            }

            if (bottomAlive)
            {
                result = MatchResult.Victory(PlayerSlot.Bottom, MatchEndReason.KingTowerDestroyed);
            }
            else if (topAlive)
            {
                result = MatchResult.Victory(PlayerSlot.Top, MatchEndReason.KingTowerDestroyed);
            }
            else
            {
                // Os dois Reis no mesmo tick. Raro, mas possivel com varias bolas em jogo.
                result = MatchResult.Draw(MatchEndReason.KingTowerDestroyed);
            }

            return true;
        }

        /// <summary>Cascata de desempate aplicada quando o tempo regulamentar acaba.</summary>
        public static MatchResult ResolveByTiebreak(MatchState state)
        {
            int bottomTowers = state.CountAliveTowers(PlayerSlot.Bottom);
            int topTowers = state.CountAliveTowers(PlayerSlot.Top);

            if (bottomTowers != topTowers)
            {
                PlayerSlot winner = bottomTowers > topTowers ? PlayerSlot.Bottom : PlayerSlot.Top;
                return MatchResult.Victory(winner, MatchEndReason.TiebreakResolved);
            }

            float bottomHealth = state.TotalTowerHealth(PlayerSlot.Bottom);
            float topHealth = state.TotalTowerHealth(PlayerSlot.Top);

            if (Math.Abs(bottomHealth - topHealth) > HealthComparisonTolerance)
            {
                PlayerSlot winner = bottomHealth > topHealth ? PlayerSlot.Bottom : PlayerSlot.Top;
                return MatchResult.Victory(winner, MatchEndReason.TiebreakResolved);
            }

            return MatchResult.Draw(MatchEndReason.TimeExpired);
        }

        private static void Finish(MatchState state, MatchResult result, MatchEventQueue events)
        {
            state.Result = result;
            state.Phase = MatchPhase.Finished;

            events.Enqueue(MatchEvent.PhaseChanged(state.Tick, MatchPhase.Finished));
            events.Enqueue(MatchEvent.MatchEnded(state.Tick, result.Outcome));
        }
    }
}
