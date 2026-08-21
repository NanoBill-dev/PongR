using NUnit.Framework;
using PongRoyale.Core.Effects;
using PongRoyale.Core.Events;
using PongRoyale.Core.Simulation;

namespace PongRoyale.Tests.EditMode
{
    /// <summary>
    /// Ciclo de vida dos efeitos de power-up: conceder, contar o tempo, expirar e combinar.
    ///
    /// O resolver nao sabe o que cada efeito FAZ — so quais estao em vigor e por quanto
    /// tempo. E essa separacao que permite construir a aquisicao inteira (drop, coleta,
    /// interceptacao, redencao) antes de existir um unico efeito concreto.
    /// </summary>
    public sealed class EffectResolverTests
    {
        private const ushort Turbina = 1;
        private const ushort Canhao = 2;
        private const ushort Ima = 3;

        private MatchState state;
        private MatchEventQueue events;

        [SetUp]
        public void SetUp()
        {
            state = MatchStateFactory.CreateInitial(TestConfigs.Default(), PlayerSlot.Bottom);
            events = new MatchEventQueue();
        }

        private void Advance(float seconds)
        {
            int ticks = TicksFor(seconds);
            for (int i = 0; i < ticks; i++)
            {
                EffectResolver.Advance(state, MatchConstants.FixedDeltaTime, events);
            }
        }

        private static int TicksFor(float seconds) =>
            (int)System.Math.Ceiling(seconds / MatchConstants.FixedDeltaTime);

        [Test]
        public void NoEffectIsActiveAtTheStartOfTheMatch()
        {
            Assert.IsFalse(EffectResolver.HasAnyActive(state, PlayerSlot.Bottom));
            Assert.IsFalse(EffectResolver.HasAnyActive(state, PlayerSlot.Top));
        }

        [Test]
        public void GrantedEffectBecomesActive()
        {
            EffectResolver.Grant(state, PlayerSlot.Bottom, Turbina, 6f, events);

            Assert.IsTrue(EffectResolver.IsActive(state, PlayerSlot.Bottom, Turbina));
        }

        [Test]
        public void EffectBelongsOnlyToThePlayerWhoCollectedIt()
        {
            EffectResolver.Grant(state, PlayerSlot.Bottom, Turbina, 6f, events);

            Assert.IsFalse(EffectResolver.IsActive(state, PlayerSlot.Top, Turbina));
        }

        [Test]
        public void EffectExpiresAfterItsDuration()
        {
            EffectResolver.Grant(state, PlayerSlot.Bottom, Turbina, 1f, events);

            Advance(0.9f);
            Assert.IsTrue(EffectResolver.IsActive(state, PlayerSlot.Bottom, Turbina), "Expirou cedo demais.");

            Advance(0.2f);
            Assert.IsFalse(EffectResolver.IsActive(state, PlayerSlot.Bottom, Turbina));
        }

        [Test]
        public void GainingAndExpiringAreAnnounced()
        {
            // A apresentacao depende destes eventos para acender e apagar o feedback visual.
            EffectResolver.Grant(state, PlayerSlot.Bottom, Turbina, 0.5f, events);
            Advance(0.6f);

            bool gained = false;
            bool expired = false;

            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].Type == MatchEventType.EffectGained && events[i].Value == Turbina)
                {
                    gained = true;
                }

