using PongRoyale.Core.Simulation;
using UnityEngine;

namespace PongRoyale.Gameplay.Pickups
{
    /// <summary>
    /// Desenha um power-up caindo. Como as bolas, existe um objeto por vaga do estado desde
    /// o inicio da partida — nada de Instantiate no meio do jogo (secao 35).
    ///
    /// A cor identifica QUEM vai coletar e a sigla identifica O QUE e. Isso e legibilidade,
    /// nao enfeite: o jogador precisa saber num relance se vale abrir a defesa para buscar
    /// aquilo, e o adversario precisa decidir se queima a interceptacao unica sabendo o que
    /// esta em jogo.
    ///
    /// A sigla substitui o icone enquanto nao existe arte por carta.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PickupView : MonoBehaviour
    {
        [SerializeField] private MatchRunner runner;
        [SerializeField, Min(0)] private int pickupIndex;
        [SerializeField] private TextMesh label;

        [SerializeField] private Color bottomColor = new Color(0.30f, 0.80f, 1.00f, 1f);
        [SerializeField] private Color topColor = new Color(1.00f, 0.42f, 0.62f, 1f);

        private SpriteRenderer spriteRenderer;
        private ushort shownEffectId;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.enabled = false;
            SetLabelVisible(false);
        }

        private void Start()
        {
            if (runner == null || !runner.IsReady)
            {
                return;
            }

            float diameter = runner.Config.Pickup.Radius * 2f;
            SpriteFitter.Fit(spriteRenderer, diameter, diameter);
        }

        private void LateUpdate()
        {
            if (runner == null || !runner.IsReady || pickupIndex >= runner.Simulation.State.Pickups.Length)
            {
                return;
            }

            var pickup = runner.Simulation.State.Pickups[pickupIndex];

            if (spriteRenderer.enabled != pickup.IsActive)
            {
                spriteRenderer.enabled = pickup.IsActive;
                SetLabelVisible(pickup.IsActive);
            }

            if (!pickup.IsActive)
            {
                return;
            }

            transform.position = pickup.Position.ToWorldPosition();
            spriteRenderer.color = pickup.Collector == PlayerSlot.Bottom ? bottomColor : topColor;

            // A sigla so e consultada quando a carta muda: buscar no BalanceData a cada
            // frame seria varredura de array por drop, por quadro, sem necessidade.
            if (label != null && shownEffectId != pickup.EffectId)
            {
                shownEffectId = pickup.EffectId;
                label.text = runner.CardAbbreviation(pickup.EffectId);
            }
        }

        private void SetLabelVisible(bool visible)
        {
            if (label != null)
            {
                label.gameObject.SetActive(visible);
            }
        }
    }
}
