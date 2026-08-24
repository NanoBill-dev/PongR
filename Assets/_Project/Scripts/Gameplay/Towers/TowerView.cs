using PongRoyale.Core.Combat;
using PongRoyale.Core.Simulation;
using UnityEngine;

namespace PongRoyale.Gameplay.Towers
{
    /// <summary>
    /// Espelha uma torre. Placeholder sem barra de vida: a vida aparece como transparencia,
    /// so para dar leitura visual do dano ate a HUD existir na FASE 2.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TowerView : MonoBehaviour
    {
        private const float MinimumVisibleAlpha = 0.25f;

        [SerializeField] private MatchRunner runner;
        [SerializeField] private PlayerSlot owner = PlayerSlot.Bottom;
        [SerializeField] private TowerKind kind = TowerKind.King;
        [SerializeField] private Color baseColor = Color.white;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (runner == null || !runner.IsReady)
            {
                return;
            }

            var tower = runner.Simulation.State.GetTower(owner, kind);

            transform.position = tower.Position.ToWorldPosition();
            SpriteFitter.Fit(spriteRenderer, tower.HalfWidth * 2f, tower.HalfHeight * 2f);
        }

        private void LateUpdate()
        {
            if (runner == null || !runner.IsReady)
            {
                return;
            }

            var tower = runner.Simulation.State.GetTower(owner, kind);

            if (spriteRenderer.enabled != tower.IsAlive)
            {
                spriteRenderer.enabled = tower.IsAlive;
            }

            if (!tower.IsAlive)
            {
                return;
            }

            float alpha = Mathf.Lerp(MinimumVisibleAlpha, 1f, tower.HealthFraction);
            spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }
    }
}
