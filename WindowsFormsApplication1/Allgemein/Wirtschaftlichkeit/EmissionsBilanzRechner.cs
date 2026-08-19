using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Emissionsbilanz gekoppelte vs. getrennte Erzeugung (Konzept Kap. 2.8;
    /// Stufe W3, Phase 8) — Muster BHKW-Plan Tab_Energiebilanz/Tab_Emissionen.
    ///
    /// Systemgrenze (wie Alt-Verfahren): die getrennte Referenz erzeugt DIESELBE
    /// Brennstoff-Wärme (BHKW + Kessel) in einem Referenzkessel und DENSELBEN
    /// KWK-Strom in einem Referenz-Kraftwerkspark (Katalog Tab_Kraftwerkspark,
    /// inkl. Netzverluste). Wärmepumpe/Solar/PV sind in beiden Welten identisch
    /// und kürzen sich heraus.
    ///
    /// Faktoren je Träger über die bewährte Kette (Vorgabe 11.08.2026):
    /// Projektwert (energy_project_settings) → Katalog Tab_Brennstoff_Stamm →
    /// energy_carrier. Einheiten: CO₂ g/kWh, SO₂/NOx mg/kWh (Kenndaten-Katalog).
    ///
    /// Rechnet LIVE aus den persistierten Jahresergebnissen (Tab_Ergebnis*) —
    /// keine eigene Persistenz nötig; Reiter, Word und Excel rufen denselben
    /// Rechner mit denselben Parametern.
    ///
    /// <para>
    /// <b>Leitentscheidungen L12 und L13.</b> Die Systemgrenze oben IST die
    /// Stromgutschriftmethode: Der KWK-Strom wird in der getrennten Referenz im
    /// Kraftwerkspark erzeugt und damit gutgeschrieben. Genau diese Methode ist zum
    /// 01.01.2027 abgeschafft (GModG — Grundlagen, Abschnitt 7.4). Seit L12 liegen
    /// deshalb beide Rechenwege parallel vor, umgeschaltet über das Gültig-ab-Datum
    /// des Verdrängungsstrommix im Gesetzeskatalog (<see cref="BilanzKonvention"/>);
    /// L13 legt daneben offen, mit welcher Konvention biogenes Verbrennungs-CO₂
    /// bewertet wird. Beides steht im Bericht.
    /// </para>
    /// </summary>
    public static class EmissionsBilanzRechner
    {
        public const string TAB_PARK = "Tab_Kraftwerkspark";

        // ------------------------------------------------------------- Katalog

        /// <summary>Legt den Kraftwerkspark-Katalog an und befüllt ihn beim ersten
        /// Mal (Deutscher Strommix, Erdgas-GuD, Steinkohle) — in den Kenndaten pflegbar.</summary>
        public static void StelleKatalogSicher()
        {
            try
            {
                using (var conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    DataTable schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables,
                        new object[] { null, null, TAB_PARK, "TABLE" });
                    bool neu = schema == null || schema.Rows.Count == 0;
                    if (neu)
                    {
                        using (var cmd = new OleDbCommand(
                            "CREATE TABLE " + TAB_PARK + " (" +
                            "ID LONG CONSTRAINT PK_KwPark PRIMARY KEY, " +
                            "Bezeichner TEXT(100), " +
                            "Wirkungsgrad DOUBLE, " +      // el. Wirkungsgrad [%]; 100 = Faktoren je kWh Strom
                            "CO2 DOUBLE, " +               // g/kWh Brennstoff
                            "SO2 DOUBLE, " +               // mg/kWh Brennstoff
                            "NOx DOUBLE, " +               // mg/kWh Brennstoff
                            "Netzverluste DOUBLE)", conn))
                            cmd.ExecuteNonQuery();

                        // Vorbefüllung (Vorgabewerte, im Katalog änderbar):
                        //  - Strommix: Faktoren je kWh STROM (η = 100 %, Netzverluste 0 —
                        //    im Mixfaktor bereits enthalten), CO₂ analog Katalogträger
                        //    „Elektrische Energie" (560 g/kWh).
                        //  - GuD/Steinkohle: Brennstoff-Faktoren aus Tab_Brennstoff_Stamm
                        //    (Erdgas 240 / Kohle 400 g/kWh) + typischer el. Wirkungsgrad.
                        Seed(conn, 1, "Deutscher Strommix (Katalogwert Strom)", 100, 560, 200, 280, 0);
                        Seed(conn, 2, "Erdgas-GuD-Kraftwerk", 58, 240, 0.3, 110, 5);
                        Seed(conn, 3, "Steinkohle-Kraftwerk", 42, 400, 600, 220, 5);
                    }
                }
            }
            catch { }
        }

        private static void Seed(OleDbConnection conn, int id, string name,
                                 double eta, double co2, double so2, double nox, double verluste)
        {
            using (var cmd = new OleDbCommand(
                "INSERT INTO " + TAB_PARK + " (ID, Bezeichner, Wirkungsgrad, CO2, SO2, NOx, Netzverluste) " +
                "VALUES (?,?,?,?,?,?,?)", conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@w", eta);
                cmd.Parameters.AddWithValue("@c", co2);
                cmd.Parameters.AddWithValue("@s", so2);
                cmd.Parameters.AddWithValue("@x", nox);
                cmd.Parameters.AddWithValue("@v", verluste);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Alle Katalogeinträge (für die Auswahl im Parameterdialog).</summary>
        public static List<Kraftwerkspark> LadeKatalog()
        {
            StelleKatalogSicher();
            var liste = new List<Kraftwerkspark>();
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT * FROM " + TAB_PARK + " ORDER BY ID");
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                        liste.Add(new Kraftwerkspark
                        {
                            Id = Convert.ToInt32(r["ID"]),
                            Bezeichner = r["Bezeichner"] != DBNull.Value ? r["Bezeichner"].ToString() : "",
                            WirkungsgradProzent = D(r, "Wirkungsgrad") ?? 100,
                            CO2 = D(r, "CO2") ?? 0,
                            SO2 = D(r, "SO2") ?? 0,
                            NOx = D(r, "NOx") ?? 0,
                            NetzverlusteProzent = D(r, "Netzverluste") ?? 0
                        });
            }
            catch { }
            return liste;
        }

        public static Kraftwerkspark LadePark(int id)
        {
            if (id <= 0) return null;
            foreach (Kraftwerkspark p in LadeKatalog())
                if (p.Id == id) return p;
            return null;
        }

        // ------------------------------------------------------------- Bilanz

        /// <summary>
        /// Emissionsbilanz eines Projekts aus dem letzten Simulationsergebnis.
        /// null = kein Ergebnis oder kein Kraftwerkspark gewählt.
        /// </summary>
        public static EmissionsBilanz Berechne(int idProjekt, WirtschaftlichkeitParameter p)
        {
            return Berechne(idProjekt, p, null);
        }

        /// <summary>
        /// Wie <see cref="Berechne(int,WirtschaftlichkeitParameter)"/>, mit bereits
        /// aufgelösten Bilanzierungsregeln (spart den Katalogzugriff, wenn der Aufrufer
        /// sie ohnehin hat). <paramref name="konvention"/> <c>null</c> ⇒ sie werden hier
        /// aus dem Katalog bestimmt.
        /// </summary>
        public static EmissionsBilanz Berechne(int idProjekt, WirtschaftlichkeitParameter p,
                                               BilanzKonvention konvention)
        {
            if (p == null || p.IdKraftwerkspark <= 0) return null;
            Kraftwerkspark park = LadePark(p.IdKraftwerkspark);
            if (park == null) return null;

            ErgebnisModel m = null;
            try { m = new ErgebnisCtrl().Load(idProjekt); } catch { }
            if (m == null) return null;

            // LEITENTSCHEIDUNGEN L12/L13 — der Rechenweg dieser Bilanz und die
            // Bilanzierungskonvention für Biomasse. Beide werden hier EINMAL aufgelöst
            // und mit dem Ergebnis mitgegeben, damit Reiter und Bericht sie ausweisen
            // können, statt sie zu erraten.
            BilanzKonvention k = konvention ?? BilanzKonvention.Bestimme(p, new GesetzKatalog());

            var b = new EmissionsBilanz
            {
                IdProjekt = idProjekt,
                ParkName = park.Bezeichner,
                Konvention = k
            };

            // ---------------- gekoppelt: Brennstoff-Emissionen BHKW + Kessel ----------------
            double co2 = 0, so2 = 0, nox = 0, brennstoffWaerme = 0, kwkStrom = 0;
            double biogenMWh = 0;                                 // L13
            bool co2Voll = true, so2Voll = true, noxVoll = true;   // je Schadstoff (Review Phase 8)

            Action<int, double> add = (carrierId, verbrauchMWh) =>
            {
                if (verbrauchMWh <= 0) return;
                if (carrierId <= 0) { co2Voll = so2Voll = noxVoll = false; return; }
                if (IstBiogenerTraeger(carrierId)) biogenMWh += verbrauchMWh;   // L13
                Faktoren f = LadeFaktoren(idProjekt, carrierId);
                // Einheiten: MWh × 1000 kWh × Faktor ÷ 1e6 →
                //   CO₂ [g/kWh]  → t/a  = MWh × Faktor / 1000
                //   SO₂/NOx [mg/kWh] → kg/a = MWh × Faktor / 1000
                if (f.CO2.HasValue) co2 += verbrauchMWh * f.CO2.Value / 1000.0; else co2Voll = false;
                if (f.SO2.HasValue) so2 += verbrauchMWh * f.SO2.Value / 1000.0; else so2Voll = false;
                if (f.NOx.HasValue) nox += verbrauchMWh * f.NOx.Value / 1000.0; else noxVoll = false;
            };

            if (m.BHKW != null)
            {
                brennstoffWaerme += m.BHKW.Waermeproduktion;
                kwkStrom = m.BHKW.Stromproduktion;
                if (m.BHKW.Module != null)
                    foreach (ErgebnisBHKWModulModel mo in m.BHKW.Module) add(mo.CarrierId, mo.Verbrauch);
            }
            if (m.Heizkessel != null)
            {
                brennstoffWaerme += m.Heizkessel.Waermeproduktion;
                if (m.Heizkessel.Module != null)
                    foreach (ErgebnisHeizkesselModulModel mo in m.Heizkessel.Module) add(mo.CarrierId, mo.Verbrauch);
            }

            if (brennstoffWaerme <= 0 && kwkStrom <= 0)
            {
                b.Hinweis = "Keine Brennstoff-Erzeuger — Emissionsbilanz entfällt.";
                return b;
            }

            // LEITENTSCHEIDUNG L13 — biogenes Verbrennungs-CO₂. Der Brennstoffkatalog
            // führt für biogene Träger reine VORKETTENwerte (Holz und Pellets 20,
            // Biogas 140, Rapsöl und Tierische Fette 210 g/kWh); das Verbrennungs-CO₂
            // steht dort mit null. Das ist die Konvention von GEG/GModG, UBA-Emissions-
            // bilanz und BAFA EEW — und die Vorgabe. Wer die Konvention des
            // UBA-CO₂-Rechners wählt, bekommt den Faktor aus dem Katalog ZUSÄTZLICH auf
            // die biogene Menge; der Betrag steht getrennt, damit sichtbar bleibt,
            // welcher Teil aus der Wahl stammt.
            b.CO2BiogenT = biogenMWh * k.BiogenZuschlagGJeKWh / 1000.0;   // MWh × g/kWh → t
            if (co2Voll) b.CO2GekoppeltT = co2 + b.CO2BiogenT;
            if (so2Voll) b.SO2GekoppeltKg = so2;
            if (noxVoll) b.NOxGekoppeltKg = nox;
            if (!co2Voll || !so2Voll || !noxVoll)
                b.Hinweis = "Emissionsfaktoren unvollständig (Kostenmaske/Katalog prüfen) — " +
                            "betroffene Schadstoffe erscheinen als „—“.";

            // ---------------- getrennt: Referenzkessel + Kraftwerkspark ----------------
            // Wirkungsgrade tolerant lesen: Werte ≤ 1,5 gelten als Bruch (0,9),
            // größere als Prozent (90); anschließend auf [10 %, 110 %] geklemmt
            // (Review Phase 8 — verhindert 100-fach überhöhte Referenzmengen).
            Faktoren rk = LadeKatalogFaktoren(p.RefKesselIdBrennstoff);
            double eta = Wirkungsgrad(p.RefKesselWirkungsgrad);
            double etaPark = Wirkungsgrad(park.WirkungsgradProzent);
            double verlust = Math.Min(0.99, Math.Max(0, park.NetzverlusteProzent / 100.0));

            // -------- LEITENTSCHEIDUNG L12: die Gutschrift für den KWK-Strom --------
            //
            // Bis 31.12.2026 erzeugt die getrennte Referenz denselben KWK-Strom im
            // Kraftwerkspark; genau das IST die Stromgutschriftmethode. Zum 01.01.2027
            // entfällt der Verdrängungsstrommix ersatzlos (GModG, BGBl. 2026 I Nr. 226),
            // die Methode ist abgeschafft und die KWK-Wärme nach DIN EN 15316-4-5 zu
            // bewerten. Umgeschaltet wird über dasselbe Gültig-ab-Datum aus dem Katalog
            // (BilanzKonvention) — hier steht KEINE Jahreszahl.
            //
            // ABGRENZUNG, die auch im Bericht steht: Verdrahtet ist der WEGFALL der
            // Gutschrift, nicht das Zuteilungsverfahren der DIN EN 15316-4-5 — deren
            // Text gehört nicht zur Faktenbasis des Vorhabens. Wer dennoch eine
            // Gutschrift will, wählt den Substitutionsfaktor; das ist eine methodische
            // Wahl ohne Rechtsvorgabe und wird als solche ausgewiesen.
            double parkCO2 = 0, parkSO2 = 0, parkNOx = 0;
            string nachtrag = null;    // Hinweis, der die Bestandsmeldungen nicht verdrängen darf
            if (k.Stromgutschrift)
            {
                // KWK-Strom frei Verbraucher → Erzeugung inkl. Netzverluste.
                double brennstoffPark = kwkStrom / (1.0 - verlust) / etaPark;   // MWh
                parkCO2 = brennstoffPark * park.CO2 / 1000.0;                   // t/a
                parkSO2 = brennstoffPark * park.SO2 / 1000.0;                   // kg/a
                parkNOx = brennstoffPark * park.NOx / 1000.0;
            }
            else if (k.Substitution && k.SubstitutionsfaktorGJeKWh.HasValue)
            {
                // Der Substitutionsfaktor ist eine Größe je kWh STROM, kein
                // Brennstofffaktor — Wirkungsgrad und Netzverluste des Parks gehen
                // deshalb NICHT ein. Für SO₂ und NOx gibt es keinen belegten
                // Substitutionswert; sie bleiben ohne Gutschrift, und der Hinweis sagt es.
                parkCO2 = kwkStrom * k.SubstitutionsfaktorGJeKWh.Value / 1000.0;   // t/a
                if (kwkStrom > 0)
                    nachtrag = "Substitutionsmethode: Gutschrift nur für CO₂ — für SO₂ und NOx " +
                               "gibt es keinen belegten Substitutionsfaktor.";
            }
            b.CO2GutschriftStromT = parkCO2;

            // Referenzkessel-Anteil je Schadstoff nur mit vorhandenem Faktor.
            double brennstoffRef = brennstoffWaerme / eta;                      // MWh Brennstoff
            bool refNoetig = brennstoffWaerme > 0;

            if (!refNoetig || rk.CO2.HasValue)
                b.CO2GetrenntT = (refNoetig ? brennstoffRef * rk.CO2.Value / 1000.0 : 0) + parkCO2;
            else if (b.Hinweis == null)
                b.Hinweis = "Referenzkessel-Träger ohne CO₂-Faktor (Katalog Tab_Brennstoff_Stamm prüfen).";
            if (!refNoetig || rk.SO2.HasValue)
                b.SO2GetrenntKg = (refNoetig ? brennstoffRef * rk.SO2.Value / 1000.0 : 0) + parkSO2;
            if (!refNoetig || rk.NOx.HasValue)
                b.NOxGetrenntKg = (refNoetig ? brennstoffRef * rk.NOx.Value / 1000.0 : 0) + parkNOx;

            // Zuletzt: die Hinweise der Bilanzierungsregeln. Sie stehen HINTER den
            // Bestandsmeldungen, damit sie keine davon verdrängen (die Zuweisungen oben
            // prüfen auf b.Hinweis == null).
            if (nachtrag != null) b.Hinweis = Anhaengen(b.Hinweis, nachtrag);
            if (k.Hinweis != null) b.Hinweis = Anhaengen(b.Hinweis, k.Hinweis);

            return b;
        }

        /// <summary>Hinweistexte verketten, ohne einen bestehenden zu verdrängen.</summary>
        private static string Anhaengen(string bisher, string neu)
        {
            return string.IsNullOrEmpty(bisher) ? neu : bisher + " | " + neu;
        }

        /// <summary>
        /// LEITENTSCHEIDUNG L13 — ist dieser Energieträger biogen? Entschieden über die
        /// Brennstoffkategorie, mit derselben Regel wie im <c>KostenEmissionRechner</c>
        /// (<see cref="BilanzKonvention.IstBiogen"/>).
        ///
        /// <para>Bewusst OHNE Zwischenspeicher: Je Lauf gibt es eine Handvoll Module,
        /// und ein prozessweiter Cache über eine im Katalog pflegbare Einstufung wäre
        /// nach der ersten Katalogänderung falsch. <c>LadeFaktoren</c> fragt aus
        /// demselben Grund ebenfalls je Träger neu.</para>
        /// </summary>
        private static bool IstBiogenerTraeger(int carrierId)
        {
            if (carrierId <= 0) return false;
            bool treffer = false;
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT bs.ID_Kategorie, bs.Bezeichner FROM energy_carrier AS ec " +
                    "INNER JOIN Tab_Brennstoff_Stamm AS bs ON ec.id_brennstoff = bs.ID " +
                    "WHERE ec.id = ?",
                    new OleDbParameter("@c", carrierId));
                if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["ID_Kategorie"] != DBNull.Value)
                    treffer = BilanzKonvention.IstBiogen(
                        Convert.ToInt32(dt.Rows[0]["ID_Kategorie"]),
                        dt.Rows[0]["Bezeichner"] != DBNull.Value
                            ? dt.Rows[0]["Bezeichner"].ToString() : "");
            }
            catch { }
            return treffer;
        }

        /// <summary>Toleranter Wirkungsgrad: ≤ 1,5 = Bruch, sonst Prozent; Klemmung [0,10 … 1,10].</summary>
        private static double Wirkungsgrad(double wert)
        {
            double eta = wert <= 1.5 ? wert : wert / 100.0;
            return Math.Min(1.10, Math.Max(0.10, eta));
        }

        // ------------------------------------------------------------- Faktoren

        private class Faktoren
        {
            public double? CO2;   // g/kWh
            public double? SO2;   // mg/kWh
            public double? NOx;   // mg/kWh
        }

        /// <summary>Faktorkette je Projekt-Träger: Projektwert → Tab_Brennstoff_Stamm → energy_carrier.</summary>
        private static Faktoren LadeFaktoren(int idProjekt, int carrierId)
        {
            var f = new Faktoren();
            double? sCO2 = null, sSO2 = null, sNOx = null;
            try
            {
                DataTable s = DataRepository.GetDataTable(
                    "SELECT co2, so2, nox FROM energy_project_settings " +
                    "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                    new OleDbParameter("@p", idProjekt), new OleDbParameter("@c", carrierId));
                if (s != null && s.Rows.Count > 0)
                { sCO2 = D(s.Rows[0], "co2"); sSO2 = D(s.Rows[0], "so2"); sNOx = D(s.Rows[0], "nox"); }
            }
            catch { }

            double? bCO2 = null, bSO2 = null, bNOx = null;
            double? kCO2 = null, kSO2 = null, kNOx = null;
            try
            {
                DataTable b = DataRepository.GetDataTable(
                    "SELECT bs.CO2 AS bsCO2, bs.SO2 AS bsSO2, bs.NOx AS bsNOx, " +
                    "ec.co2 AS ecCO2, ec.so2 AS ecSO2, ec.nox AS ecNOx " +
                    "FROM energy_carrier AS ec " +
                    "LEFT JOIN Tab_Brennstoff_Stamm AS bs ON ec.id_brennstoff = bs.ID " +
                    "WHERE ec.id = ?",
                    new OleDbParameter("@c", carrierId));
                if (b != null && b.Rows.Count > 0)
                {
                    bCO2 = D(b.Rows[0], "bsCO2"); bSO2 = D(b.Rows[0], "bsSO2"); bNOx = D(b.Rows[0], "bsNOx");
                    kCO2 = D(b.Rows[0], "ecCO2"); kSO2 = D(b.Rows[0], "ecSO2"); kNOx = D(b.Rows[0], "ecNOx");
                }
            }
            catch { }

            f.CO2 = Erster(sCO2, bCO2, kCO2);
            f.SO2 = Erster(sSO2, bSO2, kSO2);
            f.NOx = Erster(sNOx, bNOx, kNOx);
            return f;
        }

        /// <summary>Faktoren direkt aus dem Brennstoff-Katalog (Referenzkessel-Träger).</summary>
        private static Faktoren LadeKatalogFaktoren(int idBrennstoff)
        {
            var f = new Faktoren();
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT CO2, SO2, NOx FROM Tab_Brennstoff_Stamm WHERE ID = ?",
                    new OleDbParameter("@id", idBrennstoff));
                if (dt != null && dt.Rows.Count > 0)
                {
                    f.CO2 = D(dt.Rows[0], "CO2");
                    f.SO2 = D(dt.Rows[0], "SO2");
                    f.NOx = D(dt.Rows[0], "NOx");
                }
            }
            catch { }
            return f;
        }

        private static double? Erster(double? a, double? b, double? c)
        {
            if (a.HasValue && a.Value > 0) return a;
            if (b.HasValue && b.Value > 0) return b;
            if (c.HasValue && c.Value > 0) return c;
            return null;
        }

        private static double? D(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[spalte]); } catch { return null; }
        }
    }
}
