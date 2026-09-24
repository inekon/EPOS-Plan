using System;
using System.Collections.Generic;
using System.Globalization;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Regeln und Texte des Zuordnungsdialogs — oberflächenfrei</b> (Datenaustauschkonzept
    /// 2.1, Muster <see cref="ImportKonfliktModell"/>), damit Windows und iOS dieselben Zeilen
    /// gleich beschriften und der Dialog (Welle 2) keinen Anzeigetext selbst bildet.
    ///
    /// <para><b>Drei-Schichten-Regel</b>: Kein Anzeigetext ist Steuerwert. Herkunft, Gruppe,
    /// Zielfeld und Aufzählungswerte sind Werte bzw. ASCII-Schlüssel; die Beschriftung kommt
    /// allein aus <c>MyResource.Resource</c> — Gruppen <c>GIMP_GRP_*</c>, Felder
    /// <c>GIMP_FELD_*</c>, Herkunft <c>GIMP_HERKUNFT_*</c>, Belege <c>GIMP_BELEG_*</c>; die
    /// Aufzählungswerte nehmen die Schlüssel des Gebäudeeditors (<c>GEBK_BAUART_*</c>,
    /// <c>GEBK_RAND_*</c>, <c>GEB_BAK_*</c>), damit derselbe Begriff nicht zwei Texte bekommt.</para>
    ///
    /// <para><b>Zahlen</b> zeigt <see cref="WertText"/> in der Anzeigekultur; Belege und Meldungen
    /// tragen ihre Werte invariant (Muster <see cref="PruefMeldung"/>).</para>
    /// </summary>
    internal static class GebaeudeZuordnungsModell
    {
        /// <summary>Beschriftung einer Herkunft — Anzeige, nie Steuerwert.</summary>
        public static string HerkunftText(Importherkunft herkunft)
        {
            switch (herkunft)
            {
                case Importherkunft.Ifc: return MyResource.Resource.GIMP_HERKUNFT_IFC;
                case Importherkunft.GbXml: return MyResource.Resource.GIMP_HERKUNFT_GBXML;
                case Importherkunft.Katalog: return MyResource.Resource.GIMP_HERKUNFT_KATALOG;
                case Importherkunft.Vorgabe: return MyResource.Resource.GIMP_HERKUNFT_VORGABE;
                case Importherkunft.Manuell: return MyResource.Resource.GIMP_HERKUNFT_MANUELL;
                default: return MyResource.Resource.GIMP_HERKUNFT_LEER;
            }
        }

        /// <summary>Beschriftung einer Gruppe (<c>GebaeudeZielfelder.GRUPPE_*</c>).</summary>
        public static string GruppenText(string gruppe) => Ressource("GIMP_GRP_" + gruppe) ?? gruppe ?? "";

        /// <summary>Beschriftung eines Zielfelds (<see cref="GebaeudeZielfelder"/>).</summary>
        public static string FeldText(string zielfeld) => Ressource("GIMP_FELD_" + zielfeld) ?? zielfeld ?? "";

        /// <summary>
        /// Der Wert einer Zeile zur Anzeige: Zahl in der Anzeigekultur, Aufzählungswert mit dem Text
        /// des Gebäudeeditors, leer als Strich.
        /// </summary>
        public static string WertText(GebaeudeFeldzeile zeile)
        {
            if (zeile == null) return "";
            if (zeile.Textwert != null) return TextwertText(zeile.Zielfeld, zeile.Textwert);
            return ZahlText(zeile.Wert);
        }

        /// <summary>Eine Zahl in der Anzeigekultur (höchstens drei Nachkommastellen); <c>null</c> = „—".</summary>
        public static string ZahlText(double? wert)
            => wert.HasValue ? wert.Value.ToString("#,##0.###", CultureInfo.CurrentCulture) : MyResource.Resource.GIMP_WERT_LEER;

        /// <summary>Der Anzeigetext eines Aufzählungswerts; ein unbekannter Wert erscheint, wie er ist.</summary>
        public static string TextwertText(string zielfeld, string wert)
        {
            if (wert == null) return MyResource.Resource.GIMP_WERT_LEER;
            switch (zielfeld)
            {
                case GebaeudeZielfelder.BAUART:
                    if (wert == GebaeudeZielfelder.BAUART_LEICHT) return MyResource.Resource.GEBK_BAUART_LEICHT;
                    if (wert == GebaeudeZielfelder.BAUART_SCHWER) return MyResource.Resource.GEBK_BAUART_SCHWER;
                    if (wert == GebaeudeZielfelder.BAUART_SEHR_SCHWER) return MyResource.Resource.GEBK_BAUART_SEHRSCHWER;
                    return wert;
                case GebaeudeZielfelder.GRUND_RANDBEDINGUNG:
                    if (wert == DbWerte.GRUND_ERDREICH) return MyResource.Resource.GEBK_RAND_ERDREICH;
                    if (wert == DbWerte.GRUND_KELLER) return MyResource.Resource.GEBK_RAND_KELLER;
                    if (wert == DbWerte.GRUND_AUSSENLUFT) return MyResource.Resource.GEBK_RAND_AUSSENLUFT;
                    return wert;
                case GebaeudeZielfelder.BAUALTERSKLASSE:
                    string klasse = wert.Length == 1 ? Ressource("GEB_BAK_" + wert) : null;
                    return klasse == null ? wert : wert + " – " + klasse;
                default:
                    return wert;
            }
        }

        /// <summary>Eine Zeile als Satz: Gruppe, Feld, Wert mit Einheit, Herkunft.</summary>
        public static string ZeilenText(GebaeudeFeldzeile zeile)
        {
            if (zeile == null) return "";
            string einheit = zeile.HatWert && zeile.Textwert == null ? zeile.Einheit : "";
            return Formatieren(MyResource.Resource.GIMP_ZEILE, GruppenText(zeile.Gruppe), FeldText(zeile.Zielfeld),
                               WertText(zeile), einheit, HerkunftText(zeile.Herkunft)).Trim();
        }

        /// <summary>Der Text eines Belegs; ohne Beleg leer. Fehlt der Schlüssel, die sprachneutrale Kurzfassung.</summary>
        public static string BelegText(GebaeudeBeleg beleg)
        {
            if (beleg == null) return "";
            string vorlage = Ressource(beleg.Schluessel);
            if (vorlage == null) return beleg.ToString();
            return beleg.Werte.Length == 0 ? vorlage : Formatieren(vorlage, beleg.Werte);
        }

        /// <summary>
        /// Der Text einer Meldung — derselbe Weg wie im Ganglinienimport, mit einem Zusatz: Ein Wert,
        /// der ein Zielfeldschlüssel (<c>U_AUSSENWAND</c>) oder ein Randbedingungswert
        /// (<c>KELLER</c>) ist, erscheint mit seiner Beschriftung statt als Schlüssel.
        /// </summary>
        public static string MeldungText(PruefMeldung meldung)
        {
            if (meldung == null) return "";
            var werte = new string[meldung.Werte.Length];
            for (int i = 0; i < werte.Length; i++)
            {
                string w = meldung.Werte[i];
                werte[i] = GebaeudeZielfelder.Finde(w) != null ? FeldText(w)
                         : w == DbWerte.GRUND_ERDREICH || w == DbWerte.GRUND_KELLER || w == DbWerte.GRUND_AUSSENLUFT
                             ? TextwertText(GebaeudeZielfelder.GRUND_RANDBEDINGUNG, w)
                             : w;
            }
            return GanglinienProtokollText.Text(new PruefMeldung(meldung.Stufe, meldung.Schluessel, werte));
        }

        /// <summary>Der Anzeigetext der Stufe einer Meldung.</summary>
        public static string StufeText(PruefStufe stufe) => GanglinienProtokollText.StufeText(stufe);

        /// <summary>Die Schemaanzeige des Dialogkopfs, etwa „gbXML-Version 0.37".</summary>
        public static string SchemaText(GebaeudeImportProfil profil, string schemastand)
        {
            string vorlage = profil == null ? null : Ressource(profil.SchemaanzeigeSchluessel);
            string stand = string.IsNullOrEmpty(schemastand) ? MyResource.Resource.GIMP_WERT_LEER : schemastand;
            return vorlage == null ? stand : Formatieren(vorlage, stand);
        }

        /// <summary>
        /// Der Kopf des Dialogs: Datei, Gebäude, Baualtersklasse und die Bilanz der Zeilen — wie viele
        /// aus der Datei, wie viele vorbelegt, wie viele leer.
        /// </summary>
        public static string KopfText(GebaeudeImportSatz satz)
        {
            if (satz == null) return "";
            int ausDatei = 0, vorgabe = 0, leer = 0;
            foreach (GebaeudeFeldzeile z in satz.Zeilen)
            {
                if (z.Herkunft == Importherkunft.GbXml || z.Herkunft == Importherkunft.Ifc) ausDatei++;
                else if (z.Herkunft == Importherkunft.Vorgabe) vorgabe++;
                else if (!z.HatWert) leer++;
            }
            string gebaeude = string.IsNullOrWhiteSpace(satz.Gebaeudename) ? satz.Gebaeudekennung : satz.Gebaeudename;
            string klasse = satz.Baualtersklasse.HasValue
                ? TextwertText(GebaeudeZielfelder.BAUALTERSKLASSE, satz.Baualtersklasse.Value.ToString())
                : MyResource.Resource.GIMP_WERT_LEER;
            return Formatieren(MyResource.Resource.GIMP_KOPF, satz.Quelle?.Dateiname ?? "", gebaeude, klasse,
                               ausDatei.ToString(CultureInfo.CurrentCulture), vorgabe.ToString(CultureInfo.CurrentCulture),
                               leer.ToString(CultureInfo.CurrentCulture));
        }

        /// <summary>Die Plausibilität am OK-Weg — dieselbe Prüfung wie <see cref="GebaeudeImportAblauf.Pruefen"/>.</summary>
        public static IReadOnlyList<PruefMeldung> Pruefe(GebaeudeImportSatz satz) => GebaeudeImportAblauf.Pruefen(satz);

        private static string Ressource(string schluessel)
        {
            if (string.IsNullOrEmpty(schluessel)) return null;
            try
            {
                string text = MyResource.Resource.ResourceManager.GetString(schluessel, MyResource.Resource.Culture);
                return string.IsNullOrEmpty(text) ? null : text;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string Formatieren(string vorlage, params object[] werte)
        {
            try
            {
                return string.Format(CultureInfo.CurrentCulture, vorlage, werte);
            }
            catch (FormatException)
            {
                return vorlage + " (" + string.Join("; ", werte) + ")";
            }
        }
    }
}
