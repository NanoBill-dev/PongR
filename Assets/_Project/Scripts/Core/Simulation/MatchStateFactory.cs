using System.Numerics;
using PongRoyale.Core.Ball;
using PongRoyale.Core.Combat;
using PongRoyale.Core.Paddle;

namespace PongRoyale.Core.Simulation
{
    /// <summary>
    /// Monta o estado inicial de uma partida a partir da configuracao. Toda a geometria
    /// da arena e derivada aqui, uma unica vez, para que nenhum resolver precise recalcular
    /// posicao de torre ou linha de raquete durante o jogo.
    ///
    /// Nao ha aleatoriedade: quem saca e parametro. Duas chamadas com os mesmos argumentos
    /// produzem estados identicos, o que e o que torna teste e replay possiveis (ADR-002).
    /// </summary>
    public static class MatchStateFactory
    {
        public static MatchState CreateInitial(MatchConfig config, PlayerSlot serveToward)
        {
            var state = new MatchState(config);

            CreatePaddles(state, config);
            CreateTowers(state, config);
            CreatePlayers(state);
            SpawnInitialBall(state, config, serveToward);

            return state;
        }

        private static void CreatePaddles(MatchState state, MatchConfig config)
        {
            float lineDistanceFromCenter = config.Arena.HalfHeight - config.Arena.PaddleLineOffsetFromEdge;

            state.Paddles[PlayerSlot.Bottom.ToIndex()] =
                PaddleState.Create(PlayerSlot.Bottom.DirectionSign() * lineDistanceFromCenter);

            state.Paddles[PlayerSlot.Top.ToIndex()] =
                PaddleState.Create(PlayerSlot.Top.DirectionSign() * lineDistanceFromCenter);
        }

        private static void CreateTowers(MatchState state, MatchConfig config)
        {
            CreateTowerRow(state, config, PlayerSlot.Bottom);
            CreateTowerRow(state, config, PlayerSlot.Top);
        }

        private static void CreateTowerRow(MatchState state, MatchConfig config, PlayerSlot slot)
        {
            TowerConfig towers = config.Tower;
            float rowY = slot.DirectionSign() * (config.Arena.HalfHeight - towers.RowOffsetFromEdge);
            byte owner = (byte)slot;

            state.Towers[MatchState.TowerIndex(slot, TowerKind.King)] = TowerState.Create(
                new Vector2(0f, rowY),
                towers.KingMaxHealth,
                towers.KingHalfSize.X,
                towers.KingHalfSize.Y,
                TowerKind.King,
                owner);

            state.Towers[MatchState.TowerIndex(slot, TowerKind.LeftGuard)] = TowerState.Create(
                new Vector2(-towers.GuardOffsetFromCenter, rowY),
                towers.GuardMaxHealth,
                towers.GuardHalfSize.X,
                towers.GuardHalfSize.Y,
                TowerKind.LeftGuard,
                owner);

            state.Towers[MatchState.TowerIndex(slot, TowerKind.RightGuard)] = TowerState.Create(
                new Vector2(towers.GuardOffsetFromCenter, rowY),
                towers.GuardMaxHealth,
                towers.GuardHalfSize.X,
                towers.GuardHalfSize.Y,
                TowerKind.RightGuard,
                owner);
        }

        private static void CreatePlayers(MatchState state)
        {
            for (int i = 0; i < state.Players.Length; i++)
            {
                state.Players[i] = PlayerState.Create();
            }
        }

        private static void SpawnInitialBall(MatchState state, MatchConfig config, PlayerSlot serveToward)
        {
            var direction = new Vector2(0f, serveToward.DirectionSign());

            state.Balls[0] = BallState.Create(
                Vector2.Zero,
                direction,
                config.Ball.InitialSpeed,
                config.Ball.BaseDamage);
        }
    }
}
