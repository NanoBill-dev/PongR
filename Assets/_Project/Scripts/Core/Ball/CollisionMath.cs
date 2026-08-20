using System;
using System.Numerics;

namespace PongRoyale.Core.Ball
{
    /// <summary>
    /// Geometria de colisao da bola. Funcoes puras: entram numeros, saem numeros, sem
    /// estado e sem dependencia da simulacao. Isso e proposital — e o pedaco mais sujeito
    /// a bug sutil do projeto, entao precisa ser testavel isoladamente.
    ///
    /// Todas as consultas sao por VARREDURA (swept), nunca por sobreposicao. A 25 u/s a
    /// bola percorre 0,42 unidades por tick e a raquete tem 0,4 de espessura: testar
    /// sobreposicao no fim do passo simplesmente perderia a raquete (tunneling).
    /// </summary>
    public static class CollisionMath
    {
        public const float Epsilon = 1e-5f;

        private const float DegreesToRadians = (float)(Math.PI / 180.0);

        /// <summary>
        /// Circulo varrido contra uma caixa alinhada aos eixos, pelo metodo das fatias
        /// (slab). A caixa e inflada pelo raio da bola, o que reduz o problema a um raio
        /// contra caixa.
        ///
        /// Limitacao aceita: inflar a caixa deixa os cantos quadrados em vez de
        /// arredondados. Na pratica isso significa que um roçar exatamente na quina
        /// registra colisao alguns milimetros antes do ideal geometrico. Para um jogo
        /// arcade e imperceptivel, e evita a matematica de capsula.
        /// </summary>
        /// <param name="origin">Centro da bola no inicio do movimento.</param>
        /// <param name="delta">Deslocamento completo do passo.</param>
        /// <param name="time">Fracao de 0 a 1 do deslocamento ate o toque.</param>
        /// <param name="normal">Normal da face tocada, apontando contra o movimento.</param>
        public static bool SweepCircleVsBox(
            Vector2 origin,
            Vector2 delta,
            float radius,
            Vector2 boxCenter,
            Vector2 boxHalfSize,
            out float time,
            out Vector2 normal)
        {
            time = 0f;
            normal = Vector2.Zero;

            Vector2 relative = origin - boxCenter;
            Vector2 expanded = boxHalfSize + new Vector2(radius, radius);

            float entryTime = float.NegativeInfinity;
            float exitTime = float.PositiveInfinity;
            int entryAxis = -1;
            float entryNormalSign = 0f;

            for (int axis = 0; axis < 2; axis++)
            {
                float axisDelta = axis == 0 ? delta.X : delta.Y;
                float axisRelative = axis == 0 ? relative.X : relative.Y;
                float axisExtent = axis == 0 ? expanded.X : expanded.Y;

                if (Math.Abs(axisDelta) < Epsilon)
                {
                    // Movimento paralelo a este eixo: so ha colisao se ja estiver dentro da faixa.
                    if (Math.Abs(axisRelative) > axisExtent)
                    {
                        return false;
                    }

                    continue;
                }

                float first = (-axisExtent - axisRelative) / axisDelta;
                float second = (axisExtent - axisRelative) / axisDelta;
                float near = Math.Min(first, second);
                float far = Math.Max(first, second);

                if (near > entryTime)
                {
                    entryTime = near;
                    entryAxis = axis;
                    entryNormalSign = axisDelta > 0f ? -1f : 1f;
                }

                exitTime = Math.Min(exitTime, far);
            }

            if (entryAxis < 0)
            {
                // Parado nos dois eixos: nao ha varredura a fazer.
                return false;
            }

            if (entryTime > exitTime || entryTime > 1f || exitTime < 0f)
            {
                return false;
            }

            // entryTime negativo significa que a bola ja comecou sobreposta. Resolve no
            // instante zero empurrando para fora, em vez de ignorar e deixar atravessar.
            time = entryTime < 0f ? 0f : entryTime;
            normal = entryAxis == 0
                ? new Vector2(entryNormalSign, 0f)
                : new Vector2(0f, entryNormalSign);
            return true;
        }

