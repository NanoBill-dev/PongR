namespace PongRoyale.Core.Paddle
{
    /// <summary>
    /// Estado da raquete. Em retrato (ADR-003) ela so se move no eixo X, entao guardar
    /// um Vector2 seria armazenar um Y que nunca muda. Struct mutavel dentro de array,
    /// pela mesma razao da bola.
    /// </summary>
    public struct PaddleState
    {
        /// <summary>Posicao atual no eixo X, em unidades de mundo.</summary>
        public float PositionX;

        /// <summary>Posicao fixa no eixo Y, definida pela arena no inicio da partida.</summary>
        public float LineY;

        /// <summary>Velocidade atual em X. Usada pela suavizacao e pela validacao antifraude.</summary>
        public float VelocityX;

        /// <summary>Alvo pedido pelo input. A raquete persegue este valor, limitada por MaxSpeed.</summary>
        public float TargetX;

        public static PaddleState Create(float lineY)
        {
            return new PaddleState
            {
                PositionX = 0f,
                LineY = lineY,
                VelocityX = 0f,
                TargetX = 0f
            };
        }
    }
}
