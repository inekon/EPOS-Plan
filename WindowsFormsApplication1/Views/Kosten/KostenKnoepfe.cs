using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Serienbaustein der Etappe KD6 (Konzept Kostendialoge § 9, Folien 6/7):
    /// der Block „Kosten" mit den drei Aufrufen „Investitionskosten…",
    /// „Betriebskosten…", „Energiekosten…" für die Anlagen- und
    /// Geräte-Eigenschaften-Dialoge — EIN Helfer, damit jeder weitere Dialog
    /// ein Einzeiler bleibt.
    ///
    /// <para><b>Kontexte:</b> Mit <c>projektId &gt; 0</c> (Projekt-Anlagendialog)
    /// öffnen Invest/Betrieb den reduzierten Kosteneditor (<see cref="Form_Kosten"/>,
    /// § 6.4) mit vorgewählter Komponente; im Stammkontext der Gerätedatenbank
    /// (<c>projektId = 0</c>) die Stammvorlage (<see cref="Form_KostenKomponente"/>).
    /// „Energiekosten…" führt in die Energieträgerverwaltung, im Projektkontext
    /// vorgefiltert auf den Träger.</para>
    ///
    /// <para><b>FK8 (entschieden 25.08.2025):</b> <see cref="Sperren"/> setzt die
    /// alten eingebetteten Kostenfelder eine Version lang schreibgeschützt — mit
    /// dem Hinweis „gepflegt wird im Kostendialog" — statt sie sofort zu
    /// entfernen (keine Funktionslücke).</para>
    /// </summary>
    internal static class KostenKnoepfe
    {
        /// <summary>
        /// Baut die Knopfleiste (drei Knöpfe nebeneinander, Höhe 40) und liefert
        /// sie zum Platzieren — der Dialog entscheidet, wo sie sitzt (typisch:
        /// <c>Dock = Bottom</c> plus etwas mehr Fensterhöhe).
        /// </summary>
        /// <param name="eigner">Formular für <c>ShowDialog</c>-Ownership</param>
        /// <param name="komponente">Tab_KostenKomponente-Name (DbWerte.KOSTEN_KOMPONENTE_*)</param>
        /// <param name="projektId">zur KLICKZEIT ausgewertet (Projektdialoge setzen ihr
        /// Projekt oft erst nach dem Konstruktor); 0 = Stammkontext</param>
        /// <param name="carrierId">Träger-Vorfilter, zur Klickzeit; null = keiner</param>
        /// <param name="fk8Hinweis">optionaler FK8-Hinweistext rechts in der Leiste</param>
        public static Panel Leiste(Form eigner, string komponente,
                                   Func<int> projektId, Func<int?> carrierId,
                                   string fk8Hinweis = null)
        {
            var leiste = new Panel { Height = 40 };

            Button invest = Knopf(T("KDLG_KNOPF_INVEST", "Investitionskosten…"), 8);
            invest.Click += (s, e) => OeffneKosten(eigner, komponente, projektId(), betrieb: false);
            leiste.Controls.Add(invest);

            Button betrieb = Knopf(T("KDLG_KNOPF_BETRIEB", "Betriebskosten…"), 8 + 158);
            betrieb.Click += (s, e) => OeffneKosten(eigner, komponente, projektId(), betrieb: true);
            leiste.Controls.Add(betrieb);

            Button energie = Knopf(T("KDLG_KNOPF_ENERGIE", "Energiekosten…"), 8 + 316);
            energie.Click += (s, e) =>
            {
                using (var dlg = new Form_Energietraeger())
                {
                    dlg.SetControls(projektId());
                    int? traeger = carrierId != null ? carrierId() : null;
                    if (traeger.HasValue) dlg.WaehleTraeger(traeger.Value);
                    dlg.ShowDialog(eigner);
                }
            };
            leiste.Controls.Add(energie);

            if (!string.IsNullOrEmpty(fk8Hinweis))
            {
                // Kurzform in der Leiste (der Platz rechts ist knapp), Vollsatz
                // als Tooltip - die Botschaft "hier nur lesen" bleibt sichtbar.
                var lbl = new Label
                {
                    AutoSize = true,
                    ForeColor = Color.Firebrick,
                    Location = new Point(8 + 480, 12),
                    Text = T("KDLG_FK8_KURZ", "Gepflegt wird im Kostendialog.")
                };
                new ToolTip().SetToolTip(lbl, fk8Hinweis);
                leiste.Controls.Add(lbl);
            }

            return leiste;
        }

        private static Button Knopf(string text, int x)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, 6),
                Size = new Size(150, 28),
                UseVisualStyleBackColor = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
        }

        private static void OeffneKosten(Form eigner, string komponente, int projektId, bool betrieb)
        {
            if (projektId > 0)
            {
                // KD6a: Der Projektkontext läuft über den NEUEN Kostendialog —
                // Form_Kosten bleibt nur noch Logikträger (LiesKomponentenSummen u. a.).
                using (var dlg = new Form_KostenKomponente())
                {
                    dlg.SetProjekt(projektId, ProjektName(projektId), komponente, betrieb);
                    dlg.ShowDialog(eigner);
                }
            }
            else
            {
                using (var dlg = new Form_KostenKomponente())
                {
                    dlg.SetControls(komponente);
                    if (betrieb) dlg.WaehleBetrieb();
                    dlg.ShowDialog(eigner);
                }
            }
        }

        /// <summary>
        /// FK8: Bestandsfelder schreibschützen — TextBoxen bleiben lesbar/kopierbar
        /// (ReadOnly), alles andere wird deaktiviert. Der Pflegeort-Hinweis läuft
        /// über die Leiste (<see cref="Leiste"/>, Parameter <c>fk8Hinweis</c>).
        /// </summary>
        public static void Sperren(params Control[] felder)
        {
            foreach (Control c in felder)
            {
                if (c == null) continue;
                var tb = c as TextBoxBase;
                if (tb != null) tb.ReadOnly = true;
                else c.Enabled = false;
            }
        }

        private static string ProjektName(int projektId)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT Projektname FROM Tab_Projekt WHERE ID = ?",
                    new System.Data.OleDb.OleDbParameter("@p", projektId));
                return o == null || o == DBNull.Value ? "" : Convert.ToString(o);
            }
            catch { return ""; }
        }

        /// <summary>Der FK8-Standardhinweis (für die Leiste).</summary>
        public static string Fk8Hinweis()
        {
            return T("KDLG_FK8_HINWEIS", "Gepflegt wird im Kostendialog — Felder schreibgeschützt.");
        }

        /// <summary>
        /// Träger-Vorfilter für „Energiekosten…" im Projektkontext (§ 9,
        /// Folie 26): der Energieträger der Anlagenzeile(n) der Komponente —
        /// bei mehreren Zeilen die jüngste. null = keine Anlage/kein Träger.
        /// </summary>
        public static int? TraegerDerKomponente(int projektId, string geraeteSpalte)
        {
            if (projektId <= 0) return null;
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT MAX(ID_Carrier) FROM Tab_Energieanlagen " +
                    "WHERE ID_Projekt = ? AND [" + geraeteSpalte + "] IS NOT NULL " +
                    "AND ID_Carrier IS NOT NULL",
                    new System.Data.OleDb.OleDbParameter("@p", projektId));
                if (o == null || o == DBNull.Value) return null;
                int id = Convert.ToInt32(o);
                return id > 0 ? (int?)id : null;
            }
            catch { return null; }
        }

        private static string T(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
        }
    }
}
