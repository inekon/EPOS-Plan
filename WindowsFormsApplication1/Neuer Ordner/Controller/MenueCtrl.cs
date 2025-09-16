using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.Odbc;
using System.Data;

namespace WindowsFormsApplication1
{
    class MenueCtrl
    {
        public WizardParent wizparent;

        public MenueCtrl()
        {
            wizparent = null;
        }

        public Form OpenForm(Type clazz, bool bChildWindow=true)
        {
            if (bChildWindow)
            {
                MDIHelperClass openFrm = new MDIHelperClass();
                Form mdichild = openFrm.openForm(clazz, Program.mdifrm);
                return mdichild;
            }
            else
            {
                Object theObject = Activator.CreateInstance(clazz);
                Form openFrm = (Form)theObject;
                openFrm.Show();
                openFrm.Focus();
                return openFrm;
            }
       }
       
       public void SetProjektname()
       {
            ApplikationCtrl ctrl = new ApplikationCtrl();
            ctrl.ReadSingle("Select * from Tab_Applikation where ID=1");
            FormMain frm = (FormMain)Program.mainfrm;
            frm.SetProjekt(ctrl.m_szProjektname);
       }
       
       public void ProjektNeu()
       {
            List<WizardItemClass> frm = new List<WizardItemClass>();
            frm.Add(new WizardItemClass(new Wizard_Komponenten(), WizardItemClass.KOMPONENTEN_ITEM));
            frm.Add(new WizardItemClass(new Wizard_Projekt(), WizardItemClass.PROJEKT_ITEM));
            frm.Add(new WizardItemClass(new Wizard_Klima(), WizardItemClass.KLIMA_ITEM));
            frm.Add(new WizardItemClass(new Wizard_Gebaeude(), WizardItemClass.GEBAEUDE_ITEM));
            frm.Add(new WizardItemClass(new Wizard_WP(), WizardItemClass.WP_ITEM));
            frm.Add(new WizardItemClass(new Wizard_Solar(), WizardItemClass.SOLAR_ITEM));
            frm.Add(new WizardItemClass(new Wizard_PV(), WizardItemClass.PV_ITEM));
            frm.Add(new WizardItemClass(new Wizard_Sp(), WizardItemClass.SP_ITEM));
            frm.Add(new WizardItemClass(new Wizard_Referenz(), WizardItemClass.REFERENZ_ITEM));
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
            frm.Add(new WizardItemClass(new Wizard_Klima(),WizardItemClass.KLIMA_ITEM));
            frm.Add(new WizardItemClass(new Wizard_Gebaeude(), WizardItemClass.GEBAEUDE_ITEM));
            frm.Add(new WizardItemClass(new Wizard_WP(), WizardItemClass.WP_ITEM));
            frm.Add(new WizardItemClass(new Wizard_Solar(), WizardItemClass.SOLAR_ITEM));
            frm.Add(new WizardItemClass(new Wizard_PV(), WizardItemClass.PV_ITEM));
            frm.Add(new WizardItemClass(new Wizard_Sp(), WizardItemClass.SP_ITEM));
            frm.Add(new WizardItemClass(new Wizard_Referenz(), WizardItemClass.REFERENZ_ITEM));
            wizparent = new WizardParent(frm);
            Program.wizardctrl.parentform = wizparent;
            wizparent.SetWizardMode(WizardParent.WIZARD_MODE_BEARBEITEN);
            wizparent.ShowDialog();
            if (wizparent.gespeichert)
            {
                MessageBox.Show("Daten gespeichert");
            }
       }

