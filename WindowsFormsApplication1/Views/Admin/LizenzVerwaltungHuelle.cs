using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Lizenz;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Lizenzverwaltung (iU9-W15c.5) — Ersatz für
    /// <c>Views/Admin/Form_LizenzVerwaltung.{cs,Designer.cs,resx}</c>
    /// (302 + 246 + 119 Z.).
    ///
    /// <para><b>Sie hält die Fachseite, die Komponente hält nur die Eingaben.</b>
    /// Alles, was der Vorläufer über <see cref="LizenzManager"/>,
    /// <see cref="LizenzServerClient"/> und <see cref="GeraeteId"/> tat, läuft hier
    /// über <c>LizenzCtrl</c>; die Komponente bekommt ein Lagebild
    /// (<see cref="LizenzGaben"/>) und fünf Delegaten. Das ist keine Bequemlichkeit,
    /// sondern Regel S-2 der Welle: Auf iOS liest <c>Pruefe()</c> den Schlüsselbund
    /// SYNCHRON, und eine Razor-Komponente ruft immer vom Zeichenfaden (Befund
    /// W15c-B13).</para>
    ///
    /// <para><b>Der Trial-Name kommt von hier</b> (Entscheid E-16): Unter Windows ist
    /// es der Anmeldename, wie im Bestand (<c>Environment.UserName</c>,
    /// <c>Form_LizenzVerwaltung.cs:226</c>); auf iOS wäre es der Gerätename. Der
    /// Server nimmt ihn als reine Zusatzangabe, die Anmeldung läuft über die
    /// E-Mail.</para>
    ///
    /// <para><b>Zwei Rollen, ein Parametersatz.</b> <see cref="Oeffnen"/> zeigt die
    /// Verwaltung als eigenes Fenster (Menü Administration → Lizenz…);
    /// <see cref="Gaben"/> liefert denselben Satz ohne <c>Geschlossen</c>, damit der
    /// Lizenzdialog sie als ÜBERLAGERUNG zeigen kann statt als zweites Fenster
    /// (Risiko R2, Entscheid E-11).</para>
    /// </summary>
    internal static class LizenzVerwaltungHuelle
    {
        /// <summary>
        /// Gewünschtes Innenmaß. Der Vorläufer war ein FixedDialog mit 560 × 486; die
        /// Razor-Fassung braucht mehr Höhe für dieselben drei Gruppen, weil ihre
        /// Berührungsziele 44 px hoch sind (Entscheid E-13).
        /// </summary>
        private static readonly Size MASS = new Size(700, 620);

        /// <summary>Öffnet die Lizenzverwaltung als eigenes Fenster.</summary>
        internal static void Oeffnen(IWin32Window besitzer)
        {
            BlazorDialogForm<LizenzVerwaltungDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create(
                    new object(), () => { if (dlg != null) dlg.Schliessen(true); })
            };

            dlg = new BlazorDialogForm<LizenzVerwaltungDialog>(Titel(), MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
        }

        /// <summary>
        /// Der Fenstertitel — <c>LIZ_TITEL</c> plus Produktname. Der Produktname ist
        /// eine Anwendungskonstante und kein Übersetzungsgut; zusammengesetzt wird er
        /// wie im Vorläufer (<c>TexteSetzen</c>, <c>:101</c>).
        /// </summary>
        internal static string Titel()
            => MyResource.Resource.LIZ_TITEL + " — " + MDIMainForm.PRODUKTNAME;

        /// <summary>
        /// Der PARAMETERSATZ ohne <c>Geschlossen</c> — für das eigene Fenster wie für
        /// die Überlagerung im Lizenzdialog.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Lage"] = Lage(),
                ["EmailVorgabe"] = LizenzManager.Token?.Benutzer ?? "",

                ["Aktivierenweg"] = (Func<string, string, Task<(bool Ok, string Meldung)>>)
                                    LizenzCtrl.Aktivieren,
                ["LicLesen"] = (Func<Task<(string Schluessel, string Email, string Meldung)>>)LicWaehlen,
                ["Trialweg"] = (Func<string, Task<(bool Ok, string Meldung)>>)Trial,
                ["Freigebenweg"] = (Func<Task<(bool Ok, bool Netzfehler, string Meldung)>>)
                                   LizenzCtrl.Freigeben,
                ["Auffrischen"] = (Func<Task<LizenzGaben>>)(() => Task.FromResult(Lage())),
                ["EmailPruefen"] = (Func<string, bool>)LizenzCtrl.EmailGueltig,

                ["TitelText"] = Titel(),
                ["GruppeStatus"] = MyResource.Resource.LIZ_GRP_STATUS,
                ["GruppeAktivieren"] = MyResource.Resource.LIZ_GRP_AKTIVIEREN,
                ["GruppeAktionen"] = MyResource.Resource.LIZ_GRP_AKTIONEN,
                ["LabelSchluessel"] = MyResource.Resource.LIZ_LBL_SCHLUESSEL,
                ["LabelEmail"] = MyResource.Resource.LIZ_LBL_EMAIL,
                ["KnopfLic"] = MyResource.Resource.LIZ_BTN_LIC,
                ["KnopfAktivieren"] = MyResource.Resource.LIZ_BTN_AKTIVIEREN,
                ["KnopfTrial"] = MyResource.Resource.LIZ_BTN_TRIAL,
                ["KnopfFreigeben"] = MyResource.Resource.LIZ_BTN_FREIGEBEN,
                ["KnopfSchliessen"] = MyResource.Resource.LIZ_BTN_SCHLIESSEN,
                ["HinweisAktivierung"] = MyResource.Resource.LIZ_HINWEIS_AKTIVIERUNG,
                ["LinkPortal"] = MyResource.Resource.LIZ_LINK_PORTAL,
                ["MsgEingabeFehlt"] = MyResource.Resource.LIZ_MSG_EINGABE_FEHLT,
                ["MsgEmailUngueltig"] = MyResource.Resource.LIZ_MSG_EMAIL_UNGUELTIG,
                ["MsgAktiviert"] = MyResource.Resource.LIZ_MSG_AKTIVIERT,
                ["MsgAktivierungFehler"] = MyResource.Resource.LIZ_MSG_AKTIVIERUNG_FEHLER,
                ["MsgLicOhneSchluessel"] = MyResource.Resource.LIZ_MSG_LIC_OHNE_SCHLUESSEL,
                ["MsgTrialEmail"] = MyResource.Resource.LIZ_MSG_TRIAL_EMAIL,
                ["MsgTrialOk"] = MyResource.Resource.LIZ_MSG_TRIAL_OK,
                ["MsgTrialFehler"] = MyResource.Resource.LIZ_MSG_TRIAL_FEHLER,
                ["MsgFreigebenFrage"] = MyResource.Resource.LIZ_MSG_FREIGEBEN_FRAGE,
                ["MsgServerNichtErreichbar"] = MyResource.Resource.LIZ_MSG_SERVER_NICHT_ERREICHBAR,
                ["StatusAktivierung"] = MyResource.Resource.LIZ_STATUS_AKTIVIERUNG,
                ["StatusTrial"] = MyResource.Resource.LIZ_STATUS_TRIAL,
                ["StatusFreigabe"] = MyResource.Resource.LIZ_STATUS_FREIGABE,
                ["HinweisLicGeladen"] = MyResource.Resource.LIZ_HINWEIS_LIC_GELADEN,
                ["JaText"] = MyResource.Resource.ALLG_BTN_JA,
                ["NeinText"] = MyResource.Resource.ALLG_BTN_NEIN,
            };
        }

        /// <summary>Das Lagebild aus dem Kern — fünf Anzeigewerte, kein Token (Regel S-3).</summary>
        private static LizenzGaben Lage()
        {
            return new LizenzGaben(
                LizenzCtrl.Zustandsname(),
                LizenzCtrl.Statustext(),
                LizenzCtrl.Detailtext(),
                LizenzCtrl.HatToken,
                LizenzCtrl.PortalUrl);
        }

        /// <summary>
        /// „Lizenzdatei (.lic)…": Der WÄHLER gehört der Plattform
        /// (<c>Dienste.Datei.DateiOeffnen</c>), das LESEN dem Kern. Ein abgebrochener
        /// Wähler liefert leere Werte OHNE Meldung — dann bleibt in der Komponente
        /// alles, wie es war (bitgleich zu <c>if (dialog.ShowDialog(this) !=
        /// DialogResult.OK) return;</c>).
        /// </summary>
        private static Task<(string Schluessel, string Email, string Meldung)> LicWaehlen()
        {
            string pfad = Dienste.Datei.DateiOeffnen(MyResource.Resource.LIZ_DLG_LIC_TITEL,
                                                     MyResource.Resource.LIZ_DLG_LIC_FILTER, null);
            if (string.IsNullOrEmpty(pfad))
                return Task.FromResult(("", "", ""));

            var (schluessel, email) = LizenzCtrl.LicLesen(pfad);
            return Task.FromResult((schluessel, email,
                                    string.IsNullOrWhiteSpace(schluessel)
                                        ? MyResource.Resource.LIZ_MSG_LIC_OHNE_SCHLUESSEL
                                        : ""));
        }

        /// <summary>
        /// „Testversion anfordern…" mit dem Namen dieses Arbeitsplatzes (E-16). Der
        /// Bestand schickte <c>Environment.UserName</c>; das bleibt unter Windows so.
        /// </summary>
        private static Task<(bool Ok, string Meldung)> Trial(string email)
            => LizenzCtrl.Trial(email, Environment.UserName);
    }
}
