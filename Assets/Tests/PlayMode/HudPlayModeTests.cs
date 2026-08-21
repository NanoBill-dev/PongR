using System.Collections;
using NUnit.Framework;
using PongRoyale.Core.Ball;
using PongRoyale.Core.Combat;
using PongRoyale.Core.Simulation;
using PongRoyale.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PongRoyale.Tests.PlayMode
{
    /// <summary>
    /// A HUD mostrando os numeros certos. Sao verificacoes de FIACAO e de leitura: o Core ja
    /// tem 138 testes provando que a regra esta correta, e estes provam que o jogador
    /// enxerga o que de fato aconteceu.
    ///
    /// Vale especialmente para o dano: se a HUD mostrasse o dano base enquanto a torre perde
    /// o dano decrescente, o balanceamento seria calibrado com numero errado.
    /// </summary>
    public sealed class HudPlayModeTests
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

        /// <summary>Busca por caminho, incluindo objetos desativados.</summary>
        private static GameObject FindChild(string parentPath, string childName)
        {
            var parent = GameObject.Find(parentPath);
            Assert.IsNotNull(parent, $"Objeto ausente na cena: {parentPath}");

            Transform child = parent.transform.Find(childName);
            Assert.IsNotNull(child, $"Filho ausente: {parentPath}/{childName}");

            return child.gameObject;
        }

        private static void ClearPaddlesFromTheWay(MatchRunner runner)
        {
            foreach (PlayerSlot slot in new[] { PlayerSlot.Bottom, PlayerSlot.Top })
            {
                ref var paddle = ref runner.Simulation.State.GetPaddle(slot);
                paddle.PositionX = -3.8f;
                paddle.PreviousPositionX = -3.8f;
                paddle.TargetX = -3.8f;
            }
        }

        [UnityTest]
        public IEnumerator TowerHealthLabelShowsTheCurrentHealth()
        {
            MatchRunner runner = FindRunner();
            yield return null;

            var label = FindChild("Hud/TowerHealth_Bottom_King", "Label").GetComponent<TextMesh>();
            float health = runner.Simulation.State.GetTower(PlayerSlot.Bottom, TowerKind.King).Health;

            Assert.AreEqual(Mathf.CeilToInt(health).ToString(), label.text);
        }

        [UnityTest]
        public IEnumerator TowerHealthLabelFollowsDamage()
        {
            MatchRunner runner = FindRunner();
            yield return null;

            runner.Simulation.State.GetTower(PlayerSlot.Bottom, TowerKind.King).Health = 1234f;
            yield return null;

            var label = FindChild("Hud/TowerHealth_Bottom_King", "Label").GetComponent<TextMesh>();

            Assert.AreEqual("1234", label.text);
        }

        [UnityTest]
        public IEnumerator DestroyedTowerHidesItsBar()
        {
            MatchRunner runner = FindRunner();
            yield return null;

            runner.Simulation.State.GetTower(PlayerSlot.Top, TowerKind.LeftGuard).Health = 0f;
            yield return null;

            var background = FindChild("Hud/TowerHealth_Top_LeftGuard", "Background").GetComponent<SpriteRenderer>();

            Assert.IsFalse(background.enabled, "Torre destruida nao pode continuar mostrando barra.");
        }

        [UnityTest]
        public IEnumerator ClockStartsAtTheFullMatchDuration()
        {
            MatchRunner runner = FindRunner();
            yield return null;

            var label = FindChild("Hud/MatchClock", "Label").GetComponent<TextMesh>();
            int totalSeconds = Mathf.CeilToInt(runner.Config.Rules.MatchDurationSeconds);

            Assert.AreEqual($"{totalSeconds / 60:0}:{totalSeconds % 60:00}", label.text);
        }

        [UnityTest]
        public IEnumerator ClockCountsDown()
        {
            MatchRunner runner = FindRunner();
            yield return null;

            var label = FindChild("Hud/MatchClock", "Label").GetComponent<TextMesh>();
            string atStart = label.text;

            runner.Simulation.State.ElapsedSeconds = 5f;
            yield return null;

            Assert.AreNotEqual(atStart, label.text);
        }

        [UnityTest]
        public IEnumerator DamageNumberAppearsWhenATowerIsHit()
        {
            // O numero precisa aparecer e trazer o dano REALMENTE aplicado.
            MatchRunner runner = FindRunner();
            yield return null;

            ClearPaddlesFromTheWay(runner);
            runner.Simulation.State.Balls[0] = BallState.Create(
                new System.Numerics.Vector2(0f, -6.9f),
                new System.Numerics.Vector2(0f, -1f),
                speed: 8f,
                damage: 250f);

            yield return new WaitForSeconds(0.3f);

            bool anyVisible = false;
            var host = GameObject.Find("Hud/DamageNumbers");

            for (int i = 0; i < host.transform.childCount; i++)
            {
                GameObject child = host.transform.GetChild(i).gameObject;
                if (child.activeSelf)
                {
                    anyVisible = true;
                    Assert.AreEqual("-250", child.GetComponent<TextMesh>().text);
                }
            }

            Assert.IsTrue(anyVisible, "Nenhum numero de dano apareceu apos a torre ser atingida.");
        }

        [UnityTest]
        public IEnumerator ResultPanelStaysHiddenDuringThePlay()
        {
            FindRunner();
            yield return new WaitForSeconds(0.2f);

            GameObject label = FindChild("Hud/MatchResult", "Label");

            Assert.IsFalse(label.activeSelf, "O painel de resultado nao pode aparecer com a partida em andamento.");
        }

        [UnityTest]
        public IEnumerator ResultPanelAnnouncesTheWinnerAndTheReason()
        {
            MatchRunner runner = FindRunner();
            yield return null;

            runner.Simulation.State.GetTower(PlayerSlot.Top, TowerKind.King).Health = 0f;
            yield return new WaitForSeconds(0.2f);

            var label = FindChild("Hud/MatchResult", "Label").GetComponent<TextMesh>();

            Assert.IsTrue(label.gameObject.activeSelf, "O painel precisa aparecer quando a partida acaba.");
            StringAssert.Contains("VITORIA", label.text);
            StringAssert.Contains("Torre Rei", label.text);
        }
    }
}
