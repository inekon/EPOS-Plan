using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE ZEHN NEUEN VORLAGEN DER KAELTEBEDARFSPROFILE - Welle KBP-2, Konzept Kaeltebedarfsprofile 6.2 (Entscheid OE-3).
    //
    // WAS. Zehn ausgelieferte Saetze hinter dem Bestand 1-6 (KaeltetypSaat, bleibt unveraendert): je ein Kopfsatz in
    // Tab_Kaeltebedarf_STAMM (zwoelf Monatswerte, Jahresmenge 100 MWh, Temperaturpaar als ANGABE, Art-Vorschlag
    // Kaelte_Art) und ein gleichnamiges Wochenprofil in Tab_Kaeltetyp_STAMM (168 Stundenwerte), beide ReadOnly = 1, mit
    // ausdruecklicher ID 7 bis 16. Neutral benannt, runde Werte - keine Hersteller-, Produkt- oder Messdaten.
    //
    // DIE ZAHLEN.
    //   Wochenprofil  relative Last, 1 = Spitze; die Profilroutine normiert je Monat auf den Monatswert.
    //   Grundlast     der Wochenwert AUSSERHALB der Nutzungszeit relativ zur Spitze (Konzept 6.2, Spalte Grundlast-Anteil);
    //                 bei den Vorlagen mit Nachtwert (Krankenhaus) und den Raumkuehlungen mit Jahresgang steht er als
    //                 Wintermonatsfaktor - je Vorlage im Kommentar genannt.
    //   Monatswerte   Q_M(m) = 100 MWh * f(m) * d(m) / Summe f(k) * d(k), zusammen 100 (wie KaeltetypSaat).
    //
    // QUELLEN (je Vorlage im Kommentar). Nutzungszeit und Nutzungstage nach den Nutzungsprofilen der
    // DIN V 18599-10:2018-09 (Nichtwohngebaeude; abgeloest durch DIN/TS 18599-10:2025-10, Nummern dort in Klammern) -
    // vor der Auslieferung gegen die gueltige Ausgabe pruefen ("Ausgabe pruefen"). Tagesgang, Monatsverteilung und
    // Grundlast folgen keiner Normtabelle: Sie sind ausgewiesene Richtwerte ("Richtwert, Quelle prüfen"); der
    // Verlauf aus inneren und aeusseren Lasten ist nur qualitativ nach VDI 2078 gebildet, VDI 3807 dient allein der
    // Plausibilisierung des Jahresverbrauchs, nicht der Form. Keine Genauigkeit, die die Quelle nicht traegt.
    //
    // WIEDERHOLBAR UND NIE UEBERSCHREIBEND. Schluessel ist der Bezeichner je Tabelle: Steht er schon (gesaet oder
    // eigen), bleibt die Zeile. Fehlt er, wird mit der vorgesehenen ID geschrieben, wenn sie frei ist, sonst mit
    // vergebener ID (eine eigene Anwenderzeile kann die ID belegen). Kein UPDATE. Je Satz ein Vorgang.
    //
    // ERGEBNISNEUTRAL. Kein Referenzprojekt ordnet einen dieser Saetze zu; der Referenzlauf bleibt byte-gleich.
    // ====================================================================================

    /// <summary>Eine ausgelieferte Vorlage eines Kältebedarfsprofils ab ID 7 (Konzept Kältebedarfsprofile 6.2).</summary>
    public sealed class Kaeltevorlage
    {
        internal Kaeltevorlage(int id, string name, string kurz, string art, double vorlauf, double ruecklauf,
                               double grundlast, double[] woche, double[] monatsfaktoren, string quelle)
        {
            Id = id;
            Name = name;
            Kurz = kurz;
            Art = art;
            Vorlauf = vorlauf;
            Ruecklauf = ruecklauf;
            Grundlast = grundlast;
            Woche = woche;
            Monatsfaktoren = monatsfaktoren;
            Quelle = quelle;
        }

        /// <summary>Die vorgesehene ID in beiden Katalogen.</summary>
        public int Id { get; }

        /// <summary>Der Bezeichner — derselbe im Kopfsatz, als Typ und im Wochenprofil.</summary>
        public string Name { get; }

        /// <summary>Der Bedarf in einem Satz (ohne den Vermerk).</summary>
        public string Kurz { get; }

        /// <summary>Der Art-Vorschlag (<see cref="KaelteArtSchema.RAUM"/> oder <see cref="KaelteArtSchema.PROZESS"/>).</summary>
        public string Art { get; }

        /// <summary>Vorlauf [°C] — Angabe ohne Wirkung.</summary>
        public double Vorlauf { get; }

        /// <summary>Rücklauf [°C] — Angabe ohne Wirkung.</summary>
        public double Ruecklauf { get; }

        /// <summary>Der Grundlast-Anteil relativ zur Spitze (Konzept 6.2).</summary>
        public double Grundlast { get; }

        /// <summary>Das Wochenprofil, 168 Werte, Index 0 = Montag 0 Uhr.</summary>
        public double[] Woche { get; }

        /// <summary>Die zwölf Monatsfaktoren.</summary>
        public double[] Monatsfaktoren { get; }

        /// <summary>Die Belegquelle in Kurzform (Norm und Profil bzw. „Richtwert, Quelle prüfen").</summary>
        public string Quelle { get; }

        /// <summary>Die Beschreibung des Satzes samt Vermerk.</summary>
        public string Beschreibung => Kurz + " " + KaeltevorlagenSaat.VERMERK;

        /// <summary>Die zwölf Monatswerte [MWh], zusammen <see cref="KaeltetypSaat.JAHRESMENGE_MWH"/>.</summary>
        public double[] Monatswerte()
        {
            double nenner = 0;
            for (int m = 0; m < 12; m++) nenner += Monatsfaktoren[m] * KaeltetypSaat.TAGE[m];
            var w = new double[12];
            for (int m = 0; m < 12; m++)
                w[m] = KaeltetypSaat.JAHRESMENGE_MWH * Monatsfaktoren[m] * KaeltetypSaat.TAGE[m] / nenner;
            return w;
        }

        public override string ToString() => Name;
    }

    /// <summary>
    /// <b>Die Saat der zehn neuen Kältebedarfsvorlagen</b> (IDs 7–16) — Werte, Quellen und Regeln stehen im Kopf der
    /// Datei. Gerufen im Schemaschritt <see cref="KaeltebedarfsprofilVorlagenSchema"/> (Migration, Werkzeug, Testvorrichtung).
    /// </summary>
    public static class KaeltevorlagenSaat
    {
        /// <summary>Der Vermerk jeder Beschreibung.</summary>
        public const string VERMERK = "Formmodell mit Richtwerten, keine Messung; nur sensible Kaelte; Jahresmenge 100 MWh, im Projekt über die Jahressumme skalieren.";

        /// <summary>Der Vermerk der Vorlagen mit Ferien (Konzept 6.2, Doppelzählung vermeiden).</summary>
        public const string VERMERK_FERIEN = "Ferien und Feiertage nicht in den Monatswerten: Ferien über den Betriebskalender wählen.";

        /// <summary>Die erste ID der neuen Vorlagen (hinter dem Bestand 1–6).</summary>
        public const int ERSTE_ID = 7;

        /// <summary>Die letzte ID der neuen Vorlagen.</summary>
        public const int LETZTE_ID = 16;

        /// <summary>Kurzform der Quelle der Nutzungszeiten.</summary>
        public const string DIN = "DIN V 18599-10:2018-09";

        /// <summary>Kennung eines nicht belegten Werts.</summary>
        public const string RICHTWERT = "Richtwert, Quelle prüfen";

        /// <summary>Der Kopfkatalog.</summary>
        public const string TAB_KOPF = KaeltebedarfSchema.TAB_KOPF_STAMM;

        /// <summary>Der Typkatalog der Wochenprofile.</summary>
        public const string TAB_TYP = KaeltebedarfSchema.TAB_TYP_STAMM;

        private static readonly Kaeltevorlage[] SAETZE = Bauen();

        /// <summary>Die zehn Vorlagen in Auslieferungsreihenfolge (ID 7 bis 16).</summary>
        public static IReadOnlyList<Kaeltevorlage> Alle => SAETZE;

        // =================================================================================
        // Die zehn Vorlagen (Konzept Kaeltebedarfsprofile, Tafel 6.2)
        // =================================================================================

        private static Kaeltevorlage[] Bauen()
        {
            const string R = KaelteArtSchema.RAUM, P = KaelteArtSchema.PROZESS;
            int[] werktage = { 0, 1, 2, 3, 4 }, wochenende = { 5, 6 }, mitSamstag = { 0, 1, 2, 3, 4, 5 }, sonntag = { 6 };
            int[] alle = { 0, 1, 2, 3, 4, 5, 6 };
            double[] gleich = Faktoren(1.0);
            // Prozesskaelte der Schichtbetriebe: zwoelf Monate fast gleich, +10 % Juli/August (Richtwert, Quelle pruefen).
            double[] schicht = Faktoren(1.0, (7, 1.1), (8, 1.1));

            return new[]
            {
                // 7 Krankenhaus. Nutzungszeit 0-24 Uhr, 365 d: DIN V 18599-10:2018-09, Profil 10 Bettenzimmer (TS 2025: 10),
                // Ausgabe pruefen. Tag 7-20 Uhr 1,0, Nacht 0,6, Wochenende wie Werktag; Monate Grundlast 0,4 ganzjaehrig,
                // Spitze Juni-August; Gang, Monate, Grundlast: Richtwert, Quelle pruefen.
                new Kaeltevorlage(7, "Krankenhaus",
                    "Raumkühlung eines Krankenhauses (Bettenbereich) an sieben Tagen rund um die Uhr, tags 7 bis 20 Uhr volle Last, nachts 60 %; ganzjährige Grundlast 40 %, Spitze Juni bis August.",
                    R, 16, 19, 0.4, Woche(Band(alle, 0, 24, 0.6), Band(alle, 7, 20, 1.0)),
                    Faktoren(0.4, (5, 0.6), (6, 0.9), (7, 1.0), (8, 1.0), (9, 0.6)),
                    "Nutzungszeit " + DIN + " Profil 10 Bettenzimmer; Gang, Monate, Grundlast: " + RICHTWERT),
                // 8 Prozesskaelte einschichtig. Mo-Fr 6-14 Uhr 1,0, sonst Grundlast 0,1 (Schichtbild wie Prozesswaerme
                // "Einschicht 5 Tage"); alles Richtwert, Quelle pruefen.
                new Kaeltevorlage(8, "Prozesskälte einschichtig",
                    "Prozesskälte eines Betriebs im Einschichtbetrieb Montag bis Freitag 6 bis 14 Uhr, sonst Grundlast 10 %; zwölf Monate fast gleich, Juli und August +10 %.",
                    P, 6, 12, 0.1, Woche(Band(alle, 0, 24, 0.1), Band(werktage, 6, 14, 1.0)), schicht,
                    RICHTWERT + " (Schichtbild wie Prozesswärme Einschicht 5 Tage)"),
                // 9 Prozesskaelte zweischichtig. Mo-Fr 6-22 Uhr 1,0, sonst Grundlast 0,15; Richtwert, Quelle pruefen.
                new Kaeltevorlage(9, "Prozesskälte zweischichtig",
                    "Prozesskälte eines Betriebs im Zweischichtbetrieb Montag bis Freitag 6 bis 22 Uhr, sonst Grundlast 15 %; zwölf Monate fast gleich, Juli und August +10 %.",
                    P, 6, 12, 0.15, Woche(Band(alle, 0, 24, 0.15), Band(werktage, 6, 22, 1.0)), schicht,
                    RICHTWERT + " (Schichtbild wie Prozesswärme Zweischicht 5 Tage)"),
                // 10 Prozesskaelte dreischichtig. Mo-Fr 0-24 Uhr 1,0, Sa/So Grundlast 0,2; Richtwert, Quelle pruefen.
                new Kaeltevorlage(10, "Prozesskälte dreischichtig",
                    "Prozesskälte eines Betriebs im Dreischichtbetrieb Montag bis Freitag rund um die Uhr, Samstag und Sonntag Grundlast 20 %; zwölf Monate fast gleich, Juli und August +10 %.",
                    P, 6, 12, 0.2, Woche(Band(wochenende, 0, 24, 0.2), Band(werktage, 0, 24, 1.0)), schicht,
                    RICHTWERT + " (Schichtbild wie Prozesswärme Dreischicht 5 Tage)"),
                // 11 Rechenzentrum. Nutzungszeit 0-24 Uhr, 365 d: DIN V 18599-10:2018-09, Profil 21 Rechenzentrum (TS 2025: 21),
                // Ausgabe pruefen. Grundlast 0,85, Mo-Fr 8-18 Uhr 1,0 (Aufschlag rund 15 %), zwoelf Monate gleich;
                // Aufschlag und Grundlast: Richtwert, Quelle pruefen.
                new Kaeltevorlage(11, "Rechenzentrum",
                    "Kühlung der IT-Last eines Rechenzentrums an sieben Tagen rund um die Uhr nahezu gleich (85 %), Montag bis Freitag 8 bis 18 Uhr volle Last; zwölf Monate gleich.",
                    P, 18, 24, 0.85, Woche(Band(alle, 0, 24, 0.85), Band(werktage, 8, 18, 1.0)), gleich,
                    "Nutzungszeit " + DIN + " Profil 21 Rechenzentrum; Aufschlag, Grundlast: " + RICHTWERT),
                // 12 Buero/Verwaltung. Nutzungszeit 7-18 Uhr, 250 d: DIN V 18599-10:2018-09, Profile 1 und 2 Einzel-/Gruppenbuero
                // (TS 2025: 1, 2), Ausgabe pruefen. Gang qualitativ VDI 2078 (innere Lasten, Spitze 13-16 Uhr durch Sonne
                // und Speicherwirkung): Mo-Fr 7-13 0,7, 13-16 1,0, 16-18 0,8, sonst Grundlast 0,1 (Serverraeume, Technik);
                // Monate April-Oktober, Spitze Juli/August, Winter 0,1; Monate, Grundlast: Richtwert, Quelle pruefen.
                new Kaeltevorlage(12, "Büro/Verwaltung",
                    "Raumkühlung eines Büro- und Verwaltungsbaus Montag bis Freitag 7 bis 18 Uhr mit Spitze 13 bis 16 Uhr, sonst Grundlast 10 % für Server- und Technikräume; April bis Oktober, Spitze Juli und August.",
                    R, 16, 19, 0.1,
                    Woche(Band(alle, 0, 24, 0.1), Band(werktage, 7, 13, 0.7), Band(werktage, 13, 16, 1.0), Band(werktage, 16, 18, 0.8)),
                    Faktoren(0.1, (4, 0.3), (5, 0.5), (6, 0.8), (7, 1.0), (8, 1.0), (9, 0.6), (10, 0.2)),
                    "Nutzungszeit " + DIN + " Profile 1/2 Einzel-/Gruppenbüro; Gang qualitativ VDI 2078; Monate, Grundlast: " + RICHTWERT),
                // 13 Hotel. Nutzungszeit Hotelzimmer 21-8 Uhr, 365 d: DIN V 18599-10:2018-09, Profil 11 (TS 2025: 11), Ausgabe
                // pruefen. Ganztaegig: 0-8 Uhr 0,5 (Zimmer), 8-17 Uhr 0,6 (Allgemeinflaechen), 17-23 Uhr 1,0, 23-24 Uhr 0,6,
                // Wochenende wie Werktag; Monate Mai-September, Winter Grundlast 0,25 (Kueche, Kuehlraeume); Spitze,
                // Monate, Grundlast: Richtwert, Quelle pruefen.
                new Kaeltevorlage(13, "Hotel",
                    "Raumkühlung eines Hotels an sieben Tagen rund um die Uhr, Spitze 17 bis 23 Uhr, Zimmer nachts belegt; Mai bis September, ganzjährige Grundlast 25 % für Küche und Kühlräume.",
                    R, 16, 19, 0.25,
                    Woche(Band(alle, 0, 8, 0.5), Band(alle, 8, 17, 0.6), Band(alle, 17, 23, 1.0), Band(alle, 23, 24, 0.6)),
                    Faktoren(0.25, (5, 0.5), (6, 0.8), (7, 1.0), (8, 1.0), (9, 0.6)),
                    "Nutzungszeit " + DIN + " Profil 11 Hotelzimmer; Spitze, Monate, Grundlast: " + RICHTWERT),
                // 14 Lebensmitteleinzelhandel mit Gewerbekaelte. Nutzungszeit 8-20 Uhr, 300 d (rund Mo-Sa):
                // DIN V 18599-10:2018-09, Profil 7 Einzelhandel/Kaufhaus Lebensmittelabteilung mit Kuehlprodukten
                // (TS 2025: 7), Ausgabe pruefen. Kuehlmoebel 168 h Grundlast 0,6, Oeffnungszeit Mo-Sa 1,0, Sonntag
                // Grundlast; ganzjaehrig, Juli/August +20 %; Gewerbekaelte, Monate, Grundlast: Richtwert, Quelle pruefen.
                new Kaeltevorlage(14, "Lebensmitteleinzelhandel mit Gewerbekälte",
                    "Gewerbekälte der Kühlmöbel eines Lebensmittelmarkts an sieben Tagen rund um die Uhr mit Grundlast 60 %, Öffnungszeit Montag bis Samstag 8 bis 20 Uhr volle Last; ganzjährig, Juli und August +20 %.",
                    P, -2, 4, 0.6, Woche(Band(alle, 0, 24, 0.6), Band(mitSamstag, 8, 20, 1.0)),
                    Faktoren(1.0, (7, 1.2), (8, 1.2)),
                    "Nutzungszeit " + DIN + " Profil 7 Einzelhandel mit Kühlprodukten; Gewerbekälte, Monate, Grundlast: " + RICHTWERT),
                // 15 Schule/Hochschule. Nutzungszeit Klassenzimmer 8-15 Uhr, 200 d, Hoersaal 8-18 Uhr, 150 d:
                // DIN V 18599-10:2018-09, Profile 8 und 9 (TS 2025: 8, 9), Ausgabe pruefen. Mo-Fr 8-15 Uhr 1,0, 15-18 Uhr 0,4
                // (Hoersaal), sonst Grundlast 0,05 (auch Sa/So); Monate Mai-September OHNE Ferienabzug - Ferien ueber den
                // Betriebskalender; Monate, Grundlast: Richtwert, Quelle pruefen.
                new Kaeltevorlage(15, "Schule/Hochschule",
                    "Raumkühlung einer Schule oder Hochschule Montag bis Freitag 8 bis 15 Uhr, Hörsäle bis 18 Uhr mit 40 %, sonst Grundlast 5 %; Mai bis September. " + VERMERK_FERIEN,
                    R, 16, 19, 0.05,
                    Woche(Band(alle, 0, 24, 0.05), Band(werktage, 8, 15, 1.0), Band(werktage, 15, 18, 0.4)),
                    Faktoren(0.05, (5, 0.4), (6, 0.8), (7, 1.0), (8, 1.0), (9, 0.5)),
                    "Nutzungszeit " + DIN + " Profile 8 Klassenzimmer, 9 Hörsaal; Monate, Grundlast: " + RICHTWERT),
                // 16 Labor. Nutzungszeit 7-18 Uhr, 250 d: DIN V 18599-10:2018-09, Profil 36 Labor (TS 2025: 38), Ausgabe
                // pruefen. Mo-Fr 7-18 Uhr 1,0, sonst Grundlast 0,4 (Abzuege, Geraete); ganzjaehrig mit Sommerspitze
                // (Winter 0,7); Grundlast, Monate: Richtwert, Quelle pruefen.
                new Kaeltevorlage(16, "Labor",
                    "Raumkühlung eines Laborbaus Montag bis Freitag 7 bis 18 Uhr, sonst Grundlast 40 % für Abzüge und Geräte; ganzjährig mit Sommerspitze Juni bis August.",
                    R, 16, 19, 0.4, Woche(Band(alle, 0, 24, 0.4), Band(werktage, 7, 18, 1.0)),
                    Faktoren(0.7, (5, 0.85), (6, 1.0), (7, 1.0), (8, 1.0), (9, 0.85)),
                    "Nutzungszeit " + DIN + " Profil 36 Labor; Grundlast, Monate: " + RICHTWERT),
            };
        }

        /// <summary>Ein Stundenband [von, bis) mit Last an den genannten Wochentagen (0 = Montag).</summary>
        private static (int[] Tage, int Von, int Bis, double Last) Band(int[] tage, int von, int bis, double last)
            => (tage, von, bis, last);

        /// <summary>Ein Wochenprofil: die Bänder der Reihe nach, ein späteres überschreibt ein früheres.</summary>
        private static double[] Woche(params (int[] Tage, int Von, int Bis, double Last)[] baender)
        {
            var w = new double[168];
            foreach ((int[] tage, int von, int bis, double last) in baender)
                foreach (int tag in tage)
                    for (int h = von; h < bis; h++) w[tag * 24 + h] = last;
            return w;
        }

        /// <summary>Zwölf Faktoren mit Grundwert und Ausnahmen (Monat 1–12).</summary>
        private static double[] Faktoren(double grund, params (int Monat, double Faktor)[] ausnahmen)
        {
            var f = new double[12];
            for (int m = 0; m < 12; m++) f[m] = grund;
            foreach ((int monat, double faktor) in ausnahmen) f[monat - 1] = faktor;
            return f;
        }

        // =================================================================================
        // Der Schreibweg
        // =================================================================================

        /// <summary>Was ein Lauf getan hat.</summary>
        public sealed class Bericht
        {
            /// <summary>Zahl der in diesem Lauf angelegten Kopfsätze.</summary>
            public int Koepfe { get; internal set; }

            /// <summary>Zahl der in diesem Lauf angelegten Wochenprofile.</summary>
            public int Profile { get; internal set; }

            /// <summary>Namen, unter denen ein EIGENER Satz des Anwenders steht (übergangen).</summary>
            public List<string> Eigene { get; } = new List<string>();

            /// <summary>Zahl der Sätze, deren vorgesehene ID belegt war (mit vergebener ID geschrieben).</summary>
            public int IdBelegt { get; internal set; }

            /// <summary>Die Zeile für Protokoll und Werkzeug.</summary>
            public string Zeile()
                => Koepfe.ToString(CultureInfo.InvariantCulture) + " Kaeltebedarfsvorlage(n) und " +
                   Profile.ToString(CultureInfo.InvariantCulture) + " Wochenprofil(e) von " +
                   Alle.Count.ToString(CultureInfo.InvariantCulture) + " Vorlagen gesaet (ReadOnly = 1, IDs " +
                   ERSTE_ID.ToString(CultureInfo.InvariantCulture) + "-" + LETZTE_ID.ToString(CultureInfo.InvariantCulture) +
                   (IdBelegt > 0 ? ", " + IdBelegt.ToString(CultureInfo.InvariantCulture) + " mit vergebener ID" : "") + ")";
        }

        /// <summary>Steht jede Vorlage unter ihrem Namen in beiden Katalogen (ein eigener Satz zählt als belegt)?</summary>
        public static bool Vollstaendig()
        {
            if (!DataRepository.TabelleVorhanden(TAB_KOPF) || !DataRepository.TabelleVorhanden(TAB_TYP)) return false;
            foreach (Kaeltevorlage s in Alle)
            {
                DataRow kopf = Zeile(TAB_KOPF, s.Name), typ = Zeile(TAB_TYP, s.Name);
                if (Eigen(kopf) || Eigen(typ)) continue;
                if (kopf == null || typ == null) return false;
            }
            return true;
        }

        private static bool Eigen(DataRow r) => r != null && !Kennzeichen(r);

        /// <summary>
        /// Schreibt die fehlenden Vorlagen, je Satz in einem Vorgang. <b>Wiederholbar und nie überschreibend.</b>
        /// Setzt die Tabellen und die Spalte <see cref="KaeltebedarfsprofilVorlagenSchema.SPALTE_ART"/> voraus. Fehler werfen.
        /// </summary>
        public static Bericht Ausfuehren(IList<string> bericht)
        {
            if (!DataRepository.TabelleVorhanden(TAB_KOPF) || !DataRepository.TabelleVorhanden(TAB_TYP))
                throw new InvalidOperationException("Die Tabellen " + TAB_KOPF + " und " + TAB_TYP + " fehlen.");
            bool mitArt = DataRepository.SpalteVorhanden(TAB_KOPF, KaeltebedarfsprofilVorlagenSchema.SPALTE_ART);

            var b = new Bericht();
            foreach (Kaeltevorlage s in Alle)
            {
                DataRow kopf = Zeile(TAB_KOPF, s.Name);
                DataRow typ = Zeile(TAB_TYP, s.Name);
                if (Eigen(kopf) || Eigen(typ))
                {
                    b.Eigene.Add(s.Name);
                    bericht?.Add("Kaeltebedarfsvorlage \"" + s.Name + "\" nicht gesaet: ein eigener Satz oder Typ traegt den Namen");
                    continue;
                }
                if (kopf != null && typ != null) continue;
                // Die Auskunft VOR dem Vorgang.
                bool kopfIdFrei = IdFrei(TAB_KOPF, s.Id), typIdFrei = IdFrei(TAB_TYP, s.Id);

                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        if (kopf == null)
                        {
                            KopfSchreiben(v, s, kopfIdFrei, mitArt);
                            b.Koepfe++;
                            if (!kopfIdFrei) b.IdBelegt++;
                        }
                        if (typ == null)
                        {
                            TypSchreiben(v, s, typIdFrei);
                            b.Profile++;
                        }
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
            }
            bericht?.Add(b.Zeile());
            return b;
        }

        private static DataRow Zeile(string tabelle, string name)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, ReadOnly FROM \"" + tabelle + "\" WHERE Bezeichner = ?", new DbParam("@b", name));
            return dt != null && dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        private static bool IdFrei(string tabelle, int id)
        {
            object o = DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE ID = ?", new DbParam("@id", id));
            return o == null || o == DBNull.Value || Convert.ToInt64(o, CultureInfo.InvariantCulture) == 0;
        }

        private static bool Kennzeichen(DataRow r)
            => r["ReadOnly"] != DBNull.Value && Convert.ToInt64(r["ReadOnly"], CultureInfo.InvariantCulture) != 0;

        /// <summary>Der Kopfsatz: [ID,] Name, Typ = Name, Beschreibung, zwölf Monatswerte, Paar, Art, ReadOnly = 1.</summary>
        private static void KopfSchreiben(DbVorgang v, Kaeltevorlage s, bool mitId, bool mitArt)
        {
            double[] monat = s.Monatswerte();
            var spalten = new StringBuilder(mitId ? "ID, " : "");
            var werte = new StringBuilder(mitId ? "?, " : "");
            var p = new List<DbParam>();
            if (mitId) p.Add(new DbParam("@id", s.Id));
            spalten.Append("Bezeichner, Typ, Beschreibung");
            werte.Append("?, ?, ?");
            p.Add(new DbParam("@bz", s.Name));
            p.Add(new DbParam("@ty", s.Name));
            p.Add(new DbParam("@be", s.Beschreibung));
            for (int m = 0; m < 12; m++)
            {
                spalten.Append(", Monat_" + (m + 1).ToString(CultureInfo.InvariantCulture));
                werte.Append(", ?");
                p.Add(new DbParam("@m" + (m + 1).ToString("D2", CultureInfo.InvariantCulture), monat[m]));
            }
            spalten.Append(", Vorlauf, Ruecklauf, ReadOnly");
            werte.Append(", ?, ?, 1");
            p.Add(new DbParam("@vl", s.Vorlauf));
            p.Add(new DbParam("@rl", s.Ruecklauf));
            if (mitArt)
            {
                spalten.Append(", " + KaeltebedarfsprofilVorlagenSchema.SPALTE_ART);
                werte.Append(", ?");
                p.Add(new DbParam("@art", s.Art));
            }
            v.Ausfuehren("INSERT INTO Tab_Kaeltebedarf_STAMM (" + spalten + ") VALUES (" + werte + ")", p.ToArray());
        }

        /// <summary>Das Wochenprofil: [ID,] Name, Beschreibung, 168 Werte, ReadOnly = 1.</summary>
        private static void TypSchreiben(DbVorgang v, Kaeltevorlage s, bool mitId)
        {
            var spalten = new StringBuilder(mitId ? "ID, Bezeichner, Beschreibung" : "Bezeichner, Beschreibung");
            var werte = new StringBuilder(mitId ? "?, ?, ?" : "?, ?");
            var p = new List<DbParam>();
            if (mitId) p.Add(new DbParam("@id", s.Id));
            p.Add(new DbParam("@bz", s.Name));
            p.Add(new DbParam("@be", s.Beschreibung));
            for (int j = 0; j < 168; j++)
            {
                spalten.Append(", \"" + (j + 1).ToString(CultureInfo.InvariantCulture) + "\"");
                werte.Append(", ?");
                p.Add(new DbParam("@w" + (j + 1).ToString("D3", CultureInfo.InvariantCulture), s.Woche[j]));
            }
            spalten.Append(", ReadOnly");
            werte.Append(", 1");
            v.Ausfuehren("INSERT INTO Tab_Kaeltetyp_STAMM (" + spalten + ") VALUES (" + werte + ")", p.ToArray());
        }
    }
}
