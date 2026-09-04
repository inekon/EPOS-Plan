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
    /// <c>Init*</c>-Methoden</b> von <c>MDIMainForm</c>. Was dort in 43
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
                ["Produktname"] = MDIMainForm.PRODUKTNAME,
                ["Gattung"] = MyResource.Resource.START_GATTUNG,
                ["Claim"] = MyResource.Resource.HAUPT_CLAIM,
                ["VersionText"] = Versionszeile()
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
        /// </remarks>
        internal Task<bool> Weg(string ziel, string argument)
        {
            if (string.IsNullOrEmpty(ziel)) return Task.FromResult(false);

            IWin32Window wirt = _besitzer?.Invoke();

            switch (ziel)
            {
                // ---- Menü „Projekt" -----------------------------------------
                case Seitenschluessel.ProjektNeu:
                    ProjektAssistent(neu: true);
                    return Task.FromResult(true);

                case Seitenschluessel.ProjektBearbeiten:
                    ProjektAssistent(neu: false);
                    return Task.FromResult(true);

                case Seitenschluessel.ProjektOeffnen:
                    new MenueCtrl().ProjektOeffnen();
                    return Task.FromResult(true);

                case Seitenschluessel.ProjektZuletzt:
                    new MenueCtrl().ProjektOeffnen(true);
                    return Task.FromResult(true);

                case Seitenschluessel.ProjektLoeschen:
                    new MenueCtrl().ProjektDelete();
                    return Task.FromResult(true);

                case Seitenschluessel.ProjektTransfer:
                    ProjektTransferHuelle.Oeffnen(wirt);
                    return Task.FromResult(true);

                case Seitenschluessel.ProjektAlsVariante:
                    // iU9-W16b.3: Das offene Projekt kommt aus dem Kern.
                    AlsVarianteHuelle.Zeige(wirt, Dienste.Projekt.Id, Dienste.Projekt.Name);
                    return Task.FromResult(true);

                // ANWENDERENTSCHEID W16c-E-3 (04.09.2026): Seitenschluessel
                // .BerichteKosten hat hier KEINEN Fall mehr. Bis dahin holte der
                // Menüpunkt „Varianten und Bericht…" den sechsten Reiter der
                // Startseite nach vorn (wörtlich MenuItem_VariantenBericht_Click
                // → StartseiteHuelle.ZeigeBerichteKosten); jetzt fällt er durch,
                // MaskeOeffnen meldet false — „BERICHTE_KOSTEN" steht in
                // Ansichten, nicht in Masken —, und Hauptfenster.Springe lässt
                // die AppWurzel auf die ANSICHT wechseln. Das ist derselbe Weg
                // wie auf iOS; der Parametersatz dafür steht in Gaben().

                // ---- Menü „Administration" ----------------------------------
                case Seitenschluessel.Klimadaten:
                    KlimadatenHuelle.Oeffnen(wirt);
                    return Task.FromResult(true);

                case Seitenschluessel.Kostenverwaltung:
                    KostenKomponenteHuelle.Oeffnen(wirt);
                    return Task.FromResult(true);

                case Seitenschluessel.EnergietraegerVerwaltung:
                    EnergietraegerHuelle.Oeffnen(wirt, 0);
                    return Task.FromResult(true);

                case Seitenschluessel.Einstellungen:
                    EinstellungenHuelle.Oeffnen(wirt);
                    return Task.FromResult(true);

                case Seitenschluessel.Gesetzeskatalog:
                    GesetzeskatalogHuelle.Oeffnen(wirt);
                    return Task.FromResult(true);

                case Seitenschluessel.KatalogDubletten:
                    KatalogDublettenHuelle.Oeffnen(wirt);
                    return Task.FromResult(true);

                case Seitenschluessel.LizenzVerwaltung:
                    LizenzVerwaltungHuelle.Oeffnen(wirt);
                    return Task.FromResult(true);

                // ---- Menü „Hilfe" -------------------------------------------
                case Seitenschluessel.Lizenztext:
                    LizenzHuelle.Anzeigen(wirt);
                    return Task.FromResult(true);

                case Seitenschluessel.KiAssistent:
                    KiChatHuelle.Oeffnen(wirt);
                    return Task.FromResult(true);

                case Seitenschluessel.Version:
                    Versionsmeldung();
                    return Task.FromResult(true);

                case Seitenschluessel.Dokumentation:
                    Dokumentation();
                    return Task.FromResult(true);

                // ---- Sprache ------------------------------------------------
                case Seitenschluessel.SpracheDeutsch:
                    SpracheSetzen("de", englisch: false);
                    return Task.FromResult(true);

                case Seitenschluessel.SpracheEnglisch:
                    SpracheSetzen("en", englisch: true);
                    return Task.FromResult(true);

                // ---- Die 25 Maskenschlüssel ---------------------------------
                default:
                    return Task.FromResult(MaskeOeffnen(ziel, argument));
            }
        }

        /// <summary>
        /// Ein Maskenschlüssel geht unmittelbar an <c>Dienste.Navigation</c> —
        /// die 21 einzeiligen <c>MenueCtrl</c>-Methoden dazwischen entfallen.
        /// </summary>
        private static bool MaskeOeffnen(string ziel, string argument)
        {
            if (!Maskenschluessel.Contains(ziel)) return false;

            // Zwei Schlüssel brauchen ein Argument. Der PV-Import bekommt seine
            // Quelle aus der Menütabelle ("CEC"), die Lastspitzenkappung das
            // offene Projekt — wörtlich MenueCtrl.PeakShavingBearbeiten.
            if (ziel == Seitenschluessel.PeakShaving)
                Dienste.Navigation.OeffneMaske(ziel, Dienste.Projekt.Id);
            else if (!string.IsNullOrEmpty(argument))
                Dienste.Navigation.OeffneMaske(ziel, argument);
            else
                Dienste.Navigation.OeffneMaske(ziel);

            return true;
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
            string text = MDIMainForm.PRODUKTNAME + Environment.NewLine +
                          MyResource.Resource.HAUPT_CLAIM + Environment.NewLine + Environment.NewLine +
                          Versionszeile() + Environment.NewLine +
                          MyResource.Resource.HAUPT_UEBER_HAUS;

            Dienste.Dialog.Meldung(
                text,
                string.Format(MyResource.Resource.HAUPT_UEBER_TITEL, MDIMainForm.PRODUKTNAME));
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
                adresse = MDIMainForm.DOKU_URL;

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
