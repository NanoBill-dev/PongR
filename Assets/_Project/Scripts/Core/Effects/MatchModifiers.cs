using PongRoyale.Core.Combat;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Core.Effects
{
    /// <summary>
    /// Valores EFETIVOS de configuracao, ja com os power-ups ativos aplicados.
    ///
    /// Os resolvers deixam de ler `state.Config.X` e passam a perguntar aqui. Com isso, uma
    /// carta que so muda um numero vira uma linha de DADO na tabela de modificadores, e nao
    /// um `if` novo espalhado pela fisica — que e o que a secao 16 do prompt mestre proibe.
    ///
    /// Modificadores MULTIPLICAM sobre a base e nunca substituem por valor absoluto. Por
    /// isso um efeito que expira nao precisa restaurar nada: o valor volta sozinho porque a
    /// base nunca foi sobrescrita. E o que mantem o determinismo mesmo com efeitos entrando
    /// e saindo no meio de um rali.
    /// </summary>
    public static class MatchModifiers
    {
        /// <summary>
        /// Multiplicador de um alvo para um jogador. Percorre os efeitos dos DOIS lados:
        /// os do proprio jogador que agem sobre si, e os do adversario marcados como
        /// TargetsOpponent.
        /// </summary>
        public static float For(MatchState state, PlayerSlot affected, ModifierTarget target)
        {
            if (target == ModifierTarget.None)
            {
                return 1f;
            }

            float multiplier = 1f;

            multiplier *= Accumulate(state, affected, target, wantsOpponentTargeting: false);
            multiplier *= Accumulate(state, affected.Opponent(), target, wantsOpponentTargeting: true);

            return multiplier;
        }

        /// <summary>
        /// Velocidade da bola: quem manda e a POSSE, nao a raquete. O efeito vale enquanto a
        /// bola for sua; quando o adversario rebate, ele desliga. E assim que uma carta que
        /// age sobre a bola compartilhada deixa de ser simetrica.
        /// </summary>
        public static float BallSpeed(MatchState state, sbyte ballOwner)
        {
            return ForBallOwner(state, ballOwner, ModifierTarget.BallSpeed);
        }

        public static float BallDamage(MatchState state, sbyte ballOwner)
        {
            return ForBallOwner(state, ballOwner, ModifierTarget.BallDamage);
        }

        public static float BallMaxSpeed(MatchState state, sbyte ballOwner)
        {
            return state.Config.Ball.MaxSpeed * ForBallOwner(state, ballOwner, ModifierTarget.BallMaxSpeed);
        }

        public static float PaddleSweepCarry(MatchState state, PlayerSlot slot)
        {
            return state.Config.Paddle.SweepCarry * For(state, slot, ModifierTarget.PaddleSweepCarry);
        }

        public static float PaddleMaxSpeed(MatchState state, PlayerSlot slot)
        {
            return state.Config.Paddle.MaxSpeed * For(state, slot, ModifierTarget.PaddleMaxSpeed);
        }

        public static float PaddleHalfWidth(MatchState state, PlayerSlot slot)
        {
            return state.Config.Paddle.HalfWidth * For(state, slot, ModifierTarget.PaddleWidth);
        }

        public static float PaddleMaxDeflectionDegrees(MatchState state, PlayerSlot slot)
        {
            return state.Config.Ball.MaxDeflectionFromNormalDegrees
                   * For(state, slot, ModifierTarget.PaddleMaxDeflection);
        }

        /// <summary>
        /// Multiplicador de dano recebido por uma torre. O dono da torre e quem sofre, entao
        /// e o efeito do ADVERSARIO dele que aumenta esse valor.
        /// </summary>
        public static float TowerDamageTaken(MatchState state, PlayerSlot towerOwner, TowerKind kind)
        {
            ModifierTarget target = kind == TowerKind.King
                ? ModifierTarget.KingTowerDamageTaken
                : ModifierTarget.GuardTowerDamageTaken;

            return For(state, towerOwner, target);
        }

        public static float GuardTowerHalfWidth(MatchState state, PlayerSlot towerOwner)
        {
            return state.Config.Tower.GuardHalfSize.X
                   * For(state, towerOwner, ModifierTarget.GuardTowerHalfWidth);
        }

        private static float ForBallOwner(MatchState state, sbyte ballOwner, ModifierTarget target)
        {
            if (ballOwner < 0 || ballOwner >= MatchConstants.PlayerCount)
            {
                // Bola sem dono, logo apos o saque: nenhum efeito de posse vale.
                return 1f;
            }

            return Accumulate(state, (PlayerSlot)ballOwner, target, wantsOpponentTargeting: false);
        }

        private static float Accumulate(
            MatchState state, PlayerSlot effectOwner, ModifierTarget target, bool wantsOpponentTargeting)
        {
            EffectModifier[] catalog = state.Config.Effects.Modifiers;
            if (catalog == null || catalog.Length == 0)
            {
                return 1f;
            }

            float multiplier = 1f;

            for (int i = 0; i < catalog.Length; i++)
            {
                EffectModifier modifier = catalog[i];

                if (modifier.Target != target || modifier.TargetsOpponent != wantsOpponentTargeting)
                {
                    continue;
                }

                if (EffectResolver.IsActive(state, effectOwner, modifier.EffectId))
                {
                    multiplier *= modifier.Multiplier;
                }
            }

            return multiplier;
        }
    }
}
