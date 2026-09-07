using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine Rückmeldung eines Stromspeicher-Zerlegers — ein SCHLÜSSEL und seine
    /// Platzhalterwerte, kein fertiger Satz. Dieselbe Regel wie bei
    /// <see cref="CecFortschritt"/>: Der Kern kennt keine Anzeigetexte, der Wirt
    /// übersetzt.
    /// </summary>
    public readonly struct SpeicherImportMeldung
    {
        public SpeicherImportMeldung(string schluessel, params string[] werte)
        {
            Schluessel = schluessel ?? "";
            Werte = werte ?? Array.Empty<string>();
        }

        /// <summary>Sprachneutraler Schlüssel, z. B. <c>SPIMP_MSG_GELADEN</c>.</summary>
        public string Schluessel { get; }

        /// <summary>Platzhalterwerte in der Reihenfolge <c>{0}</c>, <c>{1}</c>, …</summary>
        public string[] Werte { get; }

        public override string ToString()
        {
            return Werte.Length == 0 ? Schluessel : Schluessel + " (" + string.Join("; ", Werte) + ")";
        }
    }

    /// <summary>
    /// <b>EIN gelesener Stromspeicher aus einer fremden Liste</b> — das
    /// Zwischenformat zwischen Quelldatei und Katalog
    /// (<c>Konzept_Stromspeicherimport_EPOS-Plan.md</c>, Anwenderwunsch
    /// <b>W13‑E‑2</b> vom 07.09.2026).
    ///
    /// <para><b>Warum ein eigener Satz und nicht gleich das Modell.</b> Genau wie
    /// bei den vier VDI-Zerlegern liegt zwischen Datei und Katalogmodell eine
    /// Umrechnung, die man PRÜFEN können muss, ohne eine Datenbank zu haben:
    /// Watt nach Kilowatt, Prozent nach Bruch, englischer Zellchemietext nach
    /// deutschem Persistenzwert. Dieser Satz trägt die Quellwerte bereits in
    /// EPOS-Einheiten; <see cref="NachModell"/> setzt sie nur noch um.</para>
    ///
    /// <para><b>Was der Satz NICHT trägt.</b> Kosten (<c>Modulkosten</c>,
    /// <c>Leistungskosten</c>, <c>Investition_Fix</c>, <c>Verschleisskosten</c>),
    /// Degradation, Start-Ladezustand und zugesicherte Zyklen. Keine der vier
    /// geprüften Quellen führt sie; sie blieben erfunden. Sie bleiben deshalb 0
    /// und fallen im Rechenweg auf die Vorgaben zurück
    /// (<see cref="StromspeicherModel.WIRKUNGSGRAD_RT_VORGABE"/>,
    /// <see cref="StromspeicherModel.C_VER_VORGABE"/>) — dieselbe Handhabung wie
    /// beim Heizkesselimport, der Raumbedarf und Wartungskosten ebenfalls nicht
    /// setzt. Die vier Kostenspalten sind im Katalog ohnehin
    /// <c>AusschlussSpalten</c> (<c>KatalogRegistry</c>), zählen also beim
    /// Dublettenvergleich nicht mit.</para>
    /// </summary>
    public sealed class StromspeicherImportSatz
    {
        /// <summary>Der Hersteller, wie ihn die Quelle schreibt.</summary>
        public string Hersteller = "";

        /// <summary>Die Modell-/Typenbezeichnung, wie sie die Quelle schreibt.</summary>
        public string Modell = "";

        /// <summary>Der Zellchemietext der Quelle im Original (englisch), leer wenn keiner.</summary>
        public string Technologie = "";

        /// <summary>Dauer-Entladeleistung [kW] — <c>Tab_Stromspeicher_STAMM.Leistung</c>.</summary>
        public double LeistungKw;

        /// <summary>Kapazität [kWh] — <c>Tab_Stromspeicher_STAMM.Energie</c>.</summary>
        public double EnergieKwh;

        /// <summary>
        /// Round-Trip-Wirkungsgrad als BRUCH 0…1 — <c>Wirkungsgrad_RT</c>.
        /// <b>0 bedeutet „liefert die Quelle nicht"</b> und fällt im Rechenweg auf
        /// <see cref="StromspeicherModel.WIRKUNGSGRAD_RT_VORGABE"/> zurück
        /// (<c>StromspeicherSimCtrl.LeseParameter</c>).
        /// </summary>
        public double WirkungsgradRt;

        /// <summary>Standby-/Eigenverbrauch [W] — <c>Standby_Verbrauch</c>; 0 = unbekannt.</summary>
        public double StandbyW;

        /// <summary>Kennung der Quelldatei/Quelle, für Protokoll und Prüfung.</summary>
        public string Quelle = "";

        /// <summary>
        /// Der Bezeichner, wie ihn der Import vorbelegt: <c>Hersteller: Modell</c>.
        ///
        /// <para>Die Schreibweise mit Doppelpunkt ist KEINE Erfindung — sie ist die
        /// der CEC-Modul- und -Wechselrichterlisten, aus denen EPOS-Plan bereits
        /// liest, und der Modulimport gewinnt den Hersteller genau so zurück (Text
        /// vor dem ersten Doppelpunkt). Fehlt der Hersteller, bleibt das Modell
        /// allein stehen.</para>
        /// </summary>
        public string Bezeichner
        {
            get
            {
                string h = (Hersteller ?? "").Trim();
                string m = (Modell ?? "").Trim();
                if (h.Length == 0) return m;
                if (m.Length == 0) return h;
                return h + ": " + m;
            }
        }

        /// <summary>
        /// Der Katalogsatz aus dem Dateisatz. <paramref name="bezeichner"/> darf der
        /// vom Anwender geänderte Name sein; leer heißt <see cref="Bezeichner"/>.
        /// </summary>
        public StromspeicherModel NachModell(string bezeichner = null)
        {
            StromspeicherModel m = new StromspeicherModel();
            m.m_szBezeichner = string.IsNullOrWhiteSpace(bezeichner) ? Bezeichner : bezeichner.Trim();

            // ZUSAETZLICH zum Praefix (Migrationsschritt 68, W14a-E-10-Q7): Der
            // Bezeichner traegt den Hersteller weiterhin als "Hersteller: Modell" -
            // daran haengt die Wiedererkennung eines Satzes und der Rueckfall der
            // Anzeige -, und die neue SPALTE traegt ihn noch einmal fuer sich. Wer den
            // Bezeichner im Konfliktdialog umbenennt, verliert damit den Hersteller
            // nicht mehr.
            m.m_szFirma = (Hersteller ?? "").Trim();
            m.m_szTyp = TypAusTechnologie(Technologie);
            m.m_Leistung = LeistungKw;
            m.m_Energie = EnergieKwh;
            m.m_WirkungsgradRT = WirkungsgradRt;
            m.m_StandbyVerbrauch = StandbyW;

            // Bewusst NICHT gesetzt und deshalb Modell-Vorgabewert 0: Degradation,
            // Ladezustand, Modulkosten, Zyklen_Zugesichert, Verschleisskosten,
            // Leistungskosten, Investition_Fix. Siehe Klassenkommentar.
            return m;
        }

        /// <summary>
        /// Die Werte für die Vorprüfung auf Dubletten — genau die Importspalten des
        /// Katalogs <c>STROMSPEICHER</c> ohne dessen vier Ausschlussspalten.
        /// </summary>
        public IDictionary<string, object> Vergleichswerte(string bezeichner = null)
        {
            StromspeicherModel m = NachModell(bezeichner);
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "Typ", m.m_szTyp },
                { "Leistung", m.m_Leistung },
                { "Energie", m.m_Energie },
                { "Degradation", m.m_Degradation },
                { "Ladezustand", m.m_Ladezustand },
                { "Wirkungsgrad_RT", m.m_WirkungsgradRT },
                { "Zyklen_Zugesichert", m.m_ZyklenZugesichert },
                { "Standby_Verbrauch", m.m_StandbyVerbrauch }
            };
        }

        // =================================================================
        //  Umrechnungen, die jeder Zerleger braucht
        // =================================================================

        /// <summary>
        /// Der deutsche Persistenzwert der Spalte <c>Typ</c> zum englischen
        /// Zellchemietext einer Liste.
        ///
        /// <para><b>Die Spalte ist Freitext</b> (<c>DbWerte</c> beim Eintrag
        /// <c>SP_TYP_LITHIUM_IONEN</c>), der Bestand führt „Lithium-Ionen",
        /// „Lithium-Ionen-Akkus" und „Lithium-Eisen-Phosphat". Diese Tabelle bildet
        /// die fünf Technologietexte ab, die die CEC-Liste am 07.09.2026 wirklich
        /// führt; alles Unbekannte bleibt UNVERÄNDERT stehen, statt still zu
        /// „Lithium-Ionen" zu werden — ein falscher Wert wäre schlimmer als ein
        /// englischer.</para>
        /// </summary>
        public static string TypAusTechnologie(string roh)
        {
            string t = (roh ?? "").Trim();
            if (t.Length == 0) return "";

            switch (t.ToLowerInvariant())
            {
                case "lithium iron phosphate":
                case "lithium iron":
                case "lifepo4":
                    return DbWerte.SP_TYP_LITHIUM_EISEN_PHOSPHAT;
                case "lithium ion":
                case "lithium-ion":
                    return DbWerte.SP_TYP_LITHIUM_IONEN;
                case "lithium titanate oxide":
                    return DbWerte.SP_TYP_LITHIUM_TITANAT;
                case "lithium nickel manganese cobalt":
                    return DbWerte.SP_TYP_LITHIUM_NMC;
                case "iron flow battery":
                    return DbWerte.SP_TYP_EISEN_REDOX_FLOW;
                default:
                    return t;
            }
        }

        /// <summary>
        /// Der Round-Trip-Wirkungsgrad als Bruch aus einer Herstellerangabe.
        ///
        /// <para><b>Die CEC-Liste mischt zwei Schreibweisen.</b> Die Spalte heißt
        /// „Manufacturer Declared Roundtrip Efficiency (%, AC-AC)", trägt aber
        /// neben 85, 93 und 97,5 auch 0,88 — derselbe Wert, einmal in Prozent und
        /// einmal als Bruch (gemessen am 07.09.2026: 1 103 numerische Angaben,
        /// Kleinstwert 0,88, Größtwert 97,5). Die Weiche ist deshalb
        /// wertabhängig und nicht spaltenabhängig:</para>
        /// <list type="bullet">
        ///   <item>Wert &gt; 1 gilt als Prozent und wird durch 100 geteilt,</item>
        ///   <item>Wert in (0…1] gilt bereits als Bruch,</item>
        ///   <item>alles andere (Text, „No Information Submitted", 0, negativ,
        ///         &gt; 100) ergibt 0 = „nicht geliefert".</item>
        /// </list>
        /// <para>Ein Wert ≤ 0,5 wäre für einen Batteriespeicher unglaubwürdig; er
        /// bleibt trotzdem stehen, weil die Prüfung in die Maske gehört (S1) und
        /// nicht in den Zerleger — der liest, er urteilt nicht.</para>
        /// </summary>
        public static double WirkungsgradAusText(string text)
        {
            double roh = Zahl(text);
            if (roh <= 0.0) return 0.0;
            if (roh > 100.0) return 0.0;
            return roh > 1.0 ? roh / 100.0 : roh;
        }

        /// <summary>
        /// Eine Zahl aus einer Fremddatei — kulturunabhängig. Punkt ist das
        /// Dezimalzeichen; ein Komma wird als zweites Dezimalzeichen zugelassen
        /// (Excel-Ausleitung), wie in <c>CecWechselrichterDienst.Zahl</c>. Ein
        /// Tausendertrennzeichen kommt in keiner der geprüften Listen vor und
        /// würde hier zum Dezimalzeichen — deshalb wird ein Text mit MEHR als
        /// einem Komma verworfen statt geraten.
        /// </summary>
        public static double Zahl(string s)
        {
            string t = (s ?? "").Trim();
            if (t.Length == 0) return 0.0;
            if (t == "-" || t.Equals("N/A", StringComparison.OrdinalIgnoreCase)) return 0.0;

            int kommas = 0;
            foreach (char c in t) if (c == ',') kommas++;
            if (kommas > 1) return 0.0;
            if (kommas == 1) t = t.Replace(",", ".");

            double d;
            return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out d) ? d : 0.0;
        }

        /// <summary>
        /// Der Inhalt einer Textdatei — <b>UTF-8, sonst ANSI</b>.
        ///
        /// <para>Die Listen kommen heute als UTF-8 (mit oder ohne Vorspann): so
        /// liefert die CEC ihre Mappe, so schreibt <c>bslib</c> seine CSV-Datei,
        /// und so leitet „CSV UTF-8" aus Excel aus. Der alte Excel-Ausgang
        /// „CSV (Trennzeichen-getrennt)" schreibt dagegen Windows-1252 — dort
        /// stünde jedes „ö" sonst als Ersatzzeichen im Katalog. Entschieden wird
        /// deshalb am BYTESTROM: Was sich streng als UTF-8 lesen lässt, IST UTF-8;
        /// alles andere geht durch <see cref="AnsiEncoding"/>, den die
        /// VDI-3805-Zerleger seit jeher verwenden.</para>
        /// </summary>
        public static string LiesText(string pfad)
        {
            byte[] rohdaten = System.IO.File.ReadAllBytes(pfad);
            try
            {
                var streng = new System.Text.UTF8Encoding(false, true);
                int vorspann = (rohdaten.Length >= 3 && rohdaten[0] == 0xEF
                                && rohdaten[1] == 0xBB && rohdaten[2] == 0xBF) ? 3 : 0;
                return streng.GetString(rohdaten, vorspann, rohdaten.Length - vorspann);
            }
            catch (DecoderFallbackException)
            {
                return AnsiEncoding.Get().GetString(rohdaten);
            }
        }

        /// <summary>
        /// Eine Textdatei als Tabelle. Das Trennzeichen ist das häufigere von
        /// <c>;</c> und <c>,</c> in der ersten belegten Zeile — dieselbe Regel wie
        /// beim Modul- und Wechselrichterimport. Zerlegt wird mit
        /// <see cref="NReco.Csv.CsvReader"/>, weil die CEC-Kopfzeile
        /// Zeilenumbrüche IN Anführungszeichen führt; ein zeilenweiser Zerleger
        /// zerrisse sie.
        /// </summary>
        public static List<string[]> TabelleAusText(string text)
        {
            var raus = new List<string[]>();
            if (string.IsNullOrEmpty(text)) return raus;

            char trenner = ',';
            foreach (string zeile in text.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(zeile)) continue;
                int semi = 0, komma = 0;
                foreach (char c in zeile) { if (c == ';') semi++; else if (c == ',') komma++; }
                trenner = semi > komma ? ';' : ',';
                break;
            }

            using (var leser = new System.IO.StringReader(text))
            {
                var csv = new NReco.Csv.CsvReader(leser, trenner.ToString());

                // NReco setzt KEINE Vorgabe fuer BufferSize - ohne diese Zeile
                // teilt der Leser durch null. Die Groesse begrenzt die Laenge
                // EINES Satzes: Die CEC-Liste fuehrt 45 Spalten, darunter eine
                // Bemerkungsspalte mit ganzen Saetzen, und ihre Kopfzeile traegt
                // Zeilenumbrueche in Anfuehrungszeichen. 256 kB je Satz sind
                // reichlich und kosten nichts (Heizkesselimport: 32 kB,
                // Spotpreisleser: 64 kB).
                csv.BufferSize = 262144;
                csv.TrimFields = true;
                while (csv.Read())
                {
                    string[] felder = new string[csv.FieldsCount];
                    for (int i = 0; i < csv.FieldsCount; i++) felder[i] = csv[i] ?? "";
                    raus.Add(felder);
                }
            }
            return raus;
        }

        /// <summary>
        /// Der Vergleichsname einer Kopfzellenbeschriftung: klein, ohne
        /// Zeilenumbrüche, ohne Mehrfachleerzeichen, ohne Fußnotenziffern am Ende.
        ///
        /// <para><b>Warum die Fußnotenziffern weg müssen.</b> Die CEC-Liste
        /// beschriftet ihre Spalten mit angehängten Verweisen —
        /// „Maximum Continuous Discharge Rate4", „Certified JA12 Control
        /// Strategies1". Die Ziffer gehört zur Fußnote, nicht zum Spaltennamen,
        /// und sie ändert sich mit der Fußnotenliste. Eine Erkennung, die sie
        /// mitliest, bricht bei der nächsten Auflage.</para>
        /// </summary>
        public static string Kopfname(string roh)
        {
            string t = (roh ?? "").Trim();
            if (t.Length == 0) return "";

            var sb = new System.Text.StringBuilder(t.Length);
            bool letztesLeer = false;
            foreach (char c in t)
            {
                char z = (c == '\r' || c == '\n' || c == '\t') ? ' ' : c;
                if (z == ' ')
                {
                    if (!letztesLeer && sb.Length > 0) sb.Append(' ');
                    letztesLeer = true;
                }
                else
                {
                    sb.Append(char.ToLowerInvariant(z));
                    letztesLeer = false;
                }
            }

            string s = sb.ToString().Trim();
            // Fußnotenziffern am Ende abschneiden - aber nur, wenn davor ein
            // Buchstabe steht ("...rate4" ja, "ul 9540" nein).
            int ende = s.Length;
            while (ende > 0 && s[ende - 1] >= '0' && s[ende - 1] <= '9') ende--;
            if (ende > 0 && ende < s.Length && char.IsLetter(s[ende - 1])) s = s.Substring(0, ende);
            return s;
        }
    }
}
