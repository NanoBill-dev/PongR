using UnityEngine;

namespace PongRoyale.Presentation.Hud
{
    /// <summary>
    /// Desenha um numero usando o atlas de glifos da arte, em vez de fonte.
    ///
    /// O atlas tem 14 celulas de 8x10 na ordem "0123456789-+!x". Os recortes sao criados uma
    /// vez em <see cref="Awake"/> com Sprite.Create — assim nao dependemos de fatiar a
    /// textura no importador, que e configuracao facil de perder numa reimportacao.
    ///
    /// Os renderers dos digitos sao criados uma unica vez e reaproveitados: numero de dano
    /// aparece dezenas de vezes por partida e nao pode alocar.
    /// </summary>
    public sealed class SpriteNumber : MonoBehaviour
    {
        private const string GlyphOrder = "0123456789-+!x";
        private const int CellWidth = 8;
        private const int CellHeight = 10;

        [SerializeField] private Texture2D atlas;
        [SerializeField, Min(1)] private int maxCharacters = 6;
        [SerializeField] private float pixelsPerUnit = 32f;
        [SerializeField] private int sortingOrder = 100;

        private Sprite[] glyphs;
        private SpriteRenderer[] slots;

        public float Height => CellHeight / pixelsPerUnit;

        private void Awake()
        {
            BuildGlyphs();
            BuildSlots();
            Clear();
        }

        /// <summary>Troca o atlas em tempo de execucao — e o que muda a cor do numero.</summary>
        public void SetAtlas(Texture2D newAtlas)
        {
            if (newAtlas == null || newAtlas == atlas)
            {
                return;
            }

            atlas = newAtlas;
            BuildGlyphs();
        }

        public void SetTint(Color color)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].color = color;
            }
        }

        public void Show(string text)
        {
            if (slots == null)
            {
                return;
            }

            float glyphWidth = CellWidth / pixelsPerUnit;
            int shown = Mathf.Min(text.Length, slots.Length);

            // Centraliza o conjunto no proprio transform.
            float startX = -(shown - 1) * 0.5f * glyphWidth;

            for (int i = 0; i < slots.Length; i++)
            {
                if (i >= shown)
                {
                    slots[i].enabled = false;
                    continue;
                }

                int glyph = GlyphOrder.IndexOf(text[i]);
                if (glyph < 0)
                {
                    slots[i].enabled = false;
                    continue;
                }

                slots[i].sprite = glyphs[glyph];
                slots[i].enabled = true;
                slots[i].transform.localPosition = new Vector3(startX + i * glyphWidth, 0f, 0f);
            }
        }

        public void Clear()
        {
            if (slots == null)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].enabled = false;
            }
        }

        private void BuildGlyphs()
        {
            if (atlas == null)
            {
                return;
            }

            glyphs = new Sprite[GlyphOrder.Length];

            for (int i = 0; i < GlyphOrder.Length; i++)
            {
                glyphs[i] = Sprite.Create(
                    atlas,
                    new Rect(i * CellWidth, 0f, CellWidth, CellHeight),
                    new Vector2(0.5f, 0.5f),
                    pixelsPerUnit);
            }
        }

        private void BuildSlots()
        {
            slots = new SpriteRenderer[maxCharacters];

            for (int i = 0; i < maxCharacters; i++)
            {
                var host = new GameObject("Glyph_" + i, typeof(SpriteRenderer));
                host.transform.SetParent(transform, worldPositionStays: false);

                slots[i] = host.GetComponent<SpriteRenderer>();
                slots[i].sortingOrder = sortingOrder;
            }
        }
    }
}
