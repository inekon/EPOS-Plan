using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Wärmegestehungskosten „nur Wärmeerzeuger" (Anwenderentscheid 30.09.2026) an echten
    /// Projekten der Testdatenbank — dieselbe Kette wie <see cref="WirtschaftlichkeitAnkerTests"/>
    /// (gebuchter Stand → <c>KostenEmissionRechner</c> → <c>WirtschaftlichkeitCtrl.Berechne</c>).
    ///
    /// <para><b>Gemessen am 30.09.2026</b> (gebuchte Stände der Testdatenbank, ohne Stundenreihen):
    /// 1024 (Wärmepumpe, Heizstab, Elektrokessel, BHKW, 365 MWh Stromverbraucher) alt
    /// 0,49953 → neu 0,06162 €/kWh; 1030 (Kessel, zwei BHKW, Stromganglinie 4.790 MWh) alt
    /// 0,23979 → neu 0,00684 €/kWh — bis hierher stand der Kapitalwert des ganzen Projekts im
    /// Zähler, samt Haushaltsstrom. 1030 trägt einen gebuchten Stand von vor Befund B‑1 (der
    /// Kesselbrennstoff fehlt in den Modulzeilen, Warnung „Kesselbrennstoff fehlt"); die kleine
    /// Zahl ist deshalb eine Aussage über diesen Stand, nicht über das Projekt. Der Kapitalwert
    /// bewegt sich nicht (<see cref="WirtschaftlichkeitAnkerTests"/>).</para>
    ///
    /// <para><b>Entgangene § 9b-Entlastung</b> (Anwenderentscheid 02.10.2026): Kein Projekt der
    /// Testdatenbank ist produzierendes Gewerbe (1024 ohne Angabe, 1030 „kein produzierendes
    /// Gewerbe") — die beiden Anker oben bleiben deshalb bitgleich. Die Fälle mit umgestellter
    /// Unternehmensart (auf der Arbeitskopie) halten den Abzug: 1024 alt 0,06162 → neu
    /// 0,06541 €/kWh, 1030 alt 0,00684 → neu 0,00825 €/kWh; der Kapitalwert bleibt der ohne
    /// Abzug.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WaermegestehungAnkerTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Reine Wärmepumpe ohne Stromverbraucher, Photovoltaik und Stromspeicher.</summary>
        private const int PROJEKT_WP = 1019;

        private static WirtschaftlichkeitErgebnis Rechne(int idProjekt, out VariantenDaten v,
                                                         out WirtschaftlichkeitParameter p,
                                                         Action<WirtschaftlichkeitParameter> anpassen = null)
        {
            p = new WirtschaftlichkeitCtrl().LadeParameter(idProjekt);
            anpassen?.Invoke(p);
            v = new VariantenDaten
            {
                IdProjekt = idProjekt,
                IstStamm = true,
                Projektname = "Wärmegestehung " + idProjekt,
                Ergebnis = new ErgebnisCtrl().Load(idProjekt)
            };
            KostenEmissionRechner.Berechne(v);
            var daten = new BerichtsDaten { IdStamm = idProjekt, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            return new WirtschaftlichkeitCtrl().Berechne(daten, p).FirstOrDefault(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.IdProjekt == idProjekt);
        }

        /// <summary>Die bisherige Formel: der Kapitalwert des GANZEN Projekts im Zähler.</summary>
        private static double Projektformel(WirtschaftlichkeitErgebnis e, VariantenDaten v, WirtschaftlichkeitParameter p)
        {
            double a = KapitalwertRechner.Annuitaet(p.Zinssatz / 100.0, p.Betrachtungszeitraum);
            return (-e.Kapitalwert.Value * a) / (v.Ergebnis.Energiebedarf.Waermebedarf_Gesamt * 1000.0);
        }

        /// <summary>
        /// Gehört alles der Wärmeerzeugung — keine Stromverbraucher, keine Stromanlage, keine
        /// Stromerlöse —, ist die neue Kennzahl die alte, bitgleich.
        /// </summary>
        [Fact]
        public void Ohne_Strom_ausser_dem_der_Waermeerzeuger_bleibt_die_Kennzahl_die_alte()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitErgebnis e = Rechne(PROJEKT_WP, out VariantenDaten v, out WirtschaftlichkeitParameter p);
            Assert.NotNull(e);
            Assert.True(e.Gestehungskosten.HasValue);
            Assert.Equal(Projektformel(e, v, p), e.Gestehungskosten.Value, 12);
            Assert.Equal(0.14067749151691059, e.Gestehungskosten.Value, 10);
        }

        /// <summary>
        /// 1024: Wärmepumpe, Heizstab, Elektrokessel und BHKW neben 365 MWh Stromverbrauchern. Der
        /// Haushaltsstrom fällt aus dem Zähler; der Strom der Wärmeerzeuger (96,02 MWh) steht zum
        /// Arbeitspreis darin, der im Projekt verbrauchte BHKW-Strom als Gutschrift.
        /// </summary>
        [Fact]
        public void Waermegestehungskosten_1024_ohne_Haushaltsstrom()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitErgebnis e = Rechne(1024, out VariantenDaten v, out WirtschaftlichkeitParameter p);
            Assert.NotNull(e);
            Assert.Equal(-2896359.13, e.Kapitalwert.Value, 2);                 // projektweit, unverändert
            Assert.Equal(0.4995274387410206, Projektformel(e, v, p), 10);      // die bisherige Zahl
            Assert.Equal(0.06161616494867317, e.Gestehungskosten.Value, 10);   // die neue Zahl

            Waermegestehung.Zerlegung z = e.GestehungZerlegung;
            Assert.NotNull(z);
            Assert.Equal(e.Gestehungskosten.Value, z.ZaehlerEurJahr / z.WaermeKwh, 12);
            Assert.Equal(34549.97, z.StromgutschriftJahr1, 2);                 // 73,91 MWh × 0,46746 €/kWh

            EnergieTraegerNachweis strom = e.EnergiekostenJeTraeger.Single(t => t.Netzstrom);
            Assert.Equal(96.02, strom.WaermeMengeMWh, 2);                       // 29,38 + 13,65 + 52,99
            Assert.Equal(461.02, strom.VerbrauchGesamtMWh, 2);                  // + 365 MWh Grundstrom
        }

        /// <summary>1030: Kessel und zwei BHKW neben einer Stromganglinie von 4.790 MWh.</summary>
        [Fact]
        public void Waermegestehungskosten_1030_ohne_Stromganglinie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitErgebnis e = Rechne(1030, out VariantenDaten v, out WirtschaftlichkeitParameter p);
            Assert.NotNull(e);
            Assert.Equal(-21895377.28, e.Kapitalwert.Value, 2);
            Assert.Equal(0.23978800641410303, Projektformel(e, v, p), 10);
            Assert.Equal(0.00684209053429058, e.Gestehungskosten.Value, 10);
            Assert.Equal(0.0, e.EnergiekostenJeTraeger.Single(t => t.Netzstrom).WaermeMengeMWh, 12);
        }

        /// <summary>
        /// <b>Die Stromsteuer zählt einmal</b> (Register EZ‑21, Befund 1 der Nachlese P646): 1030 mit
        /// den flachen Stundenreihen des Prüffalls B6 rechnet die Befreiung nach § 9 Abs. 1 Nr. 3
        /// StromStG — 432,3 MWh KWK-Eigenstrom × 20,50 €/MWh Regelsatz = 8.862,15 €/a. Im Modus
        /// ERLOES hebt die Reihe den Kapitalwert um ihren Barwert (8.862,15 € × RBF(3 %, 20) =
        /// 131.846,41 €), die Wärmegestehungskosten nicht: Die Stromgutschrift (432,3 MWh ×
        /// 0,25 €/kWh = 108.075 €/a) bewertet denselben Eigenstrom zum Arbeitspreis samt
        /// Stromsteuer. Gestehung in beiden Modi 0,0068421 €/kWh. Nach der Regel der Welle #642
        /// zählte die Reihe im Modus ERLOES zur Wärme — die Kennzahl lag um 8.862,15 € × RBF ×
        /// a(3 %, 20) ÷ 6.137.560 kWh = 8.862,15 ÷ 6.137.560 = 0,0014439 €/kWh tiefer (0,0053982).
        /// </summary>
        [Fact]
        public void Im_Modus_ERLOES_zaehlt_die_Stromsteuer_einmal()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitErgebnis ausweis = RechneMitStundenreihen(DbWerte.STROMST_BEFREIUNG_MODUS_AUSWEIS,
                                                                        out VariantenDaten v, out WirtschaftlichkeitParameter p);
            WirtschaftlichkeitErgebnis erloes = RechneMitStundenreihen(DbWerte.STROMST_BEFREIUNG_MODUS_ERLOES,
                                                                       out _, out _);
            Assert.NotNull(ausweis);
            Assert.NotNull(erloes);
            Assert.True(erloes.StromsteuerBefreiungAlsErloes);
            Assert.Equal(432.3 * 20.50, erloes.StromsteuerBefreiungJahr1, 2);          // 8.862,15 €/a
            Assert.Equal(432.3 * 1000.0 * 0.25, erloes.GestehungZerlegung.StromgutschriftJahr1, 2);

            // Der Kapitalwert bucht die Reihe (unverändert): + Barwert der flachen Reihe.
            double rbf = 1.0 / KapitalwertRechner.Annuitaet(p.Zinssatz / 100.0, p.Betrachtungszeitraum);
            Assert.Equal(erloes.StromsteuerBefreiungJahr1 * rbf,
                         erloes.Kapitalwert.Value - ausweis.Kapitalwert.Value, 2);     // 131.846,41 €

            // Die Wärmegestehung nicht: dieselbe Zahl in beiden Modi.
            Assert.Equal(ausweis.Gestehungskosten.Value, erloes.Gestehungskosten.Value, 12);
            Assert.Equal(0.0068420905342943495, erloes.Gestehungskosten.Value, 10);

            // Die Zahl nach der Regel der Welle #642 — die Befreiung ein zweites Mal als Erlös.
            double waermeKwh = v.Ergebnis.Energiebedarf.Waermebedarf_Gesamt * 1000.0;
            Assert.Equal(6137560.0, waermeKwh, 6);
            Assert.Equal(0.0053982, erloes.Gestehungskosten.Value - erloes.StromsteuerBefreiungJahr1 / waermeKwh, 7);
        }

        /// <summary>
        /// 1030 mit den flachen Stundenreihen des Prüffalls B6
        /// (<see cref="StromsteuerBefreiungModusTests.Stundenreihen"/>) und den beiden Haken des
        /// § 9 Abs. 1 Nr. 3 StromStG — der einzige Stand der Testdatenbank, an dem die Befreiung
        /// einen Betrag ergibt; der Modus kommt aus dem Parametersatz.
        /// </summary>
        private static WirtschaftlichkeitErgebnis RechneMitStundenreihen(string modus, out VariantenDaten v,
                                                                       out WirtschaftlichkeitParameter p)
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            p = ctrl.LadeParameter(1030);
            p.StromsteuerBefreiungModus = modus;
            p.HocheffizienzNachweis = true;
            p.RaeumlicherZusammenhang = true;
            ctrl.SpeichereParameter(p);
            v = new VariantenDaten
            {
                IdProjekt = 1030,
                IstStamm = true,
                Projektname = "Wärmegestehung ERLOES",
                Ergebnis = new ErgebnisCtrl().Load(1030),
                Zeitreihen = StromsteuerBefreiungModusTests.Stundenreihen()
            };
            KostenEmissionRechner.Berechne(v);
            var daten = new BerichtsDaten { IdStamm = 1030, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            return new WirtschaftlichkeitCtrl().Berechne(daten, p).FirstOrDefault(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.IdProjekt == 1030);
        }

        /// <summary>Stellt die Unternehmensart eines Projekts der Arbeitskopie um.</summary>
        private static void Unternehmensart(int idProjekt, string art)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_ProjektWirtschaftlichkeit SET Unternehmensart = ? WHERE ID_Projekt = ?",
                new DbParam("@a", art), new DbParam("@p", idProjekt));
        }

        /// <summary>
        /// 1024 als Unternehmen des produzierenden Gewerbes (Anwenderentscheid 02.10.2026): Die
        /// Entlastung nach § 9b StromStG, die dem ersetzten Netzbezug ohnehin zustünde, mindert die
        /// Stromgutschrift — 73,91 MWh × 20,00 €/MWh = 1.478,20 €/a, als Annuität eines festen Satzes
        /// genau dieser Betrag. Wärmegestehungskosten alt 0,06162 → neu 0,06541 €/kWh. Kapitalwert,
        /// Barwerte, Energiekosten und die § 9b-Entlastung des Projekts sind die vor dem Abzug.
        /// </summary>
        [Fact]
        public void Waermegestehungskosten_1024_produzierendes_Gewerbe_ohne_entgangene_9b_Entlastung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Unternehmensart(1024, DbWerte.UNTERNEHMENSART_PROD_GEWERBE);
            WirtschaftlichkeitErgebnis e = Rechne(1024, out VariantenDaten v, out _);
            Assert.NotNull(e);
            Assert.True(e.ProduzierendesGewerbe);
            // projektweit, vom Abzug unberührt (gemessen ohne ihn)
            Assert.Equal(-2784891.14, e.Kapitalwert.Value, 2);
            Assert.Equal(2884358.13, e.BarwertAusgaben.Value, 2);
            Assert.Equal(111467.99, e.BarwertEinnahmen.Value, 2);
            Assert.Equal(188167.18, e.EnergiekostenJahr.Value, 2);
            Assert.Equal(7492.40, e.StromsteuerEntlastungJahr1, 2);

            Assert.Equal(0.06540904720048853, e.Gestehungskosten.Value, 10);   // alt 0,06161616494867317
            double waermeKwh = v.Ergebnis.Energiebedarf.Waermebedarf_Gesamt * 1000.0;
            Assert.Equal(0.06161616494867317 + 73.91 * 20.0 / waermeKwh, e.Gestehungskosten.Value, 12);

            Waermegestehung.Zerlegung z = e.GestehungZerlegung;
            Assert.Equal(34549.97, z.StromgutschriftJahr1, 2);
            Assert.Equal(1478.20, z.Entgangene9bJahr1, 2);
            Assert.Equal(e.Gestehungskosten.Value, z.ZaehlerEurJahr / z.WaermeKwh, 12);
        }

        /// <summary>
        /// 1030 als Unternehmen des produzierenden Gewerbes: 432,3 MWh BHKW-Eigenstrom × 20,00 €/MWh
        /// = 8.646 €/a. Wärmegestehungskosten alt 0,00684 → neu 0,00825 €/kWh; Kapitalwert unberührt.
        /// </summary>
        [Fact]
        public void Waermegestehungskosten_1030_produzierendes_Gewerbe_ohne_entgangene_9b_Entlastung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Unternehmensart(1030, DbWerte.UNTERNEHMENSART_PROD_GEWERBE);
            WirtschaftlichkeitErgebnis e = Rechne(1030, out VariantenDaten v, out _);
            Assert.NotNull(e);
            Assert.Equal(-20602441.40, e.Kapitalwert.Value, 2);                  // gemessen ohne Abzug
            Assert.Equal(1352374.36, e.BarwertEinnahmen.Value, 2);
            Assert.Equal(0.008250793667131643, e.Gestehungskosten.Value, 10);   // alt 0,00684209053429058
            double waermeKwh = v.Ergebnis.Energiebedarf.Waermebedarf_Gesamt * 1000.0;
            Assert.Equal(0.00684209053429058 + 8646.0 / waermeKwh, e.Gestehungskosten.Value, 12);
            Assert.Equal(8646.0, e.GestehungZerlegung.Entgangene9bJahr1, 2);
        }

        /// <summary>
        /// Eine Position der Photovoltaik — Investition und Zuschuss — bewegt den Kapitalwert, nicht
        /// die Wärmegestehungskosten; dieselbe Investition als Wärmezentrale bewegt beide.
        /// </summary>
        [Fact]
        public void Photovoltaik_zaehlt_nicht_die_Waermezentrale_schon()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitErgebnis vorher = Rechne(PROJEKT_WP, out _, out WirtschaftlichkeitParameter p);
            double a = KapitalwertRechner.Annuitaet(p.Zinssatz / 100.0, p.Betrachtungszeitraum);

            Position(Waermegestehung.KOMPONENTE_PHOTOVOLTAIK, "KAPITALGEBUNDEN", 10000.0);
            Position(Waermegestehung.KOMPONENTE_PHOTOVOLTAIK, DbWerte.KOSTENART_ZUSCHUSS, 2000.0);
            WirtschaftlichkeitErgebnis pv = Rechne(PROJEKT_WP, out VariantenDaten v, out _);
            Assert.Equal(vorher.Kapitalwert.Value - 8000.0, pv.Kapitalwert.Value, 2);
            Assert.Equal(vorher.Gestehungskosten.Value, pv.Gestehungskosten.Value, 12);

            Position(Waermegestehung.KOMPONENTE_WAERMEZENTRALE, "KAPITALGEBUNDEN", 10000.0);
            WirtschaftlichkeitErgebnis zentrale = Rechne(PROJEKT_WP, out v, out _);
            Assert.Equal(pv.Kapitalwert.Value - 10000.0, zentrale.Kapitalwert.Value, 2);
            double waermeKwh = v.Ergebnis.Energiebedarf.Waermebedarf_Gesamt * 1000.0;
            Assert.Equal(vorher.Gestehungskosten.Value + 10000.0 * a / waermeKwh,
                         zentrale.Gestehungskosten.Value, 10);
        }

        /// <summary>Eine Investitionsposition ohne Ersatz und Restwert (Nutzungsdauer = Zeitraum).</summary>
        private static void Position(int komponente, string kostenart, double betrag)
        {
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_ProjektWerte (ProjektID, KomponentenID, KategorieID, EingegebenerWert, " +
                "Nutzungsdauer, Kostenart, Bemessung) VALUES (?, ?, 1, ?, 20, ?, 'BETRAG')",
                new DbParam("@p", PROJEKT_WP), new DbParam("@k", komponente),
                new DbParam("@w", betrag), new DbParam("@a", kostenart));
        }
    }
}
