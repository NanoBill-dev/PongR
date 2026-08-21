using System;
using System.Numerics;
using PongRoyale.Core.Combat;
using PongRoyale.Core.Effects;
using PongRoyale.Core.Events;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Core.Pickups
{
    /// <summary>
    /// A queda, a coleta e a interceptacao dos power-ups.
    ///
    /// Esta e a peca que faz o sistema inteiro funcionar, e o motivo e um so: coletar exige
    /// a RAQUETE, que e a mesma que esta defendendo. Destruir a torre nao entrega o premio,
    /// entrega uma ESCOLHA — buscar o power-up e abrir a defesa, ou manter a defesa e perder
    /// o premio. E o mesmo recurso disputado por dois objetivos ao mesmo tempo, que e o que
    /// impede o sistema de virar bola de neve para quem ja esta ganhando.
    /// </summary>
    public static class PickupResolver
    {
        /// <summary>
        /// Nasce a partir de uma torre lateral destruida, carregando a carta que o ADVERSARIO
        /// do dono da torre escolheu colocar ali, e cai em direcao a ele.
        ///
        /// Vale inclusive no gol contra: a carta esta atribuida aquela torre, nao a quem a
        /// derrubou. Derrubar a propria torre lateral entrega o premio ao adversario.
        /// </summary>
        public static void SpawnFromTower(MatchState state, int towerIndex, MatchEventQueue events)
        {
            TowerState tower = state.Towers[towerIndex];

            if (tower.RewardEffectId == 0 || tower.Kind == TowerKind.King)
            {
                return;
            }

            PlayerSlot collector = ((PlayerSlot)tower.OwnerSlot).Opponent();
            Spawn(state, tower.Position, tower.RewardEffectId, collector, canBeIntercepted: true, events);
        }

        public static bool Spawn(
            MatchState state,
            Vector2 position,
            ushort effectId,
            PlayerSlot collector,
            bool canBeIntercepted,
            MatchEventQueue events)
        {
            if (effectId == 0)
            {
                return false;
            }

            int slot = FindFreeSlot(state);
            if (slot < 0)
            {
                // Teto atingido. Falhar em silencio e melhor que crescer array em partida;
                // com 2 torres laterais por lado mais a redencao, o teto nao e alcancavel.
                return false;
            }

            state.Pickups[slot] = PickupState.Create(position, effectId, collector, canBeIntercepted);
            events?.Enqueue(MatchEvent.PickupSpawned(state.Tick, collector, effectId, position));
            return true;
        }

        /// <summary>Avanca a queda e resolve coleta, interceptacao e saida da arena.</summary>
        public static void Advance(MatchState state, float deltaTime, MatchEventQueue events)
        {
            for (int i = 0; i < state.Pickups.Length; i++)
            {
                if (state.Pickups[i].IsActive)
                {
                    AdvanceOne(state, ref state.Pickups[i], deltaTime, events);
                }
            }
        }

        private static void AdvanceOne(
            MatchState state, ref PickupState pickup, float deltaTime, MatchEventQueue events)
        {
            PlayerSlot collector = pickup.Collector;
            PlayerSlot interceptor = collector.Opponent();

            // Cai em direcao ao lado de quem vai coletar.
            float fallDirection = collector.DirectionSign();
            float previousY = pickup.Position.Y;
            float nextY = previousY + fallDirection * state.Config.Pickup.FallSpeed * deltaTime;

            pickup.Position = new Vector2(pickup.Position.X, nextY);

            // A raquete do adversario esta ANTES no caminho, entao a chance dele vem primeiro.
            if (pickup.CanBeIntercepted
                && state.GetPlayer(interceptor).HasInterceptionAvailable
                && CrossedPaddleLine(state, interceptor, previousY, nextY)
                && IsWithinPaddle(state, interceptor, pickup.Position.X))
            {
                state.GetPlayer(interceptor).HasInterceptionAvailable = false;
                events?.Enqueue(MatchEvent.PickupIntercepted(
                    state.Tick, interceptor, pickup.EffectId, pickup.Position));

                pickup = PickupState.Empty;
                return;
            }

            if (CrossedPaddleLine(state, collector, previousY, nextY)
                && IsWithinPaddle(state, collector, pickup.Position.X))
            {
                EffectResolver.Grant(
                    state, collector, pickup.EffectId, state.Config.Effects.DefaultDurationSeconds, events);

                events?.Enqueue(MatchEvent.PickupCollected(
                    state.Tick, collector, pickup.EffectId, pickup.Position));

                pickup = PickupState.Empty;
                return;
            }

            // O criterio de descarte e a borda da ARENA, e nao a borda da tela. A area
            // visivel muda com o aparelho, e no online os dois lados discordariam sobre
            // quando o drop sumiu.
            if (Math.Abs(nextY) >= state.Config.Arena.HalfHeight)
            {
                RecordLostDrop(state, collector, pickup.EffectId);

                events?.Enqueue(MatchEvent.PickupLost(
                    state.Tick, collector, pickup.EffectId, pickup.Position));

                pickup = PickupState.Empty;
            }
        }

        private static bool CrossedPaddleLine(MatchState state, PlayerSlot slot, float previousY, float nextY)
        {
            float line = state.GetPaddle(slot).LineY;
            return (previousY > line && nextY <= line) || (previousY < line && nextY >= line);
        }

        private static bool IsWithinPaddle(MatchState state, PlayerSlot slot, float pickupX)
        {
            float reach = MatchModifiers.PaddleHalfWidth(state, slot) + state.Config.Pickup.Radius;
            return Math.Abs(pickupX - state.GetPaddle(slot).PositionX) <= reach;
        }

        /// <summary>
        /// Guarda o drop perdido para a redencao do passo 5. So conta perda por NAO TER
        /// COLETADO: drop interceptado nao entra, porque a redencao existe para dar segunda
        /// chance a um erro proprio, nao para desfazer uma jogada do adversario.
        /// </summary>
        private static void RecordLostDrop(MatchState state, PlayerSlot collector, ushort effectId)
        {
            int first = collector.ToIndex() * MatchState.MaxLostDropsPerPlayer;

            for (int i = first; i < first + MatchState.MaxLostDropsPerPlayer; i++)
            {
                if (state.LostDrops[i] == 0)
                {
                    state.LostDrops[i] = effectId;
                    return;
                }
            }
        }

        public static bool HasLostDrop(MatchState state, PlayerSlot slot)
        {
            int first = slot.ToIndex() * MatchState.MaxLostDropsPerPlayer;

            for (int i = first; i < first + MatchState.MaxLostDropsPerPlayer; i++)
            {
                if (state.LostDrops[i] != 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static int CountActive(MatchState state)
        {
            int active = 0;
            for (int i = 0; i < state.Pickups.Length; i++)
            {
                if (state.Pickups[i].IsActive)
                {
                    active++;
                }
            }

            return active;
        }

        private static int FindFreeSlot(MatchState state)
        {
            for (int i = 0; i < state.Pickups.Length; i++)
            {
                if (!state.Pickups[i].IsActive)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
