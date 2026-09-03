using System;
using System.Collections.Generic;
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
                case Masken.WpAdministration:
                    using (Form_WP frm = new Form_WP()) return MitOk(frm);

                case Masken.StromspeicherAdmin:
                    using (Form_AdminStromspeicher frm = new Form_AdminStromspeicher()) return MitOk(frm);

                case Masken.GebaeudeAdmin:
                    using (Form_Gebaeude frm = new Form_Gebaeude())
                    {
                        frm.m_bAdmin = true;
                        frm.SetControls("");
                        return MitOk(frm);
                    }

                case Masken.GebaeudetypenAdmin:
                    using (Form_EingGebTyp frm = new Form_EingGebTyp())
                    {
                        frm.SetControls();
                        return MitOk(frm);
                    }

                case Masken.WaermebedarfExternAdmin:
                    using (Form_AdminWaermeeinlesen frm = new Form_AdminWaermeeinlesen())
                    {
                        frm.SetControls();
                        return MitOk(frm);
                    }

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

                case Masken.StromganglinieAdmin:
                    using (Form_Stromganglinie_Admin frm = new Form_Stromganglinie_Admin())
                    {
                        frm.SetControls();
                        return MitOk(frm);
                    }

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

                case Masken.WpImport:
                    using (Form_WP_einlesen frm = new Form_WP_einlesen()) return MitOk(frm);

                case Masken.HeizkesselAdmin:
                    using (Form_Heizkessel_Admin frm = new Form_Heizkessel_Admin()) return MitOk(frm);

                case Masken.BhkwAdmin:
                    using (Form_BHKWAdmin frm = new Form_BHKWAdmin()) return MitOk(frm);

                case Masken.SolarkollektorenAdmin:
                    using (Form_SolarKollektorenAdmin frm = new Form_SolarKollektorenAdmin()) return MitOk(frm);

                case Masken.PvAdmin:
                    using (Form_AdminPV frm = new Form_AdminPV()) return MitOk(frm);

                case Masken.HeizkesselImport:
                    using (Form_Heizkessel_einlesen frm = new Form_Heizkessel_einlesen()) return MitOk(frm);

                case Masken.PufferSpImport:
                    using (Form_PufferSp_einlesen frm = new Form_PufferSp_einlesen()) return MitOk(frm);

                case Masken.PufferSpAdmin:
                    using (Form_PufferSp_Admin frm = new Form_PufferSp_Admin()) return MitOk(frm);

                case Masken.SolarkollektorenImport:
                    using (Form_SolarKollektoren_einlesen frm = new Form_SolarKollektoren_einlesen()) return MitOk(frm);

                // --- Masken mit Argument ---------------------------------------------
                case Masken.PeakShaving:
                    using (Form_PeakShaving frm = new Form_PeakShaving(Ganzzahl(argumente, 0)))
                        return MitOk(frm);

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
                        return LoeschwahlUebernehmen(argumente, frm.ZuLoeschen, frm.SicherungGewuenscht);
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

        /// <summary>
        /// Übergibt das Ergebnis des Löschdialogs — seit dem Umbau auf Häkchenauswahl
        /// eine LISTE statt eines einzelnen Projekts (Nutzerauftrag 02.09.2026).
        /// Ohne Fach im Argument bleibt es beim reinen „mit OK beendet".
        /// </summary>
        private static bool LoeschwahlUebernehmen(object[] argumente, List<ProjektModel> liste,
                                                  bool sicherung)
        {
            Projektloeschwahl fach = argumente != null && argumente.Length > 0
                ? argumente[0] as Projektloeschwahl
                : null;

            if (fach == null) return true;   // Aufrufer will das Ergebnis nicht

            fach.ZuLoeschen = liste ?? new List<ProjektModel>();
            fach.SicherungGewuenscht = sicherung;
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
    }
}
