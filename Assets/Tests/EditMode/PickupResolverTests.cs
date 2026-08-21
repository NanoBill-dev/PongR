using System.Numerics;
using NUnit.Framework;
using PongRoyale.Core.Ball;
using PongRoyale.Core.Combat;
using PongRoyale.Core.Effects;
using PongRoyale.Core.Events;
using PongRoyale.Core.Paddle;
using PongRoyale.Core.Pickups;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Tests.EditMode
{
    /// <summary>
    /// A queda, a coleta e a interceptacao dos power-ups.
    ///
    /// E a peca central do sistema, e o motivo e um so: coletar exige a RAQUETE, a mesma
    /// que esta defendendo. Destruir a torre nao entrega o premio, entrega uma ESCOLHA.
    /// </summary>
    public sealed class PickupResolverTests
    {
        private const ushort CardLeft = 3;
        private const ushort CardRight = 4;

        private MatchState state;
        private MatchEventQueue events;

        /// <summary>Deck: o jogador de baixo pos cartas nas duas laterais do de cima.</summary>
        private static MatchLoadout Loadout =>
            new MatchLoadout(CardLeft, CardRight, 0, 0);

        [SetUp]
        public void SetUp()
        {
            state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom, Loadout);
            events = new MatchEventQueue();
        }

        private void Advance(float seconds)
        {
            int ticks = (int)System.Math.Round(seconds * MatchConstants.SimulationTicksPerSecond);
            for (int i = 0; i < ticks; i++)
            {
                PickupResolver.Advance(state, MatchConstants.FixedDeltaTime, events);
            }
        }

        private void SetPaddleX(PlayerSlot slot, float x)
        {
            ref PaddleState paddle = ref state.GetPaddle(slot);
            paddle.PositionX = x;
            paddle.PreviousPositionX = x;
            paddle.TargetX = x;
        }

        private bool HasEvent(MatchEventType type)
        {
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].Type == type)
                {
                    return true;
                }
            }

            return false;
        }

        // ---------- atribuicao ----------

        [Test]
        public void CardsAreAssignedToTheOpponentGuardTowers()
        {
            // A carta que o jogador de baixo escolheu fica na torre DE CIMA, e e para ele
            // que o drop vai cair.
            Assert.AreEqual(CardLeft, state.GetTower(PlayerSlot.Top, TowerKind.LeftGuard).RewardEffectId);
            Assert.AreEqual(CardRight, state.GetTower(PlayerSlot.Top, TowerKind.RightGuard).RewardEffectId);
        }

        [Test]
        public void KingTowersCarryNoReward()
        {
            Assert.AreEqual(0, state.GetTower(PlayerSlot.Top, TowerKind.King).RewardEffectId);
            Assert.AreEqual(0, state.GetTower(PlayerSlot.Bottom, TowerKind.King).RewardEffectId);
        }

        // ---------- nascimento ----------

        [Test]
        public void DestroyingAGuardTowerDropsItsCard()
        {
            PickupResolver.SpawnFromTower(
                state, MatchState.TowerIndex(PlayerSlot.Top, TowerKind.LeftGuard), events);

            Assert.AreEqual(1, PickupResolver.CountActive(state));
            Assert.IsTrue(HasEvent(MatchEventType.PickupSpawned));
        }

        [Test]
        public void ATowerWithoutACardDropsNothing()
        {
            PickupResolver.SpawnFromTower(
                state, MatchState.TowerIndex(PlayerSlot.Bottom, TowerKind.LeftGuard), events);

            Assert.AreEqual(0, PickupResolver.CountActive(state));
        }

        [Test]
        public void TheDropFallsTowardWhoeverChoseTheCard()
        {
            PickupResolver.SpawnFromTower(
                state, MatchState.TowerIndex(PlayerSlot.Top, TowerKind.LeftGuard), events);

            float startY = state.Pickups[0].Position.Y;
            Advance(0.5f);

            Assert.AreEqual(PlayerSlot.Bottom, state.Pickups[0].Collector);
            Assert.Less(state.Pickups[0].Position.Y, startY, "O drop precisa descer rumo a quem escolheu.");
        }

        [Test]
        public void AnOwnGoalStillHandsThePrizeToTheOpponent()
        {
            // Regra dura de proposito: a carta esta atribuida AQUELA TORRE, nao a quem a
            // derrubou. Derrubar a propria lateral entrega o premio ao adversario.
            var mirrored = new MatchLoadout(0, 0, CardLeft, 0);
            state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom, mirrored);
            events = new MatchEventQueue();

            PickupResolver.SpawnFromTower(
                state, MatchState.TowerIndex(PlayerSlot.Bottom, TowerKind.LeftGuard), events);

            Assert.AreEqual(PlayerSlot.Top, state.Pickups[0].Collector);
        }

        // ---------- coleta ----------

        [Test]
        public void ThePaddleCollectsTheDropAndTheEffectStarts()
        {
            SetPaddleX(PlayerSlot.Bottom, -3.2f);
            SetPaddleX(PlayerSlot.Top, 4f);

            PickupResolver.SpawnFromTower(
                state, MatchState.TowerIndex(PlayerSlot.Top, TowerKind.LeftGuard), events);

            Advance(5f);

            Assert.AreEqual(0, PickupResolver.CountActive(state), "O drop deveria ter sido coletado.");
            Assert.IsTrue(EffectResolver.IsActive(state, PlayerSlot.Bottom, CardLeft));
            Assert.IsTrue(HasEvent(MatchEventType.PickupCollected));
        }

        [Test]
        public void MissingTheDropLosesItAndArmsTheRedemption()
        {
            // Raquetes longe do caminho: ninguem pega.
            SetPaddleX(PlayerSlot.Bottom, 3.8f);
            SetPaddleX(PlayerSlot.Top, 3.8f);

            PickupResolver.SpawnFromTower(
                state, MatchState.TowerIndex(PlayerSlot.Top, TowerKind.LeftGuard), events);

            Advance(6f);

            Assert.AreEqual(0, PickupResolver.CountActive(state));
            Assert.IsFalse(EffectResolver.IsActive(state, PlayerSlot.Bottom, CardLeft));
            Assert.IsTrue(HasEvent(MatchEventType.PickupLost));
            Assert.IsTrue(
                PickupResolver.HasLostDrop(state, PlayerSlot.Bottom),
                "Perder por nao coletar e o que habilita a redencao.");
        }

        [Test]
        public void TheDropDiesAtTheArenaEdgeAndNotAtTheScreenEdge()
        {
            // Criterio de MUNDO: a area visivel muda com o aparelho, e no online os dois
            // lados discordariam sobre quando o drop sumiu.
            SetPaddleX(PlayerSlot.Bottom, 3.8f);
            SetPaddleX(PlayerSlot.Top, 3.8f);

            PickupResolver.SpawnFromTower(
                state, MatchState.TowerIndex(PlayerSlot.Top, TowerKind.LeftGuard), events);

            Advance(2f);
            Assert.AreEqual(1, PickupResolver.CountActive(state), "Ainda dentro da arena.");

            Advance(4f);
            Assert.AreEqual(0, PickupResolver.CountActive(state));
        }

        // ---------- interceptacao ----------

        [Test]
        public void TheOpponentCanStealTheDropOnTheWay()
        {
            // A raquete do adversario esta ANTES no caminho, entao a chance dele vem primeiro.
            SetPaddleX(PlayerSlot.Top, -3.2f);
            SetPaddleX(PlayerSlot.Bottom, -3.2f);

            PickupResolver.SpawnFromTower(
                state, MatchState.TowerIndex(PlayerSlot.Top, TowerKind.LeftGuard), events);

            Advance(5f);

            Assert.IsTrue(HasEvent(MatchEventType.PickupIntercepted));
            Assert.IsFalse(EffectResolver.IsActive(state, PlayerSlot.Bottom, CardLeft));
            Assert.IsFalse(state.GetPlayer(PlayerSlot.Top).HasInterceptionAvailable);
        }

        [Test]
        public void AnInterceptedDropDoesNotArmTheRedemption()
        {
            // A redencao existe para dar segunda chance a um erro PROPRIO, nao para desfazer
            // uma jogada do adversario.
            SetPaddleX(PlayerSlot.Top, -3.2f);
            SetPaddleX(PlayerSlot.Bottom, -3.2f);

            PickupResolver.SpawnFromTower(
                state, MatchState.TowerIndex(PlayerSlot.Top, TowerKind.LeftGuard), events);

            Advance(5f);

            Assert.IsFalse(PickupResolver.HasLostDrop(state, PlayerSlot.Bottom));
        }

        [Test]
        public void InterceptionIsSpentAndTheSecondDropGoesThrough()
        {
            // Uma por partida. E o que obriga a decidir sob pressao, sem saber o que esta
            // caindo, e impede que um veterano zere a economia ofensiva do outro lado.
            SetPaddleX(PlayerSlot.Top, -3.2f);
            SetPaddleX(PlayerSlot.Bottom, -3.2f);

            PickupResolver.SpawnFromTower(
                state, MatchState.TowerIndex(PlayerSlot.Top, TowerKind.LeftGuard), events);
            Advance(5f);
            events.Clear();

            // Segundo drop, mesmo caminho.
            PickupResolver.Spawn(
                state, new Vector2(-3.2f, 8f), CardRight, PlayerSlot.Bottom, canBeIntercepted: true, events);
            Advance(5f);

            Assert.IsFalse(HasEvent(MatchEventType.PickupIntercepted), "A interceptacao ja tinha sido gasta.");
            Assert.IsTrue(EffectResolver.IsActive(state, PlayerSlot.Bottom, CardRight));
        }

        [Test]
        public void ADropMarkedUnstealableIgnoresTheInterception()
        {
            // E o caso do drop de redencao: comprado com defesa impecavel, nao pode ser
            // roubado nem com a interceptacao disponivel.
            SetPaddleX(PlayerSlot.Top, -3.2f);
            SetPaddleX(PlayerSlot.Bottom, -3.2f);

            PickupResolver.Spawn(
                state, new Vector2(-3.2f, 8f), CardLeft, PlayerSlot.Bottom, canBeIntercepted: false, events);

            Advance(5f);

            Assert.IsFalse(HasEvent(MatchEventType.PickupIntercepted));
            Assert.IsTrue(EffectResolver.IsActive(state, PlayerSlot.Bottom, CardLeft));
            Assert.IsTrue(state.GetPlayer(PlayerSlot.Top).HasInterceptionAvailable, "Nao pode nem gastar a chance.");
        }

        [Test]
        public void MissingSidewaysDoesNotCollect()
        {
            // A coleta e posicional: passar longe em X nao pega, mesmo cruzando a linha.
            SetPaddleX(PlayerSlot.Bottom, 3.8f);
            SetPaddleX(PlayerSlot.Top, 3.8f);

            PickupResolver.Spawn(
                state, new Vector2(-3.2f, 8f), CardLeft, PlayerSlot.Bottom, canBeIntercepted: false, events);

            Advance(6f);

            Assert.IsFalse(EffectResolver.IsActive(state, PlayerSlot.Bottom, CardLeft));
            Assert.IsTrue(HasEvent(MatchEventType.PickupLost));
        }

        // ---------- integracao com a partida ----------

        [Test]
        public void DestroyingTheTowerWithTheBallProducesTheDrop()
        {
            // O caminho completo: a bola derruba a torre e o power-up nasce dali.
            SetPaddleX(PlayerSlot.Top, -3.8f);
            SetPaddleX(PlayerSlot.Bottom, -3.8f);

            state.GetTower(PlayerSlot.Top, TowerKind.RightGuard).Health = 100f;
            state.Balls[0] = BallState.Create(new Vector2(3.2f, 7.0f), new Vector2(0f, 1f), 8f, 250f);

            for (int i = 0; i < 8; i++)
            {
                BallResolver.Advance(state, MatchConstants.FixedDeltaTime, events);
                state.Tick++;
            }

            Assert.IsFalse(state.GetTower(PlayerSlot.Top, TowerKind.RightGuard).IsAlive);
            Assert.AreEqual(1, PickupResolver.CountActive(state));
            Assert.AreEqual(CardRight, state.Pickups[0].EffectId);
        }

        [Test]
        public void PickupsChangeTheStateHash()
        {
            ulong before = MatchStateHash.Compute(state);

            PickupResolver.SpawnFromTower(
                state, MatchState.TowerIndex(PlayerSlot.Top, TowerKind.LeftGuard), events);

            Assert.AreNotEqual(before, MatchStateHash.Compute(state));
        }
    }
}
