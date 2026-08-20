using System;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Core.Paddle
{
    /// <summary>
    /// Move as raquetes em direcao ao alvo pedido pelo input.
    ///
    /// Este e o ponto onde as tres protecoes da secao 20 sao aplicadas, e todas moram na
    /// SIMULACAO, nunca na UI — assim valem identicas quando o servidor rodar este mesmo
    /// codigo (ADR-001):
    ///
    ///   1. O alvo pedido e saturado nos limites da arena. Nao existe pedir posicao ilegal.
    ///   2. O passo por tick e limitado por MaxSpeed. Nao existe teleporte.
    ///   3. A posicao final e saturada de novo. Nao existe sair da arena por erro numerico.
    ///
    /// Um cliente adulterado pode mandar o comando que quiser: o pior que consegue e pedir
    /// a borda da arena, que e exatamente o que um jogador honesto tambem pode pedir.
    /// </summary>
    public static class PaddleResolver
    {
        private const float Epsilon = 1e-5f;

        /// <summary>
        /// Registra o alvo pedido por um comando, ja saturado. Separado de
        /// <see cref="Advance"/> porque comandos chegam em ritmo diferente do tick:
        /// 30 por segundo do input contra 60 de simulacao.
        /// </summary>
        public static void SetTarget(ref PaddleState paddle, float requestedX, MatchConfig config)
        {
            paddle.TargetX = ClampToArena(requestedX, config);
        }

        public static void Advance(MatchState state, float deltaTime)
        {
            for (int i = 0; i < state.Paddles.Length; i++)
            {
                AdvancePaddle(ref state.Paddles[i], state.Config, deltaTime);
            }
        }

        private static void AdvancePaddle(ref PaddleState paddle, MatchConfig config, float deltaTime)
        {
            paddle.PreviousPositionX = paddle.PositionX;

            if (deltaTime <= Epsilon)
            {
                paddle.VelocityX = 0f;
                return;
            }

            float target = ClampToArena(paddle.TargetX, config);

            // Suavizacao exponencial: a fracao percorrida depende do tempo decorrido, nao
            // do numero de ticks. Com passo fixo isso e equivalente a um lerp, mas nao
            // quebra se um dia o passo variar.
            float blend = config.Paddle.SmoothingTime <= Epsilon
                ? 1f
                : 1f - (float)Math.Exp(-deltaTime / config.Paddle.SmoothingTime);

            float step = (target - paddle.PositionX) * blend;
            float maxStep = config.Paddle.MaxSpeed * deltaTime;
            step = Math.Clamp(step, -maxStep, maxStep);

            float next = ClampToArena(paddle.PositionX + step, config);

            paddle.VelocityX = (next - paddle.PositionX) / deltaTime;
            paddle.PositionX = next;
        }

        /// <summary>
        /// Limite legal do centro da raquete: a arena menos a propria meia-largura, para
        /// que a raquete inteira caiba dentro do campo.
        /// </summary>
        public static float ClampToArena(float x, MatchConfig config)
        {
            float limit = Math.Max(0f, config.Arena.HalfWidth - config.Paddle.HalfWidth);
            return Math.Clamp(x, -limit, limit);
        }
    }
}
