using System;
using EPOS.UI.Dialoge.Projekt;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Windows-Fassung von <see cref="INavigation"/>: Hier — und nur hier — kennt
    /// die Anwendung die Zuordnung von Schlüssel zu Maske.
    ///
    /// <para><b>Eine Tabelle, ein Ort.</b> <see cref="OeffneMaske"/> ordnet die
    /// Maskenschlüssel den Formularklassen bzw. — nach Paket iU9 — den Razor-Hüllen zu.
    /// Vorher standen diese Zuordnungen 45-mal im Programmtext verstreut, jedes Mal als
    /// <c>new Form_X(); frm.ShowDialog();</c>.</para>
    ///
    /// <para><b>Seit iU9-W16b.1 ohne <c>OeffneGewerk</c>.</b> Die zweite Tabelle ordnete
    /// zwölf Gewerksschlüssel den zwölf <c>Set*Control</c>-Methoden des Detailformulars
    /// <c>FormMain</c> zu. Mit dem Anwenderentscheid E-7 (K6-a) ist dieser Altzweig
    /// stillgelegt: <c>FormMain</c>, <c>Form_StromTest</c>, <c>StromTestClass</c> und die
    /// zwölf <c>*KontextMenuCtrl</c> sind gelöscht (3 811 Zeilen, Befunde W16-B27/B28),
    /// und mit ihnen die Methode, die Konstantenklasse <c>Gewerke</c> und der
    /// Maskenschlüssel <c>Masken.ProjektDetail</c>.</para>
    /// </summary>
    public sealed class WinFormsNavigation : INavigation
    {
        /// <inheritdoc/>
        public bool OeffneMaske(string maske, params object[] argumente)
        {
            if (string.IsNullOrEmpty(maske)) return false;

            switch (maske)
            {
                // --- Stammdaten und Herstellerdaten: in sich geschlossene Masken ------
                // iU9-W7.3: Die Waermepumpen-Datenbank ist die Razor-Komponente
                // WaermepumpeStammDialog; Form_WP ist im selben Schritt GELOESCHT
                // (Regel M1). Die Huelle liefert dasselbe true/false wie MitOk.
                case Masken.WpAdministration:
                    return WaermepumpeStammHuelle.Oeffnen(null);

                // iU9-W14a.3: Die beiden Modulkataloge sind EINE Razor-Komponente
                // mit zwei Auspraegungen (ModulKatalogProfil im Kern).
                case Masken.StromspeicherAdmin:
                    return StromspeicherAdminHuelle.Oeffnen(null);

                // iU9-W9.2: Die Gebaeudeverwaltung ist die Razor-Komponente GebaeudeDialog
                // im Modus Admin; Form_Gebaeude ist im selben Schritt GELOESCHT (Regel M1).
                // Die Huelle liefert dasselbe true/false wie MitOk.
                case Masken.GebaeudeAdmin:
                    return GebaeudeHuelle.Katalogverwaltung(null);

                // iU9-W8.4: Die Gebaeudetypen-Verwaltung ist die Razor-Komponente
                // GebaeudetypDialog; Form_EingGebTyp ist im selben Schritt GELOESCHT
                // (Regel M1). Die Huelle liefert dasselbe true/false wie MitOk.
                case Masken.GebaeudetypenAdmin:
                    return GebaeudetypHuelle.Oeffnen(null);

                // iU9-W13.2: Die Verwaltung der externen Waermebedarfsganglinien
                // ist die Razor-Komponente WaermebedarfAdminDialog. Der
                // Rueckgabewert sagt jetzt etwas: Beim Vorlaeufer war er IMMER
                // false, weil btn_OK_Click nur ein Feld "result" setzte und nie
                // this.DialogResult (Befund W13-B4).
                case Masken.WaermebedarfExternAdmin:
                    return WaermebedarfAdminHuelle.Oeffnen(null);

                // iU9-W14b.1: Die drei Bedarfs-Katalogverwaltungen sind EINE
                // Razor-Komponente mit drei Auspraegungen; die Huelle waehlt sie
                // ueber BedarfsArt. Die drei WinForms-Masken sind im selben Schritt
                // GELOESCHT (Regel M1). Der Aufruf frm.SetControls("") entfaellt: Die
                // Komponente laedt ihre Liste selbst, und ein Projekt hatten die drei
                // Verwaltungen ohnehin nie.
                case Masken.ProzesswaermeAdmin:
                    return BedarfAdminHuelle.Oeffnen(null, BedarfsArt.Prozesswaerme);

                case Masken.StromverbraucherAdmin:
                    return BedarfAdminHuelle.Oeffnen(null, BedarfsArt.Stromverbraucher);

                // iU9-W12.4: Die Verwaltung ist die Razor-Komponente
                // StromganglinieAdminDialog; die Huelle zeigt sie modal.
                case Masken.StromganglinieAdmin:
                    return StromganglinieAdminHuelle.Oeffnen(null);

                // iU9-W14b.2: Die Verwaltung der Solarthermieganglinien ist die
                // Razor-Komponente SolarganglinieAdminDialog; die Huelle zeigt sie
                // modal. Der Rueckgabewert sagt jetzt etwas: Beim Vorlaeufer war er
                // IMMER false, weil btn_OK_Click nur ein Feld "result" setzte und nie
                // this.DialogResult (Befund W14-B4).
                case Masken.SolarganglinieAdmin:
                    return SolarganglinieAdminHuelle.Oeffnen(null);

                case Masken.BrauchwasserAdmin:
                    return BedarfAdminHuelle.Oeffnen(null, BedarfsArt.Brauchwasser);

                // iU9-W13.1: Die vier VDI-3805-Katalogimporte sind EINE
                // Razor-Komponente mit vier Auspraegungen; die Huelle waehlt sie
                // ueber KatalogImportArt. Der Rueckgabewert sagt jetzt, ob etwas
                // geschrieben wurde - beim Vorlaeufer Form_WP_einlesen war er
                // IMMER false, weil die Maske ihr DialogResult nie setzte
                // (Befund W13-B4b).
                case Masken.WpImport:
                    return KatalogImportHuelle.Oeffnen(null, KatalogImportArt.Waermepumpe);

                // iU9-W14a.1: Die vier Erzeuger-Katalogbrowser sind EINE
                // Razor-Komponente mit vier Auspraegungen (KatalogBrowserProfil im
                // Kern); je Maskenschluessel steht eine schmale Huelle davor. Der
                // Rueckgabewert sagt jetzt etwas: Drei der vier Vorlaeufer setzten
                // ueberhaupt kein DialogResult und lieferten IMMER false
                // (Befund W14-B4, Angleichung E-1).
                case Masken.HeizkesselAdmin:
                    return HeizkesselAdminHuelle.Oeffnen(null);

                case Masken.BhkwAdmin:
                    return BhkwAdminHuelle.Oeffnen(null);

                case Masken.SolarkollektorenAdmin:
                    return SolarkollektorAdminHuelle.Oeffnen(null);

                case Masken.PvAdmin:
                    return PvAdminHuelle.Oeffnen(null);

                case Masken.HeizkesselImport:
                    return KatalogImportHuelle.Oeffnen(null, KatalogImportArt.Heizkessel);

                case Masken.PufferSpImport:
                    return KatalogImportHuelle.Oeffnen(null, KatalogImportArt.Pufferspeicher);

                case Masken.PufferSpAdmin:
                    return PufferSpAdminHuelle.Oeffnen(null);

                case Masken.SolarkollektorenImport:
                    return KatalogImportHuelle.Oeffnen(null, KatalogImportArt.Solarkollektoren);

                // --- Masken mit Argument ---------------------------------------------
                // iU9-W13.3: Der PV-Modulimport ist die Razor-Komponente
                // PvModulImportDialog. Das Argument sagt, mit welcher Quelle sie
                // aufmacht ("CEC" bzw. "PAN"); bis dahin oeffneten die beiden
                // Menuepunkte dieselbe Maske im SELBEN Zustand (Befund W13-B51)
                // und gingen ganz an der Navigation vorbei (B55).
                case Masken.PvImport:
                    return PvModulImportHuelle.Oeffnen(null, TextOder(argumente, 0, "CEC"));

                // iU9-W12.6: Die Lastspitzenkappung ist die Razor-Komponente
                // PeakShavingDialog; die Huelle zeigt sie modal. Der Rueckgabewert
                // war schon beim Vorlaeufer immer false (Befund W12-B24) - sein
                // einziger Fussknopf trug DialogResult.Cancel.
                case Masken.PeakShaving:
                    return PeakShavingHuelle.Oeffnen(null, Ganzzahl(argumente, 0));

                // iU9-W15a.4: „Speichern unter" ist die Razor-Komponente ProjektKopieDialog;
                // ausgewertet wird wie beim Vorlaeufer nur das DialogResult.
                case Masken.ProjektSpeichernUnter:
                    return ProjektKopieHuelle.Oeffnen(null);

                // --- Masken, die eine Projektwahl herausgeben -------------------------
                // iU9-W15a.3: Beide Schluessel zeigen auf DIESELBE Razor-Komponente
                // (ProjektWahlDialog) - sie unterscheiden sich nur im Zweck. Das
                // Projektwahl-Fach fuellt die Huelle genau wie WahlUebernehmen zuvor;
                // an ihm haengt der ganze Projektwechsel (Befund W15a-B45).
                case Masken.ProjektAuswahl:
                    return ProjektWahlHuelle.Oeffnen(null,
                               ProjektWahlDialog.ProjektZweck.Oeffnen, argumente);

                case Masken.ProjektDelete:
                    return ProjektWahlHuelle.Oeffnen(null,
                               ProjektWahlDialog.ProjektZweck.Loeschen, argumente);

                // --- Zusammengesetzte Abläufe ----------------------------------------
                case Masken.Assistent:
                    return AssistentZeigen(Ganzzahl(argumente, 0));

            }

            return false;
        }

        /// <summary>
        /// Frischt die Menüleiste auf.
        ///
        /// <para>Die MDI-Menüleiste kennt heute keinen eigenen Auffrischweg: Ihre
        /// Freischaltungen hängen am Projektkontext und werden von
        /// <c>Form_Start.ProjektKontextUebernehmen</c> mitgezogen. Die Methode steht in
        /// der Schnittstelle, weil eine andere Oberfläche das trennen wird; hier bleibt
        /// sie folgenlos, statt denselben Weg ein zweites Mal anzustoßen.</para>
        /// </summary>
        public void MenueAktualisieren()
        {
        }

        /// <inheritdoc/>
        public void AnsichtAktualisieren(string bereich)
        {
            if (string.IsNullOrEmpty(bereich)) return;

            switch (bereich)
            {
                case Ansichten.Varianten:
                    StartseiteHuelle.Aktuelle?.VariantenAnzeigeAktualisieren();
                    break;

                case Ansichten.BerichteKosten:
                    StartseiteHuelle.Aktuelle?.ZeigeBerichteKosten();
                    break;

            }
        }

        // ==================================================================
        //  Zusammengesetzte Abläufe
        // ==================================================================

        /// <summary>
        /// Der Projektassistent. Baut den Rahmen, meldet ihn beim
        /// <see cref="WizardCtrl"/> an, zeigt ihn modal und meldet zurück, ob gespeichert
        /// wurde — inhaltlich unverändert gegenüber <c>MenueCtrl.AssistentZeigen</c>.
        /// </summary>
        private static bool AssistentZeigen(int betriebsart)
        {
            // iU9-W16a.5: Der Assistent ist eine Razor-Seite (EPOS.UI/Seiten/Assistent/
            // AssistentSeite.razor) in einer modalen Huelle - beide Aufrufer werten
            // "gespeichert" aus und ziehen danach den Projektkontext nach.
            return AssistentHuelle.Oeffnen(null, betriebsart);
        }

        // ==================================================================
        //  Kleinkram
        // ==================================================================

        // iU9-W16c.3: MitOk(Form) und WahlUebernehmen sind WEG. Beide waren die
        // letzten Reste der Zeit, als diese Tabelle Formulare BAUTE: MitOk zeigte
        // eine Maske modal und las ihr DialogResult, WahlUebernehmen fuellte das
        // Projektwahl-Fach. Seit W15a fuellen die Huellen es selbst, und seit
        // W15c gibt es in diesem switch kein "new Form_X()" mehr - beide Methoden
        // standen ohne Aufrufer da (nur noch in Kommentaren genannt).

        private static int Ganzzahl(object[] argumente, int stelle)
        {
            if (argumente == null || argumente.Length <= stelle) return 0;
            try { return Convert.ToInt32(argumente[stelle]); }
            catch { return 0; }
        }

        private static string Text(object[] argumente, int stelle)
        {
            if (argumente == null || argumente.Length <= stelle) return "";
            return argumente[stelle] as string ?? "";
        }

        /// <summary>
        /// Ein Textargument mit VORGABE (iU9-W13.3): Der PV-Modulimport braucht
        /// seine Quelle auch dann, wenn ein Aufrufer sie nicht mitgibt.
        /// </summary>
        private static string TextOder(object[] argumente, int stelle, string vorgabe)
        {
            string wert = Text(argumente, stelle);
            return wert.Length > 0 ? wert : vorgabe;
        }
    }
}