       public void ProjektOeffnen(bool zuletzt=false)
       {
           Form_ProjektOpen frm = new Form_ProjektOpen();
           ApplikationCtrl ctrl = new ApplikationCtrl();
           ProjektCtrl ctrlproj = new ProjektCtrl();

           ctrl.ReadSingle("Select * from Tab_Applikation where ID=1");

           if (!zuletzt)
           {
               DialogResult ret = frm.ShowDialog();
               if (ret == DialogResult.OK)
               {
                   Program.mainfrm = (FormMain)Program.menuectrl.OpenForm(typeof(FormMain), true);
                   FormMain frmmain = (FormMain)Program.mainfrm;
                    
                   ctrlproj.ReadSingle("Select * from Tab_Projekt where ID=" + frm.m_ID_Projekt);
                   frmmain.SetProjekt(frm.m_szProjekt);
                   frmmain.SetIDProjekt(frm.m_ID_Projekt);
                   frmmain.SetKlima(frm.m_szKlimaregion);
                   frmmain.SetBearbeiter(ctrlproj.m_szBearbeiter);
                   frmmain.SetAenderungsdatum(ctrlproj.m_Aenderungsdatum);
                   frmmain.SetBeschreibung(ctrlproj.m_szBeschreibung);                
                   frmmain.SetKunde(ctrlproj.m_szKunde); 
                   frmmain.SetWPControl(frm.m_szProjekt);
                   frmmain.SetSPControl(frm.m_szProjekt);
                   frmmain.SetHeizkesselControl(frm.m_szProjekt);
                   frmmain.SetGebaeudeControl(frm.m_szProjekt);
                   frmmain.SetWaermebedarfExternControl(frm.m_szProjekt); 
                   frmmain.Add_WPKontext();
                   frmmain.Add_GebäudeKontext();
                   frmmain.Add_HeizkesselKontext();
                   frmmain.Add_WaermebedarfExternKontext();
                   frmmain.SetNetzverluste(ctrlproj.m_nNetzverluste, ctrlproj.m_szEinheit);
               }
           }
           else
           {
               // falls zuletzt geöffnetes Projekt gelöscht wurde
               if (ctrl.m_szProjektname != "")
               {
                   Program.mainfrm = (FormMain)Program.menuectrl.OpenForm(typeof(FormMain), true);
                   FormMain frmmain = (FormMain)Program.mainfrm;

                   ctrlproj.ReadSingle("Select * from Tab_Projekt where ID=" + ctrl.m_ID_Projekt);
                   frmmain.SetProjekt(ctrl.m_szProjektname);
                   frmmain.SetIDProjekt(ctrl.m_ID_Projekt);
                   frmmain.SetKlima(frmmain.GetKlimaregion(ctrlproj.m_ID_Klimaregion));
                   frmmain.SetBearbeiter(ctrlproj.m_szBearbeiter);
                   frmmain.SetKunde(ctrlproj.m_szKunde);
                   frmmain.SetAenderungsdatum(ctrlproj.m_Aenderungsdatum);
                   frmmain.SetBeschreibung(ctrlproj.m_szBeschreibung); 
                   frmmain.SetWPControl(ctrl.m_szProjektname);
                   frmmain.SetSPControl(ctrl.m_szProjektname);
                   frmmain.SetHeizkesselControl(ctrl.m_szProjektname);
                   frmmain.SetGebaeudeControl(ctrl.m_szProjektname);
                   frmmain.SetWaermebedarfExternControl(frm.m_szProjekt); 
                   frmmain.Add_WPKontext();
                   frmmain.Add_GebäudeKontext();
                   frmmain.Add_SpKontext();
                   frmmain.Add_HeizkesselKontext();
                   frmmain.Add_WaermebedarfExternKontext();
                   frmmain.SetNetzverluste(ctrlproj.m_nNetzverluste, ctrlproj.m_szEinheit);  
               }
           }
       }

       public void ProjektDelete(bool zuletzt = false)
       {
            ProjektCtrl ctrlproj = new ProjektCtrl();
            WErzeugerCtrl ctrlwerz = new WErzeugerCtrl();
            ApplikationCtrl ctrlapp = new ApplikationCtrl();
            Form_ProjektDelete frm = new Form_ProjektDelete();
 
            DialogResult ret = frm.ShowDialog();
            if (ret == DialogResult.OK)
            {
                try
                {
                    OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Tab_Applikation", Program.DBConnection);
                    DataSet dataSet = new DataSet();
                    
                    adapter.Fill(dataSet, "Tab_Applikation");

                    DataRow row = dataSet.Tables["Tab_Applikation"].Rows[0];
                    if ((int)row["ID_Projekt"] == frm.ID_Projekt)
                    {
                        row["Projektname"] = "";
                        row["ID_Projekt"] = 0;
                        row["ID_Klimaregion"] = 0;
                        row["Klimaregion"] = "";
                        OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                        adapter.Update(dataSet, "Tab_Applikation");
                    }
                }
                catch (OdbcException sqlEx)
                {
                    // Fehler beim Datenbankzugriff abfangen
                    Console.WriteLine("SQL Fehler: " + sqlEx.Message);
                    return;
                }
                catch (Exception ex)
                {
                    // Allgemeine Fehler abfangen
                    Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
                    return;
                }

                ctrlwerz.ID_Projekt = frm.ID_Projekt;
                ctrlwerz.Delete();

                ctrlproj.m_szProjektname = frm.szProjekt;
                ctrlproj.Delete(frm.szProjekt);
            }
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
           Form_Waermeeinlesen frm = new Form_Waermeeinlesen();
           frm.m_bAdmin = true;
           frm.SetControls(""); 
           frm.ShowDialog(); 
       }

     }
}
