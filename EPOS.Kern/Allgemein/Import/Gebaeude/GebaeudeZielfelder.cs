using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Ein Zielfeld des Gebäudeimports: Schlüssel, Gruppe, Einheit, Reihenfolge.</summary>
    public sealed class GebaeudeZielfeld
    {
        internal GebaeudeZielfeld(string schluessel, string gruppe, string einheit, int reihenfolge,
                                  bool istText = false, bool nurPruefgroesse = false)
        {
            Schluessel = schluessel;
            Gruppe = gruppe;
            Einheit = einheit;
            Reihenfolge = reihenfolge;
            IstText = istText;
            NurPruefgroesse = nurPruefgroesse;
        }

        /// <summary>Sprachneutraler ASCII-Schlüssel (<see cref="GebaeudeZielfelder"/>), nie ein Anzeigetext.</summary>
        public string Schluessel { get; }

        /// <summary>Sprachneutraler ASCII-Schlüssel der Gruppe (<c>GebaeudeZielfelder.GRUPPE_*</c>).</summary>
        public string Gruppe { get; }

        /// <summary>Einheitenzeichen (sprachneutral, Glossar § 11); leer bei Aufzählungsfeldern.</summary>
        public string Einheit { get; }

        /// <summary>Position in der Zeilenliste des Dialogs, ab 1.</summary>
        public int Reihenfolge { get; }

        /// <summary>Trägt das Feld einen Aufzählungswert (<see cref="GebaeudeFeldzeile.Textwert"/>) statt einer Zahl?</summary>
        public bool IstText { get; }

        /// <summary>Nur Prüfgröße — wird angezeigt und geprüft, aber nie geschrieben.</summary>
        public bool NurPruefgroesse { get; }
    }

    /// <summary>
    /// <b>Die EINE Liste der Zielfelder des Einzonen-Wegs</b> (Stufe G4c, Zonenregel X4:
    /// eine Zone je Gebäude, geschrieben wird in <c>Tab_Gebaeude</c>). Beide Leser — gbXML jetzt,
    /// IFC mit G4a — füllen dieselben Felder (Datenaustauschkonzept 2.1): Der Satz beschreibt das
    /// ZIEL, nicht die Datei.
    ///
    /// <para><b>Orientierung.</b> Die Felder folgen dem Feldsatz des Gebäudeeditors
    /// (<c>EPOS.UI/Dialoge/Bedarf/GebaeudeKatalogDaten.cs</c>) und den Spalten von
    /// <c>Tab_Gebaeude</c>; die Spalte steht je Schlüssel im Kommentar. Geschrieben wird erst mit
    /// Welle 3 der Stufe G4c.</para>
    ///
    /// <para><b>Was bewusst KEIN Zielfeld ist.</b> Rahmenanteil, Verschattungsfaktor, Masseanteil
    /// außen, Innenflächenfaktor, Strahlungsanteil und Heizleistungsgrenze: gbXML kennt keine
    /// dieser Größen (Datenaustauschkonzept 3.4, Zeile „— → Rahmenanteil"), die Spalten bleiben
    /// NULL und damit bei der Vorgabe des Eingangsbauers (<c>GebaeudeFestwerte.VORGABE_*</c>). Die
    /// alte Einzelspalte <c>Luftwechselrate</c> auch nicht — der Luftwechsel geht nach D12 auf die
    /// Infiltration.</para>
    /// </summary>
    public static class GebaeudeZielfelder
    {
        // ------------------------------------------------------------------ Gruppen

        /// <summary>Gruppe Kenngrößen.</summary>
        public const string GRUPPE_KENNGROESSEN = "KENNGROESSEN";
        /// <summary>Gruppe Außenwand.</summary>
        public const string GRUPPE_AUSSENWAND = "AUSSENWAND";
        /// <summary>Gruppe Fenster.</summary>
        public const string GRUPPE_FENSTER = "FENSTER";
        /// <summary>Gruppe Dach.</summary>
        public const string GRUPPE_DACH = "DACH";
        /// <summary>Gruppe Grundfläche.</summary>
        public const string GRUPPE_GRUND = "GRUND";
        /// <summary>Gruppe sonstige Flächen.</summary>
        public const string GRUPPE_SONSTIGE = "SONSTIGE";
        /// <summary>Gruppe Wärmebrücken.</summary>
        public const string GRUPPE_WAERMEBRUECKEN = "WAERMEBRUECKEN";
        /// <summary>Gruppe Lüftung.</summary>
        public const string GRUPPE_LUEFTUNG = "LUEFTUNG";
        /// <summary>Gruppe Sollwerte.</summary>
        public const string GRUPPE_SOLLWERTE = "SOLLWERTE";

        // ------------------------------------------------------------------ Kenngrößen

        /// <summary>Nutzfläche [m²] — beheizte Netto-Grundfläche (E13); Spalten <c>Wohnflaeche_gesamt</c> und <c>Nutzflaeche</c>.</summary>
        public const string NUTZFLAECHE = "NUTZFLAECHE";
        /// <summary>Raumhöhe [m] (<c>Raumhoehe</c>).</summary>
        public const string RAUMHOEHE = "RAUMHOEHE";
        /// <summary>Beheiztes Volumen [m³] — nur Prüfgröße gegen Nutzfläche × Raumhöhe, keine Spalte.</summary>
        public const string VOLUMEN = "VOLUMEN";
        /// <summary>Fläche je Nutzer [m²] (<c>Flaeche_Nutzer</c>; <c>Bewohner</c> rechnet die Hülle daraus).</summary>
        public const string FLAECHE_JE_NUTZER = "FLAECHE_JE_NUTZER";
        /// <summary>Innere Wärmegewinne [W], zeitlich konstant (<c>Interne_Waermegewinne</c>, im Modell <c>InnereGewinne_W</c>).</summary>
        public const string INNERE_GEWINNE = "INNERE_GEWINNE";
        /// <summary>Baualtersklasse, Buchstabe A…U (<c>Baualtersklasse</c>) — Anwenderangabe, steuert die Vorgaben.</summary>
        public const string BAUALTERSKLASSE = "BAUALTERSKLASSE";
        /// <summary>Bauart <see cref="BAUART_LEICHT"/>/<see cref="BAUART_SCHWER"/>/<see cref="BAUART_SEHR_SCHWER"/>; der Editor bildet daraus die <c>Bauweise</c>.</summary>
        public const string BAUART = "BAUART";
        /// <summary>Bauweise (Speichermasse) [Wh/K] (<c>Bauweise</c>, im Modell <c>Bauweise_WhK</c>).</summary>
        public const string BAUWEISE = "BAUWEISE";

        // ------------------------------------------------------------------ Außenwand

        /// <summary>Fläche Außenwand [m²], netto nach Fensterabzug (U14) (<c>Flaeche_Außenwand</c>).</summary>
        public const string FLAECHE_AUSSENWAND = "FLAECHE_AUSSENWAND";
        /// <summary>U-Wert Außenwand [W/(m²K)] (<c>k_Wert_Außenwand</c>).</summary>
        public const string U_AUSSENWAND = "U_AUSSENWAND";

        // ------------------------------------------------------------------ Fenster

        /// <summary>Fensterfläche Nord [m²] (<c>Fensterflaeche_Nord</c>).</summary>
        public const string FENSTER_NORD = "FENSTER_NORD";
        /// <summary>Fensterfläche Ost [m²] (<c>Fensterflaeche_Ost</c>; die Summe Ost + West geht in <c>Fensterflaeche_Ost_West</c>).</summary>
        public const string FENSTER_OST = "FENSTER_OST";
        /// <summary>Fensterfläche Süd [m²] (<c>Fensterflaeche_Sued</c>).</summary>
        public const string FENSTER_SUED = "FENSTER_SUED";
        /// <summary>Fensterfläche West [m²] (<c>Fensterflaeche_West</c>).</summary>
        public const string FENSTER_WEST = "FENSTER_WEST";
        /// <summary>Gesamte Fensterfläche [m²] (<c>gesamte_Fensterflaeche</c>) — die Summe der vier Sektoren.</summary>
        public const string FENSTER_GESAMT = "FENSTER_GESAMT";
        /// <summary>U-Wert Fenster [W/(m²K)] (<c>k_Wert_Fenster</c>).</summary>
        public const string U_FENSTER = "U_FENSTER";
        /// <summary>Gesamtenergiedurchlassgrad g [–] (<c>Fensterdurchlassgrad</c>).</summary>
        public const string G_WERT = "G_WERT";

        // ------------------------------------------------------------------ Dach

        /// <summary>Dachfläche [m²] (<c>Dachflaeche</c>).</summary>
        public const string FLAECHE_DACH = "FLAECHE_DACH";
        /// <summary>U-Wert Dach [W/(m²K)] (<c>k_Wert_Dachflaeche</c>).</summary>
        public const string U_DACH = "U_DACH";

        // ------------------------------------------------------------------ Grundfläche

        /// <summary>Grundfläche [m²] (<c>Grundflaeche</c>).</summary>
        public const string FLAECHE_GRUND = "FLAECHE_GRUND";
        /// <summary>U-Wert Grundfläche [W/(m²K)] (<c>k_Wert_Grundflaeche</c>).</summary>
        public const string U_GRUND = "U_GRUND";
        /// <summary>Randbedingung der Grundfläche, <c>DbWerte.GRUND_*</c> (<c>Grundflaeche_Randbedingung</c>).</summary>
        public const string GRUND_RANDBEDINGUNG = "GRUND_RANDBEDINGUNG";

        // ------------------------------------------------------------------ Sonstige

        /// <summary>Sonstige Flächen [m²] (<c>Sonstige_Flaechen</c>) — Außentüren, Flächen gegen Unbeheizt, Außenluftböden.</summary>
        public const string FLAECHE_SONSTIGE = "FLAECHE_SONSTIGE";
        /// <summary>U-Wert Sonstiges [W/(m²K)] (<c>k_Wert_Sonstiges</c>).</summary>
        public const string U_SONSTIGE = "U_SONSTIGE";

        // ------------------------------------------------------------------ Wärmebrücken

        /// <summary>ψ Anschluss Fenster–Wand [W/(mK)] (<c>WBVK_Anschluß_Fenster_Wand</c>).</summary>
        public const string PSI_FENSTER_WAND = "PSI_FENSTER_WAND";
        /// <summary>ψ Anschluss Wand–Dach [W/(mK)] (<c>WBVK_Anschluß_Wand_Dach</c>).</summary>
        public const string PSI_WAND_DACH = "PSI_WAND_DACH";
        /// <summary>ψ Anschluss Außenwand–Kellerdecke [W/(mK)] (<c>WBVK_Anschluß_Außenwand_Kellerdecke</c>).</summary>
        public const string PSI_AUSSENWAND_KELLER = "PSI_AUSSENWAND_KELLER";
        /// <summary>Anschlusslänge Fenster–Wand [m] (<c>Abmessung_Anschluß_Fenster_Wand</c>) — bleibt leer (U15).</summary>
        public const string LAENGE_FENSTER_WAND = "LAENGE_FENSTER_WAND";
        /// <summary>Anschlusslänge Wand–Dach [m] (<c>Abmessung_Anschluß_Wand_Dach</c>) — bleibt leer (U15).</summary>
        public const string LAENGE_WAND_DACH = "LAENGE_WAND_DACH";
        /// <summary>Anschlusslänge Außenwand–Kellerdecke [m] (<c>Abmessung_Anschluß_Außenwand_Kellerdecke</c>) — bleibt leer (U15).</summary>
        public const string LAENGE_AUSSENWAND_KELLER = "LAENGE_AUSSENWAND_KELLER";

        // ------------------------------------------------------------------ Lüftung

        /// <summary>Infiltration [1/h] (<c>Luftwechsel_Infiltration</c>) — trägt nach D12 den ganzen gelesenen Luftwechsel.</summary>
        public const string LUFTWECHSEL_INFILTRATION = "LUFTWECHSEL_INFILTRATION";
        /// <summary>Nutzerlüftung [1/h] (<c>Luftwechsel_Nutzer</c>) — bleibt nach D12 leer (= Vorgabe des Modells).</summary>
        public const string LUFTWECHSEL_NUTZER = "LUFTWECHSEL_NUTZER";

        // ------------------------------------------------------------------ Sollwerte

        /// <summary>Heizsollwert am Tag [°C] (<c>Raumsolltemperatur_Tag</c>) — nur, wenn die Datei ihn trägt.</summary>
        public const string SOLL_TAG = "SOLL_TAG";

        // ------------------------------------------------------------------ Aufzählungswerte

        /// <summary>Bauart „leicht" — Persistenz über <see cref="Gebaeudebauweise.LEICHT"/>.</summary>
        public const string BAUART_LEICHT = "LEICHT";
        /// <summary>Bauart „schwer" — die Vorgabe ohne auswertbare Schichten (Umsetzungskonzept 3.4, Zeile Bauweise).</summary>
        public const string BAUART_SCHWER = "SCHWER";
        /// <summary>Bauart „sehr schwer".</summary>
        public const string BAUART_SEHR_SCHWER = "SEHR_SCHWER";

        private static readonly GebaeudeZielfeld[] _alle = Bauen();

        /// <summary>Alle Zielfelder in der Reihenfolge des Dialogs.</summary>
        public static IReadOnlyList<GebaeudeZielfeld> Alle => _alle;

        /// <summary>Das Zielfeld zu einem Schlüssel; <c>null</c>, wenn es keins gibt.</summary>
        public static GebaeudeZielfeld Finde(string schluessel)
        {
            foreach (GebaeudeZielfeld f in _alle)
                if (string.Equals(f.Schluessel, schluessel, StringComparison.Ordinal)) return f;
            return null;
        }

        /// <summary>Der Bauart-Index des Gebäudeeditors (<see cref="Gebaeudebauweise"/>) zu einem Bauart-Schlüssel; −1 = unbekannt.</summary>
        public static int BauartIndex(string bauart)
        {
            if (string.Equals(bauart, BAUART_LEICHT, StringComparison.Ordinal)) return Gebaeudebauweise.LEICHT;
            if (string.Equals(bauart, BAUART_SCHWER, StringComparison.Ordinal)) return Gebaeudebauweise.SCHWER;
            if (string.Equals(bauart, BAUART_SEHR_SCHWER, StringComparison.Ordinal)) return Gebaeudebauweise.SEHR_SCHWER;
            return -1;
        }

        private static GebaeudeZielfeld[] Bauen()
        {
            int n = 0;
            GebaeudeZielfeld F(string s, string g, string e, bool text = false, bool pruef = false)
                => new GebaeudeZielfeld(s, g, e, ++n, text, pruef);

            return new[]
            {
                F(NUTZFLAECHE, GRUPPE_KENNGROESSEN, "m²"),
                F(RAUMHOEHE, GRUPPE_KENNGROESSEN, "m"),
                F(VOLUMEN, GRUPPE_KENNGROESSEN, "m³", pruef: true),
                F(FLAECHE_JE_NUTZER, GRUPPE_KENNGROESSEN, "m²"),
                F(INNERE_GEWINNE, GRUPPE_KENNGROESSEN, "W"),
                F(BAUALTERSKLASSE, GRUPPE_KENNGROESSEN, "", text: true),
                F(BAUART, GRUPPE_KENNGROESSEN, "", text: true),
                F(BAUWEISE, GRUPPE_KENNGROESSEN, "Wh/K"),

                F(FLAECHE_AUSSENWAND, GRUPPE_AUSSENWAND, "m²"),
                F(U_AUSSENWAND, GRUPPE_AUSSENWAND, "W/(m²K)"),

                F(FENSTER_NORD, GRUPPE_FENSTER, "m²"),
                F(FENSTER_OST, GRUPPE_FENSTER, "m²"),
                F(FENSTER_SUED, GRUPPE_FENSTER, "m²"),
                F(FENSTER_WEST, GRUPPE_FENSTER, "m²"),
                F(FENSTER_GESAMT, GRUPPE_FENSTER, "m²"),
                F(U_FENSTER, GRUPPE_FENSTER, "W/(m²K)"),
                F(G_WERT, GRUPPE_FENSTER, "–"),

                F(FLAECHE_DACH, GRUPPE_DACH, "m²"),
                F(U_DACH, GRUPPE_DACH, "W/(m²K)"),

                F(FLAECHE_GRUND, GRUPPE_GRUND, "m²"),
                F(U_GRUND, GRUPPE_GRUND, "W/(m²K)"),
                F(GRUND_RANDBEDINGUNG, GRUPPE_GRUND, "", text: true),

                F(FLAECHE_SONSTIGE, GRUPPE_SONSTIGE, "m²"),
                F(U_SONSTIGE, GRUPPE_SONSTIGE, "W/(m²K)"),

                F(PSI_FENSTER_WAND, GRUPPE_WAERMEBRUECKEN, "W/(mK)"),
                F(PSI_WAND_DACH, GRUPPE_WAERMEBRUECKEN, "W/(mK)"),
                F(PSI_AUSSENWAND_KELLER, GRUPPE_WAERMEBRUECKEN, "W/(mK)"),
                F(LAENGE_FENSTER_WAND, GRUPPE_WAERMEBRUECKEN, "m"),
                F(LAENGE_WAND_DACH, GRUPPE_WAERMEBRUECKEN, "m"),
                F(LAENGE_AUSSENWAND_KELLER, GRUPPE_WAERMEBRUECKEN, "m"),

                F(LUFTWECHSEL_INFILTRATION, GRUPPE_LUEFTUNG, "1/h"),
                F(LUFTWECHSEL_NUTZER, GRUPPE_LUEFTUNG, "1/h"),

                F(SOLL_TAG, GRUPPE_SOLLWERTE, "°C"),
            };
        }
    }
}
