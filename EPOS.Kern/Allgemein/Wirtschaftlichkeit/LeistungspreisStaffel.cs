using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die zweistufige Leistungspreis-Staffel des Stromträgers</b> — bis zur
    /// Staffelgrenze gilt der erste, darüber der zweite Preis, je in €/(kW·a) auf die
    /// Jahres-Bezugsspitze (Entscheid Q11, Anwender 22.09.2026; Weg 2 aus Nach #291).
    ///
    /// <para><b>Wo sie steht.</b> In der Kostenverwaltung neben der
    /// Energiepreisstruktur: an der Projektübersteuerung des Stromträgers
    /// (<c>energy_project_settings</c>, Spalten aus
    /// <see cref="SchemaKatalog.Schritt104_LeistungspreisStaffel"/>), gepflegt auf der
    /// Trägerkarte, geschrieben über <see cref="EnergietraegerPreisCtrl.StaffelSchreiben"/>.
    /// Bis dahin stand sie im Tarifsatz (<c>Tab_ProjektTarif</c>) und rechnete nur im
    /// Zonenmodell, das mit demselben Entscheid entfällt.</para>
    ///
    /// <para><b>Was sie bepreist.</b> Den Leistungsanteil des Netzbezugs
    /// (<see cref="KostenEmissionRechner"/>): Eine gepflegte Staffel geht dem
    /// Leistungspreis des Trägers und seinen saisonalen Sätzen VOR — dieselbe Rangfolge
    /// wie bisher, als der Tarifsatz den ganzen Stromanteil ersetzte. Bemessen wird an
    /// der <b>Viertelstunden</b>spitze des Jahres (<see cref="Netzbezugsspitze.JahrKW"/>),
    /// wie jeder Leistungspreis des Stromträgers — nicht mehr an der höchsten
    /// Stundenlast der Strommatrix, die die Spitze glättet.</para>
    ///
    /// <para><b>Gepflegt</b> ist die Staffel, sobald einer der beiden Preise größer als 0
    /// ist; eine Grenze ≤ 0 oder leer heißt „alles zum Preis der zweiten Stufe" (die
    /// Rechnung des Zonenmodells, wortgleich übernommen).</para>
    /// </summary>
    public sealed class LeistungspreisStaffel
    {
        /// <summary>Staffelgrenze [kW]; null = nicht gepflegt.</summary>
        public double? GrenzeKW { get; set; }

        /// <summary>Preis bis zur Grenze [€/(kW·a)]; null = nicht gepflegt.</summary>
        public double? Preis1EurKWa { get; set; }

        /// <summary>Preis über der Grenze [€/(kW·a)]; null = nicht gepflegt.</summary>
        public double? Preis2EurKWa { get; set; }

        /// <summary>true, sobald einer der beiden Preise größer als 0 ist — dann rechnet
        /// die Staffel und geht dem Leistungspreis des Trägers vor.</summary>
        public bool Gepflegt
        {
            get { return (Preis1EurKWa ?? 0) > 0 || (Preis2EurKWa ?? 0) > 0; }
        }

        /// <summary>Der Staffelbetrag [€/a] für eine Jahres-Bezugsspitze [kW].</summary>
        public double Betrag(double spitzeKW)
        {
            return Betrag(spitzeKW, GrenzeKW ?? 0, Preis1EurKWa ?? 0, Preis2EurKWa ?? 0);
        }

        /// <summary>
        /// Der Staffelbetrag [€/a]: <c>min(S, G) × P1 + max(0, S − G) × P2</c> mit
        /// <c>G = max(0, Grenze)</c>. Ohne Spitze (≤ 0) kein Betrag.
        /// </summary>
        public static double Betrag(double spitzeKW, double grenzeKW, double preis1, double preis2)
        {
            if (spitzeKW <= 0) return 0;
            double grenze = Math.Max(0, grenzeKW);
            double stufe1 = Math.Min(spitzeKW, grenze);
            double stufe2 = Math.Max(0, spitzeKW - grenze);
            return stufe1 * preis1 + stufe2 * preis2;
        }

        /// <summary>
        /// Liegt die Spitze über der Grenze und ist ein Preis der zweiten Stufe gepflegt?
        /// Dann trifft eine Kappung zuerst diesen Preis — die Frage der Speicherauslegung
        /// (<see cref="SpeicherAuslegungVorgabenCtrl.StaffelQuelle"/>).
        /// </summary>
        public bool ZweiteStufe(double spitzeKW)
        {
            return spitzeKW > Math.Max(0, GrenzeKW ?? 0) && (Preis2EurKWa ?? 0) > 0;
        }

        /// <summary>Der Preis der Stufe, in der die Spitze liegt [€/(kW·a)] — der Preis,
        /// den eine Kappung zuerst einspart; ohne bekannte Spitze der der ersten Stufe.</summary>
        public double PreisAnDerSpitze(double spitzeKW)
        {
            return ZweiteStufe(spitzeKW) ? (Preis2EurKWa ?? 0) : (Preis1EurKWa ?? 0);
        }
    }
}
