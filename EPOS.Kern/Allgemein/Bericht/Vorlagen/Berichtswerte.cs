using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Angaben zur Installation, die den Bericht erstellt — die Quelle von
    /// <c>ersteller.*</c> (Konzept Berichtsvorlagen 5.2, BV-Q8).
    ///
    /// <para><b>Die Firma kommt von außen:</b> Die Hülle liest die Einstellung
    /// <c>BerichtFirma</c> mit Rückfall auf <c>LizenzToken.Firma</c> — beides kennt der Kern
    /// nicht. <b>Programm und Fassung setzt der Kern selbst</b>, wenn sie fehlen
    /// (<see cref="PROGRAMM"/>, die Produktfassung des Deckblatts); eine Hülle, die etwas
    /// anderes zeigen will, setzt sie ausdrücklich.</para>
    /// </summary>
    public sealed class Erstellerangaben
    {
        /// <summary>Der Programmname, wenn keiner übergeben wird.</summary>
        public const string PROGRAMM = "EPOS-Plan";

        /// <summary>Die erstellende Firma; leer bei einer Lizenz ohne Firma und ohne Einstellung.</summary>
        public string Firma { get; set; }

        /// <summary>Der Programmname; <c>null</c> = <see cref="PROGRAMM"/>.</summary>
        public string Programm { get; set; }

        /// <summary>Die Programmfassung; <c>null</c> = die Produktfassung wie auf dem Deckblatt.</summary>
        public string Version { get; set; }

        /// <summary>
        /// Das Firmenlogo für <c>bild.ersteller.logo</c> (PNG oder JPEG, Anwenderentscheid BV-E2-1): die
        /// Bytes der Datei aus der Einstellung <c>BerichtLogo</c>, die <c>BerichtsvorlagenCtrl.Ersteller</c>
        /// einmal lädt; <c>null</c> = kein Logo, dann entfällt das Platzhalterbild.
        /// </summary>
        public byte[] Logo { get; set; }

        /// <summary>Der Dateiname des Logos ohne Pfad (Alternativtext des gefüllten Bildes); <c>null</c> ohne.</summary>
        public string LogoDateiname { get; set; }

        /// <summary>
        /// Warum das eingestellte Logo fehlt — „Logo nicht gefunden: &lt;pfad&gt;“, zu groß, kein PNG oder
        /// JPEG —, in der Sprache der Oberfläche; <c>null</c> = kein Befund. Die Engine nennt ihn als
        /// Warnung des Laufs, sobald die Vorlage das Logo führt.
        /// </summary>
        public string LogoWarnung { get; set; }
    }

    /// <summary>
    /// <b>Der Wertesatz eines Berichtslaufs</b> (Konzept Berichtsvorlagen 5.1) — gebildet nach
    /// <c>BerichtsDatenSammler.SammleFuerBericht</c> aus dem Berichtsbaum, der Auswahl, der
    /// Sprache und den Erstellerangaben. Die Quellen des <see cref="Vorlagenfeldkatalog"/>
    /// lesen nur hieraus: <b>Beim Auflösen wird die Datenbank nicht berührt.</b>
    ///
    /// <para><b>Dieselben Quellen wie die Kapitel.</b> Kennzahlwerte kommen aus
    /// <see cref="VariantenDaten.Kennzahlen"/>, dem Verzeichnis, das der Sammler über
    /// <see cref="KennzahlenKatalog.Berechne"/> füllt und aus dem auch die Bausteine lesen;
    /// ein Baum, der nicht durch den Sammler ging, zeigt deshalb hier wie dort „—“. Die
    /// Beschriftungen der Emissionskennzahlen folgen dem Modus des Laufs
    /// (<see cref="EmissionsAusweis.ModusAusVarianten"/>).</para>
    ///
    /// <para><b>Sprache und Kultur</b> werden übergeben, nicht aus der Oberfläche gelesen:
    /// <c>de-DE</c> oder <c>en-US</c> wie <see cref="BerichtTexte.Kultur"/>, Texte aus
    /// <c>MyResource</c> in eben dieser Kultur.</para>
    /// </summary>
    public sealed class Berichtswerte
    {
        private readonly Dictionary<string, Kennzahl> _kennzahlen;

        private Berichtswerte(BerichtsDaten daten, BerichtsKonfiguration konfig, bool englisch,
                              Erstellerangaben ersteller)
        {
            Daten = daten;
            Konfiguration = konfig;
            Englisch = englisch;
            Kultur = BerichtTexte.KulturFuer(englisch);
            Ersteller = ersteller;

            List<VariantenDaten> alle = daten.Varianten ?? new List<VariantenDaten>();
            Stamm = alle.FirstOrDefault(v => v != null && v.IstStamm);
            Varianten = alle.Where(v => v != null && !v.IstStamm).ToList();

            Emissionsmodus = EmissionsAusweis.ModusAusVarianten(alle);
            Kennzahlen = KennzahlenKatalog.Alle(Emissionsmodus);
            _kennzahlen = new Dictionary<string, Kennzahl>(StringComparer.Ordinal);
            foreach (Kennzahl k in Kennzahlen) _kennzahlen[k.Schluessel] = k;

            AktiveKapitel = Berichtskapitel.Alle
                .Where(k => k.IstAktiv(konfig))
                .Select(k => k.Name)
                .ToList();

            Produktausweis = DeckblattBaustein.ProduktausweisNoetig(daten);
            Anlagenkopplung = DeckblattBaustein.KopplungImBericht(daten);
        }

        /// <summary>
        /// Bildet den Wertesatz. <paramref name="konfig"/> <c>null</c> heißt alle Bausteine (wie
        /// <see cref="WordBerichtGenerator.AktiveBausteine"/>); <paramref name="ersteller"/>
        /// <c>null</c> heißt ohne Firma. Programm und Fassung ergänzt der Kern, wenn sie fehlen;
        /// das übergebene Objekt bleibt unverändert.
        /// </summary>
        public static Berichtswerte Aus(BerichtsDaten daten, BerichtsKonfiguration konfig, bool englisch,
                                        Erstellerangaben ersteller)
        {
            if (daten == null) throw new ArgumentNullException(nameof(daten));

            var vollstaendig = new Erstellerangaben
            {
                Firma = ersteller?.Firma,
                Programm = string.IsNullOrWhiteSpace(ersteller?.Programm) ? Erstellerangaben.PROGRAMM : ersteller.Programm,
                Version = string.IsNullOrWhiteSpace(ersteller?.Version) ? Produktfassung() : ersteller.Version,
                Logo = ersteller?.Logo,
                LogoDateiname = ersteller?.LogoDateiname,
                LogoWarnung = ersteller?.LogoWarnung,
            };
            return new Berichtswerte(daten, konfig, englisch, vollstaendig);
        }

        /// <summary>Der Berichtsbaum des Laufs.</summary>
        public BerichtsDaten Daten { get; }

        /// <summary>Auswahl und Optionen des Laufs; <c>null</c> = alle Bausteine.</summary>
        public BerichtsKonfiguration Konfiguration { get; }

        /// <summary>Englischer Bericht?</summary>
        public bool Englisch { get; }

        /// <summary>Die Kultur des Berichts: <c>en-US</c> oder <c>de-DE</c>.</summary>
        public CultureInfo Kultur { get; }

        /// <summary>Die Erstellerangaben, um Programm und Fassung ergänzt.</summary>
        public Erstellerangaben Ersteller { get; }

        /// <summary>Das Stammprojekt des Laufs; <c>null</c>, wenn der Baum keines trägt.</summary>
        public VariantenDaten Stamm { get; }

        /// <summary>Die gewählten Varianten ohne den Stamm, in der Reihenfolge des Baums.</summary>
        public IReadOnlyList<VariantenDaten> Varianten { get; }

        /// <summary>Der Emissionsmodus des Laufs (<c>CO2</c>, <c>CO2E</c> oder gemischt).</summary>
        public string Emissionsmodus { get; }

        /// <summary>Der Kennzahlenkatalog, beschriftet nach <see cref="Emissionsmodus"/>.</summary>
        public IReadOnlyList<Kennzahl> Kennzahlen { get; }

        /// <summary>
        /// Die Namen der angehakten Kapitel (<see cref="Berichtskapitel.Name"/>) in Berichtsreihenfolge —
        /// der Anhang E mit dem Häkchen „Wirtschaftlichkeit“.
        /// </summary>
        public IReadOnlyList<string> AktiveKapitel { get; }

        /// <summary>Hat ein Stand ein Gebäude nach VDI 6007 gerechnet (Produktausweis des Deckblatts)?</summary>
        public bool Produktausweis { get; }

        /// <summary>Hat ein Stand ein Gebäude mit Anlagenkopplung gerechnet?</summary>
        public bool Anlagenkopplung { get; }

        /// <summary>Die Kennzahl zum Schlüssel, beschriftet nach dem Modus des Laufs; <c>null</c> = unbekannt.</summary>
        public Kennzahl FindeKennzahl(string schluessel)
        {
            if (schluessel == null) return null;
            return _kennzahlen.TryGetValue(schluessel, out Kennzahl k) ? k : null;
        }

        /// <summary>Ein Text aus <c>MyResource</c> in der Kultur des Berichts; fehlt er, der Schlüssel selbst.</summary>
        public string Text(string ressourcenschluessel)
        {
            if (string.IsNullOrEmpty(ressourcenschluessel)) return "";
            string text = null;
            try { text = MyResource.Resource.ResourceManager.GetString(ressourcenschluessel, Kultur); }
            catch { text = null; }
            return text ?? ressourcenschluessel;
        }

        // =====================================================================
        //  Kontext eines Blocks (Konzept 4.7, BV-E4)
        // =====================================================================

        /// <summary>
        /// Alle Stände des Laufs für <c>{{#je stand}}</c>: der Stamm (wenn der Baum einen trägt)
        /// vor den Varianten, in der Reihenfolge des Baums.
        /// </summary>
        public IReadOnlyList<VariantenDaten> Staende
        {
            get
            {
                var alle = new List<VariantenDaten>();
                if (Stamm != null) alle.Add(Stamm);
                alle.AddRange(Varianten);
                return alle;
            }
        }

        /// <summary>
        /// Der laufende Stand in <c>{{#je stand}}</c> bzw. <c>{{#je variante}}</c>; <c>null</c>
        /// außerhalb eines Standblocks. Quellen von <c>stand.*</c> lesen ihn.
        /// </summary>
        public VariantenDaten LaufenderStand { get; private set; }

        /// <summary>
        /// Das laufende Gebäude in <c>{{#je gebaeude}}</c> (eine Zeile aus
        /// <see cref="ProjektDetails.Gebaeude"/>); <c>null</c> außerhalb. Quellen von
        /// <c>gebaeude.*</c> lesen es.
        /// </summary>
        public System.Data.DataRow LaufendesGebaeude { get; private set; }

        /// <summary>Innerhalb eines Standblocks?</summary>
        public bool ImStandblock { get { return LaufenderStand != null; } }

        /// <summary>Innerhalb eines Gebäudeblocks?</summary>
        public bool ImGebaeudeblock { get { return LaufendesGebaeude != null; } }

        /// <summary>
        /// Derselbe Wertesatz mit <paramref name="stand"/> als laufendem Stand; das Gebäude des
        /// äußeren Kontexts entfällt. Das Original bleibt unverändert.
        /// </summary>
        public Berichtswerte MitStand(VariantenDaten stand)
        {
            var kopie = (Berichtswerte)MemberwiseClone();
            kopie.LaufenderStand = stand;
            kopie.LaufendesGebaeude = null;
            return kopie;
        }

        /// <summary>
        /// Derselbe Wertesatz mit <paramref name="gebaeude"/> als laufendem Gebäude; der laufende
        /// Stand bleibt. Das Original bleibt unverändert.
        /// </summary>
        public Berichtswerte MitGebaeude(System.Data.DataRow gebaeude)
        {
            var kopie = (Berichtswerte)MemberwiseClone();
            kopie.LaufendesGebaeude = gebaeude;
            return kopie;
        }

        // =====================================================================
        //  Standwerte, Paarsicht, beste Variante (Konzept 4.7, 9.5, BV-E4)
        // =====================================================================

        /// <summary>Was die Kopien eines Wertesatzes teilen (<see cref="MitStand"/>, <see cref="MitGebaeude"/>
        /// kopieren den Verweis): einmal gebildet, von jeder Kopie gelesen.</summary>
        private sealed class Geteilt
        {
            internal WirtschaftsBerichtswerte Wirtschaft;
            internal BesteVariante.Auswahl Beste;
            internal IReadOnlyDictionary<string, string> Kapitelstellen;
        }

        private readonly Geteilt _geteilt = new Geteilt();

        /// <summary>
        /// Der Wertesatz der Wirtschaftlichkeit: der des Sammlers (<see cref="BerichtsDaten.Wirtschaft"/>), ohne
        /// Sammler einer über <see cref="WirtschaftsBerichtswerte.Von"/> — einmal je Wertesatz, geteilt mit
        /// seinen Blockkopien. Beim Füllen nach dem Sammler rechnet er nichts.
        /// </summary>
        public WirtschaftsBerichtswerte Wirtschaft
        {
            get { return Daten.Wirtschaft ?? (_geteilt.Wirtschaft ??= WirtschaftsBerichtswerte.Von(Daten)); }
        }

        /// <summary>
        /// Die Überschrift vor jedem Kapitel in DIESEM Bericht (Stelle der Anhang-E-Checkliste, Konzept 11 Nr. 3) — die
        /// Engine setzt sie, sobald sie die Kapitelstellen der Vorlage kennt; geteilt mit den Blockkopien. <c>null</c> =
        /// die eigenen Überschriften der Kapitel (BV-E5, <c>tabelle.anhang_e.checkliste</c>).
        /// </summary>
        public IReadOnlyDictionary<string, string> Kapitelstellen
        {
            get { return _geteilt.Kapitelstellen; }
            internal set { _geteilt.Kapitelstellen = value; }
        }

        /// <summary>Der Stand zu einer Projektkennung; <c>null</c> = nicht im Lauf.</summary>
        public VariantenDaten FindeStand(int idProjekt)
        {
            return Staende.FirstOrDefault(v => v.IdProjekt == idProjekt);
        }

        /// <summary>
        /// Die gezeigten Stände der Wirtschaftlichkeit in ihrer Reihenfolge: in der Paarsicht A und B, sonst
        /// alle Stände des Baums (<see cref="Vergleichssicht.Spalten"/>) — die Stände der besten Variante.
        /// </summary>
        public IReadOnlyList<int> Staendefolge
        {
            get
            {
                List<int> ids = (Daten.Varianten ?? new List<VariantenDaten>()).Where(v => v != null).Select(v => v.IdProjekt).ToList();
                return Daten.Sicht != null ? Daten.Sicht.Spalten(ids) : ids;
            }
        }

        /// <summary>
        /// Stand A des Paarvergleichs (Konzept 4.7): in Sicht 2 der Stand <see cref="Vergleichssicht.IdA"/>,
        /// in Sicht 1 mit genau einer Variante der Stamm; sonst <c>null</c>.
        /// </summary>
        public VariantenDaten StandA
        {
            get
            {
                if (Daten.Sicht != null && Daten.Sicht.IstPaar) return FindeStand(Daten.Sicht.IdA);
                return Varianten.Count == 1 ? Stamm : null;
            }
        }

        /// <summary>
        /// Stand B des Paarvergleichs: in Sicht 2 der Stand <see cref="Vergleichssicht.IdB"/>, in Sicht 1 mit
        /// genau einer Variante diese; sonst <c>null</c>.
        /// </summary>
        public VariantenDaten StandB
        {
            get
            {
                if (Daten.Sicht != null && Daten.Sicht.IstPaar) return FindeStand(Daten.Sicht.IdB);
                return Varianten.Count == 1 ? Varianten[0] : null;
            }
        }

        /// <summary>
        /// Die beste Variante der Wirtschaftlichkeit (<see cref="BesteVariante.Waehle"/>, Konzept 9.5) über die
        /// <see cref="Staendefolge"/> im Erwartungsfall — dieselbe Wahl wie die Kacheln der Seite; einmal je
        /// Wertesatz.
        /// </summary>
        public BesteVariante.Auswahl Beste
        {
            get
            {
                return _geteilt.Beste ??= BesteVariante.Waehle(Wirtschaft.Ergebnisse, Daten.IdStamm, Staendefolge);
            }
        }

        /// <summary>Die Produktfassung wie auf dem Deckblatt; leer, wenn sie sich nicht bestimmen lässt.</summary>
        private static string Produktfassung()
        {
            try { return DeckblattBaustein.ProduktFassung(); }
            catch { return ""; }
        }
    }
}
