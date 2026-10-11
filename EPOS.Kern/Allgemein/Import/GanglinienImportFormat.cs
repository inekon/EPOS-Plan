using System;
using System.Globalization;
using System.Threading.Tasks;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>Wie der Optionendialog die gelesene Reihe zeigt.</summary>
    public enum GanglinienBildart
    {
        /// <summary>Jahresverlauf über die Jahresstunden (Ganglinie, Quellprofil, Spotpreis).</summary>
        Jahr = 0,

        /// <summary>Tagesansicht (TWW-Messreihen); das Bild liefert <see cref="GanglinienImportFormat.Dateibild"/> oder der <see cref="GanglinienImportFormat.Bildzeichner"/>.</summary>
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
        /// Zeichnet das Bild selbst (für <see cref="GanglinienBildart.Tag"/> ohne <see cref="Dateibild"/>); <c>null</c> = der
        /// Jahresverlauf aus <see cref="Bildreihe"/>.
        /// </summary>
        public Func<double[], Zeichenmodell> Bildzeichner { get; init; }

        /// <summary>
        /// Eine weitere Prüfung der gelesenen Werte nach der Werteanzahl: der benannte Grund einer
        /// Ablehnung, leer = in Ordnung; <c>null</c> = keine.
        /// </summary>
        public Func<double[], string> Pruefung { get; init; }

        /// <summary>
        /// Schlägt das Jahr des Jahresfeldes aus der Datei vor (Spotpreis: das Jahr der ersten
        /// Datenzeile); <c>null</c> oder 0 = kein Vorschlag. Nur mit <see cref="Jahresfeld"/>.
        /// </summary>
        public Func<string, int> Jahrvorschlag { get; init; }

        /// <summary>
        /// <b>Das Bild aus der Datei selbst</b>, gelesen mit dem Fachleser des Formats (Spotpreis:
        /// Kalenderaufbereitung der Stundenpreise; TWW-Messreihe: Zeitstempel, Lücken, Sommerzeit).
        /// Hat ein Format ihn, ist ER die Prüfung: Seine Ablehnung ist der Grund, seine Werte sind
        /// die Werte der Probe, sein Bild ersetzt <see cref="Bildzeichner"/>. Die Wertspalte des
        /// Dialogs wirkt dann nur auf Vorschautabelle und Zeilenzählung — so liest der Dialog genau,
        /// was der Weg danach übernimmt. <c>null</c> = die Wertspalte des Dialogs.
        /// </summary>
        public Func<string, GanglinienDateibild> Dateibild { get; init; }

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

        /// <summary>
        /// <b>Die Spotpreisreihe</b>: Preis je Stunde in ct/kWh — so liest die Datei
        /// (Spalte „Spotmarktpreis in ct/kWh“) und so legt der Import sie ab
        /// (<see cref="DbWerte.PREISREIHE_EINHEIT_CT_KWH"/>). Die Werteanzahl der DATEI ist frei
        /// (8 784 Zeilen im Schaltjahr, Lücken, doppelte Stunden der Zeitumstellung); die
        /// Kalenderaufbereitung (<see cref="SpotpreisImportCtrl.Pruefe"/>) macht daraus immer genau
        /// 8 760 Stundenpreise, und genau die zeigt das Jahresbild. Preisachse, Jahresfeld mit dem
        /// Jahr der Datei als Vorschlag.
        /// </summary>
        public static GanglinienImportFormat Spotpreis() => new GanglinienImportFormat
        {
            Kennung = KENNUNG_SPOTPREIS,
            Einheit = DbWerte.PREISREIHE_EINHEIT_CT_KWH,
            AchsenSchluessel = nameof(MyResource.Resource.IMPORT_ACHSE_SPOTPREIS),
            Farbrolle = Farbrolle.KOSTENPROFIL,
            Werteanzahl = 0,
            Jahresfeld = true,
            TitelSchluessel = nameof(MyResource.Resource.IMPORT_TITEL_SPOTPREIS),
            Jahrvorschlag = pfad => SpotpreisLeser.LiesDatei(pfad).Jahr,
            Dateibild = SpotpreisDateibild
        };

        /// <summary>Die Spotpreisdatei über den Weg des Imports (Leser und Kalenderaufbereitung).</summary>
        internal static GanglinienDateibild SpotpreisDateibild(string pfad)
        {
            SpotpreisImportCtrl.Lauf lauf = new SpotpreisImportCtrl().Pruefe(pfad);
            if (!lauf.Erfolgreich || lauf.Reihe == null)
                return GanglinienDateibild.Abgelehnt(string.IsNullOrWhiteSpace(lauf.Protokoll)
                    ? MyResource.Resource.PREIS_IMPORT_KEINE_DATEN : lauf.Protokoll.Trim());
            double[] werte = (double[])lauf.Reihe.StundenreiheCtKwh.Clone();
            GanglinienImportFormat f = Spotpreis();
            CultureInfo k = CultureInfo.CurrentCulture;
            string kennzahl = string.Format(k, MyResource.Resource.IMPORT_PROBE_SPOTPREIS,
                lauf.Jahr.ToString(CultureInfo.InvariantCulture),
                lauf.Reihe.MittelCtKwh.ToString("0.000", k),
                lauf.Reihe.MinCtKwh.ToString("0.000", k),
                lauf.Reihe.MaxCtKwh.ToString("0.000", k));
            return new GanglinienDateibild(
                ChartRenderer.JahresverlaufModell("", werte, f.Achsentitel, f.Farbrolle), werte, kennzahl, "");
        }

        /// <summary>
        /// <b>Die TWW-Messreihe</b> des Zapfprofils: Tagesansicht statt Jahresbild
        /// (<see cref="GanglinienTagesbild"/>), Werteanzahl frei (die Auflösung misst der Leser
        /// aus den Zeitstempeln). Gelesen wird mit dem Leser des Einspielens
        /// (<see cref="Messreihenleser"/>) unter den Eingaben der Maske — den Strom öffnet die
        /// Hülle, deshalb kommt <paramref name="dateibild"/> von ihr.
        /// </summary>
        /// <param name="groesse">Die gewählte Größe; <c>null</c> = aus der Kopfzeile (Einheit erst aus der Datei).</param>
        /// <param name="dateibild">Der Leser der Hülle: Pfad → Tagesbild oder benannte Ablehnung.</param>
        internal static GanglinienImportFormat TwwMessreihe(ZapfMessgroesse? groesse,
                                                            Func<string, GanglinienDateibild> dateibild)
            => new GanglinienImportFormat
            {
                Kennung = KENNUNG_TWW_MESSREIHE,
                Einheit = GanglinienTagesbild.Einheit(groesse),
                AchsenSchluessel = groesse == ZapfMessgroesse.Volumen
                    ? nameof(MyResource.Resource.IMPORT_ACHSE_TWW_VOLUMEN)
                    : nameof(MyResource.Resource.IMPORT_ACHSE_TWW_ENERGIE),
                Farbrolle = Farbrolle.WARMWASSER,
                Werteanzahl = 0,
                Bildart = GanglinienBildart.Tag,
                TitelSchluessel = nameof(MyResource.Resource.IMPORT_TITEL_TWW_MESSREIHE),
                Dateibild = dateibild
            };
    }

    /// <summary>
    /// Was der Fachleser eines Formats aus der Datei gemacht hat (<see cref="GanglinienImportFormat.Dateibild"/>):
    /// das Bild, die Werte in der Einheit des Formats, eine Kennzahlenzeile und — bei einer
    /// Ablehnung — der benannte Grund.
    /// </summary>
    /// <param name="Modell">Das Bild; <c>null</c> bei einer Ablehnung.</param>
    /// <param name="Werte">Die Werte, wie der Weg sie übernimmt.</param>
    /// <param name="Kennzahl">Eine Zeile zu den Werten (Jahr, Tage); leer = keine.</param>
    /// <param name="Grund">Der Grund einer Ablehnung; leer bei Erfolg.</param>
    /// <param name="Einheit">Die Einheit der Werte, wenn erst die Datei sie nennt; leer = die des Formats.</param>
    public sealed record GanglinienDateibild(Zeichenmodell Modell, double[] Werte, string Kennzahl, string Grund,
                                             string Einheit = "")
    {
        /// <summary>Eine benannte Ablehnung.</summary>
        public static GanglinienDateibild Abgelehnt(string grund)
            => new GanglinienDateibild(null, Array.Empty<double>(), "", grund ?? "");
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
