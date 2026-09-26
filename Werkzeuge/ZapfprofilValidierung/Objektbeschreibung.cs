using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WindowsFormsApplication1;

namespace ZapfprofilValidierung
{
    /// <summary>Ein Ferienfenster der Beschreibung (Jahrestage 1 … 365 einschließlich).</summary>
    internal sealed class Ferienangabe
    {
        public int Beginn { get; set; }
        public int Ende { get; set; }
    }

    /// <summary>Was der Anwender über die gemessene Reihe sagt (der Rest steht in ihrer Kopfzeile).</summary>
    internal sealed class Messangabe
    {
        /// <summary>Der Dateiname der Reihe im Objektordner; leer = <c>messreihe.csv</c>.</summary>
        public string Datei { get; set; }

        /// <summary>„Energie", „Volumen", „Leistung"; leer = die Einheit der Kopfzeile entscheidet.</summary>
        public string Groesse { get; set; }

        /// <summary>„Ortszeit" (Vorgabe) oder „Normalzeit".</summary>
        public string Zeitstempel { get; set; }

        /// <summary>Höchster zugelassener Lückenanteil; <c>null</c> = der Parameter des Katalogs.</summary>
        public double? LueckenanteilHoechstens { get; set; }

        /// <summary>Die Quelle der Reihe — ein anonymer Vermerk (Zählerart, Zeitraum), kein Objektname.</summary>
        public string Quelle { get; set; }
    }

    /// <summary>Der Kalender des Objektjahrs (der Kern kennt keine Feiertagstabelle, 4.2).</summary>
    internal sealed class Kalenderangabe
    {
        /// <summary>Wochentag des 1. Januar, 0 = Montag … 6 = Sonntag.</summary>
        public int WochentagJan1 { get; set; }

        /// <summary>Die Feiertagsregion — nur ein Vermerk für den Bericht, sie rechnet nicht.</summary>
        public string Feiertagsregion { get; set; }

        /// <summary>Die Feiertage als Jahrestage 1 … 365; sie tragen Sonntagsmenge und Sonntagsgang.</summary>
        public int[] Feiertage { get; set; }

        /// <summary>Bis zu vier Ferienfenster der Zone.</summary>
        public Ferienangabe[] Ferien { get; set; }
    }

    /// <summary>
    /// <b>Woher die Bezugsmenge eines Messobjekts stammt</b> (<c>bezugsmenge_herkunft</c>). Sie
    /// entscheidet, ob das Objekt die √N-Skalierung trägt: Eine gesetzte runde Zahl sagt nichts über
    /// die Größe des Objekts.
    /// </summary>
    internal enum Bezugsmengenherkunft
    {
        /// <summary>Aus der Veröffentlichung der Quelle belegt.</summary>
        Veroeffentlichung = 1,

        /// <summary>Aus einer belegten Größe mit einer benannten Annahme abgeleitet (Wohnungen mal Belegung).</summary>
        Abgeleitet = 2,

        /// <summary>Eine gesetzte Zahl ohne Beleg.</summary>
        Platzhalter = 3,

        /// <summary>Unbekannt — nur ein Rechenwert größer null; das Niveau kommt allein aus der Kalibrierung.</summary>
        Unbekannt = 4
    }

    /// <summary>Die Stochastik des Objekts (4.4); ohne Ensemble bleibt die Spitzenstreuung leer.</summary>
    internal sealed class Stochastikangabe
    {
        public int Seed { get; set; } = 1;

        /// <summary>Zahl der Realisierungen des Jahresensembles; 0 oder 1 = keines.</summary>
        public int Realisierungen { get; set; }

        /// <summary>
        /// Ist die verglichene Jahresreihe die gezogene Realisierung zum Seed (<c>true</c>) oder der
        /// deterministische Pfad? Vorgabe <c>true</c>, sobald ein Ensemble gezogen wird — der Bericht
        /// hält dann genau das gegen die Messung, was der Lauf rechnet.
        /// </summary>
        public bool? JahresreiheStochastisch { get; set; }
    }

    /// <summary>
    /// <b>Die Beschreibung eines Messobjekts</b> — <c>objekt.json</c> neben der Messreihe. Sie trägt
    /// die <b>anonyme Kennung</b>, den Bezeichner der Nutzungsart aus dem Katalog, die Bezugsmenge,
    /// den Kalender, die Temperaturen, die Bilanzgrenze des Zählers und die Stochastik.
    ///
    /// <para><b>Kein Objektname, keine Anschrift, kein Kunde</b> (Konzept Kapitel 9 K5): Die Kennung
    /// ist das Einzige, was in einen Bericht kommt, und sie ist Sache des Anwenders. Das Werkzeug
    /// prüft das nicht inhaltlich — es kann einen Namen nicht von einer Kennung unterscheiden —,
    /// aber die Datei bleibt beim Anwender, und der Bericht führt nichts weiter.</para>
    /// </summary>
    internal sealed class Objektbeschreibung
    {
        /// <summary>Die anonyme Kennung, etwa <c>MFH-01</c>; sie steht in jedem Bericht.</summary>
        public string Kennung { get; set; }

