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
        /// <b>Die Ferienzeiträume des Gebäudes</b>: die ersten vier aus <c>Ferienbeginn/-ende_1…4</c> mit ihrem Namen
        /// (<see cref="Konditionierungsstand.Feriennamen"/>, ohne Namen „Ferien n"; eine Grenze 0
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
                string name = k < stand.Gebaeude.Feriennamen.Count ? stand.Gebaeude.Feriennamen[k] : null;
                liste.Add(new Ferienzeile(string.IsNullOrWhiteSpace(name) ? Ferienname(k + 1) : name, (int)von, (int)bis));
            }
            if (stand.Gebaeude.Ferienliste.Count > 0)
            {
                liste.AddRange(stand.Gebaeude.Ferienliste);    // die Ferienliste des gemeinsamen Kalenders (Stufe 2)
                return liste;
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
        /// Spalten werden 0 = „aus"), die weiteren in die Ferienliste des gemeinsamen Kalenders ab Rang 204; danach wird
        /// der Matrixbereich jedes angelegten Kalenders von Gebäude und Zonen erneuert („Matrix erneut", P12).
        /// </summary>
        public static Konditionierungsschritt FerienSetzen(Konditionierungsarbeitsstand stand, IReadOnlyList<(int Beginn, int Ende)> zeitraeume)
            => FerienlisteSetzen(stand, (zeitraeume ?? Array.Empty<(int, int)>())
                                        .Select((f, i) => new Ferienzeile(Ferienname(i + 1), f.Beginn, f.Ende)).ToList());

        /// <summary>
        /// <b>Die Ferienliste</b> (Konzept 7.8, Stufe 2): beliebig viele benannte Ferienzeiträume. Die ersten vier stehen in
        /// den Gebäudespalten <c>Ferienbeginn/-ende_1…4</c> (ihr Spiegel im gemeinsamen Kalender, Rang 200 … 203, schreibt
        /// der Trigger; ihre Namen trägt <see cref="Konditionierungsstand.Feriennamen"/>, der Schreibweg setzt sie danach in
        /// den Bezeichner der Spiegelperiode), die weiteren als Ferienperioden des gemeinsamen Kalenders ab Rang 204
        /// (<see cref="Konditionierungsstand.Ferienliste"/>). Der Generator liest die ganze Liste und macht aus jedem
        /// Zeitraum eine FERIEN-Periode auf dessen Rang (200 … 309) mit der Ferienangabe der Größe; der Matrixbereich jedes
        /// Kalenders folgt („Matrix erneut"). Zeilen „Ferien n" der Stufe 1 im Eigenband fallen dabei.
        /// </summary>
        public static Konditionierungsschritt FerienlisteSetzen(Konditionierungsarbeitsstand stand, IReadOnlyList<Ferienzeile> ferien)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            var liste = (ferien ?? Array.Empty<Ferienzeile>()).ToList();
            int platz = Matrixeingang.FERIENZEITRAEUME + Kalendergemeinschaft.RANG_FERIENLISTE_LETZTER - Kalendergemeinschaft.RANG_FERIENLISTE + 1;
            if (liste.Count > platz)
                return Konditionierungsschritt.Fehler(Text(MyResource.Resource.KOND_MSG_BEDIENUNG_FERIEN_ZU_VIELE, platz));
            foreach (Ferienzeile f in liste)
                if (f == null || string.IsNullOrWhiteSpace(f.Name) || f.Name.Trim().Length > KonditionierungSchema.BEZEICHNER_MAX_ZEICHEN)
                    return Konditionierungsschritt.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_BEDIENUNG_WOCHE_NAME,
                        f?.Name ?? "", DbWerte.KOND_ART_FERIEN));
            var z = liste.Select(f => (Beginn: f.Beginn, Ende: f.Ende)).ToList();
            foreach ((int beginn, int ende) in z)
                if (beginn < Kalenderregel.TAG_MIN || beginn > Kalenderregel.TAG_MAX
                    || ende < Kalenderregel.TAG_MIN || ende > Kalenderregel.TAG_MAX)
                    return Konditionierungsschritt.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_PERIODE_TAG,
                        beginn.ToString(CultureInfo.InvariantCulture), ende.ToString(CultureInfo.InvariantCulture)));

            // 1. Die vier Gebäudespalten und die Ferienliste dahinter (gemeinsamer Kalender ab Rang 204); die Liste geht
            //    mit in die Bestandsfelder, aus denen der Generator die FERIEN-Perioden auf Rang 200 … 309 macht.
            Konditionierungsarbeitsstand a = stand.MitGebaeude(stand.Gebaeude.MitBestand(b =>
            {
                for (int k = 0; k < Matrixeingang.FERIENZEITRAEUME; k++)
                {
                    b.Ferienbeginn[k] = k < z.Count ? z[k].Beginn : 0.0;
                    b.Ferienende[k] = k < z.Count ? z[k].Ende : 0.0;
                }
            }));
            a = a.MitGebaeude(a.Gebaeude.MitFerienliste(liste.Count > Matrixeingang.FERIENZEITRAEUME
                ? liste.Skip(Matrixeingang.FERIENZEITRAEUME).Select(f => new Ferienzeile(f.Name.Trim(), f.Beginn, f.Ende)).ToList()
                : new List<Ferienzeile>()));
            // Die Namen der ersten vier: der Schreibweg setzt sie in den Bezeichner der Spiegelperiode (Rang 200 … 203).
            a = a.MitGebaeude(a.Gebaeude.MitFeriennamen(liste.Take(Matrixeingang.FERIENZEITRAEUME).Select(f => f.Name.Trim())));

            // 2. Die Zeilen „Ferien n" der Stufe 1 fallen — der Generator trägt die Ferienliste selbst.
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
                    if (perioden.Count == k.Perioden.Count) continue;
                    ebene = ebene.MitKalender(g, new Konditionierungskalender(k.Groesse, k.Grundangabe, k.Nennwert, perioden),
                                              ebene.Herkunft(g));
                }
                a = a.MitEbene(zone, ebene);
            }

            // 3. Der Matrixbereich jedes angelegten Kalenders folgt — am Gebäude und an jeder Zone.
            foreach (long? zone in Ebenenorte(a))
                foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                {
                    if (a.Ebene(zone)?.Kalender(g) == null) continue;
                    Konditionierungsschritt s = Konditionierungsarbeit.MatrixErneut(a, new Konditionierungsort(g, zone));
                    if (!s.Ok) return s;
                    a = s.Stand;
                }
            return Konditionierungsschritt.Gut(a);
        }

        // =================================================================
        //  Wochenende und Feiertagsland des Gebäudes
        // =================================================================

        /// <summary>
        /// <b>Die Wochenendtage des Gebäudes</b> (0 = Montag) aus der Spalte <c>Wochenendtage</c>; leer heißt Samstag und
        /// Sonntag (<see cref="WochenendtageVorgabe"/>).
        /// </summary>
        public static IReadOnlyList<int> Wochenendtage(Konditionierungsarbeitsstand stand)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            int maske = stand.Gebaeude.Bestand.Wochenendtage;
            return Enumerable.Range(0, 7).Where(d => KalenderbedienungSchema.IstWochenendtag(maske, d)).ToList();
        }

        /// <summary>
        /// <b>Das Schnellfeld „Wochenende"</b>: setzt die Wochenendtage des Gebäudes (0 = Montag … 6 = Sonntag, leer = kein
        /// Wochenende); der Matrixbereich jedes angelegten Kalenders folgt.
        /// </summary>
        public static Konditionierungsschritt WochenendeSetzen(Konditionierungsarbeitsstand stand, IEnumerable<int> tage)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            int maske = 0;
            foreach (int d in tage ?? Enumerable.Empty<int>())
            {
                if (d < 0 || d > 6)
                    return Konditionierungsschritt.Fehler(Text(MyResource.Resource.KOND_MSG_BEDIENUNG_WOCHENENDE, d));
                maske |= 1 << d;
            }
            Konditionierungsarbeitsstand a = stand.MitGebaeude(stand.Gebaeude.MitBestand(b => b.Wochenendtage = maske));
            foreach (long? zone in Ebenenorte(a))
                foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                {
                    if (a.Ebene(zone)?.Kalender(g) == null) continue;
                    Konditionierungsschritt s = Konditionierungsarbeit.MatrixErneut(a, new Konditionierungsort(g, zone));
                    if (!s.Ok) return s;
                    a = s.Stand;
                }
            return Konditionierungsschritt.Gut(a);
        }

        /// <summary>Das Feiertagsland des Gebäudes (ISO-Kürzel); <c>null</c> = nur die bundeseinheitlichen Feiertage.</summary>
        public static string Feiertagsland(Konditionierungsarbeitsstand stand)
            => (stand ?? throw new ArgumentNullException(nameof(stand))).Gebaeude.Bestand.Feiertagsland;

        /// <summary>Der Rang einer Landesregel im gemeinsamen Kalender: 109 + ihre Stelle in <see cref="DbWerte.KOND_FEIERTAGE_LAENDER"/>.</summary>
        public static int RangLandesregel(string regel)
        {
            for (int i = 0; i < DbWerte.KOND_FEIERTAGE_LAENDER.Count; i++)
                if (string.Equals(DbWerte.KOND_FEIERTAGE_LAENDER[i], regel, StringComparison.Ordinal)) return Standardfahrplan.RANG_FEIERTAG_LETZTER + 1 + i;
            return -1;
        }

        /// <summary>
        /// Ist die Gemeinschaftsperiode eine Regelperiode des Feiertagslands — Art Feiertag, eine der acht Landesregeln an
        /// ihrem Rang? Erkannt an Regel, Rang und Eigentümer (der gemeinsame Kalender des Gebäudes), nie am Text.
        /// </summary>
        public static bool IstLandesregel(Gemeinschaftsperiode p)
            => p != null && p.Regel.IstFeiertag && string.Equals(p.Regel.Art, DbWerte.KOND_ART_FEIERTAG, StringComparison.Ordinal)
               && RangLandesregel(p.Regel.Feiertagsregel) == p.Rang;

        /// <summary>
        /// <b>Das Schnellfeld „Feiertagsland"</b> (Konzept 7.8): setzt das Land am Gebäude und legt die Regelperioden seiner
        /// Landesregeln im gemeinsamen Kalender des Gebäudes an — Maske alle, „wie Sonntag", Rang 109 ff. (Rangregel 3.2:
        /// Feiertagsregeln &lt; Ferien &lt; eigene Zeilen &lt; Saison). Beim Wechsel oder Leeren fallen die Regelperioden des
        /// alten Landes (nur die, die es angelegt hat: Regel und Rang); die neun bundeseinheitlichen Regeln bleiben unberührt.
        /// </summary>
        public static Konditionierungsschritt FeiertagslandSetzen(Konditionierungsarbeitsstand stand, string land)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            string l = string.IsNullOrWhiteSpace(land) ? null : land.Trim();
            if (l != null && !Landesfeiertage.Bekannt(l))
                return Konditionierungsschritt.Fehler(Text(MyResource.Resource.KOND_MSG_BEDIENUNG_LAND, l));
            Konditionierungsstand g = stand.Gebaeude.MitBestand(b => b.Feiertagsland = l);
            var liste = g.Gemeinsam.Where(p => !IstLandesregel(p)).ToList();
            IReadOnlyList<string> namen = Landesfeiertagsnamen();
            foreach (string regel in Landesfeiertage.Regeln(l))
            {
                int rang = RangLandesregel(regel);
                if (liste.Any(p => p.Rang == rang))
                    return Konditionierungsschritt.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_RANG_BELEGT,
                        rang.ToString(CultureInfo.InvariantCulture), regel));
                int i = rang - Standardfahrplan.RANG_FEIERTAG_LETZTER - 1;
                liste.Add(new Gemeinschaftsperiode(Kalenderregel.Feiertag(rang, i < namen.Count ? namen[i] : regel, regel,
                                                                          Kalenderangabe.AlsWochentag(7)),
                                                   KalenderbedienungSchema.MASKE_ALLE));
            }
            return GemeinsamUebernehmen(stand, null, g.MitGemeinsam(liste));
        }

        /// <summary>Die acht Namen der Landesregeln in der Reihenfolge von <see cref="DbWerte.KOND_FEIERTAGE_LAENDER"/>.</summary>
        public static IReadOnlyList<string> Landesfeiertagsnamen()
        {
            string t = MyResource.Resource.KOND_TEXT_FEIERTAGE_LAENDER;
            return string.IsNullOrEmpty(t) ? Array.Empty<string>() : t.Split(';');
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
            // Stufe 2: die Landesfeiertage sind die Regelperioden des Feiertagslands im gemeinsamen Kalender des Gebäudes.
            return FeiertagslandSetzen(s.Stand, land);
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

            var auftraege = new List<(int Rang, Zuordnungsschluessel Neu, IReadOnlyDictionary<Konditionierungsgroesse, Kalenderangabe> Angaben, Zuordnungszeile Quelle)>();
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
                    auftraege.Add((z.IstGemeinsam ? z.Gemeinsam.Rang : z.Raenge.Values.Max(),
                                   Zuordnungsschluessel.Zeitraum(z.Schluessel.Name, nb, ne), z.Angaben, z));
                }
            }
            if (auftraege.Count == 0)
                return Konditionierungsschritt.Fehler(Text(MyResource.Resource.KOND_MSG_BEDIENUNG_MONAT_LEER, quellmonat));

            Konditionierungsarbeitsstand a = stand;
            foreach (var auftrag in auftraege.OrderBy(x => x.Rang))
            {
                Zuordnungszeile vorhanden = Zuordnungen(a, zone).FirstOrDefault(z => z.Schluessel == auftrag.Neu);
                Konditionierungsschritt s = auftrag.Quelle.IstGemeinsam
                    ? GemeinsamSetzen(a, zone, vorhanden, auftrag.Neu, auftrag.Quelle.Angabe, auftrag.Quelle.Maske)
                    : Gekoppelt(a, zone, vorhanden != null ? auftrag.Neu : null, auftrag.Neu, auftrag.Angaben);
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
