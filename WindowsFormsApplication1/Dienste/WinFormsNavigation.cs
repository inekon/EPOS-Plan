using System;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Windows-Fassung von <see cref="INavigation"/>: Hier — und nur hier — kennt
    /// die Anwendung die Zuordnung von Schlüssel zu Maske.
    ///
    /// <para><b>Zwei Tabellen, ein Ort.</b> <see cref="OeffneGewerk"/> ordnet die zwölf
    /// Gewerksschlüssel den zwölf <c>Set*Control</c>-Methoden von <c>FormMain</c> zu;
    /// <see cref="OeffneMaske"/> ordnet die Maskenschlüssel den Formularklassen zu.
    /// Vorher standen diese Zuordnungen 35- bzw. 45-mal im Programmtext verstreut, jedes
    /// Mal als direkter Zugriff auf <c>Program.mainfrm</c> bzw. als <c>new Form_X()</c>.</para>
    ///
    /// <para><b>Was hier ABSICHTLICH nicht steht.</b> Die Bearbeitungsdialoge der
    /// Kontextmenüs (<c>Form_BHKWEing</c>, <c>Form_PufferSp</c>, <c>Form_Gebaeude</c> …).
    /// Sie tauschen mit ihrem Aufrufer typisierte Modelllisten und Auswahlzeilen aus —
    /// füllen, zeigen, zurücklesen. Ein Schlüssel plus <c>object[]</c> könnte das nur
    /// abbilden, indem der halbe Kontextmenü-Controller hierher zöge; die
    /// <c>*KontextMenuCtrl</c> sind ohnehin Oberflächenbausteine (sie führen
    /// <c>ListView</c> und <c>ContextMenuStrip</c>) und wandern mit ihren Masken in
    /// Paket iU9. Siehe den iU5-Statusblock im Umsetzungskonzept.</para>
    ///
    /// <para><b>Kein geöffnetes Detailformular ist kein Fehler.</b> <c>Program.mainfrm</c>
    /// ist <c>null</c>, solange der Anwender kein Projekt geöffnet hat. Der Bestand
    /// stürzte an dieser Stelle ab; hier wird still nichts getan — eine Liste, die es
    /// nicht gibt, muss nicht aufgefrischt werden.</para>
    /// </summary>
    public sealed class WinFormsNavigation : INavigation
    {
        /// <inheritdoc/>
        public void OeffneGewerk(string gewerk, int idProjekt, string projektname)
        {
            FormMain frm = Program.mainfrm;
            if (frm == null || string.IsNullOrEmpty(gewerk)) return;

            string name = projektname ?? "";

            switch (gewerk)
            {
                case Gewerke.Waermepumpe: frm.SetWPControl(name); break;
                case Gewerke.Bhkw: frm.SetBHKWControl(name); break;
                case Gewerke.Stromspeicher: frm.SetSPControl(name); break;
                case Gewerke.Heizkessel: frm.SetHeizkesselControl(name); break;
                case Gewerke.Gebaeude: frm.SetGebaeudeControl(name); break;
                case Gewerke.WaermebedarfExtern: frm.SetWaermebedarfExternControl(name); break;
                case Gewerke.Prozesswaerme: frm.SetProzesswaermeControl(idProjekt); break;
                case Gewerke.Strombedarf: frm.SetStrombedarfControl(idProjekt); break;
                case Gewerke.Stromganglinie: frm.SetStromganglinieControl(name); break;
                case Gewerke.Photovoltaik: frm.SetPVControl(name); break;
                case Gewerke.Pufferspeicher: frm.SetPufferSpControl(name); break;
                case Gewerke.Solarthermie: frm.SetSolarControl(name); break;
            }
        }

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

                case Masken.StromspeicherAdmin:
                    using (Form_AdminStromspeicher frm = new Form_AdminStromspeicher()) return MitOk(frm);

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

                case Masken.ProzesswaermeAdmin:
                    using (Form_Prozesswaerme_Admin frm = new Form_Prozesswaerme_Admin())
                    {
                        frm.SetControls("");
                        return MitOk(frm);
                    }

                case Masken.StromverbraucherAdmin:
                    using (Form_Stromverbraucher_Admin frm = new Form_Stromverbraucher_Admin())
                    {
                        frm.SetControls("");
                        return MitOk(frm);
                    }

                // iU9-W12.4: Die Verwaltung ist die Razor-Komponente
                // StromganglinieAdminDialog; die Huelle zeigt sie modal.
                case Masken.StromganglinieAdmin:
                    return StromganglinieAdminHuelle.Oeffnen(null);

                case Masken.SolarganglinieAdmin:
                    using (Form_Solarganglinie_Admin frm = new Form_Solarganglinie_Admin())
                    {
                        frm.SetControls();
                        return MitOk(frm);
                    }

                case Masken.BrauchwasserAdmin:
                    using (Form_Brauchwasser_Admin frm = new Form_Brauchwasser_Admin())
                    {
                        frm.SetControls("");
                        return MitOk(frm);
                    }

                // iU9-W13.1: Die vier VDI-3805-Katalogimporte sind EINE
                // Razor-Komponente mit vier Auspraegungen; die Huelle waehlt sie
                // ueber KatalogImportArt. Der Rueckgabewert sagt jetzt, ob etwas
                // geschrieben wurde - beim Vorlaeufer Form_WP_einlesen war er
                // IMMER false, weil die Maske ihr DialogResult nie setzte
                // (Befund W13-B4b).
                case Masken.WpImport:
                    return KatalogImportHuelle.Oeffnen(null, KatalogImportArt.Waermepumpe);

                case Masken.HeizkesselAdmin:
                    using (Form_Heizkessel_Admin frm = new Form_Heizkessel_Admin()) return MitOk(frm);

                case Masken.BhkwAdmin:
                    using (Form_BHKWAdmin frm = new Form_BHKWAdmin()) return MitOk(frm);

                case Masken.SolarkollektorenAdmin:
                    using (Form_SolarKollektorenAdmin frm = new Form_SolarKollektorenAdmin()) return MitOk(frm);

                case Masken.PvAdmin:
                    using (Form_AdminPV frm = new Form_AdminPV()) return MitOk(frm);

                case Masken.HeizkesselImport:
                    return KatalogImportHuelle.Oeffnen(null, KatalogImportArt.Heizkessel);

                case Masken.PufferSpImport:
                    return KatalogImportHuelle.Oeffnen(null, KatalogImportArt.Pufferspeicher);

                case Masken.PufferSpAdmin:
                    using (Form_PufferSp_Admin frm = new Form_PufferSp_Admin()) return MitOk(frm);

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

                case Masken.ProjektSpeichernUnter:
                    using (Form_ProjektSpeichernUnter frm = new Form_ProjektSpeichernUnter())
                        return MitOk(frm);

                // --- Masken, die eine Projektwahl herausgeben -------------------------
                case Masken.ProjektAuswahl:
                    using (Form_ProjektAuswahl frm = new Form_ProjektAuswahl())
                    {
                        if (!MitOk(frm)) return false;
                        return WahlUebernehmen(argumente, frm.m_ID_Projekt, frm.m_szProjekt);
                    }

                case Masken.ProjektDelete:
                    using (Form_ProjektDelete frm = new Form_ProjektDelete())
                    {
                        if (!MitOk(frm)) return false;
                        return WahlUebernehmen(argumente, frm.ID_Projekt, frm.szProjekt);
                    }

                // --- Zusammengesetzte Abläufe ----------------------------------------
                case Masken.Assistent:
                    return AssistentZeigen(Ganzzahl(argumente, 0));

                case Masken.ProjektDetail:
                    return ProjektDetailZeigen(Text(argumente, 0), Ganzzahl(argumente, 1));
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
                    Program.startfrm?.VariantenAnzeigeAktualisieren();
                    break;

                case Ansichten.BerichteKosten:
                    Program.startfrm?.ZeigeBerichteKosten();
                    break;

                case Ansichten.ProjektDetail:
                    ProjektnameNachziehen();
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
            WizardParent wizparent = new WizardParent(AssistentSeiten.Erzeugen());

            WizardCtrl ctrl = WizardCtrl.Aktueller;
            if (ctrl != null) ctrl.parentform = wizparent;

            wizparent.SetWizardMode(betriebsart);
            wizparent.ShowDialog();

            return wizparent.gespeichert;
        }

        /// <summary>
        /// Der EINE Ladeweg ins Detailformular „Konfiguration Projekt": Stammdaten, alle
        /// Gewerkslisten, alle Kontextmenüs, Anzeige als Dialog, danach den
        /// Projektkontext der Startseite nachziehen.
        ///
        /// <para>Der Ablauf stand bis iU5 in <c>MenueCtrl.ProjektInFormMainLaden</c> und
        /// ist zeilengleich hierher gezogen — er ist von der ersten bis zur letzten
        /// Anweisung Oberflächenarbeit und gehört damit in den Adapter, nicht in einen
        /// Controller.</para>
        /// </summary>
        private static bool ProjektDetailZeigen(string szProjekt, int idProjekt)
        {
            ProjektCtrl ctrlproj = new ProjektCtrl();
            ctrlproj.ReadSingle(szProjekt);

            Program.mainfrm = new FormMain();
            FormMain frmmain = Program.mainfrm;

            string szKlima = frmmain.GetKlimaregion(ctrlproj.m_ID_Klimaregion);

            frmmain.SetProjekt(szProjekt);
            frmmain.SetIDProjekt(idProjekt);
            frmmain.SetKlima(szKlima);
            Program.startfrm.SetKlima(szKlima);
            frmmain.SetBearbeiter(ctrlproj.m_szBearbeiter);
            frmmain.SetKunde(ctrlproj.m_szKunde);
            frmmain.SetAenderungsdatum(ctrlproj.m_Aenderungsdatum);
            frmmain.SetBeschreibung(ctrlproj.m_szBeschreibung);
            frmmain.SetWPControl(szProjekt);
            frmmain.SetBHKWControl(szProjekt);
            frmmain.SetSPControl(szProjekt);
            frmmain.SetHeizkesselControl(szProjekt);
            frmmain.SetGebaeudeControl(szProjekt);
            frmmain.SetWaermebedarfExternControl(szProjekt);
            frmmain.SetProzesswaermeControl(idProjekt);
            frmmain.SetStrombedarfControl(idProjekt);
            frmmain.SetStromganglinieControl(szProjekt);
            frmmain.SetPVControl(szProjekt);
            frmmain.SetPufferSpControl(szProjekt);
            frmmain.SetSolarControl(szProjekt);
            frmmain.Add_WPKontext();
            frmmain.Add_BHKWKontext();
            frmmain.Add_GebäudeKontext();
            frmmain.Add_HeizkesselKontext();
            frmmain.Add_WaermebedarfExternKontext();
            frmmain.Add_ProzesswaermeKontext();
            frmmain.Add_StrombedarfKontext();
            frmmain.Add_StromganglinieKontext();
            frmmain.Add_SpKontext();
            frmmain.Add_PVKontext();
            frmmain.Add_SolarKontext();

            frmmain.ShowDialog();

            Program.startfrm.m_szProjektname = szProjekt;
            Program.startfrm.m_ID_Projekt = idProjekt;
            Program.startfrm.SetTextProjekt(szProjekt);
            return true;
        }

        /// <summary>
        /// Zieht den Projektnamen aus <c>Tab_Applikation</c> in den Kopf des
        /// Detailformulars nach — der Inhalt des bisherigen
        /// <c>MenueCtrl.SetProjektname</c>.
        /// </summary>
        private static void ProjektnameNachziehen()
        {
            FormMain frm = Program.mainfrm;
            if (frm == null) return;

            ApplikationCtrl ctrl = new ApplikationCtrl();
            ctrl.ReadSingle();
            frm.SetProjekt(ctrl.m_szProjektname);
        }

        // ==================================================================
        //  Kleinkram
        // ==================================================================

        private static bool MitOk(Form frm)
        {
            return frm.ShowDialog() == DialogResult.OK;
        }

        private static bool WahlUebernehmen(object[] argumente, int id, string name)
        {
            Projektwahl fach = argumente != null && argumente.Length > 0
                ? argumente[0] as Projektwahl
                : null;

            if (fach == null) return true;   // Aufrufer will das Ergebnis nicht

            fach.Id = id;
            fach.Name = name ?? "";
            return true;
        }

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
