using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Diagnose des Imports allein aus der Projektdatei an der Anwenderdatei</b> unter <c>Quellen/</c>: Dieselbe Datei
    /// einmal über den IFC-Weg und einmal allein aus ihrer Projektdatei gelesen — beheizte Fläche, beheiztes Volumen,
    /// Fensterfläche und Fläche gegen Erdreich stimmen je innerhalb 1 % überein. Die Anwenderdatei wird nur gelesen; der Test
    /// nennt keine Werte, Namen oder Bezeichnungen der Datei, nur die relativen Abweichungen. Fehlt die Datei (sie liegt
    /// nicht im Repositorium), wird übersprungen; in einem Worktree gilt der Ordner <c>Quellen/</c> des Hauptbaums.
    /// </summary>
    public sealed class SqprojQuelldateiImportDiagnoseTests : IDisposable
    {
        private const string STAMM = "Sportheim_1970_unsaniert";
        private const double TOLERANZ = 0.01;

        private readonly ITestOutputHelper _aus;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public SqprojQuelldateiImportDiagnoseTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _kultur.Dispose();

        /// <summary>Die Wurzel des Arbeitsbaums dieser Datei (der Ordner mit <c>WP-Plan.Kern.slnf</c>).</summary>
        private static string Wurzel([CallerFilePath] string eigeneDatei = null)
        {
            string o = Path.GetDirectoryName(eigeneDatei);
            while (o != null && !File.Exists(Path.Combine(o, "WP-Plan.Kern.slnf")))
                o = Path.GetDirectoryName(o);
            return o;
        }

        /// <summary>
        /// Der Hauptbaum zu einem Worktree: <c>.git</c> ist dort eine Datei <c>gitdir: &lt;haupt&gt;/.git/worktrees/&lt;name&gt;</c>;
        /// <c>null</c> im Hauptbaum selbst oder ohne lesbaren Verweis.
        /// </summary>
        private static string Hauptbaum(string wurzel)
        {
            string datei = wurzel == null ? null : Path.Combine(wurzel, ".git");
            if (datei == null || !File.Exists(datei)) return null;
            string zeile = File.ReadLines(datei).FirstOrDefault() ?? "";
            const string PRAEFIX = "gitdir:";
            if (!zeile.StartsWith(PRAEFIX, StringComparison.Ordinal)) return null;
            string gitdir = zeile.Substring(PRAEFIX.Length).Trim();
            int i = gitdir.Replace('\\', '/').LastIndexOf("/.git/worktrees/", StringComparison.Ordinal);
            return i < 0 ? null : gitdir.Substring(0, i);
        }

        /// <summary>Eine Anwenderdatei unter <c>Quellen/</c> dieses Baums, sonst des Hauptbaums; <c>null</c>, wenn sie fehlt.</summary>
        private static string Pfad(string datei)
        {
            string wurzel = Wurzel();
            foreach (string baum in new[] { wurzel, Hauptbaum(wurzel) })
            {
                if (baum == null) continue;
                string pfad = Path.Combine(baum, "Quellen", datei);
                if (File.Exists(pfad) && new FileInfo(pfad).Length >= 1024) return pfad;
            }
            return null;
        }

        private static GebaeudeImportAblauf Lesen(string pfad)
        {
            var a = new GebaeudeImportAblauf();
            using (var f = new FileStream(pfad, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                Assert.True(a.Lesen(f, pfad, GebaeudeImportProfil.FuerDatei(pfad)) > 0,
                            string.Join(" | ", a.Meldungen.Where(m => m.Stufe == SpeicherEngine.PruefStufe.Fehler)));
            return a;
        }

        /// <summary>Die vier Vergleichsgrößen des Gebäudes 0: beheizte Fläche, beheiztes Volumen, Fensterfläche des Satzes, Fläche gegen Erdreich.</summary>
        private static (double Flaeche, double Volumen, double Fenster, double Erdreich) Messen(GebaeudeImportAblauf a)
        {
            AbbildGebaeude g = a.Abbild.Gebaeude[0];
            List<AbbildRaum> warm = g.Raeume.Where(r => r.Beheizt).ToList();
            GebaeudeImportSatz satz = a.Zuordnen(0, null);
            double Feld(string f) => satz.Zeile(f)?.Wert ?? 0.0;
            // Erdreich: die Hüllfläche gegen Erdreich (Bodenplatten und Wände am Erdreich, brutto).
            double erdreich = g.Bauteile.Where(b => b.Randbedingung == Randbedingung.Erdreich).Sum(b => b.BruttoflaecheM2 ?? 0.0);
            return (warm.Sum(r => r.FlaecheM2 ?? 0.0), warm.Sum(r => r.VolumenM3 ?? 0.0), Feld(GebaeudeZielfelder.FENSTER_GESAMT), erdreich);
        }

        private static double Abweichung(double ifc, double sqproj)
            => Math.Abs(ifc) < 1e-9 ? (Math.Abs(sqproj) < 1e-9 ? 0.0 : double.PositiveInfinity) : (sqproj - ifc) / ifc;

        [Fact]
        public void Projektdatei_allein_trifft_den_IFC_Weg_derselben_Datei()
        {
            string sq = Pfad(STAMM + ".sqproj"), ifc = Pfad(STAMM + ".ifc");
            if (sq == null || ifc == null)
            {
                _aus.WriteLine("Anwenderdatei fehlt — übersprungen.");
                return;
            }
            GebaeudeImportAblauf ib = Lesen(ifc);
            var ia = Messen(ib);
            GebaeudeImportAblauf pa = Lesen(sq);
            Assert.True(pa.AlleinAusProjektdatei);
            var pm = Messen(pa);
            var abweichungen = new (string Groesse, double Wert)[]
            {
                ("beheizte Fläche", Abweichung(ia.Flaeche, pm.Flaeche)),
                ("beheiztes Volumen", Abweichung(ia.Volumen, pm.Volumen)),
                ("Fenster", Abweichung(ia.Fenster, pm.Fenster)),
                ("Erdreich", Abweichung(ia.Erdreich, pm.Erdreich)),
            };
            // Nur zur Auskunft (keine Abnahmegröße): die Grundfläche des Einzonenwegs; der IFC-Weg trägt die Decke über unbeheizt
            // zusätzlich als aus dem Körper abgeleitete Trenndecke (siehe Abweichungen_des_Einzonensatzes_sind_benannt).
            double Grund(GebaeudeImportAblauf x) => x.Zuordnen(0, null).Zeile(GebaeudeZielfelder.FLAECHE_GRUND)?.Wert ?? 0.0;
            _aus.WriteLine("Grundfläche des Einzonenwegs (Auskunft): "
                           + (100.0 * Abweichung(Grund(ib), Grund(pa))).ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture) + " %");
            foreach ((string groesse, double wert) in abweichungen)
                _aus.WriteLine(groesse + ": " + (100.0 * wert).ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture) + " %");
            Assert.All(abweichungen, x => Assert.True(Math.Abs(x.Wert) <= TOLERANZ,
                x.Groesse + ": " + (100.0 * x.Wert).ToString("0.00", CultureInfo.InvariantCulture) + " % > 1 %"));
        }

        private static string Prozent(double x) => (100.0 * x).ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture) + " %";

        /// <summary>
        /// <b>Die Abweichungen des Einzonensatzes sind benannt</b> (kein Lesefehler der Projektdatei):
        /// <list type="bullet">
        /// <item><b>Außenwand:</b> Die Bruttofläche beider Wege stimmt; der Projektdateiweg zieht jede Öffnung von ihrer Wand ab,
        /// ohne Rückfall auf die Nettofläche der Datei. Der Nettounterschied entsteht auf dem IFC-Weg, wo Öffnungen an Wänden
        /// hängen, die kleiner sind als ihre Öffnungen (Rückfall auf die Nettofläche der Datei).</item>
        /// <item><b>Grundfläche:</b> Der Projektdateiweg zählt die Decke über unbeheizt je beheiztem Raum einmal (Bruttomaß, höchstens
        /// das 1,5-fache der Raumfläche); der IFC-Weg trägt dieselbe Decke zusätzlich als aus dem Körper abgeleitete Trenndecke.</item>
        /// <item><b>U-Werte:</b> <c>UValue</c> jeder Hüllfläche ist das U ihres verknüpften Aufbaus, und dieses U folgt aus dessen
        /// Schichten samt Rsi/Rse der Datei — die Datei ist in sich stimmig; die IFC verweist auf andere Aufbauten (Datenstand).</item>
        /// </list>
        /// Nur relative Prüfungen; die Datei wird nur gelesen; ohne Datei übersprungen.
        /// </summary>
        [Fact]
        public void Abweichungen_des_Einzonensatzes_sind_benannt()
        {
            string sq = Pfad(STAMM + ".sqproj"), ifc = Pfad(STAMM + ".ifc");
            if (sq == null || ifc == null)
            {
                _aus.WriteLine("Anwenderdatei fehlt — übersprungen.");
                return;
            }
            GebaeudeImportAblauf ib = Lesen(ifc), pa = Lesen(sq);
            Huelleneinordnung ie = GebaeudeHuelleneinordnung.Einordnen(ib.Abbild, 0, r => r.Beheizt);
            Huelleneinordnung pe = GebaeudeHuelleneinordnung.Einordnen(pa.Abbild, 0, r => r.Beheizt);

            // 1. Außenwand: brutto gleich, netto auf dem Projektdateiweg Brutto − Öffnungen ohne Rückfall.
            static List<Huellposten> Wand(Huelleneinordnung e)
                => e.Huelle.Where(p => !p.Verworfen && p.Summenfeld == GebaeudeZielfelder.FLAECHE_AUSSENWAND).ToList();
            double iBrutto = Wand(ie).Sum(p => p.BruttoM2 ?? 0.0), pBrutto = Wand(pe).Sum(p => p.BruttoM2 ?? 0.0);
            double iNetto = Wand(ie).Sum(p => p.NettoM2 ?? 0.0), pNetto = Wand(pe).Sum(p => p.NettoM2 ?? 0.0);
            _aus.WriteLine("Außenwand brutto: " + Prozent(Abweichung(iBrutto, pBrutto)) + ", netto: " + Prozent(Abweichung(iNetto, pNetto))
                           + ", IFC-Wände mit Rückfall auf die Nettofläche der Datei: " + Wand(ie).Count(p => p.NettoRueckfall));
            Assert.True(Math.Abs(Abweichung(iBrutto, pBrutto)) <= TOLERANZ, "Außenwand brutto " + Prozent(Abweichung(iBrutto, pBrutto)));
            Assert.DoesNotContain(Wand(pe), p => p.NettoRueckfall || p.NettoNegativ);

            // 2. Grundfläche: Decken über unbeheizt je beheiztem Raum einmal gezählt.
            static double GroessterAnteil(GebaeudeImportAblauf a, Huelleneinordnung e)
            {
                var raeume = a.Abbild.Gebaeude[0].Raeume.Where(r => r.Beheizt && r.FlaecheM2 > 0.0).ToDictionary(r => r.Kennung, StringComparer.Ordinal);
                var je = new Dictionary<string, double>(StringComparer.Ordinal);
                foreach (Huellposten p in e.Huelle.Where(p => !p.Verworfen && p.Seite == Huellseite.Unbeheizt && p.Boden == true && p.HeizPos >= 0))
                {
                    string k = p.Bauteil.Nachbarn[p.HeizPos].Kennung;
                    if (raeume.ContainsKey(k)) je[k] = (je.TryGetValue(k, out double s) ? s : 0.0) + (p.BruttoM2 ?? 0.0);
                }
                return je.Count == 0 ? 0.0 : je.Max(x => x.Value / raeume[x.Key].FlaecheM2.Value);
            }
            double pAnteil = GroessterAnteil(pa, pe);
            double iKoerper = ie.Huelle.Where(p => !p.Verworfen && p.Summenfeld == GebaeudeZielfelder.FLAECHE_GRUND
                                                   && p.Bauteil.Trenndeckenherkunft == AbbildBauteil.TRENNDECKE_KOERPER).Sum(p => p.BruttoM2 ?? 0.0);
            double iGrund = ie.Huelle.Where(p => !p.Verworfen && p.Summenfeld == GebaeudeZielfelder.FLAECHE_GRUND).Sum(p => p.BruttoM2 ?? 0.0);
            _aus.WriteLine("Grund: größter Anteil Decke über unbeheizt je Raumfläche (Projektdatei) "
                           + pAnteil.ToString("0.00", CultureInfo.InvariantCulture)
                           + ", Anteil der aus dem Körper abgeleiteten Trenndecken an der IFC-Grundfläche " + Prozent(iKoerper / Math.Max(iGrund, 1e-9)));
            Assert.InRange(pAnteil, 0.5, 1.5);

            // 3. U-Werte: UValue = U des verknüpften Aufbaus = U aus dessen Schichten mit Rsi/Rse der Datei.
            SqprojAbbild datei = SqprojLeser.Lesen(sq);
            int geprueft = 0;
            foreach (SqprojHuellflaeche h in datei.Huellflaechen)
            {
                if (h.AufbauKennung == null || !datei.Aufbauten.TryGetValue(h.AufbauKennung, out SqprojAufbau a) || !a.HatSchichten) continue;
                double r = a.Schichten.Where(s => s.DickeM > 0.0 && s.LambdaWmK > 0.0).Sum(s => s.DickeM.Value / s.LambdaWmK.Value)
                           + (a.RsiM2KW ?? 0.0) + (a.RseM2KW ?? 0.0);
                Assert.True(Math.Abs(Abweichung(a.UWert.Value, 1.0 / r)) <= TOLERANZ, "Aufbau-U gegen Schichten " + Prozent(Abweichung(a.UWert.Value, 1.0 / r)));
                Assert.True(h.UWert.HasValue && Math.Abs(Abweichung(a.UWert.Value, h.UWert.Value)) <= TOLERANZ,
                            "UValue gegen Aufbau-U " + (h.UWert.HasValue ? Prozent(Abweichung(a.UWert.Value, h.UWert.Value)) : "leer"));
                geprueft++;
            }
            _aus.WriteLine("U-Werte: " + geprueft + " opake Hüllflächen stimmig mit ihrem Aufbau");
            Assert.True(geprueft > 0);
        }
    }
}
