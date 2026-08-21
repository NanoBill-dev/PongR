using NUnit.Framework;
using PongRoyale.Core.Combat;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Tests.EditMode
{
    public sealed class MatchStateTests
    {
        [Test]
        public void EveryTowerGetsItsOwnIndex()
        {
            var indices = new System.Collections.Generic.HashSet<int>();

            foreach (PlayerSlot slot in new[] { PlayerSlot.Bottom, PlayerSlot.Top })
            {
                foreach (TowerKind kind in new[] { TowerKind.King, TowerKind.LeftGuard, TowerKind.RightGuard })
                {
                    Assert.IsTrue(
                        indices.Add(MatchState.TowerIndex(slot, kind)),
                        $"Indice repetido para {slot}/{kind}: as torres se sobrescreveriam no array.");
                }
            }

            Assert.AreEqual(MatchState.TotalTowers, indices.Count);
        }

        [Test]
        public void InitialStateStartsInWarmUpAndUndecided()
        {
            MatchState state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);

            Assert.AreEqual(MatchPhase.WarmUp, state.Phase);
            Assert.IsFalse(state.Result.IsDecided);
            Assert.AreEqual(0, state.Tick);
        }

        [Test]
        public void TowersAreMirroredOnOppositeSides()
        {
            MatchState state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);

            float bottomKingY = state.GetTower(PlayerSlot.Bottom, TowerKind.King).Position.Y;
            float topKingY = state.GetTower(PlayerSlot.Top, TowerKind.King).Position.Y;

            Assert.Less(bottomKingY, 0f, "A Torre Rei de baixo precisa ficar no lado negativo de Y.");
            Assert.Greater(topKingY, 0f, "A Torre Rei de cima precisa ficar no lado positivo de Y.");
            Assert.AreEqual(bottomKingY, -topKingY, 1e-4f, "Os dois lados precisam ser espelhados.");
        }

        [Test]
        public void KingSitsBetweenTheTwoGuards()
        {
            MatchState state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);

            float leftX = state.GetTower(PlayerSlot.Bottom, TowerKind.LeftGuard).Position.X;
            float kingX = state.GetTower(PlayerSlot.Bottom, TowerKind.King).Position.X;
            float rightX = state.GetTower(PlayerSlot.Bottom, TowerKind.RightGuard).Position.X;

            Assert.Less(leftX, kingX);
            Assert.Less(kingX, rightX);
            Assert.AreEqual(0f, kingX, 1e-4f, "O Rei fica no centro do lado.");
        }

        [Test]
        public void TowersStayInsideTheArena()
        {
            MatchConfig config = TestConfigs.Default();
            MatchState state = MatchStateFactory.CreateInitial(config, PlayerSlot.Bottom);

            for (int i = 0; i < state.Towers.Length; i++)
            {
                var tower = state.Towers[i];
                Assert.LessOrEqual(
                    System.Math.Abs(tower.Position.X) + tower.HalfWidth,
                    config.Arena.HalfWidth + 1e-4f,
                    $"A torre {i} vaza pela lateral da arena.");
                Assert.LessOrEqual(
                    System.Math.Abs(tower.Position.Y) + tower.HalfHeight,
                    config.Arena.HalfHeight + 1e-4f,
                    $"A torre {i} vaza pelo fundo da arena.");
            }
        }

        [Test]
        public void PaddlesSitInFrontOfTheirOwnTowers()
        {
            MatchState state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);

            float paddleY = state.GetPaddle(PlayerSlot.Bottom).LineY;
            float towerY = state.GetTower(PlayerSlot.Bottom, TowerKind.King).Position.Y;

            Assert.Greater(paddleY, towerY, "A raquete precisa ficar a frente das proprias torres.");
            Assert.Less(paddleY, 0f, "E ainda assim no proprio lado do campo.");
        }

        [Test]
        public void MatchStartsWithExactlyOneBall()
        {
            MatchState state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);

            Assert.AreEqual(1, state.CountActiveBalls());
            Assert.AreEqual(1, state.FindFreeBallSlot(), "A segunda vaga de bola precisa estar livre.");
        }

        [Test]
        public void ServeDirectionFollowsTheRequestedSide()
        {
            MatchState towardBottom = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);
            MatchState towardTop = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Top);

            Assert.Less(towardBottom.Balls[0].Direction.Y, 0f);
            Assert.Greater(towardTop.Balls[0].Direction.Y, 0f);
        }

        [Test]
        public void PlayersStartWithoutDefenceAndReceivingCharges()
        {
            // O elixir deixou de ser recurso por jogador e virou metronomo global: ninguem
            // comeca com carga, e os dois comecam habilitados a receber.
            MatchState state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);

            foreach (PlayerSlot slot in new[] { PlayerSlot.Bottom, PlayerSlot.Top })
            {
                Assert.AreEqual(0, state.GetPlayer(slot).DefenseCharges);
                Assert.AreEqual(0, state.GetPlayer(slot).CleanCycles);
                Assert.IsTrue(state.GetPlayer(slot).ReceivesCharges);
            }

            Assert.AreEqual(0, state.ElixirCycleTicks);
            Assert.AreEqual(0, state.CompletedElixirCycles);
            Assert.AreEqual(0, state.PlayedTicks);
        }

        [Test]
        public void GetTowerReturnsAReferenceSoDamageSticks()
        {
            // Este teste protege a decisao de usar structs mutaveis em array. Se algum dia
            // GetTower passar a devolver copia, o dano seria perdido em silencio e todo o
            // sistema de combate falharia sem erro de compilacao.
            MatchState state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);

            state.GetTower(PlayerSlot.Top, TowerKind.King).Health -= 1000f;

            Assert.AreEqual(4000f, state.GetTower(PlayerSlot.Top, TowerKind.King).Health, 1e-4f);
        }

        [Test]
        public void KingIsAliveUntilHealthReachesZero()
        {
            MatchState state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);

            Assert.IsTrue(state.IsKingAlive(PlayerSlot.Top));

            state.GetTower(PlayerSlot.Top, TowerKind.King).Health = 0f;

            Assert.IsFalse(state.IsKingAlive(PlayerSlot.Top));
        }
    }
}
