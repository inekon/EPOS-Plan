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
        /// Die Umrechnungen eines Brennstoffs — wortgleich aus
        /// <c>ucFuelSettings.GetConversions</c>.
        /// </summary>
        public static List<EnergyConversion> Umrechnungen(int idBrennstoff)
        {
            var liste = new List<EnergyConversion>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT id_brennstoff, from_unit, to_unit, factor FROM ENERGY_CONVERSION " +
                "WHERE id_brennstoff = ?",
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
                    ? (int?)Convert.ToInt32(row["ID_Umrechnung"]) : null
            };
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
        /// Die Klappliste „Preisbasis" eines Traegers — JE EINHEIT GENAU EIN
        /// EINTRAG, die Abrechnungseinheit zuerst, danach die Zieleinheiten der
        /// Regeln in Regelreihenfolge.
        ///
        /// <para><b>Befund W4-B-1 (Windows-Abnahme 04.09.2026).</b> Der Vorlaeufer
        /// fuellte die Liste mit der <c>to_unit</c> JEDER Regel des Brennstoffs
        /// (<c>ucFuelSettings.cs:1935–1954</c> — „kein Filter auf <c>from_unit</c>,
        /// <c>aktiv</c> oder Traeger, kein <c>ORDER BY</c>", festgehalten in
        /// <c>Konzept_Einheitenbruch_Energietraeger_EPOS-Plan.md</c> § 2.2), und
        /// die Blazor-Huelle uebernahm das wortgleich. Weil die Gas-Brennstoffe
        /// neben der Identitaetsregel <c>Nm³ → Nm³</c> auch den z-Faktor
        /// <c>m³ → Nm³</c> fuehren, stand „Nm³" ZWEIMAL in der Liste; bei Erdgas E
        /// kam ueber Regel 67 (<c>Nm³ → kWh</c>) das gemeldete „Nm³, kWh, Nm³"
        /// zustande. Umgekehrt blieb die Liste — und damit das Feld — LEER,
        /// sobald der Brennstoff gar keine Regel fuehrt.</para>
        ///
        /// <para><b>Die Abrechnungseinheit steht immer an erster Stelle.</b> Sie
        /// ist die Einheit, in der die Werte in der Datenbank liegen; eine Liste,
        /// die sie nicht anbietet, kann den gespeicherten Zustand nicht zeigen.
        /// Traegt der Brennstoff eine Regel auf diese Einheit, haengt der Eintrag
        /// an ihr — bevorzugt an der Identitaetsregel (<c>from = to</c>), sonst an
        /// der ersten Regel mit passender <c>to_unit</c>. Das ist genau die Regel,
        /// die der Vorlaeufer ueber seinen Textvergleich fand, weshalb sich fuer
        /// jeden Traeger des Katalogs weder Faktor noch gespeicherte
        /// <c>ID_Umrechnung</c> aendern.</para>
        ///
        /// <para><b>Verglichen wird normalisiert</b> (<see cref="EinheitSchluessel"/>):
        /// ohne Rand-Leerzeichen, ohne Gross-/Kleinschreibung und mit „³"/„²" auf
        /// „3"/„2" — „Nm3" und „Nm³" sind dieselbe Einheit. Angezeigt wird die
        /// zuerst gefundene Schreibweise.</para>
        /// </summary>
        /// <param name="abrechnungseinheit"><c>energy_carrier.billing_unit</c>; leer erlaubt.</param>
        /// <param name="umrechnungen">Die Regeln des Brennstoffs in Lesereihenfolge; <c>null</c> erlaubt.</param>
        public static List<Preisbasis> Preisbasen(string abrechnungseinheit,
                                                  IReadOnlyList<EnergyConversion> umrechnungen)
        {
            var liste = new List<Preisbasis>();
            var gesehen = new HashSet<string>(StringComparer.Ordinal);

            string basis = (abrechnungseinheit ?? "").Trim();
            if (basis.Length > 0)
            {
                liste.Add(new Preisbasis
                {
                    Einheit = basis,
                    Umrechnung = BasisRegel(basis, umrechnungen),
                    Faktor = 1.0
                });
                // Der Faktor der Basis ist der Faktor ihrer Regel - ohne Regel 1.
                if (liste[0].Umrechnung != null) liste[0].Faktor = liste[0].Umrechnung.Factor;
                gesehen.Add(EinheitSchluessel(basis));
            }

            if (umrechnungen != null)
            {
                foreach (EnergyConversion c in umrechnungen)
                {
                    if (c == null) continue;
                    string ziel = (c.ToUnitCode ?? "").Trim();
                    if (ziel.Length == 0) continue;
                    if (!gesehen.Add(EinheitSchluessel(ziel))) continue;

                    liste.Add(new Preisbasis { Einheit = ziel, Umrechnung = c, Faktor = c.Factor });
                }
            }

            return liste;
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

            string sql = "SELECT valid_from, heizwert, arbeitspreis, grundpreis, " +
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

            /// <summary>Gewählte Umrechnung; −1 = keine (dann wird NULL geschrieben).</summary>
            public int IdUmrechnung = -1;

            /// <summary>Anzeigetext der Basiseinheit — geht in die Historie.</summary>
            public string Basiseinheit = "";
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
        /// ihn — wortgleich aus <c>SpeichereWerte</c>, zweiter Zweig. Gerufen
        /// wird die Methode nur, wenn sich etwas geändert hat (die Prüfung
        /// bleibt beim Aufrufer, der die DB-Urwerte hält).
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
                "AND valid_from = ?", pruefen));

            if (vorhanden > 0)
            {
                DataRepository.ExecuteSQL(
                    @"UPDATE energy_price
                      SET arbeitspreis = ?, heizwert = ?, grundpreis = ?,
                          arbeitspreis_unit = ?, leistungspreis = ?
                      WHERE carrier_id = ? AND id_projekt = ? AND valid_from = ?",
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
        /// Die Projektübersteuerung — wortgleich aus <c>SpeichereWerte</c>,
        /// dritter Zweig (Upsert: erst UPDATE, bei 0 Zeilen INSERT).
        /// </summary>
        public static void Projektwerte(int projektId, int traegerId, Preisstand stand)
        {
            object idUmrechnung = stand.IdUmrechnung != -1
                ? (object)stand.IdUmrechnung : DBNull.Value;

            int zeilen = (int)DataRepository.ExecuteNonQuery(
                @"UPDATE energy_Project_settings
                  SET custom_price_work = ?, custom_price_power = ?, custom_hi = ?, custom_hs = ?,
                      custom_price_base = ?, ID_Umrechnung = ?,
                      co2 = ?, so2 = ?, nox = ?
                  WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam[]
                {
                    new DbParam("@p", stand.Arbeitspreis),
                    new DbParam("@pl", stand.Leistungspreis),
                    new DbParam("@hi", stand.Hi),
                    new DbParam("@hs", stand.Hs),
                    new DbParam("@b", stand.Grundpreis),
                    new DbParam("@cid", idUmrechnung),
                    new DbParam("@co2", stand.CO2),
                    new DbParam("@so2", stand.SO2),
                    new DbParam("@nox", stand.NOx),
                    new DbParam("@pid", projektId),
                    new DbParam("@eid", traegerId)
                });

            if (zeilen != 0) return;

            DataRepository.ExecuteSQL(
                @"INSERT INTO energy_Project_settings
                  (ID_Projekt, [ID_Energieträger], custom_price_work, custom_price_power,
                   custom_hi, custom_Hs, custom_price_base, ID_Umrechnung, co2, so2, nox)
                  VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                new DbParam[]
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
                });
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
        }

        private static double? Zahl(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture); }
            catch { return null; }
        }
    }
}
