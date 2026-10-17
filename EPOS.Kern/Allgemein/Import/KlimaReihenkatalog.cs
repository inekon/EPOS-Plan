using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>Die Gruppe einer Größe in der Reihenwahl (Konzept Klimadatenimport § 4a).</summary>
    public enum KlimaReihengruppe
    {
        /// <summary>Lufttemperatur, Feuchte, Wasserdampfgehalt.</summary>
        TemperaturFeuchte = 0,
        /// <summary>Global-, Direkt-, Diffus-, Gegen- und Ausstrahlung.</summary>
        Strahlung = 1,
        /// <summary>Die vier Fassadenwerte (nur im Jahresverlauf).</summary>
        Fassadenwerte = 2,
        /// <summary>Bedeckungsgrad, Luftdruck, Wind.</summary>
        BewoelkungDruckWind = 3,
        /// <summary>Hat einen eigenen Reiter oder kein Bild (Sonnenwinkel, Windrichtung, Qualitätskennung).</summary>
        Ohne = 4
    }

    /// <summary>Wie eine Größe zum Monatswert verdichtet wird.</summary>
    public enum KlimaMonatsregel
    {
        /// <summary>Monatsmittel über die vorhandenen Stunden.</summary>
        Mittel = 0,
        /// <summary>Monatssumme ∑ W/m² · 1 h / 1 000 = kWh/m² (Strahlungen).</summary>
        Summe = 1,
        /// <summary>Kein Bild (Windrichtung: ein Winkelmittel täuscht; Qualitätskennung: keine Größe; KD-Q12).</summary>
        KeinBild = 2
    }

    /// <summary>Woher die Werte einer Größe kommen.</summary>
    public enum KlimaReihenherkunft
    {
        /// <summary>Eine Spalte von <c>Tab_Solar(_STAMM)</c>, die die Quelle führt.</summary>
        Rechenspalte = 0,
        /// <summary>Eine Spalte von <c>Tab_Solar(_STAMM)</c>, die EPOS-Plan rechnet (Fassadenwerte, Sonnenwinkel).</summary>
        Gerechnet = 1,
        /// <summary>Ein Rohwert der Quelle aus <c>Tab_Klimastunde_STAMM</c> (KD-4); gezeigt, sobald der Schritt 238 steht (Teil B).</summary>
        Rohwert = 2
    }

    /// <summary>
    /// <b>Eine Größe des Reihenkatalogs</b> (Auftrag KD-8). Der <see cref="Schluessel"/> steht allein hier;
    /// <c>KlimaExportOptionen.Groessen</c> (KD-5) nimmt Schlüssel, Spalte und Einheit von hier — eine Quelle für
    /// Reihenwahl und Export.
    /// </summary>
    public sealed class KlimaReihe
    {
        /// <summary>Der Schlüssel (<c>R_GLOBAL</c>, <c>F_SUED</c>, <c>ROH_Luftdruck_hPa</c> …).</summary>
        public string Schluessel { get; init; } = "";

        /// <summary>Die Größe des Parameterbestands; <c>null</c> bei Fassadenwerten und Sonnenwinkel.</summary>
        public KlimaParameter? Parameter { get; init; }

        /// <summary>Der volle Name in der aktuellen Sprache.</summary>
        public Func<string> Name { get; init; } = () => "";

        /// <summary>Der kurze Name auf dem Chip.</summary>
        public Func<string> Kurzname { get; init; } = () => "";

        /// <summary>Die Einheit der Stundenwerte (W/m², °C, %, Achtel …).</summary>
        public string Einheit { get; init; } = "";

        public KlimaReihengruppe Gruppe { get; init; }

        public KlimaMonatsregel Monatsregel { get; init; }

        public KlimaReihenherkunft Herkunft { get; init; }

        /// <summary>Die Spalte in <c>Tab_Solar(_STAMM)</c> bzw. in der Rohwerttabelle; leer = keine.</summary>
        public string Spalte { get; init; } = "";

        /// <summary>Die Reihenfolge in Wahl und Export.</summary>
        public int Reihenfolge { get; init; }

        /// <summary>Die Farbrolle der Reihe im Bild.</summary>
        public Farbrolle Rolle { get; init; } = Farbrolle.UNBENANNT;

        /// <summary>Steht als Chip in der Vorschau (Monatsbild)?</summary>
        public bool InVorschau { get; init; }

        /// <summary>Steht als Chip im Reiter „Weitere Reihen“ des Jahresverlaufs?</summary>
        public bool ImJahresverlauf { get; init; }

        /// <summary>Die Einheit im Monatsbild: kWh/m² bei Summen, sonst die der Stundenwerte.</summary>
        public string Monatseinheit => Monatsregel == KlimaMonatsregel.Summe ? KlimaReihenkatalog.EINHEIT_SUMME : Einheit;

        /// <summary>Ein Rohwert der Quelle (Marke „R“; gezeigt, sobald die Rohwerttabelle des Schritts 238 steht, Teil B).</summary>
        public bool IstRohwert => Herkunft == KlimaReihenherkunft.Rohwert;
    }

    /// <summary>Eine Reihe einer Region mit ihren Stundenwerten und der Aussage, ob die Quelle sie führt.</summary>
    public sealed class KlimaReihenwerte
    {
        public KlimaReihe Reihe { get; init; }

        /// <summary>Die Stundenwerte (NULL als <c>NaN</c>).</summary>
        public double[] Werte { get; init; } = Array.Empty<double>();

        /// <summary>Führt die Quelle die Größe? Nein bei durchgehend NULL oder durchgehend 0.</summary>
        public bool Gefuehrt { get; init; }
    }

    /// <summary>
    /// <b>Der Reihenkatalog des Klimadatenimports</b> (Auftrag KD-8, Konzept Klimadatenimport § 4a): je
    /// Größe Einheit, Gruppe, Monatsregel, Herkunft und Reihenfolge — einmal im Kern; die Oberfläche
    /// fragt nur ab. Teil A zeigt die Spalten von <c>Tab_Solar(_STAMM)</c>; Teil B zeigt die Rohwert-Größen
    /// (Luftdruck, Wind, Wasserdampf, Ausstrahlung, Direktstrahlung horizontal; Windrichtung und
    /// Qualitätskennung ohne Bild) mit <see cref="KlimaReihenherkunft.Rohwert"/>, sobald die Rohwerttabelle
    /// des Schritts 238 steht (<see cref="RohwerteFreigeschaltet"/>, aus dem Datenstand): in der Vorschau aus
    /// der gelesenen Rohreihe, im Jahresverlauf aus <c>Tab_Klimastunde_STAMM</c> (Leser
    /// <c>KlimaExportCtrl.RohwerteLesen</c>, <see cref="AusRohwerten"/>); eine Größe, die die Region nicht
    /// führt, bleibt weich gesperrt („führt nicht“).
    ///
    /// <para><b>Schlüssel und Spalten</b> stehen allein hier — <c>R_*</c> für Rechenspalten, <c>F_*</c> für
    /// gerechnete Spalten, <c>ROH_</c> + Spalte der Rohwerttabelle für Rohwerte —, der Export
    /// (<c>KlimaExportOptionen</c>, KD-5) nimmt sie als Aliasse.</para>
    /// </summary>
    public static class KlimaReihenkatalog
    {
        public const string R_TEMPERATUR = "R_TEMPERATUR";
        public const string R_LUFTFEUCHTE = "R_LUFTFEUCHTE";
        public const string R_GLOBAL = "R_GLOBAL";
        public const string R_DIREKT_NORMAL = "R_DIREKT_NORMAL";
        public const string R_DIFFUS = "R_DIFFUS";
        public const string R_GEGENSTRAHLUNG = "R_GEGENSTRAHLUNG";
        public const string R_BEDECKUNG = "R_BEDECKUNG";
        public const string F_NORD = "F_NORD";
        public const string F_OST = "F_OST";
        public const string F_SUED = "F_SUED";
        public const string F_WEST = "F_WEST";
        public const string F_SONNENWINKEL = "F_SONNENWINKEL";

        /// <summary>Präfix der Rohwert-Schlüssel (<c>ROH_</c> + Spalte der Rohwerttabelle; der Export nimmt ihn als Alias).</summary>
        public const string ROH_PREFIX = "ROH_";

        /// <summary>Die Einheit einer Monatssumme der Strahlung.</summary>
        public const string EINHEIT_SUMME = "kWh/m²";

        /// <summary>Höchstens so viele Einheiten zugleich im Monatsbild (Achsenregel, KD-Q10).</summary>
        public const int MONATSBILD_EINHEITEN_MAX = 2;

        /// <summary>Höchstens so viele Reihen je Bild im Jahresverlauf (KD-Q11).</summary>
        public const int JAHRESVERLAUF_REIHEN_MAX = 3;

        /// <summary>Die Vorgabe des Monatsbilds: Globalstrahlung und Temperatur.</summary>
        public static readonly IReadOnlyList<string> VORGABE_VORSCHAU = new[] { R_GLOBAL, R_TEMPERATUR };

        /// <summary>Die Vorgabe des Reiters „Weitere Reihen“: die Globalstrahlung.</summary>
        public static readonly IReadOnlyList<string> VORGABE_JAHRESVERLAUF = new[] { R_GLOBAL };

        /// <summary>
        /// Sind die Rohwerte freigeschaltet? Aus dem DATENSTAND (Teil B, Konzept § 4a.3, Anwenderentscheid 16.10.2026):
        /// ja, sobald die Rohwerttabelle und die Regionsspalten des Schritts 238 (<see cref="KlimaRohwerteSchema"/>) in
        /// der Datenbank stehen. Die Hülle fragt einmal je Ansicht (<see cref="AusRohwerten"/>), die Vorschau trägt
        /// dieselbe Auskunft als <c>KlimaVorschauErgebnis.RohwerteGespeichert</c>; vorher bekommen die Rohwerte keinen
        /// Chip (<see cref="Sichtbar"/>).
        /// </summary>
        public static bool RohwerteFreigeschaltet() => KlimaRohwerteSchema.SchemaVollstaendig();

        /// <summary>
        /// Zeigt diese Fassung die Größe? Rechen- und gerechnete Spalten immer, Rohwerte nur freigeschaltet
        /// (<paramref name="rohwerteFreigeschaltet"/>, aus dem Datenstand) — sonst ohne Chip (§ 4a.3).
        /// </summary>
        public static bool Sichtbar(KlimaReihe r, bool rohwerteFreigeschaltet)
            => r != null && (!r.IstRohwert || rohwerteFreigeschaltet);

        private static KlimaReihe Rechen(int nr, string schluessel, KlimaParameter p, Func<string> name, Func<string> kurz,
                                         string einheit, KlimaReihengruppe gruppe, KlimaMonatsregel regel, string spalte,
                                         Farbrolle rolle, bool imJahr = true)
            => new KlimaReihe
            {
                Reihenfolge = nr, Schluessel = schluessel, Parameter = p, Name = name, Kurzname = kurz, Einheit = einheit,
                Gruppe = gruppe, Monatsregel = regel, Herkunft = KlimaReihenherkunft.Rechenspalte, Spalte = spalte,
                Rolle = rolle, InVorschau = true, ImJahresverlauf = imJahr
            };

        private static KlimaReihe Fassade(int nr, string schluessel, Func<string> name, Func<string> kurz, string spalte,
                                          Farbrolle rolle)
            => new KlimaReihe
            {
                Reihenfolge = nr, Schluessel = schluessel, Name = name, Kurzname = kurz, Einheit = "W/m²",
                Gruppe = KlimaReihengruppe.Fassadenwerte, Monatsregel = KlimaMonatsregel.Summe,
                Herkunft = KlimaReihenherkunft.Gerechnet, Spalte = spalte, Rolle = rolle, ImJahresverlauf = true
            };

        private static KlimaReihe Roh(int nr, string spalte, KlimaParameter p, Func<string> name, Func<string> kurz,
                                      string einheit, KlimaReihengruppe gruppe, KlimaMonatsregel regel, Farbrolle rolle)
            => new KlimaReihe
            {
                Reihenfolge = nr, Schluessel = ROH_PREFIX + spalte, Parameter = p, Name = name, Kurzname = kurz,
                Einheit = einheit, Gruppe = gruppe, Monatsregel = regel, Herkunft = KlimaReihenherkunft.Rohwert,
                Spalte = spalte, Rolle = rolle, InVorschau = regel != KlimaMonatsregel.KeinBild,
                ImJahresverlauf = regel != KlimaMonatsregel.KeinBild
            };

        /// <summary>Alle Größen in ihrer Reihenfolge.</summary>
        public static readonly IReadOnlyList<KlimaReihe> Reihen = new List<KlimaReihe>
        {
            // ---- Rechenspalten von Tab_Solar(_STAMM) (Teil A) ----
            Rechen(10, R_TEMPERATUR, KlimaParameter.Lufttemperatur, () => MyResource.Resource.KLIMA_RW_NAME_TEMPERATUR,
                   () => MyResource.Resource.KLIMA_RW_KURZ_TEMPERATUR, "°C", KlimaReihengruppe.TemperaturFeuchte,
                   KlimaMonatsregel.Mittel, "Temperatur", Farbrolle.AUSSENTEMPERATUR, imJahr: false),
            Rechen(20, R_LUFTFEUCHTE, KlimaParameter.Luftfeuchte, () => MyResource.Resource.KLIMA_RW_NAME_FEUCHTE,
                   () => MyResource.Resource.KLIMA_RW_KURZ_FEUCHTE, "%", KlimaReihengruppe.TemperaturFeuchte,
                   KlimaMonatsregel.Mittel, "Luftfeuchte", Farbrolle.SERIE_3),
            Rechen(40, R_GLOBAL, KlimaParameter.Globalstrahlung, () => MyResource.Resource.KLIMA_RW_NAME_GLOBAL,
                   () => MyResource.Resource.KLIMA_RW_KURZ_GLOBAL, "W/m²", KlimaReihengruppe.Strahlung,
                   KlimaMonatsregel.Summe, "Globalstrahlung", Farbrolle.WAERME_SOLAR),
            Rechen(50, R_DIREKT_NORMAL, KlimaParameter.DirektstrahlungNormal, () => MyResource.Resource.KLIMA_RW_NAME_DIREKT_NORMAL,
                   () => MyResource.Resource.KLIMA_RW_KURZ_DIREKT_NORMAL, "W/m²", KlimaReihengruppe.Strahlung,
                   KlimaMonatsregel.Summe, "Direktstrahlung", Farbrolle.SERIE_1),
            Rechen(70, R_DIFFUS, KlimaParameter.Diffusstrahlung, () => MyResource.Resource.KLIMA_RW_NAME_DIFFUS,
                   () => MyResource.Resource.KLIMA_RW_KURZ_DIFFUS, "W/m²", KlimaReihengruppe.Strahlung,
                   KlimaMonatsregel.Summe, "Diffusstrahlung", Farbrolle.SERIE_2),
            Rechen(80, R_GEGENSTRAHLUNG, KlimaParameter.Gegenstrahlung, () => MyResource.Resource.KLIMA_RW_NAME_GEGENSTRAHLUNG,
                   () => MyResource.Resource.KLIMA_RW_KURZ_GEGENSTRAHLUNG, "W/m²", KlimaReihengruppe.Strahlung,
                   KlimaMonatsregel.Summe, "Gegenstrahlung", Farbrolle.SERIE_4),
            Rechen(130, R_BEDECKUNG, KlimaParameter.Bedeckungsgrad, () => MyResource.Resource.KLIMA_RW_NAME_BEDECKUNG,
                   () => MyResource.Resource.KLIMA_RW_KURZ_BEDECKUNG, "Achtel", KlimaReihengruppe.BewoelkungDruckWind,
                   KlimaMonatsregel.Mittel, "Bedeckungsgrad", Farbrolle.SERIE_5),

            // ---- Gerechnete Spalten (Fassadenwerte, Sonnenwinkel) ----
            Fassade(100, F_SUED, () => MyResource.Resource.KLIMA_RW_FASSADE_SUED, () => MyResource.Resource.KLIMA_RW_KURZ_SUED,
                    "Sol_Sued", Farbrolle.SERIE_6),
            Fassade(101, F_WEST, () => MyResource.Resource.KLIMA_RW_FASSADE_WEST, () => MyResource.Resource.KLIMA_RW_KURZ_WEST,
                    "Sol_West", Farbrolle.SERIE_7),
            Fassade(102, F_OST, () => MyResource.Resource.KLIMA_RW_FASSADE_OST, () => MyResource.Resource.KLIMA_RW_KURZ_OST,
                    "Sol_Ost", Farbrolle.SERIE_8),
            Fassade(103, F_NORD, () => MyResource.Resource.KLIMA_RW_FASSADE_NORD, () => MyResource.Resource.KLIMA_RW_KURZ_NORD,
                    "Sol_Nord", Farbrolle.SERIE_1),
            new KlimaReihe
            {
                // Eigener Reiter im Jahresverlauf, kein Chip.
                Reihenfolge = 200, Schluessel = F_SONNENWINKEL, Name = () => MyResource.Resource.KLIMA_REIHE_SONNENWINKEL,
                Kurzname = () => MyResource.Resource.KLIMA_REIHE_SONNENWINKEL, Einheit = "°",
                Gruppe = KlimaReihengruppe.Ohne, Monatsregel = KlimaMonatsregel.Mittel,
                Herkunft = KlimaReihenherkunft.Gerechnet, Spalte = "Sonnenwinkel", Rolle = Farbrolle.SONNENWINKEL
            },

            // ---- Rohwerte (Teil B, KD-4): die Spalten von Tab_Klimastunde_STAMM ----
            Roh(30, KlimaRohwerteSchema.SPALTE_WASSERDAMPF, KlimaParameter.Wasserdampfgehalt, () => MyResource.Resource.KLIMA_RW_NAME_WASSERDAMPF,
                () => MyResource.Resource.KLIMA_PAR_WASSERDAMPFGEHALT, "g/kg", KlimaReihengruppe.TemperaturFeuchte,
                KlimaMonatsregel.Mittel, Farbrolle.SERIE_3),
            Roh(60, KlimaRohwerteSchema.SPALTE_DIREKT_HORIZONTAL, KlimaParameter.DirektstrahlungHorizontal,
                () => MyResource.Resource.KLIMA_RW_NAME_DIREKT_HORIZONTAL,
                () => MyResource.Resource.KLIMA_PAR_DIREKTSTRAHLUNGHORIZONTAL, "W/m²", KlimaReihengruppe.Strahlung,
                KlimaMonatsregel.Summe, Farbrolle.SERIE_1),
            Roh(90, KlimaRohwerteSchema.SPALTE_AUSSTRAHLUNG, KlimaParameter.Ausstrahlung, () => MyResource.Resource.KLIMA_RW_NAME_AUSSTRAHLUNG,
                () => MyResource.Resource.KLIMA_PAR_AUSSTRAHLUNG, "W/m²", KlimaReihengruppe.Strahlung,
                KlimaMonatsregel.Summe, Farbrolle.SERIE_4),
            Roh(140, KlimaRohwerteSchema.SPALTE_LUFTDRUCK, KlimaParameter.Luftdruck, () => MyResource.Resource.KLIMA_RW_NAME_LUFTDRUCK,
                () => MyResource.Resource.KLIMA_PAR_LUFTDRUCK, "hPa", KlimaReihengruppe.BewoelkungDruckWind,
                KlimaMonatsregel.Mittel, Farbrolle.SERIE_6),
            Roh(150, KlimaRohwerteSchema.SPALTE_WINDGESCHWINDIGKEIT, KlimaParameter.Windgeschwindigkeit,
                () => MyResource.Resource.KLIMA_RW_NAME_WIND,
                () => MyResource.Resource.KLIMA_PAR_WINDGESCHWINDIGKEIT, "m/s", KlimaReihengruppe.BewoelkungDruckWind,
                KlimaMonatsregel.Mittel, Farbrolle.SERIE_7),
            Roh(160, KlimaRohwerteSchema.SPALTE_WINDRICHTUNG, KlimaParameter.Windrichtung, () => MyResource.Resource.KLIMA_RW_NAME_WINDRICHTUNG,
                () => MyResource.Resource.KLIMA_PAR_WINDRICHTUNG, "°", KlimaReihengruppe.Ohne,
                KlimaMonatsregel.KeinBild, Farbrolle.UNBENANNT),
            Roh(170, KlimaRohwerteSchema.SPALTE_QUALITAET, KlimaParameter.QualitaetStrahlung, () => MyResource.Resource.KLIMA_RW_NAME_QUALITAET,
                () => MyResource.Resource.KLIMA_PAR_QUALITAETSTRAHLUNG, "", KlimaReihengruppe.Ohne,
                KlimaMonatsregel.KeinBild, Farbrolle.UNBENANNT)
        }.OrderBy(r => r.Reihenfolge).ToList();

        /// <summary>Die Größe zu einem Schlüssel; <c>null</c> = unbekannt.</summary>
        public static KlimaReihe Reihe(string schluessel)
            => Reihen.FirstOrDefault(r => string.Equals(r.Schluessel, schluessel, StringComparison.Ordinal));

        /// <summary>Die Größe des Parameterbestands; <c>null</c> = keine.</summary>
        public static KlimaReihe Reihe(KlimaParameter p) => Reihen.FirstOrDefault(r => r.Parameter == p);

        /// <summary>Der Name einer Gruppe in der aktuellen Sprache.</summary>
        public static string Gruppenname(KlimaReihengruppe g) => g switch
        {
            KlimaReihengruppe.TemperaturFeuchte => MyResource.Resource.KLIMA_RW_GRUPPE_TEMPERATUR,
            KlimaReihengruppe.Strahlung => MyResource.Resource.KLIMA_RW_GRUPPE_STRAHLUNG,
            KlimaReihengruppe.Fassadenwerte => MyResource.Resource.KLIMA_RW_GRUPPE_FASSADE,
            KlimaReihengruppe.BewoelkungDruckWind => MyResource.Resource.KLIMA_RW_GRUPPE_BEWOELKUNG,
            _ => ""
        };

        // =====================================================================
        // Achsenregel des Monatsbilds (KD-Q10)
        // =====================================================================

        /// <summary>
        /// Die Einheiten einer Wahl im Monatsbild in Achsenfolge: Strahlung (kWh/m²) immer links,
        /// sonst die zuerst gewählte Einheit links.
        /// </summary>
        public static IReadOnlyList<string> Monatseinheiten(IEnumerable<string> wahl)
        {
            var einheiten = new List<string>();
            foreach (string k in wahl ?? Array.Empty<string>())
            {
                KlimaReihe r = Reihe(k);
                if (r == null || einheiten.Contains(r.Monatseinheit)) continue;
                einheiten.Add(r.Monatseinheit);
            }
            int i = einheiten.IndexOf(EINHEIT_SUMME);
            if (i > 0) { einheiten.RemoveAt(i); einheiten.Insert(0, EINHEIT_SUMME); }
            return einheiten;
        }

        /// <summary>
        /// Warum eine Reihe im Monatsbild nicht wählbar ist; leer = wählbar. Gewählt und letzte →
        /// „mindestens eine bleibt“; dritte Einheit → „höchstens zwei Einheiten“ (weich gesperrt, KD-Q10).
        /// </summary>
        public static string SperrgrundMonatsbild(string schluessel, IReadOnlyCollection<string> wahl)
        {
            KlimaReihe r = Reihe(schluessel);
            if (r == null) return "";
            wahl ??= Array.Empty<string>();
            if (wahl.Contains(schluessel))
                return wahl.Count <= 1 ? MyResource.Resource.KLIMA_RW_LETZTE_BLEIBT : "";
            IReadOnlyList<string> einheiten = Monatseinheiten(wahl);
            if (einheiten.Count >= MONATSBILD_EINHEITEN_MAX && !einheiten.Contains(r.Monatseinheit))
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KLIMA_RW_ZWEI_EINHEITEN,
                                     string.Join(", ", einheiten));
            return "";
        }

        // =====================================================================
        // Regel des Jahresverlaufs (KD-Q11)
        // =====================================================================

        /// <summary>
        /// Der Klick auf eine Reihe im Jahresverlauf: gleiche Einheit fügt hinzu (bis drei), andere
        /// Einheit zeigt sie allein, die letzte bleibt. Gibt die neue Wahl zurück; <paramref name="grund"/>
        /// nennt, warum sich nichts geändert hat.
        /// </summary>
        public static IReadOnlyList<string> JahresverlaufKlick(IReadOnlyList<string> wahl, string schluessel, out string grund)
        {
            grund = "";
            var neu = new List<string>(wahl ?? Array.Empty<string>());
            KlimaReihe r = Reihe(schluessel);
            if (r == null) return neu;
            if (neu.Contains(schluessel))
            {
                if (neu.Count <= 1) { grund = MyResource.Resource.KLIMA_RW_LETZTE_BLEIBT; return neu; }
                neu.Remove(schluessel);
                return neu;
            }
            KlimaReihe erste = neu.Count > 0 ? Reihe(neu[0]) : null;
            if (erste == null || erste.Einheit != r.Einheit) return new List<string> { schluessel };
            if (neu.Count >= JAHRESVERLAUF_REIHEN_MAX) { grund = MyResource.Resource.KLIMA_RW_DREI_REIHEN; return neu; }
            neu.Add(schluessel);
            return neu;
        }

        // =====================================================================
        // Reihenbestand einer Region (Rechenspalten, Teil A; Rohwerte, Teil B)
        // =====================================================================

        /// <summary>
        /// Die Rechenspalten einer Region für den Reiter „Weitere Reihen“: je Größe mit
        /// <see cref="KlimaReihe.ImJahresverlauf"/> die Stundenwerte aus den gelesenen Zeilen
        /// (<c>SolardatenCtrl</c>). <b>Führt nicht</b> heißt: alle Stunden NULL (Feuchte,
        /// Gegenstrahlung, Bedeckungsgrad — so speichert der Import, was die Quelle nicht liefert) oder
        /// alle Stunden 0 (eine nicht nullbare Spalte ohne Wert).
        /// </summary>
        public static IReadOnlyList<KlimaReihenwerte> AusRechenspalten(IReadOnlyList<SolardatenModel> zeilen)
        {
            var liste = new List<KlimaReihenwerte>();
            if (zeilen == null || zeilen.Count == 0) return liste;
            foreach (KlimaReihe r in Reihen)
            {
                if (!r.ImJahresverlauf || r.Herkunft == KlimaReihenherkunft.Rohwert) continue;
                var werte = new double[zeilen.Count];
                for (int i = 0; i < zeilen.Count; i++) werte[i] = Spaltenwert(zeilen[i], r.Schluessel) ?? double.NaN;
                liste.Add(new KlimaReihenwerte { Reihe = r, Werte = werte, Gefuehrt = Gefuehrt(werte) });
            }
            return liste;
        }

        /// <summary>Führt eine Reihe Werte? Nein, wenn jede Stunde NULL (NaN) oder jede 0 ist.</summary>
        public static bool Gefuehrt(IReadOnlyList<double> werte)
        {
            if (werte == null) return false;
            foreach (double w in werte)
                if (!double.IsNaN(w) && w != 0.0) return true;
            return false;
        }

        /// <summary>
        /// Die Rohwert-Reihen einer Katalogregion für den Reiter „Weitere Reihen“ (Teil B): je Rohwert-Größe mit Bild
        /// die Stundenwerte aus <see cref="KlimaExportDaten.Rohwerte"/> (Leser <c>KlimaExportCtrl.RohwerteLesen</c>,
        /// Stunde 1 … 8760 im Raster der Quelle). <b>Führt nicht</b> heißt: die Region trägt keine Rohwerte
        /// (<c>Rohdaten = 0</c>, Altbestand nach KD-Q7) oder die Spalte ist in jeder Stunde NULL (etwa Wasserdampf und
        /// Ausstrahlung bei PVGIS) — der Chip bleibt dann weich gesperrt. <c>null</c> = keine Reihen.
        /// </summary>
        public static IReadOnlyList<KlimaReihenwerte> AusRohwerten(KlimaExportDaten roh)
        {
            var liste = new List<KlimaReihenwerte>();
            if (roh == null) return liste;
            foreach (KlimaReihe r in Reihen)
            {
                if (!r.ImJahresverlauf || !r.IstRohwert) continue;
                double[] werte;
                if (roh.Rohdaten && roh.Rohwerte.TryGetValue(r.Spalte, out double[] w) && w != null && w.Length > 0)
                    werte = (double[])w.Clone();
                else
                {
                    werte = new double[KlimaRohwerteSchema.STUNDE_MAX];
                    for (int i = 0; i < werte.Length; i++) werte[i] = double.NaN;
                }
                liste.Add(new KlimaReihenwerte { Reihe = r, Werte = werte, Gefuehrt = roh.Rohdaten && Gefuehrt(werte) });
            }
            return liste;
        }

        /// <summary>Die erste Stunde (0-basiert) jedes Monats im Gemeinjahr, dazu 8760 als Ende.</summary>
        private static readonly int[] MONATSBEGINN =
            { 0, 744, 1416, 2160, 2880, 3624, 4344, 5088, 5832, 6552, 7296, 8016, 8760 };

        /// <summary>Der Monat (0 … 11) einer Stunde 1 … 8760 im Gemeinjahr (kein Schaltjahr); −1 außerhalb.</summary>
        public static int MonatVonStunde(int stunde)
        {
            if (stunde < KlimaRohwerteSchema.STUNDE_MIN || stunde > KlimaRohwerteSchema.STUNDE_MAX) return -1;
            int h = stunde - 1;
            for (int m = 0; m < 12; m++)
                if (h < MONATSBEGINN[m + 1]) return m;
            return -1;
        }

        /// <summary>Der Rohwert einer gelesenen Stunde (Vorschau, Teil B); <c>null</c> = die Quelle führt ihn nicht.</summary>
        internal static double? Rohstundenwert(KlimaRohstunde s, string schluessel) => schluessel switch
        {
            ROH_PREFIX + KlimaRohwerteSchema.SPALTE_WASSERDAMPF => s.Wasserdampfgehalt_g_kg,
            ROH_PREFIX + KlimaRohwerteSchema.SPALTE_DIREKT_HORIZONTAL => s.DirektHorizontal,
            ROH_PREFIX + KlimaRohwerteSchema.SPALTE_AUSSTRAHLUNG => s.Ausstrahlung,
            ROH_PREFIX + KlimaRohwerteSchema.SPALTE_LUFTDRUCK => s.Luftdruck_hPa,
            ROH_PREFIX + KlimaRohwerteSchema.SPALTE_WINDGESCHWINDIGKEIT => s.Windgeschwindigkeit_m_s,
            ROH_PREFIX + KlimaRohwerteSchema.SPALTE_WINDRICHTUNG => s.Windrichtung_Grad,
            _ => null
        };

        private static double? Spaltenwert(SolardatenModel z, string schluessel) => schluessel switch
        {
            R_TEMPERATUR => z.Außen_Temp,
            R_LUFTFEUCHTE => z.Luftfeuchte,
            R_GLOBAL => z.Globalstrahlung,
            R_DIREKT_NORMAL => z.Direktstrahlung,
            R_DIFFUS => z.Diffusstrahlung,
            R_GEGENSTRAHLUNG => z.Gegenstrahlung,
            R_BEDECKUNG => z.Bedeckungsgrad,
            F_SUED => z.Sol_Sued,
            F_WEST => z.Sol_West,
            F_OST => z.Sol_Ost,
            F_NORD => z.Sol_Nord,
            F_SONNENWINKEL => z.Sonnenwinkel,
            _ => null
        };

        /// <summary>Der Stundenwert eines gelesenen Satzes (Vorschau); <c>null</c> = kein Wert.</summary>
        internal static double? Stundenwert(TmyHourlyData s, string schluessel) => schluessel switch
        {
            R_TEMPERATUR => s.Temperature,
            R_LUFTFEUCHTE => s.Humidity,
            R_GLOBAL => s.GlobalIrradiance,
            R_DIREKT_NORMAL => s.DirectIrradiance,
            R_DIFFUS => s.DiffuseIrradiance,
            R_GEGENSTRAHLUNG => s.Gegenstrahlung,
            R_BEDECKUNG => s.Bedeckungsgrad,
            _ => null
        };
    }
}
