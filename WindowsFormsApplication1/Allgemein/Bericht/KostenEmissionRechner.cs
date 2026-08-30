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
    ///  - Emissionsfaktoren-Quelle: seit Etappe E5 EINE Kette für beide Rechner in
    ///    <see cref="EmissionsFaktorLader"/> — PROJEKTWERT (energy_project_settings.co2)
    ///    → aktive emissionswert-Zeile des Trägers → Tab_Brennstoff_Stamm.CO2 (über
    ///    energy_carrier.id_brennstoff) → energy_carrier.co2. Bis E4 fehlte die zweite
    ///    Stufe; die Reihenfolge der übrigen ist die Vorgabe vom 11.08.2026.
    ///  - BERECHNUNGSMODUS (Konzept F7): CO2Gesamt und CO2Spezifisch führen im Modus
    ///    CO2E das CO₂-Äquivalent der ausgewählten Arten statt des reinen CO₂ — der
    ///    Netzstrom-Anteil eingeschlossen. CO2Brennstoff (BEHG) bleibt in BEIDEN Modi
    ///    reines CO₂: Abgabepflichtig ist nach EBeV 2030 das Kohlendioxid, nicht sein
    ///    Äquivalent.
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
        /// <summary>
        /// CO₂-Faktor des Netzstroms [g/kWh] (deutscher Strommix, Vorgabewert). Er
        /// greift NUR, wenn dem Projekt kein Stromträger zugeordnet ist — sonst gilt
        /// der Faktor dieses Trägers.
        ///
        /// <para><b>435 statt bisher 380</b> (Nutzerentscheid 29.08.2026, Etappe E5):
        /// BAFA, Informationsblatt CO₂-Faktoren EEW, Zeile „El. Strom
        /// (Effizienzmaßnahme)" — 0,435 tCO₂/MWh. Der Wert ersetzt den alten
        /// Strommix-Vorgabewert und folgt damit demselben Beschluss wie die Saat der
        /// Stromträger aus Etappe E1 (<c>Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md</c>
        /// § 2.2/§ 3): Sonst rechnete dieselbe Anwendung je nach Datenlage mit 380
        /// oder 435.</para>
        /// </summary>
        public const double STROMMIX_CO2_G_JE_KWH = 435.0;

        public static void Berechne(VariantenDaten v)
        {
            if (v == null || v.Ergebnis == null) return;
            try { BerechneIntern(v); }
            catch
            {
                v.Energiekosten = null; v.StromkostenNetz = null;
                v.EnergieLeistungsanteil = null;
                v.CO2Gesamt = null; v.CO2Spezifisch = null; v.CO2Brennstoff = null;
                v.BiogenMengeMWh = 0; v.BiogenBehgMengeMWh = 0;
                v.EmissionsModus = DbWerte.EMISSION_MODUS_CO2;
                v.CO2StrommixRueckfall = false;
            }
        }

        private static void BerechneIntern(VariantenDaten v)
        {
            ErgebnisModel m = v.Ergebnis;

            // BERECHNUNGSMODUS (F7) - EINMAL je Lauf gelesen und am Ergebnis vermerkt.
            // Der Vermerk ist der Grund, weshalb ein Bericht die Zahl richtig
            // beschriften kann: Er nennt den Modus, in dem sie ENTSTANDEN ist, und
            // nicht den, der beim Drucken gerade eingestellt sein mag.
            string modus = EmissionenCtrl.ModusFuerRechenlauf(v.IdProjekt);
            v.EmissionsModus = modus;

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

            // F7: Die BEHG-Menge hat ihre EIGENE Vollständigkeit. Im Modus CO2 sind
            // beide Fahnen deckungsgleich (wirksamer Faktor = reines CO₂); im Modus
            // CO2E kann ein Träger ein Äquivalent führen, ohne dass sein reines CO₂
            // gepflegt wäre - dann ist die Kennzahl bestimmbar und die Abgabemenge
            // nicht. Eine gemeinsame Fahne machte aus dem einen Loch zwei.
            bool behgVollstaendig = verbrauchOhneTraeger <= 0;

            // KD4/FK6: Leistungsanteil der Gasträger; der Stromträger bleibt außen
            // vor (sein Leistungspreis ist die Tarifstruktur, Schritt 21).
            double leistungsAnteil = 0;
            bool leistungGepflegt = false;
            int stromCarrierId = FindeStromTraeger(v.IdProjekt);

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

                // Leistungspreis (Etappe KD4/FK6): Basis ist die VORGEHALTENE
                // Anschlussleistung aus den Gerätedaten (§ 7.1-Umsetzung); Modus
                // JAHR = Satz × kW, MONAT = Satz × kW × 12. Eine gepflegte
                // Saisonreihe (FK6a) gilt vor dem konstanten Satz: Summe der zwölf
                // Monatssätze × kW. Fehlt die Basis (keine plausible
                // Geräteleistung), entsteht bewusst KEIN Anteil — ein Fantasiewert
                // wäre schlimmer als ein fehlender.
                if ((info.ReihenSummeJeKW.HasValue || info.PreisLeistung.HasValue) &&
                    kv.Key != stromCarrierId)
                {
                    double kw = AnschlussleistungKW(v.IdProjekt, kv.Key);
                    if (kw > 0)
                    {
                        double anteil = info.ReihenSummeJeKW.HasValue
                            ? info.ReihenSummeJeKW.Value * kw
                            : (string.Equals(info.LeistungsModus,
                                   DbWerte.LEISTUNGSPREIS_MODUS_MONAT, StringComparison.Ordinal)
                                ? info.PreisLeistung.Value * kw * 12.0
                                : info.PreisLeistung.Value * kw);
                        brennstoffKosten += anteil;
                        leistungsAnteil += anteil;
                        leistungGepflegt = true;
                    }
                }

                // CO₂ (g/kWh, s. Klassenkommentar). Die ausgewiesene Kennzahl folgt dem
                // MODUS (F7), die BEHG-Basis bleibt reines CO₂.
                double? wirksam = info.Faktoren.Wirksam(modus);
                if (wirksam.HasValue && wirksam.Value > 0)
                    brennstoffCO2t += kv.Value * wirksam.Value / 1000.0;
                else
                    co2Vollstaendig = false;

                if (info.CO2.HasValue && info.CO2.Value > 0)
                {
                    if (info.BehgPflichtig)
                        behgCO2t += kv.Value * info.CO2.Value / 1000.0;   // BEHG-Basis (Phase 7/W2)
                }
                else
                    behgVollstaendig = false;
            }

            // ---------------- Netzbezug Strom ----------------
            double netzbezugMWh = m.Energiebedarf != null ? m.Energiebedarf.Stromrestbedarf : 0;
            double? stromKosten = null;
            double stromCO2 = STROMMIX_CO2_G_JE_KWH;   // Vorgabewert, falls kein Träger gepflegt
            int stromCarrier = stromCarrierId;   // bereits vor der Brennstoffschleife bestimmt (KD4)

            // BEFUND 30.08.2026: Der Vorgabewert greift STILL. Er wird jetzt festgehalten
            // (v.CO2StrommixRueckfall) - siehe Feldkommentar in VariantenDaten.
            bool strommixRueckfall = true;
            if (stromCarrier > 0)
            {
                TraegerInfo strom = LadeTraeger(v.IdProjekt, stromCarrier);
                if (strom.PreisArbeit.HasValue)
                {
                    stromKosten = netzbezugMWh * 1000.0 * strom.PreisArbeit.Value;
                    if (strom.Grundpreis.HasValue) stromKosten += strom.Grundpreis.Value;
                }
                // Emissionsfaktor des Strom-Trägers über dieselbe Kette wie die
                // Brennstoffe (EmissionsFaktorLader) und im selben MODUS (F7) — der
                // Netzstrom-Anteil gehört zu CO2Gesamt und darf keine andere Methode
                // führen als der Rest der Kennzahl.
                double? stromWirksam = strom.Faktoren.Wirksam(modus);
                if (stromWirksam.HasValue && stromWirksam.Value > 0)
                {
                    stromCO2 = stromWirksam.Value;
                    strommixRueckfall = false;
                }
            }
            // Ohne Netzbezug ändert der Vorgabewert nichts - dann ist er kein Rückfall,
            // sondern eine Zahl, die mit 0 MWh multipliziert wird.
            v.CO2StrommixRueckfall = strommixRueckfall && netzbezugMWh > 0;

            double netzCO2t = netzbezugMWh * stromCO2 / 1000.0;

            // ---------------- Kennzahlen setzen ----------------
            v.BiogenMengeMWh = biogenMWh;             // L13 — reine Mengen, keine Wertung
            v.BiogenBehgMengeMWh = biogenBehgMWh;
            v.StromkostenNetz = stromKosten;
            v.Energiekosten = (kostenVollstaendig && stromKosten.HasValue)
                ? (double?)(brennstoffKosten + stromKosten.Value)
                : (kostenVollstaendig && verbrauchJeTraeger.Count > 0 && netzbezugMWh <= 0
                    ? (double?)brennstoffKosten : null);

            // KD4/FK6: Leistungsanteil getrennt ausweisen (in Energiekosten enthalten).
            v.EnergieLeistungsanteil = leistungGepflegt ? (double?)leistungsAnteil : null;

            bool hatBrennstoff = verbrauchJeTraeger.Count > 0 || verbrauchOhneTraeger > 0;
            if (!hatBrennstoff)
            {
                v.CO2Gesamt = netzCO2t;                     // reine Strom-Systeme
                v.CO2Brennstoff = 0.0;
            }
            else
            {
                v.CO2Gesamt = co2Vollstaendig ? (double?)(brennstoffCO2t + netzCO2t) : null;
                v.CO2Brennstoff = behgVollstaendig ? (double?)behgCO2t : null;   // nur abgabepflichtige Träger
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

            /// <summary>Der Faktorsatz des Trägers aus der EINEN Lesekette
            /// (<see cref="EmissionsFaktorLader"/>, Etappe E5): reines CO₂,
            /// CO₂-Äquivalent nach F6 und die Luftschadstoffe.</summary>
            public EmissionsFaktorSatz Faktoren = new EmissionsFaktorSatz();

            /// <summary>Reines CO₂ [g/kWh] — die Größe der BEHG-Abgabemenge; NIE
            /// modusabhängig (Kurzzugriff auf <see cref="Faktoren"/>).</summary>
            public double? CO2 { get { return Faktoren.Co2GKwh; } }

            public double? EffHi;         // kWh je Abrechnungseinheit (null/0 = Direktabrechnung)
            public bool BehgPflichtig = true;   // fossiler Brennstoff (Phase 7/W2)

            /// <summary>L13 — biogener Träger (Holz, Pellets, Rapsöl, Tierische Fette, Biogas).</summary>
            public bool Biogen;

            /// <summary>L13 — biogener Träger, der zugleich BEHG-Brennstoff ist
            /// (flüssige Biomasse). Nur hier wirkt ein fehlender Nachhaltigkeitsnachweis.</summary>
            public bool BehgBiogen;

            /// <summary>Leistungspreis (Etappe KD4/FK6): Projektwert vor Katalogwert,
            /// 0 zählt wie beim Arbeitspreis als NICHT GEPFLEGT (Befund D5).
            /// Einheit je <see cref="LeistungsModus"/>: €/(kW·a) bzw. €/(kW·Monat).</summary>
            public double? PreisLeistung;

            /// <summary><c>energy_carrier.price_power_modus</c> — JAHR (Vorgabe) oder
            /// MONAT; der Modus ist Katalogsache je Träger (FK6), keine Projektgröße.</summary>
            public string LeistungsModus = DbWerte.LEISTUNGSPREIS_MODUS_JAHR;

            /// <summary>FK6a — Summe der 12 Monatssätze der saisonalen
            /// Leistungspreis-Reihe [€/(kW·a)-äquivalent]; null = keine Reihe
            /// gepflegt. Eine gepflegte Reihe gilt VOR dem konstanten Satz
            /// (§ 7.1); Projektreihe vor Stammreihe löst
            /// <see cref="PreisreiheCtrl.ReadTraegerReihe"/> auf.</summary>
            public double? ReihenSummeJeKW;
        }

        /// <summary>
        /// Vorgehaltene Anschlussleistung eines Trägers [kW] aus den GERÄTEDATEN der
        /// Projektanlagen (Etappe KD4, Konzept Kostendialoge § 7.1): BHKW
        /// (Pel + Ptherm) / η_gesamt, Kessel Ptherm / η. Bewusst Gerätedaten statt
        /// Simulationszeitreihe: Der Gas-Leistungspreis bepreist die VORGEHALTENE
        /// Leistung des Anschlusses, und Ergebnis-Zeitreihen werden hausregelkonform
        /// nicht persistiert. Wirkungsgrade werden nach dem Parser-Muster normiert
        /// (Wert &gt; 1,5 = Prozentangabe ÷ 100); außerhalb (0; 1,5] wird die Anlage
        /// übersprungen — Basis fehlt statt Fantasiewert.
        /// </summary>
        internal static double AnschlussleistungKW(int idProjekt, int carrierId)
        {
            double summe = 0;
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT b.Pel AS BhkwPel, b.Ptherm AS BhkwPth, b.Wirkungsgrad AS BhkwEta, " +
                    "h.Ptherm AS KesselPth, h.Wirkungsgrad_Gas AS KesselEtaGas, " +
                    "h.[Wirkungsgrad_Öl] AS KesselEtaOel " +
                    "FROM (Tab_Energieanlagen AS e LEFT JOIN Tab_BHKW AS b ON e.ID_BHKW = b.ID) " +
                    "LEFT JOIN Tab_Heizkessel AS h ON e.ID_Kessel = h.ID " +
                    "WHERE e.ID_Projekt = ? AND e.ID_Carrier = ?",
                    new OleDbParameter("@p", idProjekt), new OleDbParameter("@c", carrierId));
                if (dt == null) return 0;

                foreach (DataRow r in dt.Rows)
                {
                    double? bhkwPel = W(r, "BhkwPel"), bhkwPth = W(r, "BhkwPth");
                    if (bhkwPel.HasValue || bhkwPth.HasValue)
                    {
                        double eta = EtaNormiert(W(r, "BhkwEta"));
                        if (eta > 0)
                            summe += ((bhkwPel ?? 0) + (bhkwPth ?? 0)) / eta;
                        continue;
                    }

                    double? kesselPth = W(r, "KesselPth");
                    if (kesselPth.HasValue && kesselPth.Value > 0)
                    {
                        double etaGas = EtaNormiert(W(r, "KesselEtaGas"));
                        double etaOel = EtaNormiert(W(r, "KesselEtaOel"));
                        double eta = etaGas > 0 ? etaGas : etaOel;
                        if (eta > 0) summe += kesselPth.Value / eta;
                    }
                }
            }
            catch { }
            return summe;
        }

        /// <summary>Wirkungsgrad-Normierung (Parser-Muster): &gt; 1,5 gilt als
        /// Prozentangabe; außerhalb (0; 1,5] bleibt 0 („Basis fehlt").</summary>
        private static double EtaNormiert(double? eta)
        {
            if (!eta.HasValue || eta.Value <= 0) return 0;
            double e = eta.Value;
            if (e > 1.5) e /= 100.0;
            return (e > 0 && e <= 1.5) ? e : 0;
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

            // Emissionsfaktoren: EINE Kette für beide Rechner (Etappe E5).
            info.Faktoren = EmissionsFaktorLader.Lade(idProjekt, carrierId);

            double? sPreis = null, sGrund = null, sLeistung = null;
            try
            {
                DataTable s = DataRepository.GetDataTable(
                    "SELECT custom_price_work, custom_price_base, custom_price_power " +
                    "FROM energy_project_settings " +
                    "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                    new OleDbParameter("@p", idProjekt), new OleDbParameter("@c", carrierId));
                if (s != null && s.Rows.Count > 0)
                {
                    sPreis = W(s.Rows[0], "custom_price_work");
                    sGrund = W(s.Rows[0], "custom_price_base");
                    sLeistung = W(s.Rows[0], "custom_price_power");
                }
            }
            catch { }

            double? kPreis = null, kGrund = null, kLeistung = null;
            try
            {
                DataTable k = DataRepository.GetDataTable(
                    "SELECT price_work, price_base, price_power, price_power_modus " +
                    "FROM energy_carrier WHERE id = ?",
                    new OleDbParameter("@c", carrierId));
                if (k != null && k.Rows.Count > 0)
                {
                    kPreis = W(k.Rows[0], "price_work");
                    kGrund = W(k.Rows[0], "price_base");
                    kLeistung = W(k.Rows[0], "price_power");

                    object modus = k.Rows[0]["price_power_modus"];
                    if (modus != null && modus != DBNull.Value &&
                        string.Equals(Convert.ToString(modus),
                            DbWerte.LEISTUNGSPREIS_MODUS_MONAT, StringComparison.Ordinal))
                        info.LeistungsModus = DbWerte.LEISTUNGSPREIS_MODUS_MONAT;
                }
            }
            catch { }

            // Leistungspreis: Projektwert vor Katalogwert, 0 = nicht gepflegt
            // (dieselbe Regel wie beim Arbeitspreis, Befund D5).
            if (sLeistung.HasValue && sLeistung.Value > 0) info.PreisLeistung = sLeistung;
            else if (kLeistung.HasValue && kLeistung.Value > 0) info.PreisLeistung = kLeistung;

            // FK6a: saisonale Leistungspreis-Reihe (12 Monatssätze). Sie gilt vor dem
            // konstanten Satz; die Ebenen (Projekt vor Stamm) löst der Controller auf.
            try
            {
                PreisreiheCtrl prc = new PreisreiheCtrl();
                PreisreiheModel reihe = prc.ReadTraegerReihe(idProjekt, carrierId);
                if (reihe != null && string.Equals(reihe.Einheit,
                        DbWerte.PREISREIHE_EINHEIT_EUR_KW_MONAT, StringComparison.Ordinal))
                {
                    double[] werte = prc.ReadWerte(reihe.ID);
                    if (werte != null && werte.Length > 0)
                    {
                        double summe = 0;
                        foreach (double wert in werte) summe += wert;
                        if (summe > 0) info.ReihenSummeJeKW = summe;
                    }
                }
            }
            catch { }

            // BEHG-Einstufung und Biogen-Kennzeichen aus dem Brennstoff-Katalog
            // (Tab_Brennstoff_Stamm über energy_carrier.id_brennstoff). Der
            // EMISSIONSFAKTOR kommt seit Etappe E5 nicht mehr von hier, sondern aus
            // der einen Lesekette (EmissionsFaktorLader) - diese Abfrage klärt nur
            // noch die EINSTUFUNG des Trägers.
            try
            {
                DataTable b = DataRepository.GetDataTable(
                    "SELECT bs.ID_Kategorie, bs.Bezeichner FROM energy_carrier AS ec " +
                    "INNER JOIN Tab_Brennstoff_Stamm AS bs ON ec.id_brennstoff = bs.ID " +
                    "WHERE ec.id = ?",
                    new OleDbParameter("@c", carrierId));
                if (b != null && b.Rows.Count > 0)
                {
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
            // Vorrangkette wie beim CO₂-Faktor: Projektwert → Katalogwert → null.
            info.PreisArbeit = (sPreis.HasValue && sPreis.Value > 0) ? sPreis
                             : ((kPreis.HasValue && kPreis.Value > 0) ? kPreis : null);
            info.Grundpreis = sGrund ?? kGrund;
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

        // ================================================================= ETAPPE H2
        // Zwei schmale Zugänge für den Endenergie-Auflöser der Betriebskosten
        // (EndenergieAufloeser, Konzept_BHKW_Wirtschaftlichkeit § 4.5). Sie nutzen
        // DIESELBEN Bausteine wie die Kostenschleife oben (LadeTraeger,
        // FindeStromTraeger) — der Arbeitspreis bleibt damit EINE Wahrheit. Die in E3
        // gegen die Referenz gestellte Schleife selbst bleibt unangetastet
        // (Rechenweg-Disziplin); ihre Kostenformel „Verbrauch × 1000 / eff_hi ×
        // Preis" ist mit „Verbrauch × 1000 × ArbeitspreisJeKwh" algebraisch gleich.

        /// <summary>
        /// Arbeitspreis eines Trägers in €/kWh — bei Direktabrechnung der gepflegte
        /// Satz, sonst über den effektiven Heizwert (kWh je Abrechnungseinheit)
        /// umgerechnet; null = kein Preis gepflegt. Grund- und Leistungspreis gehören
        /// ausdrücklich NICHT dazu: Die anlagenscharfe Endenergie bemisst sich am
        /// Arbeitsanteil („Verbrauch des Moduls × Trägerpreis", Konzept § 4.5) —
        /// trägerweite Fixbeträge lassen sich keiner Anlage zurechnen.
        /// </summary>
        internal static double? ArbeitspreisJeKwh(int idProjekt, int carrierId)
        {
            if (carrierId <= 0) return null;
            TraegerInfo info = LadeTraeger(idProjekt, carrierId);
            if (!info.PreisArbeit.HasValue) return null;
            return (info.EffHi.HasValue && info.EffHi.Value > 0)
                ? info.PreisArbeit.Value / info.EffHi.Value
                : info.PreisArbeit.Value;
        }

        /// <summary><c>energy_carrier.id</c> des Stromträgers des Projekts
        /// (<c>pricing_model = 'ELECTRICITY'</c>); 0 = keiner gepflegt.</summary>
        internal static int StromTraegerId(int idProjekt)
        {
            return FindeStromTraeger(idProjekt);
        }

        private static double? W(DataRow r, string col)
        {
            if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[col]); } catch { return null; }
        }
    }
}
