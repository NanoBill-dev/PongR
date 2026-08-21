using PongRoyale.Core.Simulation;
using PongRoyale.Gameplay;
using UnityEngine;

namespace PongRoyale.Presentation.Hud
{
    /// <summary>
    /// Relogio da partida, mostrando o tempo restante. Fica no espaco vertical que sobra
    /// acima da arena em telas altas — exatamente a margem que o ArenaCameraFitter cria.
    ///
    /// Muda de cor quando entra o ultimo minuto, que na FASE 2 sera tambem o inicio do
    /// elixir dobrado: o jogador precisa perceber a virada sem ler numero.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchClockView : MonoBehaviour
    {
        [SerializeField] private MatchRunner runner;
        [SerializeField] private TextMesh label;

        private static readonly Color NormalColor = new Color(0.85f, 0.88f, 0.95f, 1f);
        private static readonly Color FinalStretchColor = new Color(1f, 0.55f, 0.35f, 1f);

        private void LateUpdate()
        {
            if (runner == null || !runner.IsReady || label == null)
            {
                return;
            }

            MatchState state = runner.Simulation.State;

            float remaining = Mathf.Max(0f, state.Config.Rules.MatchDurationSeconds - state.ElapsedSeconds);
            int totalSeconds = Mathf.CeilToInt(remaining);

            label.text = $"{totalSeconds / 60:0}:{totalSeconds % 60:00}";
            label.color = remaining <= state.Config.Rules.FinalStretchSeconds
                ? FinalStretchColor
                : NormalColor;
        }
    }
}
