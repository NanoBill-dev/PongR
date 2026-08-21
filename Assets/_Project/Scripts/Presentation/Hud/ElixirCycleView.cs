using PongRoyale.Core.Economy;
using PongRoyale.Core.Simulation;
using PongRoyale.Gameplay;
using UnityEngine;

namespace PongRoyale.Presentation.Hud
{
    /// <summary>
    /// A barra de ciclo na divisao dos lados, com os indicadores de carga dos DOIS
    /// jogadores.
    ///
    /// A barra e UMA SO porque o metronomo e global — os dois jogadores recebem a batida no
    /// mesmo instante, entao nao existe divergencia possivel entre o que cada um ve.
    ///
    /// Mostrar as cargas do adversario e decisao de design, nao descuido: ver os tres
    /// diamantes dele acesos avisa que ele esta perto da redencao, e muda como voce ataca.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ElixirCycleView : MonoBehaviour
    {
        [SerializeField] private MatchRunner runner;

        [Header("Barra")]
        [SerializeField] private Transform barFill;
        [SerializeField] private float barWidth = 8f;
        [SerializeField] private float barHeight = 0.18f;

        [Header("Cargas")]
        [SerializeField] private SpriteRenderer[] bottomCharges;
        [SerializeField] private SpriteRenderer[] topCharges;

        private static readonly Color ChargeReadyColor = new Color(0.55f, 0.85f, 1f, 1f);
        private static readonly Color ChargeEmptyColor = new Color(0.25f, 0.27f, 0.34f, 0.6f);
        private static readonly Color BerserkColor = new Color(0.95f, 0.35f, 0.30f, 0.85f);

        private void Start()
        {
            if (barFill != null)
            {
                barFill.localScale = new Vector3(0f, barHeight, 1f);
            }
        }

        private void LateUpdate()
        {
            if (runner == null || !runner.IsReady)
            {
                return;
            }

            MatchState state = runner.Simulation.State;

            UpdateBar(state);
            UpdateCharges(state, PlayerSlot.Bottom, bottomCharges);
            UpdateCharges(state, PlayerSlot.Top, topCharges);
        }

        private void UpdateBar(MatchState state)
        {
            if (barFill == null)
            {
                return;
            }

            float progress = Mathf.Clamp01(ElixirResolver.CycleProgress(state));
            float width = barWidth * progress;

            // Cresce a partir da ponta esquerda, e nao do centro: enchimento de barra e lido
            // como progresso quando avanca numa direcao so.
            barFill.localScale = new Vector3(width, barHeight, 1f);
            barFill.localPosition = new Vector3(-barWidth * 0.5f + width * 0.5f, 0f, 0f);
        }

        private void UpdateCharges(MatchState state, PlayerSlot slot, SpriteRenderer[] indicators)
        {
            if (indicators == null)
            {
                return;
            }

            PlayerState player = state.GetPlayer(slot);

            for (int i = 0; i < indicators.Length; i++)
            {
                if (indicators[i] == null)
                {
                    continue;
                }

                bool filled = i < player.DefenseCharges;

                // Berserk pinta os indicadores de vermelho apagado: o jogador nao so esta
                // sem carga, ele nao vai receber mais nenhuma.
                indicators[i].color = filled
                    ? ChargeReadyColor
                    : (player.ReceivesCharges ? ChargeEmptyColor : BerserkColor);
            }
        }
    }
}
