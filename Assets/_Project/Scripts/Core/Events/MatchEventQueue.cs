using System;

namespace PongRoyale.Core.Events
{
    /// <summary>
    /// Fila de eventos produzida pela simulacao e drenada pela apresentacao a cada frame.
    ///
    /// Usa um array proprio em vez de List&lt;MatchEvent&gt; ou Queue&lt;T&gt; por dois motivos:
    /// o buffer cresce ate o pico e nunca mais aloca (zero GC em regime, secao 35), e a
    /// leitura por indice devolve os eventos na ordem exata em que ocorreram, o que
    /// importa quando um mesmo tick produz rebatida, dano e destruicao de torre.
    /// </summary>
    public sealed class MatchEventQueue
    {
        private const int DefaultCapacity = 32;

        private MatchEvent[] events;

        public MatchEventQueue(int capacity = DefaultCapacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "A capacidade precisa ser positiva.");
            }

            events = new MatchEvent[capacity];
            Count = 0;
        }

        public int Count { get; private set; }

        public int Capacity => events.Length;

        public MatchEvent this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return events[index];
            }
        }

        public void Enqueue(in MatchEvent matchEvent)
        {
            if (Count == events.Length)
            {
                Array.Resize(ref events, events.Length * 2);
            }

            events[Count] = matchEvent;
            Count++;
        }

        /// <summary>
        /// Esvazia a fila mantendo o buffer alocado. Chamado pela apresentacao depois de
        /// consumir os eventos do frame.
        /// </summary>
        public void Clear()
        {
            Count = 0;
        }
    }
}
