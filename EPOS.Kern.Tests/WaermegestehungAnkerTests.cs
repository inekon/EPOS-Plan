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
    /// </summary>
    [Collection("Testdatenbank")]
    public class WaermegestehungAnkerTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Reine Wärmepumpe ohne Stromverbraucher, Photovoltaik und Stromspeicher.</summary>
        private const int PROJEKT_WP = 1019;

        private static WirtschaftlichkeitErgebnis Rechne(int idProjekt, out VariantenDaten v,
                                                         out WirtschaftlichkeitParameter p)
        {
            p = new WirtschaftlichkeitCtrl().LadeParameter(idProjekt);
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
