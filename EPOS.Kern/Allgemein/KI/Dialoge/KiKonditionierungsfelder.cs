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
    /// <c>kond_lueftung_nacht_dt</c> (ΔT der Nachtauskühlung). Je Größe schließt
    /// <c>kond_&lt;größe&gt;_vorlage</c> die Spalte ab (Welle U2; Entwurf KP2 D9): die Zeile „Vorlage" unter
    /// der Matrix als WAHL aus der Liste der Karte — Setzen trägt die Aktion des Knopfs „Übernehmen", Lesen
    /// nennt die Herkunft des angelegten Kalenders.</para>
    /// <para><b>Bestandszellen behalten ihre Namen</b> (Entwurf KP2, Festlegung 5): Heizen Tag, Nacht,
    /// Wochenende und Ferien, Kühlen Tag, Infiltration, Nutzerlüftung und innere Wärmegewinne stehen
    /// schon unter ihren Katalogfeldern (<c>soll_tag</c>, <c>nachtabsenkung</c>,
    /// <c>wochenendabsenkung</c>, <c>soll_ferien</c>, <c>kuehl_sollwert</c>,
    /// <c>luftwechsel_infiltration</c>, <c>luftwechsel_nutzer</c>, <c>waermegewinne</c>); das
    /// Nachtfenster der Heizspalte ist <c>nacht_beginn</c>/<c>nacht_ende</c>. Nur der Kühlsollwert der
    /// Nacht hatte noch keines — er heißt nach seiner Spalte <c>kuehl_sollwert_nacht</c>.</para>
    /// <para><b>Nur im Katalogeditor.</b> Die Gebäudeverwaltung trägt den Reiter nicht; ihre
    /// Stammblattgruppe „Konditionierung" kommt mit Welle U4.</para>
    /// </remarks>
    public static class KiKonditionierungsfelder
    {
        /// <summary>Der Typname der Sichtklasse, die die Felder als Feldtafel beantwortet.</summary>
        public const string SICHT = "GebaeudeKatalogKiSicht";

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

            /// <summary>Die Vorlage der Größe (Zeile „Vorlage" unter der Matrix, keine Zelle).</summary>
            Vorlage
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

            /// <summary>Die Größe (Spalte der Matrix).</summary>
            public Konditionierungsgroesse Groesse { get; }

            /// <summary>Der Platz der Größe in <see cref="Konditionierungsgroessen.Alle"/> — die Spalte der Oberfläche.</summary>
            public int Groessenplatz => IndexIn(Konditionierungsgroessen.Alle, Groesse);

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
        public static IReadOnlyList<Feld> Alle { get; } = Bauen();

        private static readonly Dictionary<string, Feld> NACH_SCHLUESSEL = Verzeichnis();

        /// <summary>Das Feld zu einem Schlüssel; <c>null</c> = kein Feld der Karte.</summary>
        public static Feld Finde(string schluessel)
            => schluessel != null && NACH_SCHLUESSEL.TryGetValue(schluessel, out Feld f) ? f : null;

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

        private static List<Feld> Bauen()
        {
            var felder = new List<Feld>();
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                string gw = GROESSENWORT[(int)g];
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

                    // Der Wert: eine Bestandszelle unter ihrem Katalogfeld - steht es schon im Editor, nicht noch einmal.
                    if (bestand == null) felder.Add(new Feld(basis, g, zi, Teil.Wert, false));
                    else if (!BESTEHEND.Contains(bestand)) felder.Add(new Feld(bestand, g, zi, Teil.Wert, true));

                    if (MitAus(g, z)) felder.Add(new Feld(basis + "_aus", g, zi, Teil.Aus, bestand != null));

                    // Das Nachtfenster einer Spalte; das der Heizspalte ist nacht_beginn/nacht_ende.
                    if (z == DbWerte.KOND_ZEILE_NACHT && g != Konditionierungsgroesse.Heizsoll)
                    {
                        felder.Add(new Feld(basis + "_von", g, zi, Teil.Von, false));
                        felder.Add(new Feld(basis + "_bis", g, zi, Teil.Bis, false));
                    }
                    if (z == DbWerte.KOND_ZEILE_NACHT && g == Konditionierungsgroesse.Lueftung)
                        felder.Add(new Feld(basis + "_dt", g, zi, Teil.DeltaT, false));
                }

                // Die Zeile „Vorlage" schließt die Spalte ab — keine Zelle, die Aktion des Knopfs „Übernehmen".
                felder.Add(new Feld("kond_" + gw + "_vorlage", g, -1, Teil.Vorlage, false));
            }
            return felder;
        }

        private static Dictionary<string, Feld> Verzeichnis()
        {
            var d = new Dictionary<string, Feld>(StringComparer.Ordinal);
            foreach (Feld f in Alle) d.Add(f.Schluessel, f);
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
                yield return Dialogfeld(f);
        }

        private static KiDialogFeld Dialogfeld(Feld f)
        {
            string groesse = Groessenname(f.Groesse);
            string pfad = SICHT + "." + f.Schluessel;
            CultureInfo c = CultureInfo.CurrentCulture;
            if (f.Teil == Teil.Vorlage)
                return new KiDialogFeld(f.Schluessel, pfad, groesse + " · " + MyResource.Resource.KOND_LBL_ZEILE_VORLAGE,
                                        KiParameterTyp.Wahl, string.Format(c, MyResource.Resource.KOND_TXT_KI_VORLAGE, groesse));
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
                                           string.Format(c, MyResource.Resource.KOND_TXT_KI_SAISON, groesse, teil),
                                           leerErlaubt: true, min: 1, max: 365)
                        : new KiDialogFeld(f.Schluessel, pfad, groesse + " · " + teil, KiParameterTyp.Ganzzahl,
                                           string.Format(c, MyResource.Resource.KOND_TXT_KI_FENSTER, groesse, teil),
                                           einheit: KiDialogTexte.EINHEIT_STUNDE, leerErlaubt: true,
                                           min: Nachtzeit.STUNDE_MIN, max: Nachtzeit.STUNDE_MAX);
                case Teil.DeltaT:
                    return new KiDialogFeld(f.Schluessel, pfad, groesse + " · " + MyResource.Resource.KOND_LBL_AUSSENABSTAND,
                                            KiParameterTyp.Zahl, MyResource.Resource.KOND_TXT_HINWEIS_NACHTAUSKUEHLUNG,
                                            einheit: KiDialogTexte.EINHEIT_KELVIN, leerErlaubt: true, min: 0, max: 5);
                default:
                    (string einheit, double? min, double? max) = Bereich(f);
                    return new KiDialogFeld(f.Schluessel, pfad, groesse + " · " + zeile, KiParameterTyp.Zahl,
                                            string.Format(c, MyResource.Resource.KOND_TXT_KI_ZELLE, groesse, zeile, einheit),
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
