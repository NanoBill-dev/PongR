using PongRoyale.Core.Simulation;
using PongRoyale.Gameplay;
using UnityEngine;

namespace PongRoyale.Presentation.Hud
{
    /// <summary>
    /// Painel de fim de partida. Antes dele o jogo simplesmente congelava sem dizer nada,
    /// e nem dava para saber qual criterio de desempate tinha decidido.
    ///
    /// Mostra o motivo junto com o resultado de proposito: numa vitoria por vida somada, o
    /// jogador precisa entender POR QUE ganhou, senao a regra parece arbitraria.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchResultView : MonoBehaviour
    {
        [SerializeField] private MatchRunner runner;
        [SerializeField] private TextMesh label;
        [SerializeField] private SpriteRenderer backdrop;

        [Tooltip("Lado que o jogador local controla. Define o que e vitoria e o que e derrota.")]
        [SerializeField] private PlayerSlot localSlot = PlayerSlot.Bottom;

        private static readonly Color VictoryColor = new Color(0.45f, 0.95f, 0.55f, 1f);
        private static readonly Color DefeatColor = new Color(0.95f, 0.40f, 0.40f, 1f);
        private static readonly Color DrawColor = new Color(0.85f, 0.85f, 0.90f, 1f);

        private bool shown;

        private void Awake()
        {
            SetVisible(false);
        }

        private void LateUpdate()
        {
            if (runner == null || !runner.IsReady || shown)
            {
                return;
            }

            MatchResult result = runner.Simulation.State.Result;

            if (!result.IsDecided)
            {
                return;
            }

            shown = true;
            SetVisible(true);

            label.text = BuildText(result);
            label.color = ColorFor(result);
        }

        private string BuildText(MatchResult result)
        {
            string headline = result.Outcome switch
            {
                MatchOutcome.Draw => "EMPATE",
                MatchOutcome.Victory when result.WinnerSlot == (sbyte)localSlot => "VITORIA",
                MatchOutcome.Victory => "DERROTA",
                _ => string.Empty
            };

            return $"{headline}\n{ReasonText(result.Reason)}";
        }

        private static string ReasonText(MatchEndReason reason) => reason switch
        {
            MatchEndReason.KingTowerDestroyed => "Torre Rei destruida",
            MatchEndReason.TiebreakResolved => "Decidido no desempate",
            MatchEndReason.TimeExpired => "Tempo esgotado",
            MatchEndReason.OpponentForfeited => "Adversario desistiu",
            _ => string.Empty
        };

        private Color ColorFor(MatchResult result)
        {
            if (result.Outcome == MatchOutcome.Draw)
            {
                return DrawColor;
            }

            return result.WinnerSlot == (sbyte)localSlot ? VictoryColor : DefeatColor;
        }

        private void SetVisible(bool visible)
        {
            if (label != null)
            {
                label.gameObject.SetActive(visible);
            }

            if (backdrop != null)
            {
                backdrop.enabled = visible;
            }
        }
    }
}
