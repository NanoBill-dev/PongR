using UnityEngine;

namespace PongRoyale.Gameplay.Ball
{
    /// <summary>
    /// Espelha uma bola da simulacao num Transform. A view nao decide nada: le o estado e
    /// desenha. Toda a fisica ja aconteceu no Core antes deste componente rodar.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BallView : MonoBehaviour
    {
        [SerializeField] private MatchRunner runner;
        [SerializeField, Min(0)] private int ballIndex;

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

            // O sprite tem 1 unidade de diametro, entao a escala e o diametro desejado.
            float diameter = runner.Config.Ball.Radius * 2f;
            SpriteFitter.Fit(spriteRenderer, diameter, diameter);
        }

        private void LateUpdate()
        {
            if (runner == null || !runner.IsReady || ballIndex >= runner.Simulation.State.Balls.Length)
            {
                return;
            }

            var ball = runner.Simulation.State.Balls[ballIndex];

            if (spriteRenderer.enabled != ball.IsActive)
            {
                spriteRenderer.enabled = ball.IsActive;
            }

            if (ball.IsActive)
            {
                transform.position = ball.Position.ToWorldPosition();
            }
        }
    }
}
