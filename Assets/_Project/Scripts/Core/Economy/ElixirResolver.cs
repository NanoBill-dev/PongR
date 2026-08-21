using PongRoyale.Core.Events;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Core.Economy
{
    /// <summary>
    /// O metronomo do elixir e as cargas de defesa.
    ///
    /// A barra e UMA SO, compartilhada pelos dois jogadores, e nunca para. Isso elimina por
    /// construcao qualquer divergencia entre os dois clientes: nao existe cronometro
    /// individual que possa dessincronizar. O que varia por jogador e apenas se ele RECEBE
    /// a carga na batida.
    /// </summary>
    public static class ElixirResolver
    {
        /// <summary>
        /// Ticks de um ciclo completo. Arredondado uma vez, a partir da configuracao.
        /// </summary>
        public static int CycleTicks(MatchConfig config) =>
            (int)System.Math.Round(config.Elixir.CycleSeconds * MatchConstants.SimulationTicksPerSecond);

        /// <summary>Progresso do ciclo atual, de 0 a 1. E o que a barra da HUD desenha.</summary>
        public static float CycleProgress(MatchState state)
        {
            int total = CycleTicks(state.Config);
            return total <= 0 ? 0f : state.ElixirCycleTicks / (float)total;
        }

        /// <summary>
        /// Conta a barra e distribui as cargas na virada do ciclo.
        ///
        /// Nao recebe deltaTime de proposito: o passo e fixo por contrato (ADR-008) e a
        /// contagem e INTEIRA. Acumular segundos em float faria a batida cair um tick antes
        /// ou depois conforme o erro acumulado, e no online os dois lados divergiriam.
        /// </summary>
        public static void Advance(MatchState state, MatchEventQueue events)
        {
            int cycleTicks = CycleTicks(state.Config);
            if (cycleTicks <= 0)
            {
                return;
            }

            state.ElixirCycleTicks++;

            if (state.ElixirCycleTicks < cycleTicks)
            {
                return;
            }

            state.ElixirCycleTicks -= cycleTicks;
            state.CompletedElixirCycles++;

            events?.Enqueue(MatchEvent.ElixirCycleCompleted(state.Tick, state.CompletedElixirCycles));

            GrantCharge(state, PlayerSlot.Bottom, events);
            GrantCharge(state, PlayerSlot.Top, events);
        }

        /// <summary>
        /// Tenta absorver um acerto com uma carga de defesa. Devolve true se absorveu, e
        /// nesse caso a torre nao recebe dano.
        ///
        /// Gastar uma carga ZERA a contagem de ciclos limpos: a redencao volta a exigir a
        /// sequencia inteira.
        /// </summary>
        public static bool TryAbsorbHit(MatchState state, PlayerSlot slot, MatchEventQueue events)
        {
            ref PlayerState player = ref state.GetPlayer(slot);

            if (player.DefenseCharges == 0)
            {
                return false;
            }

            player.DefenseCharges--;
            player.CleanCycles = 0;

            events?.Enqueue(MatchEvent.DefenseChargeConsumed(state.Tick, slot, player.DefenseCharges));
            return true;
        }

        /// <summary>
        /// Coloca o jogador em modo berserk: ele para de receber cargas e perde as que tem.
        /// Chamado pela redencao, que e onde essa troca e paga.
        /// </summary>
        public static void EnterBerserk(MatchState state, PlayerSlot slot, MatchEventQueue events)
        {
            ref PlayerState player = ref state.GetPlayer(slot);

            player.DefenseCharges = 0;
            player.CleanCycles = 0;
            player.ReceivesCharges = false;

            events?.Enqueue(MatchEvent.DefenseChargeConsumed(state.Tick, slot, 0));
        }

        /// <summary>Devolve o jogador ao abastecimento normal.</summary>
        public static void ResumeCharging(MatchState state, PlayerSlot slot)
        {
            state.GetPlayer(slot).ReceivesCharges = true;
        }

        /// <summary>
        /// Se o jogador cumpriu a parte defensiva da redencao. A outra parte — ter um drop
        /// perdido pendente — e avaliada pelo sistema de power-ups.
        /// </summary>
        public static bool HasCleanStreakForRedemption(MatchState state, PlayerSlot slot)
        {
            PlayerState player = state.GetPlayer(slot);

            return player.ReceivesCharges
                   && player.CleanCycles >= state.Config.Elixir.CleanCyclesForRedemption
                   && player.DefenseCharges >= state.Config.Elixir.MaxDefenseCharges;
        }

        private static void GrantCharge(MatchState state, PlayerSlot slot, MatchEventQueue events)
        {
            ref PlayerState player = ref state.GetPlayer(slot);

            if (!player.ReceivesCharges)
            {
                return;
            }

            // A contagem limpa sobe mesmo com as cargas no teto: o que ela mede e tempo sem
            // tomar acerto, nao acumulo.
            if (player.CleanCycles < byte.MaxValue)
            {
                player.CleanCycles++;
            }

            if (player.DefenseCharges >= state.Config.Elixir.MaxDefenseCharges)
            {
                return;
            }

            player.DefenseCharges++;
            events?.Enqueue(MatchEvent.DefenseChargeGained(state.Tick, slot, player.DefenseCharges));
        }
    }
}
