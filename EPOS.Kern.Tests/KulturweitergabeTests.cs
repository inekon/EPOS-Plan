using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SpeicherEngine;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der BELEG zu <see cref="Kulturweitergabe"/> (Auftrag #232, Anwenderentscheid
    /// „#231: Empfehlung umsetzen" vom 12.09.2026).
    ///
    /// <para><b>Was die Lücke wirklich ist — gemessen, nicht vermutet.</b> Setzt ein
    /// Aufrufer seine Kultur AUSDRÜCKLICH (<c>Thread.CurrentThread.CurrentUICulture = …</c>
    /// oder <c>CultureInfo.CurrentUICulture = …</c>), so trägt .NET sie seit .NET Core
    /// über den <c>ExecutionContext</c> von selbst in jeden <c>Task.Run</c>, in jedes
    /// Arbeitspaket eines <c>Parallel.For</c> und sogar in einen frisch gestarteten
    /// <c>Thread</c> — diese Setzer schreiben intern einen <c>AsyncLocal</c>-Wert. Genau
    /// das hält <see cref="Ein_gepinnter_Aufrufer_vererbt_seine_Kultur_ohnehin"/> fest.
    /// Die Lücke ist der ANDERE Fall: Hat der Aufrufer nichts gesetzt und bezieht seine
    /// Kultur selbst nur aus dem prozessweiten
    /// <c>CultureInfo.DefaultThreadCurrentUICulture</c>, dann gibt es nichts zu erben —
    /// Aufrufer und Arbeitsfäden lesen denselben VERÄNDERLICHEN Wert bei jedem Zugriff
    /// neu, und eine Umschaltung von außen trifft sie einzeln. So entstand Befund #231:
    /// über 50 Testklassen setzen diesen Wert und stellen ihn zurück; wer daneben
    /// rechnet, wechselt mitten im Lauf die Sprache.</para>
    ///
    /// <para><b>Wie diese Fälle den Zustand herstellen — OHNE den Prozess zu stören.</b>
    /// Der naheliegende Versuchsaufbau — den prozessweiten Vorgabewert mitten im Lauf
    /// umschalten — wurde gebaut, gemessen und VERWORFEN: Er reißt genau die Testklassen
    /// mit, die er beschreibt (zwei von drei Läufen unter <c>LANG=en_US.UTF-8</c> ließen
    /// <c>SpeicherOptimierungCtrlTests</c> fallen, ohne ihn vier von vier grün). Hier
    /// wird deshalb NICHTS Prozessweites angefasst. Stattdessen:</para>
    /// <list type="number">
    ///   <item>Der Aufruferfaden (der Testfaden) steht auf <c>de-AT</c> — nur er, über
    ///   <c>CultureInfo.CurrentCulture</c>, zurückgestellt in <c>Dispose</c>. Warum
    ///   ausgerechnet <c>de-AT</c>, steht bei <see cref="De"/>.</item>
    ///   <item>Die Arbeitsfäden gehören einem eigenen Planer
    ///   (<see cref="EnglischeArbeitsfaeden"/>) und stehen fest auf <c>en-US</c> — die
    ///   Lage des Windows-Läufers, nur eben nachprüfbar statt zufällig.</item>
    ///   <item>Der <c>ExecutionContext</c>-Fluss ist im Versuch unterdrückt
    ///   (<see cref="ExecutionContext.SuppressFlow"/>), denn im Befund #231 hatte der
    ///   Aufrufer seine Kultur gar nicht gesetzt — es gab nichts zu erben.</item>
    /// </list>
    ///
    /// <para><b>Warum diese Klasse die <c>Kulturvorrichtung</c> NICHT führt.</b> Die
    /// pinnt auch die zwei PROZESSWEITEN Vorgabewerte; diese Klasse braucht davon
    /// nichts und will den Prozess nicht anfassen. Gepinnt wird nur der eigene Faden.</para>
    /// </summary>
    public sealed class KulturweitergabeTests : IDisposable
    {
        /// <summary>
        /// Die Kultur des Aufrufers in allen Fällen — <b>de-AT und nicht de-DE, mit
        /// Absicht</b>. Über 50 Testklassen setzen <c>de-DE</c> prozessweit und stellen
        /// es zurück; eine Behauptung über <c>de-DE</c> wäre in einem parallelen Lauf
        /// nicht mehr zu unterscheiden von „der Vorgabewert stand gerade zufällig so".
        /// <c>de-AT</c> setzt NIEMAND sonst — steht es auf einem Arbeitsfaden, kann es
        /// nur von der Vorrichtung kommen. Fachlich ändert sich nichts: Das
        /// Dezimaltrennzeichen ist dasselbe Komma, und die Ressourcen fallen über
        /// <c>de</c> auf die neutrale (deutsche) <c>.resx</c> zurück.
        /// </summary>
        private static readonly CultureInfo De = new CultureInfo("de-AT");

        /// <summary>Der deutsche Ressourcentext, an dem die Sprache abzulesen ist.</summary>
        private const string KapazitaetDe = "Nennkapazität C_nom";

        /// <summary>Derselbe Schlüssel auf Englisch (Satellitenressource <c>en-US</c>).</summary>
        private const string KapazitaetEn = "Nominal capacity C_nom";

        private readonly CultureInfo _vorherKultur = CultureInfo.CurrentCulture;
        private readonly CultureInfo _vorherOberflaeche = CultureInfo.CurrentUICulture;

        public KulturweitergabeTests()
        {
            CultureInfo.CurrentCulture = De;
            CultureInfo.CurrentUICulture = De;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            CultureInfo.CurrentCulture = _vorherKultur;
            CultureInfo.CurrentUICulture = _vorherOberflaeche;
        }

        // =====================================================================
        //  Versuchsaufbau
        // =====================================================================

        /// <summary>Eine Aufzeichnung aus EINEM Arbeitspaket.</summary>
        private sealed class Marke
        {
            public int Faden;
            public string Kultur;
            public string Oberflaeche;
            public string Zahl;
            public string Text;
        }

        /// <summary>
        /// Was ein Arbeitspaket von seiner Kultur zu sehen bekommt: Fadenkennung,
        /// beide Kulturnamen, ein formatierte Zahl (Dezimaltrennzeichen) und ein
        /// Ressourcentext (Sprache der Satellitenressource).
        /// </summary>
        private static Marke Aufzeichnen()
        {
            return new Marke
            {
                Faden = Environment.CurrentManagedThreadId,
                Kultur = CultureInfo.CurrentCulture.Name,
                Oberflaeche = CultureInfo.CurrentUICulture.Name,
                Zahl = (1234.5).ToString("0.0", CultureInfo.CurrentCulture),
                Text = WindowsFormsApplication1.MyResource.Resource.OPT_KZ_KAPAZITAET
            };
        }

        /// <summary>
        /// <b>Das Testdoppel der Umgebung:</b> ein Planer mit drei eigenen Fäden, die
        /// fest auf <c>en-US</c> stehen. Er macht den Zufall des CI-Läufers zur
        /// nachprüfbaren Bedingung — ohne einen einzigen prozessweiten Schreibzugriff.
        /// </summary>
        private sealed class EnglischeArbeitsfaeden : TaskScheduler, IDisposable
        {
            private readonly BlockingCollection<Task> _warteschlange = new BlockingCollection<Task>();
            private readonly List<Thread> _faeden = new List<Thread>();

            public EnglischeArbeitsfaeden(int anzahl)
            {
                CultureInfo en = new CultureInfo("en-US");
                for (int i = 0; i < anzahl; i++)
                {
                    Thread faden = new Thread(() =>
                    {
                        // Der Faden steht auf en-US. Traegt eine Aufgabe einen
                        // ExecutionContext, gewinnt DER - genau das prueft
                        // Ein_gepinnter_Aufrufer_vererbt_seine_Kultur_ohnehin.
                        CultureInfo.CurrentCulture = en;
                        CultureInfo.CurrentUICulture = en;
                        foreach (Task aufgabe in _warteschlange.GetConsumingEnumerable())
                            TryExecuteTask(aufgabe);
                    });
                    faden.IsBackground = true;
                    faden.Name = "EnglischerArbeitsfaden" + i;
                    faden.Start();
                    _faeden.Add(faden);
                }
            }

            /// <inheritdoc />
            protected override void QueueTask(Task task) { _warteschlange.Add(task); }

            /// <summary>Nie am Aufruferfaden ausführen — sonst stünde die Arbeit auf de-AT.</summary>
            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) { return false; }

            /// <inheritdoc />
            protected override IEnumerable<Task> GetScheduledTasks() { return _warteschlange.ToArray(); }

            /// <inheritdoc />
            public override int MaximumConcurrencyLevel { get { return _faeden.Count; } }

            /// <inheritdoc />
            public void Dispose()
            {
                _warteschlange.CompleteAdding();
                foreach (Thread f in _faeden) f.Join(TimeSpan.FromSeconds(30));
            }
        }

        /// <summary>
        /// Das Arbeitspaket der Versuche: Es wartet, bis ein ZWEITER Faden angekommen
        /// ist (sonst prüfte der Fall nur einen einzigen), und zeichnet dann auf.
        /// </summary>
        private sealed class Versuch
        {
            private readonly ConcurrentBag<Marke> _marken = new ConcurrentBag<Marke>();
            private readonly ManualResetEventSlim _zweiterDa = new ManualResetEventSlim(false);
            private int _angekommen;

            public void Arbeitspaket(int index)
            {
                if (Interlocked.Increment(ref _angekommen) >= 2) _zweiterDa.Set();
                _zweiterDa.Wait(TimeSpan.FromSeconds(30));
                _marken.Add(Aufzeichnen());
            }

            public List<Marke> Marken { get { return _marken.ToList(); } }
        }

        // =====================================================================
        //  Der Beleg — Parallel.For
        // =====================================================================

        /// <summary>
        /// <b>Der Beleg.</b> Über <see cref="Kulturweitergabe.For"/> rechnet JEDES
        /// Arbeitspaket in der Kultur des Aufrufers — Dezimaltrennzeichen und
        /// Ressourcentext deutsch —, obwohl die Arbeitsfäden auf <c>en-US</c> stehen und
        /// nichts erben. Und es laufen wirklich mehrere Fäden.
        /// </summary>
        [Fact]
        public void Die_Arbeitspakete_tragen_die_Kultur_des_Aufrufers()
        {
            var versuch = new Versuch();

            using (var faeden = new EnglischeArbeitsfaeden(3))
            using (ExecutionContext.SuppressFlow())
            {
                Kulturweitergabe.For(0, 256, Optionen(faeden), versuch.Arbeitspaket);
            }

            List<Marke> marken = versuch.Marken;
            Assert.Equal(256, marken.Count);
            Assert.True(marken.Select(m => m.Faden).Distinct().Count() >= 2,
                        "Es lief nur ein Faden - der Fall prüft dann nicht, was er soll.");

            Assert.All(marken, m =>
            {
                Assert.Equal("de-AT", m.Kultur);
                Assert.Equal("de-AT", m.Oberflaeche);
                Assert.Equal("1234,5", m.Zahl);
                Assert.Equal(KapazitaetDe, m.Text);
            });
        }

        /// <summary>
        /// <b>Die Gegenprobe.</b> Derselbe Versuch mit nacktem <c>Parallel.For</c>
        /// (Testdoppel <see cref="NacktesParallelFor"/>) rechnet ENGLISCH — Punkt statt
        /// Komma, englischer Ressourcentext. Ohne diesen Fall bewiese der Beleg oben
        /// nichts: Er liefe auch dann grün, wenn die Vorrichtung gar nichts täte.
        /// </summary>
        [Fact]
        public void Gegenprobe_Nacktes_ParallelFor_rechnet_in_der_Sprache_des_Arbeitsfadens()
        {
            var versuch = new Versuch();

            using (var faeden = new EnglischeArbeitsfaeden(3))
            using (ExecutionContext.SuppressFlow())
            {
                NacktesParallelFor(0, 256, Optionen(faeden), versuch.Arbeitspaket);
            }

            List<Marke> marken = versuch.Marken;
            Assert.Equal(256, marken.Count);
            Assert.All(marken, m =>
            {
                Assert.Equal("en-US", m.Oberflaeche);
                Assert.Equal("1234.5", m.Zahl);
                Assert.Equal(KapazitaetEn, m.Text);
            });
        }

        /// <summary>
        /// <b>Der gemessene Befund zur Vererbung.</b> Derselbe nackte <c>Parallel.For</c>
        /// auf denselben englischen Arbeitsfäden — nur OHNE
        /// <see cref="ExecutionContext.SuppressFlow"/> — rechnet deutsch: Der gepinnte
        /// Aufrufer vererbt seine Kultur über den <c>ExecutionContext</c> von selbst.
        ///
        /// <para>Dieser Fall hält fest, warum die Gegenprobe oben den Fluss unterdrückt
        /// und warum die Vorrichtung trotzdem nötig ist: Sie greift genau dort, wo es
        /// NICHTS zu erben gibt — beim Aufrufer, dessen Kultur selbst nur aus dem
        /// veränderlichen prozessweiten Vorgabewert stammt (Befund #231).</para>
        /// </summary>
        [Fact]
        public void Ein_gepinnter_Aufrufer_vererbt_seine_Kultur_ohnehin()
        {
            var versuch = new Versuch();

            using (var faeden = new EnglischeArbeitsfaeden(3))
            {
                NacktesParallelFor(0, 256, Optionen(faeden), versuch.Arbeitspaket);
            }

            Assert.All(versuch.Marken, m => Assert.Equal("de-AT", m.Oberflaeche));
        }

        /// <summary>
        /// <b>Das Testdoppel.</b> Wortgleich zur Vorrichtung, nur ohne die
        /// Kulturweitergabe — der Zustand vor Auftrag #232.
        /// </summary>
        private static void NacktesParallelFor(int von, int bis, ParallelOptions optionen, Action<int> arbeit)
        {
            Parallel.For(von, bis, optionen, arbeit);
        }

        private static ParallelOptions Optionen(TaskScheduler planer)
            => new ParallelOptions { TaskScheduler = planer, MaxDegreeOfParallelism = 3 };

        /// <summary>
        /// <see cref="Kulturweitergabe.ForEach{T}"/> tut dasselbe wie
        /// <see cref="Kulturweitergabe.For"/> — die Hülle gibt es für beide Formen,
        /// damit niemand für eine Aufzählung wieder auf das nackte <c>Parallel</c>
        /// ausweicht.
        /// </summary>
        [Fact]
        public void ForEach_traegt_die_Kultur_ebenso()
        {
            var versuch = new Versuch();

            using (var faeden = new EnglischeArbeitsfaeden(3))
            using (ExecutionContext.SuppressFlow())
            {
                Kulturweitergabe.ForEach(Enumerable.Range(0, 128), Optionen(faeden), versuch.Arbeitspaket);
            }

            List<Marke> marken = versuch.Marken;
            Assert.Equal(128, marken.Count);
            Assert.All(marken, m =>
            {
                Assert.Equal("de-AT", m.Oberflaeche);
                Assert.Equal(KapazitaetDe, m.Text);
            });
        }

        // =====================================================================
        //  Der Beleg — die echte Rastersuche
        // =====================================================================

        /// <summary>
        /// Dieselbe Aussage am echten Rechenweg: Die Rastersuche
        /// (<see cref="SpeicherOptimierer"/>, 72 Punkte auf den Fäden des Pools) meldet
        /// ihren Fortschritt aus den RECHNENDEN Fäden. Der Fluss ist unterdrückt — die
        /// Fäden erben also nichts und stünden auf der Umgebung des Prozesses (invariant
        /// oder <c>en-US</c>). Sie melden trotzdem <c>de-AT</c>, und ein <c>de-AT</c> auf
        /// einem Arbeitsfaden kann NUR aus der Vorrichtung stammen — seit #232 parallelt
        /// <c>SpeicherOptimierer</c> über sie.
        /// </summary>
        [Fact]
        public void Die_Rastersuche_meldet_in_der_Sprache_des_Aufrufers()
        {
            var gesammelt = new ConcurrentBag<Marke>();
            int angekommen = 0;
            var zweiterDa = new ManualResetEventSlim(false);

            IProgress<OptimiererFortschritt> melder = new Melder(_ =>
            {
                int n = Interlocked.Increment(ref angekommen);
                if (n >= 2) zweiterDa.Set();
                if (n <= 2) zweiterDa.Wait(TimeSpan.FromSeconds(30));
                gesammelt.Add(Aufzeichnen());
            });

            using (ExecutionContext.SuppressFlow())
            {
                new SpeicherOptimierer().Optimiere(Eingang(), Basis(), Suchraum(), melder,
                                                   CancellationToken.None);
            }

            List<Marke> marken = gesammelt.ToList();
            Assert.Equal(72, marken.Count);
            Assert.True(marken.Select(m => m.Faden).Distinct().Count() >= 2,
                        "Die Rastersuche lief auf einem einzigen Faden.");

            Assert.All(marken, m =>
            {
                Assert.Equal("de-AT", m.Oberflaeche);
                Assert.Equal("1234,5", m.Zahl);
                Assert.Equal(KapazitaetDe, m.Text);
            });
        }

        /// <summary>Ein <see cref="IProgress{T}"/>, der SYNCHRON im rechnenden Faden läuft
        /// — anders als <see cref="Progress{T}"/>, der in den Aufruferkontext zurückspringt.</summary>
        private sealed class Melder : IProgress<OptimiererFortschritt>
        {
            private readonly Action<OptimiererFortschritt> _tun;
            public Melder(Action<OptimiererFortschritt> tun) { _tun = tun; }
            public void Report(OptimiererFortschritt wert) { _tun(wert); }
        }

        private static SpeicherEingang Eingang()
        {
            double[] last = { 0.0, 0.0, 40.0, 40.0 };
            double[] pv = { 40.0, 40.0, 0.0, 0.0 };
            double[] preis = { 20.0, 20.0, 20.0, 20.0 };
            return new SpeicherEingang(last, pv, preis);
        }

        private static SpeicherParameter Basis() => new SpeicherParameter
        {
            CNomKwh = 10.0,
            PKw = 10.0,
            SoCMinKwh = 0.0,
            SoCMaxKwh = 10.0,
            RoundTripWirkungsgrad = 1.0,
            DtH = 0.25,
            VerguetungCtKwh = 5.0,
            CCapEurProKwh = 100.0,
            CPowEurProKw = 50.0,
            IFixEur = 1000.0,
            Kapitalzins = 0.0,
            NutzungsdauerA = 10.0,
            DegradationProA = 0.0,
            CVerEurProKwhZyklus = 0.025
        };

        /// <summary>Zwölf Kapazitäten mal sechs C-Raten, kein Feinraster: 72 Rasterpunkte.</summary>
        private static OptimiererOptionen Suchraum() => new OptimiererOptionen
        {
            CMinKwh = 10.0,
            CMaxKwh = 120.0,
            Stuetzstellen = 12,
            RMin = 0.5,
            RMax = 3.0,
            RSchritt = 0.5,
            Feinraster = false,
            MaxParallel = 4
        };

        // =====================================================================
        //  Der Beleg — Task.Run
        // =====================================================================

        /// <summary>
        /// Dieselbe Aussage für die <c>Task.Run</c>-Hülle, und die Gegenprobe daneben im
        /// selben Fall: Beide Aufgaben starten unter unterdrücktem Fluss, erben also
        /// nichts. Die Hülle liefert die Kultur des Aufrufers (<c>de-AT</c>), das nackte
        /// <c>Task.Run</c> die Umgebung des Prozesses — und die ist nie <c>de-AT</c>.
        /// </summary>
        [Fact]
        public async Task Starten_traegt_die_Kultur_in_den_Arbeitsfaden_das_nackte_TaskRun_nicht()
        {
            Task<Marke> mitHuelle;
            Task<Marke> ohneHuelle;
            using (ExecutionContext.SuppressFlow())
            {
                mitHuelle = Kulturweitergabe.Starten(() => Aufzeichnen());
                ohneHuelle = Task.Run(() => Aufzeichnen());
            }

            Marke mit = await mitHuelle;
            Marke ohne = await ohneHuelle;

            Assert.Equal("de-AT", mit.Kultur);
            Assert.Equal("de-AT", mit.Oberflaeche);
            Assert.Equal("1234,5", mit.Zahl);
            Assert.Equal(KapazitaetDe, mit.Text);

            // Gegenprobe: ohne Huelle steht der Arbeitsfaden auf der Umgebung des
            // Prozesses - invariant (Linux-Laeufer) oder en-US (Windows-Laeufer), nie de-AT.
            Assert.NotEqual("de-AT", ohne.Kultur);
            Assert.NotEqual("de-AT", ohne.Oberflaeche);
        }

        /// <summary>
        /// <see cref="Kulturweitergabe.StartenAsync"/> setzt die Kultur um den
        /// SYNCHRONEN Anlauf herum; über die <c>await</c>-Stellen hinweg trägt .NET sie
        /// von selbst weiter.
        /// </summary>
        [Fact]
        public async Task StartenAsync_traegt_die_Kultur_auch_ueber_ein_await()
        {
            Marke erfasst = null;
            Task aufgabe;
            using (ExecutionContext.SuppressFlow())
            {
                aufgabe = Kulturweitergabe.StartenAsync(async () =>
                {
                    await Task.Yield();
                    erfasst = Aufzeichnen();
                });
            }
            await aufgabe;

            Assert.Equal("de-AT", erfasst.Oberflaeche);
            Assert.Equal("1234,5", erfasst.Zahl);
            Assert.Equal(KapazitaetDe, erfasst.Text);
        }

        // =====================================================================
        //  Der Halter
        // =====================================================================

        /// <summary>
        /// Der Halter stellt den Vorstand des Fadens wieder her — Arbeitsfäden sind
        /// Pool-Fäden, ein stehengelassener Wert wäre genau der Fehler, den die
        /// Vorrichtung verhindern soll.
        /// </summary>
        [Fact]
        public void Der_Halter_stellt_den_Vorstand_des_Fadens_zurueck()
        {
            CultureInfo en = new CultureInfo("en-US");
            Kulturstand stand = Kulturweitergabe.Erfassen();

            Assert.Equal("de-AT", stand.Kultur.Name);
            Assert.Equal("de-AT", stand.Oberflaeche.Name);

            CultureInfo.CurrentCulture = en;
            CultureInfo.CurrentUICulture = en;

            using (stand.Auftragen())
            {
                Assert.Equal("de-AT", CultureInfo.CurrentCulture.Name);
                Assert.Equal("de-AT", CultureInfo.CurrentUICulture.Name);
                Assert.Equal(KapazitaetDe, WindowsFormsApplication1.MyResource.Resource.OPT_KZ_KAPAZITAET);
            }

            Assert.Equal("en-US", CultureInfo.CurrentCulture.Name);
            Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);
            // Dispose dieser Klasse stellt den Faden danach ganz zurueck.
        }

        /// <summary>
        /// <b>Gegenprobe zu den zwei Ressourcentexten:</b> Sie sind wirklich
        /// verschieden, und der Schlüssel liest wirklich <c>CurrentUICulture</c> — sonst
        /// könnte keiner der Fälle oben eine Sprache von der anderen unterscheiden, und
        /// alle liefen grün durch.
        /// </summary>
        [Fact]
        public void Die_zwei_Ressourcentexte_unterscheiden_sich_wirklich()
        {
            Assert.NotEqual(KapazitaetDe, KapazitaetEn);

            Assert.Equal(KapazitaetDe, WindowsFormsApplication1.MyResource.Resource.OPT_KZ_KAPAZITAET);

            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            Assert.Equal(KapazitaetEn, WindowsFormsApplication1.MyResource.Resource.OPT_KZ_KAPAZITAET);
            // Dispose dieser Klasse stellt den Faden danach zurueck.
        }
    }
}
