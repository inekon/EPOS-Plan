using System;
using System.Collections.Generic;
using System.Globalization;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Feldkarte der Vorgabe-Matrix für den Assistenten</b> (Stufe KP2, Welle U1; Teilkonzept
    /// Konditionierungsprofile 3.3, 7.2) — EIN Profil, aus dem der Dialogkatalog die Felder des Reiters
    /// „Konditionierung" erzeugt und über das die Sichtklasse des Editors sie als FELDTAFEL
    /// (<c>IKiFeldtafel</c>) beantwortet: keine zweite Feldliste von Hand (Muster Katalogbrowser).
    /// </summary>
    /// <remarks>
    /// <para><b>Die Schlüssel.</b> <c>kond_&lt;größe&gt;_&lt;zeile&gt;</c> für den Wert einer Zelle
    /// (<c>heizen</c>, <c>kuehlen</c>, <c>lueftung</c>, <c>geraete</c>, <c>personen</c>; <c>nennwert</c>,
    /// <c>tag</c>, <c>nacht</c>, <c>wochenende</c>, <c>ferien</c>, <c>saison</c>), dazu
    /// <c>_aus</c> (Heizen und Kühlen: der Zustand „aus"), <c>_von</c>/<c>_bis</c> (das Nachtfenster
    /// einer Spalte in vollen Stunden, die Saison in Tagen des Gemeinjahrs) und
    /// <c>kond_lueftung_nacht_dt</c> (ΔT der Nachtauskühlung). Je Größe eröffnet
    /// <c>kond_&lt;größe&gt;_vorlage</c> die Spalte (Welle U2; Entwurf KP2 D9): die Zeile „Vorlage" über dem
    /// Nennwert als WAHL aus der Liste der Karte — Setzen trägt die Aktion des Knopfs „Übernehmen", Lesen
    /// nennt die Herkunft des angelegten Kalenders.</para>
    /// <para><b>Bestandszellen behalten ihre Namen</b> (Entwurf KP2, Festlegung 5): Heizen Tag, Nacht,
    /// Wochenende und Ferien, Kühlen Tag, Infiltration, Nutzerlüftung und innere Wärmegewinne stehen
    /// schon unter ihren Katalogfeldern (<c>soll_tag</c>, <c>nachtabsenkung</c>,
    /// <c>wochenendabsenkung</c>, <c>soll_ferien</c>, <c>kuehl_sollwert</c>,
    /// <c>luftwechsel_infiltration</c>, <c>luftwechsel_nutzer</c>, <c>waermegewinne</c>); das
    /// Nachtfenster der Heizspalte ist <c>nacht_beginn</c>/<c>nacht_ende</c>. Nur der Kühlsollwert der
    /// Nacht hatte noch keines — er heißt nach seiner Spalte <c>kuehl_sollwert_nacht</c>.</para>
    /// <para><b>Die Karte im Einzelnen</b> (Welle U3): <c>kond_&lt;größe&gt;_woche</c> schließt die Spalte
    /// ab — die Woche des angelegten Kalenders als Text (168 Werte durch „;" getrennt, „aus" für
    /// abgeschaltet; ein einzelner Wert ist die Grundangabe). Die Werkzeuge der Karte (Zeitfenster,
    /// Feiertage, Zeitstruktur, Periodenliste) sind Handlungen mit eigener Eingabe, keine Felder: Der
    /// Assistent setzt ihr Ergebnis über die Woche oder erklärt sie aus dem Aktionswissen.</para>
    /// <para><b>Drei Masken.</b> Der Katalogeditor (Reiter „Konditionierung") und die Gebäudeverwaltung
    /// (Blatt „Konditionierung", Stufe KP2, Welle U4) führen dieselben Felder an derselben Sichtklasse
    /// (<see cref="SICHT"/>). Der Zonendialog führt die ZONENKARTE (<see cref="Zonenfelder"/>) an seiner
    /// Sichtklasse (<see cref="ZONENSICHT"/>): ohne die Kühlspalte (an der Zone gesperrt bis KU3), mit dem
    /// Nachtfenster der Heizspalte als eigenen Feldern (<c>kond_heizen_nacht_von</c>/<c>_bis</c> — an einer
    /// Zone steht es in der Zelle, nicht in Bestandsspalten); ihre Bestandszellen stehen schon unter den
    /// Feldern der Zone (<c>soll_tag</c>, <c>soll_nacht</c>, <c>soll_wochenende</c>, <c>soll_ferien</c>,
    /// <c>infiltration</c>, <c>nutzerlueftung</c>, <c>gewinne</c>). Leer heißt an der Zone „wie
    /// Gebäude". Die Vorlage einer Größe führt die Zonenkarte nicht — die Karten der Zone bieten keine
    /// Vorlagen.</para>
    /// <para><b>Die Abkürzung „alle Größen"</b> (E57; Stufe KP2, Welle U5): <c>kond_vorlage_alle</c> steht vor den
    /// Spalten — die Liste in der Kopfzelle der Zeile „Vorlage", keine Zelle und keine Größe
    /// (<see cref="Teil.VorlageAlle"/>, <see cref="Feld.Groessenplatz"/> −1). Eine WAHL aus jedem Namen, der in
    /// mindestens einer der fünf Listen steht; Setzen stellt die EINE Rückfrage für alle Größen, die der Anwender
    /// selbst beantwortet (wie bei <c>kond_&lt;größe&gt;_vorlage</c>), Lesen nennt die gemeinsame Herkunft. Am
    /// Gebäude und Katalogbau, nicht an der Zone.</para>
    /// </remarks>
    public static class KiKonditionierungsfelder
    {
        /// <summary>Der Typname der Sichtklasse, die die Felder als Feldtafel beantwortet.</summary>
        public const string SICHT = "GebaeudeKatalogKiSicht";

        /// <summary>Der Typname der Sichtklasse des Zonendialogs, die die Zonenkarte als Feldtafel beantwortet.</summary>
        public const string ZONENSICHT = "ZonenKiSicht";

        /// <summary>
        /// Der Schlüssel der Abkürzung „gleichnamige Vorlage in allen Größen übernehmen" (E57; Stufe KP2,
        /// Welle U5) — die Liste „alle Größen" in der Kopfzelle der Zeile „Vorlage".
        /// </summary>
        public const string VORLAGE_ALLE = "kond_vorlage_alle";

        /// <summary>Was ein Feld an seiner Zelle trägt.</summary>
        public enum Teil
        {
            /// <summary>Der Wert der Zelle.</summary>
            Wert,

            /// <summary>Der Zustand „aus" (Heizen und Kühlen).</summary>
            Aus,

            /// <summary>Beginn des Nachtfensters bzw. Start der Saison.</summary>
            Von,

            /// <summary>Ende des Nachtfensters bzw. der Saison.</summary>
            Bis,

            /// <summary>ΔT der Nachtauskühlung.</summary>
            DeltaT,

            /// <summary>Die Vorlage der Größe (Zeile „Vorlage" der Matrix, keine Zelle).</summary>
            Vorlage,

            /// <summary>
            /// Die Woche des angelegten Kalenders der Größe als Text (Karte im Einzelnen, Welle U3; keine
            /// Zelle): 168 Werte oder ein Wert der Grundangabe.
            /// </summary>
            Woche,

            /// <summary>
            /// Die gleichnamige Vorlage in allen Größen (Abkürzung nach E57, Welle U5): die Liste „alle Größen"
            /// in der Kopfzelle der Zeile „Vorlage" — keine Zelle und keine Größe.
            /// </summary>
            VorlageAlle
        }

        /// <summary>Ein Feld der Karte: Schlüssel, Zelle (Größe, Zeile) und Teil.</summary>
        public sealed class Feld
        {
            internal Feld(string schluessel, Konditionierungsgroesse groesse, int zeile, Teil teil, bool bestand)
            {
                Schluessel = schluessel;
                Groesse = groesse;
                Zeilenplatz = zeile;
                Teil = teil;
                Bestandszelle = bestand;
            }

            /// <summary>Der Feldname des Katalogs und zugleich der Schlüssel der Tafel.</summary>
            public string Schluessel { get; }

            /// <summary>
            /// Die Größe (Spalte der Matrix); an der Abkürzung „alle Größen" (<see cref="Teil.VorlageAlle"/>)
            /// ohne Bedeutung — dort steht die erste, und <see cref="Groessenplatz"/> ist −1.
            /// </summary>
            public Konditionierungsgroesse Groesse { get; }

            /// <summary>
            /// Der Platz der Größe in <see cref="Konditionierungsgroessen.Alle"/> — die Spalte der Oberfläche;
            /// <c>-1</c> = keine Größe (<see cref="Teil.VorlageAlle"/>).
            /// </summary>
            public int Groessenplatz => Teil == Teil.VorlageAlle ? -1 : IndexIn(Konditionierungsgroessen.Alle, Groesse);

            /// <summary>
            /// Der Platz der Zeile in <see cref="DbWerte.KOND_ZEILEN"/> — die Zeile der Oberfläche; <c>-1</c> =
            /// keine Zelle (<see cref="Teil.Vorlage"/>).
            /// </summary>
            public int Zeilenplatz { get; }

            /// <summary>Das Zeilenkennwort (<see cref="DbWerte.KOND_ZEILEN"/>); <c>null</c> = keine Zelle.</summary>
            public string Zeile => Zeilenplatz < 0 ? null : DbWerte.KOND_ZEILEN[Zeilenplatz];

            /// <summary>Was das Feld an der Zelle trägt.</summary>
            public Teil Teil { get; }

            /// <summary>Ist die Zelle eine Bestandszelle (ihr Wert steht in einer Spalte des Gebäudes)?</summary>
            public bool Bestandszelle { get; }
        }

        private static readonly string[] GROESSENWORT = { "heizen", "kuehlen", "lueftung", "geraete", "personen" };

        private static readonly string[] ZEILENWORT = { "nennwert", "tag", "nacht", "wochenende", "ferien", "saison" };

        /// <summary>
        /// Die Katalogfelder der Bestandszellen — „Größe|Zeile" → Feldname. Alle bis auf
        /// <c>kuehl_sollwert_nacht</c> stehen schon in der Feldliste des Editors
        /// (<see cref="BESTEHEND"/>); die Karte führt sie nicht ein zweites Mal.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> BESTANDSNAMEN =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { Paar(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_TAG), "soll_tag" },
                { Paar(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_NACHT), "nachtabsenkung" },
                { Paar(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_WOCHENENDE), "wochenendabsenkung" },
                { Paar(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_FERIEN), "soll_ferien" },
                { Paar(Konditionierungsgroesse.Kuehlsoll, DbWerte.KOND_ZEILE_TAG), "kuehl_sollwert" },
                { Paar(Konditionierungsgroesse.Kuehlsoll, DbWerte.KOND_ZEILE_NACHT), "kuehl_sollwert_nacht" },
                { Paar(Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NENNWERT), "luftwechsel_infiltration" },
                { Paar(Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_TAG), "luftwechsel_nutzer" },
                { Paar(Konditionierungsgroesse.Geraete, DbWerte.KOND_ZEILE_NENNWERT), "waermegewinne" },
            };

        /// <summary>Die Bestandsnamen, die der Editor schon als eigene Felder führt.</summary>
        public static readonly ISet<string> BESTEHEND = new HashSet<string>(StringComparer.Ordinal)
        {
            "soll_tag", "nachtabsenkung", "wochenendabsenkung", "soll_ferien", "kuehl_sollwert",
            "luftwechsel_infiltration", "luftwechsel_nutzer", "waermegewinne"
        };

        /// <summary>Alle Felder der Karte in der Reihenfolge der Matrix (Spalte für Spalte, Zeile für Zeile).</summary>
        public static IReadOnlyList<Feld> Alle { get; } = Bauen(zone: false);

        /// <summary>
        /// Die Felder der ZONENKARTE (Stufe KP2, Welle U4; Teilkonzept 3.4, 7.3): ohne die Kühlspalte, mit
        /// dem Nachtfenster der Heizspalte, ohne die Bestandszellen (die Zone führt sie unter eigenen Namen).
        /// </summary>
        public static IReadOnlyList<Feld> Zonenfelder { get; } = Bauen(zone: true);

        private static readonly Dictionary<string, Feld> NACH_SCHLUESSEL = Verzeichnis(Alle);

        private static readonly Dictionary<string, Feld> ZONE_NACH_SCHLUESSEL = Verzeichnis(Zonenfelder);

        /// <summary>Das Feld zu einem Schlüssel; <c>null</c> = kein Feld der Karte.</summary>
        public static Feld Finde(string schluessel)
            => schluessel != null && NACH_SCHLUESSEL.TryGetValue(schluessel, out Feld f) ? f : null;

        /// <summary>Das Feld der Zonenkarte zu einem Schlüssel; <c>null</c> = kein Feld der Zonenkarte.</summary>
        public static Feld FindeZone(string schluessel)
            => schluessel != null && ZONE_NACH_SCHLUESSEL.TryGetValue(schluessel, out Feld f) ? f : null;

        /// <summary>
        /// Der Name der Bestandszelle (Größe, Zeile), unter dem der Katalog sie führt; <c>null</c> = die
        /// Zelle hat keine Bestandsspalte.
        /// </summary>
        public static string Bestandsname(Konditionierungsgroesse groesse, string zeile)
            => BESTANDSNAMEN.TryGetValue(Paar(groesse, zeile), out string n) ? n : null;

        /// <summary>Gibt es die Zelle? Den Nennwert führen Lüftung, Geräte und Personen, die Saison Heizen und Kühlen.</summary>
        public static bool Gibt(Konditionierungsgroesse g, string zeile)
        {
            if (zeile == DbWerte.KOND_ZEILE_NENNWERT)
                return g == Konditionierungsgroesse.Lueftung || Konditionierungsgroessen.HatNennwert(g);
            if (zeile == DbWerte.KOND_ZEILE_SAISON)
                return g == Konditionierungsgroesse.Heizsoll || g == Konditionierungsgroesse.Kuehlsoll;
            return true;
        }

        /// <summary>Kennt die Zelle „aus"? Heizen und Kühlen außer Nennwert und Saison.</summary>
        public static bool MitAus(Konditionierungsgroesse g, string zeile)
            => (g == Konditionierungsgroesse.Heizsoll || g == Konditionierungsgroesse.Kuehlsoll)
               && zeile != DbWerte.KOND_ZEILE_NENNWERT && zeile != DbWerte.KOND_ZEILE_SAISON;

        private static List<Feld> Bauen(bool zone)
        {
            var felder = new List<Feld>();

            // Die Abkürzung „alle Größen" (E57, Welle U5) steht vor den Spalten: die Liste in der Kopfzelle der
            // Zeile „Vorlage". An der Zone bietet die Karte keine Vorlagen - kein Knopf, also kein Feld.
            if (!zone) felder.Add(new Feld(VORLAGE_ALLE, Konditionierungsgroessen.Alle[0], -1, Teil.VorlageAlle, false));

            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                // An der Zone ist die Kuehlspalte gesperrt (Zonenregel, bis KU3).
                if (zone && g == Konditionierungsgroesse.Kuehlsoll) continue;
                string gw = GROESSENWORT[(int)g];

                // Die Zeile „Vorlage" eröffnet die Spalte — keine Zelle, die Aktion des Knopfs „Übernehmen".
                // An der Zone bietet die Karte keine Vorlagen (Welle U2 am Gebäude und Katalogbau): kein
                // Knopf, also kein Feld.
                if (!zone) felder.Add(new Feld("kond_" + gw + "_vorlage", g, -1, Teil.Vorlage, false));

                for (int zi = 0; zi < DbWerte.KOND_ZEILEN.Count; zi++)
                {
                    string z = DbWerte.KOND_ZEILEN[zi];
                    if (!Gibt(g, z)) continue;
                    string basis = "kond_" + gw + "_" + ZEILENWORT[zi];
                    string bestand = Bestandsname(g, z);

                    if (z == DbWerte.KOND_ZEILE_SAISON)
                    {
                        felder.Add(new Feld(basis + "_von", g, zi, Teil.Von, false));
                        felder.Add(new Feld(basis + "_bis", g, zi, Teil.Bis, false));
                        continue;
                    }

                    // Der Wert: eine Bestandszelle unter ihrem Katalogfeld - steht es schon im Editor, nicht noch
                    // einmal; an der Zone stehen alle Bestandszellen schon unter den Feldern der Zone.
                    if (bestand == null) felder.Add(new Feld(basis, g, zi, Teil.Wert, false));
                    else if (!zone && !BESTEHEND.Contains(bestand)) felder.Add(new Feld(bestand, g, zi, Teil.Wert, true));

                    if (MitAus(g, z)) felder.Add(new Feld(basis + "_aus", g, zi, Teil.Aus, bestand != null));

                    // Das Nachtfenster einer Spalte; das der Heizspalte ist am Gebaeude nacht_beginn/nacht_ende,
                    // an der Zone steht es in der Zelle.
                    if (z == DbWerte.KOND_ZEILE_NACHT && (zone || g != Konditionierungsgroesse.Heizsoll))
                    {
                        felder.Add(new Feld(basis + "_von", g, zi, Teil.Von, false));
                        felder.Add(new Feld(basis + "_bis", g, zi, Teil.Bis, false));
                    }
                    if (z == DbWerte.KOND_ZEILE_NACHT && g == Konditionierungsgroesse.Lueftung)
                        felder.Add(new Feld(basis + "_dt", g, zi, Teil.DeltaT, false));
                }

                // Die Woche des angelegten Kalenders schließt die Spalte ab (Karte im Einzelnen, Welle U3);
                // an der Zone trägt die Karte keinen Inhalt im Einzelnen - kein Knopf, also kein Feld.
                if (!zone) felder.Add(new Feld("kond_" + gw + "_woche", g, -1, Teil.Woche, false));
            }
            return felder;
        }

        private static Dictionary<string, Feld> Verzeichnis(IReadOnlyList<Feld> felder)
        {
            var d = new Dictionary<string, Feld>(StringComparer.Ordinal);
            foreach (Feld f in felder) d.Add(f.Schluessel, f);
            return d;
        }

        // =====================================================================
        //  Die Felder des Dialogkatalogs
        // =====================================================================

        /// <summary>
        /// <b>Die Felder des Katalogs</b> — je Feld der Karte eines: Name = Schlüssel, Eigenschaftspfad =
        /// Sichtklasse + Schlüssel (die Sichtklasse löst ihn als FELDTAFEL auf), Anzeigename wie im
        /// Reiter („Heizen · Tag"), Einheit und Grenzen der Zelle.
        /// </summary>
        public static IEnumerable<KiDialogFeld> Dialogfelder()
        {
            foreach (Feld f in Alle)
                yield return Dialogfeld(f, zone: false);
        }

        /// <summary>
        /// <b>Die Felder der Zonenkarte</b> (Stufe KP2, Welle U4) — wie <see cref="Dialogfelder"/>, an der
        /// Sichtklasse des Zonendialogs; die Erläuterung sagt „leer = wie Gebäude".
        /// </summary>
        public static IEnumerable<KiDialogFeld> ZonenDialogfelder()
        {
            foreach (Feld f in Zonenfelder)
                yield return Dialogfeld(f, zone: true);
        }

        private static KiDialogFeld Dialogfeld(Feld f, bool zone)
        {
            string groesse = Groessenname(f.Groesse);
            string pfad = (zone ? ZONENSICHT : SICHT) + "." + f.Schluessel;
            CultureInfo c = CultureInfo.CurrentCulture;
            if (f.Teil == Teil.VorlageAlle)
                return new KiDialogFeld(f.Schluessel, pfad,
                                        MyResource.Resource.KOND_LBL_ZEILE_VORLAGE + " · " + MyResource.Resource.KOND_LBL_VORLAGE_ALLE,
                                        KiParameterTyp.Wahl, MyResource.Resource.KOND_TXT_KI_VORLAGE_ALLE);
            if (f.Teil == Teil.Vorlage)
                return new KiDialogFeld(f.Schluessel, pfad, groesse + " · " + MyResource.Resource.KOND_LBL_ZEILE_VORLAGE,
                                        KiParameterTyp.Wahl, string.Format(c, MyResource.Resource.KOND_TXT_KI_VORLAGE, groesse));
            if (f.Teil == Teil.Woche)
                return new KiDialogFeld(f.Schluessel, pfad, groesse + " · " + MyResource.Resource.KOND_LBL_STANDARDWOCHE,
                                        KiParameterTyp.Text,
                                        string.Format(c, MyResource.Resource.KOND_TXT_KI_WOCHE, groesse, Bereich(f).Einheit),
                                        leerErlaubt: true);
            string zeile = Zeilenname(f);

            switch (f.Teil)
            {
                case Teil.Aus:
                    return new KiDialogFeld(f.Schluessel, pfad, groesse + " · " + zeile + " · " + MyResource.Resource.KOND_LBL_AUS,
                                            KiParameterTyp.Wahrheitswert,
                                            string.Format(c, MyResource.Resource.KOND_TXT_KI_AUS, groesse, zeile));
                case Teil.Von:
                case Teil.Bis:
                    bool saison = f.Zeile == DbWerte.KOND_ZEILE_SAISON;
                    string teil = saison
                        ? (f.Teil == Teil.Von ? MyResource.Resource.KOND_LBL_SAISON_START : MyResource.Resource.KOND_LBL_SAISON_ENDE)
                        : (f.Teil == Teil.Von ? MyResource.Resource.KOND_LBL_NACHTFENSTER_VON : MyResource.Resource.KOND_LBL_NACHTFENSTER_BIS);
                    return saison
                        ? new KiDialogFeld(f.Schluessel, pfad, groesse + " · " + teil, KiParameterTyp.Ganzzahl,
                                           string.Format(c, zone ? MyResource.Resource.KOND_TXT_KI_ZONE_SAISON
                                                                 : MyResource.Resource.KOND_TXT_KI_SAISON, groesse, teil),
                                           leerErlaubt: true, min: 1, max: 365)
                        : new KiDialogFeld(f.Schluessel, pfad, groesse + " · " + teil, KiParameterTyp.Ganzzahl,
                                           string.Format(c, zone ? MyResource.Resource.KOND_TXT_KI_ZONE_FENSTER
                                                                 : MyResource.Resource.KOND_TXT_KI_FENSTER, groesse, teil),
                                           einheit: KiDialogTexte.EINHEIT_STUNDE, leerErlaubt: true,
                                           min: Nachtzeit.STUNDE_MIN, max: Nachtzeit.STUNDE_MAX);
                case Teil.DeltaT:
                    return new KiDialogFeld(f.Schluessel, pfad, groesse + " · " + MyResource.Resource.KOND_LBL_AUSSENABSTAND,
                                            KiParameterTyp.Zahl, MyResource.Resource.KOND_TXT_HINWEIS_NACHTAUSKUEHLUNG,
                                            einheit: KiDialogTexte.EINHEIT_KELVIN, leerErlaubt: true, min: 0, max: 5);
                default:
                    (string einheit, double? min, double? max) = Bereich(f);
                    return new KiDialogFeld(f.Schluessel, pfad, groesse + " · " + zeile, KiParameterTyp.Zahl,
                                            string.Format(c, zone ? MyResource.Resource.KOND_TXT_KI_ZONE_ZELLE
                                                                  : MyResource.Resource.KOND_TXT_KI_ZELLE, groesse, zeile, einheit),
                                            einheit: einheit, leerErlaubt: true, min: min, max: max);
            }
        }

        /// <summary>Einheit und Grenzen des Werts einer Zelle — wie die Zelle im Reiter.</summary>
        private static (string Einheit, double? Min, double? Max) Bereich(Feld f)
        {
            switch (f.Groesse)
            {
                case Konditionierungsgroesse.Heizsoll:
                    return (KiDialogTexte.EINHEIT_GRAD_C, Konditionierungsgroessen.Min(f.Groesse), Konditionierungsgroessen.Max(f.Groesse));
                case Konditionierungsgroesse.Kuehlsoll:
                    return (KiDialogTexte.EINHEIT_GRAD_C, Gebaeudemodellvorgaben.KUEHLSOLLWERT_MIN, Gebaeudemodellvorgaben.KUEHLSOLLWERT_MAX);
                case Konditionierungsgroesse.Lueftung:
                    return (KiDialogTexte.EINHEIT_1_H, 0.0, 10.0);
                default:
                    return f.Zeile == DbWerte.KOND_ZEILE_NENNWERT
                        ? (KiDialogTexte.EINHEIT_W, 0.0, (double?)null)
                        : (KiDialogTexte.EINHEIT_PROZENT, 0.0, 100.0);
            }
        }

        /// <summary>Der Name der Größe wie im Reiter („Heizen").</summary>
        public static string Groessenname(Konditionierungsgroesse g)
        {
            switch (g)
            {
                case Konditionierungsgroesse.Heizsoll: return MyResource.Resource.KOND_LBL_GROESSE_HEIZEN;
                case Konditionierungsgroesse.Kuehlsoll: return MyResource.Resource.KOND_LBL_GROESSE_KUEHLEN;
                case Konditionierungsgroesse.Lueftung: return MyResource.Resource.KOND_LBL_GROESSE_LUEFTUNG;
                case Konditionierungsgroesse.Geraete: return MyResource.Resource.KOND_LBL_GROESSE_GERAETE;
                default: return MyResource.Resource.KOND_LBL_GROESSE_PERSONEN;
            }
        }

        /// <summary>
        /// Der Name der Zeile wie im Reiter; die Lüftung nennt ihre Zeilen nach dem, was sie sind
        /// (Infiltration, Nutzerlüftung, Nachtauskühlung).
        /// </summary>
        private static string Zeilenname(Feld f)
        {
            if (f.Groesse == Konditionierungsgroesse.Lueftung)
            {
                if (f.Zeile == DbWerte.KOND_ZEILE_NENNWERT) return MyResource.Resource.KOND_LBL_INFILTRATION;
                if (f.Zeile == DbWerte.KOND_ZEILE_TAG) return MyResource.Resource.KOND_LBL_NUTZERLUEFTUNG;
                if (f.Zeile == DbWerte.KOND_ZEILE_NACHT) return MyResource.Resource.KOND_LBL_NACHTAUSKUEHLUNG;
            }
            switch (f.Zeile)
            {
                case DbWerte.KOND_ZEILE_NENNWERT: return MyResource.Resource.KOND_LBL_ZEILE_NENNWERT;
                case DbWerte.KOND_ZEILE_TAG: return MyResource.Resource.KOND_LBL_ZEILE_TAG;
                case DbWerte.KOND_ZEILE_NACHT: return MyResource.Resource.KOND_LBL_ZEILE_NACHT;
                case DbWerte.KOND_ZEILE_WOCHENENDE: return MyResource.Resource.KOND_LBL_ZEILE_WOCHENENDE;
                case DbWerte.KOND_ZEILE_FERIEN: return MyResource.Resource.KOND_LBL_ZEILE_FERIEN;
                default: return MyResource.Resource.KOND_LBL_ZEILE_SAISON;
            }
        }

        private static string Paar(Konditionierungsgroesse g, string zeile) => ((int)g).ToString(CultureInfo.InvariantCulture) + "|" + zeile;

        private static int IndexIn(IReadOnlyList<Konditionierungsgroesse> liste, Konditionierungsgroesse g)
        {
            for (int i = 0; i < liste.Count; i++) if (liste[i] == g) return i;
            return -1;
        }
    }
}
