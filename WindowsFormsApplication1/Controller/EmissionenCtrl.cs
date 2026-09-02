using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der EMISSIONS-TAB eines Energieträgers, UI-frei (Etappe E3,
    /// Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md § 4.1): laden der
    /// ausgewählten Arten samt geltendem Wert und Herkunft, CO₂e-Summe nach
    /// F6/F3, Herkunftslogik nach F8, Berechnungsmodus nach F7 und der
    /// Speicherplan der Kontext-Regel.
    ///
    /// <para>Der Reiter in <see cref="ucFuelSettings"/> ist nur Darstellung
    /// darüber — Hausmuster Ä9: die Regel steht an EINER Stelle und ist ohne
    /// Fenster prüfbar.</para>
    ///
    /// <para><b>Kontext-Regel</b> (Umsetzungsklärung zu § 4.1, festgelegt
    /// 29.08.2026):</para>
    /// <list type="bullet">
    ///   <item><description><b>Katalogkontext</b> (<c>projektId ≤ 0</c>): alle
    ///     ausgewählten Arten sind editierbar. Führend schreibt das Speichern die
    ///     aktive <c>emissionswert</c>-Zeile je Art (UPDATE bzw. INSERT, Herkunft
    ///     nach F8) und SPIEGELT die drei Kernarten CO₂/SO₂/NOx zusätzlich nach
    ///     <c>energy_carrier.co2/so2/nox</c>. Seit Etappe E5 lesen die Rechner die
    ///     aktive Zeile zuerst; der Spiegel bleibt, weil die Altspalte die unterste
    ///     Rückfallebene ist (F9) und eine Struktur, die der Altleser nicht sieht,
    ///     eine zweite Wahrheit wäre.</description></item>
    ///   <item><description><b>Projektkontext</b> (<c>projektId &gt; 0</c>): nur
    ///     die drei Kernarten sind editierbar; ihr Schreibweg bleibt der heutige
    ///     (<c>energy_project_settings.co2/so2/nox</c>, NULL = Katalogwert gilt)
    ///     und läuft weiterhin über <see cref="ucFuelSettings"/> — deshalb steht
    ///     er im Plan, wird hier aber NICHT ausgeführt: zwei Schreiber derselben
    ///     Zelle sind eine Fehlerquelle ohne Gegenwert. Weitere Arten erscheinen
    ///     LESEND mit ihrem Katalogwert.</description></item>
    /// </list>
    ///
    /// <para><b>Deferred-Semantik (Ä12/Ä14):</b> Jede Feldänderung lebt bis zum
    /// ausdrücklichen „Speichern" nur im Objekt. Abbrechen und Trägerwechsel
    /// übernehmen nichts — auch keine Übernahme aus dem Katalog-Dialog.</para>
    /// </summary>
    public class EmissionenCtrl
    {
        private readonly int _projektId;
        private readonly int _carrierId;
        private readonly List<EmissionsZeile> _zeilen = new List<EmissionsZeile>();

        private string _modus = DbWerte.EMISSION_MODUS_CO2;
        private string _modusGeladen = DbWerte.EMISSION_MODUS_CO2;

        /// <summary>Kürzel der drei Arten, die es auch als Altspalte gibt (F9).</summary>
        private static readonly string[] KERNARTEN =
        {
            DbWerte.EMISSIONSART_CO2, DbWerte.EMISSIONSART_SO2, DbWerte.EMISSIONSART_NOX
        };

        /// <param name="projektId">0 = Katalogkontext (Stammdaten).</param>
        /// <param name="carrierId">Der Energieträger, dessen Werte gepflegt werden.</param>
        public EmissionenCtrl(int projektId, int carrierId)
        {
            _projektId = projektId > 0 ? projektId : 0;
            _carrierId = carrierId;
        }

        /// <summary>true im Projektkontext (nur Kernarten editierbar).</summary>
        public bool Projektkontext
        {
            get { return _projektId > 0; }
        }

        /// <summary>Der Träger, dessen Werte dieser Stand führt.</summary>
        public int CarrierId
        {
            get { return _carrierId; }
        }

        /// <summary>Das Projekt (0 = Katalogkontext).</summary>
        public int ProjektId
        {
            get { return _projektId; }
        }

        /// <summary>Die Zeilen des Reiters — eine je ausgewählter Art (F5).</summary>
        public IList<EmissionsZeile> Zeilen
        {
            get { return _zeilen; }
        }

        /// <summary>
        /// false, wenn der Artenkatalog nicht lesbar ist (Migrationsschritt 57
        /// fehlt). Der Reiter zeigt dann die drei Bestandsfelder weiter — eine
        /// leere Emissionsmaske wäre schlimmer als die alte.
        /// </summary>
        public bool Verfuegbar { get; private set; }

        /// <summary>Berechnungsmodus (F7), Werte
        /// <see cref="DbWerte.EMISSION_MODUS_CO2"/> bzw.
        /// <c>…_CO2E</c>. Das Setzen wirkt erst mit
        /// <see cref="Speichern"/> (deferred).</summary>
        public string Modus
        {
            get { return _modus; }
            set
            {
                _modus = string.Equals(value, DbWerte.EMISSION_MODUS_CO2E,
                                       StringComparison.OrdinalIgnoreCase)
                         ? DbWerte.EMISSION_MODUS_CO2E : DbWerte.EMISSION_MODUS_CO2;
            }
        }

        /// <summary>true, wenn der Modus gegenüber dem geladenen Stand geändert wurde.</summary>
        public bool ModusGeaendert
        {
            get { return !string.Equals(_modus, _modusGeladen, StringComparison.Ordinal); }
        }

        /// <summary>Beschreibt, WOHIN der Modus geschrieben wird (F7) — das
        /// Projektfeld im Projektkontext, sonst die globale Vorgabe.</summary>
        public string ModusOrt
        {
            get
            {
                return Projektkontext
                    ? SchemaKatalog.TAB_PROJEKT + " (Projekt " + _projektId + ")"
                    : SchemaKatalog.TAB_APPLIKATION + " (globale Vorgabe)";
            }
        }

        // =====================================================================
        // Laden
        // =====================================================================

        /// <summary>
        /// Liest die ausgewählten Arten und je Art den geltenden Wert des Trägers.
        ///
        /// <para><b>Leseweg je Art:</b> aktive <c>emissionswert</c>-Zeile des
        /// Trägers → sonst, bei den drei Kernarten, die Altspalte in
        /// <c>energy_carrier</c> (F9) → sonst leer. Im Projektkontext legt sich
        /// darüber die Projektübersteuerung der Kernarten; weicht sie vom
        /// Katalogwert ab, ist sie ein eigener Wert und die Herkunft sagt das.</para>
        /// </summary>
        public void Laden()
        {
            _zeilen.Clear();

            List<EmissionsartModel> arten = EmissionskatalogCtrl.Arten(true);
            Verfuegbar = arten.Count > 0;

            _modusGeladen = ModusLesen();
            _modus = _modusGeladen;

            if (!Verfuegbar) return;

            Dictionary<int, EmissionswertModel> aktive = EmissionskatalogCtrl.AktiveWerte(_carrierId);
            Dictionary<string, double?> altspalten = AltspaltenTraeger();
            Dictionary<string, double?> projektwerte = Projektkontext
                ? AltspaltenProjekt() : new Dictionary<string, double?>();

            foreach (EmissionsartModel a in arten)
            {
                var z = new EmissionsZeile
                {
                    Art = a,
                    NurLesend = Projektkontext && !IstKernart(a.Kuerzel)
                };

                EmissionswertModel w;
                if (aktive.TryGetValue(a.ID, out w) && w != null)
                {
                    z.WertId = w.ID;
                    z.Wert = w.Wert;
                    z.Quelle = w.Quelle;
                    z.QuelleText = w.Herkunftstext;
                    z.IstCo2e = w.IstCo2e;
                    z.HerkunftId = w.HerkunftId;
                }
                else if (IstKernart(a.Kuerzel))
                {
                    // F9-Rückfallebene: die Altspalte des Trägers. Sie ist auch nach
                    // E5 eine Zahl, mit der die Rechner arbeiten (unterste Ebene der
                    // Lesekette) - sie zu verschweigen hieße, ein leeres Feld über
                    // einen wirksamen Wert zu legen.
                    double? alt;
                    altspalten.TryGetValue(a.Kuerzel, out alt);
                    z.Wert = alt;
                    z.Quelle = DbWerte.EMISSIONSWERT_QUELLE_EIGENER_WERT;
                    z.QuelleText = EmissionskatalogCtrl.TEXT_TRAEGERSPALTE;
                }

                if (Projektkontext && IstKernart(a.Kuerzel))
                {
                    double? pw;
                    if (projektwerte.TryGetValue(a.Kuerzel, out pw) && pw.HasValue)
                    {
                        // Nur ein ABWEICHENDER Projektwert ist eine eigene Aussage;
                        // die bloße Kopie des Katalogwertes (so entsteht sie beim
                        // Speichern seit jeher) darf die belegte Herkunft nicht
                        // überschreiben - sonst verlöre Erdgas E seinen BAFA-Vermerk,
                        // nur weil das Projekt einmal gespeichert wurde.
                        if (!z.Wert.HasValue || Math.Abs(pw.Value - z.Wert.Value) > 1e-9)
                        {
                            z.Wert = pw;
                            z.Quelle = DbWerte.EMISSIONSWERT_QUELLE_EIGENER_WERT;
                            z.QuelleText = EmissionskatalogCtrl.TEXT_PROJEKTWERT;
                            z.IstCo2e = false;
                            z.HerkunftId = null;
                        }
                    }
                }

                if (string.IsNullOrEmpty(z.QuelleText)) z.QuelleText = "—";
                _zeilen.Add(z);
            }
        }

        /// <summary>
        /// Neu laden, den BEARBEITUNGSSTAND aber behalten (nach einem Besuch im
        /// Katalog-Dialog: die Artenauswahl kann sich geändert haben, die
        /// ungespeicherten Feldänderungen dürfen davon nicht verschwinden —
        /// deferred-Semantik, Ä12/Ä14).
        /// </summary>
        public void NeuLadenMitBearbeitungsstand()
        {
            var offen = new Dictionary<string, EmissionsZeile>(StringComparer.OrdinalIgnoreCase);
            foreach (EmissionsZeile z in _zeilen)
                if (z.Geaendert && !offen.ContainsKey(z.Kuerzel)) offen.Add(z.Kuerzel, z);

            string modus = _modus;
            bool modusOffen = ModusGeaendert;

            Laden();

            foreach (EmissionsZeile z in _zeilen)
            {
                EmissionsZeile alt;
                if (!offen.TryGetValue(z.Kuerzel, out alt)) continue;
                z.Wert = alt.Wert;
                z.Quelle = alt.Quelle;
                z.QuelleText = alt.QuelleText;
                z.IstCo2e = alt.IstCo2e;
                z.HerkunftId = alt.HerkunftId;
                z.Geaendert = true;
            }

            if (modusOffen) _modus = modus;
        }

        /// <summary>Die Zeile einer Art am Kürzel; <c>null</c>, wenn nicht ausgewählt.</summary>
        public EmissionsZeile Zeile(string kuerzel)
        {
            return ZeileAus(_zeilen, kuerzel);
        }

        /// <summary>true für CO₂, SO₂ und NOx — die Arten mit Altspalte (F9).</summary>
        public static bool IstKernart(string kuerzel)
        {
            foreach (string k in KERNARTEN)
                if (string.Equals(k, kuerzel, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>Spaltenname der Kernart in <c>energy_carrier</c> bzw.
        /// <c>energy_project_settings</c>; leer bei jeder anderen Art.</summary>
        public static string Altspalte(string kuerzel)
        {
            if (string.Equals(kuerzel, DbWerte.EMISSIONSART_CO2, StringComparison.OrdinalIgnoreCase))
                return "co2";
            if (string.Equals(kuerzel, DbWerte.EMISSIONSART_SO2, StringComparison.OrdinalIgnoreCase))
                return "so2";
            if (string.Equals(kuerzel, DbWerte.EMISSIONSART_NOX, StringComparison.OrdinalIgnoreCase))
                return "nox";
            return "";
        }

        // =====================================================================
        // Bearbeiten
        // =====================================================================

        /// <summary>
        /// HANDEINGABE (F8): Der getippte Text wird geprüft (Komma ODER Punkt,
        /// Hausmuster <see cref="Program.ZahlParsen"/>) und übernommen; leerer
        /// Text heißt „nicht gepflegt". Eine echte Änderung setzt die Herkunft auf
        /// „Eigener Wert" und löscht das Kennzeichen <c>ist_co2e</c> — wer die Zahl
        /// selbst tippt, hat keine Fundstelle dafür, dass sie schon ein Äquivalent
        /// wäre.
        /// </summary>
        /// <returns>false, wenn der Text keine Zahl ist — dann bleibt der alte
        /// Wert stehen und der Aufrufer setzt das Feld zurück.</returns>
        public bool WertEingeben(EmissionsZeile z, string text)
        {
            if (z == null) return false;
            if (z.NurLesend) return false;

            double? neu;
            if (string.IsNullOrWhiteSpace(text)) neu = null;
            else
            {
                double d;
                if (!Program.ZahlParsen(text, out d)) return false;
                if (d < 0) return false;
                neu = d;
            }

            bool gleich = (!neu.HasValue && !z.Wert.HasValue) ||
                          (neu.HasValue && z.Wert.HasValue && Math.Abs(neu.Value - z.Wert.Value) < 1e-9);
            if (gleich) return true;

            z.Wert = neu;
            z.Quelle = DbWerte.EMISSIONSWERT_QUELLE_EIGENER_WERT;
            z.QuelleText = DbWerte.EMISSIONSWERT_TEXT_EIGENER_WERT;
            z.IstCo2e = false;
            z.HerkunftId = null;
            z.Geaendert = true;
            return true;
        }

        /// <summary>
        /// ÜBERNAHME aus dem Katalog (F8): Der Zahlenwert wird KOPIERT, die
        /// Herkunft am Trägerwert vermerkt. Geschrieben wird erst mit
        /// <see cref="Speichern"/> — so verwirft „Abbrechen" auch die Übernahme
        /// (Ä12/Ä14).
        /// </summary>
        public bool KatalogwertUebernehmen(EmissionsZeile z, EmissionswertModel vorlage)
        {
            if (z == null || z.NurLesend) return false;
            if (vorlage == null || !vorlage.Wert.HasValue) return false;

            z.Wert = vorlage.Wert;
            z.Quelle = string.IsNullOrEmpty(vorlage.Quelle)
                ? DbWerte.EMISSIONSWERT_QUELLE_EIGENER_WERT : vorlage.Quelle;
            z.QuelleText = vorlage.Herkunftstext;
            z.IstCo2e = vorlage.IstCo2e;
            z.HerkunftId = vorlage.ID > 0 ? (int?)vorlage.ID : null;
            z.Geaendert = true;
            return true;
        }

        // =====================================================================
        // CO2-Aequivalent-Summe (F6, Sonderfall F3)
        // =====================================================================

        /// <summary>
        /// Die CO₂e-Summe des Trägers in g/kWh (F6):
        /// <c>Σ wert_normiert(g/kWh) × äquivalenzfaktor</c> über die ausgewählten
        /// Arten. <b>Sonderfall F3:</b> Trägt das CO₂-Feld einen Wert mit
        /// <c>ist_co2e</c>, IST die Summe genau dieser Wert — CH₄ und N₂O stecken
        /// bereits darin, jede Addition wäre doppelt gezählt.
        /// </summary>
        public double SummeCo2eGKwh()
        {
            return SummeCo2eGKwh(_zeilen);
        }

        /// <summary>
        /// Dieselbe Summe über einen BELIEBIGEN Zeilensatz — die Fassung, die seit
        /// Etappe E5 auch die Rechner nutzen (<see cref="EmissionsFaktorLader"/>).
        /// Der Reiter und der Rechenlauf bilden die Summe damit nachweislich gleich;
        /// eine zweite Fassung derselben Regel gibt es nicht.
        /// </summary>
        public static double SummeCo2eGKwh(IEnumerable<EmissionsZeile> zeilen)
        {
            EmissionsZeile co2 = ZeileAus(zeilen, DbWerte.EMISSIONSART_CO2);
            if (co2 != null && co2.Wert.HasValue && co2.IstCo2e)
                return co2.Art != null ? co2.Art.NormiertGKwh(co2.Wert.Value) : co2.Wert.Value;

            double summe = 0.0;
            if (zeilen != null)
                foreach (EmissionsZeile z in zeilen) summe += z.BeitragGKwh;
            return summe;
        }

        /// <summary>true, wenn die Summe nach F3 zustande kommt — dann steht der
        /// Hinweis „CO₂-Wert ist bereits Äquivalent" daneben.</summary>
        public bool SummeIstBereitsAequivalent()
        {
            return SummeIstBereitsAequivalent(_zeilen);
        }

        /// <inheritdoc cref="SummeIstBereitsAequivalent()"/>
        public static bool SummeIstBereitsAequivalent(IEnumerable<EmissionsZeile> zeilen)
        {
            EmissionsZeile co2 = ZeileAus(zeilen, DbWerte.EMISSIONSART_CO2);
            return co2 != null && co2.Wert.HasValue && co2.IstCo2e;
        }

        private static EmissionsZeile ZeileAus(IEnumerable<EmissionsZeile> zeilen, string kuerzel)
        {
            if (zeilen == null) return null;
            foreach (EmissionsZeile z in zeilen)
                if (string.Equals(z.Kuerzel, kuerzel, StringComparison.OrdinalIgnoreCase))
                    return z;
            return null;
        }

        // =====================================================================
        // Berechnungsmodus (F7)
        // =====================================================================

        /// <summary>Die GLOBALE VORGABE aus <c>Tab_Applikation</c>; fehlt die
        /// Spalte oder ist sie leer, gilt <c>CO2</c> — das heutige Verhalten.</summary>
        public static string VorgabeLesen()
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT [" + SchemaKatalog.SPALTE_EMISSION_BERECHNUNGSMODUS + "] " +
                    "FROM [" + SchemaKatalog.TAB_APPLIKATION + "] LIMIT 1");
                return Normiert(o);
            }
            catch { return DbWerte.EMISSION_MODUS_CO2; }
        }

        /// <summary>Schreibt die globale Vorgabe (gilt für NEUE Projekte, F7).</summary>
        public static bool VorgabeSchreiben(string modus)
        {
            try
            {
                return DataRepository.ExecuteSQL(
                    "UPDATE [" + SchemaKatalog.TAB_APPLIKATION + "] SET [" +
                    SchemaKatalog.SPALTE_EMISSION_BERECHNUNGSMODUS + "] = ?",
                    EmissionskatalogCtrl.Text(Normiert(modus)));
            }
            catch { return false; }
        }

        /// <summary>Der Modus, in dem DIESES Projekt rechnet (F7).</summary>
        public static string ProjektModusLesen(int projektId)
        {
            if (projektId <= 0) return VorgabeLesen();
            return Normiert(ProjektModusRoh(projektId));
        }

        /// <summary>
        /// DER MODUS EINES RECHENLAUFS (F7, Etappe E5) — die Auflösung, an die sich
        /// beide Rechner halten:
        ///
        /// <list type="number">
        ///   <item><description>das Projektfeld <c>Tab_Projekt.Emission_Berechnungsmodus</c>,</description></item>
        ///   <item><description>ist es leer oder fehlt es, die globale Vorgabe in
        ///     <c>Tab_Applikation</c>,</description></item>
        ///   <item><description>ist auch die leer, <c>CO2</c> — das heutige Verhalten.</description></item>
        /// </list>
        ///
        /// <para>Der Unterschied zu <see cref="ProjektModusLesen"/> ist die MITTLERE
        /// Stufe: Der Dialog zeigt, was am Projekt STEHT (leer heißt dort „noch nicht
        /// entschieden" und wird als CO₂ angezeigt); der Rechenlauf braucht dagegen
        /// den Wert, der tatsächlich GILT. Ein vor Migrationsschritt 57 angelegtes
        /// Projekt ohne Eintrag rechnet damit im Modus der Vorgabe und nicht
        /// stillschweigend anders als seine Nachbarn.</para>
        /// </summary>
        public static string ModusFuerRechenlauf(int projektId)
        {
            string roh = projektId > 0 ? ProjektModusRoh(projektId) : "";
            if (!string.IsNullOrWhiteSpace(roh)) return Normiert(roh);
            return VorgabeLesen();
        }

        /// <summary>Der ROHTEXT des Projektfeldes; leer, wenn NULL, leer oder die
        /// Spalte fehlt. ACE-Falle: die Ganzzahl steht als Literal im SQL.</summary>
        private static string ProjektModusRoh(int projektId)
        {
            if (projektId <= 0) return "";
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT [" + SchemaKatalog.SPALTE_EMISSION_BERECHNUNGSMODUS + "] FROM [" +
                    SchemaKatalog.TAB_PROJEKT + "] WHERE ID = " +
                    EmissionskatalogCtrl.Ganz(projektId));
                return o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
            }
            catch { return ""; }
        }

        /// <summary>Schreibt den Modus des Projekts (F7).</summary>
        public static bool ProjektModusSchreiben(int projektId, string modus)
        {
            if (projektId <= 0) return false;
            try
            {
                return DataRepository.ExecuteSQL(
                    "UPDATE [" + SchemaKatalog.TAB_PROJEKT + "] SET [" +
                    SchemaKatalog.SPALTE_EMISSION_BERECHNUNGSMODUS + "] = ? WHERE ID = " +
                    EmissionskatalogCtrl.Ganz(projektId),
                    EmissionskatalogCtrl.Text(Normiert(modus)));
            }
            catch { return false; }
        }

        private string ModusLesen()
        {
            return Projektkontext ? ProjektModusLesen(_projektId) : VorgabeLesen();
        }

        private static string Normiert(object o)
        {
            string s = o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
            return string.Equals(s, DbWerte.EMISSION_MODUS_CO2E, StringComparison.OrdinalIgnoreCase)
                   ? DbWerte.EMISSION_MODUS_CO2E : DbWerte.EMISSION_MODUS_CO2;
        }

        // =====================================================================
        // Speichern
        // =====================================================================

        /// <summary>
        /// Der Speicherplan der Kontext-Regel — GEBILDET, nicht ausgeführt. Er ist
        /// die prüfbare Fassung dessen, was <see cref="Speichern"/> tut, und
        /// zugleich die Protokollzeile für den Prüfstand.
        /// </summary>
        public List<EmissionsSpeicherschritt> Speicherplan()
        {
            var plan = new List<EmissionsSpeicherschritt>();

            foreach (EmissionsZeile z in _zeilen)
            {
                if (!z.Geaendert || z.NurLesend) continue;

                string wertText = z.Wert.HasValue
                    ? z.Wert.Value.ToString("0.####", CultureInfo.InvariantCulture)
                    : "(leer)";

                if (Projektkontext)
                {
                    if (!IstKernart(z.Kuerzel)) continue;
                    plan.Add(new EmissionsSpeicherschritt
                    {
                        Art = "PROJEKT",
                        Kuerzel = z.Kuerzel,
                        Wert = z.Wert,
                        Quelle = z.Quelle,
                        QuelleText = z.QuelleText,
                        IstCo2e = z.IstCo2e,
                        WertId = 0,
                        Klartext = "PROJEKT energy_project_settings." + Altspalte(z.Kuerzel) +
                                   " = " + wertText + " (Projekt " + _projektId +
                                   ", Träger " + _carrierId + ") - geschrieben vom Bestandsweg"
                    });
                    continue;
                }

                plan.Add(new EmissionsSpeicherschritt
                {
                    Art = z.WertId > 0 ? "UPDATE" : "INSERT",
                    Kuerzel = z.Kuerzel,
                    Wert = z.Wert,
                    Quelle = z.Quelle,
                    QuelleText = z.QuelleText,
                    IstCo2e = z.IstCo2e,
                    HerkunftId = z.HerkunftId,
                    WertId = z.WertId,
                    Klartext = (z.WertId > 0 ? "UPDATE" : "INSERT") + " emissionswert " +
                               z.Kuerzel + " = " + wertText + " " + z.Art.Einheit +
                               ", Quelle " + z.Quelle + " (" + z.QuelleText + ")" +
                               ", ist_co2e=" + (z.IstCo2e ? "wahr" : "falsch") +
                               (z.HerkunftId.HasValue ? ", herkunft_id=" + z.HerkunftId.Value : "") +
                               (z.WertId > 0 ? ", id=" + z.WertId : "")
                });

                string spalte = Altspalte(z.Kuerzel);
                if (spalte.Length > 0)
                {
                    plan.Add(new EmissionsSpeicherschritt
                    {
                        Art = "SPIEGEL",
                        Kuerzel = z.Kuerzel,
                        Wert = z.Wert,
                        WertId = 0,
                        Klartext = "SPIEGEL energy_carrier." + spalte + " = " + wertText +
                                   " (Träger " + _carrierId + ") - unterste Rückfallebene (F9)"
                    });
                }
            }

            if (ModusGeaendert)
            {
                plan.Add(new EmissionsSpeicherschritt
                {
                    Art = "MODUS",
                    Wert = null,
                    Klartext = "MODUS " + ModusOrt + " = " + _modus + " (F7)"
                });
            }

            return plan;
        }

        /// <summary>
        /// Führt den Speicherplan aus. Die <c>PROJEKT</c>-Schritte bleiben dabei
        /// bewusst liegen — sie schreibt der Bestandsweg des Dialogs
        /// (<c>ucFuelSettings.SpeichereWerte</c>), und zwei Schreiber derselben
        /// Zelle wären eine Fehlerquelle ohne Gegenwert.
        /// </summary>
        /// <returns>false, sobald ein Schritt scheitert; die vorherigen bleiben
        /// geschrieben (dieselbe Zusage wie im übrigen Dialog — es gibt hier
        /// keine Transaktionsklammer).</returns>
        public bool Speichern()
        {
            bool alleOk = true;

            foreach (EmissionsSpeicherschritt s in Speicherplan())
            {
                switch (s.Art)
                {
                    case "INSERT":
                        {
                            EmissionsZeile z = Zeile(s.Kuerzel);
                            if (z == null || z.Art == null) break;
                            int id = DataRepository.ExecuteInsertAndGetId(
                                "INSERT INTO " + SchemaKatalog.TAB_EMISSIONSWERT +
                                " (emissionsart_id, carrier_id, quelle, quelle_text, wert, " +
                                "  ist_co2e, ist_aktiv, herkunft_id, ist_auslieferung, gueltig_ab) " +
                                "VALUES (" + EmissionskatalogCtrl.Ganz(z.Art.ID) + ", " +
                                EmissionskatalogCtrl.Ganz(_carrierId) + ", ?, ?, ?, ?, TRUE, " +
                                (z.HerkunftId.HasValue
                                    ? EmissionskatalogCtrl.Ganz(z.HerkunftId.Value) : "NULL") +
                                ", FALSE, ?)",
                                new[]
                                {
                                    EmissionskatalogCtrl.Text(z.Quelle),
                                    EmissionskatalogCtrl.Text(z.QuelleText),
                                    EmissionskatalogCtrl.Komma(z.Wert ?? 0.0),
                                    EmissionskatalogCtrl.JaNein(z.IstCo2e),
                                    EmissionskatalogCtrl.Datum(DateTime.Today)
                                });
                            if (id > 0) z.WertId = id; else alleOk = false;
                            break;
                        }

                    case "UPDATE":
                        {
                            EmissionsZeile z = Zeile(s.Kuerzel);
                            if (z == null || z.WertId <= 0) break;
                            bool ok = DataRepository.ExecuteSQL(
                                "UPDATE " + SchemaKatalog.TAB_EMISSIONSWERT +
                                " SET quelle = ?, quelle_text = ?, wert = ?, ist_co2e = ?, " +
                                "     herkunft_id = " +
                                (z.HerkunftId.HasValue
                                    ? EmissionskatalogCtrl.Ganz(z.HerkunftId.Value) : "NULL") +
                                ", ist_aktiv = TRUE, gueltig_ab = ? WHERE id = " +
                                EmissionskatalogCtrl.Ganz(z.WertId),
                                EmissionskatalogCtrl.Text(z.Quelle),
                                EmissionskatalogCtrl.Text(z.QuelleText),
                                EmissionskatalogCtrl.Komma(z.Wert ?? 0.0),
                                EmissionskatalogCtrl.JaNein(z.IstCo2e),
                                EmissionskatalogCtrl.Datum(DateTime.Today));
                            if (!ok) alleOk = false;
                            break;
                        }

                    case "SPIEGEL":
                        {
                            string spalte = Altspalte(s.Kuerzel);
                            if (spalte.Length == 0) break;
                            bool ok = DataRepository.ExecuteSQL(
                                "UPDATE energy_carrier SET " + spalte + " = ? WHERE id = " +
                                EmissionskatalogCtrl.Ganz(_carrierId),
                                EmissionskatalogCtrl.Komma(s.Wert ?? 0.0));
                            if (!ok) alleOk = false;
                            break;
                        }

                    case "MODUS":
                        {
                            bool ok = Projektkontext
                                ? ProjektModusSchreiben(_projektId, _modus)
                                : VorgabeSchreiben(_modus);
                            if (ok) _modusGeladen = _modus; else alleOk = false;
                            break;
                        }
                }
            }

            // Der Bearbeitungsstand ist verbucht - auch die PROJEKT-Zeilen, deren
            // Zahl der Bestandsweg im selben Speichervorgang schreibt.
            foreach (EmissionsZeile z in _zeilen) z.Geaendert = false;
            return alleOk;
        }

        // =====================================================================
        // Altspalten (F9)
        // =====================================================================

        private Dictionary<string, double?> AltspaltenTraeger()
        {
            var d = new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase);
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT co2, so2, nox FROM energy_carrier WHERE id = " +
                    EmissionskatalogCtrl.Ganz(_carrierId));
                if (dt == null || dt.Rows.Count == 0) return d;
                DataRow r = dt.Rows[0];
                d[DbWerte.EMISSIONSART_CO2] = EmissionskatalogCtrl.KommaOderNull(r["co2"]);
                d[DbWerte.EMISSIONSART_SO2] = EmissionskatalogCtrl.KommaOderNull(r["so2"]);
                d[DbWerte.EMISSIONSART_NOX] = EmissionskatalogCtrl.KommaOderNull(r["nox"]);
            }
            catch { }
            return d;
        }

        private Dictionary<string, double?> AltspaltenProjekt()
        {
            var d = new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase);
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT co2, so2, nox FROM energy_project_settings " +
                    "WHERE ID_Projekt = " + EmissionskatalogCtrl.Ganz(_projektId) +
                    " AND [ID_Energieträger] = " + EmissionskatalogCtrl.Ganz(_carrierId));
                if (dt == null || dt.Rows.Count == 0) return d;
                DataRow r = dt.Rows[0];
                d[DbWerte.EMISSIONSART_CO2] = EmissionskatalogCtrl.KommaOderNull(r["co2"]);
                d[DbWerte.EMISSIONSART_SO2] = EmissionskatalogCtrl.KommaOderNull(r["so2"]);
                d[DbWerte.EMISSIONSART_NOX] = EmissionskatalogCtrl.KommaOderNull(r["nox"]);
            }
            catch { }
            return d;
        }
    }
}
