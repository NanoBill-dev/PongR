using PongRoyale.Core.Events;
using PongRoyale.Gameplay;
using UnityEngine;

namespace PongRoyale.Presentation.Hud
{
    /// <summary>
    /// Numeros de dano flutuantes. Le a fila de eventos da simulacao e mostra o valor
    /// REALMENTE aplicado — que com o decaimento de acertos consecutivos nem sempre e o
    /// dano base. E aqui que o decaimento fica visivel: 250, depois 162, depois 106.
    ///
    /// Os rotulos sao criados uma unica vez e reciclados em rodizio. Nada de Instantiate no
    /// meio da partida (secao 35): num pinball atras da raquete isso seria varias alocacoes
    /// por segundo, justamente no momento de maior tensao.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageNumbersView : MonoBehaviour
    {
        [SerializeField] private MatchRunner runner;
        [SerializeField] private TextMesh[] labels;

        [Header("Animacao")]
        [SerializeField] private float lifetime = 0.9f;
        [SerializeField] private float riseSpeed = 1.4f;

        private static readonly Color DamageColor = new Color(1f, 0.85f, 0.35f, 1f);

        private float[] remainingLifetime;
        private int nextLabel;

        private void Awake()
        {
            remainingLifetime = new float[labels.Length];

            for (int i = 0; i < labels.Length; i++)
            {
                labels[i].gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            // LateUpdate porque o MatchRunner limpa a fila no inicio do Update: ler antes
            // pegaria os eventos do frame anterior.
            if (runner != null && runner.IsReady)
            {
                ConsumeEvents(runner.Simulation.Events);
            }

            AnimateActiveLabels();
        }

        private void ConsumeEvents(MatchEventQueue events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                MatchEvent matchEvent = events[i];

                if (matchEvent.Type == MatchEventType.TowerDamaged)
                {
                    Show(matchEvent.Position.ToWorldPosition(), matchEvent.Value);
                }
            }
        }

        private void Show(Vector3 worldPosition, float damage)
        {
            if (labels.Length == 0)
            {
                return;
            }

            // Rodizio: o rotulo mais antigo e reaproveitado. Com muitos acertos seguidos o
            // primeiro numero some antes da hora, o que e preferivel a alocar mais.
            TextMesh label = labels[nextLabel];

            label.transform.position = worldPosition;
            label.text = $"-{Mathf.RoundToInt(damage)}";
            label.color = DamageColor;
            label.gameObject.SetActive(true);

            remainingLifetime[nextLabel] = lifetime;
            nextLabel = (nextLabel + 1) % labels.Length;
        }

        private void AnimateActiveLabels()
        {
            for (int i = 0; i < labels.Length; i++)
            {
                if (remainingLifetime[i] <= 0f)
                {
                    continue;
                }

                remainingLifetime[i] -= Time.deltaTime;

                if (remainingLifetime[i] <= 0f)
                {
                    labels[i].gameObject.SetActive(false);
                    continue;
                }

                labels[i].transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

                float fade = remainingLifetime[i] / lifetime;
                labels[i].color = new Color(DamageColor.r, DamageColor.g, DamageColor.b, fade);
            }
        }
    }
}
