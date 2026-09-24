using System;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // Eine Zeile der Tab_ProjektPhotovoltaik (PV-Konzept Paragraf 6.1, Etappe P3):
    // die PV-Verguetungsangaben eines STAMMPROJEKTS (Muster Tab_ProjektTarif).
    // NULL heisst durchgaengig "nicht gepflegt / Rueckfall", nie 0.
    // ---------------------------------------------------------------------------
    public class ProjektPhotovoltaikModel
    {
        /// <summary>Primaerschluessel (MAX(ID)+1-Hausmuster).</summary>
        public int ID;

        /// <summary>Stammprojekt (eindeutig - idx_ProjektPhotovoltaik).</summary>
        public int ID_Projekt;

        /// <summary>false = exakt Bestandsverhalten (Flat-Einspeiseverguetung wie
        /// bisher) - das Abnahmekriterium der Etappe P4.</summary>
        public bool Aktiv;

        /// <summary>DbWerte.PV_VERMARKTUNG_* (EV / MARKTPRAEMIE / SONSTIGE_DV / KEINE).</summary>
        public string Vermarktungsform = DbWerte.PV_VERMARKTUNG_EV;

        /// <summary>DbWerte.PV_EINSPEISEART_* (UEBERSCHUSS / VOLL).</summary>
        public string Einspeiseart = DbWerte.PV_EINSPEISEART_UEBERSCHUSS;

        /// <summary>Pflichtangabe; bestimmt Degressionsstand und Paragraf-51-Regime.</summary>
        public DateTime Inbetriebnahme = DateTime.MinValue;

        /// <summary>kWp-Override; null = rechnerisch aus den Anlagen (V3).</summary>
        public double? KwpOverride;

        /// <summary>AW-Override [ct/kWh]; null = Katalogherleitung (EegSatzRechner).</summary>
        public double? AwOverride;

        /// <summary>DV-Entgelt [ct/kWh]; Vorbelegung 0,40 (N5) setzt der Controller.</summary>
        public double? DvEntgelt;

        /// <summary>PPA-Festpreis [ct/kWh] (Form c); alternativ der Spot-Aufschlag.</summary>
        public double? PpaPreis;

        /// <summary>PPA-Aufschlag auf Spot [ct/kWh] (Form c mit Reihe).</summary>
        public double? PpaSpotAufschlag;

        /// <summary>
        /// ETAPPE E9a (Schritt D, Schemaschritt 118): DV-Entgelt des Szenarios Günstig
        /// [ct/kWh]; <c>null</c> (und 0) = wie Erwartet (<see cref="DvEntgelt"/>). Wirksam
        /// über <see cref="ProjektPhotovoltaikCtrl.FuerSzenario"/>.
        /// </summary>
        public double? DvEntgeltBest;

        /// <summary>ETAPPE E9a: DV-Entgelt des Szenarios Ungünstig [ct/kWh]; <c>null</c> = wie Erwartet.</summary>
        public double? DvEntgeltWorst;

        /// <summary>ETAPPE E9a (Schritt D): PPA-Festpreis des Szenarios Günstig [ct/kWh];
        /// <c>null</c> (und 0) = wie Erwartet (<see cref="PpaPreis"/>).</summary>
        public double? PpaPreisBest;

        /// <summary>ETAPPE E9a: PPA-Festpreis des Szenarios Ungünstig [ct/kWh]; <c>null</c> = wie Erwartet.</summary>
        public double? PpaPreisWorst;

        /// <summary>DbWerte.PV_SCHALTER_* - AUTO wendet die Regel aus PV-Konzept 4.4 an.</summary>
        public string Par51_Anwenden = DbWerte.PV_SCHALTER_AUTO;

        /// <summary>Einbaujahr des intelligenten Messsystems; wirkt ab dem Folgejahr.</summary>
        public int? IMSys_Einbaujahr;

        /// <summary>Stufe-1-Pauschale "Ausfallanteil der Einspeisearbeit" [%];
        /// Vorbelegung 20 (F5) setzt der Controller.</summary>
        public double? AusfallanteilProzent;

        /// <summary>Paragraf-51a-Kompensation anwenden (Standard true).</summary>
        public bool Par51a_Kompensieren = true;

        /// <summary>DbWerte.PV_SCHALTER_* - 60-%-Kappung (Paragraf 9 Abs. 2).</summary>
        public string Kappung60_Anwenden = DbWerte.PV_SCHALTER_AUTO;

        /// <summary>Projekt-Override des Jahresmarktwerts [ct/kWh]; null = Katalog.</summary>
        public double? MarktwertJahresmittel;

        /// <summary>Szenarioparameter Marktwertentwicklung [%/a]; 0 = konstant.</summary>
        public double MarktwertEntwicklung;

        /// <summary>Option 4.5.1: Netzbezug stundenscharf aus der Preiszeitreihe bewerten.</summary>
        public bool BezugAusPreisreihe;

        /// <summary>
        /// Jaehrliche Leistungsdegradation der PV-Anlagen [%/a] (Stufe E2.4,
        /// Migrationsschritt 63).
        ///
        /// <para><b>null = 0 %/a</b>, also ergebnisneutral - die Hausregel „der
        /// Vorgabewert ist der, der nichts aendert". Beim ANLEGEN einer neuen Zeile
        /// belegt <see cref="ProjektPhotovoltaikCtrl.LiesOderVorbelegt"/> mit 0,5 vor
        /// (Muster N5/F5); Bestandszeilen bleiben NULL und rechnen wie bisher.</para>
        ///
        /// <para>Wirkt als Faktor (1 − d/100)^(t−1) in <see cref="PvErloesRechner"/> auf
        /// Einspeiseerloes, Paragraf-51-Ausfall/-Gutschrift und den vermiedenen Bezug des
        /// Jahres t - <b>nicht</b> in der Stundensimulation des Basisjahres.</para>
        /// </summary>
        public double? Degradation;

        /// <summary>
        /// Uebernimmt diese Zeile die Verguetung ihres STAMMPROJEKTS (Konzept § 2.16,
        /// Schemaschritt 93)?
        ///
        /// <para><b>true = uebernommen</b> - die Zeile bleibt als Rueckweg zu den
        /// eigenen Werten stehen, gelesen wird sie nicht; der Rechenweg nimmt die Zeile
        /// des Stamms (<see cref="ProjektPhotovoltaikCtrl.LiesAufgeloest"/>).
        /// <b>false = eigene Werte</b> - diese Zeile gilt.</para>
        ///
        /// <para><b>Die Vorgabe des Modells ist false</b>, und das ist die
        /// ergebnisneutrale Lesart: Wer eine Zeile hat, hat eigene Werte — genau der
        /// Bestand vor Schemaschritt 93, und genau das, was eine fehlende Spalte oder
        /// ein NULL an einer vorhandenen Zeile bedeutet. „Uebernommen" ist dagegen der
        /// Zustand OHNE Zeile (die Vorgabe jeder neuen Variante) oder mit
        /// ausdruecklich gesetztem Kennzeichen. Der STAMM fuehrt immer eigene Werte —
        /// fuer ihn ist das Feld ohne Bedeutung
        /// (<see cref="ProjektPhotovoltaikCtrl.LiesAufgeloest"/> fragt es dort gar
        /// nicht).</para>
        /// </summary>
        public bool UebernahmeStamm;

        public DateTime? GeaendertAm;
    }
}
