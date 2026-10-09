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
    /// <b>Eine Zuordnungszeile</b> „von–bis → Wochenprofil, gilt für" (Konzept 7.8): Stufe 2 EINE Periode des gemeinsamen
    /// Kalenders mit Maske (<see cref="Gemeinsam"/>); als Lesebrücke für Altbestand, den die Migration nicht zusammengeführt
    /// hat, die gekoppelten Kopien einer eigenen Periode in den Kalendern der Größen (<see cref="IstGemeinsam"/> falsch).
    /// </summary>
    public sealed class Zuordnungszeile
    {
        internal Zuordnungszeile(Zuordnungsschluessel schluessel, int ersterTag,
                                 IReadOnlyDictionary<Konditionierungsgroesse, int> raenge,
                                 IReadOnlyDictionary<Konditionierungsgroesse, Kalenderangabe> angaben,
                                 Gemeinschaftsperiode gemeinsam = null)
        {
            Schluessel = schluessel;
            ErsterTag = ersterTag;
            Raenge = raenge;
            Angaben = angaben;
            Gemeinsam = gemeinsam;
        }

        /// <summary>Die Gemeinschaftsperiode der Zeile; <c>null</c> bei gekoppelten Kopien (Altbestand).</summary>
        public Gemeinschaftsperiode Gemeinsam { get; }

        /// <summary>Ist die Zeile eine Periode des gemeinsamen Kalenders?</summary>
        public bool IstGemeinsam => Gemeinsam != null;

        /// <summary>Die Maske „gilt für" (31 = alle); bei gekoppelten Kopien die Größen, die eine Kopie tragen.</summary>
        public int Maske => Gemeinsam?.Maske ?? Gemeinschaftsperiode.MaskeVon(Raenge.Keys);

        /// <summary>Die Angabe der Zeile (bei gekoppelten Kopien die der ersten Größe).</summary>
        public Kalenderangabe Angabe => Gemeinsam?.Regel.Angabe ?? Angaben.Values.FirstOrDefault();

        /// <summary>Der Verweis auf die benannte Woche der Angabe; <c>null</c> = keiner.</summary>
        public long? IdWoche => Angabe?.IdWoche;

        /// <summary>Der Kopplungsschlüssel.</summary>
        public Zuordnungsschluessel Schluessel { get; }

        /// <summary>Der erste Tag im Bezugsjahr (bei einer Feiertagsregel aufgelöst) — die Sortierung der Liste.</summary>
        public int ErsterTag { get; }

        /// <summary>Der Rang der Kopie je Größe.</summary>
        public IReadOnlyDictionary<Konditionierungsgroesse, int> Raenge { get; }

        /// <summary>Die Angabe der Kopie je Größe.</summary>
        public IReadOnlyDictionary<Konditionierungsgroesse, Kalenderangabe> Angaben { get; }

        /// <summary>Die Größen, für die die Zeile gilt (Schemareihenfolge) — die der Maske bzw. die mit Kopie.</summary>
        public IReadOnlyList<Konditionierungsgroesse> GiltFuer
            => Gemeinsam != null ? Gemeinschaftsperiode.Groessen(Gemeinsam.Maske)
                                 : Konditionierungsgroessen.Alle.Where(g => Raenge.ContainsKey(g)).ToList();

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
    public sealed record Wochenprofil(Konditionierungsgroesse Groesse, int? Rang, string Name, double[] Werte, long? IdWoche = null)
    {
        /// <summary>Ist es die Standardwoche?</summary>
        public bool IstStandardwoche => !Rang.HasValue && !IdWoche.HasValue;

        /// <summary>Ist es eine benannte Woche (<c>Tab_Konditionierungswoche</c>)?</summary>
        public bool IstBenannt => IdWoche.HasValue;
    }

    /// <summary>
    /// Wo ein Wochenprofil steht: Ort (Größe, Zone) und die benannte Woche <paramref name="IdWoche"/> — oder (Altbestand)
    /// der Rang einer Periode mit eingebetteter Woche; beides <c>null</c> = Standardwoche.
    /// </summary>
    public sealed record Profilort(Konditionierungsort Ort, int? Rang = null, long? IdWoche = null);

    /// <summary>
    /// <b>Die Kalenderbedienung, Stufe 2</b> (Konzept Konditionierungsprofile 7.8, E110; Schemaschritt
    /// <see cref="KalenderbedienungSchema"/>): benannte Wochen, Zuordnungszeilen als Perioden des gemeinsamen Kalenders mit
    /// Maske, Einzeltage, Schnellfelder (Wochenende, Feiertagsland, Ferienliste), Kopieren und Jahresraster — rein über
    /// dem Arbeitsstand wie <see cref="Konditionierungsarbeit"/>, ohne Datenbank und ohne Oberfläche.
    /// </summary>
    /// <remarks>
    /// <para><b>Das Modell.</b> Ein Wochenprofil ist die Standardwoche eines Größenkalenders oder eine benannte Woche
    /// (<see cref="BenannteWoche"/>); eine Zuordnungszeile ist EINE <see cref="Gemeinschaftsperiode"/> mit Maske „gilt
    /// für" (31 = alle), Ändern und Löschen wirken auf diese eine Zeile, eine abgewählte Größe verlässt die Maske. Die
    /// Größenkalender des Arbeitsstands tragen sie ausgebreitet, wie der Lauf sie liest — so prüfen die Werkzeuge der
    /// Karte (<see cref="Kalenderwerkzeuge"/>) jede Größe der Maske gegen ihre Grenzen (Konzept 3.6). Geschrieben wird
    /// allein im OK-Weg des Editors, die Gemeinschaftsperiode als eine Zeile (<see cref="KonditionierungCtrl"/>).</para>
    /// <para><b>Aufloesen und Gekoppelt bleiben als Lesebrücke.</b> Die Migration des Schritts führt gekoppelte Kopien
    /// nur zusammen, wenn ihr Rang im gemeinsamen Kalender frei ist; was dort liegen bleibt, und eine Wirkung
    /// „Standardwoche" über mehrere Größen (je Größe eine andere Woche — keine eine Zeile) bleibt als gekoppelte Kopien
    /// je Größe. Für sie gelten <see cref="Aufloesen"/> und <see cref="Gekoppelt"/> weiter; der Datenbankweg
    /// <c>Kalendergemeinschaft.Aufloesen</c>/<c>Zusammenfuehren</c> im Schreibweg entfällt, <c>Zusammenfuehren</c> bleibt
    /// allein der Migration.</para>
    /// <para><b>Rangregel</b> (Konzept 3.2, unverändert): Der höhere Rang gewinnt — Feiertagsregeln 100 … 108 unter
    /// den Ferien 1–4 (200 … 203), darüber die eigenen Zeilen 310 … 899, darüber die Saison 900. Eine neue Zeile kommt
    /// über die ranghöchste eigene (<see cref="Kalenderwerkzeuge.PeriodeSetzen"/>), Ferien ab 5 an den untersten
    /// freien Platz des Eigenbands.</para>
    /// </remarks>
    public static partial class Kalenderbedienung
    {
        /// <summary>Die Wochenendtage der Vorgabe (0 = Montag): Samstag und Sonntag.</summary>
        public static IReadOnlyList<int> WochenendtageVorgabe { get; } = new[] { 5, 6 };

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
            foreach (BenannteWoche w in stand.Ebene(ort.Zone)?.Wochen ?? Array.Empty<BenannteWoche>())
                if (w.Groesse == ort.Groesse) liste.Add(new Wochenprofil(w.Groesse, null, w.Name, w.Werte.ToArray(), w.Id));
            foreach (Kalenderregel r in k.Perioden)          // Altbestand: eingebettete Wochen eigener Perioden
                if (r.Angabe.Art == Angabeart.Woche && !r.Angabe.IdWoche.HasValue && !Konditionierungsarbeit.IstMatrixbereich(r))
                    liste.Add(new Wochenprofil(ort.Groesse, r.Rang, r.Bezeichner, r.Angabe.Woche.ToArray()));
            return liste;
        }

        /// <summary>Das Wochenprofil am Profilort; <c>null</c>, wenn es keines gibt.</summary>
        public static Wochenprofil Profil(Konditionierungsarbeitsstand stand, Profilort p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            return Wochenprofile(stand, p.Ort).FirstOrDefault(w => w.Rang == p.Rang && w.IdWoche == p.IdWoche);
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
            if (p.IdWoche.HasValue) return BenannteWocheSetzen(stand, p.Ort, p.IdWoche.Value, null, werte);
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
        //  Benannte Wochen (Tab_Konditionierungswoche)
        // =================================================================

        /// <summary>
        /// <b>Eine benannte Woche anlegen</b> (Konzept 7.8): Name eindeutig je Größe am Ort, Werte in den Grenzen der Größe
        /// (Konzept 3.6); ohne Werte die Standardwoche des Kalenders, der am Ort gilt. Die Woche bekommt eine vorläufige
        /// Id ≤ 0, die Zeile entsteht im OK-Weg.
        /// </summary>
        public static Konditionierungsschritt WocheAnlegen(Konditionierungsarbeitsstand stand, Konditionierungsort ort, string name,
                                                           IReadOnlyList<double> werte = null)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (ort == null) throw new ArgumentNullException(nameof(ort));
            Konditionierungsstand ebene = stand.Ebene(ort.Zone);
            if (ebene == null) return ZoneFehlt(ort.Zone);
            string n = (name ?? "").Trim();
            string fehler = WochennameFehler(ebene, ort.Groesse, n, null);
            if (fehler != null) return Konditionierungsschritt.Fehler(fehler);
            if (werte == null)
            {
                Konditionierungskalender k = ebene.Kalender(ort.Groesse) ?? stand.Ansichtskalender(ort.Groesse, ort.Zone);
                if (k == null) return KeinKalender(ort.Groesse);
                werte = Kalenderwerkzeuge.WocheAus(k);
            }
            fehler = WochenGrenzen(ort.Groesse, werte);
            if (fehler != null) return Konditionierungsschritt.Fehler(fehler);
            long id = Math.Min(0, ebene.Wochen.Select(w => w.Id).DefaultIfEmpty(0).Min()) - 1;
            var wochen = ebene.Wochen.ToList();
            wochen.Add(new BenannteWoche(id, ort.Groesse, n, werte.ToArray()));
            return Konditionierungsschritt.Gut(stand.MitEbene(ort.Zone, ebene.MitWochen(wochen)));
        }

        /// <summary><b>Eine benannte Woche umbenennen</b> — die Verweise bleiben (sie gehen über die Id).</summary>
        public static Konditionierungsschritt WocheUmbenennen(Konditionierungsarbeitsstand stand, Konditionierungsort ort, long idWoche,
                                                              string name)
            => BenannteWocheSetzen(stand, ort, idWoche, name ?? "", null);

        /// <summary>
        /// <b>Eine benannte Woche löschen</b> — benannt abgelehnt, solange eine Zeile auf sie verweist (Konzept 7.8; das
        /// Schema kennt keine Löschregel für <c>ID_Woche</c>).
        /// </summary>
        public static Konditionierungsschritt WocheLoeschen(Konditionierungsarbeitsstand stand, Konditionierungsort ort, long idWoche)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (ort == null) throw new ArgumentNullException(nameof(ort));
            Konditionierungsstand ebene = stand.Ebene(ort.Zone);
            if (ebene == null) return ZoneFehlt(ort.Zone);
            BenannteWoche w = ebene.Wochen.FirstOrDefault(x => x.Id == idWoche && x.Groesse == ort.Groesse);
            if (w == null) return ProfilFehlt(new Profilort(ort, null, idWoche));
            int verweise = Wochenverweise(ebene, idWoche);
            if (verweise > 0)
                return Konditionierungsschritt.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_BEDIENUNG_WOCHE_VERWIESEN,
                                                                    w.Name, verweise.ToString(CultureInfo.InvariantCulture)));
            return Konditionierungsschritt.Gut(stand.MitEbene(ort.Zone, ebene.MitWochen(ebene.Wochen.Where(x => x.Id != idWoche))));
        }

        /// <summary>Wie viele Zeilen der Ebene auf die Woche verweisen (Gemeinschaftsperioden und eigene Perioden).</summary>
        public static int Wochenverweise(Konditionierungsstand ebene, long idWoche)
        {
            if (ebene == null) return 0;
            int n = ebene.Gemeinsam.Count(p => p.Regel.Angabe.IdWoche == idWoche);
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                n += ebene.EigenerKalender(g)?.Perioden.Count(r => r.Angabe.IdWoche == idWoche) ?? 0;
            return n;
        }

        /// <summary>
        /// Name und/oder Werte einer benannten Woche ändern; jede Periode, die auf sie verweist, folgt mit den neuen Werten
        /// (in jeder Größe ihrer Maske geprüft).
        /// </summary>
        internal static Konditionierungsschritt BenannteWocheSetzen(Konditionierungsarbeitsstand stand, Konditionierungsort ort, long idWoche,
                                                                    string name, IReadOnlyList<double> werte)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            Konditionierungsstand ebene = stand.Ebene(ort.Zone);
            if (ebene == null) return ZoneFehlt(ort.Zone);
            BenannteWoche w = ebene.Wochen.FirstOrDefault(x => x.Id == idWoche && x.Groesse == ort.Groesse);
            if (w == null) return ProfilFehlt(new Profilort(ort, null, idWoche));
            string n = name == null ? w.Name : name.Trim();
            string fehler = name == null ? null : WochennameFehler(ebene, w.Groesse, n, idWoche);
            if (fehler == null && werte != null) fehler = WochenGrenzen(w.Groesse, werte);
            if (fehler != null) return Konditionierungsschritt.Fehler(fehler);
            var neu = new BenannteWoche(w.Id, w.Groesse, n, (werte ?? w.Werte).ToArray());
            Konditionierungsstand e = ebene.MitWochen(ebene.Wochen.Select(x => x.Id == idWoche ? neu : x));
            if (werte == null) return Konditionierungsschritt.Gut(stand.MitEbene(ort.Zone, e));

            Kalenderangabe angabe = Kalenderangabe.AusBenannterWoche(idWoche, neu.Werte);
            foreach (Gemeinschaftsperiode p in e.Gemeinsam.Where(p => p.Regel.Angabe.IdWoche == idWoche))
                foreach (Konditionierungsgroesse g in Gemeinschaftsperiode.Groessen(p.Maske))
                {
                    string grenze = e.Kalender(g) == null ? null : WochenGrenzen(g, neu.Werte);
                    if (grenze != null) return Konditionierungsschritt.Fehler(grenze);
                }
            e = e.MitGemeinsam(e.Gemeinsam.Select(p => p.Regel.Angabe.IdWoche == idWoche
                ? new Gemeinschaftsperiode(MitAngabe(p.Regel, angabe), p.Maske) : p).ToList());
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                Konditionierungskalender k = e.Kalender(g);
                if (k == null || !k.Perioden.Any(r => r.Angabe.IdWoche == idWoche && !Kalendervergleich.AngabeGleich(r.Angabe, angabe))) continue;
                string grenze = WochenGrenzen(g, neu.Werte);
                if (grenze != null) return Konditionierungsschritt.Fehler(grenze);
                e = e.MitKalender(g, new Konditionierungskalender(k.Groesse, k.Grundangabe, k.Nennwert,
                    k.Perioden.Select(r => r.Angabe.IdWoche == idWoche ? MitAngabe(r, angabe) : r).ToList()), e.Herkunft(g));
            }
            return Konditionierungsschritt.Gut(stand.MitEbene(ort.Zone, e));
        }

        /// <summary>Der Fehltext eines Wochennamens (leer, zu lang, je Größe schon vergeben); <c>null</c> = gut.</summary>
        private static string WochennameFehler(Konditionierungsstand ebene, Konditionierungsgroesse g, string name, long? ausser)
        {
            bool vergeben = ebene.Wochen.Any(w => w.Groesse == g && w.Id != ausser && string.Equals(w.Name, name, StringComparison.Ordinal));
            return string.IsNullOrEmpty(name) || name.Length > KonditionierungSchema.BEZEICHNER_MAX_ZEICHEN || vergeben
                ? string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_BEDIENUNG_WOCHE_NAME, name ?? "",
                                Konditionierungsgroessen.Kennwort(g))
                : null;
        }

        /// <summary>Die Grenzprüfung einer Woche in der Größe (Konzept 3.6) über das Werkzeug der Standardwoche; <c>null</c> = gut.</summary>
        internal static string WochenGrenzen(Konditionierungsgroesse g, IReadOnlyList<double> werte)
        {
            if (werte == null || werte.Count != Kalenderwoche.WOCHENWERTE) return Text(MyResource.Resource.KOND_MSG_BEDIENUNG_WOCHE, werte?.Count ?? 0);
            var probe = new Konditionierungskalender(g, Kalenderangabe.Abgeschaltet, null, Array.Empty<Kalenderregel>());
            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.Standardwoche(probe, werte);
            return b.Ok ? null : b.Meldung;
        }

        /// <summary>Dieselbe Regel mit anderer Angabe.</summary>
        internal static Kalenderregel MitAngabe(Kalenderregel r, Kalenderangabe a)
            => r.IstFeiertag ? Kalenderregel.Feiertag(r.Rang, r.Bezeichner, r.Feiertagsregel, a)
                             : Kalenderregel.Zeitraum(r.Rang, r.Art, r.Bezeichner, r.Beginn, r.Ende, a);

        // =================================================================
        //  Zuordnung und Einzeltage (Gemeinschaftsperioden; gekoppelte Kopien als Lesebrücke)
        // =================================================================

        /// <summary>
        /// <b>Die Zuordnungszeilen am Ort</b> (Gebäude oder Zone): die Perioden des gemeinsamen Kalenders — ohne den
        /// Matrixbereich — und, als Lesebrücke, die gekoppelten Kopien eigener Perioden; sortiert nach dem ersten Tag,
        /// dann nach dem Namen.
        /// </summary>
        public static IReadOnlyList<Zuordnungszeile> Zuordnungen(Konditionierungsarbeitsstand stand, long? zone)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            Konditionierungsstand ebene = stand.Ebene(zone);
            var liste = new List<Zuordnungszeile>();
            if (ebene == null) return liste;
            foreach (Gemeinschaftsperiode p in ebene.Gemeinsam)
            {
                if (Konditionierungsarbeit.IstMatrixbereich(p.Regel)) continue;
                var raenge = new Dictionary<Konditionierungsgroesse, int>();
                var angaben = new Dictionary<Konditionierungsgroesse, Kalenderangabe>();
                foreach (Konditionierungsgroesse g in Gemeinschaftsperiode.Groessen(p.Maske))
                    if (ebene.Kalender(g) != null) { raenge[g] = p.Rang; angaben[g] = p.Regel.Angabe; }
                Zuordnungsschluessel s = Zuordnungsschluessel.Von(p.Regel);
                liste.Add(new Zuordnungszeile(s, ErsterTag(s, stand.Kalender), raenge, angaben, p));
            }
            liste.AddRange(GekoppelteZuordnungen(ebene, stand.Kalender));
            return liste.OrderBy(z => z.ErsterTag).ThenBy(z => z.Schluessel.Name, StringComparer.Ordinal).ToList();
        }

        /// <summary>Die gekoppelten Kopien eigener Perioden (Stufe 1, Altbestand) als Zuordnungszeilen.</summary>
        private static IEnumerable<Zuordnungszeile> GekoppelteZuordnungen(Konditionierungsstand ebene, Gemeinjahrkalender kalender)
        {
            var raenge = new Dictionary<Zuordnungsschluessel, Dictionary<Konditionierungsgroesse, int>>();
            var angaben = new Dictionary<Zuordnungsschluessel, Dictionary<Konditionierungsgroesse, Kalenderangabe>>();
            var folge = new List<Zuordnungsschluessel>();
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                Konditionierungskalender k = ebene.EigenerKalender(g);
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
            return folge.Select(s => new Zuordnungszeile(s, ErsterTag(s, kalender), raenge[s], angaben[s]));
        }

        /// <summary>
        /// <b>Eine Zuordnungszeile anlegen oder ändern</b> (Konzept 7.8): Die Zeile <paramref name="neu"/> mit der Wirkung
        /// <paramref name="angabe"/> wird EINE Periode des gemeinsamen Kalenders mit der Maske <paramref name="giltFuer"/>
        /// (<c>null</c> = alle, 31). Die Zeile <paramref name="alt"/> wird an ihrem Rang ersetzt, sonst kommt die neue über
        /// die ranghöchste eigene; eine abgewählte Größe verlässt die Maske. Eine Wirkung „Standardwoche" über mehrere
        /// Größen (je Größe eine andere Woche) und ein Profil ohne benannte Woche bleiben gekoppelte Kopien (Lesebrücke).
        /// <paramref name="alt"/> <c>null</c> legt neu an.
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
            List<Konditionierungsgroesse> auswahl = giltFuer?.ToList();
            int maske = Gemeinschaftsperiode.MaskeVon(auswahl);
            if (maske == 0)
                return Konditionierungsschritt.Fehler(Text(MyResource.Resource.KOND_MSG_BEDIENUNG_KEINE_GROESSE, neu.Name));
            Zuordnungszeile bisher = null;
            if (alt != null)
            {
                bisher = Zuordnungen(stand, zone).FirstOrDefault(z => z.Schluessel == alt);
                if (bisher == null) return Konditionierungsschritt.Fehler(Text(MyResource.Resource.KOND_MSG_BEDIENUNG_ZEILE_FEHLT, alt.Name));
            }
            Kalenderangabe a = GemeinsameAngabe(ebene, angabe, maske, out string fehler, out bool gekoppelt);
            if (fehler != null) return Konditionierungsschritt.Fehler(fehler);
            if (!gekoppelt) return GemeinsamSetzen(stand, zone, bisher, neu, a, maske);

            // Lesebrücke: je Größe eine eigene Kopie (Stufe 1). Eine bisherige Gemeinschaftsperiode fällt vorher.
            Konditionierungsarbeitsstand a0 = stand;
            if (bisher != null && bisher.IstGemeinsam)
            {
                a0 = stand.MitEbene(zone, ebene.MitGemeinsam(ebene.Gemeinsam.Where(p => p.Rang != bisher.Gemeinsam.Rang)));
                ebene = a0.Ebene(zone);
            }
            List<Konditionierungsgroesse> groessen = Groessen(ebene, auswahl);
            if (groessen.Count == 0)
                return Konditionierungsschritt.Fehler(Text(MyResource.Resource.KOND_MSG_BEDIENUNG_KEINE_GROESSE, neu.Name));
            var angaben = new Dictionary<Konditionierungsgroesse, Kalenderangabe>();
            foreach (Konditionierungsgroesse g in groessen)
            {
                Konditionierungskalender k = ebene.Kalender(g);
                if (k == null) return KeinKalender(g);
                Kalenderangabe ag = Aufloesen(k, angabe, out string f);
                if (ag == null) return Konditionierungsschritt.Fehler(f);
                angaben[g] = ag;
            }
            return Gekoppelt(a0, zone, bisher != null && !bisher.IstGemeinsam ? alt : null, neu, angaben);
        }

        /// <summary>
        /// Die EINE Angabe einer Gemeinschaftsperiode: „aus", Wert, „wie Wochentag", die benannte Woche des Namens (in einer
        /// Größe der Maske) oder — bei genau einer Größe mit Kalender — deren Standardwoche. Sonst
        /// <paramref name="gekoppelt"/> (je Größe eine andere Woche).
        /// </summary>
        private static Kalenderangabe GemeinsameAngabe(Konditionierungsstand ebene, Zuordnungsangabe angabe, int maske,
                                                       out string fehler, out bool gekoppelt)
        {
            fehler = null;
            gekoppelt = false;
            switch (angabe.Wirkung)
            {
                case Zuordnungswirkung.Aus: return Kalenderangabe.Abgeschaltet;
                case Zuordnungswirkung.Wert:
                    return double.IsFinite(angabe.Wert) ? Kalenderangabe.AusWert(angabe.Wert) : Kalenderangabe.Abgeschaltet;
                case Zuordnungswirkung.WieWochentag:
                    if (angabe.Wochentag < 1 || angabe.Wochentag > 7)
                    {
                        fehler = Text(MyResource.Resource.KOND_MSG_WOCHENTAG_UNGUELTIG, angabe.Wochentag);
                        return null;
                    }
                    return Kalenderangabe.AlsWochentag(angabe.Wochentag);
            }
            IReadOnlyList<Konditionierungsgroesse> groessen = Gemeinschaftsperiode.Groessen(maske);
            if (string.IsNullOrWhiteSpace(angabe.Profil))
            {
                var mitKalender = groessen.Where(g => ebene.Kalender(g) != null).ToList();
                if (mitKalender.Count == 1) return Kalenderangabe.AusWoche(Kalenderwerkzeuge.WocheAus(ebene.Kalender(mitKalender[0])));
                gekoppelt = true;
                return null;
            }
            BenannteWoche w = ebene.Wochen.Where(x => groessen.Contains(x.Groesse))
                                   .FirstOrDefault(x => string.Equals(x.Name, angabe.Profil.Trim(), StringComparison.Ordinal));
            if (w != null) return Kalenderangabe.AusBenannterWoche(w.Id, w.Werte);
            gekoppelt = true;
            return null;
        }

        /// <summary>
        /// <b>Der Schreibweg der Gemeinschaftsperiode</b>: geprüft je Größe der Maske mit Kalender über das Werkzeug der
        /// Karte (Grenzen, Tage, Regel), am Rang der bisherigen Zeile oder über der ranghöchsten eigenen; eine gekoppelte
        /// bisherige Zeile fällt in allen Größen.
        /// </summary>
        internal static Konditionierungsschritt GemeinsamSetzen(Konditionierungsarbeitsstand stand, long? zone, Zuordnungszeile bisher,
                                                                Zuordnungsschluessel neu, Kalenderangabe angabe, int maske)
        {
            Konditionierungsstand ebene = stand.Ebene(zone);
            if (ebene == null) return ZoneFehlt(zone);
            IReadOnlyList<Konditionierungsgroesse> groessen = Gemeinschaftsperiode.Groessen(maske);
            if (Zuordnungen(stand, zone).Any(z => z.Schluessel == neu && (bisher == null || z.Schluessel != bisher.Schluessel)))
                return Konditionierungsschritt.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_BEDIENUNG_ZEILE_DOPPELT,
                    neu.Name, Konditionierungsgroessen.Kennwort(groessen[0])));
            Konditionierungsarbeitsstand a0 = stand;
            if (bisher != null && !bisher.IstGemeinsam)
            {
                Konditionierungsschritt s = Gekoppelt(stand, zone, bisher.Schluessel, null, new Dictionary<Konditionierungsgroesse, Kalenderangabe>());
                if (!s.Ok) return s;
                a0 = s.Stand;
                ebene = a0.Ebene(zone);
            }
            string art = neu.IstFeiertag ? DbWerte.KOND_ART_FEIERTAG : DbWerte.KOND_ART_ZEITRAUM;
            foreach (Konditionierungsgroesse g in groessen)
            {
                Konditionierungskalender k = ebene.EigenerKalender(g);
                if (k == null) continue;
                Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.PeriodeSetzen(k, null, art, neu.Name, neu.Beginn, neu.Ende,
                                                                                     neu.Feiertagsregel, angabe);
                if (!b.Ok) return Konditionierungsschritt.Fehler(b.Meldung);
            }
            int rang = bisher?.Gemeinsam?.Rang ?? NeuerRang(ebene);
            if (rang < 0)
                return Konditionierungsschritt.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_RANG_BAND_VOLL,
                    Standardfahrplan.RANG_EIGEN.ToString(CultureInfo.InvariantCulture),
                    Standardfahrplan.RANG_EIGEN_LETZTER.ToString(CultureInfo.InvariantCulture)));
            Kalenderregel regel = neu.IstFeiertag
                ? Kalenderregel.Feiertag(rang, neu.Name, neu.Feiertagsregel, angabe)
                : Kalenderregel.Zeitraum(rang, art, neu.Name, neu.Beginn, neu.Ende, angabe);
            var liste = ebene.Gemeinsam.Where(p => bisher?.Gemeinsam == null || p.Rang != bisher.Gemeinsam.Rang).ToList();
            liste.Add(new Gemeinschaftsperiode(regel, maske));
            return GemeinsamUebernehmen(a0, zone, ebene.MitGemeinsam(liste));
        }

        /// <summary>Übernimmt die Ebene, wenn kein Kalender über <see cref="Kalenderregel.PERIODEN_MAX"/> Perioden trägt.</summary>
        internal static Konditionierungsschritt GemeinsamUebernehmen(Konditionierungsarbeitsstand stand, long? zone, Konditionierungsstand ebene)
        {
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                int n = ebene.Kalender(g)?.Perioden.Count ?? 0;
                if (n > Kalenderregel.PERIODEN_MAX)
                    return Konditionierungsschritt.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_PERIODEN_ZU_VIELE,
                        n.ToString(CultureInfo.InvariantCulture), Kalenderregel.PERIODEN_MAX.ToString(CultureInfo.InvariantCulture)));
            }
            return Konditionierungsschritt.Gut(stand.MitEbene(zone, ebene));
        }

        /// <summary>
        /// Der Rang einer neuen Zeile im Eigenband 310 … 899: über der ranghöchsten eigenen in allen Größen und im gemeinsamen
        /// Kalender, ist das Band oben voll, der unterste freie Platz; −1, wenn keiner frei ist.
        /// </summary>
        private static int NeuerRang(Konditionierungsstand ebene)
        {
            var belegt = new HashSet<int>(ebene.Gemeinsam.Select(p => p.Rang));
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                foreach (Kalenderregel r in ebene.Kalender(g)?.Perioden ?? (IReadOnlyList<Kalenderregel>)Array.Empty<Kalenderregel>())
                    belegt.Add(r.Rang);
            int hoechster = belegt.Where(r => r >= Standardfahrplan.RANG_EIGEN && r <= Standardfahrplan.RANG_EIGEN_LETZTER)
                                  .DefaultIfEmpty(Standardfahrplan.RANG_EIGEN - 1).Max();
            if (hoechster < Standardfahrplan.RANG_EIGEN_LETZTER) return hoechster + 1;
            for (int r = Standardfahrplan.RANG_EIGEN; r <= Standardfahrplan.RANG_EIGEN_LETZTER; r++)
                if (!belegt.Contains(r)) return r;
            return -1;
        }

        /// <summary><b>Eine Zeile löschen</b> — die Gemeinschaftsperiode bzw. die gekoppelten Kopien in allen Größen.</summary>
        public static Konditionierungsschritt ZuordnungLoeschen(Konditionierungsarbeitsstand stand, long? zone, Zuordnungsschluessel schluessel)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (schluessel == null) throw new ArgumentNullException(nameof(schluessel));
            Konditionierungsstand ebene = stand.Ebene(zone);
            if (ebene == null) return ZoneFehlt(zone);
            Zuordnungszeile z = Zuordnungen(stand, zone).FirstOrDefault(x => x.Schluessel == schluessel);
            if (z == null) return Konditionierungsschritt.Fehler(Text(MyResource.Resource.KOND_MSG_BEDIENUNG_ZEILE_FEHLT, schluessel.Name));
            if (!z.IstGemeinsam)
                return Gekoppelt(stand, zone, schluessel, null, new Dictionary<Konditionierungsgroesse, Kalenderangabe>());
            return Konditionierungsschritt.Gut(stand.MitEbene(zone, ebene.MitGemeinsam(ebene.Gemeinsam.Where(p => p.Rang != z.Gemeinsam.Rang))));
        }

        /// <summary>
        /// <b>Ein Einzeltag</b> (Konzept 7.8): Tag <paramref name="tag"/> mit Bezeichnung und Wirkung „wie Sonntag"
        /// bzw. „aus" (<paramref name="aus"/>), als Zuordnungszeile.
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

        /// <summary>Der erste Tag einer Zeile im Gemeinjahr (Feiertage nach der Konvention, E114).</summary>
        internal static int ErsterTag(Zuordnungsschluessel s, Gemeinjahrkalender kalender)
            => s.IstFeiertag ? Feiertage.Jahrestag(s.Feiertagsregel, kalender) : s.Beginn;

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
