using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Lizenz;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Lizenzdialogs (iU9-W15c.11) — Ersatz für
    /// <c>Views/Help/Form_Lizenz.cs</c> (1 013 Z., ohne Designer, ohne <c>.resx</c>).
    ///
    /// <para><b>Zwei Einstiege, eine Komponente</b> — genau wie im Vorläufer, dessen
    /// Konstruktor einen <c>bool</c> nahm:</para>
    /// <list type="bullet">
    ///   <item><description><see cref="Anzeigen"/> — Menü „Hilfe → Lizenz", modal über
    ///     dem Hauptfenster.</description></item>
    ///   <item><description><see cref="ZustimmungSicherstellen"/> — die EULA-Abfrage
    ///     beim ersten Start, <b>besitzerlos</b> aus <c>Program.Main</c>.</description></item>
    /// </list>
    ///
    /// <para><b>Der Fehlerpfad der Zustimmung ist wortgleich übernommen</b> (Entscheid
    /// E-15, Befund W15c-B18): Eine nicht lesbare Ablage blockiert den Start NICHT —
    /// die Prüfung steht in <c>ZustimmungCtrl.IstZugestimmt</c>, und dort lautet der
    /// Rückgabewert im <c>catch</c> <c>true</c>.</para>
    ///
    /// <para><b>Alles Plattformgebundene steht hier</b>: die Dateiwahl über
    /// <c>Dienste.Datei</c>, die Programmfassung über das Assembly, und der
    /// Parametersatz der Lizenzverwaltung für die Überlagerung (E-11). Die Komponente
    /// kennt weder <c>LizenzManager</c> noch <c>LizenzTextCtrl</c>.</para>
    /// </summary>
    internal static class LizenzHuelle
    {
        /// <summary>
        /// Gewünschtes Innenmaß. Der Vorläufer stand auf 920 × 700 (min 660 × 480);
        /// die Razor-Fassung braucht etwas mehr Breite für dieselben drei Reiter
        /// (Entscheid E-13).
        /// </summary>
        private static readonly Size MASS = new Size(980, 760);

        /// <summary>Kleinstmaß wie im Vorläufer.</summary>
        private static readonly Size MINDEST = new Size(660, 480);

        // ==================================================================
        //  Die zwei Einstiege
        // ==================================================================

        /// <summary>Menüeinstieg „Hilfe → Lizenz".</summary>
        internal static void Anzeigen(IWin32Window besitzer)
        {
            Zeigen(besitzer, zustimmungsmodus: false);
        }

        /// <summary>
        /// Prüft, ob der Lizenzvereinbarung bereits zugestimmt wurde, und holt die
        /// Zustimmung andernfalls nach. <c>false</c> bedeutet: abgelehnt — das Programm
        /// wird dann beendet.
        /// </summary>
        /// <remarks>
        /// Aufruf beim Programmstart, in <c>Program.Main</c> vor dem Öffnen des
        /// Hauptfensters. <b>Besitzerlos</b>: Es gibt zu diesem Zeitpunkt kein Fenster.
        /// </remarks>
        internal static bool ZustimmungSicherstellen(IWin32Window besitzer = null)
        {
            if (ZustimmungCtrl.IstZugestimmt()) return true;

            return Zeigen(besitzer, zustimmungsmodus: true);
        }

        /// <summary>Der gemeinsame Weg beider Einstiege.</summary>
        private static bool Zeigen(IWin32Window besitzer, bool zustimmungsmodus)
        {
            bool ergebnis = false;
            BlazorDialogForm<LizenzDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben(zustimmungsmodus))
            {
                ["Zugestimmt"] = EventCallback.Factory.Create(
                    new object(), () => ZustimmungCtrl.Merken(Fassung(), DateTime.Now)),

                ["Geschlossen"] = EventCallback.Factory.Create<bool>(
                    new object(), ok =>
                    {
                        ergebnis = ok;
                        if (dlg != null) dlg.Schliessen(ok);
                    })
            };

            string titel = zustimmungsmodus
                ? MyResource.Resource.LIZR_TITEL_ZUSTIMMUNG
                : MyResource.Resource.LIZR_TITEL;

            dlg = new BlazorDialogForm<LizenzDialog>(titel, MASS, werte)
            {
                Mindestmass = MINDEST,
            };

            if (besitzer == null)
            {
                // Der Zustimmungsweg laeuft VOR jedem anderen Fenster: kein Besitzer,
                // dafuer Taskleisteneintrag und Bildschirmmitte (die Zusaetze aus
                // W15c.6).
                dlg.ImTaskbar = true;
                dlg.AufBildschirmMittig = true;
            }

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }

            return ergebnis;
        }

        // ==================================================================
        //  Der Parametersatz
        // ==================================================================

        /// <summary>Der Parametersatz ohne <c>Geschlossen</c> und <c>Zugestimmt</c>.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(bool zustimmungsmodus)
        {
            (LizenzTextGaben text, bool ausDatei) = TextLage();

            var werte = new Dictionary<string, object>
            {
                ["Zustimmungsmodus"] = zustimmungsmodus,
                ["Text"] = text,
                ["Lizenzstatus"] = Lizenzstatus(),
                ["Hinweise"] = Hinweise(),
                ["Komponenten"] = Komponenten(),

                ["DateiWaehlen"] = (Func<Task<LizenzTextGaben>>)DateiWaehlen,
                ["Speichern"] = (Func<string, string, Task<string>>)SpeichernUnter,
                ["VerwaltungGaben"] = LizenzVerwaltungHuelle.Gaben(),

                // Die 18 Beschriftungen als EIN Satz (offener Punkt W15c-O-2,
                // umgesetzt 04.09.2026). Das Buendel holt sie selbst aus
                // MyResource.LIZR_* in der Oberflaechensprache; hier ist nichts
                // mehr einzeln zu setzen.
                ["Texte"] = new LizenzTexte(),
            };

            // Nachgeliefert wird die Online-Fassung NUR, wenn keine oertliche Datei
            // gefunden wurde - genau wie im Vorlaeufer (LizenzLaden, :311-333).
            if (!ausDatei)
                werte["OnlineNachladen"] = (Func<Task<LizenzTextGaben>>)OnlineNachladen;

            return werte;
        }

        // ==================================================================
        //  Der Vertragstext
        // ==================================================================

        /// <summary>
        /// Die erste Registerkarte beim Öffnen: örtliche Datei, sonst
        /// Zwischenspeicher, sonst der Ladehinweis. Die Reihenfolge ist unverändert.
        /// </summary>
        private static (LizenzTextGaben Gaben, bool AusDatei) TextLage()
        {
            string treffer = LizenzTextCtrl.DateiSuchen();

            if (treffer != null)
            {
                // E-1: .rtf und .docx zeigen DENSELBEN Hinweistext - eine RTF-Anzeige
                // gibt es in HTML nicht, und der Normalfall ist ohnehin die
                // Online-Fassung.
                return (new LizenzTextGaben(
                            string.Format(MyResource.Resource.LIZR_TEXT_DATEI, treffer),
                            treffer, ""), true);
            }

            string zwischen = LizenzTextCtrl.ZwischenspeicherLesen(out string stand);
            if (!string.IsNullOrEmpty(zwischen))
                return (new LizenzTextGaben(zwischen, LizenzTextCtrl.ONLINE_FASSUNG, stand ?? ""), false);

            return (new LizenzTextGaben(
                        string.Format(MyResource.Resource.LIZR_TEXT_ONLINE_LAEDT,
                                      LizenzTextCtrl.ONLINE_FASSUNG),
                        LizenzTextCtrl.ONLINE_FASSUNG, ""), false);
        }

        /// <summary>Die Online-Fassung nachliefern; <c>null</c> = es bleibt beim Stand.</summary>
        private static async Task<LizenzTextGaben> OnlineNachladen()
        {
            var (text, stand) = await LizenzTextCtrl.OnlineFassungHolen();
            if (string.IsNullOrEmpty(text)) return null;

            return new LizenzTextGaben(text, LizenzTextCtrl.ONLINE_FASSUNG, stand ?? "");
        }

        /// <summary>
        /// „Datei wählen…": Der Wähler gehört der Plattform, der gemerkte Pfad den
        /// Einstellungen. <c>null</c> = abgebrochen.
        /// </summary>
        private static async Task<LizenzTextGaben> DateiWaehlen()
        {
            string start = null;
            try
            {
                string bisher = LizenzTextCtrl.GewaehltenPfadLesen();
                if (!string.IsNullOrEmpty(bisher) && File.Exists(bisher))
                    start = Path.GetDirectoryName(bisher);
            }
            catch { }

            // Der Wähler läuft HINTER dem Blazor-Ereignis (Befund W13‑B‑1,
            // siehe IDateiDienst).
            string pfad = await Dienste.Datei.DateiOeffnenAsync(
                MyResource.Resource.LIZR_DLG_WAEHLEN_TITEL,
                MyResource.Resource.LIZR_DLG_WAEHLEN_FILTER,
                start);
            if (string.IsNullOrEmpty(pfad)) return null;

            LizenzTextCtrl.GewaehltenPfadSpeichern(pfad);

            return new LizenzTextGaben(
                string.Format(MyResource.Resource.LIZR_TEXT_DATEI, pfad), pfad, "");
        }

        /// <summary>
        /// „Speichern unter…": Der Inhalt der aktiven Registerkarte geht in eine
        /// Textdatei. <b>Kein <c>RichTextBox.SaveFile</c> mehr</b> (E-1) — gespeichert
        /// wird, was auf dem Bildschirm steht.
        /// </summary>
        private static async Task<string> SpeichernUnter(string reiter, string inhalt)
        {
            bool istVertrag = reiter == "VERTRAG";
            string name = reiter switch
            {
                "HINWEISE" => MyResource.Resource.LIZR_DATEI_HINWEISE,
                "KOMPONENTEN" => MyResource.Resource.LIZR_DATEI_KOMPONENTEN,
                _ => MyResource.Resource.LIZR_DATEI_VERTRAG
            };

            // Auch der Speichern-Wähler läuft HINTER dem Blazor-Ereignis
            // (Befund W13‑B‑1, siehe IDateiDienst).
            string ziel = await Dienste.Datei.DateiSpeichernAsync(
                istVertrag ? MyResource.Resource.LIZR_DLG_SPEICHERN_VERTRAG_TITEL
                           : MyResource.Resource.LIZR_DLG_SPEICHERN_TEXT_TITEL,
                MyResource.Resource.LIZR_DLG_SPEICHERN_TEXT_FILTER,
                name + ".txt");

            if (string.IsNullOrEmpty(ziel)) return null;

            try
            {
                File.WriteAllText(ziel, inhalt ?? "");
                return ziel;
            }
            catch (Exception ex)
            {
                await Dienste.Dialog.WarnungAsync(
                    string.Format(MyResource.Resource.LIZR_MSG_SPEICHERN_FEHLER, ex.Message));
                return null;
            }
        }

        // ==================================================================
        //  Die drei Textblöcke
        // ==================================================================

        /// <summary>Der Lizenzstand für die Fußzeile; unlesbar ⇒ Ersatztext.</summary>
        private static string Lizenzstatus()
        {
            try { return LizenzCtrl.Statustext(); }
            catch { return MyResource.Resource.LIZR_STATUS_UNBEKANNT; }
        }

        /// <summary>
        /// Die 15 Abschnitte „Rechtliche Hinweise" in ihrer Reihenfolge. Der letzte
        /// trägt Monat und Programmfassung.
        /// </summary>
        private static List<RechtsAbschnitt> Hinweise()
        {
            var liste = new List<RechtsAbschnitt>();
            for (int i = 1; i <= 7; i++)
            {
                liste.Add(new RechtsAbschnitt(true, Text("LIZR_RH_U" + i)));
                liste.Add(new RechtsAbschnitt(false, Text("LIZR_RH_A" + i)));
            }
            liste.Add(new RechtsAbschnitt(false,
                string.Format(Text("LIZR_RH_A8"),
                              DateTime.Now.ToString("MMMM yyyy"), Fassung())));
            return liste;
        }

        /// <summary>Die 12 Abschnitte „Komponenten"; der letzte nennt das Einbettungsmodell.</summary>
        private static List<RechtsAbschnitt> Komponenten()
        {
            var liste = new List<RechtsAbschnitt>();
            for (int i = 1; i <= 6; i++)
            {
                liste.Add(new RechtsAbschnitt(true, Text("LIZR_KO_U" + i)));

                string absatz = Text("LIZR_KO_A" + i);
                if (i == 6)
                    absatz = string.Format(absatz, SemantikModell.NAME, SemantikModell.LIZENZ);

                liste.Add(new RechtsAbschnitt(false, absatz));
            }
            return liste;
        }

        /// <summary>Ein Rechtstext aus dem Katalog; die Schlüssel sind fortlaufend.</summary>
        private static string Text(string schluessel)
            => MyResource.Resource.ResourceManager.GetString(schluessel) ?? "";

        /// <summary>Die Programmfassung — dieselbe Quelle wie im Vorläufer.</summary>
        private static string Fassung()
        {
            try
            {
                Version v = Assembly.GetExecutingAssembly().GetName().Version;
                return v == null ? "" : v.ToString();
            }
            catch { return ""; }
        }
    }
}
