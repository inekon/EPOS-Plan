using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Ein Ferienzeile der Schnellfelder (Konzept 7.8): Name „Ferien n", erster und letzter Tag 1 … 365.</summary>
    public sealed record Ferienzeile(string Name, int Beginn, int Ende);

    public static partial class Kalenderbedienung
    {
        // =================================================================
        //  Ferien — beliebig viele, die ersten vier in den Gebäudespalten
        // =================================================================

        /// <summary>Ist die Zeile ein Ferienzeile ab dem fünften (Bezeichner „Ferien n", n ≥ 5, Zeitraum)?</summary>
        public static bool IstFerienzeile(Zuordnungsschluessel s)
            => s != null && !s.IstFeiertag && Feriennummer(s.Name) > Matrixeingang.FERIENZEITRAEUME;

        /// <summary>
        /// <b>Die Ferienzeiträume des Gebäudes</b>: die ersten vier aus <c>Ferienbeginn/-ende_1…4</c> (eine Grenze 0
        /// oder 366 heißt „aus" und fällt weg), dann die Ferienzeilen „Ferien 5" … der angelegten Kalender von Gebäude
        /// und Zonen, nach Nummer.
        /// </summary>
        public static IReadOnlyList<Ferienzeile> Ferienzeitraeume(Konditionierungsarbeitsstand stand)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            var liste = new List<Ferienzeile>();
            Matrixeingang b = stand.Gebaeude.Bestand;
            for (int k = 0; k < Matrixeingang.FERIENZEITRAEUME; k++)
            {
                double von = b.Ferienbeginn[k], bis = b.Ferienende[k];
                if (!Jahrestag(von) || !Jahrestag(bis)) continue;
                liste.Add(new Ferienzeile(Ferienname(k + 1), (int)von, (int)bis));
            }
            var weitere = new SortedDictionary<int, Ferienzeile>();
            foreach (Konditionierungsstand ebene in Ebenen(stand))
                foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                {
                    Konditionierungskalender kal = ebene.Kalender(g);
                    if (kal == null) continue;
                    foreach (Kalenderregel r in kal.Perioden)
                    {
                        if (Konditionierungsarbeit.IstMatrixbereich(r) || !IstFerienzeile(Zuordnungsschluessel.Von(r))) continue;
                        int n = Feriennummer(r.Bezeichner);
                        if (!weitere.ContainsKey(n)) weitere[n] = new Ferienzeile(r.Bezeichner, r.Beginn, r.Ende);
                    }
                }
            liste.AddRange(weitere.Values);
            return liste;
        }

        /// <summary>
        /// <b>Die Ferienzeiträume setzen</b> (Konzept 7.8, E110): beliebig viele Datumsbereiche (Beginn nach Ende heißt
        /// über den Jahreswechsel). Die ersten vier gehen in <c>Ferienbeginn/-ende_1…4</c> des Gebäudes (die übrigen
        /// Spalten werden 0 = „aus") — sie lesen der Generator, der Tagesbilanz-Weg und der Zapfkalender —, danach wird
        /// der Matrixbereich jedes angelegten Kalenders von Gebäude und Zonen erneuert („Matrix erneut", P12). Ab dem
        /// fünften wird jeder Zeitraum in jedem angelegten Kalender, der eine Ferienperiode trägt, eine Zeile „Ferien n"
        /// mit deren Angabe am untersten freien Platz des Eigenbands; alte Ferienzeilen fallen.
        /// </summary>
        public static Konditionierungsschritt FerienSetzen(Konditionierungsarbeitsstand stand, IReadOnlyList<(int Beginn, int Ende)> zeitraeume)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            var z = zeitraeume ?? Array.Empty<(int, int)>();
            foreach ((int beginn, int ende) in z)
                if (beginn < Kalenderregel.TAG_MIN || beginn > Kalenderregel.TAG_MAX
                    || ende < Kalenderregel.TAG_MIN || ende > Kalenderregel.TAG_MAX)
                    return Konditionierungsschritt.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_PERIODE_TAG,
                        beginn.ToString(CultureInfo.InvariantCulture), ende.ToString(CultureInfo.InvariantCulture)));

            // 1. Die vier Gebäudespalten.
            Konditionierungsarbeitsstand a = stand.MitGebaeude(stand.Gebaeude.MitBestand(b =>
            {
                for (int k = 0; k < Matrixeingang.FERIENZEITRAEUME; k++)
                {
                    b.Ferienbeginn[k] = k < z.Count ? z[k].Beginn : 0.0;
                    b.Ferienende[k] = k < z.Count ? z[k].Ende : 0.0;
                }
            }));

            // 2. Der Matrixbereich jedes angelegten Kalenders folgt — am Gebäude und an jeder Zone.
            foreach (long? zone in Ebenenorte(a))
                foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                {
                    if (a.Ebene(zone)?.Kalender(g) == null) continue;
                    Konditionierungsschritt s = Konditionierungsarbeit.MatrixErneut(a, new Konditionierungsort(g, zone));
                    if (!s.Ok) return s;
                    a = s.Stand;
                }

            // 3. Die Ferienzeilen ab der fünften.
            foreach (long? zone in Ebenenorte(a))
            {
                Konditionierungsstand ebene = a.Ebene(zone);
                foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                {
                    Konditionierungskalender k = ebene.Kalender(g);
                    if (k == null) continue;
                    var perioden = k.Perioden
                        .Where(r => Konditionierungsarbeit.IstMatrixbereich(r) || !IstFerienzeile(Zuordnungsschluessel.Von(r)))
                        .ToList();
                    Kalenderregel ferien = k.Perioden.FirstOrDefault(r => string.Equals(r.Art, DbWerte.KOND_ART_FERIEN, StringComparison.Ordinal));
                    if (ferien != null)
                        for (int i = Matrixeingang.FERIENZEITRAEUME; i < z.Count; i++)
                        {
                            int rang = Standardfahrplan.RANG_EIGEN;
                            while (rang <= Standardfahrplan.RANG_EIGEN_LETZTER && perioden.Any(r => r.Rang == rang)) rang++;
                            if (rang > Standardfahrplan.RANG_EIGEN_LETZTER)
                                return Konditionierungsschritt.Fehler(string.Format(CultureInfo.CurrentCulture,
                                    MyResource.Resource.KOND_MSG_RANG_BAND_VOLL,
                                    Standardfahrplan.RANG_EIGEN.ToString(CultureInfo.InvariantCulture),
                                    Standardfahrplan.RANG_EIGEN_LETZTER.ToString(CultureInfo.InvariantCulture)));
                            perioden.Add(Kalenderregel.Zeitraum(rang, DbWerte.KOND_ART_ZEITRAUM, Ferienname(i + 1),
                                                                z[i].Beginn, z[i].Ende, ferien.Angabe));
                        }
                    if (perioden.Count > Kalenderregel.PERIODEN_MAX)
                        return Konditionierungsschritt.Fehler(string.Format(CultureInfo.CurrentCulture,
                            MyResource.Resource.KOND_MSG_PERIODEN_ZU_VIELE,
                            perioden.Count.ToString(CultureInfo.InvariantCulture),
                            Kalenderregel.PERIODEN_MAX.ToString(CultureInfo.InvariantCulture)));
                    var neu = new Konditionierungskalender(k.Groesse, k.Grundangabe, k.Nennwert, perioden);
                    if (!Kalendervergleich.KalenderGleich(k, neu)) ebene = ebene.MitKalender(g, neu, ebene.Herkunft(g));
                }
                a = a.MitEbene(zone, ebene);
            }
            return Konditionierungsschritt.Gut(a);
        }

        // =================================================================
        //  Saison und Feiertage
        // =================================================================

        /// <summary>
        /// <b>Die Saison von–bis</b> einer Größe (Zeile <c>SAISON</c> der Matrix, Konzept 3.2): beide leer heißt
        /// ganzjährig. Ein angelegter Kalender folgt mit seinem Matrixbereich (P12) — auch wenn er von Hand geändert ist.
        /// </summary>
        public static Konditionierungsschritt SaisonSetzen(Konditionierungsarbeitsstand stand, Konditionierungsort ort, int? von, int? bis)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (ort == null) throw new ArgumentNullException(nameof(ort));
            Matrixzelle zelle = von.HasValue || bis.HasValue ? Matrixzelle.NurZeiten(von, bis) : Matrixzelle.Leer;
            Konditionierungsschritt s = Konditionierungsarbeit.ZelleSetzen(stand, ort, DbWerte.KOND_ZEILE_SAISON, zelle);
            if (!s.Ok || s.Stand.Ebene(ort.Zone)?.Kalender(ort.Groesse) == null) return s;
            return Konditionierungsarbeit.MatrixErneut(s.Stand, ort);
        }

        /// <summary>
        /// <b>„Feiertage laden"</b> (bundeseinheitlich): die neun Regeln (Rang 100 … 108, unter den Ferien) mit „wie
        /// Wochentag" in jeder gewählten Größe (<c>null</c> = alle mit angelegtem Kalender).
        /// </summary>
        public static Konditionierungsschritt FeiertageLaden(Konditionierungsarbeitsstand stand, long? zone,
                                                             IEnumerable<Konditionierungsgroesse> giltFuer = null, int wieWochentag = 7)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            Konditionierungsstand ebene = stand.Ebene(zone);
            if (ebene == null) return ZoneFehlt(zone);
            List<Konditionierungsgroesse> groessen = Groessen(ebene, giltFuer);
            if (groessen.Count == 0)
                return Konditionierungsschritt.Fehler(Text(MyResource.Resource.KOND_MSG_BEDIENUNG_KEINE_GROESSE,
                                                           MyResource.Resource.KOND_TEXT_BEDIENUNG_LANDESFEIERTAG));
            Konditionierungsarbeitsstand a = stand;
            foreach (Konditionierungsgroesse g in groessen)
            {
                Konditionierungsschritt s = Konditionierungsarbeit.Feiertage(a, new Konditionierungsort(g, zone), wieWochentag);
                if (!s.Ok) return s;
                a = s.Stand;
            }
            return Konditionierungsschritt.Gut(a);
        }

        /// <summary>
        /// <b>Die Feiertage eines Landes</b> (Stufe 1): die neun Regeln und dazu jeder Landesfeiertag des Bezugsjahrs
        /// als fester Einzeltag „Landesfeiertag XX" mit „wie Sonntag" — als Regel erst mit Stufe 2 (Schemaschritt 207).
        /// Ein schon vorhandener Einzeltag bleibt.
        /// </summary>
        public static Konditionierungsschritt LandesfeiertageLaden(Konditionierungsarbeitsstand stand, long? zone, string land,
                                                                   IEnumerable<Konditionierungsgroesse> giltFuer = null)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (!Landesfeiertage.Bekannt(land))
                return Konditionierungsschritt.Fehler(Text(MyResource.Resource.KOND_MSG_BEDIENUNG_LAND, land ?? ""));
            List<Konditionierungsgroesse> groessen = giltFuer?.ToList();
            Konditionierungsschritt s = FeiertageLaden(stand, zone, groessen);
            if (!s.Ok) return s;
            Konditionierungsarbeitsstand a = s.Stand;
            var bund = new HashSet<int>(Landesfeiertage.Jahrestage(null, a.Referenzjahr));
            string name = Text(MyResource.Resource.KOND_TEXT_BEDIENUNG_LANDESFEIERTAG, land);
            foreach (int tag in Landesfeiertage.Jahrestage(land, a.Referenzjahr))
            {
                if (bund.Contains(tag)) continue;
                Zuordnungsschluessel schluessel = Zuordnungsschluessel.Zeitraum(name, tag, tag);
                if (Zuordnungen(a, zone).Any(z => z.Schluessel == schluessel)) continue;
                s = ZuordnungSetzen(a, zone, null, schluessel, Zuordnungsangabe.WieSonntag, groessen);
                if (!s.Ok) return s;
                a = s.Stand;
            }
            return Konditionierungsschritt.Gut(a);
        }

        // =================================================================
        //  Vorlage für alle Größen, Monat kopieren
        // =================================================================

        /// <summary>
        /// <b>„Vorlage übernehmen" für alle Größen</b> (Konzept 7.4, 7.8; E57): jede Vorlage über
        /// <see cref="Konditionierungsarbeit.VorlageUebernehmen"/> an ihrer Größe, nacheinander als EIN Schritt. Lehnt
        /// eine Größe ab oder verlangt eine Rückfrage, kommt genau dieses Ergebnis zurück — nichts geändert.
        /// </summary>
        public static Konditionierungsschritt VorlageAlleUebernehmen(Konditionierungsarbeitsstand stand, long? zone,
                                                                     IEnumerable<Konditionierungsvorlage> vorlagen)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            Konditionierungsarbeitsstand a = stand;
            foreach (Konditionierungsvorlage v in vorlagen ?? Enumerable.Empty<Konditionierungsvorlage>())
            {
                if (v == null) continue;
                Konditionierungsschritt s = Konditionierungsarbeit.VorlageUebernehmen(a, new Konditionierungsort(v.Groesse, zone), v);
                if (!s.Ok) return s;
                a = s.Stand;
            }
            return Konditionierungsschritt.Gut(a);
        }

        /// <summary>
        /// <b>„Monat kopieren"</b> (Konzept 7.8): jede Zuordnungszeile und jeder Einzeltag mit festem Datum, der den
        /// Quellmonat berührt — ohne Feiertagsregeln und Ferienzeilen —, wird auf den Quellmonat beschnitten, um den
        /// Abstand der Monatsersten verschoben und auf den Zielmonat beschnitten; gekoppelt in denselben Größen mit
        /// derselben Angabe. Die Kopien kommen in der Folge ihrer Quellränge über alle vorhandenen Zeilen; eine
        /// gleiche Zeile im Ziel wird ersetzt.
        /// </summary>
        public static Konditionierungsschritt MonatKopieren(Konditionierungsarbeitsstand stand, long? zone, int quellmonat, int zielmonat)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (quellmonat < 1 || quellmonat > 12 || zielmonat < 1 || zielmonat > 12)
                return Konditionierungsschritt.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_BEDIENUNG_MONAT,
                    quellmonat.ToString(CultureInfo.InvariantCulture), zielmonat.ToString(CultureInfo.InvariantCulture)));
            if (stand.Ebene(zone) == null) return ZoneFehlt(zone);
            if (quellmonat == zielmonat) return Konditionierungsschritt.Gut(stand);
            int qa = Feiertage.Gemeinjahrestag(quellmonat, 1), qe = qa + Feiertage.TageJeMonat[quellmonat - 1] - 1;
            int za = Feiertage.Gemeinjahrestag(zielmonat, 1), ze = za + Feiertage.TageJeMonat[zielmonat - 1] - 1;

            var auftraege = new List<(int Rang, Zuordnungsschluessel Neu, IReadOnlyDictionary<Konditionierungsgroesse, Kalenderangabe> Angaben)>();
            foreach (Zuordnungszeile z in Zuordnungen(stand, zone))
            {
                if (z.Schluessel.IstFeiertag || z.IstFerien) continue;
                Kalenderregel probe = Kalenderregel.Zeitraum(Standardfahrplan.RANG_EIGEN, DbWerte.KOND_ART_ZEITRAUM, z.Schluessel.Name,
                                                             z.Schluessel.Beginn, z.Schluessel.Ende, Kalenderangabe.Abgeschaltet);
                int lauf = -1;
                for (int d = qa; d <= qe + 1; d++)
                {
                    bool drin = d <= qe && probe.Enthaelt(d - 1, -1);
                    if (drin && lauf < 0) lauf = d;
                    if (drin || lauf < 0) continue;
                    int nb = lauf - qa + za, ne = Math.Min(d - 1 - qa + za, ze);
                    lauf = -1;
                    if (nb > ze) continue;
                    auftraege.Add((z.Raenge.Values.Max(), Zuordnungsschluessel.Zeitraum(z.Schluessel.Name, nb, ne), z.Angaben));
                }
            }
            if (auftraege.Count == 0)
                return Konditionierungsschritt.Fehler(Text(MyResource.Resource.KOND_MSG_BEDIENUNG_MONAT_LEER, quellmonat));

            Konditionierungsarbeitsstand a = stand;
            foreach (var auftrag in auftraege.OrderBy(x => x.Rang))
            {
                bool vorhanden = Zuordnungen(a, zone).Any(z => z.Schluessel == auftrag.Neu);
                Konditionierungsschritt s = Gekoppelt(a, zone, vorhanden ? auftrag.Neu : null, auftrag.Neu, auftrag.Angaben);
                if (!s.Ok) return s;
                a = s.Stand;
            }
            return Konditionierungsschritt.Gut(a);
        }

        // =================================================================
        //  Helfer
        // =================================================================

        /// <summary>„Ferien n" — derselbe Bezeichner wie der Generator (<see cref="Standardfahrplan.BEZEICHNER_FERIEN"/>).</summary>
        public static string Ferienname(int nummer)
            => Standardfahrplan.BEZEICHNER_FERIEN + " " + nummer.ToString(CultureInfo.InvariantCulture);

        /// <summary>Die Nummer n eines Bezeichners „Ferien n"; 0, wenn er keiner ist.</summary>
        private static int Feriennummer(string name)
        {
            string praefix = Standardfahrplan.BEZEICHNER_FERIEN + " ";
            if (name == null || !name.StartsWith(praefix, StringComparison.Ordinal)) return 0;
            return int.TryParse(name.Substring(praefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int n) ? n : 0;
        }

        private static bool Jahrestag(double tag) => tag >= Kalenderregel.TAG_MIN && tag <= Kalenderregel.TAG_MAX && tag == Math.Floor(tag);

        private static IEnumerable<Konditionierungsstand> Ebenen(Konditionierungsarbeitsstand a)
            => new[] { a.Gebaeude }.Concat(a.Zonen.Select(z => z.Stand));

        private static IEnumerable<long?> Ebenenorte(Konditionierungsarbeitsstand a)
            => new long?[] { null }.Concat(a.Zonen.Select(z => (long?)z.Id)).ToList();
    }
}
