using System;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Gameplay.Input
{
    /// <summary>
    /// Parametros do bot. Nao entram no MatchConfig de proposito: bot nao e regra de jogo,
    /// e o servidor nunca vai precisar deles.
    /// </summary>
    [Serializable]
    public struct AiSettings
    {
        /// <summary>
        /// Intervalo entre decisoes, em segundos. Modela tempo de reacao: entre uma decisao
        /// e a proxima o bot persegue um alvo desatualizado, exatamente como um humano que
        /// leu a bola um instante atras.
        /// </summary>
        public float ReactionSeconds;

        /// <summary>Erro de mira por unidade de velocidade da bola.</summary>
        public float ErrorPerSpeedUnit;

        /// <summary>Teto do erro, para que uma bola rapida nao torne o bot inutil.</summary>
        public float MaxError;

        public static AiSettings Default => new AiSettings
        {
            ReactionSeconds = 0.12f,
            ErrorPerSpeedUnit = 0.05f,
            MaxError = 1.2f
        };
    }

    /// <summary>
    /// Cerebro do bot, em C# puro para poder ser testado sem entrar em Play Mode.
    ///
    /// Duas escolhas de design que definem a dificuldade:
    ///
    ///   1. O bot NAO preve quiques nas paredes laterais. Ele projeta a bola em linha reta
    ///      ate a linha da raquete. Isso e o que o torna bativel por angulo: uma bola jogada
    ///      na parede engana o bot, e recompensa exatamente a habilidade que o jogo quer
    ///      premiar (secao 38).
    ///   2. O erro cresce com a velocidade da bola. O bot fica mais falho conforme o rali
    ///      esquenta, que e quando o jogador humano tambem falharia.
    ///
    /// A aleatoriedade vem de uma semente explicita, entao o comportamento e reproduzivel
    /// em teste.
    /// </summary>
    public sealed class AiPaddleBrain
    {
        private readonly Random random;

        private float timeSinceDecision;
        private float decidedTargetX;
        private bool hasDecided;

        public AiPaddleBrain(int seed)
        {
            random = new Random(seed);
        }

        public float Decide(MatchState state, PlayerSlot slot, float deltaTime, AiSettings settings)
        {
            timeSinceDecision += deltaTime;

            bool dueForDecision = !hasDecided || timeSinceDecision >= settings.ReactionSeconds;
            if (dueForDecision)
            {
                timeSinceDecision = 0f;
                hasDecided = true;
                decidedTargetX = Aim(state, slot, settings);
            }

            return decidedTargetX;
        }

        private float Aim(MatchState state, PlayerSlot slot, AiSettings settings)
        {
            float lineY = state.GetPaddle(slot).LineY;

            if (!TryFindMostUrgentBall(state, slot, lineY, out int ballIndex, out float timeToArrive))
            {
                // Nenhuma bola vindo: volta para o centro, que e a posicao que cobre mais.
                return 0f;
            }

            var ball = state.Balls[ballIndex];
            var velocity = ball.Velocity;

            float predictedX = ball.Position.X + velocity.X * timeToArrive;
            float error = Math.Min(ball.CurrentSpeed * settings.ErrorPerSpeedUnit, settings.MaxError);
            float offset = (float)(random.NextDouble() * 2.0 - 1.0) * error;

            return predictedX + offset;
        }

        private static bool TryFindMostUrgentBall(
            MatchState state, PlayerSlot slot, float lineY, out int ballIndex, out float timeToArrive)
        {
            ballIndex = -1;
            timeToArrive = float.MaxValue;

            for (int i = 0; i < state.Balls.Length; i++)
            {
                if (!state.Balls[i].IsActive)
                {
                    continue;
                }

                float velocityY = state.Balls[i].Velocity.Y;
                if (Math.Abs(velocityY) < 1e-4f)
                {
                    continue;
                }

                float travelTime = (lineY - state.Balls[i].Position.Y) / velocityY;
                if (travelTime <= 0f)
                {
                    // Bola indo embora deste lado: nao e ameaca.
                    continue;
                }

                if (travelTime < timeToArrive)
                {
                    timeToArrive = travelTime;
                    ballIndex = i;
                }
            }

            return ballIndex >= 0;
        }
    }
}
