using System;
using System.Numerics;
using PongRoyale.Core.Combat;
using PongRoyale.Core.Events;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Core.Ball
{
    /// <summary>
    /// Avanca as bolas de um tick, resolvendo colisoes por varredura.
    ///
    /// O laco consome o tempo do passo em fatias: encontra a colisao mais proxima, move
    /// ate exatamente aquele instante, responde, e continua com o tempo que sobrou. Isso
    /// e o que permite uma bola rapida bater na raquete, quicar na parede lateral e ainda
    /// atingir uma torre dentro do MESMO tick, sem atravessar nada.
    ///
    /// O orcamento de iteracoes por tick (MatchConstants.MaxCollisionIterationsPerTick) e
    /// um limite duro: se a bola ficar encurralada entre duas superficies, ela para de se
    /// mover neste tick em vez de travar o jogo num laco infinito.
    /// </summary>
    public static class BallResolver
    {
        /// <summary>
        /// Folga aplicada ao longo da normal depois de cada colisao. Sem ela o erro de
        /// ponto flutuante deixa a bola exatamente sobre a superficie e a colisao seguinte
        /// dispara no mesmo lugar, prendendo a bola.
        /// </summary>
        private const float SurfaceSkin = 1e-3f;

        private const float MinRemainingTime = 1e-6f;

        private enum SurfaceKind : byte
        {
            Wall = 0,
            Paddle = 1,
            Tower = 2
        }

        private readonly struct SurfaceHit
        {
            public readonly float Time;
            public readonly Vector2 Normal;
            public readonly SurfaceKind Kind;
            public readonly int Index;

            public SurfaceHit(float time, Vector2 normal, SurfaceKind kind, int index)
            {
                Time = time;
                Normal = normal;
                Kind = kind;
                Index = index;
            }
        }

        public static void Advance(MatchState state, float deltaTime, MatchEventQueue events)
        {
            for (int i = 0; i < state.Balls.Length; i++)
            {
                if (!state.Balls[i].IsActive)
                {
                    continue;
                }

                AdvanceBall(state, ref state.Balls[i], (byte)i, deltaTime, events);
            }
        }

        private static void AdvanceBall(
            MatchState state,
            ref BallState ball,
            byte ballIndex,
            float deltaTime,
            MatchEventQueue events)
        {
            float remaining = deltaTime;
            int iterations = 0;

            while (remaining > MinRemainingTime && iterations < MatchConstants.MaxCollisionIterationsPerTick)
            {
                Vector2 delta = ball.Direction * (ball.CurrentSpeed * remaining);

                if (!TryFindEarliestHit(state, in ball, delta, out SurfaceHit hit))
                {
                    ball.Position += delta;
                    break;
                }

                ball.Position += delta * hit.Time;
                ResolveHit(state, ref ball, ballIndex, in hit, events);
                ball.Position += hit.Normal * SurfaceSkin;

                remaining *= 1f - hit.Time;
                iterations++;
            }

            KeepInsideArena(state.Config, ref ball);
        }

        private static bool TryFindEarliestHit(
            MatchState state,
            in BallState ball,
            Vector2 delta,
            out SurfaceHit hit)
        {
            hit = default;
            bool found = false;
            float bestTime = float.PositiveInfinity;

            var arenaHalfExtents = new Vector2(state.Config.Arena.HalfWidth, state.Config.Arena.HalfHeight);
            if (CollisionMath.SweepCircleInsideBounds(
                    ball.Position, delta, state.Config.Ball.Radius, arenaHalfExtents,
                    out float wallTime, out Vector2 wallNormal)
                && wallTime < bestTime)
            {
                bestTime = wallTime;
                hit = new SurfaceHit(wallTime, wallNormal, SurfaceKind.Wall, 0);
                found = true;
            }

            var paddleHalfSize = new Vector2(
                state.Config.Paddle.HalfWidth,
                state.Config.Paddle.Thickness * 0.5f);

            for (int i = 0; i < state.Paddles.Length; i++)
            {
                var paddleCenter = new Vector2(state.Paddles[i].PositionX, state.Paddles[i].LineY);

                if (CollisionMath.SweepCircleVsBox(
                        ball.Position, delta, state.Config.Ball.Radius, paddleCenter, paddleHalfSize,
                        out float paddleTime, out Vector2 paddleNormal)
                    && paddleTime < bestTime)
                {
                    bestTime = paddleTime;
                    hit = new SurfaceHit(paddleTime, paddleNormal, SurfaceKind.Paddle, i);
                    found = true;
                }
            }

            for (int i = 0; i < state.Towers.Length; i++)
            {
                if (!state.Towers[i].IsAlive)
                {
                    continue;
                }

                var towerHalfSize = new Vector2(state.Towers[i].HalfWidth, state.Towers[i].HalfHeight);

                if (CollisionMath.SweepCircleVsBox(
                        ball.Position, delta, state.Config.Ball.Radius,
                        state.Towers[i].Position, towerHalfSize,
                        out float towerTime, out Vector2 towerNormal)
                    && towerTime < bestTime)
                {
                    bestTime = towerTime;
                    hit = new SurfaceHit(towerTime, towerNormal, SurfaceKind.Tower, i);
                    found = true;
                }
            }

            return found;
        }

        private static void ResolveHit(
            MatchState state,
            ref BallState ball,
            byte ballIndex,
            in SurfaceHit hit,
            MatchEventQueue events)
        {
            switch (hit.Kind)
            {
                case SurfaceKind.Paddle:
                    ResolvePaddleHit(state, ref ball, ballIndex, in hit, events);
                    break;

                case SurfaceKind.Tower:
                    ResolveTowerHit(state, ref ball, in hit, events);
                    break;

                default:
                    ResolveWallHit(state, ref ball, ballIndex, in hit, events);
                    break;
            }
        }

        private static void ResolveWallHit(
            MatchState state,
            ref BallState ball,
            byte ballIndex,
            in SurfaceHit hit,
            MatchEventQueue events)
        {
            ball.Direction = CollisionMath.EnforceMinAngleFromHorizontal(
                CollisionMath.Reflect(ball.Direction, hit.Normal),
                state.Config.Ball.MinAngleFromHorizontalDegrees);

            ball.CollisionSequence++;
            events.Enqueue(MatchEvent.BallHitWall(state.Tick, ballIndex, ball.Position));
        }

        private static void ResolvePaddleHit(
            MatchState state,
            ref BallState ball,
            byte ballIndex,
            in SurfaceHit hit,
            MatchEventQueue events)
        {
            var slot = (PlayerSlot)hit.Index;

            float offset = CollisionMath.NormalizedPaddleOffset(
                ball.Position.X,
                state.Paddles[hit.Index].PositionX,
                state.Config.Paddle.HalfWidth);

            // A normal aponta para dentro da arena: a raquete de baixo devolve para cima.
            float inwardSign = -slot.DirectionSign();

            ball.Direction = CollisionMath.EnforceMinAngleFromHorizontal(
                CollisionMath.PaddleDeflection(
                    offset,
                    state.Config.Ball.MaxDeflectionFromNormalDegrees,
                    inwardSign),
                state.Config.Ball.MinAngleFromHorizontalDegrees);

            ball.BaseSpeed = Math.Min(
                ball.BaseSpeed * (1f + state.Config.Ball.SpeedGainPerHit),
                state.Config.Ball.MaxSpeed);

            ball.LastHitByPlayer = (sbyte)slot;
            ball.CollisionSequence++;

            events.Enqueue(MatchEvent.BallHitPaddle(state.Tick, slot, ballIndex, ball.CurrentSpeed, ball.Position));
        }

        private static void ResolveTowerHit(
            MatchState state,
            ref BallState ball,
            in SurfaceHit hit,
            MatchEventQueue events)
        {
            ref TowerState tower = ref state.Towers[hit.Index];
            var owner = (PlayerSlot)tower.OwnerSlot;
            Vector2 impactPosition = ball.Position;

            bool destroyed = DamageResolver.ApplyDamage(ref tower, ball.Damage);

            events.Enqueue(MatchEvent.TowerDamaged(
                state.Tick, owner, (byte)hit.Index, ball.Damage, impactPosition));

            if (destroyed)
            {
                state.GetPlayer(owner.Opponent()).TowersDestroyed++;
                events.Enqueue(MatchEvent.TowerDestroyed(state.Tick, owner, (byte)hit.Index, impactPosition));
            }

            ball.Direction = CollisionMath.EnforceMinAngleFromHorizontal(
                CollisionMath.Reflect(ball.Direction, hit.Normal),
                state.Config.Ball.MinAngleFromHorizontalDegrees);

            ball.CollisionSequence++;
        }

        /// <summary>
        /// Rede de seguranca. A varredura sozinha ja deveria manter a bola dentro, mas um
        /// unico escape por erro numerico significaria bola perdida e partida travada.
        /// Custa duas comparacoes por tick e elimina a classe inteira de bug.
        /// </summary>
        private static void KeepInsideArena(MatchConfig config, ref BallState ball)
        {
            float limitX = config.Arena.HalfWidth - config.Ball.Radius;
            float limitY = config.Arena.HalfHeight - config.Ball.Radius;

            ball.Position = new Vector2(
                Math.Clamp(ball.Position.X, -limitX, limitX),
                Math.Clamp(ball.Position.Y, -limitY, limitY));
        }
    }
}
