using PongRoyale.Core.Effects;
using PongRoyale.Core.Simulation;
using PongRoyale.Gameplay;
using UnityEngine;

namespace PongRoyale.Presentation.Hud
{
    /// <summary>
    /// Mostra quais power-ups estao em vigor para um jogador, e por quanto tempo.
    ///
    /// Era o buraco mais grave da interface: sem isso o jogador coleta um drop e nao sabe o
    /// que ganhou nem quando acaba — e um sistema que ninguem percebe nao influencia
    /// decisao nenhuma. Mostrar tambem os efeitos do ADVERSARIO e deliberado: saber que ele
    /// esta com dano dobrado nas suas laterais muda como voce defende.
    ///
    /// A sigla vem do BalanceData e substitui o icone enquanto nao existe arte por carta.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActiveEffectView : MonoBehaviour
    {
        [SerializeField] private MatchRunner runner;
        [SerializeField] private PlayerSlot slot = PlayerSlot.Bottom;

        [Tooltip("Um conjunto por vaga de efeito simultaneo.")]
        [SerializeField] private TextMesh[] labels;

        [SerializeField] private Transform[] timeBars;
        [SerializeField] private SpriteRenderer[] timeBarRenderers;

        [SerializeField] private float barWidth = 0.9f;
        [SerializeField] private float barHeight = 0.09f;

        private void LateUpdate()
        {
            if (runner == null || !runner.IsReady)
            {
                return;
            }

            MatchState state = runner.Simulation.State;
            int first = slot.ToIndex() * MatchState.MaxEffectsPerPlayer;
            int shown = 0;

            for (int i = 0; i < MatchState.MaxEffectsPerPlayer && shown < labels.Length; i++)
            {
                ActiveEffect effect = state.Effects[first + i];
                if (!effect.IsActive)
                {
                    continue;
                }

                labels[shown].gameObject.SetActive(true);
                labels[shown].text = runner.CardAbbreviation(effect.EffectId);

                // O tempo total nao esta no estado: um efeito combinado dura menos que um
                // sozinho. Normalizar pela duracao combinada mantem a barra honesta nos dois
                // casos, porque ela nunca comeca acima do cheio.
                float reference = Mathf.Max(state.Config.Effects.DefaultDurationSeconds, 0.01f);
                float fraction = Mathf.Clamp01(effect.RemainingSeconds / reference);

                timeBarRenderers[shown].enabled = true;
                timeBars[shown].localScale = new Vector3(barWidth * fraction, barHeight, 1f);

                shown++;
            }

            for (int i = shown; i < labels.Length; i++)
            {
                labels[i].gameObject.SetActive(false);
                timeBarRenderers[i].enabled = false;
            }
        }
    }
}
