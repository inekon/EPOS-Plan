using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

public class TabNavigationManager
{
    private TabPage _targetTab;
    private Panel _contentPanel;
    private List<Button> _navButtons = new List<Button>();
    private Color _activeColor = Color.LightSteelBlue;
    private Color _defaultColor = Color.WhiteSmoke;

    public TabNavigationManager(TabPage targetTab)
    {
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

        // 3. Content-Panel (Rechts)
        _contentPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

        // Buttons erstellen
        string[] names = { "Übersicht", "Wärmebedarf", "Strombedarf" };
        for (int i = 0; i < 3; i++)
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

        // Beispiel-Inhalt erzeugen (hier kannst du deine Logik einbauen)
        Control newControl;
        switch (index)
        {
            case 0:
                newControl = new TextBox { Multiline = true, Dock = DockStyle.Fill, Text = "Hier ist die Übersicht..." };
                break;
            case 1:
                newControl = new Label { Text = "Detail-Ansicht aktiviert", AutoSize = true, Font = new Font("Arial", 12, FontStyle.Bold) };
                break;
            default:
                newControl = new CheckBox { Text = "Option A aktivieren", Checked = true };
                break;
        }

        _contentPanel.Controls.Add(newControl);
    }
}