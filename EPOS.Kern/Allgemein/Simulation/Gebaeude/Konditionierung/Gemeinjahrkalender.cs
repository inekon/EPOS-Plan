using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Konvention, nach der Feiertagsregeln im Gemeinjahr liegen</b> (Konzept
    /// Konditionierungsprofile 3.2, Entscheid E114): das <b>Wochentagsraster</b> des Laufs — w₀, der
    /// Wochentag des Jahrestags 1 — und ein <b>optionales Jahr</b>.
    ///
    /// <para><b>Regelfall ohne Jahr:</b> Kein Jahresdatum ist relevant. Ostersonntag ist der Sonntag
    /// des Rasters, der dem Jahrestag <see cref="OSTERANKER"/> (8. April) am nächsten liegt, bei
    /// gleichem Abstand der frühere; Buß- und Bettag ist der letzte Mittwoch des Rasters vor dem
    /// Jahrestag <see cref="BUSSTAG_GRENZE"/> (23. November). So fallen Karfreitag auf einen Freitag,
    /// Ostermontag und Pfingstmontag auf einen Montag und Himmelfahrt und Fronleichnam auf einen
    /// Donnerstag des Rasters — dieselbe Woche, die Wochenprofile, Standardlastprofile und der
    /// Zapfkalender lesen.</para>
    ///
    /// <para><b>Sonderfall mit Jahr:</b> Trägt das Projekt eine Preisreihe mit Jahr, liegen Ostern
    /// und Buß- und Bettag auf den echten Daten dieses Jahres, abgebildet auf den Jahrestag des
    /// Gemeinjahrs; das Wochentagsraster bleibt das des Laufs.</para>
    ///
    /// <para>Feste Feiertage hängen an keinem der beiden Teile. Ohne Datenbank, ohne Uhr.</para>
    /// </summary>
    public readonly struct Gemeinjahrkalender : IEquatable<Gemeinjahrkalender>
    {
        /// <summary>Der Anker der Osterregel ohne Jahr: Jahrestag 98 = 8. April.</summary>
        public const int OSTERANKER = 98;

        /// <summary>Die Grenze der Buß-und-Bettag-Regel ohne Jahr: Jahrestag 327 = 23. November.</summary>
        public const int BUSSTAG_GRENZE = 327;

        private const int SONNTAG = 6;
        private const int MITTWOCH = 2;

        /// <summary>
        /// Baut die Konvention.
        /// </summary>
        /// <param name="wochentagDesErstenTags">w₀: 0 = Montag … 6 = Sonntag für den Jahrestag 1.</param>
        /// <param name="jahr">Das Jahr der Preisreihe; 0 = Regelfall ohne Jahr.</param>
        /// <exception cref="ArgumentOutOfRangeException">w₀ außerhalb 0 … 6 oder ein unmögliches Jahr.</exception>
        public Gemeinjahrkalender(int wochentagDesErstenTags, int jahr = 0)
        {
            if (wochentagDesErstenTags < 0 || wochentagDesErstenTags > 6)
                throw new ArgumentOutOfRangeException(nameof(wochentagDesErstenTags),
                    "w₀ liegt zwischen 0 (Montag) und 6 (Sonntag).");
            if (jahr != 0 && (jahr < 1583 || jahr > 9999))
                throw new ArgumentOutOfRangeException(nameof(jahr),
                    "Das Jahr ist 0 (kein Jahr) oder liegt zwischen 1583 und 9999 (das Osterdatum ist gregorianisch).");
            W0 = wochentagDesErstenTags;
            Jahr = jahr;
        }

        /// <summary>Die Konvention aus Raster und einem Jahr, das fehlen darf (<c>null</c> oder 0 = Regelfall).</summary>
        public static Gemeinjahrkalender Aus(int wochentagDesErstenTags, int? jahr)
            => new Gemeinjahrkalender(wochentagDesErstenTags, jahr ?? 0);

        /// <summary>
        /// Der Kalender eines echten Jahres — Raster und Feiertage dieses Jahres. Für Proben und für
        /// Wege, deren Raster selbst an einem Kalenderjahr hängt.
        /// </summary>
        public static Gemeinjahrkalender Kalenderjahr(int jahr)
            => new Gemeinjahrkalender(((int)new DateTime(jahr, 1, 1).DayOfWeek + 6) % 7, jahr);

        /// <summary>w₀: 0 = Montag … 6 = Sonntag für den Jahrestag 1.</summary>
        public int W0 { get; }

        /// <summary>Das Jahr der Preisreihe; 0 = Regelfall ohne Jahr.</summary>
        public int Jahr { get; }

        /// <summary>Trägt die Konvention ein Jahr (Sonderfall)?</summary>
        public bool MitJahr => Jahr > 0;

        /// <summary>Der Wochentag (0 = Montag … 6 = Sonntag) eines Jahrestags 1 … 365 im Raster.</summary>
        public int Wochentag(int jahrestag) => ((W0 + jahrestag - 1) % 7 + 7) % 7;

        /// <summary>
        /// <b>Der Jahrestag des Ostersonntags</b> im Gemeinjahr. Ohne Jahr der Sonntag des Rasters am
        /// nächsten zum 8. April (bei Gleichstand der frühere), mit Jahr das echte Osterdatum.
        /// </summary>
        public int Ostersonntag
        {
            get
            {
                if (MitJahr)
                {
                    DateTime o = Feiertage.Ostersonntag(Jahr);
                    return Feiertage.Gemeinjahrestag(o.Month, o.Day);
                }
                int vor = (Wochentag(OSTERANKER) - SONNTAG + 7) % 7;   // Tage zurück zum Sonntag davor (0 = Anker ist Sonntag)
                int nach = (7 - vor) % 7;                               // Tage vor zum Sonntag danach
                return vor <= nach ? OSTERANKER - vor : OSTERANKER + nach;
            }
        }

        /// <summary>
        /// <b>Der Jahrestag des Buß- und Bettags</b> im Gemeinjahr. Ohne Jahr der letzte Mittwoch des
        /// Rasters vor dem 23. November, mit Jahr der Mittwoch vor dem 23. November dieses Jahres.
        /// </summary>
        public int BussUndBettag
        {
            get
            {
                if (MitJahr)
                {
                    DateTime d = new DateTime(Jahr, 11, 22);
                    while (d.DayOfWeek != DayOfWeek.Wednesday) d = d.AddDays(-1);
                    return Feiertage.Gemeinjahrestag(d.Month, d.Day);
                }
                int t = BUSSTAG_GRENZE - 1;
                while (Wochentag(t) != MITTWOCH) t--;
                return t;
            }
        }

        /// <inheritdoc/>
        public bool Equals(Gemeinjahrkalender other) => W0 == other.W0 && Jahr == other.Jahr;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is Gemeinjahrkalender k && Equals(k);

        /// <inheritdoc/>
        public override int GetHashCode() => W0 * 10000 + Jahr;

        /// <summary>Gleichheit.</summary>
        public static bool operator ==(Gemeinjahrkalender a, Gemeinjahrkalender b) => a.Equals(b);

        /// <summary>Ungleichheit.</summary>
        public static bool operator !=(Gemeinjahrkalender a, Gemeinjahrkalender b) => !a.Equals(b);

        /// <summary>Kurzform für Protokoll und Schlüssel: „w₀ 3, ohne Jahr" bzw. „w₀ 3, Jahr 2026".</summary>
        public override string ToString()
            => "w₀ " + W0.ToString(CultureInfo.InvariantCulture) +
               (MitJahr ? ", Jahr " + Jahr.ToString(CultureInfo.InvariantCulture) : ", ohne Jahr");
    }
}
