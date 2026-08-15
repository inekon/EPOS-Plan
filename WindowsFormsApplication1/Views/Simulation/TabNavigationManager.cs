using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApplication1;

public interface INavigatableContent
{
    void RefreshContent();
}

public class TabNavigationManager
{
    private TabPage _targetTab;
    private Panel _contentPanel;
    private List<Button> _navButtons = new List<Button>();
    private Color _activeColor = Color.LightSteelBlue;
    private Color _defaultColor = Color.WhiteSmoke;
    public SimulationControl simctrl;

 
    public TabNavigationManager(TabPage targetTab, SimulationControl sim)
    {
        simctrl = sim;
        _targetTab = targetTab;
        SetupLayout();
    }

    private void SetupLayout()
    {
        // 1. SplitContainer für die 10% / 90% Aufteilung
        SplitContainer split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            SplitterDistance = (int)(_targetTab.Width * 0.1), // 10%
            IsSplitterFixed = false
        };

        // 2. Navigations-Panel (Links)
        TableLayoutPanel navLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = Color.FromArgb(240, 240, 240)
        };
        navLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        navLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        navLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        navLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));

        // 3. Content-Panel (Rechts)
        _contentPanel = new Panel {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
        };

        // Buttons erstellen.
        // Die Beschriftungen stehen im Ressourcenkatalog; die .resx legt Zeilenumbrueche
        // als LF ab (XML-Normierung), deshalb hier auf Environment.NewLine zurueckbiegen.
        string[] names = { WindowsFormsApplication1.MyResource.Resource.SIM_NAV_UEBERSICHT.Replace("\n", Environment.NewLine),
                           WindowsFormsApplication1.MyResource.Resource.SIM_NAV_AUTARKIE_ANALYSE.Replace("\n", Environment.NewLine),
                           WindowsFormsApplication1.MyResource.Resource.SIM_NAV_WAERMEPRODUKTION_CHART.Replace("\n", Environment.NewLine),
                           WindowsFormsApplication1.MyResource.Resource.SIM_NAV_STROMPRODUKTION_CHART.Replace("\n", Environment.NewLine) };
        
        for (int i = 0; i < 4; i++)
        {
            Button btn = new Button
            {
                Text = names[i],
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = _defaultColor,
                Tag = i // Index speichern
            };
            btn.Click += NavButton_Click;
            _navButtons.Add(btn);
            navLayout.Controls.Add(btn, 0, i);
        }

        split.Panel1.Controls.Add(navLayout);
        split.Panel2.Controls.Add(_contentPanel);
        _targetTab.Controls.Add(split);

        // Ersten Tab aktivieren
        ShowContent(0);
    }

    private void NavButton_Click(object sender, EventArgs e)
    {
        Button btn = (Button)sender;
        int index = (int)btn.Tag;
        ShowContent(index);
  
    }

    private void ShowContent(int index)
    {
        _contentPanel.Controls.Clear();

        // Button-Farben aktualisieren
        foreach (var b in _navButtons) b.BackColor = _defaultColor;
        _navButtons[index].BackColor = _activeColor;

        // Inhalt erzeugen
        Control newControl = null;

        switch (index)
        {
            case 0:
                var ueb = new NavigatorUebersicht(simctrl);
                newControl = ueb; // Zuerst die Referenz halten
                _contentPanel.Controls.Add(newControl); // Dann zum Panel (wichtig für Koordinaten!)
                newControl.Dock = DockStyle.Fill;

                // JETZT erst die Daten übergeben und berechnen
                ueb.SetControl(simctrl);
                RefreshActivePage();
                break;

            case 1:
                DashboardForm dashForm = new DashboardForm();

                // WICHTIG: Das macht das Formular zu einem "Control"
                //newControl = new Label { Text = "Detail-Ansicht aktiviert", AutoSize = true, Font = new Font("Arial", 12, FontStyle.Bold) };
                //newControl.Size = new Size(_contentPanel.Width - 20, _contentPanel.Height - 20);
                dashForm.TopLevel = false;
                dashForm.FormBorderStyle = FormBorderStyle.None; // Entfernt Rahmen und Titelzeile
                newControl = dashForm; // Jetzt kannst du es wie ein normales Control behandeln
                newControl.Dock = DockStyle.Fill;
                newControl.Location = new Point(10, 10);

                WErzeugerCtrl ctrl = new WErzeugerCtrl();
                int id = 0;
                double speicherKWh = 0; //Standardwert, z.B. 5 kWh

                // alle Sromspeicher zum Projekt durchgehen und Leistung aufsummieren (oder direkt aus sim-Objekt, falls dort schon vorhanden)
                ctrl.ReadAllFilter("ID_Projekt=" + 15 + " and ID_Type=" + WizardItemClass.SP_TYP);
                for (int i = 0; i < ctrl.rows; i++)
                {
                    id = ctrl.items[i].ID_SP;
                    RecordSet rs = new RecordSet();
                    rs.Open("select * from Tab_Stromspeicher where ID=" + id);
                    if (rs.Next())
                    {
                        speicherKWh += (double)rs.Read("Energie");
                    }
                    rs.Close();
                }
                if (speicherKWh == 0) dashForm.speicherKWh = 5; else dashForm.speicherKWh = speicherKWh;
                dashForm.Init();

                // Die theoretische Stromproduktion übergeben, Wirkungsgrad Wechselrichter 5% abgezogen
                dashForm.pvProd = simctrl.simulation_pv.pvPotentialGesamt_stuendlich;
                // Stromprofil + weitere Verbräuche 
                dashForm.stromBedarf = simctrl.simulation_pv.Strombedarf_stuendlich;

                // Solarthermie
                float[] tempST = new float[8760];
                for (int i = 0; i < 8760; i++)
                {
                    tempST[i] = (float)(simctrl.simulation_solarthermie.Waermeproduktion[i] + simctrl.simulation_solarthermie.Ueberschuss[i]);
                }
                dashForm.stProd = tempST;
                dashForm.waermeBedarf = Array.ConvertAll<double, float>(simctrl.simulation_solarthermie.Waermebedarf, x => (float)x);

                dashForm.UpdateSimulationData();
                // WICHTIG: Das Formular explizit sichtbar machen
                dashForm.Show();

                // Optional: Falls es immer noch nicht erscheint, ein BringToFront erzwingen
                dashForm.BringToFront();
                break;
            case 2:
                newControl = new NavigatorWaerme(simctrl);
                newControl.Dock = DockStyle.None;
                newControl.Location = new Point(10, 10);
                newControl.Size = new Size(_contentPanel.Width - 20, _contentPanel.Height - 20);
                break;
            default:
                newControl = new NavigatorStrom(simctrl);
                newControl.Dock = DockStyle.None;
                newControl.Location = new Point(10, 10);
                newControl.Size = new Size(_contentPanel.Width - 20, _contentPanel.Height - 20);
                ((NavigatorStrom)newControl).SetControl(simctrl);
                break;
        }

        if (newControl != null)
        {
            // 1. Zuerst Docking setzen
            newControl.Dock = DockStyle.Fill;

            // 2. Control zum Panel hinzufügen
            _contentPanel.Controls.Add(newControl);

            // 3. HANDLE ERZWINGEN (Dies verhindert die Exception!)
            if (!newControl.IsHandleCreated)
            {
                var ptr = newControl.Handle;
            }

            // 4. Sichtbarkeit sicherstellen
            newControl.Visible = true;
            newControl.BringToFront();

            // 5. Sofortiges Neuzeichnen ohne BeginInvoke (da Handle jetzt existiert)
            newControl.Refresh();
        }


    }

    public void RefreshActivePage()
    {
        // 1. Prüfen, ob überhaupt ein Control im Panel liegt
        if (_contentPanel.Controls.Count > 0)
        {
            // 2. Das oberste Control nehmen
            Control current = _contentPanel.Controls[0];

            // 3. Prüfen: "Bist du ein INavigatableContent?"
            if (current is INavigatableContent navPage)
            {
                // 4. Falls ja: Ruf die Refresh-Methode im UserControl auf
                navPage.RefreshContent();
            }
        }
    }


}