namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Kennzahlen der Kühlkurve</b> (Entwurf KK, Festlegung 12; Schritt 202) eines Projektlaufs — gelesen aus den
    /// Spalten <see cref="KuehlkurveSchema.SPALTEN_ERGEBNIS"/> der Projektzeile: der mittlere verlangte Kühlvorlauf der
    /// Kühlstunden, die Summe der Absenkung durch den Raumeinfluss und die Stunden an der Vorlaufgrenze. Für den
    /// Bedarfsdialog und den Bericht; sie stehen nur, wenn der Kreis eine wirksame Kühlkurve rechnete.
    /// </summary>
    internal sealed record KuehlkurveKennzahlen
    {
        /// <summary><c>Kuehlkurve_Vorlauf_Mittel_C</c> [°C]: mittlerer verlangter Kühlvorlauf der Kühlstunden.</summary>
        internal double? VorlaufMittelC { get; init; }

        /// <summary><c>Kuehlkurve_Absenkung_Kh</c> [Kh]: Summe der Absenkung durch den Raumeinfluss.</summary>
        internal double? AbsenkungKh { get; init; }

        /// <summary><c>Kuehlkurve_Vorlaufgrenze_Stunden</c> [h]: Kühlstunden mit der Kurve an der Vorlaufgrenze.</summary>
        internal int? VorlaufgrenzeStundenH { get; init; }

        /// <summary>Die Kennzahlen aus der Projektzeile; <c>null</c>, wenn keine der drei Spalten einen Wert trägt.</summary>
        internal static KuehlkurveKennzahlen Aus(ErgebnisEnergiebedarfModel e)
        {
            if (e == null || (!e.KuehlkurveVorlaufMittelC.HasValue && !e.KuehlkurveAbsenkungKh.HasValue &&
                              !e.KuehlkurveVorlaufgrenzeStundenH.HasValue)) return null;
            return new KuehlkurveKennzahlen
            {
                VorlaufMittelC = e.KuehlkurveVorlaufMittelC,
                AbsenkungKh = e.KuehlkurveAbsenkungKh,
                VorlaufgrenzeStundenH = e.KuehlkurveVorlaufgrenzeStundenH,
            };
        }
    }
}
