using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Verrechnung der Kennzahlgruppen „Emissionen" und „Kosten (einfach)"
    /// (Konzept Kap. 5; Phase 5) — Vorstufe zur Wirtschaftlichkeit.
    ///
    /// Grundlage: die mit Befund B1 eingeführte carrier_id an den Ergebnis-Modulen
    /// (Verbrauch je Erzeuger-Modul in MWh/a) plus das Preis-/Faktorgerüst
    /// energy_project_settings / energy_carrier / Abfrage_Energietraeger_Effektiv.
    ///
    /// Regeln:
    ///  - Fehlt für einen Träger MIT Verbrauch der Preis bzw. der CO₂-Faktor,
    ///    bleibt die betroffene Kennzahl null („—") — keine stillen Teilsummen.
    ///  - Emissionsfaktoren-Quelle (Vorgabe 11.08.2026): zuerst der PROJEKTWERT
    ///    (energy_project_settings.co2), sonst der KATALOG **Tab_Brennstoff_Stamm.CO2**
    ///    (über energy_carrier.id_brennstoff), zuletzt energy_carrier.co2.
    ///  - Einheit VERIFIZIERT (Kenndaten.accdb, 11.08.2026): die Faktoren stehen in
    ///    g/kWh (= kg/MWh) — Tab_Brennstoff_Stamm z. B. Erdgas 240, Heizöl 310,
    ///    Strom 560. t/a = MWh/a × Faktor / 1000.
    ///  - Netzbezug: Faktor des projektzugeordneten Strom-Trägers über dieselbe
    ///    Kette (Projektwert → Tab_Brennstoff_Stamm → energy_carrier); erst wenn
    ///    dort nichts gepflegt ist, greift STROMMIX_CO2_G_JE_KWH als Vorgabewert.
    ///  - CO2Brennstoff (BEHG-Basis, Phase 7/W2): nur ABGABEPFLICHTIGE Träger —
    ///    Brennstoff-Kategorien Gas/Öl/Koks/Kohle/Sonstige (Tab_BrennstoffKategorien),
    ///    ausgenommen „Biogas“. Näherung: Bio-Heizöl-Blends zählen voll als fossil,
    ///    unbekannte Träger gelten als pflichtig (konservativ); Quoten erst mit W3.
    ///  - LEITENTSCHEIDUNG L13: Zusätzlich werden die MENGEN biogener Träger geführt
    ///    (BiogenMengeMWh, BiogenBehgMengeMWh). Bewusst Mengen und keine Emissionen —
    ///    ob biogenes Verbrennungs-CO₂ angesetzt wird und ob der Nullansatz des § 8
    ///    EBeV 2030 zulässig ist, entscheidet die gewählte Konvention, nicht dieser
    ///    Rechner. Er bleibt dadurch unverändert in dem, was er bisher lieferte.
    /// </summary>
    public static class KostenEmissionRechner
    {
        /// <summary>CO₂-Faktor des Netzstroms [g/kWh] (deutscher Strommix, Vorgabewert).</summary>
        public const double STROMMIX_CO2_G_JE_KWH = 380.0;

        public static void Berechne(VariantenDaten v)
        {
            if (v == null || v.Ergebnis == null) return;
            try { BerechneIntern(v); }
            catch
            {
                v.Energiekosten = null; v.StromkostenNetz = null;
                v.CO2Gesamt = null; v.CO2Spezifisch = null; v.CO2Brennstoff = null;
                v.BiogenMengeMWh = 0; v.BiogenBehgMengeMWh = 0;
            }
        }

        private static void BerechneIntern(VariantenDaten v)
        {
            ErgebnisModel m = v.Ergebnis;

            // ---------------- Verbrauch je Energieträger einsammeln (MWh/a) ----------------
            var verbrauchJeTraeger = new Dictionary<int, double>();   // carrier_id -> MWh
            double verbrauchOhneTraeger = 0;                          // Module ohne carrier_id

            Action<int, double> add = (carrier, mwh) =>
            {
                if (mwh <= 0) return;
                if (carrier <= 0) { verbrauchOhneTraeger += mwh; return; }
                if (!verbrauchJeTraeger.ContainsKey(carrier)) verbrauchJeTraeger[carrier] = 0;
                verbrauchJeTraeger[carrier] += mwh;
            };

            if (m.BHKW != null && m.BHKW.Module != null)
                foreach (ErgebnisBHKWModulModel mo in m.BHKW.Module) add(mo.CarrierId, mo.Verbrauch);
            if (m.Heizkessel != null && m.Heizkessel.Module != null)
                foreach (ErgebnisHeizkesselModulModel mo in m.Heizkessel.Module) add(mo.CarrierId, mo.Verbrauch);

            // ---------------- Brennstoffe: Kosten + CO₂ ----------------
            double brennstoffKosten = 0, brennstoffCO2t = 0, behgCO2t = 0;
            double biogenMWh = 0, biogenBehgMWh = 0;                  // L13
            bool kostenVollstaendig = verbrauchOhneTraeger <= 0;
            bool co2Vollstaendig = verbrauchOhneTraeger <= 0;

            foreach (KeyValuePair<int, double> kv in verbrauchJeTraeger)
            {
                TraegerInfo info = LadeTraeger(v.IdProjekt, kv.Key);

                // L13: die MENGE biogener Träger — unabhängig davon, ob ein Faktor
                // gepflegt ist. Die Konventionsfrage entscheidet der Aufrufer.
                if (info.Biogen)
                {
                    biogenMWh += kv.Value;
                    if (info.BehgBiogen) biogenBehgMWh += kv.Value;
                }

                // Kosten: mengenbasiert (Heizwert vorhanden) oder Direktabrechnung je kWh.
                if (info.PreisArbeit.HasValue)
                {
                    double kosten;
                    if (info.EffHi.HasValue && info.EffHi.Value > 0)
                    {
                        double menge = kv.Value * 1000.0 / info.EffHi.Value;   // Abrechnungseinheit
                        kosten = menge * info.PreisArbeit.Value;
                    }
                    else
                        kosten = kv.Value * 1000.0 * info.PreisArbeit.Value;   // €/kWh direkt
                    if (info.Grundpreis.HasValue) kosten += info.Grundpreis.Value;   // je Träger einmal p. a.
                    brennstoffKosten += kosten;
                }
                else kostenVollstaendig = false;

                // CO₂ (g/kWh, s. Klassenkommentar).
                if (info.CO2.HasValue && info.CO2.Value > 0)
                {
                    double t = kv.Value * info.CO2.Value / 1000.0;
                    brennstoffCO2t += t;
                    if (info.BehgPflichtig) behgCO2t += t;   // BEHG-Basis (Phase 7/W2)
                }
                else
                    co2Vollstaendig = false;
            }

            // ---------------- Netzbezug Strom ----------------
            double netzbezugMWh = m.Energiebedarf != null ? m.Energiebedarf.Stromrestbedarf : 0;
            double? stromKosten = null;
            double stromCO2 = STROMMIX_CO2_G_JE_KWH;   // Vorgabewert, falls kein Träger gepflegt
            int stromCarrier = FindeStromTraeger(v.IdProjekt);
            if (stromCarrier > 0)
            {
                TraegerInfo strom = LadeTraeger(v.IdProjekt, stromCarrier);
                if (strom.PreisArbeit.HasValue)
                {
                    stromKosten = netzbezugMWh * 1000.0 * strom.PreisArbeit.Value;
                    if (strom.Grundpreis.HasValue) stromKosten += strom.Grundpreis.Value;
                }
                // Emissionsfaktor des Strom-Trägers (Projekt → Katalog Tab_Brennstoff_Stamm
                // → energy_carrier); Vorgabe 11.08.2026: Faktoren aus Projekt und DB.
                if (strom.CO2.HasValue && strom.CO2.Value > 0) stromCO2 = strom.CO2.Value;
            }
            double netzCO2t = netzbezugMWh * stromCO2 / 1000.0;

            // ---------------- Kennzahlen setzen ----------------
            v.BiogenMengeMWh = biogenMWh;             // L13 — reine Mengen, keine Wertung
            v.BiogenBehgMengeMWh = biogenBehgMWh;
            v.StromkostenNetz = stromKosten;
            v.Energiekosten = (kostenVollstaendig && stromKosten.HasValue)
                ? (double?)(brennstoffKosten + stromKosten.Value)
                : (kostenVollstaendig && verbrauchJeTraeger.Count > 0 && netzbezugMWh <= 0
                    ? (double?)brennstoffKosten : null);

            bool hatBrennstoff = verbrauchJeTraeger.Count > 0 || verbrauchOhneTraeger > 0;
            if (!hatBrennstoff)
            {
                v.CO2Gesamt = netzCO2t;                     // reine Strom-Systeme
                v.CO2Brennstoff = 0.0;
            }
            else if (co2Vollstaendig)
            {
                v.CO2Gesamt = brennstoffCO2t + netzCO2t;
                v.CO2Brennstoff = behgCO2t;                 // nur abgabepflichtige Träger
            }
            else
            {
                v.CO2Gesamt = null;
                v.CO2Brennstoff = null;
            }

            double waermeMWh = m.Energiebedarf != null ? m.Energiebedarf.Waermebedarf_Gesamt : 0;
            v.CO2Spezifisch = (v.CO2Gesamt.HasValue && waermeMWh > 0)
                ? (double?)(v.CO2Gesamt.Value * 1000.0 / waermeMWh)    // t/a → g/kWh Wärme
                : null;
        }

        // ------------------------------------------------------------- Träger-Daten

        private class TraegerInfo
        {
            public double? PreisArbeit;   // € je Abrechnungseinheit bzw. €/kWh (Direktabrechnung)
            public double? Grundpreis;    // €/a
            public double? CO2;           // g/kWh (verifiziert, s. Klassenkommentar)
            public double? EffHi;         // kWh je Abrechnungseinheit (null/0 = Direktabrechnung)
            public bool BehgPflichtig = true;   // fossiler Brennstoff (Phase 7/W2)

            /// <summary>L13 — biogener Träger (Holz, Pellets, Rapsöl, Tierische Fette, Biogas).</summary>
            public bool Biogen;

            /// <summary>L13 — biogener Träger, der zugleich BEHG-Brennstoff ist
            /// (flüssige Biomasse). Nur hier wirkt ein fehlender Nachhaltigkeitsnachweis.</summary>
            public bool BehgBiogen;
        }

        private static TraegerInfo LadeTraeger(int idProjekt, int carrierId)
        {
            var info = new TraegerInfo();
            try
            {
                DataTable eff = DataRepository.GetDataTable(
                    "SELECT eff_hi FROM Abfrage_Energietraeger_Effektiv WHERE ID_Projekt = ? AND carrier_id = ?",
                    new OleDbParameter("@p", idProjekt), new OleDbParameter("@c", carrierId));
                if (eff != null && eff.Rows.Count > 0 && eff.Rows[0][0] != DBNull.Value)
                    info.EffHi = Convert.ToDouble(eff.Rows[0][0]);
            }
            catch { }

            double? sPreis = null, sGrund = null, sCO2 = null;
            try
            {
                DataTable s = DataRepository.GetDataTable(
                    "SELECT custom_price_work, custom_price_base, co2 FROM energy_project_settings " +
                    "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                    new OleDbParameter("@p", idProjekt), new OleDbParameter("@c", carrierId));
                if (s != null && s.Rows.Count > 0)
                {
                    sPreis = W(s.Rows[0], "custom_price_work");
                    sGrund = W(s.Rows[0], "custom_price_base");
                    sCO2 = W(s.Rows[0], "co2");
                }
            }
            catch { }

            double? kPreis = null, kGrund = null, kCO2 = null;
            try
            {
                DataTable k = DataRepository.GetDataTable(
                    "SELECT price_work, price_base, co2 FROM energy_carrier WHERE id = ?",
                    new OleDbParameter("@c", carrierId));
                if (k != null && k.Rows.Count > 0)
                {
                    kPreis = W(k.Rows[0], "price_work");
                    kGrund = W(k.Rows[0], "price_base");
                    kCO2 = W(k.Rows[0], "co2");
                }
            }
            catch { }

            // Emissionsfaktor + BEHG-Einstufung aus dem Brennstoff-Katalog
            // (Tab_Brennstoff_Stamm über energy_carrier.id_brennstoff).
            double? bsCO2 = null;
            try
            {
                DataTable b = DataRepository.GetDataTable(
                    "SELECT bs.CO2, bs.ID_Kategorie, bs.Bezeichner FROM energy_carrier AS ec " +
                    "INNER JOIN Tab_Brennstoff_Stamm AS bs ON ec.id_brennstoff = bs.ID " +
                    "WHERE ec.id = ?",
                    new OleDbParameter("@c", carrierId));
                if (b != null && b.Rows.Count > 0)
                {
                    bsCO2 = W(b.Rows[0], "CO2");

                    // BEHG-pflichtig: Kategorien 1 Gas / 2 Öl / 3 Koks / 4 Kohle /
                    // 11 Sonstige (Tab_BrennstoffKategorien); Biogas ausgenommen.
                    // Holz/Pellets/Rapsöl/Tier. Fette/Strom/Fernwärme/Wasserstoff frei.
                    double? kat = W(b.Rows[0], "ID_Kategorie");
                    string bez = b.Rows[0]["Bezeichner"] != DBNull.Value
                                 ? b.Rows[0]["Bezeichner"].ToString() : "";
                    if (kat.HasValue)
                    {
                        int k2 = (int)kat.Value;
                        info.BehgPflichtig = (k2 == 1 || k2 == 2 || k2 == 3 || k2 == 4 || k2 == 11)
                                             && !bez.Trim().Equals("Biogas", StringComparison.OrdinalIgnoreCase);

                        // L13 — dieselbe Kategorieregel, EINE Stelle für beide Rechner
                        // (BilanzKonvention). Die Einstufung ist reine Auskunft; sie
                        // ändert an dieser Rechnung nichts.
                        info.Biogen = BilanzKonvention.IstBiogen(k2, bez);
                        info.BehgBiogen = info.Biogen && BilanzKonvention.IstBehgBiogen(k2);
                    }
                }
            }
            catch { }

            // Arbeitspreis: 0 zählt als NICHT GEPFLEGT (Befund D5, 18.08.2026).
            //
            // Ein Arbeitspreis von 0 kam bisher als gültiger Preis durch: Die Spalten
            // custom_price_work / price_work sind numerisch und selten NULL, W() liefert
            // deshalb 0.0 statt null. Folge: kostenVollstaendig blieb true, die
            // Energiekosten wurden zu 0,00 €/a und die Wirtschaftlichkeitsrechnung
            // speicherte einen Kapitalwert OHNE Fehlgrund — nachgewiesen an Projekt 1018
            // („Erdgas E", beide Preisspalten 0): Kapitalwert −80.464,51 € auf einer
            // Datenbasis ohne jeden Energiepreis, während Projekt 1024 korrekt
            // „Energiekosten nicht bestimmbar" meldete.
            //
            // Abgrenzung: Die Regel gilt nur für den ARBEITSPREIS und nur für Träger, die
            // ein verbrauchendes Modul überhaupt anfährt. Ein legitim kostenloser Träger
            // existiert in diesem Datenmodell nicht — energy_carrier führt ausschließlich
            // beschaffte Energie (pricing_model ANIMAL_FAT, ELECTRICITY, GASEOUS_FUEL,
            // HEAT, LIQUID_FUEL, SOLID_FUEL), jeweils mit Abrechnungseinheit und
            // Heizwert. Umweltwärme der Wärmepumpe und PV-Eigenstrom sind KEINE
            // Energieträger: In verbrauchJeTraeger landen nur BHKW- und Heizkesselmodule,
            // der Strombezug läuft separat über den Netzbezugspfad. Ein Arbeitspreis 0 ist
            // hier also immer „noch nicht erfasst", nie „kostenlos".
            //
            // Der GRUNDPREIS bleibt bewusst unangetastet: 0 €/a ist dort ein üblicher und
            // gültiger Vertragswert.
            //
            // Vorrangkette wie beim CO₂-Faktor unten: Projektwert → Katalogwert → null.
            info.PreisArbeit = (sPreis.HasValue && sPreis.Value > 0) ? sPreis
                             : ((kPreis.HasValue && kPreis.Value > 0) ? kPreis : null);
            info.Grundpreis = sGrund ?? kGrund;

            // Vorrang: Projektwert → Katalog Tab_Brennstoff_Stamm → energy_carrier.
            if (sCO2.HasValue && sCO2.Value > 0) info.CO2 = sCO2;
            else if (bsCO2.HasValue && bsCO2.Value > 0) info.CO2 = bsCO2;
            else info.CO2 = kCO2;
            return info;
        }

        // Dem Projekt zugeordneter Stromträger (pricing_model ELECTRICITY), 0 = keiner.
        private static int FindeStromTraeger(int idProjekt)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT TOP 1 ec.id FROM energy_project_settings AS s " +
                    "INNER JOIN energy_carrier AS ec ON s.[ID_Energieträger] = ec.id " +
                    "WHERE s.ID_Projekt = ? AND ec.pricing_model = 'ELECTRICITY'",
                    new OleDbParameter("@p", idProjekt));
                if (o != null && o != DBNull.Value) return Convert.ToInt32(o);
            }
            catch { }
            return 0;
        }

        private static double? W(DataRow r, string col)
        {
            if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[col]); } catch { return null; }
        }
    }
}
