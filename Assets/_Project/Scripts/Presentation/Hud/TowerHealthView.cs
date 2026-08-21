using PongRoyale.Core.Combat;
using PongRoyale.Core.Simulation;
using PongRoyale.Gameplay;
using UnityEngine;

namespace PongRoyale.Presentation.Hud
{
    /// <summary>
    /// Barra e numero de vida de uma torre, ancorados no mundo logo acima dela.
    ///
    /// Ancorar no mundo em vez de num canvas de tela e deliberado: com a arena inteira
    /// visivel, a informacao ao lado da torre e lida sem tirar os olhos da bola. Um painel
    /// no canto obrigaria o jogador a escolher entre olhar a vida e jogar.
    ///
    /// A view nao decide nada: le o estado da simulacao e desenha.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TowerHealthView : MonoBehaviour
    {
        [SerializeField] private MatchRunner runner;
        [SerializeField] private PlayerSlot owner = PlayerSlot.Bottom;
        [SerializeField] private TowerKind kind = TowerKind.King;

        [Header("Partes")]
        [SerializeField] private Transform barFill;
        [SerializeField] private SpriteRenderer barBackground;
        [SerializeField] private SpriteRenderer barFillRenderer;
        [SerializeField] private TextMesh label;

        [Header("Aparencia")]
        [SerializeField] private float barWidth = 1.8f;
        [SerializeField] private float barHeight = 0.22f;
        [SerializeField] private float verticalOffset = 1.15f;

        [Tooltip("Mostrar o numero da vida o tempo todo. Util para calibrar balanceamento; " +
                 "no jogo final provavelmente so a barra fica.")]
        [SerializeField] private bool alwaysShowNumber = true;

        private static readonly Color HealthyColor = new Color(0.35f, 0.85f, 0.45f, 1f);
        private static readonly Color WoundedColor = new Color(0.95f, 0.75f, 0.25f, 1f);
        private static readonly Color CriticalColor = new Color(0.90f, 0.30f, 0.30f, 1f);

        private void Start()
        {
            if (runner == null || !runner.IsReady)
            {
                return;
            }

            var tower = runner.Simulation.State.GetTower(owner, kind);

            // A barra fica acima da torre para o lado de baixo e abaixo para o de cima:
            // sempre voltada para o centro do campo, longe da borda da tela.
            float side = owner == PlayerSlot.Bottom ? 1f : -1f;
            transform.position = tower.Position.ToWorldPosition() + new Vector3(0f, side * verticalOffset, 0f);

            barBackground.transform.localScale = new Vector3(barWidth, barHeight, 1f);
            barFill.localScale = new Vector3(barWidth, barHeight, 1f);

            // Ancora o preenchimento na ponta esquerda, para ele encolher para um lado so.
            barFill.localPosition = new Vector3(-barWidth * 0.5f, 0f, 0f);
        }

        private void LateUpdate()
        {
            if (runner == null || !runner.IsReady)
            {
                return;
            }

            var tower = runner.Simulation.State.GetTower(owner, kind);

            barBackground.enabled = tower.IsAlive;
            barFillRenderer.enabled = tower.IsAlive;
            label.gameObject.SetActive(tower.IsAlive && alwaysShowNumber);

            if (!tower.IsAlive)
            {
                return;
            }

            float fraction = Mathf.Clamp01(tower.HealthFraction);

            // Escala a partir da ancora esquerda: a barra encolhe da direita para a esquerda.
            barFill.localScale = new Vector3(barWidth * fraction, barHeight, 1f);
            barFill.localPosition = new Vector3(-barWidth * 0.5f + barWidth * fraction * 0.5f, 0f, 0f);

            barFillRenderer.color = ColorForFraction(fraction);

            if (alwaysShowNumber)
            {
                label.text = Mathf.CeilToInt(tower.Health).ToString();
            }
        }

        private static Color ColorForFraction(float fraction)
        {
            if (fraction > 0.5f)
            {
                return HealthyColor;
            }

            return fraction > 0.25f ? WoundedColor : CriticalColor;
        }
    }
}
