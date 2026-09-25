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

        /// <summary>Die Produktfassung wie auf dem Deckblatt; leer, wenn sie sich nicht bestimmen lässt.</summary>
        private static string Produktfassung()
        {
            try { return DeckblattBaustein.ProduktFassung(); }
            catch { return ""; }
        }
    }
}
