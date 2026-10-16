using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Stufe Einfach rechnet allein mit dem freien Paketteil</b> (Anwenderentscheid 14.10.2026).
    /// Der Paketteil führt Kaltwasser und Zirkulation der Bilanz (die Werte der INEKON-Vorlage
    /// TWW-Auslegung V4 und zwei INEKON-Setzungen); eine Datenbank, die ihn in einem früheren Stand
    /// trägt, bekommt diese Schlüssel beim ersten <see cref="ZapfprofilCtrl.Verfuegbar"/> nachgetragen
    /// (<see cref="TwwPaketteilCtrl.ParameterNachtragen"/>, Liste <see cref="PaketteilNachfuehrung.PARAMETER"/>).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class TwwPaketteilParameterNachtragTests
    {
        /// <summary>Jeder nachgetragene Schlüssel steht in der Parameterdatei des Paketteils.</summary>
        [Fact]
        public void Jeder_nachgetragene_Schluessel_steht_im_Paketteil()
        {
            HashSet<string> paket = PaketSchluessel();
            Assert.NotEmpty(PaketteilNachfuehrung.PARAMETER);
            foreach (string k in PaketteilNachfuehrung.PARAMETER)
                Assert.True(paket.Contains(k), k + " fehlt in Tab_TwwParameter_STAMM.csv des freien Paketteils.");
            // Die Flächenformel Wohnen ist eine Normzahl und steht nie im freien Paketteil.
            Assert.DoesNotContain(paket, k => k.StartsWith("DIN18599.", StringComparison.Ordinal));
        }

        /// <summary>
        /// <b>Die Wache der Stufe Einfach:</b> Allein mit dem freien Paketteil (leere Datenbank, Nachladen
        /// über <see cref="ZapfprofilCtrl.Verfuegbar"/>) rechnet jede mitgelieferte Nutzungsart mit einer
        /// Bezugsart ohne Fläche — Personen, Wohneinheiten, Betten, Zimmer, Beschäftigte … — eine Zone mit
        /// Bezugsmenge 3, Bedarfsniveau mittel und den Projektvorgaben der DDL: keine Ablehnung, kein
        /// „Parameter fehlt" und ein Jahresbedarf über 0. Ausgenommen ist allein die Bezugsart Fläche: Sie
        /// braucht die Flächenformel Wohnen (Normzahl) und bleibt benannt vom externen Katalogpaket abhängig.
        /// </summary>
        [Fact]
        public void Jede_mitgelieferte_Nutzungsart_rechnet_in_der_Stufe_Einfach_allein_mit_dem_Paketteil()
        {
            using var db = new TwwTestdatenbank();
            Assert.True(ZapfprofilCtrl.Verfuegbar().Ja);
            IReadOnlyList<Nutzungsart> katalog = ZapfprofilCtrl.Katalog();
            Assert.Equal(DatenzeilenZahl("Tab_TwwNutzungsart_STAMM"), katalog.Count);
            ProjektStand projekt = ZapfprofilCtrl.ProjektVorgabe();

            int gerechnet = 0;
            foreach (Nutzungsart n in katalog)
            {
                if (n.Bezug == ZapfBezugsart.Flaeche) continue;
                var zone = new ZonenStand { Id = 1, Name = "Zone 1", IdNutzungsart = n.Id, Bezugsmenge = 3, Reihenfolge = 1 };
                ZapfprofilErgebnis e = ZapfprofilCtrl.Rechnen(1, new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { zone }, projekt),
                                                              3, ZapfprofilTestbau.We(3));
                Assert.True(e.Ablehnungen.Count == 0,
                            n.Name + ": " + string.Join(" | ", e.Ablehnungen.Select(a => a.Grund + " " + a.Klartext)));
                Assert.True(e.Kennzahlen.JahresbedarfZapfungKwh > 0, n.Name + ": Jahresbedarf 0");
                Assert.True(e.Kennzahlen.JahresverlustZirkulationKwh > 0, n.Name + ": Zirkulation 0");
                gerechnet++;
            }
            Assert.True(gerechnet >= 5, "nur " + gerechnet + " Nutzungsarten gerechnet");
        }

        /// <summary>
        /// <b>Eine Datenbank mit dem Paketteil in einem früheren Stand</b> (Katalogversion FREI-1 ohne die
        /// Kaltwasser- und Zirkulationsschlüssel) bekommt beim ersten <see cref="ZapfprofilCtrl.Verfuegbar"/>
        /// genau die fehlenden nachgetragen — <c>AUSLIEFERUNG</c>, <c>ReadOnly</c> 1, die Herkunft des Pakets,
        /// benannt im Laufprotokoll. Eine vorhandene Anwenderzeile desselben Schlüssels bleibt, wie sie ist;
        /// ein zweiter Aufruf ändert nichts.
        /// </summary>
        [Fact]
        public void Eine_Datenbank_im_frueheren_Stand_bekommt_die_fehlenden_Schluessel_und_behaelt_ihre_Zeilen()
        {
            using var db = new TwwTestdatenbank();
            Assert.True(ZapfprofilCtrl.Verfuegbar().Ja);
            // Der frühere Stand: dieselbe Katalogversion, aber ohne die nachgetragenen Schlüssel ...
            foreach (string k in PaketteilNachfuehrung.PARAMETER)
                DataRepository.ExecuteNonQuery("DELETE FROM Tab_TwwParameter_STAMM WHERE Schluessel = ?", new DbParam("?", k));
            // ... und eine Anwenderzeile für einen von ihnen.
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_TwwParameter_STAMM (Schluessel, Wert, Einheit, Quelle, Version, Herkunftsart, Status, ReadOnly, " +
                "Katalogversion) VALUES (?, 0.35, '-', 'Anwender', 'A', 'EIGENKONSTRUKTION', ?, 0, ?)",
                new DbParam("?", ZapfParameter.ZIRKULATION_ANTEIL), new DbParam("?", TwwSchema.STATUS_EIGEN),
                new DbParam("?", ZapfprofilCtrl.KATALOGVERSION_RUECKFALL));
            Assert.Equal(PaketteilNachfuehrung.PARAMETER.Count - 1, TwwPaketteilCtrl.FehlendeParameter().Count);
            long vorher = Zahl("SELECT COUNT(*) FROM Tab_TwwParameter_STAMM");

            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            ZapfVerfuegbarkeit v = ZapfprofilCtrl.Verfuegbar();

            Assert.True(v.Ja, v.Klartext);
            Assert.Equal("PAKETTEIL_PARAMETER_NACHGETRAGEN", v.Nachladen?.Kennung);
            Assert.Equal(ZapfprofilCtrl.KATALOGVERSION_RUECKFALL, v.Nachladen.Werte[0]);
            Assert.Equal(PaketteilNachfuehrung.PARAMETER.Count - 1, Convert.ToInt32(v.Nachladen.Werte[1], CultureInfo.InvariantCulture));
            Assert.Contains(v.Nachladen.Klartext, protokoll.Hinweise);
            Assert.Empty(protokoll.Warnungen);
            Assert.Empty(TwwPaketteilCtrl.FehlendeParameter());
            Assert.Equal(vorher + PaketteilNachfuehrung.PARAMETER.Count - 1, Zahl("SELECT COUNT(*) FROM Tab_TwwParameter_STAMM"));
            Assert.Equal(ZapfprofilCtrl.KATALOGVERSION_RUECKFALL, ZapfprofilCtrl.AktuelleKatalogversion());

            foreach (string k in PaketteilNachfuehrung.PARAMETER.Where(k => k != ZapfParameter.ZIRKULATION_ANTEIL))
                Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_TwwParameter_STAMM WHERE Schluessel = ? AND Status = ? AND ReadOnly = 1 " +
                                      "AND Katalogversion = ? AND Herkunftsart = 'EIGENKONSTRUKTION'",
                                      k, TwwSchema.STATUS_AUSLIEFERUNG, ZapfprofilCtrl.KATALOGVERSION_RUECKFALL));
            // Die Anwenderzeile bleibt: eine Zeile, Wert und Status unverändert.
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_TwwParameter_STAMM WHERE Schluessel = ?", ZapfParameter.ZIRKULATION_ANTEIL));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_TwwParameter_STAMM WHERE Schluessel = ? AND Wert = 0.35 AND Status = ? " +
                                  "AND ReadOnly = 0", ZapfParameter.ZIRKULATION_ANTEIL, TwwSchema.STATUS_EIGEN));
            Parametersatz ps = ZapfprofilCtrl.Parameter();
            Assert.Equal(10.0, ps.Wert(ZapfParameter.KALTWASSER_MITTEL));
            Assert.Equal(0.35, ps.Wert(ZapfParameter.ZIRKULATION_ANTEIL));

            // Wiederholbar: nichts mehr zu tun.
            ZapfVerfuegbarkeit zweit = ZapfprofilCtrl.Verfuegbar();
            Assert.True(zweit.Ja);
            Assert.Null(zweit.Nachladen);
            Assert.False(TwwPaketteilCtrl.ParameterNachtragen().Versucht);
            Assert.Equal(vorher + PaketteilNachfuehrung.PARAMETER.Count - 1, Zahl("SELECT COUNT(*) FROM Tab_TwwParameter_STAMM"));
        }

        /// <summary>
        /// <b>Ein Katalog unter eigener Katalogversion</b> bekommt die fehlenden Schlüssel in dieser Version —
        /// der, die der Parametersatz liest —, nicht unter FREI-1.
        /// </summary>
        [Fact]
        public void Nachgetragen_wird_in_die_Katalogversion_die_der_Parametersatz_liest()
        {
            using var db = new TwwTestdatenbank();
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_TwwParameter_STAMM (Schluessel, Wert, Einheit, Quelle, Version, Herkunftsart, Status, ReadOnly, " +
                "Katalogversion) VALUES (?, 11.0, '°C', 'Paket', 'P', 'FIKTIV', ?, 0, 'PAKET-7')",
                new DbParam("?", ZapfParameter.KALTWASSER_MITTEL), new DbParam("?", TwwSchema.STATUS_EIGEN));

            ZapfVerfuegbarkeit v = ZapfprofilCtrl.Verfuegbar();

            Assert.True(v.Ja, v.Klartext);
            Assert.Equal("PAKETTEIL_PARAMETER_NACHGETRAGEN", v.Nachladen?.Kennung);
            Assert.Equal("PAKET-7", v.Nachladen.Werte[0]);
            Assert.Equal("PAKET-7", ZapfprofilCtrl.AktuelleKatalogversion());
            Assert.Equal((long)PaketteilNachfuehrung.PARAMETER.Count,
                         Zahl("SELECT COUNT(*) FROM Tab_TwwParameter_STAMM WHERE Katalogversion = 'PAKET-7'"));
            Assert.Equal(11.0, ZapfprofilCtrl.Parameter().Wert(ZapfParameter.KALTWASSER_MITTEL));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_TwwParameter_STAMM WHERE Katalogversion <> 'PAKET-7'"));
        }

        /// <summary>
        /// <b>Die Testdatenbank bleibt unberührt:</b> Sie führt die Schlüssel als fiktive Zeilen ihres
        /// Testkatalogs (TEST-1); nichts fehlt, nichts wird nachgetragen.
        /// </summary>
        [Fact]
        public void Die_Testdatenbank_fuehrt_die_Schluessel_und_bleibt_unberuehrt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            long vorher = Zahl("SELECT COUNT(*) FROM Tab_TwwParameter_STAMM");
            Assert.Empty(TwwPaketteilCtrl.FehlendeParameter());

            ZapfVerfuegbarkeit v = ZapfprofilCtrl.Verfuegbar();

            Assert.True(v.Ja, v.Klartext);
            Assert.Null(v.Nachladen);
            Assert.Equal(vorher, Zahl("SELECT COUNT(*) FROM Tab_TwwParameter_STAMM"));
        }

        // =================================================================================

        private static HashSet<string> PaketSchluessel()
        {
            string[] zeilen = File.ReadAllLines(Path.Combine(Wurzel(), "Referenzlaeufe", "Katalogpaket_frei", "Tab_TwwParameter_STAMM.csv"));
            return new HashSet<string>(zeilen.Skip(1).Where(z => z.Trim().Length > 0).Select(z => z.Split(';')[0]), StringComparer.Ordinal);
        }

        private static int DatenzeilenZahl(string tabelle)
            => File.ReadAllLines(Path.Combine(Wurzel(), "Referenzlaeufe", "Katalogpaket_frei", tabelle + ".csv"))
                   .Skip(1).Count(z => z.Trim().Length > 0);

        private static long Zahl(string sql, params string[] werte)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, werte.Select(w => new DbParam("?", w)).ToArray()),
                               CultureInfo.InvariantCulture);

        private static string Wurzel([CallerFilePath] string eigeneDatei = "")
        {
            string ordner = Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
        }
    }
}
