using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Welchen Weg die erweiterte Rückfrage vor dem Start nimmt (Konzept Berichtsvorlagen 10.2,
    /// BV-Q6). Der dritte Weg, „Abbrechen“, startet keinen Lauf und braucht deshalb keinen Wert.
    /// </summary>
    public enum Startweg
    {
        /// <summary>„Mit meiner Vorlage“ — die gewählte Vorlage; unbekannte Stellen bleiben gelb stehen.</summary>
        Gewaehlt,

        /// <summary>„Mit Standardvorlage“ — für diesen einen Lauf die mitgelieferte Standardvorlage.</summary>
        Standard,
    }

    /// <summary>
    /// <b>Eine Meldung des Berichtslaufs</b> — ein Abschnitt der Laufmeldung oder ein Befund der
    /// Rückfrage vor dem Start: Kopfzeile, Aufzählung und die Kennung, unter der der Assistent sie
    /// erklärt (<see cref="KiMeldungskennung"/>, „erklären lassen“).
    /// </summary>
    public sealed class Berichtsmeldung
    {
        internal Berichtsmeldung(string kennung, string text, IReadOnlyList<string> punkte = null)
        {
            Kennung = kennung ?? "";
            Text = text ?? "";
            Punkte = punkte ?? Array.Empty<string>();
        }

        /// <summary>Die Meldungskennung, etwa <c>BV_LAUF_UNBEKANNT</c> oder <c>VF_PRUEF_UNBEKANNT</c>.</summary>
        public string Kennung { get; }

        /// <summary>Die Kopfzeile, etwa „Nicht ersetzte Platzhalter, im Bericht gelb markiert: 8“.</summary>
        public string Text { get; }

        /// <summary>Die Aufzählung darunter (höchstens <see cref="BerichtCtrl.MAX_PUNKTE"/> und „… und n weitere“).</summary>
        public IReadOnlyList<string> Punkte { get; }

        /// <inheritdoc/>
        public override string ToString() { return Kennung + ": " + Text; }
    }

    /// <summary>
    /// <b>Das Ergebnis eines Word-Laufs</b> (Konzept Berichtsvorlagen 4.10, 10.2 Schritt 5; Etappe BV-E1):
    /// welche Datei entstand, aus welcher Vorlage und warum, welche Rückfälle griffen, welche
    /// Platzhalter stehen oder leer blieben, wie viele Kommentare entfernt wurden und was die Engine
    /// warnte. Die Laufmeldung für die Hülle bildet daraus <see cref="BerichtCtrl.Laufmeldung"/>.
    ///
    /// <para><b>Zwei Wege.</b> Mit einer Vorlage füllt die Word-Engine
    /// (<see cref="WordVorlagenfueller"/>) und <see cref="Fuellergebnis"/> trägt den Befund. Fehlt
    /// die Standardvorlage selbst, entsteht der Bericht auf dem bisherigen Weg aus der Stilvorlage
    /// oder mit den eingebauten Formaten (<see cref="IstRueckfall"/>); dann gibt es kein
    /// Füllergebnis, und <see cref="Rueckfall"/> nennt den Grund.</para>
    /// </summary>
    public sealed class Berichtslauf
    {
        private static readonly IReadOnlyDictionary<string, int> Leer = new Dictionary<string, int>();

        internal Berichtslauf(string pfad, Vorlagenwahl wahl, string vorlageName, bool istRueckfall,
                              IEnumerable<string> rueckfaelle, Fuellergebnis ergebnis, string pruefsumme, bool englisch)
        {
            Pfad = pfad ?? "";
            Wahl = wahl;
            VorlageId = wahl?.Eintrag?.Id ?? BerichtsvorlagenCtrl.ID_STANDARD;
            VorlageName = vorlageName ?? wahl?.Eintrag?.Name ?? "";
            Grund = wahl?.Grund ?? Vorlagenwahlgrund.Rueckfall;
            GrundText = wahl?.GrundText ?? "";
            IstRueckfall = istRueckfall;
            Rueckfaelle = (rueckfaelle ?? Enumerable.Empty<string>()).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
            Fuellergebnis = ergebnis;
            Pruefsumme = pruefsumme ?? "";
            Englisch = ergebnis?.Englisch ?? englisch;

            Unbekannte = ergebnis?.Unbekannte ?? Array.Empty<Fuellbefund>();
            Leere = ergebnis?.Leere ?? Leer;
            Stellen = ergebnis?.Stellen ?? Leer;
            EntfernteKommentare = ergebnis?.EntfernteKommentare ?? 0;
            var warnungen = new List<string>();
            if (ergebnis != null)
            {
                warnungen.AddRange(ergebnis.Fehler);
                warnungen.AddRange(ergebnis.Warnungen);
            }
            Warnungen = warnungen;
            Hinweise = ergebnis?.Hinweise ?? Array.Empty<string>();
            LeereZusammengefasst = FasseLeereZusammen(Leere, Stellen, Englisch);
        }

        /// <summary>Die geschriebene Berichtsdatei.</summary>
        public string Pfad { get; }

        /// <summary>
        /// Die Vorlage, die gefüllt wurde, mit dem Grund ihrer Wahl — nach allen Rückfällen. Beim
        /// bisherigen Weg der Eintrag der (fehlenden) Standardvorlage mit <see cref="Vorlagenwahlgrund.Rueckfall"/>.
        /// </summary>
        public Vorlagenwahl Wahl { get; }

        /// <summary>Die Kennung der Vorlage (<c>standard</c> oder <c>eigen:</c> + Dateiname).</summary>
        public string VorlageId { get; }

        /// <summary>
        /// Der Name der Vorlage, aus der der Bericht entstand: „Standard (EPOS-Plan)“, der Name einer
        /// eigenen Vorlage — beim bisherigen Weg der Dateiname der Stilvorlage oder „Eingebaute Formate“.
        /// </summary>
        public string VorlageName { get; }

        /// <summary>Abweichung, Vorgabe, Standard oder Rückfall.</summary>
        public Vorlagenwahlgrund Grund { get; }

        /// <summary>Der Grund in Worten (Sprache der Oberfläche).</summary>
        public string GrundText { get; }

        /// <summary>Entstand der Bericht auf dem bisherigen Weg (Stilvorlage oder eingebaute Formate)?</summary>
        public bool IstRueckfall { get; }

        /// <summary>
        /// Jeder Rückfall des Laufs als Satz: eine gespeicherte, aber fehlende Vorlage, eine Vorlage, die
        /// sich nicht lesen oder füllen ließ, der Ersatz für diesen Lauf, die fehlende Standardvorlage.
        /// </summary>
        public IReadOnlyList<string> Rueckfaelle { get; }

        /// <summary>Alle Rückfälle in einem Text; leer ohne.</summary>
        public string Rueckfall { get { return string.Join(" ", Rueckfaelle); } }

        /// <summary>Die Platzhalter, die gelb stehen blieben — je Fundstelle einer.</summary>
        public IReadOnlyList<Fuellbefund> Unbekannte { get; }

        /// <summary>Je Schlüssel, wie oft er ohne Wert blieb.</summary>
        public IReadOnlyDictionary<string, int> Leere { get; }

        /// <summary>Je Schlüssel, an wie vielen Stellen er aufgelöst wurde.</summary>
        public IReadOnlyDictionary<string, int> Stellen { get; }

        /// <summary>
        /// Die leeren Platzhalter je Schlüssel zusammengefasst, in der Sprache des Laufs: „{{…}}: leer“
        /// bzw. „{{…}}: leer bei 1 von 3 Stellen“ (Konzept 4.10).
        /// </summary>
        public IReadOnlyList<string> LeereZusammengefasst { get; }

        /// <summary>Wie viele Kommentare die Vorlage trug (entfernt, gezählt wie im Prüfer).</summary>
        public int EntfernteKommentare { get; }

        /// <summary>Fehler der Vorlage beim Füllen (Platzhalter blieb stehen) und Warnungen der Engine.</summary>
        public IReadOnlyList<string> Warnungen { get; }

        /// <summary>Hinweise der Engine: angelegte Stile, entfernte Kommentare, Dokumentvorlage, Datumsfelder.</summary>
        public IReadOnlyList<string> Hinweise { get; }

        /// <summary>Der ganze Befund der Engine; <c>null</c> beim bisherigen Weg.</summary>
        public Fuellergebnis Fuellergebnis { get; }

        /// <summary>
        /// Die Prüfsumme der gefüllten Vorlagenbytes (SHA-256, wie <see cref="Pruefbefund.Pruefsumme"/>);
        /// leer beim bisherigen Weg. Gleich der Prüfsumme der Vorprüfung heißt: geprüft = gefüllt (6.8).
        /// </summary>
        public string Pruefsumme { get; }

        /// <summary>In welcher Sprache der Bericht entstand.</summary>
        public bool Englisch { get; }

        /// <summary>Die leeren Platzhalter zusammengefasst, in der gewählten Sprache.</summary>
        internal static IReadOnlyList<string> FasseLeereZusammen(IReadOnlyDictionary<string, int> leere,
                                                                   IReadOnlyDictionary<string, int> stellen, bool englisch)
        {
            var zeilen = new List<string>();
            if (leere == null) return zeilen;
            foreach (KeyValuePair<string, int> p in leere.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                int von = 0;
                if (stellen != null) stellen.TryGetValue(p.Key, out von);
                von = Math.Max(von, p.Value);
                string marke = "{{" + p.Key + "}}";
                zeilen.Add(von <= 1
                    ? Berichtslauftexte.T(englisch, nameof(R.BV_LAUF_PUNKT_LEER_EINE), marke)
                    : Berichtslauftexte.T(englisch, nameof(R.BV_LAUF_PUNKT_LEER), marke, p.Value, von));
            }
            return zeilen;
        }
    }

    /// <summary>
    /// <b>Der Befund vor dem Start</b> (Konzept Berichtsvorlagen 6.8, 10.2 Schritte 1 und 2): welche
    /// Vorlage der Lauf nimmt, was die Schnellprüfung an GENAU den gelesenen Bytes fand und ob statt der
    /// heutigen Startrückfrage die erweiterte Rückfrage kommt — mit ihren Texten und den drei Wegen.
    ///
    /// <para><b>Dieselben Bytes.</b> Der Befund behält die gelesenen Bytes; der Lauf füllt sie
    /// (<see cref="BerichtCtrl.ErzeugeWord(BerichtsDaten, BerichtsKonfiguration, Startbefund, Startweg)"/>),
    /// auch wenn der Anwender die Datei während der Simulation in Word speichert.</para>
    /// </summary>
    public sealed class Startbefund
    {
        internal Startbefund(Vorlagenwahl wahl, Pruefbefund pruefbefund, byte[] bytes, string lesefehler,
                             bool englisch, int anzahlProjekte, bool spracheAbweichend, bool sichtUnpassend,
                             bool ohneWirtschaftlichkeit, IReadOnlyList<Berichtsmeldung> befunde, string rueckfrage,
                             string wegGewaehlt, string wegStandard, string wegAbbrechen,
                             Berichtsbedarf bedarf = null)
        {
            Wahl = wahl;
            Bedarf = bedarf;
            Pruefbefund = pruefbefund;
            Bytes = bytes;
            Lesefehler = lesefehler;
            Englisch = englisch;
            AnzahlProjekte = anzahlProjekte;
            SpracheAbweichend = spracheAbweichend;
            SichtUnpassend = sichtUnpassend;
            OhneWirtschaftlichkeit = ohneWirtschaftlichkeit;
            Befunde = befunde ?? Array.Empty<Berichtsmeldung>();
            Rueckfrage = rueckfrage ?? "";
            WegGewaehlt = wegGewaehlt ?? "";
            WegStandard = wegStandard ?? "";
            WegAbbrechen = wegAbbrechen ?? "";
        }

        /// <summary>Die Vorlage des Laufs mit dem Grund ihrer Wahl und den Meldungen über Fehlendes.</summary>
        public Vorlagenwahl Wahl { get; }

        /// <summary>
        /// Die Schnellprüfung der gelesenen Bytes (mit „in Word geöffnet“); <c>null</c>, wenn die
        /// Standardvorlage fehlt und der Bericht auf dem bisherigen Weg entsteht — dort gibt es nichts
        /// zu prüfen.
        /// </summary>
        public Pruefbefund Pruefbefund { get; }

        /// <summary>Die einmal gelesenen Bytes der Vorlage; <c>null</c> beim Rückfall oder wenn sie nicht lesbar war.</summary>
        internal byte[] Bytes { get; }

        /// <summary>Warum die Vorlage nicht gelesen werden konnte; <c>null</c> ohne Lesefehler.</summary>
        public string Lesefehler { get; }

        /// <summary>Ließ sich die gewählte Vorlage lesen — gibt es den Weg „Mit meiner Vorlage“?</summary>
        public bool KannGewaehlteFuellen { get { return Bytes != null; } }

        /// <summary>Die Prüfsumme der gelesenen Bytes; leer ohne.</summary>
        public string Pruefsumme { get { return Pruefbefund?.Pruefsumme ?? ""; } }

        /// <summary>In welcher Sprache der Bericht entstehen soll (und die Texte stehen).</summary>
        public bool Englisch { get; }

        /// <summary>Wie viele Projekte der Lauf simuliert — Stammprojekt und gewählte Varianten.</summary>
        public int AnzahlProjekte { get; }

        /// <summary>Weicht die Sprache der Vorlage von der des Berichts ab?</summary>
        public bool SpracheAbweichend { get; }

        /// <summary>Nutzt die Vorlage den Paarvergleich (<c>stand.a</c>, <c>stand.b</c>), obwohl Sicht 1 gewählt ist?</summary>
        public bool SichtUnpassend { get; }

        /// <summary>
        /// Zweiter Einstieg (Wirtschaftlichkeitsseite): Die gewählte Vorlage führt keinen Schlüssel der
        /// Wirtschaftlichkeit (<see cref="Pruefbefund.HatWirtschaftlichkeit"/>) — die Rückfrage bietet
        /// für diesen Lauf die Standardvorlage an und nennt die gewählte.
        /// </summary>
        public bool OhneWirtschaftlichkeit { get; }

        /// <summary>Hat die Schnellprüfung Fehler gefunden?</summary>
        public bool HatFehler { get { return Pruefbefund?.HatFehler == true; } }

        /// <summary>
        /// Kommt statt der heutigen Startrückfrage die EINE erweiterte Rückfrage (Fehler, abweichende
        /// Sprache, unpassende Sicht, im zweiten Einstieg eine Vorlage ohne Wirtschaftlichkeit)?
        /// </summary>
        public bool BrauchtRueckfrage { get { return HatFehler || SpracheAbweichend || SichtUnpassend || OhneWirtschaftlichkeit; } }

        /// <summary>Bietet die Rückfrage „Mit Standardvorlage“ an — die gewählte ist nicht selbst die Standardvorlage?</summary>
        public bool StandardAngeboten { get { return BrauchtRueckfrage && Wahl != null && !Wahl.Eintrag.IstStandard; } }

        /// <summary>Die Befunde der Rückfrage, je mit Kennung für „erklären lassen“; leer ohne Rückfrage.</summary>
        public IReadOnlyList<Berichtsmeldung> Befunde { get; }

        /// <summary>Der Text der erweiterten Rückfrage: Zahl der Projekte, Name der Vorlage, Befunde, Frage; leer ohne.</summary>
        public string Rueckfrage { get; }

        /// <summary>Beschriftung des Wegs „Mit meiner Vorlage“.</summary>
        public string WegGewaehlt { get; }

        /// <summary>Beschriftung des Wegs „Mit Standardvorlage“.</summary>
        public string WegStandard { get; }

        /// <summary>Beschriftung des Wegs „Abbrechen“.</summary>
        public string WegAbbrechen { get; }

        /// <summary>
        /// <b>Der Bedarf der Vorlage</b> (Konzept 5.1, Etappe BV-E3): was der Sammler für GENAU diese Vorlage
        /// über den Regellauf hinaus erheben muss — Stundenreihen, Verlauf, Emissionsbilanz —, abgeleitet aus
        /// ihren Platzhaltern mit den Häkchen der Vorprüfung (<see cref="Berichtsbedarf.AusVorlage"/>); ist
        /// sie nicht lesbar oder fehlt die Standardvorlage, die <see cref="Berichtsbedarf.Vorgabe"/>. Den
        /// Bedarf des LAUFS bildet <see cref="Berichtsbedarf.FuerLauf"/> daraus mit den Häkchen des Auftrags,
        /// dem Weg der Rückfrage und der Mappe.
        /// </summary>
        public Berichtsbedarf Bedarf { get; }
    }

    /// <summary>
    /// <b>Der Befund der Excel-Vorlage vor dem Start</b> (Konzept Berichtsvorlagen 6.8, 7.4, 10.2; Anwenderentscheid
    /// BV-E7-3): welche Excel-Vorlage die Mappe nimmt und was die Schnellprüfung an GENAU den gelesenen Bytes fand.
    /// Fehler der gewählten Excel-Vorlage stehen in derselben erweiterten Rückfrage wie die der Word-Vorlage; ihr Weg
    /// „ohne Vorlage“ entspricht dort dem Weg „Mit Standardvorlage“. Ohne Excel-Vorlage gibt es nichts zu prüfen.
    /// </summary>
    public sealed class Excelstartbefund
    {
        internal Excelstartbefund(Vorlagenwahl wahl, Pruefbefund pruefbefund, byte[] bytes, bool englisch,
                                  IReadOnlyList<Berichtsmeldung> befunde)
        {
            Wahl = wahl;
            Pruefbefund = pruefbefund;
            Bytes = bytes;
            Englisch = englisch;
            Befunde = befunde ?? Array.Empty<Berichtsmeldung>();
        }

        /// <summary>Die Excel-Vorlage des Laufs mit dem Grund ihrer Wahl; „ohne Vorlage“, wenn keine gewählt ist.</summary>
        public Vorlagenwahl Wahl { get; }

        /// <summary>Die Schnellprüfung der gelesenen Bytes; <c>null</c> ohne Excel-Vorlage.</summary>
        public Pruefbefund Pruefbefund { get; }

        /// <summary>Die einmal gelesenen Bytes der Vorlage; <c>null</c> ohne Vorlage oder wenn sie nicht lesbar war.</summary>
        internal byte[] Bytes { get; }

        /// <summary>In welcher Sprache die Befunde stehen.</summary>
        public bool Englisch { get; }

        /// <summary>Ließ sich die gewählte Excel-Vorlage lesen — gibt es den Weg „Mit meiner Vorlage“?</summary>
        public bool KannGewaehlteFuellen { get { return Bytes != null; } }

        /// <summary>Hat die Schnellprüfung Fehler gefunden (auch: nicht lesbar)?</summary>
        public bool HatFehler { get { return Pruefbefund?.HatFehler == true; } }

        /// <summary>Gehört die Excel-Vorlage in die erweiterte Rückfrage? Genau dann, wenn sie Fehler hat.</summary>
        public bool BrauchtRueckfrage { get { return HatFehler; } }

        /// <summary>Die Fehler als Befunde der Rückfrage, je mit Kennung für „erklären lassen“; leer ohne.</summary>
        public IReadOnlyList<Berichtsmeldung> Befunde { get; }
    }

    /// <summary>Die Texte des Berichtslaufs aus <c>MyResource</c> in einer ausdrücklich gewählten Sprache.</summary>
    internal static class Berichtslauftexte
    {
        /// <summary>Ein Text in der gewählten Sprache, mit Argumenten formatiert; ein fehlender Schlüssel steht für sich.</summary>
        internal static string T(bool englisch, string schluessel, params object[] argumente)
        {
            CultureInfo kultur = BerichtTexte.KulturFuer(englisch);
            string muster = null;
            try { muster = R.ResourceManager.GetString(schluessel, kultur); }
            catch (Exception) { muster = null; }
            muster ??= schluessel;
            if (argumente == null || argumente.Length == 0) return muster;
            try { return string.Format(kultur, muster, argumente); }
            catch (FormatException) { return muster; }
        }
    }
}
