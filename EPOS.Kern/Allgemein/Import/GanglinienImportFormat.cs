using System;
using System.Threading.Tasks;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>Wie der Optionendialog die gelesene Reihe zeigt.</summary>
    public enum GanglinienBildart
    {
        /// <summary>Jahresverlauf über die Jahresstunden (Ganglinie, Quellprofil, Spotpreis).</summary>
        Jahr = 0,

        /// <summary>Tagesansicht (TWW-Messreihen); das Bild liefert der <see cref="GanglinienImportFormat.Bildzeichner"/>.</summary>
        Tag = 1
    }

    /// <summary>
    /// <b>Die Formatbeschreibung des Ganglinienimports</b> (IM-4): WAS eine Datei liefern soll, die über
    /// den Optionendialog „Format und Vorschau“ gelesen wird. Dialog, Baustein und Hülle benutzen
    /// dieselbe Beschreibung — der Dialog blendet danach Felder ein oder aus, die Probelesung
    /// (<see cref="GanglinienProbe.Lies(string, GanglinienImportOptionen, GanglinienImportFormat)"/>)
    /// prüft danach und zeichnet danach.
    ///
    /// <para><b>Ein neues Sonderformat</b> braucht nur eine weitere Fabrik hier (oder in seiner
    /// Hülle) — keine Änderung am Dialog: Einheit und Achse, feste oder freie Werteanzahl, die
    /// Kennzahlen Summe und Höchstwert, das Jahresfeld, die Bildart und bei Bedarf ein eigenes Bild
    /// (<see cref="Bildzeichner"/>) und eine eigene Prüfung (<see cref="Pruefung"/>).</para>
    ///
    /// <para>Die Beschreibung trägt RESSOURCENSCHLÜSSEL, keine Texte: Sie kann einmal gebaut und unter
    /// jeder Sprache benutzt werden.</para>
    /// </summary>
    public sealed class GanglinienImportFormat
    {
        /// <summary>Kennung der Ganglinie (Lastgang in kW, der Weg der Importkette).</summary>
        public const string KENNUNG_GANGLINIE = "GANGLINIE";

        /// <summary>Kennung des Quellprofils der Wärmepumpe (Temperaturen in °C).</summary>
        public const string KENNUNG_QUELLPROFIL = "QUELLPROFIL";

        /// <summary>Kennung der Spotpreisreihe (Teil 2).</summary>
        public const string KENNUNG_SPOTPREIS = "SPOTPREIS";

        /// <summary>Kennung der TWW-Messreihe (Teil 2).</summary>
        public const string KENNUNG_TWW_MESSREIHE = "TWW_MESSREIHE";

        /// <summary>Sprachneutrale Kennung des Formats (Steuerwert, kein Anzeigetext).</summary>
        public string Kennung { get; init; } = KENNUNG_GANGLINIE;

        /// <summary>Einheit der Werte im Bild und in der Probezeile („kW“, „°C“, „€/MWh“).</summary>
        public string Einheit { get; init; } = "kW";

        /// <summary>Ressourcenschlüssel der Wertachse des Bildes.</summary>
        public string AchsenSchluessel { get; init; } = nameof(MyResource.Resource.CHART_ACHSE_LEISTUNG);

        /// <summary>Farbrolle der Reihe im Bild.</summary>
        public Farbrolle Farbrolle { get; init; } = Farbrolle.BEDARF;

        /// <summary>
        /// Feste Werteanzahl (365, 8 760 …); 0 = frei. Eine Datei mit anderer Anzahl wird BENANNT
        /// abgelehnt (Probezeile und Banner), nie aufgefüllt oder still gekürzt.
        /// </summary>
        public int Werteanzahl { get; init; }

        /// <summary>Wählt der Anwender die Einheit (kW, W, kWh …)? Nur bei Lastgängen.</summary>
        public bool Einheitswahl { get; init; }

        /// <summary>Wählt der Anwender Raster und Intervallkonvention? Nur bei Lastgängen.</summary>
        public bool Rasterwahl { get; init; }

        /// <summary>Nennt die Probezeile die Jahressumme (Energie aus der Leistungsreihe)?</summary>
        public bool Summe { get; init; }

        /// <summary>Nennt die Probezeile die Höchstleistung (Nennleistung der Reihe)?</summary>
        public bool Nennleistung { get; init; }

        /// <summary>Hat der Dialog ein Jahresfeld (<see cref="GanglinienImportOptionen.Jahr"/>)?</summary>
        public bool Jahresfeld { get; init; }

        /// <summary>Jahresverlauf oder Tagesansicht.</summary>
        public GanglinienBildart Bildart { get; init; } = GanglinienBildart.Jahr;

        /// <summary>
        /// Ressourcenschlüssel des Dialogtitels; leer = „Format und Vorschau“ (<c>IMPORT_TITEL_OPTIONEN</c>).
        /// </summary>
        public string TitelSchluessel { get; init; } = "";

        /// <summary>
        /// Bereitet die gelesenen Werte für das Jahresbild auf (z. B. 365 Tageswerte → 8 760 Stunden);
        /// <c>null</c> = die Werte selbst.
        /// </summary>
        public Func<double[], double[]> Bildreihe { get; init; }

        /// <summary>
        /// Zeichnet das Bild selbst (Pflicht für <see cref="GanglinienBildart.Tag"/>); <c>null</c> = der
        /// Jahresverlauf aus <see cref="Bildreihe"/>.
        /// </summary>
        public Func<double[], Zeichenmodell> Bildzeichner { get; init; }

        /// <summary>
        /// Eine weitere Prüfung der gelesenen Werte nach der Werteanzahl: der benannte Grund einer
        /// Ablehnung, leer = in Ordnung; <c>null</c> = keine.
        /// </summary>
        public Func<double[], string> Pruefung { get; init; }

        /// <summary>Ist das der Lastgang der Importkette (kein Sonderformat)?</summary>
        public bool IstGanglinie => Kennung == KENNUNG_GANGLINIE;

        /// <summary>Die Wertachse in der aktuellen Sprache.</summary>
        public string Achsentitel => Text(AchsenSchluessel);

        /// <summary>Der Dialogtitel in der aktuellen Sprache.</summary>
        public string Titel => Text(string.IsNullOrEmpty(TitelSchluessel)
            ? nameof(MyResource.Resource.IMPORT_TITEL_OPTIONEN) : TitelSchluessel);

        private static string Text(string schluessel)
        {
            if (string.IsNullOrEmpty(schluessel)) return "";
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel, MyResource.Resource.Culture); }
            catch { }
            return string.IsNullOrEmpty(t) ? schluessel : t;
        }

        // =====================================================================
        // Die Formate
        // =====================================================================

        /// <summary>
        /// Der Lastgang der Importkette: kW, freie Werteanzahl (die Prüfung entscheidet das Raster),
        /// Einheit und Raster wählbar, Jahressumme und Höchstleistung in der Probezeile, Jahresbild.
        /// </summary>
        public static GanglinienImportFormat Ganglinie => new GanglinienImportFormat
        {
            Kennung = KENNUNG_GANGLINIE,
            Einheit = "kW",
            AchsenSchluessel = nameof(MyResource.Resource.CHART_ACHSE_LEISTUNG),
            Farbrolle = Farbrolle.BEDARF,
            Einheitswahl = true,
            Rasterwahl = true,
            Summe = true,
            Nennleistung = true
        };

        /// <summary>
        /// Das Quellprofil der Wärmepumpe: Temperaturen in °C, feste Werteanzahl der Betriebsart
        /// (<see cref="DbWerte.QuellprofilWerteanzahl"/>: 365 Tageswerte oder 8 760 Stundenwerte),
        /// Temperaturachse, ohne Summe und ohne Nennleistung. Das Bild zeigt das Jahresprofil, das
        /// die Simulation daraus macht (<see cref="QuellprofilCtrl.Jahresprofil(string, double[])"/>).
        /// </summary>
        /// <param name="betriebsart">Steuerwert der Betriebsart (<c>WQ_PROFIL_BETRIEBSART_*</c>).</param>
        public static GanglinienImportFormat Quellprofil(string betriebsart) => new GanglinienImportFormat
        {
            Kennung = KENNUNG_QUELLPROFIL,
            Einheit = QuellprofilCtrl.EINHEIT_GRAD_CELSIUS,
            AchsenSchluessel = nameof(MyResource.Resource.CHART_ACHSE_QUELLTEMPERATUR),
            Farbrolle = Farbrolle.QUELLTEMPERATUR,
            Werteanzahl = DbWerte.QuellprofilWerteanzahl(betriebsart),
            TitelSchluessel = nameof(MyResource.Resource.IMPORT_TITEL_QUELLPROFIL),
            Bildreihe = werte => QuellprofilCtrl.Jahresprofil(betriebsart, werte) ?? werte
        };
    }

    /// <summary>
    /// <b>Die Wege eines Formatimports</b> — die Naht zwischen dem Baustein der Oberfläche
    /// (<c>GanglinienFormatLauf</c>) und der Hülle. Die Hülle belegt sie mit den Lesern des Kerns
    /// hinter ihrer Fangstelle (Lesefehler im Ausnahmeprotokoll, benannte Ablehnung).
    /// </summary>
    public sealed class GanglinienFormatwege
    {
        /// <summary>Erkennt das Dateiformat; die Probe liest nach dem Format.</summary>
        public Func<string, GanglinienImportFormat, Task<GanglinienVorschau>> Erkennen { get; init; }

        /// <summary>Zerlegt die Datei mit den gewählten Optionen neu („Vorschau aktualisieren“, OK).</summary>
        public Func<string, GanglinienImportOptionen, GanglinienImportFormat, Task<GanglinienVorschau>> Vorschau { get; init; }

        /// <summary>
        /// Der Kern selbst, hinter keiner Fangstelle — für Tests und als Vorlage der Hüllen.
        /// </summary>
        public static GanglinienFormatwege Kern() => new GanglinienFormatwege
        {
            Erkennen = (pfad, format) => Task.FromResult(GanglinienDatei.Erkenne(pfad, format)),
            Vorschau = (pfad, o, format) => Task.FromResult(GanglinienDatei.Vorschau(pfad, o, format))
        };
    }

    /// <summary>
    /// Das Ergebnis eines Formatimports über den Optionendialog: die bestätigten Optionen und die
    /// Probelesung der ganzen Datei unter ihnen.
    /// </summary>
    /// <param name="Optionen">Die bestätigten Optionen (mit <see cref="GanglinienImportOptionen.Jahr"/>); <c>null</c>, wenn kein Dialog stand.</param>
    /// <param name="Probe">Die Probelesung; <c>null</c> nur bei einer unlesbaren Datei.</param>
    /// <param name="Grund">Warum nichts gelesen wurde; leer bei Erfolg.</param>
    public sealed record GanglinienFormatErgebnis(GanglinienImportOptionen Optionen, GanglinienProbe Probe, string Grund)
    {
        /// <summary>Passen die Werte zum Format?</summary>
        public bool Erfolgreich => Probe != null && Probe.Erfolgreich && string.IsNullOrEmpty(Grund);

        /// <summary>Die gelesenen Werte in der Einheit des Formats; leer ohne Erfolg.</summary>
        public double[] Werte => Erfolgreich ? Probe.Werte : Array.Empty<double>();
    }
}
