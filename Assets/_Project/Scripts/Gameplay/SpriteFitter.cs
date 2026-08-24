using UnityEngine;

namespace PongRoyale.Gameplay
{
    /// <summary>
    /// Ajusta a escala para o sprite ocupar exatamente um tamanho de mundo.
    ///
    /// Existe porque as views nao podem assumir que o sprite mede 1x1 unidade: cada arte tem
    /// resolucao propria. Dividindo pelo tamanho nativo, a colisao e o desenho continuam
    /// coincidindo mesmo se a arte for trocada por outra de resolucao diferente — que e o
    /// que evita o classico "a bola quicou antes de encostar".
    /// </summary>
    public static class SpriteFitter
    {
        public static void Fit(SpriteRenderer renderer, float worldWidth, float worldHeight)
        {
            if (renderer == null || renderer.sprite == null)
            {
                return;
            }

            Vector2 native = renderer.sprite.bounds.size;
            if (native.x <= 0f || native.y <= 0f)
            {
                return;
            }

            renderer.transform.localScale = new Vector3(worldWidth / native.x, worldHeight / native.y, 1f);
        }
    }
}
