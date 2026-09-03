using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Windows-Fassung von <see cref="IProjektKontext"/>: Sie reicht an die
    /// Startmaske <c>Form_Start</c> durch, die das offene Projekt bis Paket iU9 führt.
    ///
    /// <para><b>Nichts wird nachgebaut.</b> <see cref="Uebernehmen"/> ruft genau die
    /// beiden Methoden, die der Bestand ruft:
    /// <c>Form_Start.ProjektKontextUebernehmen</c> zieht Kopfband, Klimaregion,
    /// Statuszeichen, Reiterfreigaben, Kachelstatus und Variantenanzeige nach,
    /// <c>Form_Start.ZuletztGeoeffnetMerken</c> schreibt <c>Tab_Applikation</c> fort.
    /// Es gibt damit weiterhin genau EINE Wahrheit für den Projektwechsel.</para>
    ///
    /// <para><b>Ohne Startmaske ist kein Projekt offen.</b> Im Prüfharnisch und in
    /// Konsolenläufen ist <c>Program.startfrm</c> <c>null</c>; dann gilt „keins" — genau
    /// die Fallunterscheidung, die <c>KiAktionenProjekt.AktivesProjektErmitteln</c>
    /// schon vorher traf, bevor es ersatzweise <c>Tab_Applikation</c> liest.</para>
    /// </summary>
    public sealed class FormStartProjektKontext : IProjektKontext
    {
        /// <inheritdoc/>
        public bool Vorhanden
        {
            get
            {
                try { return Program.startfrm != null; }
                catch { return false; }
            }
        }

        /// <inheritdoc/>
        public int Id
        {
            get
            {
                try { return Program.startfrm != null ? Program.startfrm.m_ID_Projekt : 0; }
                catch { return 0; }
            }
        }

        /// <inheritdoc/>
        public string Name
        {
            get
            {
                try { return Program.startfrm != null ? (Program.startfrm.m_szProjektname ?? "") : ""; }
                catch { return ""; }
            }
        }

        /// <inheritdoc/>
        public string Klimazone
        {
            get
            {
                try { return Program.startfrm != null ? (Program.startfrm.Klimaregion ?? "") : ""; }
                catch { return ""; }
            }
        }

        /// <inheritdoc/>
        public bool Uebernehmen(int id, string name)
        {
            Form_Start start = Program.startfrm;
            if (start == null) return false;

            string szProjekt = name ?? "";

            // Ohne Namen, aber mit ID: den Namen nachschlagen. Der Name ist der
            // fuehrende Schluessel des Bestands, die ID nur der Rueckfall.
            if (string.IsNullOrWhiteSpace(szProjekt) && id > 0)
            {
                ProjektCtrl ctrlproj = new ProjektCtrl();
                ctrlproj.ReadSingle(id);
                szProjekt = ctrlproj.rows > 0 ? ctrlproj.m_szProjektname : "";
            }

            if (!start.ProjektKontextUebernehmen(szProjekt)) return false;

            start.ZuletztGeoeffnetMerken();

            Action h = Gewechselt;
            if (h != null) h();
            return true;
        }

        /// <inheritdoc/>
        public event Action Gewechselt;
    }
}
