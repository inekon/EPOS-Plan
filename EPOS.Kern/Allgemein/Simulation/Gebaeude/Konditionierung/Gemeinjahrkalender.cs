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
    /// <para><b>Sonderfall mit Jahr</b> (<see cref="Kalenderjahr"/>): Trägt das Projekt eine Preisreihe
    /// mit Jahr, gilt der Kalender dieses Jahres — das Raster seines echten 1. Januar und Ostern und Buß-
    /// und Bettag auf seinen echten Daten, abgebildet auf den Jahrestag des Gemeinjahrs. Ein Jahr mit
    /// fremdem Raster gibt es nicht: Jeder Weg zu einem Jahr prüft, dass w₀ das des Jahres ist.</para>
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
        /// Baut den <b>Regelfall</b>: das Raster w₀, ohne Jahr.
        /// </summary>
        /// <param name="wochentagDesErstenTags">w₀: 0 = Montag … 6 = Sonntag für den Jahrestag 1.</param>
        /// <exception cref="ArgumentOutOfRangeException">w₀ außerhalb 0 … 6.</exception>
        public Gemeinjahrkalender(int wochentagDesErstenTags) : this(wochentagDesErstenTags, 0)
        {
        }

        private Gemeinjahrkalender(int wochentagDesErstenTags, int jahr)
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

        /// <summary>
        /// <b>Der Sonderfall:</b> der Kalender eines echten Jahres — das Raster seines 1. Januar und seine
        /// Feiertagsdaten.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Das Jahr liegt außerhalb 1583 … 9999.</exception>
        public static Gemeinjahrkalender Kalenderjahr(int jahr)
        {
            if (jahr < 1583 || jahr > 9999)
                throw new ArgumentOutOfRangeException(nameof(jahr),
                    "Das Jahr liegt zwischen 1583 und 9999 (das Osterdatum ist gregorianisch).");
            return new Gemeinjahrkalender(WochentagJan1(jahr), jahr);
        }

        /// <summary>
        /// Die Konvention aus einem w₀ und einem Jahr, die getrennt geführt werden (Satz des Laufs,
        /// Auswertung zu w₀ und Referenzjahr): Jahr 0 ist der Regelfall <c>new Gemeinjahrkalender(w₀)</c>,
        /// ein Jahr der Sonderfall <see cref="Kalenderjahr"/> — dann muss w₀ das des Jahres sein.
        /// </summary>
        /// <exception cref="ArgumentException">Ein Jahr mit einem w₀, das nicht das seines 1. Januar ist.</exception>
        public static Gemeinjahrkalender Aus(int wochentagDesErstenTags, int jahr)
        {
            if (jahr == 0) return new Gemeinjahrkalender(wochentagDesErstenTags);
            Gemeinjahrkalender k = Kalenderjahr(jahr);
            if (k.W0 != wochentagDesErstenTags)
                throw new ArgumentException("w₀ " + wochentagDesErstenTags.ToString(CultureInfo.InvariantCulture) +
                    " ist nicht der Wochentag des 1. Januar " + jahr.ToString(CultureInfo.InvariantCulture) +
                    " (w₀ " + k.W0.ToString(CultureInfo.InvariantCulture) + "): Ein Jahr trägt sein eigenes Raster.",
                    nameof(wochentagDesErstenTags));
            return k;
        }

        /// <summary>w₀ eines echten Jahres: der Wochentag seines 1. Januar, 0 = Montag … 6 = Sonntag.</summary>
        public static int WochentagJan1(int jahr) => ((int)new DateTime(jahr, 1, 1).DayOfWeek + 6) % 7;

        /// <summary>w₀: 0 = Montag … 6 = Sonntag für den Jahrestag 1.</summary>
        public int W0 { get; }

        /// <summary>Das Jahr der Preisreihe; 0 = Regelfall ohne Jahr. Mit Jahr ist <see cref="W0"/> das dieses Jahres.</summary>
        public int Jahr { get; }

        /// <summary>Trägt die Konvention ein Jahr (Sonderfall)?</summary>
        public bool MitJahr => Jahr > 0;

        /// <summary>Der Wochentag (0 = Montag … 6 = Sonntag) eines Jahrestags 1 … 365 im Raster.</summary>
        public int Wochentag(int jahrestag) => ((W0 + jahrestag - 1) % 7 + 7) % 7;

        /// <summary>
        /// <b>Der Jahrestag des Ostersonntags</b> im Gemeinjahr. Ohne Jahr der Sonntag des Rasters am
        /// nächsten zum 8. April (bei Gleichstand der frühere), mit Jahr das echte Osterdatum — ein Sonntag
        /// des Rasters, weil das Raster das des Jahres ist.
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
