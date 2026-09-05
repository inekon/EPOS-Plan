using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ein gelesener Katalogsatz, fertig zum Anzeigen, Pruefen und Schreiben</b>
    /// (iU9-W13.0c).
    ///
    /// <para><b>Warum es die Klasse gibt.</b> Jede der vier Einlesemasken trug ein
    /// <c>FuelleModellwerte</c> — die Umrechnung vom Dateisatz auf das Katalogmodell.
    /// Beim Heizkessel ist das die einzige echte RECHNUNG der Vierlinge
    /// (Brennstoffdeckel aus der Tabelle, Oel-/Gas-Weiche, Wirkungsgrad durch 100,
    /// Platzhalter 1), bei der Waermepumpe die zweite (Regelungstext aus der
    /// Stufenzahl, <c>(int)</c>-Abschneiden). Fachaussagen im Formularcode; hier
    /// stehen sie im Kern, wo iOS sie auch erreicht.</para>
    ///
    /// <para><b>Der Bezeichner kommt von aussen.</b> In allen vier Masken ist
    /// <c>textBox_Name</c> das EINZIGE Detailfeld ohne <c>Enabled = false</c> — der
    /// Anwender darf den Namen aendern, sonst nichts. Deshalb nehmen
    /// <see cref="Vergleichswerte"/>, <see cref="Anlegen"/> und
    /// <see cref="Ueberschreiben"/> den Bezeichner als Parameter statt ihn aus dem
    /// Dateisatz zu lesen. Der Bestand tat das nur bei Heizkessel und
    /// Pufferspeicher; Solar und Waermepumpe lasen den Listeneintrag und liessen
    /// eine Handkorrektur ins Leere laufen (Befund W13-B26, Abweichung A-4).</para>
    /// </summary>
    public abstract class KatalogImportSatz
    {
        /// <summary>Der Bezeichner, wie er in der Datei steht — die Vorbelegung des Feldes.</summary>
        public abstract string Name { get; }

        /// <summary>Der Hersteller — zweite Spalte des Suchfilters.</summary>
        public abstract string Firma { get; }

        /// <summary>
        /// Der Wert, ueber den der Zahlenfilter der Maske laeuft (th. Leistung,
        /// Volumen, Aperturflaeche). Ein nicht parsbarer Text zaehlt als 0 — der
        /// Listenaufbau darf daran nicht abbrechen, den Fehler meldet erst die
        /// Uebernahme (Kommentar <c>Form_Heizkessel_einlesen:65-67</c>).
        /// </summary>
        public abstract double Filterwert { get; }

        /// <summary>
        /// Die Anzeigetexte der Detailfelder, Schluessel → Text. Die Schluessel sind
        /// die aus <see cref="KatalogImportProfil.Detailfelder"/>.
        /// </summary>
        public abstract IDictionary<string, string> Detailwerte { get; }

        /// <summary>
        /// Die Werte, die in den <see cref="ImportKandidat"/> der Vorpruefung gehen —
        /// genau die <c>ImportSpalten</c> des Katalogs.
        /// </summary>
        public abstract IDictionary<string, object> Vergleichswerte(string bezeichner);

        /// <summary>Legt den Satz im Katalog an (transaktional, im Controller).</summary>
        public abstract VdiUebernahmeErgebnis Anlegen(string bezeichner);

        /// <summary>Aktualisiert die Importfelder des Bestandssatzes mit der Id.</summary>
        public abstract VdiUebernahmeErgebnis Ueberschreiben(int bestandsId);

        /// <summary>Text eines Detailfeldes, leer wenn es der Satz nicht fuehrt.</summary>
        public string Detailwert(string schluessel)
        {
            string wert;
            return Detailwerte.TryGetValue(schluessel, out wert) ? (wert ?? "") : "";
        }

        /// <summary>Ein <c>double</c> als Anzeigetext — invariant, ohne Gruppenzeichen.</summary>
        protected static string Text(double wert)
        {
            return wert.ToString("0.######", CultureInfo.InvariantCulture);
        }

        /// <summary>Der Filterwert aus einem Textfeld: nicht parsbar zaehlt als 0.</summary>
        protected static double FilterAus(string text)
        {
            double wert;
            return ZahlText.Parsen(text, out wert) ? wert : 0.0;
        }
    }

    // ==================================================================
    // Heizkessel — VDI 3805 Blatt 3
    // ==================================================================

    /// <summary>
    /// Ein Kessel aus Blatt 3. Traegt die einzige echte Rechnung der Vierlinge
    /// (<c>Form_Heizkessel_einlesen.FuelleModellwerte</c> :451-499), woertlich.
    /// </summary>
    public sealed class HeizkesselImportSatz : KatalogImportSatz
    {
        private readonly Attrribute_hk _satz;

        public HeizkesselImportSatz(Attrribute_hk satz) { _satz = satz; }

        public override string Name => _satz.m_szName;
        public override string Firma => _satz.m_szFirma;
        public override double Filterwert => FilterAus(_satz.m_szThLeistung);

        public override IDictionary<string, string> Detailwerte => new Dictionary<string, string>
        {
            { KatalogImportProfil.FeldName,  _satz.m_szName },
            { KatalogImportProfil.FeldFirma, _satz.m_szFirma },
            { "BAUART",       _satz.m_szBauart },
            { "THLEISTUNG",   _satz.m_szThLeistung },
            { "BRENNSTOFF",   _satz.m_szBrennstoff },
            { "WIRKUNGSGRAD", _satz.m_szWirkungsgrad },
            { "VERLUSTE",     _satz.m_szVerluste }
        };

        /// <summary>
        /// Das Katalogmodell aus dem Dateisatz — woertlich der Rumpf von
        /// <c>FuelleModellwerte</c>.
        ///
        /// <para><b>Der Brennstoffdeckel</b> kommt aus <c>Tab_Brennstoff_Stamm</c>
        /// selbst (<c>MAX(ID)</c>), weil die Tabelle waechst: Der alte harte Deckel
        /// (&gt; 22 → 23) machte die spaeter ergaenzten Eintraege Sonstige (24) und
        /// Wasserstoff (25) still zu Fernwaerme. Ohne Tabellenwert bleibt 25.
        /// <paramref name="maxBrennstoff"/> laesst den Aufrufer die Abfrage EINMAL
        /// je Vorgang fuehren statt einmal je Kandidat (Befund W13-B17).</para>
        ///
        /// <para><b>Nicht gesetzt und deshalb Modell-Vorgabewert 0:</b> Raumbedarf,
        /// SO2, Staub, Investitionskosten, Wartungskosten, Nutzungsdauer
        /// (<c>DbWerte.cs:337</c>). Das bleibt so.</para>
        /// </summary>
        public HeizkesselModel NachModell(string bezeichner, int maxBrennstoff)
        {
            HeizkesselModel model = new HeizkesselModel();

            model.Name = bezeichner;
            model.Firma = _satz.m_szFirma;
            model.Beschreibung = _satz.m_szBauart;
            model.Ptherm = ZahlText.NachDouble(_satz.m_szThLeistung);

            // Der Brennstoffindex zuerst: Er wird als model.Brennstoff gespeichert
            // und entscheidet, aus welchem Feld Simulation und Wirtschaftlichkeit
            // den Wirkungsgrad spaeter lesen.
            int brennstoffindex = ZahlText.NachInt(_satz.m_szBrennstoffIndex);
            if (brennstoffindex > maxBrennstoff) brennstoffindex = maxBrennstoff;
            model.Brennstoff = brennstoffindex;

            double wirkungsgrad = ZahlText.NachDouble(_satz.m_szWirkungsgrad) / 100;
            if (brennstoffindex > 0)
            {
                // Oel = Index 6-9 und 18-22, wie SimulationSPK.Stunde_Abschluss und
                // der Brennstofffilter der Dialoge.
                bool oel = (brennstoffindex >= 6 && brennstoffindex <= 9)
                        || (brennstoffindex >= 18 && brennstoffindex <= 22);
                if (oel) model.Wirkungsgrad_Oel = wirkungsgrad;
                else model.Wirkungsgrad_Gas = wirkungsgrad;
            }
            else
            {
                // Ohne Brennstoffindex ueber die Brennstoffart des VDI-Satzes
                // (0 = Gas, 1 = Oel, sonst beide Felder). Liefert die Datei gar
                // keine Kennung, bleibt es beim Bestandsverhalten Gas.
                int art = ZahlText.NachInt(_satz.szBrennstoffart);
                if (art == 0) model.Wirkungsgrad_Gas = wirkungsgrad;
                else if (art == 1) model.Wirkungsgrad_Oel = wirkungsgrad;
                else model.Wirkungsgrad_Gas = model.Wirkungsgrad_Oel = wirkungsgrad;
            }

            if (model.Wirkungsgrad_Gas == 0 && model.Wirkungsgrad_Oel == 0)
                model.Wirkungsgrad_Gas = model.Wirkungsgrad_Oel = 1;

            model.Betriebsbereitschaftverlust = ZahlText.NachDouble(_satz.m_szVerluste);
            model.NOx = ZahlText.NachDouble(_satz.m_szNOX);
            model.CO2 = ZahlText.NachDouble(_satz.m_szCO2);
            model.CO = ZahlText.NachDouble(_satz.m_szCO);

            return model;
        }

        /// <summary>Der Deckel aus der Brennstofftabelle — EINMAL je Vorgang (W13-B17).</summary>
        public static int MaxBrennstoff()
        {
            object o = DataRepository.ExecuteScalar("SELECT MAX(ID) FROM Tab_Brennstoff_Stamm");
            return (o != null && o != DBNull.Value) ? Convert.ToInt32(o) : 25;
        }

        /// <summary>Der Deckel, den der Ablauf einmal ermittelt und weiterreicht.</summary>
        public int Deckel { get; set; } = 25;

        public override IDictionary<string, object> Vergleichswerte(string bezeichner)
        {
            HeizkesselModel m = NachModell(bezeichner, Deckel);
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "Firma", m.Firma },
                { "Ptherm", m.Ptherm },
                { "Brennstoff", m.Brennstoff },
                { "Wirkungsgrad_Gas", m.Wirkungsgrad_Gas },
                { "Wirkungsgrad_Öl", m.Wirkungsgrad_Oel },
                { "Raumbedarf", m.Raumbedarf },
                { "CO2", m.CO2 },
                { "SO2", m.SO2 },
                { "NOx", m.NOx },
                { "CO", m.CO },
                { "Staub", m.Staub },
                { "Betriebsbereitschaftverlust", m.Betriebsbereitschaftverlust }
            };
        }

        public override VdiUebernahmeErgebnis Anlegen(string bezeichner)
        {
            return new HeizkesselStammCtrl().ImportUebernehmen(NachModell(bezeichner, Deckel));
        }

        public override VdiUebernahmeErgebnis Ueberschreiben(int bestandsId)
        {
            // HeizkesselStammCtrl erbt vom Modell - UpdateImport schreibt genau
            // die Importfelder; ID, Bezeichner und Anwenderfelder bleiben stehen.
            HeizkesselStammCtrl stamm = new HeizkesselStammCtrl();
            HeizkesselModel m = NachModell(Name, Deckel);
            stamm.Name = m.Name;
            stamm.Firma = m.Firma;
            stamm.Beschreibung = m.Beschreibung;
            stamm.Ptherm = m.Ptherm;
            stamm.Brennstoff = m.Brennstoff;
            stamm.Wirkungsgrad_Gas = m.Wirkungsgrad_Gas;
            stamm.Wirkungsgrad_Oel = m.Wirkungsgrad_Oel;
            stamm.Raumbedarf = m.Raumbedarf;
            stamm.CO2 = m.CO2;
            stamm.SO2 = m.SO2;
            stamm.NOx = m.NOx;
            stamm.CO = m.CO;
            stamm.Staub = m.Staub;
            stamm.Betriebsbereitschaftverlust = m.Betriebsbereitschaftverlust;

            return stamm.UpdateImport(bestandsId)
                ? VdiUebernahmeErgebnis.Ueberschrieben
                : VdiUebernahmeErgebnis.Fehler;
        }
    }

    // ==================================================================
    // Pufferspeicher — VDI 3805 Blatt 20
    // ==================================================================

    /// <summary>Ein Speicher aus Blatt 20. Keine Rechnung, nur Zuweisungen.</summary>
    public sealed class PufferSpImportSatz : KatalogImportSatz
    {
        private readonly Attrribute_psp _satz;

        public PufferSpImportSatz(Attrribute_psp satz) { _satz = satz; }

        public override string Name => _satz.m_szName;
        public override string Firma => _satz.m_szFirma;
        public override double Filterwert => FilterAus(_satz.m_szVolumen);

        public override IDictionary<string, string> Detailwerte => new Dictionary<string, string>
        {
            { KatalogImportProfil.FeldName,  _satz.m_szName },
            { KatalogImportProfil.FeldFirma, _satz.m_szFirma },
            { "SPEICHERTYP", _satz.m_szTyp },
            { "VOLUMEN",     _satz.m_szVolumen },
            { "VERLUSTE",    _satz.m_szVerluste }
        };

        /// <summary>
        /// Das Katalogmodell. Der Speichertyp ist ein PERSISTENZWERT
        /// („Solarspeicher" / „Pufferspeicher" / „Kombispeicher") und bleibt deutsch
        /// und eingefroren — er steht so in <c>Tab_Pufferspeicher_STAMM</c>.
        /// </summary>
        public PufferSpModel NachModell(string bezeichner)
        {
            return new PufferSpModel
            {
                Name = bezeichner,
                Firma = _satz.m_szFirma,
                Speichertyp = _satz.m_szTyp,
                Betriebsbereitschaftverlust = ZahlText.NachDouble(_satz.m_szVerluste),
                Gesamtvolumen = ZahlText.NachInt(_satz.m_szVolumen)
            };
        }

        public override IDictionary<string, object> Vergleichswerte(string bezeichner)
        {
            PufferSpModel m = NachModell(bezeichner);
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "Hersteller", m.Firma },
                { "Speichertyp", m.Speichertyp },
                { "Bereitschaftsverluste", m.Betriebsbereitschaftverlust },
                { "Gesamtvolumen", m.Gesamtvolumen }
            };
        }

        public override VdiUebernahmeErgebnis Anlegen(string bezeichner)
        {
            return new PufferSpStammCtrl().ImportUebernehmen(NachModell(bezeichner));
        }

        public override VdiUebernahmeErgebnis Ueberschreiben(int bestandsId)
        {
            PufferSpStammCtrl ctrl = new PufferSpStammCtrl();
            PufferSpModel m = NachModell(Name);
            ctrl.Name = m.Name;
            ctrl.Firma = m.Firma;
            ctrl.Speichertyp = m.Speichertyp;
            ctrl.Betriebsbereitschaftverlust = m.Betriebsbereitschaftverlust;
            ctrl.Gesamtvolumen = m.Gesamtvolumen;

            return ctrl.UpdateImport(bestandsId)
                ? VdiUebernahmeErgebnis.Ueberschrieben
                : VdiUebernahmeErgebnis.Fehler;
        }
    }

    // ==================================================================
    // Solarkollektoren — VDI 3805 Blatt 19
    // ==================================================================

    /// <summary>
    /// Ein Kollektor aus Blatt 19 (<c>InitDatensatzUpdate</c> :245-262).
    /// <c>m_Leistung</c> und <c>m_kdiff</c> sind im Parser als „liefert Blatt 19
    /// nicht" vermerkt und bleiben 0.
    /// </summary>
    public sealed class SolarkollektorImportSatz : KatalogImportSatz
    {
        private readonly Attrribute_st _satz;

        public SolarkollektorImportSatz(Attrribute_st satz) { _satz = satz; }

        public override string Name => _satz.m_szName;
        public override string Firma => _satz.m_szFirma;
        public override double Filterwert => _satz.m_Aperturfläche;

        public override IDictionary<string, string> Detailwerte => new Dictionary<string, string>
        {
            { KatalogImportProfil.FeldName,  _satz.m_szName },
            { KatalogImportProfil.FeldFirma, _satz.m_szFirma },
            { "BAUART",       _satz.m_szBauart },
            // Der Bestand befuellte textBox_Beschreibung NIE, obwohl der Parser sie
            // liest und InitDatensatzUpdate sie speichert (Befund W13-B25).
            { "BESCHREIBUNG", _satz.m_szBeschreibung },
            { "APERTUR",      Text(_satz.m_Aperturfläche) },
            { "LEISTUNG",     Text(_satz.m_Leistung) },
            { "H0",           Text(_satz.m_h0) },
            { "A1",           Text(_satz.m_a1) },
            { "A2",           Text(_satz.m_a2) },
            { "KDIR",         Text(_satz.m_kdir) },
            { "KDIFF",        Text(_satz.m_kdiff) }
        };

        public SolarkollektorenModel NachModell(string bezeichner)
        {
            return new SolarkollektorenModel
            {
                m_szKollektorname = bezeichner,
                m_szFirma = _satz.m_szFirma,
                m_szBeschreibung = _satz.m_szBeschreibung,
                m_szKollektortyp = _satz.m_szBauart,
                m_h0 = _satz.m_h0,
                m_k1 = _satz.m_a1,
                m_k2 = _satz.m_a2,
                m_Kdir = _satz.m_kdir,
                m_Kdfu = _satz.m_kdiff,
                m_Modulfläche = _satz.m_Modulfläche,
                m_Aperturfläche = _satz.m_Aperturfläche
            };
        }

        public override IDictionary<string, object> Vergleichswerte(string bezeichner)
        {
            SolarkollektorenModel m = NachModell(bezeichner);
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "Firma", m.m_szFirma },
                { "Kollektortyp", m.m_szKollektortyp },
                { "Modulflaeche", m.m_Modulfläche },
                { "Aperturflaeche", m.m_Aperturfläche },
                { "h0", m.m_h0 },
                { "k1", m.m_k1 },
                { "k2", m.m_k2 },
                { "Kdir", m.m_Kdir },
                { "Kdfu", m.m_Kdfu },
                { "Vorlauf", (int)m.m_Vorlauf },
                { "Ruecklauf", (int)m.m_Ruecklauf }
            };
        }

        public override VdiUebernahmeErgebnis Anlegen(string bezeichner)
        {
            return new SolarkollektorenStammCtrl().ImportUebernehmen(NachModell(bezeichner));
        }

        public override VdiUebernahmeErgebnis Ueberschreiben(int bestandsId)
        {
            SolarkollektorenStammCtrl ctrl = new SolarkollektorenStammCtrl();
            SolarkollektorenModel m = NachModell(Name);
            ctrl.m_szKollektorname = m.m_szKollektorname;
            ctrl.m_szFirma = m.m_szFirma;
            ctrl.m_szBeschreibung = m.m_szBeschreibung;
            ctrl.m_szKollektortyp = m.m_szKollektortyp;
            ctrl.m_h0 = m.m_h0;
            ctrl.m_k1 = m.m_k1;
            ctrl.m_k2 = m.m_k2;
            ctrl.m_Kdir = m.m_Kdir;
            ctrl.m_Kdfu = m.m_Kdfu;
            ctrl.m_Modulfläche = m.m_Modulfläche;
            ctrl.m_Aperturfläche = m.m_Aperturfläche;

            return ctrl.UpdateImport(bestandsId)
                ? VdiUebernahmeErgebnis.Ueberschrieben
                : VdiUebernahmeErgebnis.Fehler;
        }
    }

    // ==================================================================
    // Waermepumpe — VDI 3805 Blatt 22
    // ==================================================================

    /// <summary>
    /// Eine Waermepumpe aus Blatt 22 (<c>FuelleModellwerte</c> :278-303) samt ihren
    /// Kennlinien. Die einzige Auspraegung, die DREI Tabellen schreibt.
    /// </summary>
    public sealed class WaermepumpeImportSatz : KatalogImportSatz
    {
        private readonly WaermepumpenImport _parser;
        private readonly int _index;

        public WaermepumpeImportSatz(WaermepumpenImport parser, int index)
        {
            _parser = parser;
            _index = index;
        }

        private _attrribute Satz => _parser._list[_index];

        public override string Name => Satz.szName;
        public override string Firma => Satz.szFirma;
        public override double Filterwert => FilterAus(Satz.szThLeistung);

        public override IDictionary<string, string> Detailwerte => new Dictionary<string, string>
        {
            { KatalogImportProfil.FeldName,  Satz.szName },
            { KatalogImportProfil.FeldFirma, Satz.szFirma },
            { "TYP",           Satz.szWPTyp },
            { "AUFSTELLUNG",   Satz.szAufstellung },
            { "THLEISTUNG",    Satz.szThLeistung },
            { "ZUSATZHEIZUNG", Satz.szElektrZuheizung },
            { "STUFEN",        Satz.szStufen },
            { "MAXVORLAUF",    Satz.szMaxVorlauf },
            { "WIRKUNGSGRAD",  Satz.szCOP },
            { "KUEHLLEISTUNG", Satz.szKuehlleistung }
        };

        /// <summary>
        /// Besetzt den Stamm-Controller (er erbt vom Modell) mit den Importwerten.
        ///
        /// <para><b>Die vier Regelungstexte sind PERSISTENZWERTE</b> („stetig",
        /// „einstufig", „zweistufig", „mehrstufig") und stehen so in
        /// <c>Tab_WP_STAMM.Regelung</c> — sie bleiben deutsch und eingefroren. Im
        /// Bestand standen sie als Literale im Formularcode (Befund W13-B31); hier
        /// stehen sie an einer Stelle, an der man sie findet.</para>
        ///
        /// <para><b>Woertlich behalten trotz Befund:</b> Die Kuehlleistung wird nur
        /// gesetzt, WENN eine elektrische Zuheizung angegeben ist — zwei fachlich
        /// unabhaengige Groessen haengen aneinander (Befund W13-B32, Anwenderfrage).
        /// <c>Baujahr</c> und <c>maxPtherm</c> werden nie gesetzt und gehen mit
        /// ihrem Vorgabewert in den Vergleich (Befund W13-B30).</para>
        /// </summary>
        internal void NachStamm(WPStammCtrl ctrl, string bezeichner)
        {
            ctrl.WPName = bezeichner;

            int stufen = ZahlText.NachInt(Satz.szStufen);
            if (stufen == 0) ctrl.Regelung = REGELUNG_STETIG;
            else if (stufen == 1) ctrl.Regelung = REGELUNG_EINSTUFIG;
            else if (stufen == 2) ctrl.Regelung = REGELUNG_ZWEISTUFIG;
            else ctrl.Regelung = REGELUNG_MEHRSTUFIG;

            ctrl.Aufstellung = Satz.szAufstellung;
            ctrl.Firma = Satz.szFirma;
            // (int) schneidet ab, es rundet nicht - Bestandsverhalten.
            ctrl.Nennleistung = (int)ZahlText.NachDouble(Satz.szThLeistung);
            ctrl.Typ = Satz.szWPTyp;
            ctrl.Bauart = Satz.szBauart;

            if (Satz.szElektrZuheizung != "")
            {
                ctrl.Heizung = (int)ZahlText.NachDouble(Satz.szElektrZuheizung);
                ctrl.Kuehlleistung = ZahlText.NachDouble(Satz.szKuehlleistung);
            }
        }

        /// <summary>Persistenzwert der Spalte <c>Regelung</c>: modulierend.</summary>
        public const string REGELUNG_STETIG = "stetig";
        /// <summary>Persistenzwert der Spalte <c>Regelung</c>: eine Stufe.</summary>
        public const string REGELUNG_EINSTUFIG = "einstufig";
        /// <summary>Persistenzwert der Spalte <c>Regelung</c>: zwei Stufen.</summary>
        public const string REGELUNG_ZWEISTUFIG = "zweistufig";
        /// <summary>Persistenzwert der Spalte <c>Regelung</c>: mehr als zwei Stufen.</summary>
        public const string REGELUNG_MEHRSTUFIG = "mehrstufig";

        public override IDictionary<string, object> Vergleichswerte(string bezeichner)
        {
            WPStammCtrl probe = new WPStammCtrl();
            NachStamm(probe, bezeichner);
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "Firma", probe.Firma },
                { "Typ", probe.Typ },
                { "Baujahr", probe.Baujahr },
                { "Aufstellung", probe.Aufstellung },
                { "Nennleistung", probe.Nennleistung },
                { "maxPtherm", probe.maxPTherm },
                { "Heizung", probe.Heizung },
                { "Regelung", probe.Regelung },
                { "Bauart", probe.Bauart },
                { "Kuehlleistung", probe.Kuehlleistung }
            };
        }

        public override VdiUebernahmeErgebnis Anlegen(string bezeichner)
        {
            WPStammCtrl ctrl = new WPStammCtrl();
            NachStamm(ctrl, bezeichner);

            List<(int Vorlauf, int Temperatur, double COP, double Ptherm)> kenn;
            List<(int Vorlauf, int Temperatur, double COP, double Pkuehl, int Last)> kuehl;
            _parser.KennlinienZu(_index, out kenn, out kuehl);

            return ctrl.ImportMitKennlinien(bezeichner, kenn, kuehl);
        }

        public override VdiUebernahmeErgebnis Ueberschreiben(int bestandsId)
        {
            WPStammCtrl ctrl = new WPStammCtrl();
            NachStamm(ctrl, Name);

            List<(int Vorlauf, int Temperatur, double COP, double Ptherm)> kenn;
            List<(int Vorlauf, int Temperatur, double COP, double Pkuehl, int Last)> kuehl;
            _parser.KennlinienZu(_index, out kenn, out kuehl);

            return ctrl.UeberschreibeMitKennlinien(bestandsId, kenn, kuehl)
                ? VdiUebernahmeErgebnis.Ueberschrieben
                : VdiUebernahmeErgebnis.Fehler;
        }
    }
}
