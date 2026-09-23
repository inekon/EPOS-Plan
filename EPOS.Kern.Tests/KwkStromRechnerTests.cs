using System;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E7c — <see cref="KwkStromRechner"/>, der zweite Fall des § 2 Nr. 16 KWKG
    /// als reine Funktion (Befund K‑1, Entscheid E7‑Q2 vom 23.09.2026), ohne Datenbank.
    ///
    /// <para>Die Zahlen sind die der Testdatenbank, Projekt 1030, Anlage „BHKW EW M 50 S"
    /// (P_el 50 kW, P_th 81 kW; gebuchter Lauf: Wärme 605,52 MWh, Strom 373,78 MWh) —
    /// damit die Proben hier dieselben sind, die <see cref="KwkgFall2Tests"/> am ganzen
    /// Rechenweg misst.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KwkStromRechnerTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        public void Dispose() => _kultur.Dispose();

        // =====================================================================
        //  (2) Die Stromkennzahl: gepflegt, berechnet, sonst keine
        // =====================================================================

        [Fact]
        public void Der_Vorschlag_ist_Pel_durch_Pth_und_sonst_keiner()
        {
            Assert.Equal(50.0 / 81.0, KwkStromRechner.Vorschlag(50, 81).Value, 12);
            Assert.Null(KwkStromRechner.Vorschlag(50, null));
            Assert.Null(KwkStromRechner.Vorschlag(null, 81));
            Assert.Null(KwkStromRechner.Vorschlag(50, 0));
            Assert.Null(KwkStromRechner.Vorschlag(0, 81));
            Assert.Null(KwkStromRechner.Vorschlag(-5, 81));
        }

        [Fact]
        public void Die_gepflegte_Kennzahl_gilt_vor_dem_Vorschlag()
        {
            KwkStromkennzahl k = KwkStromRechner.Stromkennzahl(0.5, 50, 81, DE);

            Assert.True(k.Bestimmbar);
            Assert.Equal(0.5, k.Wert.Value, 12);
            Assert.Equal(KwkStromRechner.HERKUNFT_GEPFLEGT, k.Herkunft);
            Assert.Equal(50.0 / 81.0, k.VorschlagWert.Value, 12);   // der Dialog zeigt beide
            Assert.Equal("gepflegt", k.Herleitung);
        }

        [Fact]
        public void Leer_heisst_berechnet_aus_Pel_durch_Pth_der_Geraetezeile()
        {
            KwkStromkennzahl k = KwkStromRechner.Stromkennzahl(null, 50, 81, DE);

            Assert.Equal(50.0 / 81.0, k.Wert.Value, 12);
            Assert.Equal(KwkStromRechner.HERKUNFT_BERECHNET, k.Herkunft);
            Assert.Equal("berechnet aus P_el ÷ P_th der Gerätezeile = 50,0 kW ÷ 81,0 kW", k.Herleitung);

            // 0 ist im Dialog „kein eigener Wert" — auch hier gilt dann der Vorschlag.
            Assert.Equal(KwkStromRechner.HERKUNFT_BERECHNET,
                         KwkStromRechner.Stromkennzahl(0, 50, 81, DE).Herkunft);
        }

        /// <summary>
        /// <b>Die Auflage zu E7‑Q2 (2):</b> Ohne gepflegte Kennzahl und ohne P_el ÷ P_th
        /// gibt es KEINEN Ersatzwert und keine willkürliche Vorgabe.
        /// </summary>
        [Fact]
        public void Ohne_Kennzahl_und_ohne_Pth_gibt_es_keinen_Ersatzwert()
        {
            KwkStromkennzahl k = KwkStromRechner.Stromkennzahl(null, 50, null, DE);

            Assert.False(k.Bestimmbar);
            Assert.Null(k.Wert);
            Assert.Null(k.VorschlagWert);
            Assert.Equal(KwkStromRechner.HERKUNFT_FEHLT, k.Herkunft);
            Assert.Equal("weder gepflegt noch aus P_el ÷ P_th der Gerätezeile bestimmbar", k.Herleitung);
        }

        // =====================================================================
        //  (1) Nutzwärme: die Wärme bleibt je Modul, nur der Überschuss nach P_el
        // =====================================================================

        [Fact]
        public void Der_Waermeueberschuss_wird_nur_nach_Pel_verteilt()
        {
            // 100 MWh Überschuss, Anlagen 50 und 9 kW.
            Assert.Equal(100.0 * 50.0 / 59.0, KwkStromRechner.UeberschussAnteil(100, 50, 59), 12);
            Assert.Equal(100.0 * 9.0 / 59.0, KwkStromRechner.UeberschussAnteil(100, 9, 59), 12);
            Assert.Equal(0.0, KwkStromRechner.UeberschussAnteil(0, 50, 59));
            Assert.Equal(0.0, KwkStromRechner.UeberschussAnteil(100, 50, 0));

            Assert.Equal(605.52 - 100.0 * 50.0 / 59.0,
                         KwkStromRechner.Nutzwaerme(605.52, KwkStromRechner.UeberschussAnteil(100, 50, 59)), 9);
            // Eine negative Wärme gibt es nicht.
            Assert.Equal(0.0, KwkStromRechner.Nutzwaerme(10, 20));
        }

        // =====================================================================
        //  Fall 2: min(Netto, Nutzwärme × σ)
        // =====================================================================

        [Fact]
        public void Fall_2_ist_das_Minimum_aus_Netto_und_Nutzwaerme_mal_Sigma()
        {
            KwkStromFall2 f = KwkStromRechner.Fall2(373.78, 605.52, 0,
                                                    KwkStromRechner.Stromkennzahl(0.5, 50, 81, DE));
            Assert.Equal(302.76, f.KwkStromMWh, 9);
            Assert.Equal(373.78 - 302.76, f.KuerzungMWh, 9);
            Assert.Equal(605.52, f.NutzwaermeMWh, 9);

            // Liegt Nutzwärme × σ über der Nettostromerzeugung, bleibt es bei der Nettomenge.
            KwkStromFall2 g = KwkStromRechner.Fall2(373.78, 605.52, 0,
                                                    KwkStromRechner.Stromkennzahl(0.9, 50, 81, DE));
            Assert.Equal(373.78, g.KwkStromMWh, 9);
            Assert.Equal(0.0, g.KuerzungMWh, 9);
        }

        [Fact]
        public void Ohne_Kennzahl_ist_der_KWK_Strom_null_und_die_ganze_Menge_gekuerzt()
        {
            KwkStromFall2 f = KwkStromRechner.Fall2(373.78, 605.52, 0,
                                                    KwkStromRechner.Stromkennzahl(null, 50, null, DE));
            Assert.Equal(0.0, f.KwkStromMWh);
            Assert.Equal(373.78, f.KuerzungMWh, 9);
        }

        // =====================================================================
        //  (3) Die Kürzung geht zuerst von der Einspeisung ab
        // =====================================================================

        [Fact]
        public void Die_Kuerzung_geht_zuerst_von_der_Einspeisung_ab()
        {
            double eigen = 227.0, einsp = 146.0;
            KwkStromRechner.Kuerzen(71.0, ref eigen, ref einsp);
            Assert.Equal(227.0, eigen, 12);
            Assert.Equal(75.0, einsp, 12);

            // Reicht die Einspeisung nicht, trägt der Eigenverbrauch den Rest.
            eigen = 227.0; einsp = 146.0;
            KwkStromRechner.Kuerzen(252.0, ref eigen, ref einsp);
            Assert.Equal(0.0, einsp, 12);
            Assert.Equal(121.0, eigen, 12);

            // Mehr als beides zusammen: beide 0, nie negativ.
            eigen = 227.0; einsp = 146.0;
            KwkStromRechner.Kuerzen(500.0, ref eigen, ref einsp);
            Assert.Equal(0.0, einsp);
            Assert.Equal(0.0, eigen);

            // Keine Kürzung: nichts ändert sich.
            eigen = 227.0; einsp = 146.0;
            KwkStromRechner.Kuerzen(0.0, ref eigen, ref einsp);
            Assert.Equal(227.0, eigen);
            Assert.Equal(146.0, einsp);
        }
    }
}
