using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE der Energieträger-Preispflege (iU9-W4.4) — alle SQL-Texte,
    /// die bis Welle 4 in der Maske <c>Views/Kosten/ucFuelSettings.cs</c>
    /// standen (2 103 Zeilen, Etappen K3/AP4/B2/E3/KD4).
    ///
    /// <para><b>Warum sie hierher gehören.</b> Ein Dialog in <c>EPOS.UI</c> kennt
    /// keine Datenbank (Hausregel <c>EPOS.UI/CLAUDE.md</c>, Regel F5 des
    /// Wellenplans iU9). Die Anweisungen sind <b>wortgleich</b> übernommen —
    /// dieselben Spalten, dieselbe Rundung auf vier Nachkommastellen, dieselbe
    /// Reihenfolge (erst Historie, dann Projekt-Settings), damit der
    /// Referenzlauf sie nicht bemerkt.</para>
    ///
    /// <para><b>Drei Schreibwege, ein Aufrufer.</b> <see cref="Katalogwerte"/>
    /// schreibt im Katalogkontext (Projekt 0) die Zeile <c>energy_carrier</c>
    /// selbst (Ä9); <see cref="Historie"/> legt bei einer Wertänderung einen
    /// Stand in <c>energy_price</c> an bzw. aktualisiert ihn zum gewählten Datum;
    /// <see cref="Projektwerte"/> pflegt die Übersteuerung in
    /// <c>energy_project_settings</c>. Welcher Weg gilt, entscheidet die Hülle
    /// genau wie vorher <c>SpeichereWerte</c>.</para>
    /// </summary>
    public static class EnergietraegerPreisCtrl
    {
        // =====================================================================
        // Lesen
        // =====================================================================

        /// <summary>
        /// Die AKTIVEN Umrechnungen eines Brennstoffs — aus
        /// <c>ucFuelSettings.GetConversions</c>, seit ET-D mit dem Filter
        /// <c>aktiv = 1</c>: Eine abgeschaltete Regel ist keine Regel, und wer
        /// sie las, bekam eine Einheitenkette angeboten, die der Prüfer
        /// <c>EnergieEinheitenPruefung</c> längst verworfen hat. Der Regelblock
        /// der Karte liest weiter ALLE Regeln (er zeigt ja den Schalter) —
        /// <c>EnergieEinheitenPruefung.RegelnDesBrennstoffs</c>.
        /// </summary>
        public static List<EnergyConversion> Umrechnungen(int idBrennstoff)
        {
            var liste = new List<EnergyConversion>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT id_brennstoff, from_unit, to_unit, factor FROM ENERGY_CONVERSION " +
                "WHERE id_brennstoff = ? AND aktiv = 1",
                new DbParam("@id", idBrennstoff));

            foreach (DataRow row in dt.Rows)
            {
                liste.Add(new EnergyConversion
                {
                    IDBrennstoff = Convert.ToInt32(row["Id_brennstoff"]),
                    FromUnit = row["from_unit"].ToString(),
                    ToUnitCode = row["to_unit"].ToString(),
                    Factor = Convert.ToDouble(row["factor"])
                });
            }
            return liste;
        }

        /// <summary>
        /// Die Projektübersteuerung eines Trägers; <c>null</c> = keine Zeile.
        /// Wortgleich aus <c>ucFuelSettings.GetProjectPrice</c> — nur ohne
        /// <c>dynamic</c>: Der Rückgabetyp ist jetzt benannt.
        /// </summary>
        public static Projektpreis ProjektpreisLesen(int projektId, int traegerId)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM ENERGY_PROJECT_SETTINGS WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@p", projektId),
                new DbParam("@c", traegerId));

            if (dt == null || dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new Projektpreis
            {
                Arbeitspreis = Zahl(row, "custom_price_work"),
                Grundpreis = Zahl(row, "custom_price_base"),
                Leistungspreis = Zahl(row, "custom_price_power"),
                Hi = Zahl(row, "custom_hi"),
                Hs = Zahl(row, "custom_hs"),
                CO2 = Zahl(row, "co2"),
                SO2 = Zahl(row, "so2"),
                NOx = Zahl(row, "nox"),
                IdUmrechnung = row["ID_Umrechnung"] != DBNull.Value
                    ? (int?)Convert.ToInt32(row["ID_Umrechnung"]) : null,
                Staffel = StaffelAus(row),
                // ETAPPE E7c (Schritt F, Schemaschritt 112): der Kartenzustand „Preisbasis"
                // aus seiner eigenen Spalte — NICHT mehr aus ID_Umrechnung abgeleitet.
                // Fehlt die Spalte (Datenbank vor 112), wird das benannt, nicht still
                // auf die Abrechnungseinheit zurückgefallen.
                PreisbasisSpalteFehlt = !row.Table.Columns.Contains(SchemaKatalog.SPALTE_EPS_PREISBASIS),
                Preisbasis = row.Table.Columns.Contains(SchemaKatalog.SPALTE_EPS_PREISBASIS) &&
                             row[SchemaKatalog.SPALTE_EPS_PREISBASIS] != DBNull.Value
                    ? Convert.ToString(row[SchemaKatalog.SPALTE_EPS_PREISBASIS]) : null,
                // ETAPPE E9a (Schritt C, Schemaschritt 117): die Preise je Szenario — tolerant
                // gelesen; eine Datenbank vor 117 liefert einen leeren Satz („wie Erwartet").
                Szenario = SzenarioAus(row)
            };
        }

        // =====================================================================
        // Die Trägerpreise je Szenario (Etappe E9a, Schritt C, Schemaschritt 117)
        // =====================================================================

        /// <summary>
        /// ETAPPE E9a: Führt <c>energy_project_settings</c> die sechs Spalten der
        /// Trägerpreise je Szenario (Schemaschritt 117)? Ohne sie gibt es nichts zu schreiben,
        /// und jeder Leser rechnet „wie Erwartet".
        /// </summary>
        public static bool SzenarioSpaltenVorhanden()
        {
            try
            {
                foreach (SchemaSpalte s in SchemaKatalog.Schritt117_TraegerpreisSzenario)
                    if (!DataRepository.SpalteVorhanden(s.Tabelle, s.Name)) return false;
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// ETAPPE E9a: die Trägerpreise je Szenario eines Trägers im Projekt. Leer (nie
        /// <c>null</c>), wenn die Zeile oder die Spalten fehlen — dann rechnet der Träger in
        /// allen Szenarien mit seinem Erwartet-Preis. Eigene Abfrage wie bei
        /// <see cref="StaffelLesen"/>, damit eine Datenbank ohne die Spalten die übrigen Preise
        /// nicht verliert.
        /// </summary>
        public static TraegerpreisSzenario SzenarioLesen(int projektId, int traegerId)
        {
            if (projektId <= 0 || traegerId <= 0) return new TraegerpreisSzenario();
            try
            {
                if (!SzenarioSpaltenVorhanden()) return new TraegerpreisSzenario();
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT [" + SchemaKatalog.SPALTE_EPS_PREIS_ARBEIT_BEST + "], [" +
                    SchemaKatalog.SPALTE_EPS_PREIS_ARBEIT_WORST + "], [" +
                    SchemaKatalog.SPALTE_EPS_PREIS_GRUND_BEST + "], [" +
                    SchemaKatalog.SPALTE_EPS_PREIS_GRUND_WORST + "], [" +
                    SchemaKatalog.SPALTE_EPS_PREIS_LEISTUNG_BEST + "], [" +
                    SchemaKatalog.SPALTE_EPS_PREIS_LEISTUNG_WORST +
                    "] FROM energy_project_settings WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                    new DbParam("@p", projektId),
                    new DbParam("@c", traegerId));
                if (dt != null && dt.Rows.Count > 0) return SzenarioAus(dt.Rows[0]);
            }
            catch { }
            return new TraegerpreisSzenario();
        }

        /// <summary>
        /// ETAPPE E9a: schreibt die Trägerpreise je Szenario an die Projektübersteuerung des
        /// Trägers — der Schreibweg der Trägerkarte, getrennt von <see cref="Projektwerte"/>
        /// (Muster <see cref="StaffelSchreiben"/>): Ein Speichern der Karte ohne die sechs
        /// Felder darf gepflegte Szenariopreise nicht leeren. Ein leeres Feld (oder 0) schreibt
        /// NULL („wie Erwartet"). Die Zeile muss stehen.
        /// </summary>
        /// <returns>true, wenn genau die Zeile des Trägers getroffen wurde; false ohne die
        /// Spalten (Datenbank vor Schemaschritt 117).</returns>
        public static bool SzenarioSchreiben(int projektId, int traegerId, TraegerpreisSzenario szenario)
        {
            if (!SzenarioSpaltenVorhanden()) return false;
            if (szenario == null) szenario = new TraegerpreisSzenario();
            int zeilen = DataRepository.ExecuteNonQuery(
                "UPDATE energy_project_settings SET [" + SchemaKatalog.SPALTE_EPS_PREIS_ARBEIT_BEST + "] = ?, [" +
                SchemaKatalog.SPALTE_EPS_PREIS_ARBEIT_WORST + "] = ?, [" +
                SchemaKatalog.SPALTE_EPS_PREIS_GRUND_BEST + "] = ?, [" +
                SchemaKatalog.SPALTE_EPS_PREIS_GRUND_WORST + "] = ?, [" +
                SchemaKatalog.SPALTE_EPS_PREIS_LEISTUNG_BEST + "] = ?, [" +
                SchemaKatalog.SPALTE_EPS_PREIS_LEISTUNG_WORST +
                "] = ? WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam[]
                {
                    Szenariowert("@ab", szenario.ArbeitspreisBest),
                    Szenariowert("@aw", szenario.ArbeitspreisWorst),
                    Szenariowert("@gb", szenario.GrundpreisBest),
                    Szenariowert("@gw", szenario.GrundpreisWorst),
                    Szenariowert("@lb", szenario.LeistungspreisBest),
                    Szenariowert("@lw", szenario.LeistungspreisWorst),
                    new DbParam("@pid", projektId),
                    new DbParam("@eid", traegerId)
                });
            return zeilen == 1;
        }

        /// <summary>Die sechs Szenariopreise einer Zeile, tolerant (fehlende Spalte = leer);
        /// eine 0 wird leer — „NULL/0 heißt wie Erwartet".</summary>
        private static TraegerpreisSzenario SzenarioAus(DataRow row)
        {
            return new TraegerpreisSzenario
            {
                ArbeitspreisBest = OhneNull(Zahl(row, SchemaKatalog.SPALTE_EPS_PREIS_ARBEIT_BEST)),
                ArbeitspreisWorst = OhneNull(Zahl(row, SchemaKatalog.SPALTE_EPS_PREIS_ARBEIT_WORST)),
                GrundpreisBest = OhneNull(Zahl(row, SchemaKatalog.SPALTE_EPS_PREIS_GRUND_BEST)),
                GrundpreisWorst = OhneNull(Zahl(row, SchemaKatalog.SPALTE_EPS_PREIS_GRUND_WORST)),
                LeistungspreisBest = OhneNull(Zahl(row, SchemaKatalog.SPALTE_EPS_PREIS_LEISTUNG_BEST)),
                LeistungspreisWorst = OhneNull(Zahl(row, SchemaKatalog.SPALTE_EPS_PREIS_LEISTUNG_WORST))
            };
        }

        private static double? OhneNull(double? wert)
        {
            return wert.HasValue && wert.Value != 0 ? wert : null;
        }

        /// <summary>Ein Szenariopreis als Parameter — leer oder 0 geht als NULL in die
        /// Datenbank (Nullregel der Szenariospalten).</summary>
        private static DbParam Szenariowert(string name, double? wert)
        {
            return new DbParam(name, DbParamTyp.Double)
            { Wert = wert.HasValue && wert.Value != 0 ? (object)wert.Value : DBNull.Value };
        }

        /// <summary>
        /// ETAPPE E7c (Schritt F): Führt <c>energy_project_settings</c> die Spalte
        /// <c>Preisbasis</c> (Schemaschritt 112)? Ohne sie schreibt
        /// <see cref="Projektwerte"/> die übrigen Felder wie bisher.
        /// </summary>
        public static bool PreisbasisSpalteVorhanden()
        {
            try
            {
                return DataRepository.SpalteVorhanden(SchemaKatalog.ENERGY_PROJECT_SETTINGS,
                                                      SchemaKatalog.SPALTE_EPS_PREISBASIS);
            }
            catch { return false; }
        }

        // =====================================================================
        // Die Leistungspreis-Staffel des Stromträgers (Q11, Schemaschritt 104)
        // =====================================================================

        /// <summary>
        /// Die zweistufige Leistungspreis-Staffel eines Trägers im Projekt
        /// (<c>energy_project_settings</c>, Schritt 104). Leer (nie <c>null</c>), wenn die
        /// Zeile oder die Spalten fehlen — dann rechnet keine Staffel.
        /// </summary>
        public static LeistungspreisStaffel StaffelLesen(int projektId, int traegerId)
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT [" + SchemaKatalog.SPALTE_LP_STAFFEL_GRENZE + "], [" +
                    SchemaKatalog.SPALTE_LP_STAFFEL_PREIS1 + "], [" + SchemaKatalog.SPALTE_LP_STAFFEL_PREIS2 +
                    "] FROM energy_project_settings WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                    new DbParam("@p", projektId),
                    new DbParam("@c", traegerId));
                if (dt != null && dt.Rows.Count > 0) return StaffelAus(dt.Rows[0]);
            }
            catch { }
            return new LeistungspreisStaffel();
        }

        /// <summary>
        /// Schreibt die Staffel an die Projektübersteuerung des Trägers — der Schreibweg
        /// der Trägerkarte (Kostenverwaltung). Die Zeile muss stehen: Die Karte schreibt
        /// vorher <see cref="Projektwerte"/>, das sie bei Bedarf anlegt. Ein leeres Feld
        /// schreibt NULL („nicht gepflegt").
        /// </summary>
        /// <returns>true, wenn genau die Zeile des Trägers getroffen wurde.</returns>
        public static bool StaffelSchreiben(int projektId, int traegerId, LeistungspreisStaffel staffel)
        {
            if (staffel == null) staffel = new LeistungspreisStaffel();
            int zeilen = DataRepository.ExecuteNonQuery(
                "UPDATE energy_project_settings SET [" + SchemaKatalog.SPALTE_LP_STAFFEL_GRENZE + "] = ?, [" +
                SchemaKatalog.SPALTE_LP_STAFFEL_PREIS1 + "] = ?, [" + SchemaKatalog.SPALTE_LP_STAFFEL_PREIS2 +
                "] = ? WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam[]
                {
                    Nullbar("@g", staffel.GrenzeKW),
                    Nullbar("@p1", staffel.Preis1EurKWa),
                    Nullbar("@p2", staffel.Preis2EurKWa),
                    new DbParam("@pid", projektId),
                    new DbParam("@eid", traegerId)
                });
            return zeilen == 1;
        }

        private static LeistungspreisStaffel StaffelAus(DataRow row)
        {
            return new LeistungspreisStaffel
            {
                GrenzeKW = Zahl(row, SchemaKatalog.SPALTE_LP_STAFFEL_GRENZE),
                Preis1EurKWa = Zahl(row, SchemaKatalog.SPALTE_LP_STAFFEL_PREIS1),
                Preis2EurKWa = Zahl(row, SchemaKatalog.SPALTE_LP_STAFFEL_PREIS2)
            };
        }

        private static DbParam Nullbar(string name, double? wert)
        {
            return new DbParam(name, DbParamTyp.Double)
            { Wert = wert.HasValue ? (object)Math.Round(wert.Value, 4) : DBNull.Value };
        }

        /// <summary>
        /// Die Zieleinheit einer Umrechnungszeile; <c>null</c> = keine Zeile.
        ///
        /// <para>Wortgleich aus <c>GetTargetUnitByConversionId</c>, aber mit
        /// Parameter statt Zeichenkettenverkettung (Befund 26.08.2026: eine
        /// leere oder verwaiste Id liefert keine Zeile — das ist kein Fehler,
        /// sondern heißt „keine Zieleinheit").</para>
        /// </summary>
        public static string Zieleinheit(int idUmrechnung)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT to_unit FROM energy_conversion WHERE ID = ?",
                new DbParam("@id", idUmrechnung));
            return (o == null || o == DBNull.Value) ? null : Convert.ToString(o);
        }

        /// <summary>
        /// Die Id einer Umrechnung (Brennstoff, von, nach); −1 = keine.
        /// Wortgleich aus <c>GetConvID</c>.
        /// </summary>
        public static int UmrechnungsId(EnergyConversion conv)
        {
            if (conv == null) return -1;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID FROM ENERGY_CONVERSION WHERE id_brennstoff = ? AND from_unit = ? " +
                "AND to_unit = ?",
                new DbParam("@cid", conv.IDBrennstoff),
                new DbParam("@fu", conv.FromUnit),
                new DbParam("@tu", conv.ToUnitCode));

            return (dt != null && dt.Rows.Count > 0) ? Convert.ToInt32(dt.Rows[0]["ID"]) : -1;
        }

        // =====================================================================
        // Die waehlbaren Preisbasen (W4-B-1)
        // =====================================================================

        /// <summary>
        /// Eine waehlbare Preisbasis der Klappliste „Preisbasis" — die Einheit,
        /// in der Arbeitspreis, Heiz- und Brennwert angezeigt werden.
        ///
        /// <para><see cref="Umrechnung"/> ist die Regel, die den Wert aus der
        /// Abrechnungseinheit in diese Basis traegt; <c>null</c> heisst „keine
        /// Regel" — dann ist die Basis die Abrechnungseinheit selbst und
        /// <see cref="Faktor"/> ist 1. Beim Speichern wird daraus wie bisher
        /// <c>ID_Umrechnung = -1</c> (<see cref="UmrechnungsId"/>).</para>
        /// </summary>
        public sealed class Preisbasis
        {
            /// <summary>Der Anzeigetext, in der Schreibweise der Quelle.</summary>
            public string Einheit;

            /// <summary>Abrechnungseinheit → diese Basis.</summary>
            public double Faktor;

            /// <summary>Die tragende Regel; <c>null</c> = die Abrechnungseinheit selbst.</summary>
            public EnergyConversion Umrechnung;
        }

        /// <summary>
        /// Die Klappliste „Preisbasis" eines Traegers — GENAU ZWEI EINTRAEGE:
        /// die Abrechnungseinheit (Faktor 1) und die Kilowattstunde
        /// (Faktor = HEIZWERT des Traegers). Rechnet der Traeger ohnehin nach kWh
        /// ab oder fuehrt er keinen Heizwert, bleibt der eine Eintrag.
        ///
        /// <para><b>Befund UR-1 (Anwenderfoto 18.09.2026).</b> Bis ET-D fuellte
        /// die Liste sich aus den <c>to_unit</c> der Umrechnungsregeln, und
        /// <b>deren <c>factor</c> rechnete den Arbeitspreis um</b>. Der Faktor
        /// einer Regel ist aber kein Heizwert: In der Testdatenbank traegt die
        /// Regel <c>Nm³ → kWh</c> des Erdgases E den Faktor <c>0,5</c>, waehrend
        /// Hi bei <c>10,5 kWh/Nm³</c> steht. Wer 0,07 €/kWh eingab, bekam
        /// 0,035 €/Nm³ gespeichert und las in der Formelzeile 0,0033 €/kWh —
        /// drei Zahlen, drei Wahrheiten. <b>Eine Energiemenge wird ueber ihren
        /// Heizwert in kWh getragen, nie ueber eine Einheitenregel.</b></para>
        ///
        /// <para><b>Die Regeln bleiben — als PRUEFUNG der Einheitenkette</b>
        /// (<c>EnergieEinheitenPruefung</c>, Karte: Block „Einheiten und
        /// Umrechnung"). Sie stellen hier nur noch die <c>ID_Umrechnung</c>, die
        /// die Projektzeile <c>energy_project_settings</c> seit jeher merkt:
        /// Eintrag 1 haengt an der Regel auf die Abrechnungseinheit (bevorzugt
        /// der Identitaetsregel <c>from = to</c>), Eintrag 2 an der Regel nach
        /// kWh, sofern der Brennstoff eine fuehrt. Fuehrt er keine, ist „kWh"
        /// reiner Kartenzustand — gespeichert wird ohnehin der Basiswert je
        /// Abrechnungseinheit, es geht also kein Wert verloren.</para>
        ///
        /// <para><b>Verglichen wird normalisiert</b> (<see cref="EinheitSchluessel"/>):
        /// ohne Rand-Leerzeichen, ohne Gross-/Kleinschreibung und mit „³"/„²" auf
        /// „3"/„2" — „Nm3" und „Nm³" sind dieselbe Einheit.</para>
        /// </summary>
        /// <param name="abrechnungseinheit"><c>energy_carrier.billing_unit</c>; leer erlaubt.</param>
        /// <param name="umrechnungen">Die aktiven Regeln des Brennstoffs in
        /// Lesereihenfolge; <c>null</c> erlaubt. Sie tragen nur noch die
        /// <c>ID_Umrechnung</c> der Projektzeile, nicht mehr den Faktor.</param>
        /// <param name="heizwert">Heizwert je Abrechnungseinheit [kWh] — der Faktor
        /// der kWh-Basis. ≤ 0 = keine kWh-Basis.</param>
        public static List<Preisbasis> Preisbasen(string abrechnungseinheit,
                                                  IReadOnlyList<EnergyConversion> umrechnungen,
                                                  double heizwert)
        {
            var liste = new List<Preisbasis>();

            string basis = (abrechnungseinheit ?? "").Trim();
            if (basis.Length > 0)
            {
                liste.Add(new Preisbasis
                {
                    Einheit = basis,
                    Umrechnung = BasisRegel(basis, umrechnungen),
                    Faktor = 1.0
                });
            }

            // Die Abrechnungseinheit IST die Kilowattstunde (Strom, Fernwärme):
            // Dann gibt es nur diesen einen Eintrag - ein zweiter „kWh" wäre
            // derselbe Eintrag zweimal.
            if (basis.Length > 0 && IstKwhEinheit(basis)) return liste;
            if (heizwert <= 0.0) return liste;

            liste.Add(new Preisbasis
            {
                Einheit = DbWerte.EINHEIT_KWH,
                Umrechnung = KwhRegel(basis, umrechnungen),
                Faktor = heizwert
            });

            return liste;
        }

        /// <summary>Ist diese Einheit die Kilowattstunde? (Schreibweise egal.)</summary>
        private static bool IstKwhEinheit(string einheit)
        {
            return string.Equals(EinheitSchluessel(einheit),
                                 EinheitSchluessel(DbWerte.EINHEIT_KWH),
                                 StringComparison.Ordinal);
        }

        /// <summary>
        /// Die Regel, die den Träger von der Abrechnungseinheit nach kWh trägt —
        /// sie liefert seit ET-D nicht mehr den FAKTOR (das tut der Heizwert),
        /// wohl aber die <c>ID_Umrechnung</c>, die die Projektzeile seit jeher
        /// merkt. <c>null</c> = der Brennstoff führt keine solche Regel; dann ist
        /// die Preisbasis „kWh" reiner Kartenzustand.
        /// </summary>
        private static EnergyConversion KwhRegel(string basis,
                                                 IReadOnlyList<EnergyConversion> umrechnungen)
        {
            if (umrechnungen == null) return null;
            string von = EinheitSchluessel(basis);
            string kwh = EinheitSchluessel(DbWerte.EINHEIT_KWH);

            foreach (EnergyConversion c in umrechnungen)
                if (c != null && EinheitSchluessel(c.ToUnitCode) == kwh
                              && EinheitSchluessel(c.FromUnit) == von)
                    return c;

            foreach (EnergyConversion c in umrechnungen)
                if (c != null && EinheitSchluessel(c.ToUnitCode) == kwh)
                    return c;

            return null;
        }

        /// <summary>
        /// Die Regel, an der die Abrechnungseinheit haengt: erst die
        /// Identitaetsregel (<c>from = to = Abrechnungseinheit</c>), sonst die
        /// erste Regel mit passender <c>to_unit</c>, sonst <c>null</c>.
        /// </summary>
        private static EnergyConversion BasisRegel(string basis,
                                                   IReadOnlyList<EnergyConversion> umrechnungen)
        {
            if (umrechnungen == null) return null;
            string schluessel = EinheitSchluessel(basis);

            foreach (EnergyConversion c in umrechnungen)
                if (c != null
                    && EinheitSchluessel(c.ToUnitCode) == schluessel
                    && EinheitSchluessel(c.FromUnit) == schluessel)
                    return c;

            foreach (EnergyConversion c in umrechnungen)
                if (c != null && EinheitSchluessel(c.ToUnitCode) == schluessel)
                    return c;

            return null;
        }

        /// <summary>
        /// Der Vergleichsschluessel einer Einheit: getrimmt, ohne
        /// Gross-/Kleinschreibung, „³"/„²" als „3"/„2". Kulturunabhaengig —
        /// derselbe Schluessel unter jeder Oberflaechensprache.
        /// </summary>
        public static string EinheitSchluessel(string einheit)
        {
            if (string.IsNullOrEmpty(einheit)) return "";
            return einheit.Trim()
                          .Replace('\u00b3', '3')
                          .Replace('\u00b2', '2')
                          .ToUpperInvariant();
        }

        /// <summary>
        /// Der Index der Preisbasis mit dieser Einheit; <c>null</c>, wenn die
        /// Liste leer ist. Eine unbekannte oder leere Einheit faellt auf den
        /// ersten Eintrag zurueck — das ist seit der Bereinigung die
        /// Abrechnungseinheit und nicht mehr die erste beliebige Regel.
        /// </summary>
        public static int? PreisbasisIndex(IReadOnlyList<Preisbasis> basen, string einheit)
        {
            if (basen == null || basen.Count == 0) return null;

            string schluessel = EinheitSchluessel(einheit);
            if (schluessel.Length > 0)
                for (int i = 0; i < basen.Count; i++)
                    if (EinheitSchluessel(basen[i].Einheit) == schluessel) return i;

            return 0;
        }

        /// <summary>Eine Zeile der Preishistorie (<c>energy_price</c>).</summary>
        public sealed class Historienzeile
        {
            /// <summary>
            /// Der Schluessel der Zeile (<c>energy_price.id</c>) — mit ihm laesst
            /// sich GENAU DIESE Zeile loeschen. Ohne ihn blieb nur der Weg ueber
            /// (Traeger, Projekt, Datum), und der trifft zwei Staende desselben
            /// Tages nicht auseinander.
            /// </summary>
            public int Id;

            public DateTime GueltigAb;
            public double? Heizwert;
            public string Basiseinheit;
            public double? Arbeitspreis;
            public double? Grundpreis;
            public double? Leistungspreis;
        }

        /// <summary>
        /// Die Preishistorie eines Trägers, jüngste zuerst — wortgleich aus
        /// <c>ucFuelSettings.LoadHistory</c>.
        /// </summary>
        public static List<Historienzeile> Historie(int traegerId, int? projektId)
        {
            var liste = new List<Historienzeile>();
            var parameter = new List<DbParam> { new DbParam("@cid", traegerId) };

            string sql = "SELECT id, valid_from, heizwert, arbeitspreis, grundpreis, " +
                         "arbeitspreis_unit, leistungspreis FROM energy_price WHERE carrier_id = ?";
            if (projektId.HasValue)
            {
                sql += " AND id_projekt = ?";
                parameter.Add(new DbParam("@pid", projektId.Value));
            }
            sql += " ORDER BY valid_from DESC";

            DataTable dt = DataRepository.GetDataTable(sql, parameter.ToArray());
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
            {
                liste.Add(new Historienzeile
                {
                    Id = r["id"] != DBNull.Value ? Convert.ToInt32(r["id"]) : 0,
                    GueltigAb = r["valid_from"] != DBNull.Value
                        ? Convert.ToDateTime(r["valid_from"], CultureInfo.InvariantCulture)
                        : DateTime.MinValue,
                    Heizwert = Zahl(r, "heizwert"),
                    Basiseinheit = r["arbeitspreis_unit"] != DBNull.Value
                        ? Convert.ToString(r["arbeitspreis_unit"]) : "",
                    Arbeitspreis = Zahl(r, "arbeitspreis"),
                    Grundpreis = Zahl(r, "grundpreis"),
                    Leistungspreis = Zahl(r, "leistungspreis")
                });
            }
            return liste;
        }

        /// <summary>
        /// Ist der Träger dem Projekt zugeordnet? Wortgleich aus
        /// <c>Form_Energietraeger.SpeichereOffenes</c> — ohne Zuordnung wird im
        /// Projektkontext nichts geschrieben.
        /// </summary>
        public static bool ImProjekt(int projektId, int traegerId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM energy_project_settings WHERE ID_Projekt = ? " +
                "AND [ID_Energieträger] = ?",
                new DbParam("@p", projektId),
                new DbParam("@c", traegerId));
            return o != null && o != DBNull.Value && Convert.ToInt32(o) > 0;
        }

        // =====================================================================
        // Leistungspreis-Modus (Etappe KD4, FK6)
        // =====================================================================

        /// <summary>
        /// Der Leistungspreis-Modus des Trägers. Er ist KATALOGSACHE je Träger,
        /// auch im Projektkontext (dokumentierte Zwischenlösung KD4 § 7.1).
        /// </summary>
        public static string LeistungsModus(int traegerId)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT price_power_modus FROM energy_carrier WHERE id = ?",
                    new DbParam("@id", traegerId));
                string s = (o == null || o == DBNull.Value) ? null : Convert.ToString(o);
                return string.Equals(s, DbWerte.LEISTUNGSPREIS_MODUS_MONAT, StringComparison.Ordinal)
                    ? DbWerte.LEISTUNGSPREIS_MODUS_MONAT
                    : DbWerte.LEISTUNGSPREIS_MODUS_JAHR;
            }
            catch { return DbWerte.LEISTUNGSPREIS_MODUS_JAHR; }
        }

        /// <summary>Schreibt den Leistungspreis-Modus.</summary>
        public static void LeistungsModusSchreiben(int traegerId, bool monat)
        {
            try
            {
                DataRepository.ExecuteSQL(
                    "UPDATE energy_carrier SET price_power_modus = ? WHERE id = ?",
                    new DbParam("@m", monat ? DbWerte.LEISTUNGSPREIS_MODUS_MONAT
                                            : DbWerte.LEISTUNGSPREIS_MODUS_JAHR),
                    new DbParam("@id", traegerId));
            }
            catch (Exception ex)
            {
                DataRepository.FehlerMelden(
                    "Der Leistungspreis-Modus konnte nicht gespeichert werden: " + ex.Message);
            }
        }

        // =====================================================================
        // Umrechnungsregeln (Etappe K3)
        // =====================================================================

        /// <summary>
        /// Schreibt den Bearbeitungsstand des Regelblocks nach
        /// <c>energy_conversion</c> — wortgleich aus
        /// <c>ucFuelSettings.SpeichereRegeln</c>.
        ///
        /// <para>Geschrieben wird ausschließlich, was der Anwender angefasst hat
        /// (<c>UserEdited</c>) — der Block ist eine Pflegemaske, kein
        /// Massenschreiber. Neue Zeilen bekommen ihre Id als MAX(ID)+1 (ADR-001);
        /// eine Regel ohne Zieleinheit wird übersprungen statt halbfertig
        /// gespeichert.</para>
        /// </summary>
        public static void RegelnSpeichern(IEnumerable<UmrechnungsRegel> regeln)
        {
            if (regeln == null) return;

            foreach (UmrechnungsRegel r in regeln)
            {
                if (!r.UserEdited) continue;
                if (string.IsNullOrEmpty(r.Von) || string.IsNullOrEmpty(r.Nach)) continue;
                if (r.Faktor <= 0) continue;

                if (r.Id > 0)
                {
                    DataRepository.ExecuteSQL(
                        "UPDATE [energy_conversion] SET [from_unit] = ?, [to_unit] = ?, " +
                        "[factor] = ?, [user_edited] = TRUE, [" +
                        SchemaKatalog.SPALTE_EC_FAKTOR_NAME + "] = ?, [" +
                        SchemaKatalog.SPALTE_EC_AKTIV + "] = ? WHERE [ID] = ?",
                        new DbParam[]
                        {
                            new DbParam("@von", r.Von),
                            new DbParam("@nach", r.Nach),
                            new DbParam("@f", r.Faktor),
                            new DbParam("@n", r.Name ?? ""),
                            new DbParam("@a", r.Aktiv),
                            new DbParam("@id", r.Id)
                        });
                }
                else
                {
                    object max = DataRepository.ExecuteScalar(
                        "SELECT MAX([ID]) FROM [energy_conversion]");
                    int neueId = (max == null || max == DBNull.Value ? 0 : Convert.ToInt32(max)) + 1;

                    DataRepository.ExecuteSQL(
                        "INSERT INTO [energy_conversion] ([ID], [id_brennstoff], [from_unit], " +
                        "[to_unit], [factor], [user_edited], [" +
                        SchemaKatalog.SPALTE_EC_FAKTOR_NAME + "], [" +
                        SchemaKatalog.SPALTE_EC_AKTIV + "]) VALUES (?, ?, ?, ?, ?, TRUE, ?, ?)",
                        new DbParam[]
                        {
                            new DbParam("@id", neueId),
                            new DbParam("@b", r.IdBrennstoff),
                            new DbParam("@von", r.Von),
                            new DbParam("@nach", r.Nach),
                            new DbParam("@f", r.Faktor),
                            new DbParam("@n", r.Name ?? ""),
                            new DbParam("@a", r.Aktiv)
                        });
                    r.Id = neueId;
                }
            }
        }

        // =====================================================================
        // Schreiben der Preise
        // =====================================================================

        /// <summary>Die Werte einer Trägerkarte, wie sie geschrieben werden.</summary>
        public sealed class Preisstand
        {
            public double Arbeitspreis;
            public double Grundpreis;
            public double Leistungspreis;
            public double Hi;
            public double Hs;
            public double CO2;
            public double SO2;
            public double NOx;

            /// <summary>Gewählte Umrechnung; −1 = keine (dann wird NULL geschrieben).
            /// Sie ist die REGEL der Einheitenprüfung, nicht mehr der Kartenzustand —
            /// den trägt seit Schemaschritt 112 <see cref="Preisbasis"/>.</summary>
            public int IdUmrechnung = -1;

            /// <summary>Anzeigetext der Basiseinheit — geht in die Historie.</summary>
            public string Basiseinheit = "";

            /// <summary>
            /// ETAPPE E7c (Schritt F, Mockup U32): der Einheitentext der gewählten
            /// Preisbasis („kWh" oder die Abrechnungseinheit) — der eigene Kartenzustand.
            /// Leer/<c>null</c> schreibt NULL (= Abrechnungseinheit).
            /// </summary>
            public string Preisbasis;
        }

        /// <summary>
        /// Ä9 (26.08.2026): Der KATALOGkontext (Projekt 0) schreibt die
        /// Katalogzeile selbst — ohne Projekt-Settings und ohne Preishistorie.
        /// Wortgleich aus <c>SpeichereWerte</c>, erster Zweig.
        /// </summary>
        public static void Katalogwerte(int traegerId, Preisstand stand)
        {
            DataRepository.ExecuteSQL(
                @"UPDATE energy_carrier
                  SET price_work = ?, price_base = ?, price_power = ?,
                      hi_kwh_per_unit = ?, hs_kwh_per_unit = ?,
                      co2 = ?, so2 = ?, nox = ?
                  WHERE id = ?",
                new DbParam("@ap", Math.Round(stand.Arbeitspreis, 4)),
                new DbParam("@gp", Math.Round(stand.Grundpreis, 4)),
                new DbParam("@lp", Math.Round(stand.Leistungspreis, 4)),
                new DbParam("@hi", Math.Round(stand.Hi, 4)),
                new DbParam("@hs", Math.Round(stand.Hs, 4)),
                new DbParam("@co2", stand.CO2),
                new DbParam("@so2", stand.SO2),
                new DbParam("@nox", stand.NOx),
                new DbParam("@id", traegerId));
        }

        /// <summary>
        /// Legt einen Historienstand zum gewählten Datum an bzw. aktualisiert
        /// ihn — aus <c>SpeichereWerte</c>, zweiter Zweig. Gerufen wird die
        /// Methode nur, wenn sich etwas geändert hat (die Prüfung bleibt beim
        /// Aufrufer, der die DB-Urwerte hält).
        ///
        /// <para><b>Verglichen wird der KALENDERTAG, nicht der Zeitpunkt.</b>
        /// <c>valid_from</c> ist TEXT und trägt im Bestand beides: Tagesstände
        /// (<c>2026-08-12 00:00:00</c>) aus dieser Methode und Zeitpunkte
        /// (<c>2026-08-09 20:33:16</c>) aus der Zuordnung — <c>WizardCtrl</c>
        /// und <c>EnergietraegerVarianteCtrl</c> schreiben dort
        /// <c>DateTime.Now</c>. Ein Vergleich auf Gleichheit verfehlte den
        /// Zeitpunkt desselben Tages und legte eine ZWEITE Zeile an; die Karte
        /// zeigte dann zweimal dasselbe „Gültig ab". Mit
        /// <c>date(valid_from) = date(?)</c> trifft das zweite Speichern den
        /// Stand des Tages und aktualisiert ihn. Das Datum der getroffenen
        /// Zeile bleibt, wie es ist; neu angelegt wird weiter zum Tagesbeginn.</para>
        /// </summary>
        public static void HistorieSchreiben(int traegerId, int projektId, DateTime gueltigAb,
                                             Preisstand stand)
        {
            DbParam[] pruefen =
            {
                new DbParam("@cid", traegerId),
                new DbParam("@prid", projektId),
                new DbParam("@date", DbParamTyp.Date) { Wert = gueltigAb.Date }
            };

            int vorhanden = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM energy_price WHERE carrier_id = ? AND id_projekt = ? " +
                "AND date(valid_from) = date(?)", pruefen));

            if (vorhanden > 0)
            {
                DataRepository.ExecuteSQL(
                    @"UPDATE energy_price
                      SET arbeitspreis = ?, heizwert = ?, grundpreis = ?,
                          arbeitspreis_unit = ?, leistungspreis = ?
                      WHERE carrier_id = ? AND id_projekt = ? AND date(valid_from) = date(?)",
                    new DbParam[]
                    {
                        new DbParam("@ap", Math.Round(stand.Arbeitspreis, 4)),
                        new DbParam("@hi", Math.Round(stand.Hi, 4)),
                        new DbParam("@gp", Math.Round(stand.Grundpreis, 4)),
                        new DbParam("@au", stand.Basiseinheit ?? ""),
                        new DbParam("@lp", Math.Round(stand.Leistungspreis, 4)),
                        new DbParam("@cid", traegerId),
                        new DbParam("@prid", projektId),
                        new DbParam("@date", DbParamTyp.Date) { Wert = gueltigAb.Date }
                    });
            }
            else
            {
                DataRepository.ExecuteSQL(
                    @"INSERT INTO energy_price
                      (carrier_id, id_projekt, arbeitspreis, heizwert, grundpreis,
                       valid_from, arbeitspreis_unit, leistungspreis)
                      VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                    new DbParam[]
                    {
                        new DbParam("@cid", traegerId),
                        new DbParam("@prid", projektId),
                        new DbParam("@ap", Math.Round(stand.Arbeitspreis, 4)),
                        new DbParam("@hi", Math.Round(stand.Hi, 4)),
                        new DbParam("@gp", Math.Round(stand.Grundpreis, 4)),
                        new DbParam("@date", DbParamTyp.Date) { Wert = gueltigAb.Date },
                        new DbParam("@au", stand.Basiseinheit ?? ""),
                        new DbParam("@lp", Math.Round(stand.Leistungspreis, 4))
                    });
            }
        }

        /// <summary>
        /// Löscht GENAU EINE Zeile der Preishistorie (Anwenderwunsch 14.09.2026,
        /// „historische Energieträger werte sollen gelöscht werden können").
        ///
        /// <para>Der Schlüssel ist <c>energy_price.id</c>; Träger und Projekt
        /// stehen als Riegel daneben, damit ein veralteter Stand der Karte
        /// nicht die Zeile eines anderen Trägers oder Projekts trifft. Zwei
        /// Stände desselben Tages lassen sich so einzeln entfernen.</para>
        /// </summary>
        /// <returns>Die Zahl der gelöschten Zeilen: 1 = getroffen, 0 = nichts.</returns>
        public static int HistorieLoeschen(int zeileId, int traegerId, int projektId)
        {
            if (zeileId <= 0) return 0;

            return (int)DataRepository.ExecuteNonQuery(
                "DELETE FROM energy_price WHERE id = ? AND carrier_id = ? AND id_projekt = ?",
                new DbParam[]
                {
                    new DbParam("@id", zeileId),
                    new DbParam("@cid", traegerId),
                    new DbParam("@prid", projektId)
                });
        }

        /// <summary>
        /// Die Projektübersteuerung — wortgleich aus <c>SpeichereWerte</c>,
        /// dritter Zweig (Upsert: erst UPDATE, bei 0 Zeilen INSERT).
        /// </summary>
        public static void Projektwerte(int projektId, int traegerId, Preisstand stand)
        {
            object idUmrechnung = stand.IdUmrechnung != -1
                ? (object)stand.IdUmrechnung : DBNull.Value;

            // ETAPPE E7c (Schritt F, Schemaschritt 112): Der Kartenzustand „Preisbasis"
            // wandert mit, wo die Spalte steht; eine Datenbank vor 112 schreibt die
            // übrigen Felder wie bisher. Kein −1-Rückfall mehr für die Karte: Die
            // Basis steht als Einheitentext da, unabhängig davon, ob der Brennstoff eine
            // Regel nach kWh führt.
            bool mitBasis = PreisbasisSpalteVorhanden();
            object basis = string.IsNullOrWhiteSpace(stand.Preisbasis)
                ? (object)DBNull.Value : stand.Preisbasis.Trim();

            var werte = new List<DbParam>
            {
                new DbParam("@p", stand.Arbeitspreis),
                new DbParam("@pl", stand.Leistungspreis),
                new DbParam("@hi", stand.Hi),
                new DbParam("@hs", stand.Hs),
                new DbParam("@b", stand.Grundpreis),
                new DbParam("@cid", idUmrechnung),
                new DbParam("@co2", stand.CO2),
                new DbParam("@so2", stand.SO2),
                new DbParam("@nox", stand.NOx)
            };
            if (mitBasis) werte.Add(new DbParam("@basis", basis));
            werte.Add(new DbParam("@pid", projektId));
            werte.Add(new DbParam("@eid", traegerId));

            int zeilen = (int)DataRepository.ExecuteNonQuery(
                @"UPDATE energy_Project_settings
                  SET custom_price_work = ?, custom_price_power = ?, custom_hi = ?, custom_hs = ?,
                      custom_price_base = ?, ID_Umrechnung = ?,
                      co2 = ?, so2 = ?, nox = ?" +
                (mitBasis ? ", [" + SchemaKatalog.SPALTE_EPS_PREISBASIS + "] = ?" : "") + @"
                  WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                werte.ToArray());

            if (zeilen != 0) return;

            var neu = new List<DbParam>
            {
                new DbParam("@pid", projektId),
                new DbParam("@eid", traegerId),
                new DbParam("@p", stand.Arbeitspreis),
                new DbParam("@pl", stand.Leistungspreis),
                new DbParam("@h", stand.Hi),
                new DbParam("@hs", stand.Hs),
                new DbParam("@b", stand.Grundpreis),
                new DbParam("@cid", idUmrechnung),
                new DbParam("@co2", stand.CO2),
                new DbParam("@so2", stand.SO2),
                new DbParam("@nox", stand.NOx)
            };
            if (mitBasis) neu.Add(new DbParam("@basis", basis));

            DataRepository.ExecuteSQL(
                @"INSERT INTO energy_Project_settings
                  (ID_Projekt, [ID_Energieträger], custom_price_work, custom_price_power,
                   custom_hi, custom_Hs, custom_price_base, ID_Umrechnung, co2, so2, nox" +
                (mitBasis ? ", [" + SchemaKatalog.SPALTE_EPS_PREISBASIS + "]" : "") + @")
                  VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?" + (mitBasis ? ", ?" : "") + ")",
                neu.ToArray());
        }

        // =====================================================================
        // Kleinwerkzeug
        // =====================================================================

        /// <summary>Die Projektübersteuerung eines Trägers; jede Spalte darf NULL sein (Ä-BK3).</summary>
        public sealed class Projektpreis
        {
            public double? Arbeitspreis;
            public double? Grundpreis;
            public double? Leistungspreis;
            public double? Hi;
            public double? Hs;
            public double? CO2;
            public double? SO2;
            public double? NOx;
            public int? IdUmrechnung;

            /// <summary>Die zweistufige Leistungspreis-Staffel (Schritt 104); leer, wenn
            /// nicht gepflegt oder die Spalten fehlen — nie <c>null</c>.</summary>
            public LeistungspreisStaffel Staffel = new LeistungspreisStaffel();

            /// <summary>ETAPPE E7c (Schritt F, Schritt 112): die gemerkte Preisbasis der
            /// Karte (Einheitentext); <c>null</c> = nicht gepflegt, also die
            /// Abrechnungseinheit.</summary>
            public string Preisbasis;

            /// <summary>ETAPPE E7c: Die Datenbank führt die Spalte noch nicht (vor
            /// Schemaschritt 112) — die Karte nennt das, statt still zurückzufallen.</summary>
            public bool PreisbasisSpalteFehlt;

            /// <summary>ETAPPE E9a (Schritt C, Schemaschritt 117): Arbeits-, Grund- und
            /// Leistungspreis je Szenario (Günstig/Ungünstig) — sechs nullbare Felder, leer =
            /// „wie Erwartet"; nie <c>null</c>. Geschrieben wird der Satz getrennt über
            /// <see cref="SzenarioSchreiben"/>.</summary>
            public TraegerpreisSzenario Szenario = new TraegerpreisSzenario();
        }

        private static double? Zahl(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture); }
            catch { return null; }
        }
    }
}
