using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Woher Aufbauten und U-Werte beim Weg „IFC + Projektdatei“ kommen</b> (Anwenderentscheid vom 08.10.2026): Schlägt die
    /// <see cref="Standpruefung"/> an, ist die Wahl <see cref="Offen"/>, bis der Anwender entscheidet; sonst gilt
    /// <see cref="Ifc"/> (die Rangfolge nach U-Abgleich, <see cref="SqprojAufbauwahl"/>).
    /// </summary>
    internal enum Aufbauquelle
    {
        /// <summary>Noch nicht gewählt — Übernehmen und Speichern sind gesperrt.</summary>
        Offen = 0,
        /// <summary>Aufbau und U je Bauteil aus der Projektdatei (über die GUID); Bauteile ohne Gegenstück behalten den Stand der IFC.</summary>
        Projektdatei = 1,
        /// <summary>Der Stand der IFC mit der Rangfolge nach U-Abgleich (heutiger Weg).</summary>
        Ifc = 2,
    }

    /// <summary>Ein Anzeichen dafür, dass IFC und Projektdatei aus verschiedenen Projektständen stammen.</summary>
    internal enum Standanzeichen
    {
        /// <summary>(a) Die Projektdatei ist eine Kopie, angelegt nach dem Modellstand der IFC (Journal <c>DBL:COPY</c> jünger als <c>StampEdit</c>).</summary>
        KopieNachModellstand,
        /// <summary>(b) Das Baujahr des Gebäudes weicht zwischen IFC und Projektdatei ab.</summary>
        BaujahrAbweichend,
        /// <summary>(c) Die gezeichnete Dicke passt bei mehr als der Hälfte der abweichenden opaken Bauteile nicht zur Schichtsumme.</summary>
        DickePasstNicht,
    }

    /// <summary>Ein Beispiel eines abweichenden Bauteils: Name, Art, Bruttofläche, beide U und beide Aufbaunamen (leer = keiner).</summary>
    internal sealed record StandpruefungBeispiel(string Bauteil, Bauteilart Art, double FlaecheM2, double UIfc, double UProjektdatei,
                                                 string AufbauIfc, string AufbauProjektdatei);

    /// <summary>Je Bauteilart: verglichene und abweichende Bauteile, der Median des U auf beiden Seiten [W/(m²K)].</summary>
    internal sealed record StandpruefungArt(Bauteilart Art, int Verglichen, int Abweichend, double MedianUIfc, double MedianUProjektdatei);

    /// <summary>
    /// <b>Die Zuordnung IFC-Bauteil → Hüllflächen der Projektdatei</b>: über die Eigenschaft <c>GUID</c>
    /// (<see cref="AbbildBauteil.HottcadGuid"/>) auf die Level-3-<c>GId</c>, Ausweich die dekodierte <c>GlobalId</c>
    /// (<see cref="SqprojRaumabgleich.IfcKennung"/>). Gemeinsam für <see cref="SqprojAufbauwahl"/> und <see cref="Standpruefung"/>.
    /// </summary>
    internal sealed class SqprojBauteilschluessel
    {
        private readonly Dictionary<string, List<SqprojHuellflaeche>> _jeGid;
        private readonly Dictionary<string, List<SqprojHuellflaeche>> _jeGlobalId = new Dictionary<string, List<SqprojHuellflaeche>>(StringComparer.Ordinal);

        internal SqprojBauteilschluessel(SqprojAbbild projekt)
        {
            _jeGid = projekt.Huellflaechen.Where(h => h.Gid != null).GroupBy(h => h.Gid, StringComparer.OrdinalIgnoreCase)
                                          .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            foreach (SqprojHuellflaeche h in projekt.Huellflaechen)
                if (SqprojRaumabgleich.IfcKennung(h.Gid) is string k)
                {
                    if (!_jeGlobalId.TryGetValue(k, out List<SqprojHuellflaeche> l)) _jeGlobalId[k] = l = new List<SqprojHuellflaeche>();
                    l.Add(h);
                }
        }

        /// <summary>Die Hüllflächen eines IFC-Bauteils; <c>null</c> = ohne Gegenstück. <paramref name="ueberGuid"/>: über die Eigenschaft <c>GUID</c>.</summary>
        internal List<SqprojHuellflaeche> Treffer(AbbildBauteil b, out bool ueberGuid)
        {
            ueberGuid = true;
            List<SqprojHuellflaeche> treffer = null;
            if (b.HottcadGuid != null) _jeGid.TryGetValue(b.HottcadGuid, out treffer);
            if (treffer == null)
            {
                ueberGuid = false;
                if (!string.IsNullOrEmpty(b.Kennung)) _jeGlobalId.TryGetValue(b.Kennung, out treffer);
            }
            return treffer == null || treffer.Count == 0 ? null : treffer;
        }

        /// <summary>Trägt der Treffer mehrere Hüllflächen mit verschiedenen Aufbauten (dann wird nicht geraten)?</summary>
        internal static bool Mehrdeutig(List<SqprojHuellflaeche> treffer)
            => treffer.Select(h => h.AufbauKennung ?? "").Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1;
    }

    /// <summary>
    /// <b>Die Standprüfung beim Weg „IFC + Projektdatei“</b> (Anwenderentscheid vom 08.10.2026): Gehören IFC und Projektdatei
    /// zum selben Projektstand? Plattformfrei, ohne Datenbank, schreibt nichts.
    /// <list type="number">
    /// <item><b>Je Bauteil</b> der Hülle (opak und Öffnung), über die GUID abgeglichen: das U der IFC (wie im Abbild) gegen das U
    /// der Projektdatei (<c>BmElement.UValue</c>, sonst das U des zugewiesenen Aufbaus — derselbe Wert wie beim Weg „nur
    /// Projektdatei“). Abweichend, wenn |U_PD/U_IFC − 1| &gt; <see cref="U_SCHWELLE"/>.</item>
    /// <item><b>Anschlag</b>: Die abweichenden Bauteile machen mehr als <see cref="FLAECHEN_SCHWELLE"/> der Hüllfläche aus (Brutto,
    /// opak und Fenster; Hülle = Randbedingung Außenluft, Erdreich oder unbeheizt, eine Öffnung erbt die ihres Wirts).</item>
    /// <item><b>Anzeichen</b>, jedes nur aus den Daten belegt: (a) <see cref="Standanzeichen.KopieNachModellstand"/>,
    /// (b) <see cref="Standanzeichen.BaujahrAbweichend"/>, (c) <see cref="Standanzeichen.DickePasstNicht"/>.</item>
    /// </list>
    /// </summary>
    internal sealed class Standpruefung
    {
        /// <summary>Die Schwelle der Abweichung je Bauteil: dieselbe wie der Hinweis „U weicht ab“ (10 %).</summary>
        internal const double U_SCHWELLE = GebaeudeFestwerte.UWERT_ABWEICHUNG_HINWEIS;

        /// <summary>Der Anteil der Hüllfläche, ab dem die Prüfung anschlägt (größer als 5 %).</summary>
        internal const double FLAECHEN_SCHWELLE = 0.05;

        /// <summary>Die gezeichnete Dicke passt zur Schichtsumme, wenn |d − Σd| ≤ 5 mm + 2 % von Σd.</summary>
        internal const double DICKE_TOLERANZ_M = 0.005;
        internal const double DICKE_TOLERANZ_RELATIV = 0.02;

        private readonly Dictionary<string, double> _uProjektdatei = new Dictionary<string, double>(StringComparer.Ordinal);

        private Standpruefung() { }

        /// <summary>Schlägt die Prüfung an (abweichende Fläche über <see cref="FLAECHEN_SCHWELLE"/> der Hüllfläche)?</summary>
        internal bool Angeschlagen { get; private set; }

        /// <summary>Bauteile der Hülle mit U auf beiden Seiten.</summary>
        internal int Verglichen { get; private set; }

        /// <summary>Davon abweichend (relativ über <see cref="U_SCHWELLE"/>).</summary>
        internal int Abweichend { get; private set; }

        /// <summary>Die Hüllfläche brutto [m²] (opak und Fenster, alle Hüllbauteile der IFC).</summary>
        internal double HuellflaecheM2 { get; private set; }

        /// <summary>Die Bruttofläche der abweichenden Bauteile [m²].</summary>
        internal double AbweichendM2 { get; private set; }

        /// <summary>Der Anteil der abweichenden Fläche an der Hüllfläche (0…1).</summary>
        internal double Anteil => HuellflaecheM2 > 0.0 ? AbweichendM2 / HuellflaecheM2 : 0.0;

        /// <summary>Je Bauteilart die Mediane beider Seiten (über die verglichenen Bauteile).</summary>
        internal IReadOnlyList<StandpruefungArt> JeArt { get; private set; } = Array.Empty<StandpruefungArt>();

        /// <summary>Bis zu fünf abweichende Bauteile, größte Fläche zuerst.</summary>
        internal IReadOnlyList<StandpruefungBeispiel> Beispiele { get; private set; } = Array.Empty<StandpruefungBeispiel>();

        /// <summary>Die belegten Anzeichen für einen anderen Projektstand.</summary>
        internal IReadOnlyList<Standanzeichen> Anzeichen { get; private set; } = Array.Empty<Standanzeichen>();

        /// <summary>Der jüngste Eintrag <c>DBL:COPY</c> im Journal der Projektdatei; <c>null</c> = keiner.</summary>
        internal DateTime? Kopiezeitpunkt { get; private set; }

        /// <summary>Der Modellstand der IFC (<c>StampEdit</c> am Gebäude); <c>null</c> = nicht belegt.</summary>
        internal DateTime? Modellstand { get; private set; }

        /// <summary>Das Baujahr der IFC und der Projektdatei; <c>null</c> = nicht belegt.</summary>
        internal int? BaujahrIfc { get; private set; }
        internal int? BaujahrProjektdatei { get; private set; }

        /// <summary>Abweichende opake Bauteile mit gezeichneter Dicke und Schichtsumme; davon solche, deren Dicke nicht passt.</summary>
        internal int DickeGeprueft { get; private set; }
        internal int DickeAbweichend { get; private set; }

        /// <summary>Das U der Projektdatei eines IFC-Bauteils (Schlüssel <see cref="AbbildBauteil.Kennung"/>); <c>null</c> = ohne Gegenstück.</summary>
        internal double? UProjektdatei(AbbildBauteil b)
            => b != null && b.Kennung != null && _uProjektdatei.TryGetValue(b.Kennung, out double u) ? u : null;

        /// <summary>Das U der Projektdatei an einer Hüllfläche — derselbe Wert wie beim Weg „nur Projektdatei“: <c>UValue</c>, sonst das U des Aufbaus.</summary>
        internal static double? UAnFlaeche(SqprojHuellflaeche h, SqprojAbbild projekt)
        {
            double? u = h.UWert;
            if (!(u > 0.0) && h.AufbauKennung != null && projekt.Aufbauten.TryGetValue(h.AufbauKennung, out SqprojAufbau a)) u = a.UWert;
            return u > 0.0 ? u : null;
        }

        /// <summary>
        /// <b>Prüft</b> ein IFC-Gebäude gegen die Projektdatei. Ohne gelesene Bauteiltabellen eine leere Prüfung, die nicht anschlägt.
        /// </summary>
        /// <param name="modellstand">Der Modellstand der IFC (<see cref="IfcModellstand"/>); <c>null</c> = nicht belegt.</param>
        internal static Standpruefung Pruefen(SqprojAbbild projekt, AbbildGebaeude ifc, DateTime? modellstand)
        {
            var p = new Standpruefung { Modellstand = modellstand, Kopiezeitpunkt = projekt?.Kopiezeitpunkt, BaujahrIfc = ifc?.Baujahr };
            if (projekt == null || ifc == null || !projekt.BauteileGelesen) return p;
            List<int> jahre = projekt.Gebaeude.Where(g => g.Baujahr.HasValue).Select(g => g.Baujahr.Value).Distinct().ToList();
            p.BaujahrProjektdatei = jahre.Count == 1 ? jahre[0] : null;

            var schluessel = new SqprojBauteilschluessel(projekt);
            var verglichen = new List<(AbbildBauteil B, SqprojHuellflaeche H, double Flaeche, double UIfc, double UPd, bool Abw)>();
            var gesehen = new HashSet<AbbildBauteil>(ReferenceEqualityComparer.Instance);
            void Betrachten(AbbildBauteil b, Randbedingung? wirt)
            {
                if (b == null || !gesehen.Add(b)) return;
                Randbedingung rand = b.RandbedingungWirksam ?? b.Randbedingung;
                if (!Huelle(rand) && wirt.HasValue) rand = wirt.Value;
                List<SqprojHuellflaeche> treffer = schluessel.Treffer(b, out _);
                SqprojHuellflaeche h = treffer != null && !SqprojBauteilschluessel.Mehrdeutig(treffer) ? treffer[0] : null;
                if (h != null && UAnFlaeche(h, projekt) is double uPd) p._uProjektdatei[b.Kennung ?? ""] = uPd;
                if (!Huelle(rand)) return;
                double flaeche = b.BruttoflaecheM2 ?? b.NettoflaecheM2 ?? 0.0;
                if (!(flaeche > 0.0)) return;
                p.HuellflaecheM2 += flaeche;
                if (h == null || !(b.UWertWm2K > 0.0) || UAnFlaeche(h, projekt) is not double u) return;
                bool abw = Math.Abs(u / b.UWertWm2K.Value - 1.0) > U_SCHWELLE + 1e-12;
                verglichen.Add((b, h, flaeche, b.UWertWm2K.Value, u, abw));
            }
            foreach (AbbildBauteil b in ifc.Bauteile)
            {
                Betrachten(b, null);
                Randbedingung wirt = b.RandbedingungWirksam ?? b.Randbedingung;
                foreach (AbbildBauteil o in b.Oeffnungen) Betrachten(o, Huelle(wirt) ? wirt : null);
            }

            var abweichend = verglichen.Where(v => v.Abw).ToList();
            p.Verglichen = verglichen.Count;
            p.Abweichend = abweichend.Count;
            p.AbweichendM2 = abweichend.Sum(v => v.Flaeche);
            p.Angeschlagen = p.HuellflaecheM2 > 0.0 && p.Anteil > FLAECHEN_SCHWELLE;
            p.JeArt = verglichen.GroupBy(v => v.B.Art).OrderBy(g => g.Key)
                                .Select(g => new StandpruefungArt(g.Key, g.Count(), g.Count(v => v.Abw),
                                                                  Median(g.Select(v => v.UIfc)), Median(g.Select(v => v.UPd))))
                                .ToList();
            p.Beispiele = abweichend.OrderByDescending(v => v.Flaeche).ThenBy(v => v.B.Kennung, StringComparer.Ordinal).Take(5)
                                    .Select(v => new StandpruefungBeispiel(
                                        string.IsNullOrWhiteSpace(v.B.Name) ? v.B.Kennung ?? "" : v.B.Name.Trim(), v.B.Art, v.Flaeche,
                                        v.UIfc, v.UPd, v.B.Aufbau?.Name?.Trim() ?? "", AufbauVon(v.H, projekt)?.Name?.Trim() ?? ""))
                                    .ToList();

            // (c) Die gezeichnete Dicke (BmElement.Thickness) gegen die Schichtsumme des zugewiesenen Aufbaus der Projektdatei.
            foreach (var v in abweichend.Where(v => SqprojAufbauwahl.Opak(v.B.Art)))
            {
                SqprojAufbau a = AufbauVon(v.H, projekt);
                double? summe = a != null && a.Schichten.Count > 0 && a.Schichten.All(s => s.DickeM > 0.0)
                    ? a.Schichten.Sum(s => s.DickeM.Value) : a?.DickeM;
                if (!(summe > 0.0) || !projekt.Flaechendicke.TryGetValue(v.H.Uuid, out double d) || !(d > 0.0)) continue;
                p.DickeGeprueft++;
                if (Math.Abs(d - summe.Value) > DICKE_TOLERANZ_M + DICKE_TOLERANZ_RELATIV * summe.Value) p.DickeAbweichend++;
            }

            var anzeichen = new List<Standanzeichen>();
            if (p.Kopiezeitpunkt is DateTime kopie && p.Modellstand is DateTime stand && kopie > stand)
                anzeichen.Add(Standanzeichen.KopieNachModellstand);
            if (p.BaujahrIfc is int bi && p.BaujahrProjektdatei is int bp && bi != bp)
                anzeichen.Add(Standanzeichen.BaujahrAbweichend);
            if (p.DickeGeprueft > 0 && 2 * p.DickeAbweichend > p.DickeGeprueft)
                anzeichen.Add(Standanzeichen.DickePasstNicht);
            p.Anzeichen = anzeichen;
            return p;
        }

        private static bool Huelle(Randbedingung r) => r == Randbedingung.Aussenluft || r == Randbedingung.Erdreich || r == Randbedingung.Unbeheizt;

        private static SqprojAufbau AufbauVon(SqprojHuellflaeche h, SqprojAbbild projekt)
            => h.AufbauKennung != null && projekt.Aufbauten.TryGetValue(h.AufbauKennung, out SqprojAufbau a) ? a : null;

        private static double Median(IEnumerable<double> werte)
        {
            double[] w = werte.OrderBy(x => x).ToArray();
            if (w.Length == 0) return 0.0;
            return w.Length % 2 == 1 ? w[w.Length / 2] : 0.5 * (w[w.Length / 2 - 1] + w[w.Length / 2]);
        }

        // ==================================================================
        //  Protokoll
        // ==================================================================

        /// <summary>
        /// <b>Die Warnung im Protokoll</b> (<see cref="SqprojProtokoll.STAND_ABWEICHEND"/>): Zahl und Flächenanteil der abweichenden
        /// Bauteile und die Anzeichen als Text; <c>null</c>, wenn die Prüfung nicht anschlägt.
        /// </summary>
        internal PruefMeldung Meldung()
        {
            if (!Angeschlagen) return null;
            return new PruefMeldung(PruefStufe.Warnung, SqprojProtokoll.STAND_ABWEICHEND,
                                    Abweichend.ToString(CultureInfo.InvariantCulture),
                                    Math.Round(100.0 * Anteil, 1).ToString(CultureInfo.CurrentCulture),
                                    Anzeichentext());
        }

        /// <summary>Die Anzeichen als ein Satzteil, durch „; “ getrennt; ohne Anzeichen der Text „keine weiteren“.</summary>
        internal string Anzeichentext()
            => Anzeichen.Count == 0 ? MyResource.Resource.IMP_SQ_STAND_KEIN_ANZEICHEN : string.Join("; ", Anzeichen.Select(Anzeichentext));

        /// <summary>Ein Anzeichen als Satzteil mit seinen Belegen (Zeitpunkte, Baujahre, Zahl der Bauteile).</summary>
        internal string Anzeichentext(Standanzeichen a)
        {
            switch (a)
            {
                case Standanzeichen.KopieNachModellstand:
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.IMP_SQ_STAND_ANZEICHEN_KOPIE,
                                         Kopiezeitpunkt?.ToString("g", CultureInfo.CurrentCulture) ?? "",
                                         Modellstand?.ToString("g", CultureInfo.CurrentCulture) ?? "");
                case Standanzeichen.BaujahrAbweichend:
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.IMP_SQ_STAND_ANZEICHEN_BAUJAHR,
                                         BaujahrIfc?.ToString(CultureInfo.InvariantCulture) ?? "",
                                         BaujahrProjektdatei?.ToString(CultureInfo.InvariantCulture) ?? "");
                case Standanzeichen.DickePasstNicht:
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.IMP_SQ_STAND_ANZEICHEN_DICKE, DickeAbweichend, DickeGeprueft);
                default:
                    return a.ToString();
            }
        }

        // ==================================================================
        //  Modellstand der IFC
        // ==================================================================

        private static readonly Regex GEBAEUDE = new Regex(@"#(\d+)\s*=\s*IFCBUILDING\s*\(\s*'([^']*)'", RegexOptions.CultureInvariant);
        private static readonly Regex BEZUG = new Regex(@"=\s*IFCRELDEFINESBYPROPERTIES\s*\((?:'(?:[^']|'')*'|[^;('])*\(([#\d,\s]*)\)\s*,\s*#(\d+)\s*\)",
                                                        RegexOptions.CultureInvariant);
        private static readonly Regex SATZ = new Regex(@"#(\d+)\s*=\s*IFCPROPERTYSET\s*\((?:'(?:[^']|'')*'|[^;('])*\(([#\d,\s]*)\)\s*\)",
                                                       RegexOptions.CultureInvariant);
        private static readonly Regex STEMPEL = new Regex(@"#(\d+)\s*=\s*IFCPROPERTYSINGLEVALUE\s*\(\s*'StampEdit'\s*,\s*(?:'(?:[^']|'')*'|\$)\s*,\s*IFC\w+\s*\(\s*'([^']*)'",
                                                          RegexOptions.CultureInvariant);
        private static readonly Regex VERWEIS = new Regex(@"#(\d+)", RegexOptions.CultureInvariant);

        /// <summary>
        /// <b>Der Modellstand der IFC</b>: der jüngste <c>StampEdit</c> („Zeitpunkt der letzten Bearbeitung“, Text
        /// <c>TT.MM.JJJJ hh:mm:ss</c>) in den Eigenschaftssätzen des Gebäudes <paramref name="gebaeudeKennung"/> (GlobalId; trägt
        /// die Datei nur ein Gebäude, gilt es ohne Kennung). Gelesen aus dem STEP-Text des Puffers, ohne Geometrie;
        /// <c>null</c> = nicht belegt.
        /// </summary>
        internal static DateTime? IfcModellstand(byte[] puffer, string gebaeudeKennung)
        {
            if (puffer == null || puffer.Length == 0) return null;
            string text = Encoding.Latin1.GetString(puffer);
            var gebaeude = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match m in GEBAEUDE.Matches(text)) gebaeude[m.Groups[1].Value] = m.Groups[2].Value;
            string id = gebaeude.Count == 1 ? gebaeude.Keys.First()
                      : gebaeude.FirstOrDefault(e => string.Equals(e.Value, gebaeudeKennung, StringComparison.Ordinal)).Key;
            if (id == null) return null;

            var saetze = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match m in BEZUG.Matches(text))
                if (VERWEIS.Matches(m.Groups[1].Value).Any(v => v.Groups[1].Value == id)) saetze.Add(m.Groups[2].Value);
            if (saetze.Count == 0) return null;
            var eigenschaften = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match m in SATZ.Matches(text))
                if (saetze.Contains(m.Groups[1].Value))
                    foreach (Match v in VERWEIS.Matches(m.Groups[2].Value)) eigenschaften.Add(v.Groups[1].Value);
            DateTime? juengster = null;
            foreach (Match m in STEMPEL.Matches(text))
                if (eigenschaften.Contains(m.Groups[1].Value) && Zeitpunkt(m.Groups[2].Value) is DateTime t && (juengster == null || t > juengster))
                    juengster = t;
            return juengster;
        }

        /// <summary>Ein Zeitpunkt der Form <c>TT.MM.JJJJ hh:mm[:ss]</c> (HottCAD); <c>null</c> = keiner.</summary>
        internal static DateTime? Zeitpunkt(string s)
            => DateTime.TryParseExact(s?.Trim(), new[] { "dd.MM.yyyy HH:mm:ss", "dd.MM.yyyy H:mm:ss", "dd.MM.yyyy HH:mm" },
                                      CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime t) ? t : null;

        private static readonly Regex KOPIE = new Regex(@"(\d{2}\.\d{2}\.\d{4} \d{1,2}:\d{2}(?::\d{2})?)\s*>DBL:COPY\b", RegexOptions.CultureInvariant);

        /// <summary>Der jüngste Eintrag <c>DBL:COPY</c> in den Journaltexten (<c>PrJournalEntry.JournalData</c>); <c>null</c> = keiner.</summary>
        internal static DateTime? Kopie(IEnumerable<string> journal)
        {
            DateTime? juengster = null;
            foreach (string j in journal ?? Enumerable.Empty<string>())
                if (j != null)
                    foreach (Match m in KOPIE.Matches(j))
                        if (Zeitpunkt(m.Groups[1].Value) is DateTime t && (juengster == null || t > juengster)) juengster = t;
            return juengster;
        }
    }
}
