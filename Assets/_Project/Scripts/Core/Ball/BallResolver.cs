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

            /// <summary>
            /// Posicao em X da superficie NO INSTANTE do contato. Importa para a raquete,
            /// que se move durante o tick: usar a posicao final daria um offset errado e,
            /// com ele, um angulo de saida errado.
            /// </summary>
            public readonly float SurfaceX;

            /// <summary>Quanto empurrar ao longo da normal para desencaixar a bola.</summary>
            public readonly float Separation;

            public SurfaceHit(float time, Vector2 normal, SurfaceKind kind, int index, float surfaceX, float separation)
            {
                Time = time;
                Normal = normal;
                Kind = kind;
                Index = index;
                SurfaceX = surfaceX;
                Separation = separation;
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

                // Fracao do tick que esta fatia representa. As raquetes se deslocam ao longo
                // do tick inteiro, entao a parte delas que cabe nesta fatia precisa ser
                // proporcional ao tempo que ainda resta.
                float timeScale = deltaTime > MinRemainingTime ? remaining / deltaTime : 0f;

                if (!TryFindEarliestHit(state, in ball, delta, timeScale, out SurfaceHit hit))
                {
                    ball.Position += delta;
                    break;
                }

                ball.Position += delta * hit.Time;
                ResolveHit(state, ref ball, ballIndex, in hit, events);

                // Separacao real, e nao uma folga simbolica: quando a raquete se move para
                // dentro da bola, empurrar 1 milimetro deixa a bola ainda sobreposta e ela
                // volta a colidir no tick seguinte, para sempre.
                ball.Position += hit.Normal * (hit.Separation + SurfaceSkin);

                remaining *= 1f - hit.Time;
                iterations++;
            }

            SeparateFromPaddles(state, ref ball);
            KeepInsideArena(state.Config, ref ball);
        }

        /// <summary>
        /// Garantia final de que a bola nao termina o tick dentro de uma raquete.
        ///
        /// A varredura sozinha nao basta quando a raquete se move PARA DENTRO da bola: cada
        /// iteracao resolve um contato novo em tempo praticamente zero e so empurra a folga
        /// de 1 milimetro, entao o orcamento de iteracoes acaba com a raquete tendo
        /// literalmente atropelado a bola. Aqui a bola e expulsa pelo eixo de menor
        /// penetracao, que e a saida mais curta e a que menos distorce a trajetoria.
        /// </summary>
        private static void SeparateFromPaddles(MatchState state, ref BallState ball)
        {
            float radius = state.Config.Ball.Radius;
            float reachX = state.Config.Paddle.HalfWidth + radius;
            float reachY = state.Config.Paddle.Thickness * 0.5f + radius;

            for (int i = 0; i < state.Paddles.Length; i++)
            {
                float toBallX = ball.Position.X - state.Paddles[i].PositionX;
                float toBallY = ball.Position.Y - state.Paddles[i].LineY;

                float overlapX = reachX - Math.Abs(toBallX);
                float overlapY = reachY - Math.Abs(toBallY);

                if (overlapX <= 0f || overlapY <= 0f)
                {
                    continue;
                }

                if (overlapX < overlapY)
                {
                    float sign = toBallX >= 0f ? 1f : -1f;
                    ball.Position = new Vector2(
                        ball.Position.X + sign * (overlapX + SurfaceSkin),
                        ball.Position.Y);
                }
                else
                {
                    float sign = toBallY >= 0f ? 1f : -1f;
                    ball.Position = new Vector2(
                        ball.Position.X,
                        ball.Position.Y + sign * (overlapY + SurfaceSkin));
                }
            }
        }

        private static bool TryFindEarliestHit(
            MatchState state,
            in BallState ball,
            Vector2 delta,
            float timeScale,
            out SurfaceHit hit)
        {
            hit = default;
            bool found = false;
            float bestTime = float.PositiveInfinity;

            var arenaHalfExtents = new Vector2(state.Config.Arena.HalfWidth, state.Config.Arena.HalfHeight);
            if (CollisionMath.SweepCircleInsideBounds(
                    ball.Position, delta, state.Config.Ball.Radius, arenaHalfExtents,
                    out float wallTime, out Vector2 wallNormal, out float wallSeparation)
                && wallTime < bestTime)
            {
                bestTime = wallTime;
                hit = new SurfaceHit(wallTime, wallNormal, SurfaceKind.Wall, 0, 0f, wallSeparation);
                found = true;
            }

            var paddleHalfSize = new Vector2(
                state.Config.Paddle.HalfWidth,
                state.Config.Paddle.Thickness * 0.5f);

            for (int i = 0; i < state.Paddles.Length; i++)
            {
                // Varredura RELATIVA: a raquete tambem se moveu neste tick. Resolver no
                // referencial dela e o que impede um arraste rapido de passar por dentro
                // da bola — que seria a rebatida "fantasma" mais frustrante possivel.
                float paddleTravel = (state.Paddles[i].PositionX - state.Paddles[i].PreviousPositionX) * timeScale;
                float paddleStartX = state.Paddles[i].PositionX - paddleTravel;

                var paddleCenter = new Vector2(paddleStartX, state.Paddles[i].LineY);
                var relativeDelta = new Vector2(delta.X - paddleTravel, delta.Y);

                if (CollisionMath.SweepCircleVsBox(
                        ball.Position, relativeDelta, state.Config.Ball.Radius, paddleCenter, paddleHalfSize,
                        out float paddleTime, out Vector2 paddleNormal, out float paddleSeparation)
                    && paddleTime < bestTime)
                {
                    bestTime = paddleTime;
                    float contactX = paddleStartX + paddleTravel * paddleTime;
                    hit = new SurfaceHit(paddleTime, paddleNormal, SurfaceKind.Paddle, i, contactX, paddleSeparation);
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
                        out float towerTime, out Vector2 towerNormal, out float towerSeparation)
                    && towerTime < bestTime)
                {
                    bestTime = towerTime;
                    hit = new SurfaceHit(
                        towerTime, towerNormal, SurfaceKind.Tower, i, state.Towers[i].Position.X, towerSeparation);
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

            // A raquete de baixo defende voltada para cima; a de cima, para baixo.
            float inwardSign = -slot.DirectionSign();

            // So a FACE DA FRENTE rebate. A bola pode chegar por tras — ela passa pelos vaos
            // entre as torres e volta — e nesse caso aplicar a deflexao a empurraria para
            // DENTRO da raquete, prendendo-a ali para sempre. De quebra, cada tick preso
            // somaria +2% de velocidade, o que fazia a bola disparar do nada.
            bool hitFrontFace = hit.Normal.Y * inwardSign > 0f;

            if (!hitFrontFace)
            {
                ball.Direction = CollisionMath.EnforceMinAngleFromHorizontal(
                    CollisionMath.Reflect(ball.Direction, hit.Normal),
                    state.Config.Ball.MinAngleFromHorizontalDegrees);

                ball.CollisionSequence++;
                events.Enqueue(new MatchEvent(
                    MatchEventType.BallHitObstacle, state.Tick, slot, ballIndex, 0f, ball.Position));
                return;
            }

            float offset = CollisionMath.NormalizedPaddleOffset(
                ball.Position.X,
                hit.SurfaceX,
                state.Config.Paddle.HalfWidth);

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
