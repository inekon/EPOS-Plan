using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using static EPOS.Kern.Tests.ZapfprofilTestbau;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Bausteine der Auslegungstests</b> (Stufe Z2): ein fiktiver Parametersatz mit den
    /// Schlüsseln der Bilanz (<see cref="ZapfprofilTestbau"/>) und der Auslegung
    /// (<see cref="ZapfAuslegungParameter"/>). Alle Werte sind ERFUNDEN und rund und mit Absicht
    /// von jeder Normzahl verschieden (Umsetzungskonzept Zapfprofilgenerator Kapitel 6 (a)).
    /// </summary>
    internal static class AuslegungTestbau
    {
        /// <summary>Die erfundenen Werte der Auslegung.</summary>
        internal static Dictionary<string, double> Werte() => new Dictionary<string, double>
        {
            [ZapfAuslegungParameter.KALTWASSER_AUSLEGUNG] = 12.0,
            [ZapfAuslegungParameter.W551_MINDESTTEMPERATUR] = 62.0,
            [ZapfAuslegungParameter.SPEICHERTEMPERATUR_VORGABE] = 56.0,
            [ZapfAuslegungParameter.LADUNGSFAKTOR] = 0.8,
            [ZapfAuslegungParameter.SENSORHOEHE] = 0.5,
            [ZapfAuslegungParameter.MISCHWASSERTEMPERATUR] = 44.0,
            [ZapfAuslegungParameter.VERZOEGERUNG] = 2.0,
            [ZapfAuslegungParameter.UEBERTRAGER_U_STAHL] = 400.0,
            [ZapfAuslegungParameter.UEBERTRAGER_U_EDELSTAHL] = 500.0,
            [ZapfAuslegungParameter.UEBERTRAGER_UEBERTEMPERATUR] = 20.0,
            [ZapfAuslegungParameter.UEBERTRAGERFLAECHE_KESSEL_STEIGUNG] = 0.02,
            [ZapfAuslegungParameter.UEBERTRAGERFLAECHE_KESSEL_ACHSABSCHNITT] = -1.0,
            [ZapfAuslegungParameter.UEBERTRAGERFLAECHE_WAERMEPUMPE_STEIGUNG] = 0.01,
            [ZapfAuslegungParameter.UEBERTRAGERFLAECHE_WAERMEPUMPE_ACHSABSCHNITT] = -0.5,
            [ZapfAuslegungParameter.ZEITKONSTANTE_KOEFFIZIENT] = 25.0,
            [ZapfAuslegungParameter.VEREINFACHUNG_GRENZE] = 5.0,
            [ZapfAuslegungParameter.VEREINFACHUNG_SENSORHOEHE] = 0.7,
            [ZapfAuslegungParameter.VEREINFACHUNG_SPEICHERTEMPERATUR] = 58.0,
            [ZapfAuslegungParameter.WERTEPAARE] = 5.0,
            [ZapfAuslegungParameter.DIN4708_A1] = 0.3,
            [ZapfAuslegungParameter.DIN4708_A2] = 3.0,
            [ZapfAuslegungParameter.DIN4708_Z] = 0.2,
            [ZapfAuslegungParameter.DIN4708_PB] = 4.0,
            [ZapfAuslegungParameter.DIN4708_WB_ZAPFSTELLE] = 6000.0,
            [ZapfAuslegungParameter.DIN4708_WB_BEDARF] = 5000.0,
            [ZapfAuslegungParameter.DIN4708_KAPPUNG] = 1.5,
            [ZapfAuslegungParameter.DIN4708_PROFIL_BLOECKE] = 2.0,
            ["DIN4708.Profil.Block.1.Beginn"] = 420.0,
            ["DIN4708.Profil.Block.1.Dauer"] = 10.0,
            ["DIN4708.Profil.Block.1.Anteil"] = 1.0,
            ["DIN4708.Profil.Block.2.Beginn"] = 1080.0,
            ["DIN4708.Profil.Block.2.Dauer"] = 60.0,
            ["DIN4708.Profil.Block.2.Anteil"] = 2.0,
            [ZapfAuslegungParameter.NUTZANTEIL] = 0.75,
            [ZapfAuslegungParameter.ZUSCHLAG] = 0.1,
            [ZapfAuslegungParameter.LADEFENSTER_LAENGE] = 10.0,
            [ZapfAuslegungParameter.LADEFENSTER_BEGINN] = 22.0,
            [ZapfAuslegungParameter.GLF_GUELTIGKEITSGRENZE] = 30.0,
            [ZapfAuslegungParameter.KLASSISCH_LITER] = 40.0,
            [ZapfAuslegungParameter.KLASSISCH_SPREIZUNG] = 45.0,
            [ZapfAuslegungParameter.KLASSISCH_WARNFAKTOR] = 2.5,
            [ZapfAuslegungParameter.NENNINHALT_RASTER] = 500.0,
            [ZapfAuslegungParameter.W551_GROSS_VOLUMEN] = 450.0,
            [ZapfAuslegungParameter.W551_GROSS_LEITUNG] = 4.0,
            [ZapfAuslegungParameter.W551_INHALT_JE_METER] = 0.2,
        };

        /// <summary>Bilanz- und Auslegungsparameter; einzelne Schlüssel lassen sich ersetzen oder weglassen.</summary>
        internal static Parametersatz Auslegungssatz(IDictionary<string, double> ersetzen = null, params string[] weglassen)
        {
            Dictionary<string, double> werte = Werte();
            if (ersetzen != null) foreach (var kv in ersetzen) werte[kv.Key] = kv.Value;
            foreach (string w in weglassen) werte.Remove(w);
            Parametersatz bilanz = ZapfprofilTestbau.Parameter(null, weglassen);
            Dictionary<string, double> alle = bilanz.Werte.ToDictionary(kv => kv.Key, kv => kv.Value.Wert);
            foreach (var kv in werte) alle[kv.Key] = kv.Value;
            return Parametersatz.Aus("TEST-Z2",
                alle.Select(kv => new ZapfParameterwert(kv.Key, kv.Value, "", Fiktiv)).ToArray());
        }

        /// <summary>Summenliniengrößen mit erfundenen Werten: Ladespeicher, keine Verluste, Erzeuger fest.</summary>
        internal static Summenlinienparameter Linie(double erzeugerKw = 30.0, double sensor = 0.5,
                                                         double verzoegerung = 2.0)
            => new Summenlinienparameter
            {
                KaltwasserAuslegungC = 12.0,
                SpeicherC = 62.0,
                Ladungsfaktor = 0.8,
                SensorhoeheAnteil = sensor,
                Speicherart = ZapfSpeicherart.Ladespeicher,
                VerzoegerungMin = verzoegerung,
                ErzeugerKw = erzeugerKw
            };

        /// <summary>κ = c_w · Δθ · f_l / 1000 [kWh/l] der Standardgrößen (Δθ 50 K, f_l 0,8).</summary>
        internal const double KAPPA = 1.163 * 50.0 * 0.8 / 1000.0;

        /// <summary>Ein Bedarfstag aus Ereignissen.</summary>
        internal static Bedarfstag Tag(params Zapfereignis[] e)
            => Bedarfstag.AusEreignissen(ZapfBedarfstagquelle.Konstruktor, "Testtag (fiktiv)", e, Fiktiv);

        /// <summary>
        /// Die erfundene Vorgabe der Nenninhaltsliste [l] für die Fälle mit einem Parametersatz im
        /// Speicher. Die Repo-Testdatenbank trägt stattdessen die Liste der Vorlage V4 aus dem freien
        /// Paketteil (N27) — Fälle auf ihr lesen sie mit <see cref="NenninhalteDerDatenbank"/>.
        /// </summary>
        internal static readonly double[] NENNINHALTE = { 120.0, 250.0, 400.0, 650.0, 900.0, 1400.0 };

        /// <summary>
        /// Die Vorgabe der Nenninhaltsliste [l], die die geöffnete Datenbank in der Katalogversion
        /// <paramref name="version"/> führt (<c>Speicherauslegung.Nenninhalt.Liste.{k}</c>, aufsteigend).
        /// </summary>
        internal static double[] NenninhalteDerDatenbank(string version = "TEST-1")
            => DataRepository.GetDataTable(
                    "SELECT Wert FROM Tab_TwwParameter_STAMM WHERE Schluessel LIKE ? AND Katalogversion = ?",
                    new DbParam("@s", ZapfAuslegungParameter.NENNINHALT_LISTE + "%"), new DbParam("@k", version))
                .Rows.Cast<System.Data.DataRow>()
                .Select(r => System.Convert.ToDouble(r["Wert"], System.Globalization.CultureInfo.InvariantCulture))
                .OrderBy(w => w).ToArray();

        /// <summary>
        /// Legt auf der ARBEITSKOPIE die Auslegungsparameter (<see cref="Werte"/>) samt Vorgabe der
        /// Nenninhaltsliste in der Katalogversion <paramref name="version"/> an, soweit sie fehlen —
        /// erfundene Werte. Die Repo-Testdatenbank trägt jeden dieser Schlüssel schon — die Setzungen
        /// der Speicherauslegung mit den Werten der Vorlage V4 (freier Paketteil, N27), die übrigen
        /// fiktiv aus <c>Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py</c> —; dann legt der Aufruf
        /// nichts an. Liefert die Zahl der neuen Zeilen.
        /// </summary>
        internal static int ParameterEinspielen(string version = "TEST-1")
        {
            Dictionary<string, double> werte = Werte();
            for (int k = 0; k < NENNINHALTE.Length; k++)
                werte[ZapfAuslegungParameter.NENNINHALT_LISTE + (k + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)] = NENNINHALTE[k];
            int neu = 0;
            foreach (KeyValuePair<string, double> kv in werte)
            {
                object da = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_TwwParameter_STAMM WHERE Schluessel = ? AND Katalogversion = ?",
                    new DbParam("@s", kv.Key), new DbParam("@k", version));
                if (System.Convert.ToInt64(da, System.Globalization.CultureInfo.InvariantCulture) > 0) continue;
                TwwTestdatenbank.ParameterAnlegen(kv.Key, kv.Value, version);
                neu++;
            }
            return neu;
        }
    }
}
