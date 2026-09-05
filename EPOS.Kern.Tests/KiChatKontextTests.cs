using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="KiChatKontext"/> nach iU9-W15b.0f — die Bereichszuordnung des
    /// Assistenten, jetzt plattformfrei (Befund W15b-B19, Entscheid E-9).
    ///
    /// <para><b>Warum das geprüft wird.</b> Der Bereichsname ist das einzige Stück
    /// Bedienzustand, das den Rechner verlässt. Der Klassenkopf von
    /// <c>HilfeKontext</c> sagt seit H4/H5, warum: „Der Kontext verlaesst den Rechner
    /// und darf deshalb ausschliesslich generische Bereichsbezeichnungen enthalten."
    /// Die Schranke, die das durchsetzt, ist <see cref="KiChatKontext.Freigegeben"/> —
    /// und die war bis W15b an <c>Form.ActiveForm</c> gekettet und damit ohne
    /// Oberfläche nicht prüfbar.</para>
    ///
    /// <para>Geprüft werden drei Dinge: dass die Schranke wirklich nur die Positivliste
    /// durchlässt, dass die Zuordnung nach Seitenschlüssel (iOS) trifft, und dass ein
    /// fehlender oder werfender Ermittlungshaken bei „Unbekannter Bereich" endet statt
    /// zu scheitern.</para>
    /// </summary>
    public class KiChatKontextTests
    {
        // ==================================================================
        //  Die Schranke
        // ==================================================================

        /// <summary>
        /// <b>Der Kernnachweis.</b> Freier Text kommt nicht durch — auch nicht, wenn er
        /// wie ein Bereichsname aussieht. Genau daran hängt die Zusage, dass kein
        /// Projekt- oder Kundenname über den Kontext hinausgeht.
        /// </summary>
        [Theory]
        [InlineData("Muster GmbH")]
        [InlineData("Projekt Muster GmbH, Kunde Meier")]
        [InlineData("administration")]     // Kleinschreibung ist NICHT dasselbe
        [InlineData(" Bericht ")]          // getrimmt wird, aber der Wert muss stimmen
        [InlineData("Bericht erstellen")]  // ein Fenstertitel ist kein Bereich
        public void Nur_die_Positivliste_kommt_durch(string eingabe)
        {
            string ergebnis = KiChatKontext.Freigegeben(eingabe);

            if (eingabe.Trim() == "Bericht")
                Assert.Equal("Bericht", ergebnis);
            else
                Assert.Equal(KiChatKontext.BEREICH_UNBEKANNT, ergebnis);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Ohne_Angabe_ist_der_Bereich_unbekannt(string eingabe)
        {
            Assert.Equal(KiChatKontext.BEREICH_UNBEKANNT, KiChatKontext.Freigegeben(eingabe));
        }

        /// <summary>
        /// Jeder Eintrag der Positivliste kommt durch sich selbst durch — sonst wäre die
        /// Liste an einer Stelle anders geschrieben als an der anderen.
        /// </summary>
        [Fact]
        public void Jeder_Eintrag_der_Positivliste_kommt_durch()
        {
            Assert.All(KiChatKontext.Bereiche,
                       b => Assert.Equal(b, KiChatKontext.Freigegeben(b)));
        }

        /// <summary>
        /// 27 Bereiche plus der Ersatzwert. Die Zahl steht hier, damit ein Zuwachs
        /// auffällt: Jeder neue Bereich ist eine Angabe mehr, die hinausgeht.
        /// </summary>
        [Fact]
        public void Es_sind_achtundzwanzig_freigegebene_Bezeichnungen()
        {
            Assert.Equal(28, KiChatKontext.Bereiche.Count);
            Assert.Contains(KiChatKontext.BEREICH_UNBEKANNT, KiChatKontext.Bereiche);
        }

        // ==================================================================
        //  Die Zuordnung nach Seitenschluessel (iOS)
        // ==================================================================

        /// <summary>
        /// Die sechs Seitenschlüssel, die <c>AppWurzel</c> kennt. Die Werte stehen im
        /// Kern als Zeichenkette, weil er <c>EPOS.UI</c> nicht kennt — verschreibt sich
        /// jemand auf einer der beiden Seiten, fällt es hier auf.
        /// </summary>
        [Theory]
        [InlineData("PROJEKTLISTE", "Projektverwaltung")]
        [InlineData("ENERGIETRAEGER_VARIANTE", "Kosten und Preise")]
        [InlineData("BHKW_WIRTSCHAFTLICHKEIT", "BHKW")]
        [InlineData("SIMULATION_ERGEBNIS", "Detaillierte Simulation")]
        [InlineData("KI_ASSISTENT", "Hilfe")]
        public void Seitenschluessel_treffen_ihren_Bereich(string schluessel, string bereich)
        {
            Assert.Equal(bereich, KiChatKontext.BereichFuerSeite(schluessel));
        }

        /// <summary>
        /// Die Simulationskonfiguration bekommt die FEINERE Bezeichnung — dieselbe, die
        /// die WinForms-Maske über <c>SetzeBereich</c> gesetzt hat.
        /// </summary>
        [Fact]
        public void Simulationskonfiguration_behaelt_die_feinere_Bezeichnung()
        {
            Assert.Equal(KiChatKontext.B_SIM_KONFIG,
                         KiChatKontext.BereichFuerSeite("SIMULATION_KONFIGURATION"));
            Assert.StartsWith("Simulation Konfiguration", KiChatKontext.B_SIM_KONFIG);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("GIBT_ES_NICHT")]
        public void Unbekannte_Seitenschluessel_enden_bei_unbekannt(string schluessel)
        {
            Assert.Equal(KiChatKontext.BEREICH_UNBEKANNT,
                         KiChatKontext.BereichFuerSeite(schluessel));
        }

        // ==================================================================
        //  Die Ermittlung - Sache der Huelle
        // ==================================================================

        /// <summary>
        /// Ohne Hülle gibt es keinen Bereich. Das ist die Lage des Aktionsharnischs, der
        /// Konsolenwerkzeuge und — bis iU11 — der iOS-Hülle: Der Assistent antwortet
        /// dann ohne Bereichsangabe, unschärfer, aber nicht falsch.
        /// </summary>
        [Fact]
        public void Ohne_Haken_bleibt_der_Bereich_unbekannt()
        {
            Func<string> vorher = KiChatKontext.AktiverBereich;
            try
            {
                KiChatKontext.AktiverBereich = null;
                Assert.Equal(KiChatKontext.BEREICH_UNBEKANNT, KiChatKontext.AktuellerBereich());
            }
            finally { KiChatKontext.AktiverBereich = vorher; }
        }

        /// <summary>
        /// <b>Auch die Hülle kommt an der Schranke nicht vorbei.</b> Liefert sie freien
        /// Text — durch einen Fehler oder eine übersehene Stelle —, wird daraus
        /// „Unbekannter Bereich", nicht der Text.
        /// </summary>
        [Fact]
        public void Auch_die_Huelle_kommt_an_der_Schranke_nicht_vorbei()
        {
            Func<string> vorher = KiChatKontext.AktiverBereich;
            try
            {
                KiChatKontext.AktiverBereich = () => "Projekt Muster GmbH";
                Assert.Equal(KiChatKontext.BEREICH_UNBEKANNT, KiChatKontext.AktuellerBereich());

                KiChatKontext.AktiverBereich = () => KiChatKontext.B_SIMULATION;
                Assert.Equal("Simulation", KiChatKontext.AktuellerBereich());
            }
            finally { KiChatKontext.AktiverBereich = vorher; }
        }

        /// <summary>Ein werfender Haken sperrt nicht - er wird zu „unbekannt".</summary>
        [Fact]
        public void Werfender_Haken_endet_bei_unbekannt()
        {
            Func<string> vorher = KiChatKontext.AktiverBereich;
            try
            {
                KiChatKontext.AktiverBereich = () => throw new InvalidOperationException("kein Fenster");
                Assert.Equal(KiChatKontext.BEREICH_UNBEKANNT, KiChatKontext.AktuellerBereich());
            }
            finally { KiChatKontext.AktiverBereich = vorher; }
        }
    }
}
