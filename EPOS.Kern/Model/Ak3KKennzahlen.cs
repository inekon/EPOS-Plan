namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Kennzahlen von AK3-K</b> (Entwurf AK3-K 3.5, Festlegung 20) eines Projektlaufs — gelesen aus den Spalten
    /// <see cref="Ak3KSchema.SPALTEN_ERGEBNIS"/> der Projektzeile: die Zonensperre (Zonentage mit Sperre der Gegenseite,
    /// gesperrte Energie des Probetags) und die Kälteseite im geschlossenen Kreis (Stunden an der Kälteschranke,
    /// Umschaltstunden, Kälte-Restbedarf). Für den Bedarfsdialog und den Bericht; jede Seite steht nur mit Wert.
    /// </summary>
    internal sealed record Ak3KKennzahlen
    {
        /// <summary><c>Zonensperre_Tage</c> [d]: Zonentage mit Sperre der Gegenseite.</summary>
        internal int? ZonensperreTage { get; init; }

        /// <summary><c>Zonensperre_Heizen_Gesperrt_MWh</c> [MWh]: Raumheizung des Probetags an Kühltagen.</summary>
        internal double? HeizenGesperrtMwh { get; init; }

        /// <summary><c>Zonensperre_Kuehlen_Gesperrt_MWh</c> [MWh]: Raumkühlung des Probetags an Heiztagen.</summary>
        internal double? KuehlenGesperrtMwh { get; init; }

        /// <summary><c>Ak3_Kaelteschranke_Stunden</c> [h]: Stunden, in denen die Kälteschranke griff.</summary>
        internal int? KaelteschrankeStundenH { get; init; }

        /// <summary><c>Ak3_Umschalt_Stunden</c> [h]: Stunden mit Umschaltung der Wärmepumpe.</summary>
        internal int? UmschaltStundenH { get; init; }

        /// <summary><c>Ak3_Kaelterest_Stunden</c> [h]: Stunden mit Kälte-Restbedarf.</summary>
        internal int? KaelterestStundenH { get; init; }

        /// <summary><c>Ak3_Kaelterest_MWh</c> [MWh]: Kälte-Restbedarf im Jahr.</summary>
        internal double? KaelterestMwh { get; init; }

        /// <summary>Lief die Zonensperre?</summary>
        internal bool ZonensperreErhoben => ZonensperreTage.HasValue;

        /// <summary>Rechnete der Kreis die Kälteseite?</summary>
        internal bool KreisErhoben => KaelteschrankeStundenH.HasValue;

        /// <summary>Die Kennzahlen aus der Projektzeile; <c>null</c>, wenn weder die Sperre lief noch der Kreis die Kälteseite rechnete.</summary>
        internal static Ak3KKennzahlen Aus(ErgebnisEnergiebedarfModel e)
        {
            if (e == null || (!e.ZonensperreTage.HasValue && !e.Ak3KaelteschrankeStundenH.HasValue)) return null;
            return new Ak3KKennzahlen
            {
                ZonensperreTage = e.ZonensperreTage,
                HeizenGesperrtMwh = e.ZonensperreHeizenGesperrtMwh,
                KuehlenGesperrtMwh = e.ZonensperreKuehlenGesperrtMwh,
                KaelteschrankeStundenH = e.Ak3KaelteschrankeStundenH,
                UmschaltStundenH = e.Ak3UmschaltStundenH,
                KaelterestStundenH = e.Ak3KaelterestStundenH,
                KaelterestMwh = e.Ak3KaelterestMwh,
            };
        }
    }
}
