using PongRoyale.Core.Events;
using PongRoyale.Gameplay;
using UnityEngine;

namespace PongRoyale.Presentation.Hud
{
    /// <summary>
    /// Numeros de dano flutuantes, desenhados com o atlas de glifos da arte.
    ///
    /// Le a fila de eventos e mostra o valor REALMENTE aplicado — que com o decaimento de
    /// acertos consecutivos nem sempre e o dano base. E aqui que o decaimento fica visivel.
    ///
    /// Os rotulos sao criados uma vez e reciclados em rodizio: num pinball atras da raquete
    /// isso seria varias alocacoes por segundo, no pior momento possivel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageNumbersView : MonoBehaviour
    {
        [SerializeField] private MatchRunner runner;
        [SerializeField] private SpriteNumber[] labels;

        [Header("Atlas por situacao")]
        [Tooltip("Dano comum contra as torres do adversario.")]
        [SerializeField] private Texture2D normalAtlas;

        [Tooltip("Dano sofrido pelas proprias torres. Vermelho avisa sem precisar ler.")]
        [SerializeField] private Texture2D takenAtlas;

        [Tooltip("Acerto forte: o primeiro de uma investida, sem decaimento.")]
        [SerializeField] private Texture2D criticalAtlas;

        [Header("Animacao")]
        [SerializeField] private float lifetime = 0.9f;
        [SerializeField] private float riseSpeed = 1.4f;

        [Tooltip("Dano a partir do qual o numero usa o atlas de critico.")]
        [SerializeField] private float criticalThreshold = 240f;

        [Tooltip("Lado que o jogador local controla, para saber o que e dano sofrido.")]
        [SerializeField] private Core.Simulation.PlayerSlot localSlot = Core.Simulation.PlayerSlot.Bottom;

        private float[] remainingLifetime;
        private int nextLabel;

        private void Awake()
        {
            remainingLifetime = new float[labels.Length];

            for (int i = 0; i < labels.Length; i++)
            {
                labels[i].Clear();
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
                    Show(matchEvent.Position.ToWorldPosition(), matchEvent.Value, matchEvent.Slot);
                }
            }
        }

        private void Show(Vector3 worldPosition, float damage, Core.Simulation.PlayerSlot towerOwner)
        {
            if (labels.Length == 0)
            {
                return;
            }

            SpriteNumber label = labels[nextLabel];

            // O evento traz o dono da TORRE atingida: se for o lado local, o jogador esta
            // apanhando, e o numero sai vermelho.
            bool damageTaken = towerOwner == localSlot;
            Texture2D atlas = damageTaken
                ? takenAtlas
                : (damage >= criticalThreshold ? criticalAtlas : normalAtlas);

            label.SetAtlas(atlas);
            label.SetTint(Color.white);
            label.transform.position = worldPosition;
            label.Show("-" + Mathf.RoundToInt(damage));

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
                    labels[i].Clear();
                    continue;
                }

                labels[i].transform.position += Vector3.up * (riseSpeed * Time.deltaTime);
                labels[i].SetTint(new Color(1f, 1f, 1f, remainingLifetime[i] / lifetime));
            }
        }
    }
}
