using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Einspeisegrenze des Projekts</b> (Welle M5, PV3; Schemaschritt
    /// <see cref="StromViertelstundenSchema"/>) — unveränderlich: ein Zahlenwert und seine Einheit
    /// (<see cref="DbWerte.EINSPEISEGRENZE_KW"/> oder <see cref="DbWerte.EINSPEISEGRENZE_PROZENT"/> der
    /// installierten PV-Leistung). Gelesen je Lauf (<see cref="KonfigurationCtrl.EinspeisegrenzeLesen"/>),
    /// geschrieben in einem <c>UPDATE</c> (<see cref="KonfigurationCtrl.EinspeisegrenzeSchreiben"/>).
    ///
    /// <para><b>Leer heißt keine Grenze.</b> Ein leerer, negativer oder nicht endlicher Wert ist
    /// <see cref="Keine"/>; die Einheit „kW" wird als <c>null</c> geführt (dasselbe wie ihre Vorgabe,
    /// Muster Anlagenkopplung).</para>
    /// </summary>
    public sealed record Einspeisegrenze
    {
        /// <summary>Keine Einspeisegrenze — so liest sich eine fehlende Zeile oder Spalte.</summary>
        public static readonly Einspeisegrenze Keine = new Einspeisegrenze(null, null);

        /// <summary>Legt eine Einspeisegrenze an und normalisiert sie.</summary>
        /// <param name="wert">Zahlenwert; <c>null</c>, negativ oder nicht endlich = keine Grenze.</param>
        /// <param name="einheit"><see cref="DbWerte.EINSPEISEGRENZE_KW"/> (oder <c>null</c>) bzw. <see cref="DbWerte.EINSPEISEGRENZE_PROZENT"/>.</param>
        public Einspeisegrenze(double? wert, string einheit)
        {
            Wert = wert.HasValue && !double.IsNaN(wert.Value) && !double.IsInfinity(wert.Value) && wert.Value >= 0.0
                ? wert : null;
            Einheit = einheit == DbWerte.EINSPEISEGRENZE_PROZENT ? DbWerte.EINSPEISEGRENZE_PROZENT : null;
        }

        /// <summary>Der Zahlenwert; <c>null</c> = keine Grenze.</summary>
        public double? Wert { get; }

        /// <summary><c>null</c> = kW, sonst <see cref="DbWerte.EINSPEISEGRENZE_PROZENT"/>.</summary>
        public string Einheit { get; }

        /// <summary>Die Einheit, wie sie wirkt: kW, wenn keine andere gewählt ist.</summary>
        public string EinheitWirksam => Einheit ?? DbWerte.EINSPEISEGRENZE_KW;

        /// <summary>Steht ein Wert in Prozent der installierten PV-Leistung?</summary>
        public bool InProzent => Einheit == DbWerte.EINSPEISEGRENZE_PROZENT;

        /// <summary>Ist eine Grenze gesetzt?</summary>
        public bool Gesetzt => Wert.HasValue;

        /// <summary>
        /// Die Grenze in kW am Netzanschlusspunkt — in Prozent bezogen auf die installierte
        /// PV-Leistung <paramref name="kwp"/> [kWp]. <c>null</c> ohne Grenze und bei Prozent ohne
        /// installierte Leistung (dann gibt es keine Bezugsgröße; der Lauf meldet das).
        /// </summary>
        public double? Kw(double kwp)
        {
            if (!Wert.HasValue) return null;
            if (!InProzent) return Wert.Value;
            if (!(kwp > 0.0)) return null;
            return Wert.Value / 100.0 * kwp;
        }
    }
}
