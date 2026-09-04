using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Windows-Fassung von <see cref="IProjektKontext"/> — seit iU9-W16b.0 eine
    /// <b>dünne Weiterleitung</b> auf <see cref="ProjektKontextCtrl"/> im Kern (K2).
    ///
    /// <para><b>Was sich geändert hat.</b> Bis hierher war diese Klasse eine Fassade auf
    /// <c>Program.startfrm</c>: Sie las Id, Name und Klimaregion als FELDER der
    /// Startmaske. Jetzt führt der Kern-Controller diese drei Werte, und die Startmaske
    /// bekommt sie von dort — <c>Form_Start.ProjektKontextUebernehmen</c> ruft
    /// <see cref="Kontext"/> und spiegelt anschließend nur noch Kopfband, Klimaregion,
    /// Statuszeichen, Reiterfreigaben, Kachelbitmaske und Variantenanzeige. Es gibt
    /// damit weiterhin genau EINE Wahrheit für den Projektwechsel; sie liegt nur nicht
    /// mehr in einem Fenster.</para>
    ///
    /// <para><b>Warum die Klasse noch steht.</b> Solange <c>Form_Start</c> existiert,
    /// muss ein Projektwechsel über <c>Dienste.Projekt</c> ihre Anzeige mitziehen —
    /// genau das macht <see cref="Uebernehmen"/> zusätzlich zum Kern-Aufruf. Mit
    /// W16b.3 (Razor-Startseite) tritt <see cref="ProjektKontextCtrl"/> unmittelbar an
    /// <c>Dienste.Projekt</c>, und diese Datei fällt (Risiko R-W16-4: ein falsch
    /// umgehängter Kontext schreibt in das falsche Projekt — deshalb in ZWEI
    /// Schritten).</para>
    /// </summary>
    public sealed class FormStartProjektKontext : IProjektKontext
    {
        /// <summary>
        /// Der EINE Projektkontext des Programms. Statisch, weil ihn außer dieser
        /// Weiterleitung auch <c>Form_Start</c> ruft — beide müssen denselben Stand
        /// sehen, sonst gäbe es zwei Wahrheiten.
        /// </summary>
        public static ProjektKontextCtrl Kontext { get; } = new ProjektKontextCtrl();

        /// <inheritdoc/>
        public bool Vorhanden
        {
            get
            {
                // Unveraendert die Frage nach der OBERFLAECHE: Im Pruefharnisch und in
                // Konsolenlaeufen gibt es keine Startmaske; dann gilt "keins" und nicht
                // "das zuletzt geoeffnete" - genau die Fallunterscheidung, die
                // KiAktionenProjekt.AktivesProjektErmitteln trifft.
                try { return Program.startfrm != null; }
                catch { return false; }
            }
        }

        /// <inheritdoc/>
        public int Id { get { return Kontext.Id; } }

        /// <inheritdoc/>
        public string Name { get { return Kontext.Name; } }

        /// <inheritdoc/>
        public string Klimazone { get { return Kontext.Klimazone; } }

        /// <inheritdoc/>
        public bool Uebernehmen(int id, string name)
        {
            Form_Start start = Program.startfrm;
            if (start == null) return false;

            // Der Kern setzt den Kontext und schreibt "zuletzt geoeffnet" fort; die
            // Startmaske zieht danach ihre Anzeige nach.
            if (!Kontext.Uebernehmen(id, name)) return false;

            start.AnzeigeNachziehen();

            Action h = Gewechselt;
            if (h != null) h();
            return true;
        }

        /// <inheritdoc/>
        public event Action Gewechselt;
    }
}
