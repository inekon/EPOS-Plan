using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kalenderschicht der Bedarfsprofile</b> (Entscheidungsvorlage Modellgrenzen PW2, BW2;
    /// Konzept Simulationsablauf 17; <see cref="Betriebskalenderschicht"/>, <see cref="Betriebskalender"/>).
    ///
    /// <para><b>Geprüft wird:</b> die Feiertage je Land, die Tagesarten (Ferien vor Feiertag, über den
    /// Jahreswechsel), die Schicht ohne Datenbank (Feiertag wie Sonntag, Ferienfaktor auf dem
    /// Tagesmittel, Monatsmenge erhalten bzw. gekürzt, ohne Wirkung bitgleich zur Kachelung), und auf
    /// einer Kopie der Testdatenbank Speichern/Lesen/Löschen, die Zuordnungswege und die Profilroutine
    /// für alle drei Profilarten samt Reihenfolgefestigkeit.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class BetriebskalenderTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =============================================================================
        //  Feiertage und Tagesarten
        // =============================================================================

        [Fact]
        public void Die_Feiertage_je_Land()
        {
            IReadOnlyList<int> de = Landesfeiertage.Jahrestage(null, 2025);
            Assert.Equal(9, de.Count);
            Assert.Contains(1, de);                                    // Neujahr
            Assert.Contains(Feiertage.Gemeinjahrestag(4, 18), de);     // Karfreitag 2025

            IReadOnlyList<int> by = Landesfeiertage.Jahrestage("BY", 2025);
            Assert.Equal(12, by.Count);
            Assert.Contains(6, by);                                    // Heilige Drei Könige
            Assert.Contains(Feiertage.Gemeinjahrestag(6, 19), by);     // Fronleichnam 2025
            Assert.Contains(Feiertage.Gemeinjahrestag(11, 1), by);     // Allerheiligen

            Assert.Contains(Feiertage.Gemeinjahrestag(11, 19), Landesfeiertage.Jahrestage("SN", 2025)); // Buß- und Bettag
            Assert.Contains(Feiertage.Gemeinjahrestag(10, 31), Landesfeiertage.Jahrestage("NI", 2025));
            Assert.Contains(Feiertage.Gemeinjahrestag(3, 8), Landesfeiertage.Jahrestage("BE", 2025));
            Assert.Contains(Feiertage.Gemeinjahrestag(8, 15), Landesfeiertage.Jahrestage("SL", 2025));
            Assert.Contains(Feiertage.Gemeinjahrestag(9, 20), Landesfeiertage.Jahrestage("TH", 2025));
            Assert.Equal(9, Landesfeiertage.Jahrestage("XX", 2025).Count);
            Assert.Equal(16, Landesfeiertage.BUNDESLAENDER.Distinct().Count());
        }

        [Fact]
        public void Die_Tagesarten_Ferien_vor_Feiertag()
        {
            var k = new Betriebskalender
            {
                Bezeichner = "Werk",
                Ferien = { new Ferienzeitraum(360, 2) },     // über den Jahreswechsel
                FeiertagWieSonntag = true
            };
            byte[] t = k.Tagesarten(2025);
            Assert.Equal(Betriebskalender.TAG_FERIEN, t[0]);            // Neujahr liegt in den Ferien
            Assert.Equal(Betriebskalender.TAG_FERIEN, t[1]);
            Assert.Equal(Betriebskalender.TAG_NORMAL, t[2]);
            Assert.Equal(Betriebskalender.TAG_FERIEN, t[364]);
            Assert.Equal(Betriebskalender.TAG_NORMAL, t[357]);   // 24. Dezember
            Assert.Equal(Betriebskalender.TAG_FEIERTAG, t[358]); // 25. Dezember
            Assert.Equal(Betriebskalender.TAG_FEIERTAG, t[Feiertage.Gemeinjahrestag(5, 1) - 1]);

            k.FeiertagWieSonntag = false;
            Assert.Equal(Betriebskalender.TAG_NORMAL, k.Tagesarten(2025)[Feiertage.Gemeinjahrestag(5, 1) - 1]);

            Assert.Null(k.Pruefen());
            Assert.NotNull(new Betriebskalender().Pruefen());
            Assert.NotNull(new Betriebskalender { Bezeichner = "x", Ferienfaktor = 1.2 }.Pruefen());
            Assert.NotNull(new Betriebskalender { Bezeichner = "x", Bundesland = "XX" }.Pruefen());
            Assert.NotNull(new Betriebskalender { Bezeichner = "x", Ferien = { new Ferienzeitraum(0, 3) } }.Pruefen());
        }

        // =============================================================================
        //  Die Schicht ohne Datenbank
        // =============================================================================

        /// <summary>Ein Wochenprofil: Werktage 8–17 Uhr 1, Samstag 0,5 tags, Sonntag 0,2 rund um die Uhr.</summary>
        private static double[] Woche()
        {
            var wo = new double[168];
            for (int d = 0; d < 5; d++)
                for (int s = 8; s < 18; s++) wo[d * 24 + s] = 1.0;
            for (int s = 8; s < 18; s++) wo[5 * 24 + s] = 0.5;
            for (int s = 0; s < 24; s++) wo[6 * 24 + s] = 0.2;
            return wo;
        }

        private static (int[] anfang, int[] ende) Monate()
        {
            var a = new int[12];
            var e = new int[12];
            int h = 0;
            for (int m = 0; m < 12; m++)
            {
                a[m] = h;
                h += Feiertage.TageJeMonat[m] * 24;
                e[m] = h - 1;
            }
            return (a, e);
        }

        private static double Monatssumme(double[] r, int[] a, int[] e, int m)
        {
            double s = 0;
            for (int h = a[m]; h <= e[m]; h++) s += r[h];
            return s;
        }

        [Fact]
        public void Ohne_Wirkung_bitgleich_zur_Kachelung()
        {
            (int[] a, int[] e) = Monate();
            double[] wo = Woche();
            double[] mon = Enumerable.Range(1, 12).Select(m => (double)m).ToArray();
            var alt = new double[8760];
            var neu = new double[8760];
            WPPlan.Core.BhkwPlan.StromWocheToJahr(wo, mon, alt, a, e, 2);
            Assert.True(Betriebskalenderschicht.WocheZuJahr(wo, mon, neu, a, e, 2, new byte[365], 0.0, false));
            for (int h = 0; h < 8760; h++)
                Assert.Equal(BitConverter.DoubleToInt64Bits(alt[h]), BitConverter.DoubleToInt64Bits(neu[h]));
        }

        [Fact]
        public void Feiertag_wie_Sonntag_und_Monatsmenge_erhalten()
        {
            (int[] a, int[] e) = Monate();
            double[] wo = Woche();
            double[] mon = Enumerable.Repeat(10.0, 12).ToArray();   // 10 MWh je Monat
            var k = new Betriebskalender { Bezeichner = "K", Bundesland = "BY" };
            byte[] arten = k.Tagesarten(2025);
            var r = new double[8760];
            // 1. Januar 2025 ist ein Mittwoch (Montag = 0).
            Assert.True(Betriebskalenderschicht.WocheZuJahr(wo, mon, r, a, e, 2, arten, 0.0, false));

            for (int m = 0; m < 12; m++)
                Assert.True(Math.Abs(Monatssumme(r, a, e, m) - 10000) < 1e-6, "Monat " + (m + 1));

            // Neujahr (Mittwoch) trägt den Sonntag: um 3 Uhr Bedarf, der Werktag hätte keinen.
            Assert.True(r[3] > 0);
            Assert.Equal(r[3], r[12], 12);                       // Sonntag rund um die Uhr gleich
            // 2. Januar (Donnerstag) ist ein Werktag: 3 Uhr ohne, 12 Uhr mit Bedarf.
            Assert.Equal(0.0, r[24 + 3]);
            Assert.True(r[24 + 12] > r[12]);
        }

        [Fact]
        public void Ferienfaktor_und_kuerzen()
        {
            (int[] a, int[] e) = Monate();
            double[] wo = Woche();
            double[] mon = Enumerable.Repeat(10.0, 12).ToArray();
            int von = Feiertage.Gemeinjahrestag(8, 1), bis = Feiertage.Gemeinjahrestag(8, 21);
            var k = new Betriebskalender { Bezeichner = "Sommer", FeiertagWieSonntag = false, Ferien = { new Ferienzeitraum(von, bis) } };
            byte[] arten = k.Tagesarten(2025);

            // f = 0, verteilt um: August behält seine Menge, die Ferientage sind leer.
            var umverteilt = new double[8760];
            Betriebskalenderschicht.WocheZuJahr(wo, mon, umverteilt, a, e, 2, arten, 0.0, false);
            Assert.True(Math.Abs(Monatssumme(umverteilt, a, e, 7) - 10000) < 1e-6);
            for (int h = (von - 1) * 24; h < bis * 24; h++) Assert.Equal(0.0, umverteilt[h]);

            // f = 0, gekürzt: August verliert den Anteil der Ferien; Juli bleibt.
            var gekuerzt = new double[8760];
            Betriebskalenderschicht.WocheZuJahr(wo, mon, gekuerzt, a, e, 2, arten, 0.0, true);
            double august = Monatssumme(gekuerzt, a, e, 7);
            Assert.True(august < 10000 * 0.5 && august > 0);
            Assert.True(Math.Abs(Monatssumme(gekuerzt, a, e, 6) - 10000) < 1e-6);
            // An den Betriebstagen gleich hoch wie ohne Ferien: gekürzt heißt nicht umverteilt.
            var ohne = new double[8760];
            WPPlan.Core.BhkwPlan.StromWocheToJahr(wo, mon, ohne, a, e, 2);
            int werktag = (bis + 3) * 24 + 12;
            Assert.Equal(ohne[werktag], gekuerzt[werktag], 9);

            // f = 0,5: eine Ferienstunde trägt die Hälfte des Tagesmittels (12 Uhr: (5·1 + 0,5 + 0,2)/7).
            var halb = new double[8760];
            Betriebskalenderschicht.WocheZuJahr(wo, mon, halb, a, e, 2, arten, 0.5, true);
            double mittel12 = (5 * 1.0 + 0.5 + 0.2) / 7;
            double mittel3 = 0.2 / 7;
            int ferientag = von * 24;   // 2. August
            Assert.Equal(mittel12 / mittel3, halb[ferientag + 12] / halb[ferientag + 3], 9);
            Assert.Equal(0.5 * mittel12, halb[ferientag + 12] / ohne[werktag], 9);
        }

        [Fact]
        public void Ein_Monat_ganz_in_Ferien_ohne_Kuerzen_rechnet_ohne_Ferien()
        {
            (int[] a, int[] e) = Monate();
            double[] wo = Woche();
            double[] mon = Enumerable.Repeat(10.0, 12).ToArray();
            var k = new Betriebskalender { Bezeichner = "Februar", FeiertagWieSonntag = false, Ferien = { new Ferienzeitraum(32, 59) } };
            var r = new double[8760];
            Assert.False(Betriebskalenderschicht.WocheZuJahr(wo, mon, r, a, e, 2, k.Tagesarten(2025), 0.0, false));
            Assert.True(Math.Abs(Monatssumme(r, a, e, 1) - 10000) < 1e-6);

            var g = new double[8760];
            Assert.True(Betriebskalenderschicht.WocheZuJahr(wo, mon, g, a, e, 2, k.Tagesarten(2025), 0.0, true));
            Assert.Equal(0.0, Monatssumme(g, a, e, 1));
        }

        // =============================================================================
        //  Auf der Testdatenbank
        // =============================================================================

        [Fact]
        public void Speichern_Lesen_Loeschen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var k = new Betriebskalender
            {
                Bezeichner = "Werk Süd", Bundesland = "BW", Ferienfaktor = 0.25, FerienKuerzen = true,
                Ferien = { new Ferienzeitraum(213, 233), new Ferienzeitraum(358, 3) }
            };
            int id = BetriebskalenderCtrl.Speichern(k);
            Assert.True(id > 0);
            Betriebskalender l = BetriebskalenderCtrl.Lies(id);
            Assert.Equal("Werk Süd", l.Bezeichner);
            Assert.Equal("BW", l.Bundesland);
            Assert.Equal(0.25, l.Ferienfaktor);
            Assert.True(l.FeiertagWieSonntag);
            Assert.True(l.FerienKuerzen);
            Assert.Equal(new[] { new Ferienzeitraum(213, 233), new Ferienzeitraum(358, 3) }, l.Ferien);

            l.Bezeichner = "Werk Nord";
            l.Bundesland = null;
            l.Ferien.RemoveAt(1);
            Assert.Equal(id, BetriebskalenderCtrl.Speichern(l));
            Betriebskalender l2 = BetriebskalenderCtrl.Lies(id);
            Assert.Equal("Werk Nord", l2.Bezeichner);
            Assert.Null(l2.Bundesland);
            Assert.Single(l2.Ferien);
            Assert.Contains(BetriebskalenderCtrl.Liste(), x => x.ID == id);

            Assert.Equal(0, BetriebskalenderCtrl.Speichern(new Betriebskalender()));   // ohne Namen
            Assert.True(BetriebskalenderCtrl.Loeschen(id));
            Assert.Null(BetriebskalenderCtrl.Lies(id));
        }

        /// <summary>
        /// Projekt 1041 trägt je eine Zuordnung Brauchwasser, Prozesswärme und Strom. Mit einem Kalender
        /// (Januar Ferien, f = 0, gekürzt) fällt der Januar aller drei Profilarten auf 0, die übrigen
        /// Monate bleiben; ohne Kürzen bleibt auch der Januar. Der Kalender reist über die
        /// Zuordnungswege (LiesProjekt, Add_*) mit.
        /// </summary>
        [Fact]
        public void Alle_drei_Profilarten_im_Projekt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            const int projekt = 1041;

            var quellen = new[]
            {
                ProfilQuelle.Brauchwasser(ProfilQuellmodus.Projektrechnung),
                ProfilQuelle.Prozesswaerme(ProfilQuellmodus.Projektrechnung),
                ProfilQuelle.Strom(ProfilQuellmodus.Projektrechnung)
            };
            (int[] a, int[] e) = Monate();
            var ohne = quellen.Select(q => Rechnen(q, projekt, null, a, e)).ToArray();
            foreach (double[] m in ohne.Select(x => x.monate)) Assert.True(m.Sum() > 0);

            int kalender = BetriebskalenderCtrl.Speichern(new Betriebskalender
            {
                Bezeichner = "Januarpause", FeiertagWieSonntag = true, FerienKuerzen = true,
                Ferien = { new Ferienzeitraum(1, 31) }
            });
            foreach (string z in BedarfNetzKalenderSchema.ZUORDNUNGEN)
                DataRepository.ExecuteNonQuery("UPDATE " + z + " SET ID_Betriebskalender = ? WHERE ID_Projekt = ?",
                                               new DbParam("?", kalender), new DbParam("?", projekt));

            for (int i = 0; i < quellen.Length; i++)
            {
                (double[] reihe, double[] monate) = Rechnen(quellen[i], projekt, null, a, e);
                Assert.Equal(0.0, monate[0]);
                // Die übrigen Monate behalten ihre Menge - Feiertage verteilen nur um.
                for (int m = 1; m < 12; m++)
                    Assert.True(Math.Abs(monate[m] - ohne[i].monate[m]) < 1e-9, "Profilart " + i + ", Monat " + (m + 1));
            }

            // Ohne Kürzen bleibt jede Monatsmenge (Feiertage und Ferien verteilen nur um).
            Betriebskalender k = BetriebskalenderCtrl.Lies(kalender);
            k.FerienKuerzen = false;
            BetriebskalenderCtrl.Speichern(k);
            for (int i = 0; i < quellen.Length; i++)
            {
                (double[] _, double[] monate) = Rechnen(quellen[i], projekt, null, a, e);
                for (int m = 0; m < 12; m++)
                    Assert.True(Math.Abs(monate[m] - ohne[i].monate[m]) < 1e-9, "Profilart " + i + ", Monat " + (m + 1));
            }

            // Die Zuordnungswege tragen den Kalender.
            Assert.All(Z_ProjektBrauchwasserCtrl.LiesProjekt(projekt), z => Assert.Equal(kalender, z.ID_Betriebskalender));
            Assert.All(Z_ProjektProzesswaermeCtrl.LiesProjekt(projekt), z => Assert.Equal(kalender, z.ID_Betriebskalender));
            Assert.All(Z_ProjektStromverbraucherCtrl.LiesProjekt(projekt), z => Assert.Equal(kalender, z.ID_Betriebskalender));
            var liste = Z_ProjektStromverbraucherCtrl.LiesProjekt(projekt);
            new WizardCtrl().Del_Projekt_Stromverbraucher(projekt, 0);
            Assert.True(new WizardCtrl().Add_Projekt_Stromverbraucher(projekt, liste));
            Assert.All(Z_ProjektStromverbraucherCtrl.LiesProjekt(projekt), z => Assert.Equal(kalender, z.ID_Betriebskalender));
        }

        /// <summary>
        /// Reihenfolgefest: Zwei Zuordnungen mit verschiedenen Kalendern ergeben dieselbe Summe, gleich in
        /// welcher Reihenfolge sie gerechnet werden — getauscht werden die Kalender der beiden
        /// Zuordnungszeilen (Projekt 1007, Strom; beide Zeilen zeigen auf dieselbe Kopie). Jede Zeile
        /// rechnet mit IHREM Kalender: Die Summe weicht von der ohne Kalender ab.
        /// </summary>
        [Fact]
        public void Reihenfolgefest()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            const int projekt = 1007;

            var zeilen = Z_ProjektStromverbraucherCtrl.LiesProjekt(projekt);
            Assert.Equal(2, zeilen.Count);
            int k1 = BetriebskalenderCtrl.Speichern(new Betriebskalender { Bezeichner = "A", Bundesland = "NW" });
            int k2 = BetriebskalenderCtrl.Speichern(new Betriebskalender
            {
                Bezeichner = "B", Ferienfaktor = 0.3, FerienKuerzen = true, Ferien = { new Ferienzeitraum(200, 220) }
            });
            (int[] a, int[] e) = Monate();
            ProfilQuelle q = ProfilQuelle.Strom(ProfilQuellmodus.Projektrechnung);
            (double[] ohne, _) = Rechnen(q, projekt, null, a, e);

            BetriebskalenderCtrl.ZuordnungKalenderSetzen("Z_Projekt_Stromverbraucher", zeilen[0].m_ID_Z, k1);
            BetriebskalenderCtrl.ZuordnungKalenderSetzen("Z_Projekt_Stromverbraucher", zeilen[1].m_ID_Z, k2);
            (double[] vor, double[] mvor) = Rechnen(q, projekt, null, a, e);

            BetriebskalenderCtrl.ZuordnungKalenderSetzen("Z_Projekt_Stromverbraucher", zeilen[0].m_ID_Z, k2);
            BetriebskalenderCtrl.ZuordnungKalenderSetzen("Z_Projekt_Stromverbraucher", zeilen[1].m_ID_Z, k1);
            (double[] rueck, double[] mrueck) = Rechnen(q, projekt, null, a, e);

            for (int h = 0; h < 8760; h++)
                Assert.Equal(BitConverter.DoubleToInt64Bits(vor[h]), BitConverter.DoubleToInt64Bits(rueck[h]));
            Assert.Equal(mvor, mrueck);
            Assert.True(ohne.Sum() - vor.Sum() > 1e-6, "Die Ferien von B kürzen den Juli.");
        }

        private static (double[] reihe, double[] monate) Rechnen(ProfilQuelle q, int projekt, List<string> namen, int[] a, int[] e)
        {
            var ziel = new double[8760];
            var monate = new double[12];
            ProfilBedarf.Rechnen(q, projekt, namen, 2, a, e, ziel, monate);
            return (ziel, monate);
        }
    }
}
