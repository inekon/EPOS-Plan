using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

using EPOS.UI.Bausteine;
using EPOS.UI.Seiten;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Hauptfensters (iU9-W16c.3) — die Datenseite von
    /// <c>EPOS.UI/Seiten/Hauptfenster.razor</c>.
    ///
    /// <para><b>Sie ist die Nachfolge der 34 Ereignishandler und der acht
    /// <c>Init*</c>-Methoden</b> von <c>Hauptfensterrahmen</c>. Was dort in 43
    /// Einzelstellen stand, ist hier EIN <see cref="Weg"/>: Er bekommt den
    /// Seitenschlüssel des angeklickten Menüpunkts und geht ihn — entweder als
    /// zusammengesetzten Ablauf (die neunzehn Wege des Fensters) oder über
    /// <c>Dienste.Navigation</c> als Maskenschlüssel. Meldet er <c>false</c>,
    /// wechselt die Razor-Seite die Ansicht.</para>
    ///
    /// <para><b>Die Maskentabelle wird nicht abgeschrieben.</b> Welche Schlüssel
    /// Masken sind, liest <see cref="Maskenschluessel"/> über Reflexion aus
    /// <c>Masken</c> — derselben Klasse, aus der auch <c>Seitenschluessel</c>
    /// seine Werte erbt (K7). Eine zweite Liste wäre eine zweite Wahrheit.</para>
    ///
    /// <para><b>Muster:</b> <c>StartseiteHuelle</c> (W16b.3) — Gaben-Wörterbuch,
    /// Besitzer als <c>Func&lt;IWin32Window&gt;</c>, kein <c>Oeffnen</c>. Eine
    /// Seite wird nicht gezeigt, sie steht.</para>
    /// </summary>
    internal sealed class HauptfensterHuelle
    {
        private readonly Func<IWin32Window> _besitzer;
        private readonly StartseiteHuelle _startseite;

        /// <summary>
        /// Die Werte von <c>Masken</c> — über Reflexion, damit die Liste nicht
        /// zweimal gepflegt werden muss.
        /// </summary>
        private static readonly HashSet<string> Maskenschluessel =
            new HashSet<string>(
                typeof(Masken)
                    .GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                    .Select(f => (string)f.GetRawConstantValue()),
                StringComparer.Ordinal);

        internal HauptfensterHuelle(Func<IWin32Window> besitzer, ProjektKontextCtrl kontext)
        {
            _besitzer = besitzer ?? throw new ArgumentNullException(nameof(besitzer));
            _startseite = new StartseiteHuelle(besitzer, kontext);
        }

        // =====================================================================
        //  Der Parametersatz der Seite
        // =====================================================================

        /// <summary>Der Parametersatz von <c>Hauptfenster.razor</c>.</summary>
        internal IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Weg"] = new Func<string, string, Task<bool>>(Weg),
                ["Startansicht"] = Seitenschluessel.Startseite,
                ["StartseiteGaben"] = _startseite.Gaben(),

                // ANWENDERENTSCHEID W16c-E-3 (04.09.2026): „Varianten und
                // Bericht…" wechselt jetzt auf die ANSICHT BERICHTE_KOSTEN, und
                // dafür braucht die Wurzel deren Parametersatz. Er kommt aus
                // DERSELBEN BerichteKostenHuelle wie der sechste Reiter der
                // Startseite — eine zweite wäre ein zweiter Stammzustand und
                // damit eine zweite Wahrheit. Ohne diesen Eintrag fiele die
                // Wurzel auf IProjektQuelle.BerichteKostenGaben zurück, und das
                // ist unter Windows die Standardumsetzung (null): Die Ansicht
                // bliebe stehen und zeigte nur ihr Banner.
                ["BerichteKostenGaben"] = _startseite.BerichteGaben(),

                // Das Kopfband (InitMarke). Die drei Produkttexte waren deutsche
                // Literale im Code (Befund W16-B25); zwei davon stehen jetzt im
                // Katalog, der Produktname bleibt eine Konstante — ein Markenname
                // wird nicht übersetzt.
                ["Produktname"] = Hauptfensterrahmen.PRODUKTNAME,
                ["Gattung"] = MyResource.Resource.START_GATTUNG,
                ["Claim"] = MyResource.Resource.HAUPT_CLAIM,
                ["VersionText"] = Versionszeile(),

                // DAS LIZENZBANNER (Welle iF30, Anwenderentscheid 04.09.2026).
                // Ermittelt wird die Lage HIER, in der Hülle, und nicht in der
                // Komponente: Der Weg dahinter liest die DPAPI-Ablage und den
                // Zeitanker, und eine Razor-Komponente ruft immer vom
                // Zeichenfaden (Regel S-2 aus W15c). Was hineingeht, ist ein
                // fertiger Satz samt Dringlichkeit — kein Token, kein Anker,
                // kein Schlüssel.
                ["Lizenzlage"] = LizenzLage.Ermitteln()
            };
        }

        /// <summary>„Version 1.0.0.0" — der Text des rechten Kopfbandlabels.</summary>
        internal static string Versionszeile()
        {
            return string.Format(MyResource.Resource.HAUPT_VERSION, VersionText());
        }

        /// <summary>Versionsnummer der Anwendung als Text (z. B. „1.0.0.0").</summary>
        private static string VersionText()
        {
            try
            {
                Version v = Assembly.GetExecutingAssembly().GetName().Version;
                return v == null ? "" : v.ToString();
            }
            catch { return ""; }
        }

        // =====================================================================
        //  Der EINE Weg
        // =====================================================================

        /// <summary>
        /// Der Menüweg. <c>true</c> = behandelt; <c>false</c> = die Razor-Seite
        /// soll die Ansicht wechseln.
        /// </summary>
        /// <remarks>
        /// Die Reihenfolge ist die des Bestands: Was <c>MenueCtrl</c> bediente,
        /// öffnete ein Fenster; nur was dort nicht stand, wechselte die Ansicht.
        /// Der Rückgabewert der geöffneten Maske wird bewusst NICHT
        /// weitergereicht — „abgebrochen" ist nicht „nicht behandelt".
        ///
        /// <para><b>Die Antwort kommt sofort, das Fenster eine Nachricht
        /// später</b> (Befund W16b‑B‑1, 05.09.2026). Ob dieser Weg den Schlüssel
        /// behandelt, steht in der Schlüsseltabelle und hängt an keinem
        /// Fenster — das beantwortet <see cref="Weg"/> unverändert synchron,
        /// und die <c>AppWurzel</c> wechselt die Ansicht wie bisher nur bei
        /// <c>false</c>. Das ÖFFNEN dagegen läuft über
        /// <see cref="Blazorsprung"/>: Ein <c>ShowDialog</c> aus dem
        /// <c>WebMessageReceived</c>-Rückruf der WebView2 heraus baut seine
        /// verschachtelte Nachrichtenschleife samt zweiter WebView2 INNERHALB
        /// dieses Rückrufs auf; die Begründung steht bei
        /// <see cref="Blazorsprung"/>.</para>
        /// </remarks>
        internal Task<bool> Weg(string ziel, string argument)
        {
            if (string.IsNullOrEmpty(ziel)) return Task.FromResult(false);

            // Die 25 Maskenschluessel gehen unmittelbar an Dienste.Navigation -
            // ob dieser Weg zustaendig ist, sagt die Tabelle, nicht das Fenster.
            if (Maskenschluessel.Contains(ziel))
            {
                Blazorsprung.Verzoegert(_besitzer?.Invoke(), () => MaskeOeffnen(ziel, argument));
                return Task.FromResult(true);
            }

            Action ablauf = Ablauf(ziel);
            if (ablauf == null) return Task.FromResult(false);

            Blazorsprung.Verzoegert(_besitzer?.Invoke(), ablauf);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Der EINE Ablauf zu einem Schlüssel — oder <c>null</c>, wenn dieser
        /// Weg ihn nicht führt.
        ///
        /// <para><b>Warum ein Delegat und kein zweiter <c>switch</c>.</b>
        /// <see cref="Weg"/> muss seit Befund W16b‑B‑1 zwei Fragen trennen:
        /// „behandle ich das?" (sofort, sonst wechselt die <c>AppWurzel</c> die
        /// Ansicht) und „tu es" (eine Nachricht später, siehe
        /// <see cref="Blazorsprung"/>). Eine Liste der zuständigen Schlüssel
        /// neben der Liste der Abläufe wären zwei Wahrheiten; hier beantwortet
        /// DERSELBE <c>switch</c> beides — er liefert die Antwort als Tat.</para>
        ///
        /// <para>Der Besitzer wird bewusst IM Delegaten geholt: Er wird erst
        /// gebraucht, wenn der Sprung läuft.</para>
        /// </summary>
        private Action Ablauf(string ziel)
        {
            switch (ziel)
            {
                // ---- Menü „Projekt" -----------------------------------------
                case Seitenschluessel.ProjektNeu:
                    return () => ProjektAssistent(neu: true);

                case Seitenschluessel.ProjektBearbeiten:
                    return () => ProjektAssistent(neu: false);

                case Seitenschluessel.ProjektOeffnen:
                    return () => new MenueCtrl().ProjektOeffnen();

                case Seitenschluessel.ProjektZuletzt:
                    return () => new MenueCtrl().ProjektOeffnen(true);

                case Seitenschluessel.ProjektLoeschen:
                    return () => new MenueCtrl().ProjektDelete();

                case Seitenschluessel.ProjektTransfer:
                    return () => ProjektTransferHuelle.Oeffnen(_besitzer?.Invoke());

                case Seitenschluessel.ProjektAlsVariante:
                    // iU9-W16b.3: Das offene Projekt kommt aus dem Kern.
                    return () => AlsVarianteHuelle.Zeige(_besitzer?.Invoke(),
                                                         Dienste.Projekt.Id, Dienste.Projekt.Name);

                // ANWENDERENTSCHEID W16c-E-3 (04.09.2026): Seitenschluessel
                // .BerichteKosten hat hier KEINEN Fall. Bis dahin holte der
                // Menüpunkt „Varianten und Bericht…" den sechsten Reiter der
                // Startseite nach vorn (wörtlich MenuItem_VariantenBericht_Click
                // → StartseiteHuelle.ZeigeBerichteKosten); jetzt fällt er durch,
                // dieser Weg meldet false — „BERICHTE_KOSTEN" steht in
                // Ansichten, nicht in Masken —, und Hauptfenster.Springe lässt
                // die AppWurzel auf die ANSICHT wechseln. Das ist derselbe Weg
                // wie auf iOS; der Parametersatz dafür steht in Gaben().

                // ---- Menü „Administration" ----------------------------------
                case Seitenschluessel.Klimadaten:
                    return () => KlimadatenHuelle.Oeffnen(_besitzer?.Invoke());

                case Seitenschluessel.Kostenverwaltung:
                    return () => KostenKomponenteHuelle.Oeffnen(_besitzer?.Invoke());

                case Seitenschluessel.EnergietraegerVerwaltung:
                    return () => EnergietraegerHuelle.Oeffnen(_besitzer?.Invoke(), 0);

                case Seitenschluessel.Einstellungen:
                    return () => EinstellungenHuelle.Oeffnen(_besitzer?.Invoke());

                case Seitenschluessel.Gesetzeskatalog:
                    return () => GesetzeskatalogHuelle.Oeffnen(_besitzer?.Invoke());

                case Seitenschluessel.KatalogDubletten:
                    return () => KatalogDublettenHuelle.Oeffnen(_besitzer?.Invoke());

                case Seitenschluessel.LizenzVerwaltung:
                    return () => LizenzVerwaltungHuelle.Oeffnen(_besitzer?.Invoke());

                // ---- Menü „Hilfe" -------------------------------------------
                case Seitenschluessel.Lizenztext:
                    return () => LizenzHuelle.Anzeigen(_besitzer?.Invoke());

                case Seitenschluessel.KiAssistent:
                    return () => KiChatHuelle.Oeffnen(_besitzer?.Invoke());

                case Seitenschluessel.Version:
                    return Versionsmeldung;

                case Seitenschluessel.Dokumentation:
                    return Dokumentation;

                // ---- Sprache ------------------------------------------------
                case Seitenschluessel.SpracheDeutsch:
                    return () => SpracheSetzen("de", englisch: false);

                case Seitenschluessel.SpracheEnglisch:
                    return () => SpracheSetzen("en", englisch: true);

                // ---- Die 25 Maskenschlüssel ---------------------------------
                // Sie kommen hier nicht an: Weg() erkennt sie an der
                // Schlüsseltabelle und schickt sie an MaskeOeffnen.
                default:
                    return null;
            }
        }

        /// <summary>
        /// Ein Maskenschlüssel geht unmittelbar an <c>Dienste.Navigation</c> —
        /// die 21 einzeiligen <c>MenueCtrl</c>-Methoden dazwischen entfallen.
        /// </summary>
        private static void MaskeOeffnen(string ziel, string argument)
        {
            if (!Maskenschluessel.Contains(ziel)) return;

            // Zwei Schlüssel brauchen ein Argument. Der PV-Import bekommt seine
            // Quelle aus der Menütabelle ("CEC"), die Lastspitzenkappung das
            // offene Projekt — wörtlich MenueCtrl.PeakShavingBearbeiten.
            if (ziel == Seitenschluessel.PeakShaving)
                Dienste.Navigation.OeffneMaske(ziel, Dienste.Projekt.Id);
            else if (!string.IsNullOrEmpty(argument))
                Dienste.Navigation.OeffneMaske(ziel, argument);
            else
                Dienste.Navigation.OeffneMaske(ziel);
        }

        // =====================================================================
        //  Die zusammengesetzten Abläufe
        // =====================================================================

        /// <summary>
        /// Der Projektassistent aus dem Menü — und der Nachzug des
        /// Projektkontexts danach.
        /// </summary>
        /// <remarks>
        /// Wörtlich <c>MenuItem_Neu_Click</c> (<c>:464-476</c>) und
        /// <c>MenuItem_ProjektBearbeiten_Click</c> (<c>:586-595</c>), „Befund 3":
        /// Ohne den Nachzug bliebe der Kontext auf dem zuvor geöffneten Projekt
        /// stehen, und die Startkacheln schrieben ins falsche Projekt.
        /// </remarks>
        private static void ProjektAssistent(bool neu)
        {
            MenueCtrl menu = new MenueCtrl();
            if (neu) menu.ProjektNeu(); else menu.ProjektBearbeiten();

            if (Program.wizardctrl != null && Program.wizardctrl.Projektname != "")
                Program.projektkontext?.Setzen(Program.wizardctrl.Projektname);
        }

        /// <summary>
        /// „Über EPOS-Plan" — die EINZIGE <c>MessageBox</c> des Hauptfensters
        /// (<c>:669-677</c>). Ihr Titel war ein deutsches Literal („Über " +
        /// PRODUKTNAME) und ihre Schlusszeile ebenso; beide stehen jetzt im
        /// Katalog.
        /// </summary>
        private void Versionsmeldung()
        {
            string text = Hauptfensterrahmen.PRODUKTNAME + Environment.NewLine +
                          MyResource.Resource.HAUPT_CLAIM + Environment.NewLine + Environment.NewLine +
                          Versionszeile() + Environment.NewLine +
                          MyResource.Resource.HAUPT_UEBER_HAUS;

            Dienste.Dialog.Meldung(
                text,
                string.Format(MyResource.Resource.HAUPT_UEBER_TITEL, Hauptfensterrahmen.PRODUKTNAME));
        }

        /// <summary>
        /// Die Online-Dokumentation im Browser (<c>:807-832</c>). Führend ist der
        /// Einstellwert; die englische Oberfläche geht über den
        /// Übersetzungs-Proxy (A6 / Entscheid 7.1a). Der Start selbst läuft seit
        /// W16c.3 über <c>Dienste.Datei.AdresseOeffnen</c> statt über ein
        /// unmittelbares <c>Process.Start</c>.
        /// </summary>
        private static void Dokumentation()
        {
            string adresse = Properties.Settings.Default.WordPressUrl;
            if (string.IsNullOrWhiteSpace(adresse) || adresse.Contains("localhost"))
                adresse = Hauptfensterrahmen.DOKU_URL;

            Dienste.Datei.AdresseOeffnen(DokuUebersetzung.FuerAnzeige(adresse));
        }

        /// <summary>
        /// Der Sprachwechsel (<c>:694-711</c>). Er wirkt erst beim Neustart — die
        /// Textressourcen bereits geöffneter Masken wechseln nicht.
        /// </summary>
        private static void SpracheSetzen(string sprache, bool englisch)
        {
            if (Dienste.Sprache.IstEnglisch == englisch) return;

            Dienste.Sprache.Setzen(sprache);
            Application.Restart();
        }
    }
}