        /// <summary>
        /// Circulo varrido contra as paredes da arena, visto por dentro. Devolve a parede
        /// alcancada primeiro dentro do passo.
        /// </summary>
        public static bool SweepCircleInsideBounds(
            Vector2 origin,
            Vector2 delta,
            float radius,
            Vector2 halfExtents,
            out float time,
            out Vector2 normal)
        {
            time = 1f;
            normal = Vector2.Zero;
            bool hit = false;

            TryBoundsAxis(origin.X, delta.X, halfExtents.X - radius, isHorizontal: true, ref time, ref normal, ref hit);
            TryBoundsAxis(origin.Y, delta.Y, halfExtents.Y - radius, isHorizontal: false, ref time, ref normal, ref hit);

            return hit;
        }

        private static void TryBoundsAxis(
            float origin,
            float delta,
            float limit,
            bool isHorizontal,
            ref float bestTime,
            ref Vector2 bestNormal,
            ref bool hit)
        {
            if (Math.Abs(delta) < Epsilon)
            {
                return;
            }

            float target = delta > 0f ? limit : -limit;
            float candidate = (target - origin) / delta;

            if (candidate < 0f || candidate > bestTime)
            {
                return;
            }

            bestTime = candidate;
            float sign = delta > 0f ? -1f : 1f;
            bestNormal = isHorizontal ? new Vector2(sign, 0f) : new Vector2(0f, sign);
            hit = true;
        }

        /// <summary>Reflexao especular de uma direcao em torno de uma normal unitaria.</summary>
        public static Vector2 Reflect(Vector2 direction, Vector2 normal)
        {
            return direction - 2f * Vector2.Dot(direction, normal) * normal;
        }

        /// <summary>
        /// Direcao de saida de uma rebatida, em funcao de onde a bola tocou a raquete.
        /// E aqui que mora a habilidade mecanica do jogo (secao 37): acertar de raspao
        /// manda a bola para o lado, acertar no centro devolve reto.
        /// </summary>
        /// <param name="normalizedOffset">-1 na ponta esquerda, 0 no centro, +1 na direita.</param>
        /// <param name="inwardNormalSign">+1 para a raquete de baixo, -1 para a de cima.</param>
        public static Vector2 PaddleDeflection(
            float normalizedOffset,
            float maxDeflectionDegrees,
            float inwardNormalSign)
        {
            float clamped = Math.Clamp(normalizedOffset, -1f, 1f);
            float angle = clamped * maxDeflectionDegrees * DegreesToRadians;

            return new Vector2(
                (float)Math.Sin(angle),
                inwardNormalSign * (float)Math.Cos(angle));
        }

        /// <summary>
        /// Garante que a direcao mantenha um angulo minimo com a horizontal. Sem isso a
        /// bola pode entrar num vai-e-vem quase horizontal entre as paredes laterais e
        /// nunca mais chegar a uma raquete — a partida trava sem ninguem errar nada.
        /// </summary>
        public static Vector2 EnforceMinAngleFromHorizontal(Vector2 direction, float minAngleDegrees)
        {
            float minVertical = (float)Math.Sin(minAngleDegrees * DegreesToRadians);

            if (Math.Abs(direction.Y) >= minVertical)
            {
                return direction;
            }

            float verticalSign = direction.Y >= 0f ? 1f : -1f;
            float horizontalSign = direction.X >= 0f ? 1f : -1f;
            float horizontal = (float)Math.Cos(minAngleDegrees * DegreesToRadians);

            // sin^2 + cos^2 = 1, entao o resultado ja sai normalizado.
            return new Vector2(horizontalSign * horizontal, verticalSign * minVertical);
        }

        /// <summary>Offset normalizado do impacto ao longo da raquete, saturado em -1..1.</summary>
        public static float NormalizedPaddleOffset(float ballX, float paddleX, float paddleHalfWidth)
        {
            if (paddleHalfWidth <= Epsilon)
            {
                return 0f;
            }

            return Math.Clamp((ballX - paddleX) / paddleHalfWidth, -1f, 1f);
        }
    }
}
