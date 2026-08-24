using System.Collections;
using NUnit.Framework;
using PongRoyale.Core.Combat;
using PongRoyale.Core.Pickups;
using PongRoyale.Core.Simulation;
using PongRoyale.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PongRoyale.Tests.PlayMode
{
    /// <summary>
    /// Fiacao da parte visual do drop e da barra de ciclo. O Core ja tem 200 testes provando
    /// que a REGRA esta certa; estes provam que o jogador enxerga o que esta acontecendo.
    /// </summary>
    public sealed class PickupHudPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator LoadMatchScene()
        {
            yield return SceneManager.LoadSceneAsync("Match", LoadSceneMode.Single);
        }

        private static MatchRunner FindRunner()
        {
            var runner = Object.FindFirstObjectByType<MatchRunner>();
            Assert.IsNotNull(runner, "A cena Match precisa ter um MatchRunner.");
            return runner;
        }

        private static GameObject FindChild(string parentPath, string childName)
        {
            var parent = GameObject.Find(parentPath);
            Assert.IsNotNull(parent, $"Objeto ausente na cena: {parentPath}");

            Transform child = parent.transform.Find(childName);
            Assert.IsNotNull(child, $"Filho ausente: {parentPath}/{childName}");

            return child.gameObject;
        }

        [UnityTest]
        public IEnumerator TheDeckReachesTheTowersAsRewards()
        {
            // O deck provisorio do MatchRunner precisa chegar as torres do adversario.
            MatchRunner runner = FindRunner();
            yield return null;

            Assert.AreNotEqual(
                0,
                runner.Simulation.State.GetTower(PlayerSlot.Top, TowerKind.LeftGuard).RewardEffectId,
                "A torre inimiga deveria carregar a carta escolhida pelo jogador de baixo.");
        }

        [UnityTest]
        public IEnumerator NoPassableCorridorBetweenPaddleAndTowers()
        {
            // Le a geometria REALMENTE enviada no BalanceData, e nao a do TestConfigs: e a
            // unica forma de proteger o balanceamento que o jogador recebe.
            //
            // O corredor entre a raquete e as torres tinha exatamente o diametro da bola, e
            // ela passava. Nao era so feio: cada acerto na torre gasta uma carga de defesa, e
            // gastar carga zera a sequencia limpa — com a bola pinballando la atras, os tres
            // ciclos limpos que a redencao exige nunca aconteciam. Um sistema inteiro morto
            // por meia unidade de geometria.
            MatchRunner runner = FindRunner();
            yield return null;

            MatchConfig config = runner.Config;

            float paddleBackFace =
                config.Arena.HalfHeight - config.Arena.PaddleLineOffsetFromEdge + config.Paddle.Thickness * 0.5f;
            float towerRow = config.Arena.HalfHeight - config.Tower.RowOffsetFromEdge;
            float ballDiameter = config.Ball.Radius * 2f;

            float guardGap = (towerRow - config.Tower.GuardHalfSize.Y) - paddleBackFace;
            float kingGap = (towerRow - config.Tower.KingHalfSize.Y) - paddleBackFace;

            Assert.Less(
                guardGap, ballDiameter,
                $"A bola cabe no vao ate as torres laterais ({guardGap:0.00} >= {ballDiameter:0.00}).");
            Assert.Less(
                kingGap, ballDiameter,
                $"A bola cabe no vao ate a Torre Rei ({kingGap:0.00} >= {ballDiameter:0.00}).");
        }

        [UnityTest]
        public IEnumerator PickupViewsExistAndStartHidden()
        {
            yield return null;

            var first = GameObject.Find("Pickups/Pickup_0");
            Assert.IsNotNull(first, "As views de drop precisam existir desde o inicio.");
            Assert.IsFalse(
                first.GetComponent<SpriteRenderer>().enabled,
                "Sem drop em jogo, nenhuma view pode aparecer.");
        }

        [UnityTest]
        public IEnumerator DestroyingAGuardTowerShowsTheFallingDrop()
        {
            MatchRunner runner = FindRunner();
            yield return null;

            PickupResolver.SpawnFromTower(
                runner.Simulation.State,
                MatchState.TowerIndex(PlayerSlot.Top, TowerKind.LeftGuard),
                runner.Simulation.Events);

            yield return null;

            var first = GameObject.Find("Pickups/Pickup_0");
            Assert.IsTrue(first.GetComponent<SpriteRenderer>().enabled, "O drop precisa aparecer.");

            float startY = first.transform.position.y;
            yield return new WaitForSeconds(0.4f);

            Assert.Less(first.transform.position.y, startY, "O drop precisa estar caindo.");
        }

        [UnityTest]
        public IEnumerator ElixirBarFillsOverTime()
        {
            FindRunner();
            yield return null;

            var fill = FindChild("Hud/ElixirCycle", "BarFill");
            float startWidth = fill.transform.localScale.x;

            yield return new WaitForSeconds(0.6f);

            Assert.Greater(fill.transform.localScale.x, startWidth, "A barra de ciclo nao andou.");
        }

        [UnityTest]
        public IEnumerator BothPlayersHaveChargeIndicators()
        {
            // Ver as cargas do adversario e decisao de design: tres acesas do lado dele
            // avisam que ele esta perto da redencao.
            yield return null;

            Assert.IsNotNull(FindChild("Hud/ElixirCycle", "ChargeBottom_0"));
            Assert.IsNotNull(FindChild("Hud/ElixirCycle", "ChargeTop_0"));
            Assert.IsNotNull(FindChild("Hud/ElixirCycle", "ChargeTop_2"));
        }
    }
}
