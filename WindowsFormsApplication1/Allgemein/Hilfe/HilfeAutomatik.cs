using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// F5 - Registrierung zentral, nicht 151-mal einzeln.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Das Problem.</b> Drei von 151 Formularen riefen bisher selbst
    /// <c>RegisterForm(this)</c> auf. Ein Muster, das man 151-mal von Hand anwenden
    /// muss, wird nicht angewendet - genau daran ist das Hilfesystem gescheitert.
    /// Nach dieser Klasse ist <c>help_mapping.txt</c> die EINZIGE Stelle, an der
    /// Hilfe gepflegt wird; neue Infobuttons brauchen keine Zeile Programmtext mehr.
    /// </para>
    /// <para>
    /// <b>Der gewaehlte Haken: <see cref="Application.Idle"/> ueber
    /// <see cref="Application.OpenForms"/>.</b> Eine gemeinsame Basisklasse schied
    /// aus - sie haette 151 Formulare angefasst und waere an den eingebetteten
    /// Formularen (<c>Form_Start</c> laeuft mit <c>TopLevel=false</c> als Kind von
    /// <c>Hauptfensterrahmen</c>) ohnehin vorbeigegangen. Ein <c>Form.Shown</c>-Haken gibt
    /// es anwendungsweit nicht; man muesste ihn je Formular anbringen, also wieder
    /// 151-mal. <see cref="Application.Idle"/> dagegen laeuft auch in modalen
    /// Schleifen, kommt ohne jede Aenderung an den Formularen aus und kostet je
    /// Durchgang nur einen Mengenvergleich ueber die wenigen offenen Fenster.
    /// </para>
    /// <para>
    /// <b>Dynamisch nachgeladene UserControls.</b> Ein Leerlauf-Durchgang allein
    /// saehe nur, was beim Oeffnen schon da war. Deshalb bekommt jeder Behaelter im
    /// erfassten Baum einen <see cref="Control.ControlAdded"/>-Haken. Was spaeter
    /// eingehaengt wird - etwa <c>ucFuelSettings</c> in <c>Form_Energietraeger</c> -, meldet
    /// sich damit von selbst; nachgezogen wird gebuendelt im naechsten Leerlauf,
    /// damit ein Aufbau mit Dutzenden Steuerelementen nicht Dutzende Durchgaenge
    /// ausloest.
    /// </para>
    /// <para>
    /// <b>Idempotenz.</b> Jede Wurzel wird ueber eine Menge von Verweisen genau
    /// einmal erfasst. Ein zweiter Durchgang - durch <c>ControlAdded</c>, durch das
    /// Nachziehen nach dem Ladelauf oder durch einen der drei verbliebenen
    /// Selbstaufrufe in <c>Form_Start</c>, <c>Form_Klimadaten</c> und
    /// <c>Form_Energietraeger</c> - ist harmlos: <c>HelpExtender.SetHelpKey</c> loest jede
    /// Ereignisbindung vor dem Setzen wieder und ueberschreibt den Schluessel,
    /// statt ihn zu ergaenzen.
    /// </para>
    /// </remarks>
    internal static class HilfeAutomatik
    {
        /// <summary>Der EINE Extender der Anwendung.</summary>
        private static HelpExtender _extender;

        private static WikiHelpCatalog _katalog;

        /// <summary>Bereits erfasste Registrierungswurzeln (Verweisgleichheit).</summary>
        private static readonly HashSet<Control> _wurzeln = new HashSet<Control>();

        /// <summary>Behaelter mit <c>ControlAdded</c>-Haken.</summary>
        private static readonly HashSet<Control> _ueberwacht = new HashSet<Control>();

        /// <summary>Behaelter, die im naechsten Leerlauf nachgezogen werden.</summary>
        private static readonly HashSet<Control> _nachzuziehen = new HashSet<Control>();

        private static bool _laeuft;

        /// <summary>
        /// Wird vom Abschluss des Ladelaufs gesetzt - der laeuft auf einem fremden
        /// Faden, deshalb <c>volatile</c> und deshalb nur ein Merker: die Arbeit
        /// erledigt der naechste Leerlauf auf dem Oberflaechenfaden.
        /// </summary>
        private static volatile bool _katalogFrisch;

        /// <summary>Der anwendungsweite Extender, oder <c>null</c> vor dem Start.</summary>
        internal static HelpExtender Extender => _extender;

        /// <summary>
        /// Startet die anwendungsweite Registrierung und liefert den einen
        /// Extender. Ein zweiter Aufruf ist wirkungslos.
        /// </summary>
        internal static HelpExtender Starten(WikiHelpCatalog katalog)
        {
            if (_laeuft) return _extender;

            _katalog = katalog;
            _extender = new HelpExtender(katalog);
            _laeuft = true;

            Application.Idle += Leerlauf;

            // Startwettlauf: Sobald der Ladelauf durch ist, werden alle bereits
            // erfassten Baeume erneut ausgewertet.
            katalog?.Loaded.ContinueWith(_ => { _katalogFrisch = true; }, TaskScheduler.Default);

            return _extender;
        }

        // -------------------------------------------------------------------
        // Leerlauf
        // -------------------------------------------------------------------

        private static void Leerlauf(object sender, EventArgs e)
        {
            if (_extender == null) return;

            try
            {
                NeueFormulareErfassen();
                NachgeladenesErfassen();
                NachKatalogErneuern();
            }
            catch (Exception ex)
            {
                // Eine Ausnahme aus Application.Idle risse die Anwendung mit.
                System.Diagnostics.Debug.WriteLine("[Help] FEHLER in der Hilfeautomatik: " + ex);
            }
        }

        private static void NeueFormulareErfassen()
        {
            List<Form> offene = OffeneFormulare();
            if (offene == null) return;

            foreach (Form formular in offene) WurzelErfassen(formular, formular?.Name);
        }

        /// <summary>
        /// Abzug der offenen Formulare. Aendert sich die Sammlung waehrend des
        /// Durchlaufs, wird dieser Leerlauf ausgelassen - der naechste kommt
        /// Sekundenbruchteile spaeter.
        /// </summary>
        private static List<Form> OffeneFormulare()
        {
            try
            {
                var liste = new List<Form>();
                foreach (Form formular in Application.OpenForms) liste.Add(formular);
                return liste;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        // -------------------------------------------------------------------
        // Erfassen
        // -------------------------------------------------------------------

        private static void WurzelErfassen(Control wurzel, string praefix)
        {
            if (wurzel == null || wurzel.IsDisposed) return;

            if (string.IsNullOrEmpty(praefix)) praefix = wurzel.Name;
            if (string.IsNullOrEmpty(praefix)) return;   // ohne Namen keine Zuordnung

            if (!_wurzeln.Add(wurzel)) return;           // schon erfasst

            wurzel.Disposed += WurzelEntsorgt;

            _extender.RegisterBaum(wurzel, praefix);
            BehaelterUeberwachen(wurzel);
        }

        /// <summary>
        /// Haengt den <c>ControlAdded</c>-Haken an den Behaelter und an alle
        /// Nachkommen. Mehrfachaufrufe sind gewollt - <c>_ueberwacht</c> sorgt
        /// dafuer, dass jeder Behaelter genau einen Haken bekommt.
        /// </summary>
        private static void BehaelterUeberwachen(Control behaelter)
        {
            if (behaelter == null || behaelter.IsDisposed) return;

            if (_ueberwacht.Add(behaelter))
            {
                behaelter.ControlAdded += KindHinzugefuegt;
                behaelter.Disposed += BehaelterEntsorgt;
            }

            foreach (Control kind in behaelter.Controls) BehaelterUeberwachen(kind);
        }

        private static void KindHinzugefuegt(object sender, ControlEventArgs e)
        {
            Control behaelter = sender as Control;
            if (behaelter == null) return;

            // Nicht sofort nachziehen: ein Aufbau haengt oft Dutzende
            // Steuerelemente nacheinander ein. Der naechste Leerlauf fasst das zu
            // EINEM Durchgang je Wurzel zusammen.
            _nachzuziehen.Add(behaelter);
        }

        private static void NachgeladenesErfassen()
        {
            if (_nachzuziehen.Count == 0) return;

            var behaelter = new List<Control>(_nachzuziehen);
            _nachzuziehen.Clear();

            var wurzeln = new HashSet<Control>();
            foreach (Control c in behaelter)
            {
                Control wurzel = WurzelZu(c);
                if (wurzel != null) wurzeln.Add(wurzel);
            }

            foreach (Control wurzel in wurzeln) BaumErneuern(wurzel);
        }

        /// <summary>Naechstgelegene erfasste Wurzel oberhalb eines Steuerelements.</summary>
        private static Control WurzelZu(Control control)
        {
            Control lauf = control;

            while (lauf != null && !lauf.IsDisposed)
            {
                if (_wurzeln.Contains(lauf)) return lauf;
                lauf = lauf.Parent;
            }

            return null;
        }

        private static void BaumErneuern(Control wurzel)
        {
            if (wurzel == null || wurzel.IsDisposed) return;

            _extender.RegisterBaum(wurzel, wurzel.Name);

            // Was neu eingehaengt wurde, wird ab jetzt mit ueberwacht.
            BehaelterUeberwachen(wurzel);
        }

        // -------------------------------------------------------------------
        // Startwettlauf: nach dem Ladelauf nachziehen
        // -------------------------------------------------------------------

        private static void NachKatalogErneuern()
        {
            if (!_katalogFrisch) return;
            _katalogFrisch = false;

            // Der Katalog ist jetzt vollstaendig. Alle bereits erfassten Baeume
            // werden erneut ausgewertet, damit Zuordnungen nachgezogen werden, die
            // waehrend des Ladelaufs noch ins Leere zeigten - und damit ein
            // voreilig abgeschalteter Infobutton wieder aufwacht.
            var wurzeln = new List<Control>(_wurzeln);
            foreach (Control wurzel in wurzeln) BaumErneuern(wurzel);

            System.Diagnostics.Debug.WriteLine(
                $"[Help] Katalog geladen ({(_katalog == null ? 0 : _katalog.SeitenAnzahl)} Seiten) - " +
                $"{wurzeln.Count} bereits geoeffnete Baeume nachgezogen.");
        }

        // -------------------------------------------------------------------
        // Aufraeumen
        // -------------------------------------------------------------------

        private static void WurzelEntsorgt(object sender, EventArgs e)
        {
            Control c = sender as Control;
            if (c == null) return;

            c.Disposed -= WurzelEntsorgt;
            _wurzeln.Remove(c);
            _nachzuziehen.Remove(c);
        }

        private static void BehaelterEntsorgt(object sender, EventArgs e)
        {
            Control c = sender as Control;
            if (c == null) return;

            c.Disposed -= BehaelterEntsorgt;
            c.ControlAdded -= KindHinzugefuegt;
            _ueberwacht.Remove(c);
            _nachzuziehen.Remove(c);
        }
    }
}