                if (events[i].Type == MatchEventType.EffectExpired && events[i].Value == Turbina)
                {
                    expired = true;
                }
            }

            Assert.IsTrue(gained);
            Assert.IsTrue(expired);
        }

        [Test]
        public void CollectingASecondEffectKeepsBothActive()
        {
            EffectResolver.Grant(state, PlayerSlot.Bottom, Turbina, 6f, events);
            EffectResolver.Grant(state, PlayerSlot.Bottom, Canhao, 6f, events);

            Assert.IsTrue(EffectResolver.IsActive(state, PlayerSlot.Bottom, Turbina));
            Assert.IsTrue(EffectResolver.IsActive(state, PlayerSlot.Bottom, Canhao));
            Assert.AreEqual(2, EffectResolver.CountActive(state, PlayerSlot.Bottom));
        }

        [Test]
        public void CombiningShortensBothToTheCombinedWindow()
        {
            // A decisao de ritmo do jogo: combinar e forte, porem curto.
            EffectResolver.Grant(state, PlayerSlot.Bottom, Turbina, 6f, events);
            EffectResolver.Grant(state, PlayerSlot.Bottom, Canhao, 6f, events);

            float combined = state.Config.Effects.CombinedDurationSeconds;

            Advance(combined - 0.2f);
            Assert.IsTrue(EffectResolver.IsActive(state, PlayerSlot.Bottom, Turbina));
            Assert.IsTrue(EffectResolver.IsActive(state, PlayerSlot.Bottom, Canhao));

            Advance(0.4f);
            Assert.AreEqual(0, EffectResolver.CountActive(state, PlayerSlot.Bottom), "A janela combinada nao fechou.");
        }

        [Test]
        public void CombiningLateStillGivesTheFullCombinedWindow()
        {
            // Coletar o segundo com o primeiro quase acabando nao encurta a combinacao: a
            // janela e fixa, seja qual for o momento.
            EffectResolver.Grant(state, PlayerSlot.Bottom, Turbina, 6f, events);
            Advance(5.8f);

            EffectResolver.Grant(state, PlayerSlot.Bottom, Canhao, 6f, events);

            float combined = state.Config.Effects.CombinedDurationSeconds;
            Advance(combined - 0.2f);

            Assert.AreEqual(2, EffectResolver.CountActive(state, PlayerSlot.Bottom));
        }

        [Test]
        public void CombiningCanShortenAnEffectThatWouldHaveLastedLonger()
        {
            // O tempo combinado SUBSTITUI o restante, inclusive para menos. E o preco de
            // somar os dois poderes.
            EffectResolver.Grant(state, PlayerSlot.Bottom, Turbina, 10f, events);
            EffectResolver.Grant(state, PlayerSlot.Bottom, Canhao, 10f, events);

            Advance(state.Config.Effects.CombinedDurationSeconds + 0.2f);

            Assert.AreEqual(0, EffectResolver.CountActive(state, PlayerSlot.Bottom));
        }

        [Test]
        public void CollectingTheSameEffectAgainRenewsInsteadOfDuplicating()
        {
            EffectResolver.Grant(state, PlayerSlot.Bottom, Turbina, 6f, events);
            EffectResolver.Grant(state, PlayerSlot.Bottom, Turbina, 6f, events);

            Assert.AreEqual(1, EffectResolver.CountActive(state, PlayerSlot.Bottom), "O efeito duplicou.");
        }

        [Test]
        public void OneSidesEffectsDoNotDisturbTheOther()
        {
            EffectResolver.Grant(state, PlayerSlot.Bottom, Turbina, 6f, events);
            EffectResolver.Grant(state, PlayerSlot.Top, Canhao, 6f, events);

            // Cada lado tem UM efeito, entao nenhum dos dois entrou em modo combinado.
            Advance(state.Config.Effects.CombinedDurationSeconds + 0.5f);

            Assert.IsTrue(EffectResolver.IsActive(state, PlayerSlot.Bottom, Turbina));
            Assert.IsTrue(EffectResolver.IsActive(state, PlayerSlot.Top, Canhao));
        }

        [Test]
        public void GrantingWithoutDurationDoesNothing()
        {
            EffectResolver.Grant(state, PlayerSlot.Bottom, Turbina, 0f, events);

            Assert.IsFalse(EffectResolver.HasAnyActive(state, PlayerSlot.Bottom));
            Assert.AreEqual(0, events.Count);
        }

        [Test]
        public void EmptyIdentifierIsRejected()
        {
            // Protege contra um id nao inicializado virar um efeito fantasma em vigor.
            EffectResolver.Grant(state, PlayerSlot.Bottom, ActiveEffect.None, 6f, events);

            Assert.IsFalse(EffectResolver.HasAnyActive(state, PlayerSlot.Bottom));
        }

        [Test]
        public void SlotsNeverOverflow()
        {
            // Mais efeitos do que cabem: o mais perto de acabar cede o lugar, e o total
            // continua limitado. Nada de crescer array em partida.
            for (ushort id = 1; id <= MatchState.MaxEffectsPerPlayer + 3; id++)
            {
                EffectResolver.Grant(state, PlayerSlot.Bottom, id, 6f, events);
            }

            Assert.LessOrEqual(
                EffectResolver.CountActive(state, PlayerSlot.Bottom),
                MatchState.MaxEffectsPerPlayer);
        }

        [Test]
        public void ExpiredSlotsAreReusedByNewEffects()
        {
            EffectResolver.Grant(state, PlayerSlot.Bottom, Turbina, 0.5f, events);
            Advance(0.6f);

            EffectResolver.Grant(state, PlayerSlot.Bottom, Ima, 6f, events);

            Assert.AreEqual(1, EffectResolver.CountActive(state, PlayerSlot.Bottom));
            Assert.IsTrue(EffectResolver.IsActive(state, PlayerSlot.Bottom, Ima));
            Assert.IsFalse(EffectResolver.IsActive(state, PlayerSlot.Bottom, Turbina));
        }

        [Test]
        public void EffectsChangeTheStateHash()
        {
            // Precisa entrar no hash, senao cliente e servidor divergiriam sem detectar na
            // FASE 3 — e o replay reproduziria a partida sem os power-ups.
            ulong before = MatchStateHash.Compute(state);

            EffectResolver.Grant(state, PlayerSlot.Bottom, Turbina, 6f, events);

            Assert.AreNotEqual(before, MatchStateHash.Compute(state));
        }

        [Test]
        public void ExpiredEffectLeavesNoTraceInTheHash()
        {
            // Slot reciclado nao pode diferenciar dois estados logicamente identicos.
            ulong clean = MatchStateHash.Compute(state);

            EffectResolver.Grant(state, PlayerSlot.Bottom, Turbina, 0.5f, events);
            Advance(0.6f);

            Assert.AreEqual(clean, MatchStateHash.Compute(state));
        }
    }
}
