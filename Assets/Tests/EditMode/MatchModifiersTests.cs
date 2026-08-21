using System.Numerics;
using NUnit.Framework;
using PongRoyale.Core.Ball;
using PongRoyale.Core.Combat;
using PongRoyale.Core.Effects;
using PongRoyale.Core.Events;
using PongRoyale.Core.Paddle;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Tests.EditMode
{
    /// <summary>
    /// A camada de modificadores e as primeiras cartas do conjunto v1.
    ///
    /// Nenhuma dessas cartas tem codigo proprio: sao linhas numa tabela. Estes testes
    /// verificam que a tabela chega mesmo aos resolvers — e, principalmente, que o efeito
    /// SOME sozinho quando expira, sem ninguem precisar restaurar valor nenhum.
    /// </summary>
    public sealed class MatchModifiersTests
    {
        private MatchState state;
        private MatchEventQueue events;

        [SetUp]
        public void SetUp()
        {
            state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);
            events = new MatchEventQueue();
        }

        private void SetPaddleX(PlayerSlot slot, float x)
        {
            ref PaddleState paddle = ref state.GetPaddle(slot);
            paddle.PositionX = x;
            paddle.PreviousPositionX = x;
            paddle.TargetX = x;
        }

        private void Step(int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                BallResolver.Advance(state, MatchConstants.FixedDeltaTime, events);
                state.Tick++;
            }
        }

        private void Grant(PlayerSlot slot, ushort card)
        {
            EffectResolver.Grant(state, slot, card, 6f, events);
        }

        // ---------- a camada ----------

        [Test]
        public void WithoutEffectsEveryMultiplierIsNeutral()
        {
            Assert.AreEqual(1f, MatchModifiers.For(state, PlayerSlot.Bottom, ModifierTarget.PaddleSweepCarry), 1e-4f);
            Assert.AreEqual(1f, MatchModifiers.TowerDamageTaken(state, PlayerSlot.Top, TowerKind.King), 1e-4f);
        }

        [Test]
        public void ExpiringRestoresTheBaseValueWithoutAnyoneRestoringIt()
        {
            // Modificadores MULTIPLICAM sobre a base e nunca sobrescrevem. Por isso nao
            // existe codigo de "desfazer": o valor volta porque a base nunca foi alterada.
            float baseCarry = MatchModifiers.PaddleSweepCarry(state, PlayerSlot.Bottom);

            EffectResolver.Grant(state, PlayerSlot.Bottom, TestConfigs.TestCards.Coice, 0.5f, events);
            Assert.Greater(MatchModifiers.PaddleSweepCarry(state, PlayerSlot.Bottom), baseCarry);

            for (int i = 0; i < 40; i++)
            {
                EffectResolver.Advance(state, MatchConstants.FixedDeltaTime, events);
            }

            Assert.AreEqual(baseCarry, MatchModifiers.PaddleSweepCarry(state, PlayerSlot.Bottom), 1e-4f);
        }

        [Test]
        public void EffectOfOneSideDoesNotLeakToTheOther()
        {
            Grant(PlayerSlot.Bottom, TestConfigs.TestCards.Coice);

            Assert.AreEqual(
                state.Config.Paddle.SweepCarry,
                MatchModifiers.PaddleSweepCarry(state, PlayerSlot.Top),
                1e-4f);
        }

        [Test]
        public void TwoModifiersOnTheSameTargetMultiplyTogether()
        {
            // Importa para a combinacao: dois efeitos ativos compoem, nao competem.
            MatchConfig doubled = TestConfigs.WithModifiers(
                new EffectModifier(10, ModifierTarget.GuardTowerDamageTaken, 2f, true),
                new EffectModifier(11, ModifierTarget.GuardTowerDamageTaken, 1.5f, true));

            state = MatchStateFactory.CreateInitial(doubled, PlayerSlot.Bottom);

            EffectResolver.Grant(state, PlayerSlot.Bottom, 10, 6f, events);
            EffectResolver.Grant(state, PlayerSlot.Bottom, 11, 6f, events);

            Assert.AreEqual(
                3f,
                MatchModifiers.TowerDamageTaken(state, PlayerSlot.Top, TowerKind.LeftGuard),
                1e-4f);
        }

        // ---------- Fundacao Rachada ----------

        [Test]
        public void CrackedFoundationDoublesDamageOnGuardTowers()
        {
            SetPaddleX(PlayerSlot.Bottom, 4f);
            Grant(PlayerSlot.Top, TestConfigs.TestCards.FundacaoRachada);

            float before = state.GetTower(PlayerSlot.Bottom, TowerKind.LeftGuard).Health;
            state.Balls[0] = BallState.Create(new Vector2(-3.2f, -7.0f), new Vector2(0f, -1f), 8f, 250f);

            Step(6);

            float applied = before - state.GetTower(PlayerSlot.Bottom, TowerKind.LeftGuard).Health;
            Assert.AreEqual(500f, applied, 1e-2f);
        }

        [Test]
        public void CrackedFoundationLeavesTheKingAlone()
        {
            SetPaddleX(PlayerSlot.Bottom, 4f);
            Grant(PlayerSlot.Top, TestConfigs.TestCards.FundacaoRachada);

            float before = state.GetTower(PlayerSlot.Bottom, TowerKind.King).Health;
            state.Balls[0] = BallState.Create(new Vector2(0f, -6.9f), new Vector2(0f, -1f), 8f, 250f);

            Step(4);

            Assert.AreEqual(250f, before - state.GetTower(PlayerSlot.Bottom, TowerKind.King).Health, 1e-2f);
        }

        [Test]
        public void CrackedFoundationOnlyHelpsTheSideThatCollectedIt()
        {
            // Quem coleta aumenta o dano nas torres DO ADVERSARIO. Colocar o efeito no dono
            // das torres nao pode castiga-lo.
            SetPaddleX(PlayerSlot.Bottom, 4f);
            Grant(PlayerSlot.Bottom, TestConfigs.TestCards.FundacaoRachada);

            float before = state.GetTower(PlayerSlot.Bottom, TowerKind.LeftGuard).Health;
            state.Balls[0] = BallState.Create(new Vector2(-3.2f, -7.0f), new Vector2(0f, -1f), 8f, 250f);

            Step(6);

            Assert.AreEqual(250f, before - state.GetTower(PlayerSlot.Bottom, TowerKind.LeftGuard).Health, 1e-2f);
        }

        [Test]
        public void DamageDecayStillAppliesOnTopOfTheCard()
        {
            // Nenhuma carta desliga a protecao contra o pinball atras da raquete: o
            // multiplicador da carta e o decaimento se compoem.
            SetPaddleX(PlayerSlot.Bottom, 4f);
            Grant(PlayerSlot.Top, TestConfigs.TestCards.FundacaoRachada);

            float before = state.GetTower(PlayerSlot.Bottom, TowerKind.LeftGuard).Health;
            state.Balls[0] = BallState.Create(new Vector2(-3.2f, -7.0f), new Vector2(0f, -1f), 8f, 250f);
            state.Balls[0].ConsecutiveTowerHits = 1;

            Step(6);

            float applied = before - state.GetTower(PlayerSlot.Bottom, TowerKind.LeftGuard).Health;
            Assert.AreEqual(250f * 0.65f * 2f, applied, 1e-2f);
        }

        // ---------- Coice ----------

        [Test]
        public void KickMakesTheSweepPushTheBallHarder()
        {
            float withoutCard = SweepOutgoingX(useCard: false);
            float withCard = SweepOutgoingX(useCard: true);

            Assert.Greater(withCard, withoutCard);
        }

        private float SweepOutgoingX(bool useCard)
        {
            state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);
            events = new MatchEventQueue();

            if (useCard)
            {
                Grant(PlayerSlot.Bottom, TestConfigs.TestCards.Coice);
            }

            ref PaddleState paddle = ref state.GetPaddle(PlayerSlot.Bottom);
            paddle.PositionX = 0f;
            paddle.PreviousPositionX = 0f;
            paddle.TargetX = 0f;
            paddle.VelocityX = 18f;

            float lineY = paddle.LineY;
            state.Balls[0] = BallState.Create(new Vector2(0f, lineY + 0.6f), new Vector2(0f, -1f), 8f, 250f);

            Step(10);
            return state.Balls[0].Direction.X;
        }

        // ---------- Precisao ----------

        [Test]
        public void PrecisionNarrowsTheOutgoingAngle()
        {
            float wide = EdgeHitOutgoingX(useCard: false);
            float narrow = EdgeHitOutgoingX(useCard: true);

            Assert.Less(narrow, wide, "Com Precisao o mesmo impacto precisa sair mais reto.");
            Assert.Greater(narrow, 0f, "Mas ainda precisa desviar para o lado do impacto.");
        }

        private float EdgeHitOutgoingX(bool useCard)
        {
            state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);
            events = new MatchEventQueue();

            if (useCard)
            {
                Grant(PlayerSlot.Bottom, TestConfigs.TestCards.Precisao);
            }

            // Raquete deslocada para a esquerda: a bola toca a metade direita dela.
            SetPaddleX(PlayerSlot.Bottom, -1f);
            float lineY = state.GetPaddle(PlayerSlot.Bottom).LineY;
            state.Balls[0] = BallState.Create(new Vector2(0f, lineY + 0.6f), new Vector2(0f, -1f), 8f, 250f);

            Step(10);
            return state.Balls[0].Direction.X;
        }

        // ---------- Lodo ----------

        [Test]
        public void SlimeSlowsTheOpponentPaddleOnly()
        {
            Grant(PlayerSlot.Bottom, TestConfigs.TestCards.Lodo);

            float slowed = MatchModifiers.PaddleMaxSpeed(state, PlayerSlot.Top);
            float untouched = MatchModifiers.PaddleMaxSpeed(state, PlayerSlot.Bottom);

            Assert.AreEqual(state.Config.Paddle.MaxSpeed * 0.75f, slowed, 1e-3f);
            Assert.AreEqual(state.Config.Paddle.MaxSpeed, untouched, 1e-3f);
        }

        [Test]
        public void SlimeActuallyLimitsMovementInTheSimulation()
        {
            Grant(PlayerSlot.Bottom, TestConfigs.TestCards.Lodo);

            PaddleResolver.SetTarget(ref state.GetPaddle(PlayerSlot.Top), 3.8f, state.Config);
            PaddleResolver.Advance(state, MatchConstants.FixedDeltaTime);

            float travelled = state.GetPaddle(PlayerSlot.Top).PositionX;
            float slowedBudget = state.Config.Paddle.MaxSpeed * 0.75f * MatchConstants.FixedDeltaTime;

            Assert.LessOrEqual(travelled, slowedBudget + 1e-4f);
        }

        // ---------- posse ----------

        [Test]
        public void BallEffectsNeedPossessionToApply()
        {
            // Logo apos o saque a bola nao tem dono, entao nenhum efeito de posse vale.
            MatchConfig withBallCard = TestConfigs.WithModifiers(
                new EffectModifier(20, ModifierTarget.BallSpeed, 1.4f, false));

            state = MatchStateFactory.CreateInitial(withBallCard, PlayerSlot.Bottom);
            EffectResolver.Grant(state, PlayerSlot.Bottom, 20, 6f, events);

            Assert.AreEqual(1f, MatchModifiers.BallSpeed(state, BallState.NoPlayer), 1e-4f);
            Assert.AreEqual(1.4f, MatchModifiers.BallSpeed(state, (sbyte)PlayerSlot.Bottom), 1e-4f);
            Assert.AreEqual(1f, MatchModifiers.BallSpeed(state, (sbyte)PlayerSlot.Top), 1e-4f);
        }
    }
}
