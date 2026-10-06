using System;
using System.Collections.Generic;
using System.Globalization;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Feldkarte des Blatts „Nutzungsprofile" für den Assistenten</b> (Konzept Nutzungsprofile 6.1,
    /// NP-F3, NP-F22) — EIN Profil, aus dem der Dialogkatalog die Felder des Profileditors und der
    /// Zuordnungstabelle erzeugt und über das die Sichtklasse des Gebäudeeditors die Kennwerte als
    /// FELDTAFEL (<c>IKiFeldtafel</c>) beantwortet: keine zweite Feldliste von Hand (Muster
    /// <see cref="KiKonditionierungsfelder"/>).
    /// </summary>
    /// <remarks>
    /// <para><b>Der Profileditor.</b> <c>np_&lt;kennwert&gt;</c> trägt einen Kennwert des im Blatt gewählten
    /// Profils — Nummer, Name, Beschreibung, Nutzungszeiten, Woche, Sollwerte, Außenluft samt Einheit und die
    /// Lasten. Gesetzt wird der ENTWURF im Editor; in den Katalog geht er erst mit dem Knopf „Speichern" des
    /// Blatts, der eine Handlung des Anwenders bleibt (NP-F3: der Katalog gilt projektübergreifend). Ein
    /// ausgeliefertes Profil ist schreibgeschützt. <c>np_kategorie</c> nennt die Kategorie des Profils und
    /// ist nur lesbar — sie wechselt man über den Katalogbaum.</para>
    /// <para><b>Die Zuordnung.</b> Die Zeilen der Tabelle (Art, Schlüssel, Profil) stehen als RASTER zum
    /// LESEN unter <see cref="ZUORDNUNGEN"/>, Kennzeichen die Nummer ab 1. Profilwahl, neue Zeile und
    /// Löschen schreiben sofort in den Katalog und bleiben Klicks des Anwenders.</para>
    /// <para><b>Ohne offenes Blatt</b> lesen die Felder leer, und Setzen wird mit Grund abgelehnt.</para>
    /// </remarks>
    public static class KiNutzungsprofilfelder
    {
        /// <summary>Der Typname der Sichtklasse, die die Felder als Feldtafel beantwortet.</summary>
        public const string SICHT = "GebaeudeKatalogKiSicht";

        /// <summary>Der Pfad der Zuordnungszeilen an der Sichtklasse (Raster zum Lesen).</summary>
        public const string ZUORDNUNGEN = SICHT + ".Nutzungsprofilzuordnungen[].";

        /// <summary>Das Kennzeichen einer Zuordnungszeile: die Nummer ab 1.</summary>
        public const string ZEILENKENNZEICHEN = "Nummer";

        /// <summary>Der Anfang jedes Schlüssels der Karte.</summary>
        public const string PRAEFIX = "np_";

        /// <summary>Die Einheit der flächenbezogenen Lasten.</summary>
        public const string EINHEIT_W_M2 = "W/m²";

        /// <summary>Der Schlüssel der Außenlufteinheit „Luftwechsel" (Wahl).</summary>
        public const string LUFT_JE_STUNDE = "1/h";

        /// <summary>Der Schlüssel der Außenlufteinheit „Volumenstrom je Fläche" (Wahl).</summary>
        public const string LUFT_JE_FLAECHE = "m³/(h·m²)";

        /// <summary>Der Kennwert eines Felds — die Spalte des Profils, die es trägt.</summary>
        public enum Kennwert
        {
            Nummer, Name, Beschreibung, Kategorie,
            NutzungVon, NutzungBis, BetriebVon, BetriebBis, Woche, TageJahr, Feiertage,
            HeizSoll, HeizAusserhalb, HeizAus, KuehlSoll, KuehlAusserhalb, KuehlAus,
            LuftEinheit, Luft, LuftAusserhalb,
            PersonenFlaeche, PersonenWaerme, PersonenAnteil, PersonenAusserhalb,
            Geraete, GeraeteAnteil, GeraeteAusserhalb, Beleuchtung, BeleuchtungAnteil
        }

        /// <summary>Ein Feld des Profileditors: Schlüssel, Kennwert, Typ, Einheit und Grenzen wie im Blatt.</summary>
        public sealed class Feld
        {
            internal Feld(string schluessel, Kennwert kennwert, KiParameterTyp typ, Func<string> bezeichnung,
                          string einheit = null, double? min = null, double? max = null, bool nurLesen = false)
            {
                Schluessel = schluessel;
                Kennwert = kennwert;
                Typ = typ;
                _bezeichnung = bezeichnung;
                Einheit = einheit;
                Min = min;
                Max = max;
                NurLesen = nurLesen;
            }

            private readonly Func<string> _bezeichnung;

            /// <summary>Der Feldname des Katalogs und zugleich der Schlüssel der Tafel.</summary>
            public string Schluessel { get; }

            /// <summary>Der Kennwert des Profils, den das Feld trägt.</summary>
            public Kennwert Kennwert { get; }

            /// <summary>Der Typ des Katalogfelds.</summary>
            public KiParameterTyp Typ { get; }

            /// <summary>Die Beschriftung des Felds im Blatt (in der Sprache der Oberfläche).</summary>
            public string Bezeichnung => _bezeichnung();

            /// <summary>Die Einheit; <c>null</c> = keine.</summary>
            public string Einheit { get; }

            /// <summary>Die untere Grenze wie im Blatt; <c>null</c> = keine.</summary>
            public double? Min { get; }

            /// <summary>Die obere Grenze wie im Blatt; <c>null</c> = keine.</summary>
            public double? Max { get; }

            /// <summary>Nur lesbar (die Kategorie; die Nutzungstage im Jahr, abgeleitet nach E93).</summary>
            public bool NurLesen { get; }

            /// <summary>Ein Anteil: im Profil 0 … 1, im Blatt und in der Karte in Prozent.</summary>
            public bool IstAnteil => Einheit == KiDialogTexte.EINHEIT_PROZENT;
        }

        /// <summary>Alle Felder des Profileditors in der Reihenfolge des Blatts.</summary>
        public static IReadOnlyList<Feld> Alle { get; } = new[]
        {
            new Feld("np_nummer", Kennwert.Nummer, KiParameterTyp.Text, () => MyResource.Resource.RNP_LBL_NUMMER),
            new Feld("np_name", Kennwert.Name, KiParameterTyp.Text, () => MyResource.Resource.RNP_LBL_NAME),
            new Feld("np_beschreibung", Kennwert.Beschreibung, KiParameterTyp.Text, () => MyResource.Resource.RNP_LBL_BESCHREIBUNG),
            new Feld("np_kategorie", Kennwert.Kategorie, KiParameterTyp.Text, () => MyResource.Resource.RNP_KI_KATEGORIE_NAME, nurLesen: true),
            new Feld("np_nutzung_von", Kennwert.NutzungVon, KiParameterTyp.Ganzzahl, () => MyResource.Resource.RNP_LBL_NUTZUNG_VON, KiDialogTexte.EINHEIT_STUNDE, 0, 24),
            new Feld("np_nutzung_bis", Kennwert.NutzungBis, KiParameterTyp.Ganzzahl, () => MyResource.Resource.RNP_LBL_NUTZUNG_BIS, KiDialogTexte.EINHEIT_STUNDE, 0, 24),
            new Feld("np_betrieb_von", Kennwert.BetriebVon, KiParameterTyp.Ganzzahl, () => MyResource.Resource.RNP_LBL_BETRIEB_VON, KiDialogTexte.EINHEIT_STUNDE, 0, 24),
            new Feld("np_betrieb_bis", Kennwert.BetriebBis, KiParameterTyp.Ganzzahl, () => MyResource.Resource.RNP_LBL_BETRIEB_BIS, KiDialogTexte.EINHEIT_STUNDE, 0, 24),
            new Feld("np_woche", Kennwert.Woche, KiParameterTyp.Text, () => MyResource.Resource.RNP_LBL_WOCHE),
            new Feld("np_tage_jahr", Kennwert.TageJahr, KiParameterTyp.Ganzzahl, () => MyResource.Resource.RNP_LBL_TAGE_JAHR, null, 0, 365, nurLesen: true),
            new Feld("np_feiertage", Kennwert.Feiertage, KiParameterTyp.Wahrheitswert, () => MyResource.Resource.RNP_LBL_FEIERTAGE),
            new Feld("np_heiz_soll", Kennwert.HeizSoll, KiParameterTyp.Zahl, () => MyResource.Resource.RNP_LBL_HEIZ_SOLL, KiDialogTexte.EINHEIT_GRAD_C, 5, 40),
            new Feld("np_heiz_ausserhalb", Kennwert.HeizAusserhalb, KiParameterTyp.Zahl, () => MyResource.Resource.RNP_LBL_HEIZ_AUSSERHALB, KiDialogTexte.EINHEIT_GRAD_C, 5, 40),
            new Feld("np_heiz_aus", Kennwert.HeizAus, KiParameterTyp.Wahrheitswert, () => MyResource.Resource.RNP_LBL_HEIZ_AUS),
            new Feld("np_kuehl_soll", Kennwert.KuehlSoll, KiParameterTyp.Zahl, () => MyResource.Resource.RNP_LBL_KUEHL_SOLL, KiDialogTexte.EINHEIT_GRAD_C, 10, 40),
            new Feld("np_kuehl_ausserhalb", Kennwert.KuehlAusserhalb, KiParameterTyp.Zahl, () => MyResource.Resource.RNP_LBL_KUEHL_AUSSERHALB, KiDialogTexte.EINHEIT_GRAD_C, 10, 40),
            new Feld("np_kuehl_aus", Kennwert.KuehlAus, KiParameterTyp.Wahrheitswert, () => MyResource.Resource.RNP_LBL_KUEHL_AUS),
            new Feld("np_luft_einheit", Kennwert.LuftEinheit, KiParameterTyp.Wahl, () => MyResource.Resource.RNP_LBL_LUFT_EINHEIT),
            new Feld("np_luft", Kennwert.Luft, KiParameterTyp.Zahl, () => MyResource.Resource.RNP_LBL_LUFT, null, 0, 20),
            new Feld("np_luft_ausserhalb", Kennwert.LuftAusserhalb, KiParameterTyp.Zahl, () => MyResource.Resource.RNP_LBL_LUFT_AUSSERHALB, null, 0, 20),
            new Feld("np_personen_flaeche", Kennwert.PersonenFlaeche, KiParameterTyp.Zahl, () => MyResource.Resource.RNP_LBL_PERSONEN_FLAECHE, KiDialogTexte.EINHEIT_M2, 1, 200),
            new Feld("np_personen_waerme", Kennwert.PersonenWaerme, KiParameterTyp.Zahl, () => MyResource.Resource.RNP_LBL_PERSONEN_WAERME, KiDialogTexte.EINHEIT_W, 0, 500),
            new Feld("np_personen_anteil", Kennwert.PersonenAnteil, KiParameterTyp.Zahl, () => MyResource.Resource.RNP_LBL_PERSONEN_ANTEIL, KiDialogTexte.EINHEIT_PROZENT, 0, 100),
            new Feld("np_personen_ausserhalb", Kennwert.PersonenAusserhalb, KiParameterTyp.Zahl, () => MyResource.Resource.RNP_LBL_PERSONEN_AUSSERHALB, KiDialogTexte.EINHEIT_PROZENT, 0, 100),
            new Feld("np_geraete", Kennwert.Geraete, KiParameterTyp.Zahl, () => MyResource.Resource.RNP_LBL_GERAETE, EINHEIT_W_M2, 0, 500),
            new Feld("np_geraete_anteil", Kennwert.GeraeteAnteil, KiParameterTyp.Zahl, () => MyResource.Resource.RNP_LBL_GERAETE_ANTEIL, KiDialogTexte.EINHEIT_PROZENT, 0, 100),
            new Feld("np_geraete_ausserhalb", Kennwert.GeraeteAusserhalb, KiParameterTyp.Zahl, () => MyResource.Resource.RNP_LBL_GERAETE_AUSSERHALB, KiDialogTexte.EINHEIT_PROZENT, 0, 100),
            new Feld("np_beleuchtung", Kennwert.Beleuchtung, KiParameterTyp.Zahl, () => MyResource.Resource.RNP_LBL_BELEUCHTUNG, EINHEIT_W_M2, 0, 500),
            new Feld("np_beleuchtung_anteil", Kennwert.BeleuchtungAnteil, KiParameterTyp.Zahl, () => MyResource.Resource.RNP_LBL_BELEUCHTUNG_ANTEIL, KiDialogTexte.EINHEIT_PROZENT, 0, 100),
        };

        /// <summary>Die Spalten der Zuordnungstabelle: Schlüssel und Eigenschaft der Zeile.</summary>
        public static IReadOnlyList<(string Schluessel, string Eigenschaft)> Zuordnungsspalten { get; } = new[]
        {
            ("np_zuordnung_art", "Art"),
            ("np_zuordnung_schluessel", "Schluessel"),
            ("np_zuordnung_profil", "Profil"),
        };

        private static readonly Dictionary<string, Feld> NACH_SCHLUESSEL = Verzeichnis();

        /// <summary>Das Feld zu einem Schlüssel; <c>null</c> = kein Feld des Profileditors.</summary>
        public static Feld Finde(string schluessel)
            => schluessel != null && NACH_SCHLUESSEL.TryGetValue(schluessel, out Feld f) ? f : null;

        /// <summary>Der Schlüssel der Wahl „Einheit der Außenluft".</summary>
        public const string LUFT_EINHEIT = "np_luft_einheit";

        /// <summary>Die Einträge der Wahl „Einheit der Außenluft": Schlüssel und Text sind die Einheit.</summary>
        public static IReadOnlyList<KiWahleintrag> Lufteinheiten { get; } = new[]
        {
            new KiWahleintrag(LUFT_JE_STUNDE, LUFT_JE_STUNDE),
            new KiWahleintrag(LUFT_JE_FLAECHE, LUFT_JE_FLAECHE),
        };

        /// <summary>Der Grund, wenn das Blatt „Nutzungsprofile" nicht offen ist.</summary>
        public static string GrundOhneBlatt => MyResource.Resource.RNP_KI_OHNE_BLATT;

        /// <summary>Der Grund, wenn im Blatt kein Profil gewählt ist.</summary>
        public static string GrundOhneProfil => MyResource.Resource.RNP_GRUND_OHNE_WAHL;

        /// <summary>Der Grund, wenn das Profil zur Auslieferung gehört.</summary>
        public static string GrundAusgeliefert => MyResource.Resource.RNP_TXT_AUSGELIEFERT;

        /// <summary>Der Grund, wenn die Woche nicht aus sieben Ziffern 0/1 besteht — die Erläuterung des Felds.</summary>
        public static string GrundWoche => MyResource.Resource.RNP_KI_WOCHE_ERL;

        /// <summary>Der Grund, wenn ein Wert der Wahl „Einheit der Außenluft" nicht in der Liste steht.</summary>
        public static string GrundLufteinheit
            => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RNP_KI_LUFT_EINHEIT_FALSCH,
                             LUFT_JE_STUNDE, LUFT_JE_FLAECHE);

        /// <summary>
        /// <b>Die Felder des Katalogs</b> — je Feld des Profileditors eines (Pfad = Sichtklasse + Schlüssel,
        /// die Sichtklasse löst ihn als FELDTAFEL auf), dazu die Spalten der Zuordnungstabelle als Raster
        /// zum Lesen.
        /// </summary>
        public static IEnumerable<KiDialogFeld> Dialogfelder()
        {
            CultureInfo c = CultureInfo.CurrentCulture;
            foreach (Feld f in Alle)
            {
                string erl;
                switch (f.Kennwert)
                {
                    case Kennwert.Kategorie: erl = MyResource.Resource.RNP_KI_KATEGORIE_ERL; break;
                    case Kennwert.Woche: erl = MyResource.Resource.RNP_KI_WOCHE_ERL; break;
                    case Kennwert.LuftEinheit: erl = MyResource.Resource.RNP_KI_LUFT_EINHEIT_ERL; break;
                    default:
                        erl = string.Format(c, MyResource.Resource.RNP_KI_PROFILWERT_ERL, f.Bezeichnung);
                        if (f.Kennwert == Kennwert.Luft || f.Kennwert == Kennwert.LuftAusserhalb)
                            erl += " " + MyResource.Resource.RNP_KI_LUFT_ERL;
                        break;
                }
                yield return new KiDialogFeld(f.Schluessel, SICHT + "." + f.Schluessel, f.Bezeichnung, f.Typ, erl,
                                              einheit: f.Einheit, leerErlaubt: f.Typ != KiParameterTyp.Wahl,
                                              nurLesen: f.NurLesen, min: f.Min, max: f.Max);
            }

            string[] namen =
            {
                MyResource.Resource.RNP_LBL_ART, MyResource.Resource.RNP_LBL_SCHLUESSEL, MyResource.Resource.RNP_LBL_PROFIL
            };
            for (int i = 0; i < Zuordnungsspalten.Count; i++)
            {
                (string schluessel, string eigenschaft) = Zuordnungsspalten[i];
                yield return new KiDialogFeld(schluessel, ZUORDNUNGEN + eigenschaft,
                                              MyResource.Resource.RNP_KI_ZUORDNUNG_NAME + " · " + namen[i],
                                              KiParameterTyp.Text,
                                              string.Format(c, MyResource.Resource.RNP_KI_ZUORDNUNG_ERL, namen[i]),
                                              leerErlaubt: true, zeilenkennzeichen: ZEILENKENNZEICHEN, nurLesen: true);
            }
        }

        private static Dictionary<string, Feld> Verzeichnis()
        {
            var d = new Dictionary<string, Feld>(StringComparer.Ordinal);
            foreach (Feld f in Alle) d.Add(f.Schluessel, f);
            return d;
        }
    }
}
