using System;
using PongRoyale.Core.Events;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Core.Effects
{
    /// <summary>
    /// Ciclo de vida dos efeitos de power-up: conceder, contar o tempo e expirar.
    ///
    /// Este resolver nao sabe o que cada efeito FAZ. Ele so mantem quais estao em vigor e
    /// por quanto tempo; quem consome sao os outros resolvers, perguntando
    /// <see cref="IsActive"/>. Essa separacao e o que permite construir toda a aquisicao de
    /// power-ups — drop, coleta, interceptacao, combinacao, redencao — antes de existir um
    /// unico efeito concreto.
    /// </summary>
    public static class EffectResolver
    {
        /// <summary>
        /// Concede um efeito a um jogador.
        ///
        /// REGRA DA COMBINACAO: se o jogador ja tinha algum efeito em vigor, os efeitos somam
        /// e TODOS passam a durar o tempo combinado — mais curto que a duracao normal. E a
        /// decisao de ritmo do jogo: correr para a segunda torre e combinar forte e curto, ou
        /// espacar as duas coletas e ter dois periodos longos.
        /// </summary>
        public static void Grant(
            MatchState state, PlayerSlot slot, ushort effectId, float durationSeconds, MatchEventQueue events)
        {
            if (effectId == ActiveEffect.None || durationSeconds <= 0f)
            {
                return;
            }

            bool hadActiveEffect = HasAnyActive(state, slot);
            int index = FindSlotFor(state, slot, effectId);

            state.Effects[index] = ActiveEffect.Create(effectId, durationSeconds);

            if (hadActiveEffect)
            {
                ApplyCombinedDuration(state, slot);
            }

            events?.Enqueue(MatchEvent.EffectGained(state.Tick, slot, effectId));
        }

        /// <summary>Conta o tempo e expira o que acabou. Chamado uma vez por tick.</summary>
        public static void Advance(MatchState state, float deltaTime, MatchEventQueue events)
        {
            for (int i = 0; i < state.Effects.Length; i++)
            {
                if (!state.Effects[i].IsActive)
                {
                    continue;
                }

                state.Effects[i].RemainingSeconds -= deltaTime;

                if (state.Effects[i].RemainingSeconds > 0f)
                {
                    continue;
                }

                ushort expiredId = state.Effects[i].EffectId;
                var owner = (PlayerSlot)(i / MatchState.MaxEffectsPerPlayer);

                state.Effects[i] = ActiveEffect.Empty;
                events?.Enqueue(MatchEvent.EffectExpired(state.Tick, owner, expiredId));
            }
        }

        /// <summary>
        /// Superficie de consulta para os demais resolvers. E assim que a bola, a raquete ou
        /// as torres perguntam se um efeito esta valendo, sem conhecer este sistema.
        /// </summary>
        public static bool IsActive(MatchState state, PlayerSlot slot, ushort effectId)
        {
            int first = slot.ToIndex() * MatchState.MaxEffectsPerPlayer;

            for (int i = first; i < first + MatchState.MaxEffectsPerPlayer; i++)
            {
                if (state.Effects[i].EffectId == effectId && state.Effects[i].IsActive)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasAnyActive(MatchState state, PlayerSlot slot)
        {
            int first = slot.ToIndex() * MatchState.MaxEffectsPerPlayer;

            for (int i = first; i < first + MatchState.MaxEffectsPerPlayer; i++)
            {
                if (state.Effects[i].IsActive)
                {
                    return true;
                }
            }

            return false;
        }

        public static int CountActive(MatchState state, PlayerSlot slot)
        {
            int first = slot.ToIndex() * MatchState.MaxEffectsPerPlayer;
            int active = 0;

            for (int i = first; i < first + MatchState.MaxEffectsPerPlayer; i++)
            {
                if (state.Effects[i].IsActive)
                {
                    active++;
                }
            }

            return active;
        }

        /// <summary>
        /// Onde guardar o efeito: reaproveita o slot do mesmo efeito se ele ja estiver em
        /// vigor (recoletar renova, nao duplica), senao o primeiro livre. Sem slot livre,
        /// substitui o que esta mais perto de acabar — perder o efeito com menos tempo
        /// restante e o descarte menos custoso.
        /// </summary>
        private static int FindSlotFor(MatchState state, PlayerSlot slot, ushort effectId)
        {
            int first = slot.ToIndex() * MatchState.MaxEffectsPerPlayer;
            int freeSlot = -1;
            int shortestSlot = first;
            float shortestRemaining = float.MaxValue;

            for (int i = first; i < first + MatchState.MaxEffectsPerPlayer; i++)
            {
                if (state.Effects[i].EffectId == effectId && state.Effects[i].IsActive)
                {
                    return i;
                }

                if (!state.Effects[i].IsActive && freeSlot < 0)
                {
                    freeSlot = i;
                }

                if (state.Effects[i].RemainingSeconds < shortestRemaining)
                {
                    shortestRemaining = state.Effects[i].RemainingSeconds;
                    shortestSlot = i;
                }
            }

            return freeSlot >= 0 ? freeSlot : shortestSlot;
        }

        private static void ApplyCombinedDuration(MatchState state, PlayerSlot slot)
        {
            float combined = state.Config.Effects.CombinedDurationSeconds;
            int first = slot.ToIndex() * MatchState.MaxEffectsPerPlayer;

            for (int i = first; i < first + MatchState.MaxEffectsPerPlayer; i++)
            {
                if (state.Effects[i].IsActive)
                {
                    // O tempo combinado SUBSTITUI o restante, para mais ou para menos: dois
                    // efeitos juntos valem uma janela curta e fixa, seja qual for o momento
                    // em que o segundo foi coletado.
                    state.Effects[i].RemainingSeconds = Math.Max(combined, 0f);
                }
            }
        }
    }
}
