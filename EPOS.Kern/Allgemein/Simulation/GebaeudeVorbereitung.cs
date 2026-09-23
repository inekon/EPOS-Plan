namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der modellfreie Vorbereitungsschritt</b> eines Gebäudes — was feststeht, ohne dass
    /// ein Rechenweg gelaufen ist (Vertrag: Softwarearchitektur Gebäudesimulation 1.3; E20,
    /// ADR-006 Entscheidung 1). Er läuft in der Fassade <b>vor</b> der Weiche, und beide
    /// Rechenwege lesen dasselbe.
    ///
    /// <para><b>Sechs Stücke:</b> der <see cref="Klimakalender"/> des Laufs,
    /// <see cref="VerbrauchNeu"/> je Einheit, <see cref="FlaecheAlt"/>,
    /// <see cref="Flaeche_Nutzer"/>, <see cref="Einheit"/> und
    /// <see cref="Jahresnutzungsgrad"/>. <b>Nicht</b> hier entstehen Bewohnerzahl und
    /// Skalierungsfaktor (E8) — sie entstehen je Rechenweg aus dessen erstem Lauf, und die
    /// Fassade führt die Schleife darüber.</para>
    ///
    /// <para>Fassade und Vorbereitungsschritt bleiben über die Ablösung des Altwegs hinaus.</para>
    /// </summary>
    internal sealed class GebaeudeVorbereitung
    {
        /// <summary>Die Einheit, in der die Gebäudezeile unmittelbar eine Fläche trägt.</summary>
        internal const string EINHEIT_FLAECHE = "Wohnfläche [m²]";

        private GebaeudeVorbereitung() { }

        /// <summary>Der Klimakalender des Laufs.</summary>
        internal Klimakalender Klimakalender { get; private set; }

        /// <summary>
        /// Der bisherige Verbrauch der Gebäudezeile in kWh/a, aus der Angabe in ihrer Einheit
        /// umgerechnet; 0, wenn die Einheit eine Fläche ist.
        /// </summary>
        internal double VerbrauchNeu { get; private set; }

        /// <summary>Die Gesamtfläche des Katalogsatzes (<c>Wohnflaeche_gesamt</c>) in m².</summary>
        internal double FlaecheAlt { get; private set; }

        /// <summary>Fläche je Nutzer in m².</summary>
        internal double Flaeche_Nutzer { get; private set; }

        /// <summary>Die Bezugseinheit der Angabe <c>Z_AuswahlWohnflaeche</c>.</summary>
        internal string Einheit { get; private set; }

        /// <summary>Jahresnutzungsgrad der bisherigen Erzeugung.</summary>
        internal double Jahresnutzungsgrad { get; private set; }

        /// <summary><c>true</c>, wenn die Einheit eine Fläche ist — dann gibt es keine
        /// Verbrauchs-Rückrechnung.</summary>
        internal bool IstFlaeche => Einheit == EINHEIT_FLAECHE;

        /// <summary>
        /// Bildet die Vorbereitung einer Gebäudezeile. Liest die Zeile, schreibt sie nicht.
        /// Die Umrechnung des Verbrauchs ist Anweisung für Anweisung die des Bestands
        /// (früher <c>Bewohner_und_Flaeche_berechnen</c>).
        /// </summary>
        internal static GebaeudeVorbereitung Bilden(Klimakalender kalender, ProjektGebaeudeModel item)
        {
            double VerbrauchNeu = 0.0;

            if (item.Einheit == "Ölverbrauch [l/a]")
            {
                VerbrauchNeu = item.Z_AuswahlWohnflaeche * item.Jahresnutzungsgrad * 10.08;
            }
            else if (item.Einheit == "Gasverbrauch [m³/a]")
            {
                VerbrauchNeu = item.Z_AuswahlWohnflaeche * item.Jahresnutzungsgrad * 11.48;
            }
            else if (item.Einheit == "Gasverbrauch [MWh/a] (Ho)")
            {
                VerbrauchNeu = item.Z_AuswahlWohnflaeche * item.Jahresnutzungsgrad / 1.1 * 1000;
            }
            else if (item.Einheit == "Brennstoffverbrauch [MWh/a]")
            {
                VerbrauchNeu = item.Z_AuswahlWohnflaeche * item.Jahresnutzungsgrad * 1000;
            }
            else if (item.Einheit == "Verbrauch  [MWh/a]")
            {
                VerbrauchNeu = item.Z_AuswahlWohnflaeche * 1000;
            }

            return new GebaeudeVorbereitung
            {
                Klimakalender = kalender,
                VerbrauchNeu = VerbrauchNeu,
                FlaecheAlt = item.Wohnflaeche_gesamt,
                Flaeche_Nutzer = item.Flaeche_Nutzer,
                Einheit = item.Einheit,
                Jahresnutzungsgrad = item.Jahresnutzungsgrad,
            };
        }
    }
}
