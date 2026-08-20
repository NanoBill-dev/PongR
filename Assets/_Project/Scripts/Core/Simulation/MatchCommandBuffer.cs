using System;

namespace PongRoyale.Core.Simulation
{
    /// <summary>
    /// Comandos a aplicar num tick. Espelha o MatchEventQueue de proposito: o Core tem uma
    /// fila de entrada e uma de saida, ambas com array proprio que cresce ate o pico e nao
    /// realoca mais.
    ///
    /// Na FASE 3 e aqui que a camada de rede deposita os comandos recebidos do adversario,
    /// lado a lado com os comandos locais. A simulacao nao distingue a origem.
    /// </summary>
    public sealed class MatchCommandBuffer
    {
        private const int DefaultCapacity = 8;

        private MatchCommand[] commands;

        public MatchCommandBuffer(int capacity = DefaultCapacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "A capacidade precisa ser positiva.");
            }

            commands = new MatchCommand[capacity];
            Count = 0;
        }

        public int Count { get; private set; }

        public int Capacity => commands.Length;

        public MatchCommand this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return commands[index];
            }
        }

        public void Add(in MatchCommand command)
        {
            if (Count == commands.Length)
            {
                Array.Resize(ref commands, commands.Length * 2);
            }

            commands[Count] = command;
            Count++;
        }

        public void Clear()
        {
            Count = 0;
        }
    }
}
