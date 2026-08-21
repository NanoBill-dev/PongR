using System;

namespace PongRoyale.Core.Simulation
{
    /// <summary>
    /// Resumo numerico de um estado de partida.
    ///
    /// Serve a dois propositos bem concretos:
    ///
    ///   1. Teste de regressao dourado. Uma sequencia fixa de comandos precisa produzir
    ///      sempre o mesmo hash. Qualquer refatoracao que mude COMPORTAMENTO acende alarme
    ///      na hora, mesmo sem existir um teste especifico para o caso quebrado.
    ///   2. Deteccao de divergencia na FASE 3. Cliente e servidor trocam o hash de vez em
    ///      quando; se divergir, houve dessincronizacao e da para agir antes do jogador
    ///      perceber.
    ///
    /// Usa FNV-1a sobre os BITS dos floats, nao sobre o valor. Comparar bits e o unico jeito
    /// de detectar diferencas minusculas, que sao justamente as que causam divergencia.
    /// </summary>
    public static class MatchStateHash
    {
        private const ulong FnvOffsetBasis = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        public static ulong Compute(MatchState state)
        {
            ulong hash = FnvOffsetBasis;

            Combine(ref hash, state.Tick);
            Combine(ref hash, state.ElapsedSeconds);
            Combine(ref hash, (int)state.Phase);
            Combine(ref hash, (int)state.Result.Outcome);
            Combine(ref hash, state.Result.WinnerSlot);
            Combine(ref hash, (int)state.Result.Reason);

            for (int i = 0; i < state.Balls.Length; i++)
            {
                Combine(ref hash, state.Balls[i].IsActive ? 1 : 0);

                if (!state.Balls[i].IsActive)
                {
                    // Bola inativa nao contribui com posicao: lixo de slot reciclado nao
                    // pode influenciar o hash de um estado logicamente identico.
                    continue;
                }

                Combine(ref hash, state.Balls[i].Position.X);
                Combine(ref hash, state.Balls[i].Position.Y);
                Combine(ref hash, state.Balls[i].Direction.X);
                Combine(ref hash, state.Balls[i].Direction.Y);
                Combine(ref hash, state.Balls[i].BaseSpeed);
                Combine(ref hash, state.Balls[i].SpeedMultiplier);
                Combine(ref hash, state.Balls[i].Damage);
                Combine(ref hash, state.Balls[i].CollisionSequence);
                Combine(ref hash, state.Balls[i].LastHitByPlayer);
                Combine(ref hash, state.Balls[i].ConsecutiveTowerHits);
            }

            for (int i = 0; i < state.Paddles.Length; i++)
            {
                Combine(ref hash, state.Paddles[i].PositionX);
                Combine(ref hash, state.Paddles[i].TargetX);
                Combine(ref hash, state.Paddles[i].VelocityX);
            }

            for (int i = 0; i < state.Towers.Length; i++)
            {
                Combine(ref hash, state.Towers[i].Health);
            }

            for (int i = 0; i < state.Players.Length; i++)
            {
                Combine(ref hash, state.Players[i].Elixir);
                Combine(ref hash, state.Players[i].TowersDestroyed);
            }

            return hash;
        }

        private static void Combine(ref ulong hash, float value)
        {
            Combine(ref hash, BitConverter.SingleToInt32Bits(value));
        }

        private static void Combine(ref ulong hash, int value)
        {
            for (int i = 0; i < 4; i++)
            {
                hash ^= (byte)(value >> (i * 8));
                hash *= FnvPrime;
            }
        }
    }
}
