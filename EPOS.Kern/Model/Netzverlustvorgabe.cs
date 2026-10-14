using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Netzverluste je Kanal und Zirkulation im Bestandsweg</b> eines Projekts
    /// (<c>Tab_Einstellungen</c>, <see cref="BedarfNetzKalenderSchema"/>; Entscheidungsvorlage
    /// Modellgrenzen BW4; Regeln: Konzept Simulationsablauf, Abschnitt 17).
    ///
    /// <para><b>Die Vorrangregel.</b> Sind alle drei Kanalwerte leer, gilt der Projektwert
    /// <c>Netzverluste</c>/<c>NetzverlusteEinheit</c> mit seiner anteiligen Verteilung
    /// (<c>Kanalsatz.NetzverlusteVerteilen</c>) Zeichen für Zeichen wie zuvor. Ist mindestens ein
    /// Kanalwert gesetzt (<see cref="JeKanal"/>), gilt je Kanal sein eigener Wert — ein leerer
    /// Kanal trägt dann 0 — und der Projektwert nicht mehr.</para>
    ///
    /// <para><b>Normalisiert:</b> Ein Wert ohne gültige Einheit gilt in Prozent, eine Einheit ohne
    /// Wert entfällt; die Prüfklauseln der Spalten lassen beides ohnehin nicht zu.</para>
    /// </summary>
    public sealed record Netzverlustvorgabe
    {
        /// <summary>Netzverlust des Heizkanals; <c>null</c> = kein Kanalwert.</summary>
        public double? HeizungWert { get; init; }

        /// <summary>Einheit des Heizkanalwerts (<c>%</c> oder <c>kWh/a</c>); <c>null</c> ohne Wert.</summary>
        public string HeizungEinheit { get; init; }

        /// <summary>Netzverlust des Brauchwasserkanals; <c>null</c> = kein Kanalwert.</summary>
        public double? BrauchwasserWert { get; init; }

        /// <summary>Einheit des Brauchwasserkanalwerts.</summary>
        public string BrauchwasserEinheit { get; init; }

        /// <summary>Netzverlust des Prozesskanals; <c>null</c> = kein Kanalwert.</summary>
        public double? ProzessWert { get; init; }

        /// <summary>Einheit des Prozesskanalwerts.</summary>
        public string ProzessEinheit { get; init; }

        /// <summary>Zirkulationsleistung des Bestandswegs [kW]; <c>null</c> = keine.</summary>
        public double? ZirkulationLeistungKw { get; init; }

        /// <summary>Laufzeit der Zirkulation [h/d]; <c>null</c> = keine.</summary>
        public double? ZirkulationLaufzeitHd { get; init; }

        /// <summary>Alles leer — der Projektwert gilt, keine Zirkulation.</summary>
        public static readonly Netzverlustvorgabe Leer = new Netzverlustvorgabe();

        /// <summary>Ist mindestens ein Kanalwert gesetzt? Dann gilt der Projektwert nicht mehr.</summary>
        public bool JeKanal => HeizungWert.HasValue || BrauchwasserWert.HasValue || ProzessWert.HasValue;

        /// <summary>Rechnet der Bestandsweg eine Zirkulation (Leistung und Laufzeit größer 0)?</summary>
        public bool MitZirkulation =>
            ZirkulationLeistungKw.HasValue && ZirkulationLeistungKw.Value > 0 &&
            ZirkulationLaufzeitHd.HasValue && ZirkulationLaufzeitHd.Value > 0;

        /// <summary>
        /// Der Jahresverlust der Zirkulation [kWh/a]: <c>Q = P · t_Lauf · 365</c> — dieselbe Formel
        /// wie die Methode „manuell" des Zapfprofilgenerators; 0 ohne Zirkulation.
        /// </summary>
        public double ZirkulationJahresKwh =>
            MitZirkulation ? ZirkulationLeistungKw.Value * ZirkulationLaufzeitHd.Value * TAGE : 0.0;

        /// <summary>Tage des Simulationsjahres (kein Schaltjahr).</summary>
        public const int TAGE = 365;

        /// <summary>Wert und Einheit des Kanals <paramref name="kanal"/> (<see cref="Kanal"/>); leer = (null, null).</summary>
        public (double? Wert, string Einheit) Kanalwert(int kanal)
        {
            switch (kanal)
            {
                case Kanal.HEIZUNG: return (HeizungWert, HeizungEinheit);
                case Kanal.BRAUCHWASSER: return (BrauchwasserWert, BrauchwasserEinheit);
                case Kanal.PROZESS: return (ProzessWert, ProzessEinheit);
                default: return (null, null);
            }
        }

        /// <summary>
        /// Der stündliche Aufschlag eines Kanals [kWh/h] aus seinem Jahresbedarf [kWh]: in Prozent
        /// <c>Q_k · p / 100 / 8760</c>, als feste Menge <c>W / 8760</c>; ein leerer Kanal 0.
        /// </summary>
        public double BetragJeStunde(int kanal, double jahresbedarfKwh)
        {
            (double? wert, string einheit) = Kanalwert(kanal);
            if (!wert.HasValue || !(wert.Value > 0)) return 0.0;
            if (einheit == BedarfNetzKalenderSchema.EINHEIT_KWH) return wert.Value / STUNDEN;
            return jahresbedarfKwh * wert.Value / 100.0 / STUNDEN;
        }

        /// <summary>Stunden des Simulationsjahres.</summary>
        public const int STUNDEN = 8760;

        /// <summary>Normalisiert ein Paar: Wert ohne gültige Einheit → Prozent, Einheit ohne Wert → leer.</summary>
        public static string EinheitNormal(double? wert, string einheit)
        {
            if (!wert.HasValue) return null;
            return einheit == BedarfNetzKalenderSchema.EINHEIT_KWH
                ? BedarfNetzKalenderSchema.EINHEIT_KWH : BedarfNetzKalenderSchema.EINHEIT_PROZENT;
        }

        /// <summary>Die normalisierte Form (Einheiten zu den Werten, NaN als leer).</summary>
        public Netzverlustvorgabe Normalisiert()
        {
            double? h = Endlich(HeizungWert), b = Endlich(BrauchwasserWert), p = Endlich(ProzessWert);
            return this with
            {
                HeizungWert = h, HeizungEinheit = EinheitNormal(h, HeizungEinheit),
                BrauchwasserWert = b, BrauchwasserEinheit = EinheitNormal(b, BrauchwasserEinheit),
                ProzessWert = p, ProzessEinheit = EinheitNormal(p, ProzessEinheit),
                ZirkulationLeistungKw = Endlich(ZirkulationLeistungKw),
                ZirkulationLaufzeitHd = Endlich(ZirkulationLaufzeitHd)
            };
        }

        private static double? Endlich(double? x) =>
            x.HasValue && !double.IsNaN(x.Value) && !double.IsInfinity(x.Value) ? x : null;

        /// <summary>
        /// Prüft die Eingaben gegen die Grenzen der Spalten; <c>null</c> = gültig, sonst der Grund
        /// in der Oberflächensprache. Dieselben Grenzen wie die Prüfklauseln
        /// (<see cref="BedarfNetzKalenderSchema"/>).
        /// </summary>
        public string Pruefen()
        {
            foreach (int k in new[] { Kanal.HEIZUNG, Kanal.BRAUCHWASSER, Kanal.PROZESS })
            {
                (double? wert, string einheit) = Kanalwert(k);
                if (!wert.HasValue) continue;
                if (wert.Value < 0) return MyResource.Resource.NETZKANAL_MSG_NEGATIV;
                if (EinheitNormal(wert, einheit) == BedarfNetzKalenderSchema.EINHEIT_PROZENT && wert.Value > 100)
                    return MyResource.Resource.NETZKANAL_MSG_PROZENT;
            }
            if (ZirkulationLeistungKw.HasValue &&
                (ZirkulationLeistungKw.Value < 0 || ZirkulationLeistungKw.Value > BedarfNetzKalenderSchema.ZIRK_LEISTUNG_MAX_KW))
                return MyResource.Resource.NETZKANAL_MSG_ZIRK_LEISTUNG;
            if (ZirkulationLaufzeitHd.HasValue &&
                (ZirkulationLaufzeitHd.Value < 0 || ZirkulationLaufzeitHd.Value > BedarfNetzKalenderSchema.ZIRK_LAUFZEIT_MAX_H))
                return MyResource.Resource.NETZKANAL_MSG_ZIRK_LAUFZEIT;
            return null;
        }
    }
}