        /// <summary>Der Bezeichner der Nutzungsart im Katalog (natürlicher Schlüssel, 3.1).</summary>
        public string Nutzungsart { get; set; }

        /// <summary>Die Bezugsmenge der Zone in der Bezugsart der Nutzungsart (Personen, WE, Betten …).</summary>
        public double Bezugsmenge { get; set; }

        /// <summary>
        /// Woher die Bezugsmenge stammt: „Veroeffentlichung", „Abgeleitet", „Platzhalter",
        /// „Unbekannt"; leer = nicht angegeben (zählt als belastbar, wie vor der Angabe).
        /// </summary>
        public string BezugsmengeHerkunft { get; set; }

        /// <summary>„Niedrig", „Mittel" (Vorgabe), „Hoch".</summary>
        public string Niveau { get; set; }

        /// <summary>Die Bilanzgrenze des Zählers: „Zapfstelle", „MitVerteilung", „MitSpeicher".</summary>
        public string Bilanzgrenze { get; set; }

        /// <summary>Rechnet die Zone eine Zirkulation? Vorgabe <c>true</c>.</summary>
        public bool Zirkulation { get; set; } = true;

        /// <summary>Der Speicherverlust [kWh/a] — nur bei Bilanzgrenze „MitSpeicher".</summary>
        public double? SpeicherverlustKwhJeJahr { get; set; }

        /// <summary>Die Zapftemperatur der Zone [°C]; leer = die Bezugstemperatur des Katalogs.</summary>
        public double? ZapftemperaturC { get; set; }

        /// <summary>Das Kaltwassermittel der Zone [°C]; leer = der Katalogparameter.</summary>
        public double? KaltwasserMittelC { get; set; }

        /// <summary>Die Amplitude des Kaltwassergangs [K]; leer = der Katalogparameter.</summary>
        public double? KaltwasserAmplitudeK { get; set; }

        public Kalenderangabe Kalender { get; set; }
        public Messangabe Messung { get; set; }
        public Stochastikangabe Stochastik { get; set; }

        /// <summary>Freier Vermerk für den Bericht (Anlagenart, Zeitraum) — nichts Identifizierendes.</summary>
        public string Vermerk { get; set; }

        private static readonly JsonSerializerOptions Optionen = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        /// <summary>Liest die Beschreibung; <paramref name="fehler"/> trägt den Grund bei <c>null</c>.</summary>
        internal static Objektbeschreibung Lesen(string pfad, out string fehler)
        {
            fehler = null;
            try
            {
                Objektbeschreibung o = JsonSerializer.Deserialize<Objektbeschreibung>(
                    File.ReadAllText(pfad, System.Text.Encoding.UTF8), Optionen);
                if (o == null) { fehler = "Die Datei ist leer."; return null; }
                o.Pruefen(out fehler);
                return fehler == null ? o : null;
            }
            catch (JsonException ex) { fehler = "JSON nicht lesbar: " + ex.Message; return null; }
            catch (IOException ex) { fehler = "Datei nicht lesbar: " + ex.Message; return null; }
        }

        private void Pruefen(out string fehler)
        {
            fehler = null;
            if (string.IsNullOrWhiteSpace(Kennung)) { fehler = "\"kennung\" fehlt."; return; }
            if (string.IsNullOrWhiteSpace(Nutzungsart)) { fehler = "\"nutzungsart\" fehlt."; return; }
            if (!(Bezugsmenge > 0.0)) { fehler = "\"bezugsmenge\" muss groesser als 0 sein."; return; }
            if (BezugsmengeHerkunft != null && AlsHerkunft(BezugsmengeHerkunft) == null)
            {
                fehler = "\"bezugsmenge_herkunft\" kennt nur Veroeffentlichung, Abgeleitet, Platzhalter, "
                         + "Unbekannt - nicht \"" + BezugsmengeHerkunft + "\".";
                return;
            }
            if (Kalender == null) { fehler = "\"kalender\" fehlt."; return; }
            if (Kalender.WochentagJan1 < 0 || Kalender.WochentagJan1 > 6)
            {
                fehler = "\"kalender.wochentag_jan1\" liegt ausserhalb 0 (Montag) bis 6 (Sonntag).";
                return;
            }
            foreach (int f in Kalender.Feiertage ?? new int[0])
                if (f < 1 || f > Zapfkalender.TAGE)
                {
                    fehler = "\"kalender.feiertage\" nennt den Jahrestag " + f.ToString(CultureInfo.InvariantCulture)
                             + " ausserhalb 1 bis 365.";
                    return;
                }
            foreach (Ferienangabe f in Kalender.Ferien ?? new Ferienangabe[0])
                if (f == null || f.Beginn < 1 || f.Ende > Zapfkalender.TAGE || f.Beginn > f.Ende)
                {
                    fehler = "\"kalender.ferien\" fuehrt ein Fenster ausserhalb 1 bis 365 oder mit Ende vor Beginn.";
                    return;
                }
            if ((Kalender.Ferien?.Length ?? 0) > 4) { fehler = "\"kalender.ferien\" fuehrt mehr als vier Fenster."; return; }
            if (Niveau != null && AlsNiveau(Niveau) == null)
            {
                fehler = "\"niveau\" kennt nur Niedrig, Mittel, Hoch - nicht \"" + Niveau + "\".";
                return;
            }
            if (Bilanzgrenze != null && AlsGrenze(Bilanzgrenze) == null)
            {
                fehler = "\"bilanzgrenze\" kennt nur Zapfstelle, MitVerteilung, MitSpeicher - nicht \""
                         + Bilanzgrenze + "\".";
                return;
            }
            if (Messung?.Groesse != null && AlsGroesse(Messung.Groesse) == null)
            {
                fehler = "\"messung.groesse\" kennt nur Energie, Volumen, Leistung - nicht \"" + Messung.Groesse + "\".";
                return;
            }
            if (Messung?.Zeitstempel != null && AlsZeitstempel(Messung.Zeitstempel) == null)
            {
                fehler = "\"messung.zeitstempel\" kennt nur Ortszeit und Normalzeit - nicht \""
                         + Messung.Zeitstempel + "\".";
                return;
            }
            if ((Stochastik?.Realisierungen ?? 0) > Jahresensemble.HOECHSTENS)
                fehler = "\"stochastik.realisierungen\" liegt ueber der Grenze des Kerns ("
                         + Jahresensemble.HOECHSTENS.ToString(CultureInfo.InvariantCulture) + ").";
        }

