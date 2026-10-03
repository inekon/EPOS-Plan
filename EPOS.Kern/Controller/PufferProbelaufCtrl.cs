using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace WindowsFormsApplication1
{
    /// <summary>Die Starts eines Erzeugertyps im Lauf (WP, BHKW, Kessel; alle Module des Typs zusammen).</summary>
    /// <param name="Typ">Erzeugertyp (<see cref="ProjektPuffer.TYP_WP"/>, <c>TYP_BHKW</c>, <c>TYP_KESSEL</c>).</param>
    /// <param name="StartsJahr">Starts im Jahr [1/a], wie sie der Lauf zählt.</param>
    /// <param name="StartsHeizperiode">Starts in Stunden mit Heizbedarf &gt; 0 (wie D2), aus dem Jahreswert anteilig.</param>
    /// <param name="StartsJeTag">Starts je Tag der Heizperiode: Starts der Heizperiode ÷ (Heizstunden ÷ 24).</param>
    /// <param name="Rang1">Der Typ steht an Rang 1 der Auslegung.</param>
    /// <param name="AusReihe">
    /// Der Lauf zählt für diesen Typ keine Starts (kein Modul mit Mindestleistung, Welle M4); die Starts sind die
    /// Einschaltflanken der stündlichen Wärme des Typs.
    /// </param>
    public sealed record PufferStartsLauf(int Typ, int StartsJahr, int StartsHeizperiode, double StartsJeTag, bool Rang1,
                                          bool AusReihe = false);

    /// <summary>Füllstand des Puffers in einem Monat (Anteil der nutzbaren Kapazität, 0 … 1).</summary>
    public sealed record PufferFuellstandMonat(int Monat, double Min, double Mittel, double Max);

    /// <summary>
    /// Das Ergebnis eines PROBELAUFS der Jahressimulation mit dem empfohlenen Puffervolumen
    /// (Welle P4b, V13). Es lebt nur im Speicher: nichts davon steht in der Datenbank.
    /// </summary>
    public sealed record PufferProbelaufErgebnis
    {
        public int IdProjekt { get; init; }
        public int? IdPuffer { get; init; }
        /// <summary>Volumen [l], mit dem gerechnet wurde.</summary>
        public double VolumenL { get; init; }
        /// <summary>Zeitpunkt des Laufs (Ortszeit).</summary>
        public DateTime Zeitpunkt { get; init; }
        /// <summary>Rechendauer des Laufs.</summary>
        public TimeSpan Dauer { get; init; }
        /// <summary>Starts je Erzeugertyp; leer ohne Lauf.</summary>
        public IReadOnlyList<PufferStartsLauf> Starts { get; init; } = Array.Empty<PufferStartsLauf>();
        /// <summary>Stunden mit Heizbedarf &gt; 0 (Heizperiode wie D2).</summary>
        public int Heizstunden { get; init; }
        /// <summary>Deckung des Wärmebedarfs im Lauf (0 … 1): 1 − Restwärme ÷ Wärmebedarf.</summary>
        public double? Deckung { get; init; }
        /// <summary>Füllstand des Puffers je Stunde (0 … 1), 8 760 Werte; <c>null</c>, wenn der Puffer nicht im Lauf stand.</summary>
        public double[] Fuellstand { get; init; }
        /// <summary>Füllstand je Monat (min/mittel/max); leer ohne Reihe.</summary>
        public IReadOnlyList<PufferFuellstandMonat> Monate { get; init; } = Array.Empty<PufferFuellstandMonat>();
        /// <summary>Erste Stunde der kältesten Woche (168 h, kleinste mittlere Außentemperatur); -1 ohne Reihe.</summary>
        public int KaeltesteWocheAb { get; init; } = -1;
        /// <summary>Der benannte Grund, wenn der Lauf nicht möglich war oder abbrach; sonst <c>null</c>.</summary>
        public string Fehlertext { get; init; }

        public bool Erfolgreich => Fehlertext == null;
        /// <summary>Die Starts des Rang-1-Erzeugers; <c>null</c>, wenn keiner im Lauf.</summary>
        public PufferStartsLauf Rang1 => Starts.FirstOrDefault(s => s.Rang1);

        public static PufferProbelaufErgebnis MitFehler(int idProjekt, int? idPuffer, string grund)
            => new PufferProbelaufErgebnis { IdProjekt = idProjekt, IdPuffer = idPuffer, Fehlertext = grund ?? "", Zeitpunkt = DateTime.Now };
    }

    /// <summary>
    /// Der PROBELAUF der Pufferauslegung (Welle P4b, V13 „Startzähler-Rückkopplung"): Die
    /// Jahressimulation rechnet das Projekt mit dem empfohlenen Volumen des gewählten Puffers und
    /// liefert die Starts der Erzeuger, die Deckung und die Füllstandsreihe — als Gegenprobe zur
    /// Zweipunktschätzung D2.
    ///
    /// <para><b>Wo die Starts sonst liegen.</b> Die Simulation zählt die Starts je Modul
    /// (<c>Starts_WP</c>, <c>Starts_BHKW</c>, <c>Starts_Spk</c>), schreibt sie aber in keine
    /// Ergebnistabelle; sie leben nur im Lauf. Deshalb rechnet der Probelauf selbst, und das letzte
    /// Ergebnis bleibt je Projekt und Puffer im Speicher (<see cref="Letzter"/>), bis das Programm
    /// endet.</para>
    ///
    /// <para><b>Nichts wird geschrieben.</b> Gefahren wird <see cref="SimulationRunner.Simuliere"/>
    /// (ohne <c>BaueErgebnis</c>/<c>Save</c>); das Volumen ersetzt die Naht
    /// <see cref="WaermesenkeClass.ProbelaufVolumen"/> nur im Ausführungsfluss des Laufs.
    /// <c>Tab_Pufferspeicher</c>, <c>Tab_PufferAuslegung</c> und die Ergebnistabellen bleiben
    /// unberührt; der Rechenweg ist ohne Naht derselbe.</para>
    /// </summary>
    public static class PufferProbelaufCtrl
    {
        /// <summary>Grenze der relativen Abweichung der Starts je Tag (Hinweis <c>PA-STARTS-ABWEICHUNG</c>).</summary>
        public const double ABWEICHUNG_GRENZE = 0.30;

        private static readonly ConcurrentDictionary<(int, int), PufferProbelaufErgebnis> _letzte =
            new ConcurrentDictionary<(int, int), PufferProbelaufErgebnis>();

        private static readonly object _laufsperre = new object();

        /// <summary>Der letzte erfolgreiche Probelauf für Projekt und Puffer; <c>null</c> = kein Lauf.</summary>
        public static PufferProbelaufErgebnis Letzter(int idProjekt, int? idPuffer)
            => _letzte.TryGetValue((idProjekt, idPuffer ?? 0), out PufferProbelaufErgebnis e) ? e : null;

        /// <summary>Vergisst die gemerkten Probeläufe (Tests).</summary>
        public static void Vergessen() => _letzte.Clear();

        /// <summary>
        /// Weichen die Starts je Tag des Laufs um mehr als <see cref="ABWEICHUNG_GRENZE"/> von der
        /// Schätzung ab? Bezug ist die Schätzung; ohne beide Werte oder bei Schätzung 0 und Lauf 0: nein.
        /// </summary>
        public static bool Abweichung(double? auslegung, double? lauf)
        {
            if (!auslegung.HasValue || !lauf.HasValue) return false;
            double a = auslegung.Value, l = lauf.Value;
            if (a <= 0) return l > 0;
            return Math.Abs(l - a) / a > ABWEICHUNG_GRENZE + 1e-12;
        }

        /// <summary>Der Hinweistext zur Abweichung (Ressource <c>PA_STARTS_ABWEICHUNG</c>).</summary>
        public static Textbaustein AbweichungText(double auslegung, double lauf)
            => Textbaustein.T("PA_STARTS_ABWEICHUNG",
                              "Die Jahressimulation zählt {1} Starts je Tag, die Auslegung schätzt {0} – Abweichung über 30 %.",
                              Math.Round(auslegung, 1), Math.Round(lauf, 1));

        /// <summary>Der Erzeugertyp an Rang 1 aus der Vorlage der Auslegung; 0 = keiner der drei.</summary>
        public static int Rang1Typ(PufferVorlage vorlage)
        {
            switch (vorlage)
            {
                case PufferVorlage.WP_MONO:
                case PufferVorlage.WP_BIVALENT: return ProjektPuffer.TYP_WP;
                case PufferVorlage.BHKW: return ProjektPuffer.TYP_BHKW;
                case PufferVorlage.KESSEL:
                case PufferVorlage.FESTBRENNSTOFF: return ProjektPuffer.TYP_KESSEL;
                default: return 0;
            }
        }

        /// <summary>
        /// Fährt den Probelauf für <paramref name="idProjekt"/> mit <paramref name="volumenL"/> am Puffer
        /// <paramref name="idPuffer"/>. Ein neuer, noch nicht angelegter Puffer (<c>null</c>) hat keine
        /// Senkenzuordnung und kann nicht mitrechnen — benannt abgelehnt. Die Heizperiode zählt aus
        /// <paramref name="heizreihe"/> (Stunden mit Heizbedarf &gt; 0, wie D2); ohne Reihe gilt das
        /// ganze Jahr.
        /// </summary>
        public static PufferProbelaufErgebnis Probelauf(int idProjekt, int? idPuffer, double volumenL,
                                                        IReadOnlyList<double> heizreihe, int rang1Typ,
                                                        CancellationToken abbruch = default)
        {
            if (idProjekt <= 0) return PufferProbelaufErgebnis.MitFehler(idProjekt, idPuffer, "");
            if (!idPuffer.HasValue || idPuffer.Value <= 0)
                return PufferProbelaufErgebnis.MitFehler(idProjekt, idPuffer,
                    Textbaustein.T("PAUS_PROBELAUF_NEU", "Der Probelauf braucht einen Puffer im Projekt; einen neuen Puffer zuerst übernehmen.").Aufloesen());
            if (!(volumenL > 0))
                return PufferProbelaufErgebnis.MitFehler(idProjekt, idPuffer,
                    Textbaustein.T("PAUS_PROBELAUF_OHNE_EMPFEHLUNG", "Ohne Empfehlung gibt es keinen Probelauf.").Aufloesen());

            WaermesenkeClass.PufferInfo puffer = WaermesenkeClass.PufferLesen(idPuffer.Value);
            if (puffer == null || puffer.ID_Projekt != idProjekt)
                return PufferProbelaufErgebnis.MitFehler(idProjekt, idPuffer,
                    Textbaustein.T("PAUS_PROBELAUF_PUFFER_FEHLT", "Der Puffer gehört nicht zum Projekt.").Aufloesen());

            string vorpruefung = Vorpruefen(idProjekt);
            if (vorpruefung != null) return PufferProbelaufErgebnis.MitFehler(idProjekt, idPuffer, vorpruefung);

            int volumen = (int)Math.Round(volumenL, MidpointRounding.AwayFromZero);
            var uhr = Stopwatch.StartNew();
            DateTime zeitpunkt = DateTime.Now;
            SimulationRunner runner = new SimulationRunner();
            bool ok;
            string fehler;
            // Ein Lauf zur Zeit: der Kanal des Protokolls und die Engine-Zustände sind prozessweit.
            lock (_laufsperre)
            {
                SimulationProtokoll vorher = SimulationProtokoll.Aktuell;
                try
                {
                    using (WaermesenkeClass.ProbelaufVolumen(idPuffer.Value, volumen))
                        ok = runner.Simuliere(idProjekt, out fehler, null, abbruch);
                }
                finally
                {
                    SimulationProtokoll.Wiederherstellen(vorher);
                }
            }
            uhr.Stop();
            if (!ok)
                return PufferProbelaufErgebnis.MitFehler(idProjekt, idPuffer, string.IsNullOrEmpty(fehler) ? "—" : fehler) with
                {
                    VolumenL = volumen,
                    Dauer = uhr.Elapsed
                };

            SimulationControl sim = runner.sim;
            bool[] heiz = Heizstunden(heizreihe);
            int heizstunden = heiz.Count(h => h);

            double[] fuellstand = null;
            if (sim.speicherRegistry.TryGetValue(idPuffer.Value, out SimulationPufferspeicher sp) && sp != null && sp.Q_max > 0)
                fuellstand = sp.SOC_stuendlich.Select(s => Math.Max(0.0, Math.Min(1.0, s / sp.Q_max))).ToArray();

            double bedarf = runner.simulation_Waermebedarf?.Waermebedarf_Gesamt ?? 0;
            var e = new PufferProbelaufErgebnis
            {
                IdProjekt = idProjekt,
                IdPuffer = idPuffer,
                VolumenL = volumen,
                Zeitpunkt = zeitpunkt,
                Dauer = uhr.Elapsed,
                Starts = StartsAusLauf(sim, heiz, rang1Typ),
                Heizstunden = heizstunden,
                Deckung = bedarf > 0 ? Math.Max(0.0, Math.Min(1.0, 1.0 - sim.RestwaermeMwh / bedarf)) : (double?)null,
                Fuellstand = fuellstand,
                Monate = fuellstand != null ? Monatswerte(fuellstand) : Array.Empty<PufferFuellstandMonat>(),
                KaeltesteWocheAb = KaeltesteWoche(sim.Stundentemperatur)
            };
            _letzte[(idProjekt, idPuffer.Value)] = e;
            return e;
        }

        /// <summary>
        /// Die Vorprüfung des Laufs (<see cref="SimulationLaufCtrl.Vorpruefen"/>): Lesemodus,
        /// Konfiguration, Netzverluste, Klimaregion — der benannte Grund, sonst <c>null</c>.
        /// </summary>
        public static string Vorpruefen(int idProjekt)
        {
            KonfigurationCtrl konfig = new KonfigurationCtrl();
            konfig.ProjektLesen(idProjekt);
            return SimulationLaufCtrl.Vorpruefen(idProjekt, konfig.rows > 0 ? konfig.model : null,
                                                 PufferAuslegungCtrl.Klimaregion(idProjekt));
        }

        /// <summary>Heizstunden (Bedarf &gt; 0) aus der Reihe; ohne Reihe gilt jede Stunde.</summary>
        public static bool[] Heizstunden(IReadOnlyList<double> heizreihe)
        {
            var h = new bool[8760];
            bool mitReihe = heizreihe != null && heizreihe.Count >= 8760;
            for (int i = 0; i < 8760; i++) h[i] = !mitReihe || heizreihe[i] > 0;
            return h;
        }

        /// <summary>
        /// Die Starts je Erzeugertyp aus einem gerechneten Lauf. Das Jahr zählt der Lauf je Modul; die
        /// Heizperiode teilt der Lauf nicht aus. Sie entsteht anteilig: Gewicht jeder Stunde ist eine
        /// Einschaltflanke der stündlichen Wärme des Typs (Wärme &gt; 0 nach einer Stunde ohne), läuft
        /// der Typ ohne Flanke durch, die Laufstunde. Starts der Heizperiode = Jahresstarts × Gewicht
        /// in Heizstunden ÷ Gewicht im Jahr.
        /// </summary>
        public static IReadOnlyList<PufferStartsLauf> StartsAusLauf(SimulationControl sim, bool[] heiz, int rang1Typ)
        {
            var liste = new List<PufferStartsLauf>();
            if (sim == null) return liste;
            heiz ??= Heizstunden(null);
            int heizstunden = heiz.Count(h => h);

            void Typ(int typ, int[] starts, double[] waerme)
            {
                int jahr = starts?.Sum() ?? 0;
                bool lief = waerme != null && waerme.Any(w => w > 0);
                if (jahr <= 0 && !lief && typ != rang1Typ) return;
                int hp;
                bool ausReihe = jahr <= 0 && lief;
                if (ausReihe)
                {
                    // Ohne Mindestleistung zählt der Lauf keine Starts: die Einschaltflanken der Wärmereihe.
                    jahr = Einschaltflanken(waerme, heiz, out hp);
                }
                else
                {
                    double anteil = AnteilHeizperiode(waerme, heiz);
                    hp = (int)Math.Round(jahr * anteil, MidpointRounding.AwayFromZero);
                }
                double jeTag = heizstunden > 0 ? hp / (heizstunden / 24.0) : 0;
                liste.Add(new PufferStartsLauf(typ, jahr, hp, jeTag, typ == rang1Typ, ausReihe));
            }

            Typ(ProjektPuffer.TYP_WP, sim.simulation_wp?.Starts_WP, sim.simulation_wp?.WP_Waermeproduktion_stuendlich);
            Typ(ProjektPuffer.TYP_BHKW, sim.simulation_bhkw?.Starts_BHKW, sim.simulation_bhkw?.waermeproduktion);
            Typ(ProjektPuffer.TYP_KESSEL, sim.simulation_spk?.Starts_Spk, sim.simulation_spk?.Kesselleistung_stuendlich);
            return liste;
        }

        /// <summary>
        /// Einschaltflanken einer Stundenreihe (Wärme &gt; 0 nach einer Stunde ohne; die Stunde vor dem 1. Januar ist
        /// die letzte des Jahres) im Jahr und in Heizstunden (<paramref name="inHeizperiode"/>).
        /// </summary>
        public static int Einschaltflanken(IReadOnlyList<double> waerme, bool[] heiz, out int inHeizperiode)
        {
            inHeizperiode = 0;
            if (waerme == null || waerme.Count == 0) return 0;
            int n = heiz == null ? waerme.Count : Math.Min(waerme.Count, heiz.Length);
            int flanken = 0;
            bool vorher = waerme[n - 1] > 0;
            for (int i = 0; i < n; i++)
            {
                bool an = waerme[i] > 0;
                if (an && !vorher)
                {
                    flanken++;
                    if (heiz == null || heiz[i]) inHeizperiode++;
                }
                vorher = an;
            }
            return flanken;
        }

        /// <summary>Anteil der Einschaltflanken (ersatzweise Laufstunden) in Heizstunden; ohne Reihe 1.</summary>
        public static double AnteilHeizperiode(IReadOnlyList<double> waerme, bool[] heiz)
        {
            if (waerme == null || heiz == null) return 1.0;
            int n = Math.Min(waerme.Count, heiz.Length);
            double flankenGesamt = 0, flankenHeiz = 0, laufGesamt = 0, laufHeiz = 0;
            bool vorher = n > 0 && waerme[n - 1] > 0;
            for (int i = 0; i < n; i++)
            {
                bool an = waerme[i] > 0;
                if (an)
                {
                    laufGesamt++;
                    if (heiz[i]) laufHeiz++;
                    if (!vorher)
                    {
                        flankenGesamt++;
                        if (heiz[i]) flankenHeiz++;
                    }
                }
                vorher = an;
            }
            if (flankenGesamt > 0) return flankenHeiz / flankenGesamt;
            if (laufGesamt > 0) return laufHeiz / laufGesamt;
            return 1.0;
        }

        /// <summary>Füllstand min/mittel/max je Monat (Raster 365 Tage, kein Schaltjahr).</summary>
        public static IReadOnlyList<PufferFuellstandMonat> Monatswerte(IReadOnlyList<double> fuellstand)
        {
            int[] tage = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            var liste = new List<PufferFuellstandMonat>();
            int ab = 0;
            for (int m = 0; m < 12; m++)
            {
                int bis = Math.Min(fuellstand.Count, ab + tage[m] * 24);
                double min = double.MaxValue, max = double.MinValue, summe = 0;
                int n = 0;
                for (int i = ab; i < bis; i++)
                {
                    double v = fuellstand[i];
                    if (v < min) min = v;
                    if (v > max) max = v;
                    summe += v;
                    n++;
                }
                if (n > 0) liste.Add(new PufferFuellstandMonat(m + 1, min, summe / n, max));
                ab = bis;
            }
            return liste;
        }

        /// <summary>Erste Stunde der Woche (168 h) mit der kleinsten mittleren Außentemperatur; -1 ohne Reihe.</summary>
        public static int KaeltesteWoche(IReadOnlyList<double> temperatur)
        {
            if (temperatur == null || temperatur.Count < 168) return -1;
            double summe = 0;
            for (int i = 0; i < 168; i++) summe += temperatur[i];
            double beste = summe;
            int ab = 0;
            for (int i = 168; i < temperatur.Count; i++)
            {
                summe += temperatur[i] - temperatur[i - 168];
                if (summe < beste - 1e-9)
                {
                    beste = summe;
                    ab = i - 167;
                }
            }
            return ab;
        }
    }
}
