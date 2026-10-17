using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
    /// Ein Zeitraum der Ferienliste eines Betriebskalenders (<c>Tab_Betriebskalenderferien</c>,
    /// <see cref="BetriebskalenderFerienlisteSchema"/>): wahlfreier Name und Jahrestage im Gemeinjahr (1 … 365), Beginn nach
    /// Ende heißt über den Jahreswechsel — dieselbe Lesart wie die Spaltenpaare.
    /// </summary>
    public sealed record Betriebsferien(string Name, int Von, int Bis)
    {
        /// <summary>Der Zeitraum ohne Namen.</summary>
        public Ferienzeitraum Zeitraum => new Ferienzeitraum(Von, Bis);
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
    /// <see cref="FeiertagWieSonntag"/> gesetzt ist. Ferien gehen Feiertagen vor. Mit
    /// <see cref="FerienWieSonntag"/> trägt auch ein Ferientag den Sonntag des Wochenprofils.</para>
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

        /// <summary>Die Betriebsferien der vier Spaltenpaare (höchstens <see cref="BedarfNetzKalenderSchema.FERIEN_ANZAHL"/>).</summary>
        public List<Ferienzeitraum> Ferien { get; set; } = new List<Ferienzeitraum>();

        /// <summary>
        /// Die weiteren Betriebsferien der Ferienliste (<see cref="BetriebskalenderFerienlisteSchema"/>), beliebig viele, in
        /// ihrer Reihenfolge — sie ergänzen die Spaltenpaare wie die Ferienliste des Gebäudes dessen vier Spalten.
        /// </summary>
        public List<Betriebsferien> Ferienliste { get; set; } = new List<Betriebsferien>();

        /// <summary>
        /// <b>Alle Ferienzeiträume</b> in der Reihenfolge des Rechenwegs: die Spaltenpaare 1 … 4, danach die Ferienliste.
        /// Ungültige oder fehlende Einträge stehen mit drin; <see cref="Tagesarten"/> überspringt sie, <see cref="Pruefen"/>
        /// lehnt sie ab.
        /// </summary>
        public List<Ferienzeitraum> AlleFerien()
        {
            var alle = new List<Ferienzeitraum>(Ferien ?? new List<Ferienzeitraum>());
            if (Ferienliste != null)
                foreach (Betriebsferien b in Ferienliste) alle.Add(b?.Zeitraum);
            return alle;
        }

        /// <summary>
        /// <b>Teilt Ferienzeiträume auf Spalten und Liste auf</b> — die Regel von „Aus Gebäude übernehmen…“: die ersten
        /// <see cref="BedarfNetzKalenderSchema.FERIEN_ANZAHL"/> in die Spaltenpaare, alle weiteren in ihrer Reihenfolge in
        /// die Ferienliste. Leere Einträge fallen weg.
        /// </summary>
        public static (List<Ferienzeitraum> Spalten, List<Ferienzeitraum> Liste) FerienAufteilen(IEnumerable<Ferienzeitraum> zeitraeume)
        {
            var spalten = new List<Ferienzeitraum>();
            var liste = new List<Ferienzeitraum>();
            if (zeitraeume != null)
                foreach (Ferienzeitraum f in zeitraeume)
                {
                    if (f == null) continue;
                    if (spalten.Count < BedarfNetzKalenderSchema.FERIEN_ANZAHL) spalten.Add(f);
                    else liste.Add(f);
                }
            return (spalten, liste);
        }

        /// <summary>
        /// Übernimmt Ferienzeiträume (etwa eines Gebäudes) als KOPIE: Spalten und Liste werden ersetzt
        /// (<see cref="FerienAufteilen"/>); die Zeiträume der Liste tragen keinen Namen.
        /// </summary>
        public void FerienUebernehmen(IEnumerable<Ferienzeitraum> zeitraeume)
        {
            var (spalten, liste) = FerienAufteilen(zeitraeume);
            Ferien = spalten;
            Ferienliste = liste.Select(f => new Betriebsferien(null, f.Von, f.Bis)).ToList();
        }

        /// <summary>Ferienfaktor f (0 … 1): Anteil des Tagesmittels, den ein Ferientag trägt; 0 = Stillstand.</summary>
        public double Ferienfaktor { get; set; }

        /// <summary>Feiertage wie Sonntag rechnen (Vorgabe ja); nein = Feiertage wirken nicht.</summary>
        public bool FeiertagWieSonntag { get; set; } = true;

        /// <summary>
        /// Ferientage wie Sonntag rechnen (Vorgabe nein): Ein Ferientag trägt dann Tagesmenge und
        /// Stundengang des Sonntags aus dem Wochenprofil, der <see cref="Ferienfaktor"/> wirkt nicht.
        /// <see cref="FerienKuerzen"/> bleibt wirksam.
        /// </summary>
        public bool FerienWieSonntag { get; set; }

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
        /// Die Tagesart jedes der 365 Tage (Index 0 = 1. Januar) im Gemeinjahr: Ferien (Spalten und Liste) vor Feiertag,
        /// Feiertage nur mit <see cref="FeiertagWieSonntag"/>, gelegt nach der Konvention
        /// <paramref name="kalender"/> (Wochentagsraster des Laufs und Jahr der Preisreihe, E114).
        /// </summary>
        public byte[] Tagesarten(Gemeinjahrkalender kalender)
        {
            var arten = new byte[365];
            if (FeiertagWieSonntag)
                foreach (int t in Landesfeiertage.Jahrestage(Bundesland, kalender))
                    if (t >= 1 && t <= 365) arten[t - 1] = TAG_FEIERTAG;

            foreach (Ferienzeitraum f in AlleFerien())
            {
                if (f == null || !f.Gueltig) continue;
                for (int t = 1; t <= 365; t++)
                    if (f.Enthaelt(t)) arten[t - 1] = TAG_FERIEN;
            }
            return arten;
        }

        /// <summary>
        /// Prüft die Eingaben; <c>null</c> = gültig, sonst der Grund in der Oberflächensprache.
        /// Dieselben Grenzen wie die Prüfklauseln der Tabellen: höchstens vier Zeiträume in den Spalten, beliebig viele in
        /// der Liste; alle Zeiträume (Spalten und Liste) mit gültigen Jahrestagen und ohne Überschneidung.
        /// </summary>
        public string Pruefen()
        {
            string name = (Bezeichner ?? "").Trim();
            if (name.Length == 0) return MyResource.Resource.BKAL_MSG_NAME_FEHLT;
            if (name.Length > 100) return MyResource.Resource.BKAL_MSG_NAME_LANG;
            if (Bundesland != null && !Landesfeiertage.Bekannt(Bundesland)) return MyResource.Resource.BKAL_MSG_BUNDESLAND;
            if (double.IsNaN(Ferienfaktor) || Ferienfaktor < 0 || Ferienfaktor > 1) return MyResource.Resource.BKAL_MSG_FAKTOR;
            if (Ferien != null && Ferien.Count > BedarfNetzKalenderSchema.FERIEN_ANZAHL)
                return MyResource.Resource.BKAL_MSG_FERIEN_ANZAHL;
            if (Ferienliste != null)
                foreach (Betriebsferien b in Ferienliste)
                    if (b != null && (b.Name ?? "").Trim().Length > BetriebskalenderFerienlisteSchema.NAME_MAX)
                        return MyResource.Resource.BKAL_MSG_FERIEN_NAME_LANG;
            List<Ferienzeitraum> alle = AlleFerien();
            foreach (Ferienzeitraum f in alle)
                if (f == null || !f.Gueltig) return MyResource.Resource.BKAL_MSG_FERIEN_DATUM;
            // Überschneidung: Je Jahrestag merkt sich das Raster den ersten Zeitraum (1-basiert in der Gesamtfolge).
            var belegt = new int[365];
            for (int i = 0; i < alle.Count; i++)
                for (int t = 1; t <= 365; t++)
                {
                    if (!alle[i].Enthaelt(t)) continue;
                    if (belegt[t - 1] > 0)
                        return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BKAL_MSG_FERIEN_UEBERSCHNEIDUNG,
                                             belegt[t - 1], i + 1);
                    belegt[t - 1] = i + 1;
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
            Ferienliste = new List<Betriebsferien>(Ferienliste ?? new List<Betriebsferien>()),
            Ferienfaktor = Ferienfaktor,
            FeiertagWieSonntag = FeiertagWieSonntag,
            FerienWieSonntag = FerienWieSonntag,
            FerienKuerzen = FerienKuerzen
        };
    }
}
