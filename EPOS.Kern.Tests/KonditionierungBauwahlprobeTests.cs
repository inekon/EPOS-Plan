using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.Referenzlaeufe.Skripte;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Bauwahlprobe des Referenzprojekts 1051</b> (Entwurf KP3, Festlegung 31; Teilkonzept
    /// Konditionierungsprofile 10.2; Befunde B25, B26): Welcher Katalogbau trägt die Aufheizrampe an kalten
    /// Tagen? Gerechnet wird mit dem <b>echten Kern</b> (Strahlungsanteil leer = 0,3) auf genau den
    /// Programmwegen des Saatskripts (<see cref="Konditionierungsprojekt1051"/>): Kopie von 1007 (Klima
    /// 1007001), der Kandidat als eigener Katalogbau mit „Büro" in allen fünf Größen, Ferien, Feiertagen,
    /// Heizperiode und Nachtauskühlung, zugeordnet mit seiner Fläche; Bemessung (b) mit 2 K, täglich,
    /// Reserve leer (wirksam 20 %).
    ///
    /// <para><b>Tauglich</b> ist ein Bau mit mindestens 10 Rampentagen, einer längsten Rampe ab 2 h,
    /// mindestens einem W2-Tag, erreichbarem Bemessungsfall und einer Spitze mit Rampe unter der ohne.
    /// Gewählt wird der erste taugliche Kandidat in der Reihenfolge <see cref="Konditionierungsprojekt1051.KANDIDATEN"/>;
    /// er muss <see cref="Konditionierungsprojekt1051.KATALOGBAU"/> sein. Die Gegenprobe (der Bau von 1007)
    /// ist nicht tauglich. Die Zahlen je Kandidat stehen in der Testausgabe (Bericht der Welle RP1).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    [Trait("Kategorie", "Probe")]
    public sealed class KonditionierungBauwahlprobeTests
    {
        private readonly ITestOutputHelper _aus;

        public KonditionierungBauwahlprobeTests(ITestOutputHelper aus) => _aus = aus;

        /// <summary>Das Ergebnis eines Kandidaten.</summary>
        internal sealed record Probe(string Bau, double FlaecheM2, int Rampentage, int LaengsteRampeH, int? AufheizzeitMaxH,
                                     int W1, int W2, int W3, bool Erreichbar, double PaufKW, double SpitzeMitKW,
                                     double SpitzeOhneKW, double HeizwaermeMitMWh, double HeizwaermeOhneMWh)
        {
            internal bool Tauglich => Rampentage >= 10 && LaengsteRampeH >= 2 && W2 >= 1 && Erreichbar && SpitzeMitKW < SpitzeOhneKW;

            public override string ToString() => string.Format(CultureInfo.InvariantCulture,
                "{0,-14} {1,7:0.0} m²  Rampentage {2,3}  längste Rampe {3,2} h  t_auf,max {4,2} h  W1 {5,3}  W2 {6,3}  W3 {7,3}  " +
                "erreichbar {8}  P_auf {9,7:0.00} kW  Spitze mit {10,7:0.00} / ohne {11,7:0.00} kW  " +
                "Heizwärme mit {12,7:0.000} / ohne {13,7:0.000} MWh  {14}",
                Bau, FlaecheM2, Rampentage, LaengsteRampeH, AufheizzeitMaxH?.ToString(CultureInfo.InvariantCulture) ?? "–",
                W1, W2, W3, Erreichbar ? "ja" : "nein", PaufKW, SpitzeMitKW, SpitzeOhneKW, HeizwaermeMitMWh, HeizwaermeOhneMWh,
                Tauglich ? "TAUGLICH" : "nicht tauglich");
        }

        /// <summary>Rechnet einen Kandidaten auf der eingelegten Arbeitskopie.</summary>
        internal static Probe Rechnen(string bau)
        {
            int id = new ProjektDuplizierenCtrl().Duplizieren(Konditionierungsprojekt1051.VORLAGE_NAME, "Bauwahlprobe " + bau);
            Assert.True(id > 0, "Kopie von 1007 für " + bau);
            string fehler = Konditionierungsprojekt1051.KatalogbauAnlegen(bau, "Bauwahlprobe " + bau, out int kb);
            Assert.True(fehler == null, bau + ": " + fehler);
            fehler = Konditionierungsprojekt1051.Bauen(id, kb);
            Assert.True(fehler == null, bau + ": " + fehler);

            AufheizLauf.Gebaeudelauf mit = Assert.Single(AufheizLauf.Projekt(id, Konditionierungsprojekt1051.AUFHEIZ));
            AufheizLauf.Gebaeudelauf ohne = Assert.Single(AufheizLauf.Projekt(id, Aufheizvorgabe.Aus));
            Assert.True(mit.Gerechnet && ohne.Gerechnet, bau + ": nicht gerechnet");
            Aufheizplan p = mit.Plan;
            Assert.NotNull(p);
            double flaeche = Zonenprojekt1052.Tabelle("SELECT Wohnflaeche_Waermebedarf FROM Z_ProjektGebaeude WHERE ID_Projekt = ?", id)
                .Rows.Cast<System.Data.DataRow>().Select(r => Convert.ToDouble(r[0], CultureInfo.InvariantCulture)).Single();
            return new Probe(bau, flaeche, p.Aufheiztage, p.LaengsteRampeH, p.AufheizzeitMaxH,
                             p.TageUnerreichbar, p.TageBegrenzt, mit.Ergebnis.Aufheizung?.AufheiztageNachweisband ?? 0,
                             p.Bemessung?.Wirksam?.Erreichbar ?? false,
                             (p.Bemessung?.AufheizleistungW ?? double.NaN) / 1000.0,
                             mit.Ergebnis.HeizlastW.Max() / 1000.0, ohne.Ergebnis.HeizlastW.Max() / 1000.0,
                             mit.Ergebnis.HeizlastW.Sum() / 1e6, ohne.Ergebnis.HeizlastW.Sum() / 1e6);
        }

        [Fact]
        public void Der_gewaehlte_Bau_traegt_die_Rampe_und_die_Gegenprobe_nicht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var kultur = new Kulturvorrichtung("de-DE");

            var proben = new List<Probe>();
            foreach (string bau in Konditionierungsprojekt1051.KANDIDATEN.Append(Konditionierungsprojekt1051.GEGENPROBE))
            {
                Probe p = Rechnen(bau);
                proben.Add(p);
                _aus.WriteLine(p.ToString());
            }

            Probe gewaehlt = proben.Take(Konditionierungsprojekt1051.KANDIDATEN.Length).FirstOrDefault(p => p.Tauglich);
            Assert.True(gewaehlt != null, "Kein Kandidat trägt die Rampe - Rückfall nach Festlegung 31 (knappe Heizleistung_Max, dann Art „fest“).");
            Assert.Equal(Konditionierungsprojekt1051.KATALOGBAU, gewaehlt.Bau);
            Assert.False(proben.Last().Tauglich, "Die Gegenprobe " + Konditionierungsprojekt1051.GEGENPROBE + " trägt die Rampe.");
            _aus.WriteLine("Gewählt: " + gewaehlt.Bau);
        }
    }
}
