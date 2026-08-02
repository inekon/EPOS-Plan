using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    class MenueCtrl
    {
        public WizardParent wizparent;

        public MenueCtrl()
        {
            wizparent = null;
        }

        public void SetProjektname()
        {
            ApplikationCtrl ctrl = new ApplikationCtrl();
            ctrl.ReadSingle();
            FormMain frm = (FormMain)Program.mainfrm;
            frm.SetProjekt(ctrl.m_szProjektname);
        }

        public void ProjektNeu()
        {
            List<WizardItemClass> frm = new List<WizardItemClass>();
            frm.Add(new WizardItemClass(new Wizard_Komponenten(), WizardItemClass.KOMPONENTEN_ITEM));
            frm.Add(new WizardItemClass(new Wizard_Projekt(), WizardItemClass.PROJEKT_ITEM));
            frm.Add(new WizardItemClass(new Form_Gebaeude(), WizardItemClass.GEBAEUDE_ITEM));
            frm.Add(new WizardItemClass(new Form_Waermebedarf(), WizardItemClass.WAERMEBEDARF_ITEM));
            frm.Add(new WizardItemClass(new Form_Prozesswaerme(), WizardItemClass.PROZESS_ITEM));
            frm.Add(new WizardItemClass(new Form_Stromverbraucher(), WizardItemClass.STROMSTD_ITEM));
            frm.Add(new WizardItemClass(new Wizard_Stromlastgang(), WizardItemClass.STROMLASTGANG_ITEM));
            frm.Add(new WizardItemClass(new Form_WPAuswahl(), WizardItemClass.WP_ITEM));
            frm.Add(new WizardItemClass(new Form_SolarKollektoren(), WizardItemClass.SOLAR_ITEM));
            frm.Add(new WizardItemClass(new Form_PV(), WizardItemClass.PV_ITEM));
            frm.Add(new WizardItemClass(new Form_Stromspeicher(), WizardItemClass.SP_ITEM));
            frm.Add(new WizardItemClass(new Form_Heizkessel(), WizardItemClass.KESSEL_ITEM));
            frm.Add(new WizardItemClass(new Form_BHKWEing(), WizardItemClass.BHKW_ITEM));

            wizparent = new WizardParent(frm);
            Program.wizardctrl.parentform = wizparent;
            wizparent.SetWizardMode(WizardParent.WIZARD_MODE_NEU);
            wizparent.ShowDialog();

            if (wizparent.gespeichert)
            {
                MessageBox.Show("Daten gespeichert");
            }
        }

        public void ProjektBearbeiten()
        {
            List<WizardItemClass> frm = new List<WizardItemClass>();
            frm.Add(new WizardItemClass(new Wizard_Komponenten(), WizardItemClass.KOMPONENTEN_ITEM));
            frm.Add(new WizardItemClass(new Wizard_Projekt(), WizardItemClass.PROJEKT_ITEM));
            frm.Add(new WizardItemClass(new Form_Gebaeude(), WizardItemClass.GEBAEUDE_ITEM));
            frm.Add(new WizardItemClass(new Form_Waermebedarf(), WizardItemClass.WAERMEBEDARF_ITEM));
            frm.Add(new WizardItemClass(new Form_Prozesswaerme(), WizardItemClass.PROZESS_ITEM));
            frm.Add(new WizardItemClass(new Form_Stromverbraucher(), WizardItemClass.STROMSTD_ITEM));
            frm.Add(new WizardItemClass(new Wizard_Stromlastgang(), WizardItemClass.STROMLASTGANG_ITEM));
            frm.Add(new WizardItemClass(new Form_WPAuswahl(), WizardItemClass.WP_ITEM));
            frm.Add(new WizardItemClass(new Form_SolarKollektoren(), WizardItemClass.SOLAR_ITEM));
            frm.Add(new WizardItemClass(new Form_PV(), WizardItemClass.PV_ITEM));
            frm.Add(new WizardItemClass(new Form_Stromspeicher(), WizardItemClass.SP_ITEM));
            frm.Add(new WizardItemClass(new Form_Heizkessel(), WizardItemClass.KESSEL_ITEM));
            frm.Add(new WizardItemClass(new Form_BHKWEing(), WizardItemClass.BHKW_ITEM));

            wizparent = new WizardParent(frm);
            Program.wizardctrl.parentform = wizparent;
            wizparent.SetWizardMode(WizardParent.WIZARD_MODE_BEARBEITEN);
            wizparent.ShowDialog();
            if (wizparent.gespeichert)
            {
                MessageBox.Show("Daten gespeichert");
            }
        }

        public void ProjektOeffnen(bool zuletzt = false)
        {
            Form_ProjektSpeichernUnter frm = new Form_ProjektSpeichernUnter();
            ApplikationCtrl ctrl = new ApplikationCtrl();
            ProjektCtrl ctrlproj = new ProjektCtrl();

            ctrl.ReadSingle();

            if (!zuletzt)
            {
                DialogResult ret = frm.ShowDialog();
                if (ret == DialogResult.OK)
                {
                    Program.mainfrm = new FormMain();
                    FormMain frmmain = (FormMain)Program.mainfrm;

                    ctrlproj.ReadSingle(frm.m_szProjekt);
                    frmmain.SetProjekt(frm.m_szProjekt);
                    frmmain.SetIDProjekt(frm.m_ID_Projekt);
                    frmmain.SetKlima(frm.m_szKlimaregion);
                    Program.startfrm.SetKlima(frm.m_szKlimaregion);
                    frmmain.SetBearbeiter(ctrlproj.m_szBearbeiter);
                    frmmain.SetAenderungsdatum(ctrlproj.m_Aenderungsdatum);
                    frmmain.SetBeschreibung(ctrlproj.m_szBeschreibung);
                    frmmain.SetKunde(ctrlproj.m_szKunde);
                    frmmain.SetWPControl(frm.m_szProjekt);
                    frmmain.SetBHKWControl(frm.m_szProjekt);
                    frmmain.SetSPControl(frm.m_szProjekt);
                    frmmain.SetHeizkesselControl(frm.m_szProjekt);
                    frmmain.SetGebaeudeControl(frm.m_szProjekt);
                    frmmain.SetWaermebedarfExternControl(frm.m_szProjekt);
                    frmmain.SetProzesswaermeControl(frm.m_ID_Projekt);
                    frmmain.SetStrombedarfControl(frm.m_ID_Projekt);
                    frmmain.SetStromganglinieControl(frm.m_szProjekt);
                    frmmain.SetPVControl(frm.m_szProjekt);
                    frmmain.SetPufferSpControl(frm.m_szProjekt);
                    frmmain.SetSolarControl(frm.m_szProjekt);
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

                    Program.startfrm.m_szProjektname = frm.m_szProjekt;
                    Program.startfrm.m_ID_Projekt = frm.m_ID_Projekt;
                    Program.startfrm.SetTextProjekt(frm.m_szProjekt);
                }
            }
            else
            {
                if (ctrl.m_szProjektname != "")
                {
                    Program.mainfrm = new FormMain();
                    FormMain frmmain = (FormMain)Program.mainfrm;

                    ctrlproj.ReadSingle(ctrl.m_szProjektname);
                    frmmain.SetProjekt(ctrl.m_szProjektname);
                    frmmain.SetIDProjekt(ctrl.m_ID_Projekt);
                    frmmain.SetKlima(frmmain.GetKlimaregion(ctrlproj.m_ID_Klimaregion));
                    Program.startfrm.SetKlima(frmmain.GetKlimaregion(ctrlproj.m_ID_Klimaregion));
                    frmmain.SetBearbeiter(ctrlproj.m_szBearbeiter);
                    frmmain.SetKunde(ctrlproj.m_szKunde);
                    frmmain.SetAenderungsdatum(ctrlproj.m_Aenderungsdatum);
                    frmmain.SetBeschreibung(ctrlproj.m_szBeschreibung);
                    frmmain.SetWPControl(ctrl.m_szProjektname);
                    frmmain.SetBHKWControl(ctrl.m_szProjektname);
                    frmmain.SetSPControl(ctrl.m_szProjektname);
                    frmmain.SetHeizkesselControl(ctrl.m_szProjektname);
                    frmmain.SetGebaeudeControl(ctrl.m_szProjektname);
                    frmmain.SetWaermebedarfExternControl(frm.m_szProjekt);
                    frmmain.SetProzesswaermeControl(ctrl.m_ID_Projekt);
                    frmmain.SetStrombedarfControl(ctrl.m_ID_Projekt);
                    frmmain.SetStromganglinieControl(ctrl.m_szProjektname);
                    frmmain.SetPVControl(ctrl.m_szProjektname);
                    frmmain.SetPufferSpControl(ctrl.m_szProjektname);
                    frmmain.SetSolarControl(ctrl.m_szProjektname);
                    frmmain.Add_WPKontext();
                    frmmain.Add_BHKWKontext();
                    frmmain.Add_GebäudeKontext();
                    frmmain.Add_SpKontext();
                    frmmain.Add_HeizkesselKontext();
                    frmmain.Add_WaermebedarfExternKontext();
                    frmmain.Add_ProzesswaermeKontext();
                    frmmain.Add_StrombedarfKontext();
                    frmmain.Add_StromganglinieKontext();
                    frmmain.Add_SpKontext();
                    frmmain.Add_PVKontext();
                    frmmain.Add_SolarKontext();

                    frmmain.ShowDialog();

                    Program.startfrm.m_szProjektname = ctrl.m_szProjektname;
                    Program.startfrm.m_ID_Projekt = ctrl.m_ID_Projekt;
                    Program.startfrm.SetTextProjekt(ctrl.m_szProjektname);
                }
            }
        }

        public string ProjektDelete(bool zuletzt = false)
        {
            ProjektCtrl ctrlproj = new ProjektCtrl();
            WErzeugerCtrl ctrlwerz = new WErzeugerCtrl();
            Form_ProjektDelete frm = new Form_ProjektDelete();
            string szProjekt = "";

            DialogResult ret = frm.ShowDialog();
            if (ret == DialogResult.OK && frm.szProjekt != "")
            {
                // --- NEU: MessageBox Sicherheitsabfrage vor dem tatsächlichen Löschen ---
                DialogResult dialogResult = MessageBox.Show(
                    $"Sind Sie sicher, dass Sie das Projekt '{frm.szProjekt}' und alle dazugehörigen Daten unwiderruflich löschen möchten?",
                    "Projekt löschen bestätigen",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2 // Fokus liegt zur Sicherheit auf "Nein"
                );

                // Wenn der Nutzer nicht auf "Ja" klickt, wird der Löschvorgang abgebrochen
                if (dialogResult != DialogResult.Yes)
                {
                    return "";
                }

                try
                {
                    // 1. Unhandlichen OdbcDataAdapter durch sauberes DataRepository.GetDataTable (OLEDB) ersetzt
                    string selectSql = "SELECT * FROM Tab_Applikation";
                    DataTable dt = DataRepository.GetDataTable(selectSql);

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        DataRow row = dt.Rows[0];
                        if (row["ID_Projekt"] != DBNull.Value && Convert.ToInt32(row["ID_Projekt"]) == frm.ID_Projekt)
                        {
                            // 2. Statt speicherintensiven CommandBuilder upzudaten, führen wir ein gezieltes UPDATE per Repository aus
                            string updateSql = "UPDATE Tab_Applikation SET Projektname = ?, ID_Projekt = 0";
                            OleDbParameter pName = new OleDbParameter("?", "");

                            DataRepository.ExecuteNonQuery(updateSql, pName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler beim Zurücksetzen der Tab_Applikation: " + ex.Message);
                    MessageBox.Show($"Fehler beim Zurücksetzen der Applikationsdaten: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return "";
                }

                ctrlwerz.ID_Projekt = frm.ID_Projekt;
                ctrlwerz.Delete();

                ctrlproj.m_szProjektname = frm.szProjekt;
                ctrlproj.Delete(frm.szProjekt);
                szProjekt = frm.szProjekt;

                // --- NEU: Erfolgsmeldung nach erfolgreichem Löschen ---
                MessageBox.Show($"Das Projekt '{szProjekt}' wurde erfolgreich gelöscht.", "Projekt gelöscht", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return szProjekt;
        }

        public void WP_Administration()
        {
            Form_WP frm = new Form_WP();
            frm.ShowDialog();
        }

        public void StromspeicherBearbeiten()
        {
            Form_AdminStromspeicher frm = new Form_AdminStromspeicher();
            frm.ShowDialog();
        }

        public void GebaeudeBearbeiten()
        {
            Form_Gebaeude frm = new Form_Gebaeude();
            frm.m_bAdmin = true;
            frm.SetControls("");
            frm.ShowDialog();
        }

        public void GebaeudetypenBearbeiten()
        {
            Form_EingGebTyp frm = new Form_EingGebTyp();
            frm.SetControls();
            frm.ShowDialog();
        }

        public void WaermebedarfExtern()
        {
            Form_AdminWaermeeinlesen frm = new Form_AdminWaermeeinlesen();
            frm.SetControls();
            frm.ShowDialog();
        }

        public void Prozesswaerme()
        {
            Form_Prozesswaerme_Admin frm = new Form_Prozesswaerme_Admin();
            frm.SetControls("");
            frm.ShowDialog();
        }

        public void Stromverbraucher()
        {
            Form_Stromverbraucher_Admin frm = new Form_Stromverbraucher_Admin();
            frm.SetControls("");
            frm.ShowDialog();
        }

        public void Stromganglinie()
        {
            Form_Stromganglinie_Admin frm = new Form_Stromganglinie_Admin();
            frm.SetControls();
            frm.ShowDialog();
        }

        public void Solarganglinie()
        {
            Form_Solarganglinie_Admin frm = new Form_Solarganglinie_Admin();
            frm.SetControls();
            frm.ShowDialog();
        }

        public void WPImport()
        {
            Form_WP_einlesen frm = new Form_WP_einlesen();
            frm.ShowDialog();
        }

        public void Kessel()
        {
            Form_Heizkessel_Admin frm = new Form_Heizkessel_Admin();
            frm.ShowDialog();
        }

        public void BHKW()
        {
            Form_BHKWAdmin frm = new Form_BHKWAdmin();
            frm.ShowDialog();
        }
        public void Solarkollektoren()
        {
            Form_SolarKollektorenAdmin frm = new Form_SolarKollektorenAdmin();
            frm.ShowDialog();
        }

        public void PV()
        {
            Form_AdminPV frm = new Form_AdminPV();
            frm.ShowDialog();
        }

        public void SPKImport()
        {
            Form_Heizkessel_einlesen frm = new Form_Heizkessel_einlesen();
            frm.ShowDialog();
        }

        public void PufferSPImport()
        {
            Form_PufferSp_einlesen frm = new Form_PufferSp_einlesen();
            frm.ShowDialog();
        }

        public void PufferSp()
        {
            Form_PufferSp_Admin frm = new Form_PufferSp_Admin();
            frm.ShowDialog();
        }

        public void Brauchwasser()
        {
            Form_Brauchwasser_Admin frm = new Form_Brauchwasser_Admin();
            frm.SetControls("");
            frm.ShowDialog();
        }

        public void PVImport()
        {

        }

        public void SolarThermieImport()
        {
            Form_SolarKollektoren_einlesen frm = new Form_SolarKollektoren_einlesen();
            frm.ShowDialog();
        }
    }
}