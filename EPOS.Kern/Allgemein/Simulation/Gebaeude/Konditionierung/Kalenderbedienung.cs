using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Schlüssel einer Zuordnungszeile</b> (Konzept Konditionierungsprofile 7.8): Name, Beginn und Ende —
    /// bei einer Feiertagsregel Name und Regel (Beginn = Ende = 0). Über ihn sind die Kopien einer Zeile in den
    /// Kalendern der Größen gekoppelt (Stufe 1 ohne gemeinsamen Eigentümer).
    /// </summary>
    /// <param name="Name">Der Bezeichner der Periode.</param>
    /// <param name="Beginn">Der erste Tag 1 … 365; 0 bei einer Feiertagsregel.</param>
    /// <param name="Ende">Der letzte Tag 1 … 365; 0 bei einer Feiertagsregel.</param>
    /// <param name="Feiertagsregel">Die Regel (eine der neun); <c>null</c> bei einem Zeitraum.</param>
    public sealed record Zuordnungsschluessel(string Name, int Beginn, int Ende, string Feiertagsregel = null)
    {
        /// <summary>Ein Zeitraum von Tag <paramref name="beginn"/> bis <paramref name="ende"/>.</summary>
        public static Zuordnungsschluessel Zeitraum(string name, int beginn, int ende)
            => new Zuordnungsschluessel((name ?? "").Trim(), beginn, ende);

        /// <summary>Eine Feiertagsregel.</summary>
        public static Zuordnungsschluessel Feiertag(string name, string regel)
            => new Zuordnungsschluessel((name ?? "").Trim(), 0, 0, regel);

        /// <summary>Ist es eine Feiertagsregel?</summary>
        public bool IstFeiertag => Feiertagsregel != null;

        /// <summary>Ein Einzeltag: Beginn = Ende oder eine Feiertagsregel.</summary>
        public bool IstEinzeltag => IstFeiertag || Beginn == Ende;

        /// <summary>Trifft der Schlüssel die Periode?</summary>
        public bool Trifft(Kalenderregel r)
            => r != null && string.Equals(r.Bezeichner, Name, StringComparison.Ordinal)
               && (IstFeiertag
                   ? string.Equals(r.Feiertagsregel, Feiertagsregel, StringComparison.Ordinal)
                   : !r.IstFeiertag && r.Beginn == Beginn && r.Ende == Ende);

        /// <summary>Der Schlüssel einer Periode.</summary>
        public static Zuordnungsschluessel Von(Kalenderregel r)
            => r.IstFeiertag ? Feiertag(r.Bezeichner, r.Feiertagsregel) : Zeitraum(r.Bezeichner, r.Beginn, r.Ende);
    }

    /// <summary>
    /// <b>Eine Zuordnungszeile</b> „von–bis → Wochenprofil, gilt für" (Konzept 7.8): die gekoppelten Kopien einer
    /// eigenen Periode in den Kalendern der Größen, mit Rang und Angabe je Größe.
    /// </summary>
    public sealed class Zuordnungszeile
    {
        internal Zuordnungszeile(Zuordnungsschluessel schluessel, int ersterTag,
                                 IReadOnlyDictionary<Konditionierungsgroesse, int> raenge,
                                 IReadOnlyDictionary<Konditionierungsgroesse, Kalenderangabe> angaben)
        {
            Schluessel = schluessel;
            ErsterTag = ersterTag;
            Raenge = raenge;
            Angaben = angaben;
        }

        /// <summary>Der Kopplungsschlüssel.</summary>
        public Zuordnungsschluessel Schluessel { get; }

        /// <summary>Der erste Tag im Bezugsjahr (bei einer Feiertagsregel aufgelöst) — die Sortierung der Liste.</summary>
        public int ErsterTag { get; }

        /// <summary>Der Rang der Kopie je Größe.</summary>
        public IReadOnlyDictionary<Konditionierungsgroesse, int> Raenge { get; }

        /// <summary>Die Angabe der Kopie je Größe.</summary>
        public IReadOnlyDictionary<Konditionierungsgroesse, Kalenderangabe> Angaben { get; }

        /// <summary>Die Größen, für die die Zeile gilt (Schemareihenfolge).</summary>
        public IReadOnlyList<Konditionierungsgroesse> GiltFuer
            => Konditionierungsgroessen.Alle.Where(g => Raenge.ContainsKey(g)).ToList();

        /// <summary>Ein Einzeltag (Beginn = Ende oder Feiertagsregel)?</summary>
        public bool IstEinzeltag => Schluessel.IstEinzeltag;

        /// <summary>Ein Ferienzeitraum ab dem fünften (Bezeichner „Ferien n")?</summary>
        public bool IstFerien => Kalenderbedienung.IstFerienzeile(Schluessel);

        /// <summary>Sprachunabhängige Kurzfassung.</summary>
        public override string ToString()
            => Schluessel.Name + " " + (Schluessel.IstFeiertag
                   ? Schluessel.Feiertagsregel
                   : Schluessel.Beginn.ToString(CultureInfo.InvariantCulture) + "…" +
                     Schluessel.Ende.ToString(CultureInfo.InvariantCulture)) +
               " · " + string.Join(",", GiltFuer.Select(Konditionierungsgroessen.Kennwort));
    }

    /// <summary>Was eine Zuordnungszeile in jeder Größe bewirkt (Konzept 7.8).</summary>
    public enum Zuordnungswirkung
    {
        /// <summary>Die Woche eines Wochenprofils derselben Größe (Name; leer = Standardwoche) — kopiert.</summary>
        Wochenprofil,

        /// <summary>„aus" über den ganzen Tag.</summary>
        Aus,

        /// <summary>„wie Wochentag X" der Standardwoche.</summary>
        WieWochentag,

        /// <summary>Ein Wert über den ganzen Tag.</summary>
        Wert,
    }

    /// <summary>Die Wirkung einer Zuordnungszeile, je Größe aufgelöst (<see cref="Kalenderbedienung.Aufloesen"/>).</summary>
    /// <param name="Wirkung">Die Art.</param>
    /// <param name="Profil">Der Name des Wochenprofils; leer = die Standardwoche.</param>
    /// <param name="Wochentag">Der Wochentag 1 = Montag … 7 = Sonntag.</param>
    /// <param name="Wert">Der Wert.</param>
    public sealed record Zuordnungsangabe(Zuordnungswirkung Wirkung, string Profil = null, int Wochentag = 7, double Wert = 0)
    {
        /// <summary>Die Woche des Wochenprofils <paramref name="name"/> (leer = Standardwoche).</summary>
        public static Zuordnungsangabe Profilwoche(string name) => new(Zuordnungswirkung.Wochenprofil, name);

        /// <summary>„aus".</summary>
        public static Zuordnungsangabe Abgeschaltet { get; } = new(Zuordnungswirkung.Aus);

        /// <summary>„wie Sonntag" — die Wirkung eines Feiertags.</summary>
        public static Zuordnungsangabe WieSonntag { get; } = new(Zuordnungswirkung.WieWochentag, null, 7);

        /// <summary>„wie Wochentag X" (1 = Montag … 7 = Sonntag).</summary>
        public static Zuordnungsangabe AlsWochentag(int wochentag) => new(Zuordnungswirkung.WieWochentag, null, wochentag);

        /// <summary>Ein fester Wert.</summary>
        public static Zuordnungsangabe AlsWert(double wert) => new(Zuordnungswirkung.Wert, null, 7, wert);
    }

    /// <summary>
    /// <b>Ein Wochenprofil</b> (Konzept 7.8): die Standardwoche eines Kalenders (<see cref="Rang"/> <c>null</c>) oder
    /// die Woche einer eigenen Periode. 168 Zellen, NaN = „aus".
    /// </summary>
    public sealed record Wochenprofil(Konditionierungsgroesse Groesse, int? Rang, string Name, double[] Werte)
    {
        /// <summary>Ist es die Standardwoche?</summary>
        public bool IstStandardwoche => !Rang.HasValue;
    }

    /// <summary>Wo ein Wochenprofil steht: Ort (Größe, Zone) und Rang der Periode; <c>null</c> = Standardwoche.</summary>
    public sealed record Profilort(Konditionierungsort Ort, int? Rang = null);

    /// <summary>
    /// <b>Die Kalenderbedienung, Stufe 1</b> (Konzept Konditionierungsprofile 7.8, E110): Wochenprofile, gekoppelte
    /// Zuordnungszeilen, Einzeltage, Schnellfelder, Kopieren und Jahresraster — rein über dem Arbeitsstand wie
    /// <see cref="Konditionierungsarbeit"/>, ohne Datenbank und ohne Oberfläche.
    /// </summary>
    /// <remarks>
    /// <para><b>Das heutige Modell, kein Schemaschritt.</b> Ein Wochenprofil ist die Standardwoche oder die Woche
    /// einer eigenen Periode; eine Zuordnungszeile ist je Größe eine Periode im Eigenband 310 … 899, die Kopien sind
    /// über <see cref="Zuordnungsschluessel"/> (Name + Beginn + Ende bzw. Regel) gekoppelt; ein Einzeltag ist eine
    /// Periode mit Beginn = Ende oder eine Feiertagsregel. Jede Änderung geht über die Werkzeuge der Karte
    /// (<see cref="Kalenderwerkzeuge"/>) und ihre Prüfungen; geschrieben wird allein im OK-Weg des Editors.</para>
    /// <para><b>Rangregel</b> (Konzept 3.2, unverändert): Der höhere Rang gewinnt — Feiertagsregeln 100 … 108 unter
    /// den Ferien 1–4 (200 … 203), darüber die eigenen Zeilen 310 … 899, darüber die Saison 900. Eine neue Zeile kommt
    /// über die ranghöchste eigene (<see cref="Kalenderwerkzeuge.PeriodeSetzen"/>), Ferien ab 5 an den untersten
    /// freien Platz des Eigenbands.</para>
    /// </remarks>
    public static partial class Kalenderbedienung
    {
        /// <summary>Die Wochenendtage (0 = Montag): fest Samstag und Sonntag; wählbar erst mit Stufe 2.</summary>
        public static IReadOnlyList<int> Wochenendtage { get; } = new[] { 5, 6 };

        // =================================================================
        //  Wochenprofile
        // =================================================================

        /// <summary>
        /// <b>Die Wochenprofile einer Größe</b> am Ort: die Standardwoche zuerst, dann die Wochen der eigenen Perioden
        /// nach absteigendem Rang. Ohne angelegten Kalender die Profile des Kalenders, der dort gilt (nur lesbar —
        /// jede Änderung verlangt einen angelegten).
        /// </summary>
        public static IReadOnlyList<Wochenprofil> Wochenprofile(Konditionierungsarbeitsstand stand, Konditionierungsort ort)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (ort == null) throw new ArgumentNullException(nameof(ort));
            Konditionierungskalender k = stand.Ebene(ort.Zone)?.Kalender(ort.Groesse) ?? stand.Ansichtskalender(ort.Groesse, ort.Zone);
            var liste = new List<Wochenprofil>();
            if (k == null) return liste;
            liste.Add(new Wochenprofil(ort.Groesse, null, MyResource.Resource.KOND_TEXT_BEDIENUNG_STANDARDWOCHE,
                                       Kalenderwerkzeuge.WocheAus(k)));
            foreach (Kalenderregel r in k.Perioden)
                if (r.Angabe.Art == Angabeart.Woche && !Konditionierungsarbeit.IstMatrixbereich(r))
                    liste.Add(new Wochenprofil(ort.Groesse, r.Rang, r.Bezeichner, r.Angabe.Woche.ToArray()));
            return liste;
        }

        /// <summary>Das Wochenprofil am Profilort; <c>null</c>, wenn es keines gibt.</summary>
        public static Wochenprofil Profil(Konditionierungsarbeitsstand stand, Profilort p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            return Wochenprofile(stand, p.Ort).FirstOrDefault(w => w.Rang == p.Rang);
        }

        /// <summary>
        /// <b>Ein Wochenprofil schreiben</b>: die Standardwoche (<see cref="Kalenderwerkzeuge.Standardwoche"/>) oder die
        /// Woche der eigenen Periode mit dem Rang — Name, Tage und Art bleiben. Eine Periode des Matrixbereichs
        /// (Ferien 1–4, Saison) folgt der Matrix und wird benannt abgelehnt.
        /// </summary>
        public static Konditionierungsschritt WochenprofilSetzen(Konditionierungsarbeitsstand stand, Profilort p,
                                                                 IReadOnlyList<double> werte)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (p == null) throw new ArgumentNullException(nameof(p));
            if (werte == null || werte.Count != Kalenderwoche.WOCHENWERTE)
                return Konditionierungsschritt.Fehler(Text(MyResource.Resource.KOND_MSG_BEDIENUNG_WOCHE, werte?.Count ?? 0));
            return AmKalender(stand, p.Ort, k =>
            {
                if (!p.Rang.HasValue) return Kalenderwerkzeuge.Standardwoche(k, werte);
                Kalenderregel r = k.Perioden.FirstOrDefault(x => x.Rang == p.Rang.Value);
                if (r == null)
                    return Kalenderwerkzeuge.Werkzeugbefund.Fehler(Text(MyResource.Resource.KOND_MSG_PERIODE_FEHLT, p.Rang.Value));
                if (Konditionierungsarbeit.IstMatrixbereich(r))
                    return Kalenderwerkzeuge.Werkzeugbefund.Fehler(string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KOND_MSG_PERIODE_MATRIXBEREICH, r.Bezeichner));
                return Kalenderwerkzeuge.PeriodeSetzen(k, r.Rang, r.Art, r.Bezeichner, r.Beginn, r.Ende, r.Feiertagsregel,
                                                       Kalenderangabe.AusWoche(werte));
            });
        }

        /// <summary>
        /// <b>Der Pinsel</b>: setzt die Zellen der Wochentage <paramref name="tagVon"/> … <paramref name="tagBis"/>
        /// (0 = Montag) und der Stunden <paramref name="stundeVon"/> (eingeschlossen) bis <paramref name="stundeBis"/>
        /// (ausgeschlossen, 1 … 24) auf den Wert — NaN heißt „aus". Liefert eine neue Woche; <c>null</c> bei Zellen
        /// außerhalb des Rasters.
        /// </summary>
        public static double[] Pinsel(IReadOnlyList<double> werte, int tagVon, int tagBis, int stundeVon, int stundeBis, double wert)
        {
            if (werte == null || werte.Count != Kalenderwoche.WOCHENWERTE) return null;
            if (tagVon < 0 || tagBis > 6 || tagVon > tagBis || stundeVon < 0 || stundeBis > Kalenderwoche.TAGESSTUNDEN
                || stundeVon >= stundeBis) return null;
            double[] neu = werte.ToArray();
            for (int t = tagVon; t <= tagBis; t++)
                for (int s = stundeVon; s < stundeBis; s++)
                    neu[Kalenderwoche.Stelle(t, s)] = wert;
            return neu;
        }

        /// <summary>Eine Tagesspalte (0 = Montag) auf andere Wochentage; <c>null</c> bei einem Tag außerhalb 0 … 6.</summary>
        public static double[] TagKopieren(IReadOnlyList<double> quelle, int quelltag, IReadOnlyList<double> ziel,
                                           IEnumerable<int> zieltage)
        {
            if (quelle == null || ziel == null || quelle.Count != Kalenderwoche.WOCHENWERTE
                || ziel.Count != Kalenderwoche.WOCHENWERTE || quelltag < 0 || quelltag > 6) return null;
            double[] neu = ziel.ToArray();
            foreach (int t in zieltage ?? Enumerable.Empty<int>())
            {
                if (t < 0 || t > 6) return null;
                for (int s = 0; s < Kalenderwoche.TAGESSTUNDEN; s++)
                    neu[Kalenderwoche.Stelle(t, s)] = quelle[Kalenderwoche.Stelle(quelltag, s)];
            }
            return neu;
        }

        /// <summary>
        /// <b>Gleiche Einheit</b> (Konzept 7.8): Heizen ↔ Kühlen (°C), Geräte ↔ Personen (Anteil), die Lüftung (1/h)
        /// nur zu sich. Dieselbe Größe passt immer.
        /// </summary>
        public static bool GleicheEinheit(Konditionierungsgroesse a, Konditionierungsgroesse b)
            => Einheit(a) == Einheit(b);

        private static int Einheit(Konditionierungsgroesse g) => g switch
        {
            Konditionierungsgroesse.Heizsoll or Konditionierungsgroesse.Kuehlsoll => 0,
            Konditionierungsgroesse.Lueftung => 1,
            _ => 2,
        };

        /// <summary><b>Der Pinsel als Schritt</b> auf das Wochenprofil am Profilort; <paramref name="wert"/> <c>null</c> = „aus".</summary>
        public static Konditionierungsschritt PinselAnwenden(Konditionierungsarbeitsstand stand, Profilort p, int tagVon, int tagBis,
                                                             int stundeVon, int stundeBis, double? wert)
        {
            Wochenprofil w = Profil(stand, p);
            if (w == null) return ProfilFehlt(p);
            double[] neu = Pinsel(w.Werte, tagVon, tagBis, stundeVon, stundeBis, wert ?? double.NaN);
            return neu == null
                ? Konditionierungsschritt.Fehler(MyResource.Resource.KOND_MSG_BEDIENUNG_ZELLEN)
                : WochenprofilSetzen(stand, p, neu);
        }

        /// <summary>
        /// <b>„Tag kopieren"</b>: die Spalte <paramref name="quelltag"/> des Quellprofils auf die Zieltage des
        /// Zielprofils — dasselbe Profil oder eines einer Größe gleicher Einheit.
        /// </summary>
        public static Konditionierungsschritt TagKopieren(Konditionierungsarbeitsstand stand, Profilort quelle, int quelltag,
                                                          Profilort ziel, IReadOnlyList<int> zieltage)
        {
            if (quelle == null) throw new ArgumentNullException(nameof(quelle));
            if (ziel == null) throw new ArgumentNullException(nameof(ziel));
            if (!GleicheEinheit(quelle.Ort.Groesse, ziel.Ort.Groesse)) return EinheitFehler(quelle, ziel);
            Wochenprofil q = Profil(stand, quelle);
            if (q == null) return ProfilFehlt(quelle);
            Wochenprofil z = Profil(stand, ziel);
            if (z == null) return ProfilFehlt(ziel);
            double[] neu = TagKopieren(q.Werte, quelltag, z.Werte, zieltage);
            return neu == null
                ? Konditionierungsschritt.Fehler(MyResource.Resource.KOND_MSG_BEDIENUNG_ZELLEN)
                : WochenprofilSetzen(stand, ziel, neu);
        }

        /// <summary><b>„Montag nach Di–Fr"</b> im Wochenprofil.</summary>
        public static Konditionierungsschritt MontagNachDienstagBisFreitag(Konditionierungsarbeitsstand stand, Profilort p)
            => TagKopieren(stand, p, 0, p, new[] { 1, 2, 3, 4 });

        /// <summary><b>„Samstag nach Sonntag"</b> im Wochenprofil.</summary>
        public static Konditionierungsschritt SamstagNachSonntag(Konditionierungsarbeitsstand stand, Profilort p)
            => TagKopieren(stand, p, 5, p, new[] { 6 });

        /// <summary>
        /// <b>„Woche kopieren"</b> in ein anderes Profil derselben Größe oder einer Größe gleicher Einheit; die
        /// Grenzen der Zielgröße prüft der Schreibweg (Konzept 3.6).
        /// </summary>
        public static Konditionierungsschritt WocheKopieren(Konditionierungsarbeitsstand stand, Profilort quelle, Profilort ziel)
        {
            if (quelle == null) throw new ArgumentNullException(nameof(quelle));
            if (ziel == null) throw new ArgumentNullException(nameof(ziel));
            if (!GleicheEinheit(quelle.Ort.Groesse, ziel.Ort.Groesse)) return EinheitFehler(quelle, ziel);
            Wochenprofil q = Profil(stand, quelle);
            if (q == null) return ProfilFehlt(quelle);
            if (Profil(stand, ziel) == null) return ProfilFehlt(ziel);
            return WochenprofilSetzen(stand, ziel, q.Werte);
        }

        // =================================================================
        //  Zuordnung und Einzeltage (gekoppelte Kopien)
        // =================================================================

        /// <summary>
        /// <b>Die Zuordnungszeilen am Ort</b> (Gebäude oder Zone): die eigenen Perioden aller angelegten Kalender —
        /// ohne den Matrixbereich —, gekoppelt über den Schlüssel; sortiert nach dem ersten Tag, dann nach dem Namen.
        /// </summary>
        public static IReadOnlyList<Zuordnungszeile> Zuordnungen(Konditionierungsarbeitsstand stand, long? zone)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            Konditionierungsstand ebene = stand.Ebene(zone);
            var raenge = new Dictionary<Zuordnungsschluessel, Dictionary<Konditionierungsgroesse, int>>();
            var angaben = new Dictionary<Zuordnungsschluessel, Dictionary<Konditionierungsgroesse, Kalenderangabe>>();
            var folge = new List<Zuordnungsschluessel>();
            if (ebene == null) return new List<Zuordnungszeile>();
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                Konditionierungskalender k = ebene.Kalender(g);
                if (k == null) continue;
                foreach (Kalenderregel r in k.Perioden)
                {
                    if (Konditionierungsarbeit.IstMatrixbereich(r)) continue;
                    Zuordnungsschluessel s = Zuordnungsschluessel.Von(r);
                    if (!raenge.ContainsKey(s))
                    {
                        raenge[s] = new Dictionary<Konditionierungsgroesse, int>();
                        angaben[s] = new Dictionary<Konditionierungsgroesse, Kalenderangabe>();
                        folge.Add(s);
                    }
                    if (raenge[s].ContainsKey(g)) continue;          // zwei gleiche Zeilen in einer Größe: die ranghöhere
                    raenge[s][g] = r.Rang;
                    angaben[s][g] = r.Angabe;
                }
            }
            return folge.Select(s => new Zuordnungszeile(s, ErsterTag(s, stand.Referenzjahr), raenge[s], angaben[s]))
                        .OrderBy(z => z.ErsterTag).ThenBy(z => z.Schluessel.Name, StringComparer.Ordinal)
                        .ToList();
        }

        /// <summary>
        /// <b>Eine Zuordnungszeile anlegen oder ändern</b> (Konzept 7.8): Die Zeile <paramref name="neu"/> mit der
        /// Wirkung <paramref name="angabe"/> steht danach in jeder Größe von <paramref name="giltFuer"/>
        /// (<c>null</c> = alle Größen mit angelegtem Kalender) — eine vorhandene Kopie von <paramref name="alt"/> wird
        /// an ihrem Rang ersetzt, sonst kommt die Zeile über die ranghöchste eigene; in den übrigen Größen fällt die
        /// Kopie von <paramref name="alt"/>. <paramref name="alt"/> <c>null</c> legt neu an.
        /// </summary>
        public static Konditionierungsschritt ZuordnungSetzen(Konditionierungsarbeitsstand stand, long? zone,
                                                              Zuordnungsschluessel alt, Zuordnungsschluessel neu,
                                                              Zuordnungsangabe angabe,
                                                              IEnumerable<Konditionierungsgroesse> giltFuer = null)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (neu == null) throw new ArgumentNullException(nameof(neu));
            if (angabe == null) throw new ArgumentNullException(nameof(angabe));
            Konditionierungsstand ebene = stand.Ebene(zone);
            if (ebene == null) return ZoneFehlt(zone);
            List<Konditionierungsgroesse> groessen = Groessen(ebene, giltFuer);
            if (groessen.Count == 0)
                return Konditionierungsschritt.Fehler(Text(MyResource.Resource.KOND_MSG_BEDIENUNG_KEINE_GROESSE, neu.Name));
            var angaben = new Dictionary<Konditionierungsgroesse, Kalenderangabe>();
            foreach (Konditionierungsgroesse g in groessen)
            {
                Konditionierungskalender k = ebene.Kalender(g);
                if (k == null) return KeinKalender(g);
                Kalenderangabe a = Aufloesen(k, angabe, out string fehler);
                if (a == null) return Konditionierungsschritt.Fehler(fehler);
                angaben[g] = a;
            }
            return Gekoppelt(stand, zone, alt, neu, angaben);
        }

        /// <summary><b>Eine gekoppelte Zeile löschen</b> — in allen Größen.</summary>
        public static Konditionierungsschritt ZuordnungLoeschen(Konditionierungsarbeitsstand stand, long? zone, Zuordnungsschluessel schluessel)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (schluessel == null) throw new ArgumentNullException(nameof(schluessel));
            return Gekoppelt(stand, zone, schluessel, null, new Dictionary<Konditionierungsgroesse, Kalenderangabe>());
        }

        /// <summary>
        /// <b>Ein Einzeltag</b> (Konzept 7.8): Tag <paramref name="tag"/> mit Bezeichnung und Wirkung „wie Sonntag"
        /// bzw. „aus" (<paramref name="aus"/>), gekoppelt wie eine Zuordnungszeile.
        /// </summary>
        public static Konditionierungsschritt EinzeltagSetzen(Konditionierungsarbeitsstand stand, long? zone,
                                                              Zuordnungsschluessel alt, int tag, string name, bool aus,
                                                              IEnumerable<Konditionierungsgroesse> giltFuer = null)
            => ZuordnungSetzen(stand, zone, alt, Zuordnungsschluessel.Zeitraum(name, tag, tag),
                               aus ? Zuordnungsangabe.Abgeschaltet : Zuordnungsangabe.WieSonntag, giltFuer);

        /// <summary>
        /// <b>Die Wirkung in einer Größe</b>: ein Wochenprofil wird kopiert (die Standardwoche bei leerem Namen, sonst
        /// die Woche der ranghöchsten eigenen Periode dieses Namens), „aus", „wie Wochentag" oder ein Wert; Grenzen
        /// und Wochentag prüft der Schreibweg. <c>null</c> mit <paramref name="fehler"/>, wenn das Profil fehlt.
        /// </summary>
        public static Kalenderangabe Aufloesen(Konditionierungskalender k, Zuordnungsangabe angabe, out string fehler)
        {
            if (k == null) throw new ArgumentNullException(nameof(k));
            if (angabe == null) throw new ArgumentNullException(nameof(angabe));
            fehler = null;
            switch (angabe.Wirkung)
            {
                case Zuordnungswirkung.Aus:
                    return Kalenderangabe.Abgeschaltet;
                case Zuordnungswirkung.Wert:
                    if (!double.IsFinite(angabe.Wert)) return Kalenderangabe.Abgeschaltet;
                    return Kalenderangabe.AusWert(angabe.Wert);
                case Zuordnungswirkung.WieWochentag:
                    if (angabe.Wochentag < 1 || angabe.Wochentag > 7)
                    {
                        fehler = Text(MyResource.Resource.KOND_MSG_WOCHENTAG_UNGUELTIG, angabe.Wochentag);
                        return null;
                    }
                    return Kalenderangabe.AlsWochentag(angabe.Wochentag);
                default:
                    if (string.IsNullOrWhiteSpace(angabe.Profil)) return Kalenderangabe.AusWoche(Kalenderwerkzeuge.WocheAus(k));
                    Kalenderregel r = k.Perioden.FirstOrDefault(p => p.Angabe.Art == Angabeart.Woche
                                                                     && !Konditionierungsarbeit.IstMatrixbereich(p)
                                                                     && string.Equals(p.Bezeichner, angabe.Profil.Trim(), StringComparison.Ordinal));
                    if (r != null) return Kalenderangabe.AusWoche(r.Angabe.Woche);
                    fehler = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_BEDIENUNG_PROFIL_FEHLT,
                                           angabe.Profil, Konditionierungsgroessen.Kennwort(k.Groesse));
                    return null;
            }
        }

        /// <summary>
        /// <b>Der gekoppelte Schreibweg</b>: in jeder Größe mit Angabe die Zeile <paramref name="neu"/> setzen (am Rang
        /// der Kopie von <paramref name="alt"/>, sonst neu), in jeder übrigen Größe die Kopie von
        /// <paramref name="alt"/> löschen. Ein Schlüssel, der in einer Größe schon als andere Zeile steht, wird benannt
        /// abgelehnt; ein <paramref name="alt"/>, den keine Größe führt, ebenso.
        /// </summary>
        internal static Konditionierungsschritt Gekoppelt(Konditionierungsarbeitsstand stand, long? zone, Zuordnungsschluessel alt,
                                                          Zuordnungsschluessel neu,
                                                          IReadOnlyDictionary<Konditionierungsgroesse, Kalenderangabe> angaben)
        {
            Konditionierungsstand ebene = stand.Ebene(zone);
            if (ebene == null) return ZoneFehlt(zone);
            if (alt != null && !Konditionierungsgroessen.Alle.Any(g => Kopie(ebene.Kalender(g), alt) != null))
                return Konditionierungsschritt.Fehler(Text(MyResource.Resource.KOND_MSG_BEDIENUNG_ZEILE_FEHLT, alt.Name));
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                Konditionierungskalender k = ebene.Kalender(g);
                Kalenderregel bisher = alt != null ? Kopie(k, alt) : null;
                if (angaben.TryGetValue(g, out Kalenderangabe a))
                {
                    if (k == null) return KeinKalender(g);
                    Kalenderregel belegt = Kopie(k, neu);
                    if (belegt != null && (bisher == null || belegt.Rang != bisher.Rang))
                        return Konditionierungsschritt.Fehler(string.Format(CultureInfo.CurrentCulture,
                            MyResource.Resource.KOND_MSG_BEDIENUNG_ZEILE_DOPPELT, neu.Name, Konditionierungsgroessen.Kennwort(g)));
                    string art = neu.IstFeiertag ? DbWerte.KOND_ART_FEIERTAG : DbWerte.KOND_ART_ZEITRAUM;
                    Ebenenergebnis e = Konditionierungsarbeit.Werkzeug(ebene, g, kal => Kalenderwerkzeuge.PeriodeSetzen(
                        kal, bisher?.Rang, art, neu.Name, neu.Beginn, neu.Ende, neu.Feiertagsregel, a));
                    if (!e.Ok) return Konditionierungsschritt.Fehler(e.Meldung);
                    ebene = e.Stand;
                }
                else if (bisher != null)
                {
                    Ebenenergebnis e = Konditionierungsarbeit.Werkzeug(ebene, g, kal => Kalenderwerkzeuge.PeriodeLoeschen(kal, bisher.Rang));
                    if (!e.Ok) return Konditionierungsschritt.Fehler(e.Meldung);
                    ebene = e.Stand;
                }
            }
            return Konditionierungsschritt.Gut(stand.MitEbene(zone, ebene));
        }

        /// <summary>Die Kopie der Zeile im Kalender (die ranghöchste eigene Periode mit dem Schlüssel); <c>null</c> = keine.</summary>
        internal static Kalenderregel Kopie(Konditionierungskalender k, Zuordnungsschluessel s)
            => k?.Perioden.FirstOrDefault(r => !Konditionierungsarbeit.IstMatrixbereich(r) && s.Trifft(r));

        // =================================================================
        //  Helfer
        // =================================================================

        /// <summary>Ein Schritt eines Werkzeugs am angelegten Kalender des Orts.</summary>
        internal static Konditionierungsschritt AmKalender(Konditionierungsarbeitsstand stand, Konditionierungsort ort,
                                                           Func<Konditionierungskalender, Kalenderwerkzeuge.Werkzeugbefund> werkzeug)
        {
            Konditionierungsstand ebene = stand.Ebene(ort.Zone);
            if (ebene == null) return ZoneFehlt(ort.Zone);
            Ebenenergebnis e = Konditionierungsarbeit.Werkzeug(ebene, ort.Groesse, werkzeug);
            return e.Ok ? Konditionierungsschritt.Gut(stand.MitEbene(ort.Zone, e.Stand)) : Konditionierungsschritt.Fehler(e.Meldung);
        }

        /// <summary>Die gewählten Größen; <c>null</c> = alle mit angelegtem Kalender (Schemareihenfolge, ohne Doppel).</summary>
        internal static List<Konditionierungsgroesse> Groessen(Konditionierungsstand ebene, IEnumerable<Konditionierungsgroesse> giltFuer)
            => giltFuer == null
                ? Konditionierungsgroessen.Alle.Where(g => ebene.Kalender(g) != null).ToList()
                : Konditionierungsgroessen.Alle.Where(g => giltFuer.Contains(g)).ToList();

        /// <summary>Der erste Tag einer Zeile im Bezugsjahr.</summary>
        internal static int ErsterTag(Zuordnungsschluessel s, int referenzjahr)
            => s.IstFeiertag ? Feiertage.Jahrestag(s.Feiertagsregel, referenzjahr) : s.Beginn;

        internal static Konditionierungsschritt KeinKalender(Konditionierungsgroesse g)
            => Konditionierungsschritt.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_KEIN_KALENDER,
                                                            Konditionierungsgroessen.Kennwort(g)));

        internal static Konditionierungsschritt ZoneFehlt(long? zone)
            => Konditionierungsschritt.Fehler(Text(MyResource.Resource.KOND_MSG_ZONE_FEHLT,
                                                   zone.HasValue ? zone.Value.ToString(CultureInfo.InvariantCulture) : "—"));

        private static Konditionierungsschritt ProfilFehlt(Profilort p)
            => Konditionierungsschritt.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_BEDIENUNG_PROFIL_FEHLT,
                p.Rang.HasValue ? p.Rang.Value.ToString(CultureInfo.InvariantCulture) : MyResource.Resource.KOND_TEXT_BEDIENUNG_STANDARDWOCHE,
                Konditionierungsgroessen.Kennwort(p.Ort.Groesse)));

        private static Konditionierungsschritt EinheitFehler(Profilort quelle, Profilort ziel)
            => Konditionierungsschritt.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_BEDIENUNG_EINHEIT,
                Konditionierungsgroessen.Kennwort(quelle.Ort.Groesse), Konditionierungsgroessen.Kennwort(ziel.Ort.Groesse)));

        internal static string Text(string muster, object a) => string.Format(CultureInfo.CurrentCulture, muster, a);
    }
}
