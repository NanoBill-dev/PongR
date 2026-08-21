using PongRoyale.Core.Simulation;
using UnityEngine;

namespace PongRoyale.Gameplay.Pickups
{
    /// <summary>
    /// Desenha um power-up caindo. Como as bolas, existe um objeto por vaga do estado desde
    /// o inicio da partida — nada de Instantiate no meio do jogo (secao 35).
    ///
    /// A cor identifica QUEM vai coletar. Isso e legibilidade, nao enfeite: o jogador
    /// precisa saber num relance se aquilo que esta caindo e o premio dele — e portanto se
    /// vale abrir a defesa para buscar — ou se e do adversario e ele tem a chance de roubar.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PickupView : MonoBehaviour
    {
        [SerializeField] private MatchRunner runner;
        [SerializeField, Min(0)] private int pickupIndex;

        [SerializeField] private Color bottomColor = new Color(0.30f, 0.80f, 1.00f, 1f);
        [SerializeField] private Color topColor = new Color(1.00f, 0.42f, 0.62f, 1f);

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.enabled = false;
        }

        private void Start()
        {
            if (runner == null || !runner.IsReady)
            {
                return;
            }

            float diameter = runner.Config.Pickup.Radius * 2f;
            transform.localScale = new Vector3(diameter, diameter, 1f);
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
            }

            if (!pickup.IsActive)
            {
                return;
            }

            transform.position = pickup.Position.ToWorldPosition();
            spriteRenderer.color = pickup.Collector == PlayerSlot.Bottom ? bottomColor : topColor;
        }
    }
}
