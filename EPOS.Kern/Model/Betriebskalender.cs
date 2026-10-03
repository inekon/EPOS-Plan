using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Ferienzeitraum als Jahrestage im Gemeinjahr (1 … 365); Beginn nach Ende heißt über den
    /// Jahreswechsel (Konzept Konditionierungsprofile 3.2, dieselbe Lesart).
    /// </summary>
    public sealed record Ferienzeitraum(int Von, int Bis)
    {
        /// <summary>Liegt der Jahrestag <paramref name="tag"/> (1 … 365) im Zeitraum?</summary>
        public bool Enthaelt(int tag) => Von <= Bis ? tag >= Von && tag <= Bis : tag >= Von || tag <= Bis;

        /// <summary>Sind beide Grenzen gültige Jahrestage?</summary>
        public bool Gueltig => Von >= 1 && Von <= 365 && Bis >= 1 && Bis <= 365;
    }

    /// <summary>
    /// <b>Ein Betriebskalender der Bedarfsprofile</b> (<c>Tab_Betriebskalender</c>,
    /// <see cref="BedarfNetzKalenderSchema"/>; Entscheidungsvorlage Modellgrenzen PW2, BW2; Regeln:
    /// Konzept Simulationsablauf, Abschnitt 17). Er legt Feiertage und Betriebsferien auf das
    /// Wochenprofil einer Profilzuordnung — Brauchwasser, Prozesswärme oder Strom.
    ///
    /// <para><b>Tagtyp-Regel:</b> Ein Ferientag trägt je Stunde <c>f · m_s</c>, mit dem Tagesmittel
    /// des Wochenprofils <c>m_s = (1/7) Σ_d w(d, s)</c> zur Stunde s und dem Ferienfaktor f (0 … 1);
    /// ein Feiertag außerhalb der Ferien trägt den Sonntag des Wochenprofils, wenn
    /// <see cref="FeiertagWieSonntag"/> gesetzt ist. Ferien gehen Feiertagen vor.</para>
    /// </summary>
    public sealed class Betriebskalender
    {
        /// <summary>Schlüssel der Zeile; 0 = neu.</summary>
        public int ID { get; set; }

        /// <summary>Name des Kalenders.</summary>
        public string Bezeichner { get; set; } = "";

        /// <summary>
        /// Länderkennung (<see cref="Landesfeiertage.BUNDESLAENDER"/>) für die Landesfeiertage;
        /// <c>null</c> = nur die neun bundeseinheitlichen Feiertage.
        /// </summary>
        public string Bundesland { get; set; }

        /// <summary>Bis zu vier Betriebsferien.</summary>
        public List<Ferienzeitraum> Ferien { get; set; } = new List<Ferienzeitraum>();

        /// <summary>Ferienfaktor f (0 … 1): Anteil des Tagesmittels, den ein Ferientag trägt; 0 = Stillstand.</summary>
        public double Ferienfaktor { get; set; }

        /// <summary>Feiertage wie Sonntag rechnen (Vorgabe ja); nein = Feiertage wirken nicht.</summary>
        public bool FeiertagWieSonntag { get; set; } = true;

        /// <summary>
        /// Kürzen die Ferien die Monatsmenge? Nein (Vorgabe): Die Monatsnormierung verteilt die
        /// Monatsmenge auf die übrigen Tage um. Ja: Die Menge eines Monats sinkt im Verhältnis der
        /// Ferienstunden.
        /// </summary>
        public bool FerienKuerzen { get; set; }

        /// <summary>Tagesart „gewöhnlicher Tag" in <see cref="Tagesarten"/>.</summary>
        public const byte TAG_NORMAL = 0;

        /// <summary>Tagesart „Feiertag wie Sonntag".</summary>
        public const byte TAG_FEIERTAG = 1;

        /// <summary>Tagesart „Ferientag".</summary>
        public const byte TAG_FERIEN = 2;

        /// <summary>
        /// Die Tagesart jedes der 365 Tage (Index 0 = 1. Januar) im <paramref name="referenzjahr"/>:
        /// Ferien vor Feiertag, Feiertage nur mit <see cref="FeiertagWieSonntag"/>.
        /// </summary>
        public byte[] Tagesarten(int referenzjahr)
        {
            var arten = new byte[365];
            if (FeiertagWieSonntag)
                foreach (int t in Landesfeiertage.Jahrestage(Bundesland, referenzjahr))
                    if (t >= 1 && t <= 365) arten[t - 1] = TAG_FEIERTAG;

            if (Ferien != null)
                foreach (Ferienzeitraum f in Ferien)
                {
                    if (f == null || !f.Gueltig) continue;
                    for (int t = 1; t <= 365; t++)
                        if (f.Enthaelt(t)) arten[t - 1] = TAG_FERIEN;
                }
            return arten;
        }

        /// <summary>
        /// Prüft die Eingaben; <c>null</c> = gültig, sonst der Grund in der Oberflächensprache.
        /// Dieselben Grenzen wie die Prüfklauseln der Tabelle.
        /// </summary>
        public string Pruefen()
        {
            string name = (Bezeichner ?? "").Trim();
            if (name.Length == 0) return MyResource.Resource.BKAL_MSG_NAME_FEHLT;
            if (name.Length > 100) return MyResource.Resource.BKAL_MSG_NAME_LANG;
            if (Bundesland != null && !Landesfeiertage.Bekannt(Bundesland)) return MyResource.Resource.BKAL_MSG_BUNDESLAND;
            if (double.IsNaN(Ferienfaktor) || Ferienfaktor < 0 || Ferienfaktor > 1) return MyResource.Resource.BKAL_MSG_FAKTOR;
            if (Ferien != null)
            {
                if (Ferien.Count > BedarfNetzKalenderSchema.FERIEN_ANZAHL) return MyResource.Resource.BKAL_MSG_FERIEN_ANZAHL;
                foreach (Ferienzeitraum f in Ferien)
                    if (f == null || !f.Gueltig) return MyResource.Resource.BKAL_MSG_FERIEN_DATUM;
            }
            return null;
        }

        /// <summary>Eine flache Kopie mit eigener Ferienliste.</summary>
        public Betriebskalender Kopie() => new Betriebskalender
        {
            ID = ID,
            Bezeichner = Bezeichner,
            Bundesland = Bundesland,
            Ferien = new List<Ferienzeitraum>(Ferien ?? new List<Ferienzeitraum>()),
            Ferienfaktor = Ferienfaktor,
            FeiertagWieSonntag = FeiertagWieSonntag,
            FerienKuerzen = FerienKuerzen
        };
    }
}