        /// <summary>Der Dateiname der Reihe; Vorgabe <c>messreihe.csv</c>.</summary>
        internal string Messdatei => string.IsNullOrWhiteSpace(Messung?.Datei) ? "messreihe.csv" : Messung.Datei;

        internal ZapfNiveau NiveauWert => AlsNiveau(Niveau) ?? ZapfNiveau.Mittel;

        /// <summary>Die Herkunft der Bezugsmenge; <c>null</c> = nicht angegeben.</summary>
        internal Bezugsmengenherkunft? HerkunftWert => AlsHerkunft(BezugsmengeHerkunft);

        internal ZapfBilanzgrenze? GrenzeWert => AlsGrenze(Bilanzgrenze);

        internal ZapfMessgroesse? GroesseWert => AlsGroesse(Messung?.Groesse);

        internal Messzeitstempel ZeitstempelWert => AlsZeitstempel(Messung?.Zeitstempel) ?? Messzeitstempel.Ortszeit;

        /// <summary>Die Wochenend- und Feiertagskennzeichen des Objektjahrs (365 Stellen).</summary>
        internal bool[] WeBilden()
        {
            var we = new bool[Zapfkalender.TAGE];
            for (int d = 1; d <= Zapfkalender.TAGE; d++)
                we[d - 1] = Zapfkalender.Wochentag(Kalender.WochentagJan1, d) >= Zapfkalender.SAMSTAG;
            foreach (int f in Kalender.Feiertage ?? new int[0]) we[f - 1] = true;
            return we;
        }

        /// <summary>Die Ferienfenster als Angaben des Kerns.</summary>
        internal IReadOnlyList<Ferienfenster> FerienBilden()
        {
            var liste = new List<Ferienfenster>();
            foreach (Ferienangabe f in Kalender.Ferien ?? new Ferienangabe[0])
                liste.Add(new Ferienfenster(f.Beginn, f.Ende));
            return liste;
        }

        private static Bezugsmengenherkunft? AlsHerkunft(string s)
            => s == null ? null
               : Enum.TryParse(s, true, out Bezugsmengenherkunft w) && Enum.IsDefined(typeof(Bezugsmengenherkunft), w)
                 ? w : (Bezugsmengenherkunft?)null;

        private static ZapfNiveau? AlsNiveau(string s)
            => s == null ? null
               : Enum.TryParse(s, true, out ZapfNiveau w) && Enum.IsDefined(typeof(ZapfNiveau), w) ? w : (ZapfNiveau?)null;

        private static ZapfBilanzgrenze? AlsGrenze(string s)
            => s == null ? null
               : Enum.TryParse(s, true, out ZapfBilanzgrenze w) && Enum.IsDefined(typeof(ZapfBilanzgrenze), w)
                 ? w : (ZapfBilanzgrenze?)null;

        private static ZapfMessgroesse? AlsGroesse(string s)
            => s == null ? null
               : Enum.TryParse(s, true, out ZapfMessgroesse w) && Enum.IsDefined(typeof(ZapfMessgroesse), w)
                 ? w : (ZapfMessgroesse?)null;

        private static Messzeitstempel? AlsZeitstempel(string s)
            => s == null ? null
               : Enum.TryParse(s, true, out Messzeitstempel w) && Enum.IsDefined(typeof(Messzeitstempel), w)
                 ? w : (Messzeitstempel?)null;
    }
}
