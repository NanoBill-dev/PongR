using PongRoyale.Core.Ball;
using PongRoyale.Core.Effects;
using PongRoyale.Core.Events;
using PongRoyale.Core.Paddle;

namespace PongRoyale.Core.Simulation
{
    /// <summary>
    /// A partida inteira, em um objeto. Este e o unico ponto de entrada do Core: Unity, bot
    /// e servidor conversam com ele e com mais nada.
    ///
    /// O passo e FIXO por contrato — <see cref="Tick"/> nao aceita deltaTime. Quem chama
    /// acumula o tempo real e chama Tick quantas vezes couber. Aceitar um deltaTime
    /// variavel deixaria o resultado depender do frame rate do aparelho, o que quebraria
    /// replay, teste reproduzivel e sincronizacao com o servidor de uma vez so.
    ///
    /// ORDEM DAS OPERACOES no tick, e o motivo de cada posicao:
    ///
    ///   1. Comandos      — o input deste tick precisa valer neste tick, nao no proximo.
    ///                      Um tick de atraso na propria raquete e a latencia que mais se
    ///                      sente num jogo de reflexo.
    ///   2. Raquetes      — antes da bola, para que PreviousPositionX esteja correto e a
    ///                      varredura relativa funcione.
    ///   3. Bolas         — varrem contra as raquetes ja atualizadas.
    ///   4. Relogio       — antes de avaliar, para que o resultado seja julgado com o tempo
    ///                      que ESTE tick consumiu. Depois, a partida acabaria um tick tarde.
    ///   5. Resultado     — por ultimo, ja com o dano e o tempo deste tick contabilizados.
    /// </summary>
    public sealed class MatchSimulation
    {
        public MatchSimulation(MatchConfig config, PlayerSlot firstServeToward)
        {
            State = MatchStateFactory.CreateInitial(config, firstServeToward);
            Events = new MatchEventQueue();
        }

        public MatchState State { get; }

        /// <summary>
        /// Eventos produzidos desde a ultima limpeza.
        ///
        /// A simulacao NUNCA limpa esta fila. Quem consome e que chama Clear, uma vez por
        /// frame, depois de desenhar. Se o Tick limpasse, um frame que rodasse varios ticks
        /// perderia os eventos de todos menos o ultimo — e o jogador veria uma torre cair
        /// sem som nem efeito.
        /// </summary>
        public MatchEventQueue Events { get; }

        public bool IsFinished => State.Phase == MatchPhase.Finished;

        /// <summary>
        /// Tira a partida do aquecimento. Antes disso as raquetes ja respondem ao toque,
        /// mas a bola nao sai e o relogio nao anda — o jogador consegue se posicionar
        /// durante a contagem regressiva.
        /// </summary>
        public void Begin()
        {
            if (State.Phase != MatchPhase.WarmUp)
            {
                return;
            }

            State.Phase = MatchPhase.Playing;
            Events.Enqueue(MatchEvent.PhaseChanged(State.Tick, MatchPhase.Playing));
        }

        public void Tick(MatchCommandBuffer commands)
        {
            if (State.Phase == MatchPhase.Finished)
            {
                return;
            }

            ApplyCommands(commands);

            PaddleResolver.Advance(State, MatchConstants.FixedDeltaTime);

            if (State.Phase == MatchPhase.Playing)
            {
                BallResolver.Advance(State, MatchConstants.FixedDeltaTime, Events);

                // Efeitos contam o tempo DEPOIS da fisica: um power-up concedido neste tick
                // vale por ele inteiro, e um que expira so deixa de valer no proximo.
                EffectResolver.Advance(State, MatchConstants.FixedDeltaTime, Events);

                State.ElapsedSeconds += MatchConstants.FixedDeltaTime;
            }

            State.Tick++;

            if (State.Phase == MatchPhase.Playing)
            {
                MatchOutcomeResolver.Evaluate(State, Events);
            }
        }

        private void ApplyCommands(MatchCommandBuffer commands)
        {
            if (commands == null)
            {
                return;
            }

            for (int i = 0; i < commands.Count; i++)
            {
                MatchCommand command = commands[i];

                // Um cliente adulterado pode mandar qualquer byte no slot. Indexar o array
                // com ele derrubaria a simulacao — inclusive a do servidor.
                if (command.Slot.ToIndex() >= MatchConstants.PlayerCount)
                {
                    continue;
                }

                switch (command.Type)
                {
                    case CommandType.PaddleMove:
                        PaddleResolver.SetTarget(
                            ref State.GetPaddle(command.Slot),
                            command.PaddleTargetX,
                            State.Config);
                        break;

                    case CommandType.PlayCard:
                        // FASE 2. Ignorado por enquanto, e de proposito: um comando que a
                        // simulacao ainda nao entende nao pode derrubar nada.
                        break;

                    case CommandType.Emote:
                        // Nao afeta a simulacao. A apresentacao trata pelo proprio canal.
                        break;
                }
            }
        }
    }
}
