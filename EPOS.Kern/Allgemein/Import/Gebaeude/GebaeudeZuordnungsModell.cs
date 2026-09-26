using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
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
    /// <para><b>Zahlen</b> zeigt <see cref="WertText"/> in der Anzeigekultur. Belege und Meldungen
    /// TRAGEN ihre Werte invariant (Muster <see cref="PruefMeldung"/>) — gespeichert und verglichen
    /// wird so; erst <see cref="BelegText"/> und <see cref="MeldungText"/> setzen eine Dezimalzahl
    /// darin in die Anzeigekultur (<see cref="AnzeigeWert"/>, de-DE: Komma). Das gilt nur hier, nicht
    /// global an <see cref="PruefMeldung"/>.</para>
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
                case Importherkunft.VorgabeFrei: return MyResource.Resource.GIMP_HERKUNFT_VORGABEFREI;
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
            // Eine Jahreszahl ohne Tausendertrennzeichen - aus 1965 wird nicht „1.965".
            if (zeile.Zielfeld == GebaeudeZielfelder.BAUJAHR && zeile.Wert is double jahr)
                return jahr.ToString("0.###", CultureInfo.CurrentCulture);
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

        /// <summary>
        /// Der Text eines Belegs; ohne Beleg leer. Fehlt der Schlüssel, die sprachneutrale Kurzfassung.
        /// Dezimalzahlen unter den Werten erscheinen in der Anzeigekultur (<see cref="AnzeigeWert"/>).
        /// </summary>
        public static string BelegText(GebaeudeBeleg beleg)
        {
            if (beleg == null) return "";
            string vorlage = Ressource(beleg.Schluessel);
            if (vorlage == null) return beleg.ToString();
            if (beleg.Werte.Length == 0) return vorlage;
            var werte = new object[beleg.Werte.Length];
            for (int i = 0; i < werte.Length; i++) werte[i] = AnzeigeWert(beleg.Werte[i]);
            return Formatieren(vorlage, werte);
        }

        /// <summary>
        /// <b>Ein Beleg- oder Meldungswert in der Anzeigekultur.</b> Eine invariant geschriebene
        /// Dezimalzahl (<c>18.37</c>, <c>-0.5</c>, <c>1E-05</c>) bekommt Dezimal- und Minuszeichen
        /// der aktuellen Kultur (de-DE: <c>18,37</c>); ihre Ziffern bleiben, wie sie sind. Ganzzahlen
        /// (Baujahr, Anzahl, Byte), Kennungen, Dateinamen und Texte bleiben unverändert — auch ohne
        /// Tausendertrennzeichen, damit aus dem Baujahr 2024 nicht „2.024" wird.
        /// </summary>
        public static string AnzeigeWert(string wert)
        {
            if (string.IsNullOrEmpty(wert)) return wert ?? "";
            Match m = Dezimalzahl.Match(wert);
            if (!m.Success || (!m.Groups["bruch"].Success && !m.Groups["exponent"].Success)) return wert;

            NumberFormatInfo nf = NumberFormatInfo.CurrentInfo;
            if (m.Groups["exponent"].Success)
                return double.TryParse(wert, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)
                    ? d.ToString("0.###############", CultureInfo.CurrentCulture)
                    : wert;
            return (m.Groups["minus"].Success ? nf.NegativeSign : "") + m.Groups["ganz"].Value
                   + nf.NumberDecimalSeparator + m.Groups["bruch"].Value;
        }

        /// <summary>Eine invariant geschriebene Zahl: Vorzeichen, Ganzteil, Nachkommastellen, Exponent.</summary>
        private static readonly Regex Dezimalzahl = new Regex(
            @"^(?<minus>-)?(?<ganz>\d+)(?:\.(?<bruch>\d+))?(?<exponent>[eE][+-]?\d+)?$",
            RegexOptions.CultureInvariant);

        /// <summary>
        /// Meldungen, deren Werte zwar wie Zahlen aussehen, aber keine sind — der Versionswert einer
        /// Datei („0.37") bleibt, wie er in der Datei steht.
        /// </summary>
        private static readonly HashSet<string> OhneZahlwerte = new HashSet<string>(StringComparer.Ordinal)
        {
            GbxmlImportProfil.MELDUNGSPRAEFIX + "VERSION_UNBEKANNT",
            IfcImportProfil.MELDUNGSPRAEFIX + "SCHEMA_UNBEKANNT",
        };

        /// <summary>
        /// Der Text einer Meldung — derselbe Weg wie im Ganglinienimport, mit einem Zusatz: Ein Wert,
        /// der ein Zielfeldschlüssel (<c>U_AUSSENWAND</c>) oder ein Randbedingungswert
        /// (<c>KELLER</c>) ist, erscheint mit seiner Beschriftung statt als Schlüssel, und eine
        /// Dezimalzahl in der Anzeigekultur (<see cref="AnzeigeWert"/>).
        /// </summary>
        public static string MeldungText(PruefMeldung meldung)
        {
            if (meldung == null) return "";
            bool zahlen = !OhneZahlwerte.Contains(meldung.Schluessel ?? "");
            var werte = new string[meldung.Werte.Length];
            for (int i = 0; i < werte.Length; i++)
            {
                string w = meldung.Werte[i];
                werte[i] = GebaeudeZielfelder.Finde(w) != null ? FeldText(w)
                         : w == DbWerte.GRUND_ERDREICH || w == DbWerte.GRUND_KELLER || w == DbWerte.GRUND_AUSSENLUFT
                             ? TextwertText(GebaeudeZielfelder.GRUND_RANDBEDINGUNG, w)
                             : zahlen ? AnzeigeWert(w) : w;
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
                else if (ImportherkunftWerte.IstVorgabe(z.Herkunft)) vorgabe++;
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

        /// <summary>
        /// Die Klasse, die der Satz aus dem Baujahr der Datei zog, als Index der Klappliste (0 = A …
        /// 12 = M); <c>null</c>, wenn die Datei kein Baujahr trägt. Das Baujahr führt (E47): Der Dialog
        /// zeigt diese Klasse in der Klappliste und sperrt die Wahl; die Aggregation rechnet mit ihr.
        /// </summary>
        public static int? KlasseDerDatei(GebaeudeImportSatz satz)
        {
            GebaeudeFeldzeile bak = satz?.Zeile(GebaeudeZielfelder.BAUALTERSKLASSE);
            if (bak == null || !satz.Baualtersklasse.HasValue) return null;
            if (bak.Herkunft != Importherkunft.Ifc && bak.Herkunft != Importherkunft.GbXml) return null;
            return GebaeudeStammCtrl.KlassenIndex(satz.Baualtersklasse.Value.ToString());
        }

        /// <summary>
        /// Der Hinweis unter der Klappliste: woher die Klasse kommt und wie viele der Klassenwerte
        /// (<see cref="GebaeudeVorgaben.Klassenfelder"/>) sie tatsächlich füllt. Trägt die Datei eigene
        /// U-Werte und einen g-Wert, bleiben der Klasse nur die Wärmebrücken — dann ändert eine andere
        /// Wahl wenig, und der Hinweis sagt das. Ohne Klasse der allgemeine Hinweis.
        /// </summary>
        public static string KlassenHinweis(GebaeudeImportSatz satz)
        {
            if (satz == null || !satz.Baualtersklasse.HasValue) return MyResource.Resource.GIMP_DLG_KLASSE_HINWEIS;
            int ausKlasse = 0, ausDatei = 0;
            foreach (string feld in GebaeudeVorgaben.Klassenfelder)
            {
                GebaeudeFeldzeile z = satz.Zeile(feld);
                if (z == null) continue;
                if (z.Herkunft == Importherkunft.Ifc || z.Herkunft == Importherkunft.GbXml) ausDatei++;
                else if (ImportherkunftWerte.IstVorgabe(z.Herkunft)
                         && (z.Beleg?.Schluessel == GebaeudeVorgaben.BELEG_KLASSE || z.Beleg?.Schluessel == GebaeudeVorgaben.BELEG_FREI))
                    ausKlasse++;
            }
            CultureInfo k = CultureInfo.CurrentCulture;
            string wirkung = Formatieren(MyResource.Resource.GIMP_DLG_KLASSE_WIRKUNG, ausKlasse.ToString(k),
                                         GebaeudeVorgaben.Klassenfelder.Count.ToString(k), ausDatei.ToString(k));
            if (!KlasseDerDatei(satz).HasValue) return wirkung;
            return Formatieren(MyResource.Resource.GIMP_DLG_KLASSE_AUS_BAUJAHR, satz.Baujahr?.ToString(k) ?? "") + " " + wirkung;
        }

        /// <summary>Die Plausibilität am OK-Weg — dieselbe Prüfung wie <see cref="GebaeudeImportAblauf.Pruefen"/>.</summary>
        public static IReadOnlyList<PruefMeldung> Pruefe(GebaeudeImportSatz satz, string katalogname = null)
            => GebaeudeImportAblauf.Pruefen(satz, katalogname);

        // ------------------------------------------------------------------ Welle 2: Dialog

        /// <summary>Schlüssel der Herkunft „leer" in der Oberfläche — die Persistenz kennt keinen (<see cref="ImportherkunftWerte.Wert"/>).</summary>
        public const string HERKUNFT_LEER = "LEER";

        /// <summary>
        /// Der sprachneutrale Schlüssel einer Herkunft für die Oberfläche (Stilklasse, Rückweg):
        /// der Persistenzwert (<see cref="ImportherkunftWerte"/>), für „leer" <see cref="HERKUNFT_LEER"/>.
        /// </summary>
        public static string HerkunftSchluessel(Importherkunft herkunft) => ImportherkunftWerte.Wert(herkunft) ?? HERKUNFT_LEER;

        /// <summary>Der Rückweg zu <see cref="HerkunftSchluessel"/>; ein unbekannter Schlüssel ist „leer".</summary>
        public static Importherkunft HerkunftAusSchluessel(string schluessel)
        {
            switch (schluessel)
            {
                case ImportherkunftWerte.GBXML: return Importherkunft.GbXml;
                case ImportherkunftWerte.IFC: return Importherkunft.Ifc;
                case ImportherkunftWerte.KATALOG: return Importherkunft.Katalog;
                case ImportherkunftWerte.MANUELL: return Importherkunft.Manuell;
                case ImportherkunftWerte.VORGABE: return Importherkunft.Vorgabe;
                default: return Importherkunft.Leer;
            }
        }

        /// <summary>
        /// Der Grund, warum ein Raum als beheizt oder unbeheizt gilt: vom Anwender umgestellt, die
        /// Zustandsangabe der Datei, der Treffer der Namensregel oder „keine Angabe".
        /// </summary>
        public static string RaumGrundText(GebaeudeRaumzeile raum)
        {
            if (raum == null) return "";
            if (raum.Uebersteuert) return MyResource.Resource.GIMP_RAUM_GRUND_MANUELL;
            switch (raum.Quelle)
            {
                case BeheiztQuelle.Attribut:
                    return Formatieren(MyResource.Resource.GIMP_RAUM_GRUND_ATTRIBUT, raum.Zustandsangabe ?? "");
                case BeheiztQuelle.Name:
                    return Formatieren(MyResource.Resource.GIMP_RAUM_GRUND_NAME, raum.Namenstreffer ?? raum.Name ?? "");
                case BeheiztQuelle.Lage:
                    return MyResource.Resource.GIMP_RAUM_GRUND_LAGE;
                default:
                    return MyResource.Resource.GIMP_RAUM_GRUND_ANNAHME;
            }
        }

        /// <summary>Der Anzeigename des Formats eines Profils („gbXML", „IFC") — ein Datum des Profils, nie ein Literal der Oberfläche.</summary>
        public static string FormatText(GebaeudeImportProfil profil)
            => profil == null ? "" : Ressource("GIMP_FORMAT_" + profil.Format) ?? profil.Format;

        /// <summary>Die Beschriftung einer Zonierungsregel („X4 – eine Zone je Gebäude"); eine unbekannte erscheint als Schlüssel.</summary>
        public static string ZonenregelText(string regel)
            => string.IsNullOrEmpty(regel) ? "" : Ressource("GIMP_ZONENREGEL_" + regel) ?? regel;

        /// <summary>Der Text eines Fortschrittsschritts des Ablaufs (<see cref="ImportFortschritt"/>); ohne Schlüssel leer.</summary>
        public static string FortschrittText(ImportFortschritt fortschritt)
        {
            string vorlage = Ressource(fortschritt.Schluessel);
            if (vorlage == null) return fortschritt.Schluessel ?? "";
            string[] werte = fortschritt.Werte ?? Array.Empty<string>();
            return werte.Length == 0 ? vorlage : Formatieren(vorlage, werte);
        }

        /// <summary>
        /// Eine Dateigröße zur Anzeige: unter einem Megabyte in KB, sonst in MB, je mit einer
        /// Nachkommastelle in der Anzeigekultur (1 MB = 1 024 × 1 024 Byte, wie die Grenzen der Profile).
        /// </summary>
        public static string GroesseText(long bytes)
        {
            const double KB = 1024.0, MB = 1024.0 * 1024.0;
            if (bytes < 0) bytes = 0;
            return bytes < MB
                ? (bytes / KB).ToString("0.#", CultureInfo.CurrentCulture) + " KB"
                : (bytes / MB).ToString("0.#", CultureInfo.CurrentCulture) + " MB";
        }

        // =================================================================
        //  Der Abschnitt „Baustoffe" — der Namensabgleich je Materialname
        // =================================================================

        /// <summary>Stufenschlüssel ohne Treffer.</summary>
        internal const string ABGLEICH_OHNE = "OHNE";
        /// <summary>Stufenschlüssel der ruhenden Luftschicht (N6).</summary>
        internal const string ABGLEICH_LUFTSCHICHT = "LUFTSCHICHT";
        /// <summary>Stufenschlüssel einer verworfenen Schicht ohne Stoff (N6).</summary>
        internal const string ABGLEICH_VERWORFEN = "VERWORFEN";
        /// <summary>Stufenschlüssel der eigenen Zuordnung des Anwenders (N7).</summary>
        internal const string ABGLEICH_N7 = "N7";

        /// <summary>
        /// Der sprachneutrale Schlüssel der Stufe eines Treffers — <c>N3</c>, <c>N4</c>, <c>N5</c>,
        /// <c>N7</c>, <c>LUFTSCHICHT</c>, <c>VERWORFEN</c> oder <c>OHNE</c> (auch ohne Abgleich). Stilklasse
        /// und Rückweg der Oberfläche; die Beschriftung liefert <see cref="AbgleichText"/>.
        /// </summary>
        public static string AbgleichSchluessel(Abgleichtreffer treffer)
        {
            if (treffer == null) return ABGLEICH_OHNE;
            switch (treffer.Stufe)
            {
                case Abgleichstufe.Genau: return "N3";
                case Abgleichstufe.Synonym: return "N4";
                case Abgleichstufe.Teilwort: return "N5";
                case Abgleichstufe.Anwender: return ABGLEICH_N7;
                case Abgleichstufe.Sonderfall:
                    return treffer.Sonderfall == Abgleichsonderfall.Luftschicht ? ABGLEICH_LUFTSCHICHT : ABGLEICH_VERWORFEN;
                default: return ABGLEICH_OHNE;
            }
        }

        /// <summary>Die Stufe eines Treffers als kurzer Anzeigetext („genauer Name", „Synonym", „Wortanfang", „eigene Zuordnung" …).</summary>
        public static string AbgleichText(Abgleichtreffer treffer)
        {
            string schluessel = AbgleichSchluessel(treffer);
            return Ressource("GIMP_BS_STUFE_" + schluessel) ?? schluessel;
        }

        /// <summary>Ein Katalogbaustoff als Anzeigetext: der Bezeichner, bei einer Herstellerzeile mit dem Hersteller; ohne Baustoff leer.</summary>
        public static string BaustoffText(BaustoffModel baustoff)
        {
            if (baustoff == null) return "";
            string name = baustoff.Bezeichner ?? "";
            return string.IsNullOrWhiteSpace(baustoff.Hersteller) ? name : name + " (" + baustoff.Hersteller.Trim() + ")";
        }

        /// <summary>λ, ρ und c eines Katalogbaustoffs in der Anzeigekultur, mit Einheiten; ohne Baustoff „—".</summary>
        public static string StoffwerteText(BaustoffModel baustoff)
            => baustoff == null ? MyResource.Resource.GIMP_WERT_LEER
             : Formatieren(MyResource.Resource.GIMP_BS_STOFFWERTE, ZahlText(baustoff.Lambda), ZahlText(baustoff.Rho), ZahlText(baustoff.Cp));

        /// <summary>
        /// Woher die Stoffwerte der Schichten eines Materialnamens im Vorschlag kommen: ganz aus der
        /// Datei, ganz aus dem Katalog, als ruhende Luftschicht, verworfen, in keinem vollständigen
        /// Aufbau (dann trägt das Bauteil den U-Wert) — oder gemischt, dann je Herkunft die Zahl der
        /// Schichten („Katalog 4, ohne Aufbau 1").
        /// </summary>
        public static string MaterialwerteText(GebaeudeMaterialzeile material)
        {
            if (material == null) return "";
            int n = material.Schichten, datei = material.SchichtenAusDatei, katalog = material.SchichtenAusKatalog;
            if (material.Treffer?.Sonderfall == Abgleichsonderfall.Luftschicht && datei == 0) return MyResource.Resource.GIMP_BS_WERTE_LUFTSCHICHT;
            if (material.Treffer?.Sonderfall == Abgleichsonderfall.Verwerfen && datei == 0) return MyResource.Resource.GIMP_BS_WERTE_VERWORFEN;
            if (n > 0 && datei >= n) return MyResource.Resource.GIMP_BS_WERTE_DATEI;
            if (n > 0 && katalog >= n && datei == 0) return MyResource.Resource.GIMP_BS_WERTE_KATALOG;
            if (datei == 0 && katalog == 0) return MyResource.Resource.GIMP_BS_WERTE_KEINE;
            var teile = new List<string>(3);
            if (datei > 0) teile.Add(Formatieren(MyResource.Resource.GIMP_BS_WERTE_TEIL_DATEI, datei));
            if (katalog > 0) teile.Add(Formatieren(MyResource.Resource.GIMP_BS_WERTE_TEIL_KATALOG, katalog));
            if (n - datei - katalog > 0) teile.Add(Formatieren(MyResource.Resource.GIMP_BS_WERTE_TEIL_OHNE, n - datei - katalog));
            return string.Join(", ", teile);
        }

        /// <summary>
        /// Die Zusammenfassung des Abschnitts: „16 von 20 zugeordnet, 1 ohne Treffer" — zugeordnet heißt
        /// mit Katalogbaustoff, ohne Treffer zählt nur, wer einen Baustoff bräuchte. Tragen alle Schichten
        /// vollständige Werte der Datei, sagt sie das und nennt die Treffer der Gegenprobe.
        /// </summary>
        public static string BaustoffZusammenfassung(IReadOnlyList<GebaeudeMaterialzeile> materialien)
        {
            if (materialien == null || materialien.Count == 0) return "";
            int getroffen = 0, ohne = 0, brauchen = 0;
            foreach (GebaeudeMaterialzeile m in materialien)
            {
                if (m.Treffer != null && m.Treffer.Getroffen) getroffen++;
                if (m.BrauchtAbgleich) brauchen++;
                if (IstOhneTreffer(m)) ohne++;
            }
            return brauchen == 0
                ? Formatieren(MyResource.Resource.GIMP_BS_ZUSAMMENFASSUNG_DATEI, materialien.Count, getroffen)
                : Formatieren(MyResource.Resource.GIMP_BS_ZUSAMMENFASSUNG, getroffen, materialien.Count, ohne);
        }

        /// <summary>
        /// Braucht der Name einen Baustoff und trifft keinen? Dieselbe Regel wie
        /// <see cref="GebaeudeBauteilvorschlag.OhneTreffer"/> — die gelbe Zeile des Abschnitts.
        /// </summary>
        public static bool IstOhneTreffer(GebaeudeMaterialzeile material)
            => material != null && material.BrauchtAbgleich && material.Treffer != null && material.Treffer.Stufe == Abgleichstufe.Keine;

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
