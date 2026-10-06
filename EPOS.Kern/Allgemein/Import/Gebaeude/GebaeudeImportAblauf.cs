using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Gebäudeimport als EIN Kern-Ablauf</b> — Lesen, Zuordnen, Prüfen; formatfrei, die
    /// Formate unterscheiden sich allein im Profil und im Leser (Softwarearchitektur 1.5,
    /// „zwei Profile und zwei Leser über einem Ablauf"). Muster <see cref="KatalogImportAblauf"/>.
    ///
    /// <para><b>Drei Hausregeln gelten wörtlich</b> (Umsetzungskonzept 3.2): Der Ablauf zeigt
    /// nichts an — der Zuordnungsdialog ist eine Zäsur, kein Rückruf. Ein fehlerhafter Eintrag
    /// bricht den Lauf nicht ab; Lesefehler sind <see cref="PruefMeldung"/>en der Stufe Fehler,
    /// nie Ausnahmen — nur <see cref="OperationCanceledException"/> beendet ihn. Und der Zustand
    /// lebt im Ablauf, nicht in der Komponente.</para>
    ///
    /// <para><b>Die Größengrenze fällt vor dem Parsen</b> (Softwarearchitektur 1.5, Regel 2):
    /// Die Hülle prüft mit <see cref="GroesseZulaessig"/> schon vor dem Öffnen des Stroms;
    /// <see cref="Lesen"/> prüft noch einmal, bevor ein Byte an den Leser geht — so gilt die
    /// Grenze auch für einen Aufrufer ohne Hülle.</para>
    ///
    /// <para><b>Das Lesen gehört in einen Arbeitsfaden</b> — in der Hülle über
    /// <c>Kulturweitergabe.Starten</c>, nie über <c>Task.Run</c> (Wächter
    /// <c>ParallelitaetWacheTests</c>). Deshalb nimmt <see cref="Lesen"/> Melder und Abbruch.</para>
    /// </summary>
    internal sealed class GebaeudeImportAblauf
    {
        /// <summary>Präfix der formatfreien Meldungsschlüssel des Ablaufs und der Zuordnung.</summary>
        public const string MELDUNG = "IMP_GEB_PROT_";

        private readonly List<PruefMeldung> _meldungen = new List<PruefMeldung>();
        private readonly List<string> _gebaeude = new List<string>();

        /// <summary>Die Uhr des Laufs — für Prüfstände ersetzbar.</summary>
        internal Func<DateTimeOffset> Uhr { get; set; } = () => DateTimeOffset.Now;

        /// <summary>Das Profil des letzten Laufs; <c>null</c> vor dem ersten.</summary>
        public GebaeudeImportProfil Profil { get; private set; }

        /// <summary>Die Quelle des letzten erfolgreichen Laufs; <c>null</c> ohne.</summary>
        public GebaeudeQuelle Quelle { get; private set; }

        /// <summary>Das gelesene Abbild; <c>null</c> vor dem Lesen oder nach einem Lesefehler.</summary>
        public GebaeudeAbbild Abbild { get; private set; }

        /// <summary>Was beim Lesen aufgefallen ist — Ablauf und Leser, dateiweit.</summary>
        public IReadOnlyList<PruefMeldung> Meldungen => _meldungen;

        /// <summary>Die Gebäude der Datei für die Klappliste — Name, sonst Kennung (U13: eines je Lauf).</summary>
        public IReadOnlyList<string> Gebaeude => _gebaeude;

        /// <summary>Die dazugeladene HottCAD-Projektdatei (Stufe SQ-1); <c>null</c> = keine.</summary>
        internal SqprojStand Projektdatei { get; private set; }

        /// <summary>
        /// Die Größengrenze der Projektdatei (E87, F4) — eigene Grenze, losgelöst von der des Gebäudeprofils; die Hülle belegt
        /// sie je Plattform (<see cref="SqprojProfil.GrenzeFuerPlattform"/>). Vorgabe die Windows-Grenze; 0 oder weniger = keine.
        /// </summary>
        internal long ProjektdateiMaxBytes { get; set; } = SqprojProfil.MAX_BYTES;

        /// <summary>
        /// Der Ordner der Arbeitskopie einer Projektdatei — für Prüfstände ersetzbar. Vorgabe: ein Unterordner des
        /// Temp-Ordners der Plattform (unter iOS im App-Container); die Kopie wird nach dem Lesen gelöscht.
        /// </summary>
        internal Func<string> Arbeitsordner { get; set; } = () => Path.Combine(Path.GetTempPath(), "epos-sqproj");

        /// <summary>
        /// Liegt die Größe innerhalb der Grenze des Profils? Die Hülle fragt das VOR dem Öffnen des
        /// Stroms (Softwarearchitektur 3.4); eine Grenze von 0 oder weniger heißt „keine".
        /// </summary>
        public static bool GroesseZulaessig(long bytes, GebaeudeImportProfil profil)
            => profil == null || profil.MaxBytes <= 0 || bytes <= profil.MaxBytes;

        // ==================================================================
        // 1 — Lesen
        // ==================================================================

        /// <summary>
        /// Liest eine Gebäudedatei: Größe gegen <see cref="GebaeudeImportProfil.MaxBytes"/> (benannte
        /// Ablehnung vor dem Parsen), vollständig in einen Puffer, SHA-256, dann der Leser des
        /// Profils. Liefert die Zahl der Gebäude; ein Lesefehler ergibt 0 und eine Meldung.
        /// </summary>
        /// <param name="quelle">Der Inhalt der Datei; wird gelesen, nicht geschlossen.</param>
        /// <param name="dateiname">Name der Datei — ein Pfadanteil wird abgeschnitten.</param>
        /// <param name="profil">Die Ausprägung (gbXML, IFC).</param>
        /// <param name="melder">Fortschrittsmelder; darf <c>null</c> sein.</param>
        /// <param name="abbruch">Abbruchzeichen des Anwenders.</param>
        /// <exception cref="OperationCanceledException">nur beim Abbruch durch den Anwender.</exception>
        public int Lesen(Stream quelle, string dateiname, GebaeudeImportProfil profil,
                         IProgress<ImportFortschritt> melder = null, CancellationToken abbruch = default)
        {
            _meldungen.Clear();
            _gebaeude.Clear();
            Abbild = null;
            Quelle = null;
            Projektdatei = null;
            Profil = profil ?? throw new ArgumentNullException(nameof(profil));

            if (quelle == null) return 0;
            melder?.Report(new ImportFortschritt(null, MELDUNG + "LESEN", GebaeudeQuelle.NurName(dateiname)));

            try
            {
                abbruch.ThrowIfCancellationRequested();

                // Die Größe VOR dem ersten gelesenen Byte, wo der Strom sie kennt.
                if (quelle.CanSeek)
                {
                    long rest = quelle.Length - quelle.Position;
                    if (!GroesseZulaessig(rest, profil))
                    {
                        ZuGross(rest, profil);
                        return 0;
                    }
                }

                byte[] puffer = Einlesen(quelle, profil, abbruch);
                if (puffer == null) return 0;   // zu groß — gemeldet

                string hash = Convert.ToHexStringLower(SHA256.HashData(puffer));

                IGebaeudeLeser leser = profil.LeserErzeugen();
                GebaeudeAbbild abbild;
                using (var strom = new MemoryStream(puffer, false))
                    abbild = leser.Lesen(strom, profil, melder, abbruch);
                if (abbild == null)
                {
                    _meldungen.Add(new PruefMeldung(PruefStufe.Fehler, profil.Meldung("LESEFEHLER"), ""));
                    return 0;
                }

                _meldungen.AddRange(abbild.Meldungen);
                if (HatFehler(abbild.Meldungen))
                    return 0;   // Lesefehler oder kein Gebäude — gemeldet, der Lauf endet

                Abbild = abbild;
                Quelle = new GebaeudeQuelle(profil.Format, dateiname, hash, puffer.LongLength, abbild.Schemastand,
                                            Uhr().ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture),
                                            Programmfassung(), profil.Zonenregel, abbild.FehlendeEntitaeten)
                {
                    NordwinkelGrad = abbild.NordwinkelGrad,
                };
                foreach (AbbildGebaeude g in abbild.Gebaeude) _gebaeude.Add(g.Anzeigename);
                Dateihinweise(abbild, Quelle.Dateiname);
                // HC-5: Flächenabweichung (F8) und Herleitungen der Grundrisse je Raum - dieselben, die der Import speichert.
                for (int i = 0; i < abbild.Gebaeude.Count; i++)
                    _meldungen.AddRange(GebaeudeRaumgrundrisse.Meldungen(GebaeudeRaumgrundrisse.Bilden(abbild, i)));
            }
            catch (OperationCanceledException)
            {
                Abbild = null;
                Quelle = null;
                _gebaeude.Clear();
                throw;
            }
            catch (Exception ex)
            {
                // Ein Lesefehler wird gefangen und gelegt, nicht geworfen (KatalogImportAblauf:185).
                Abbild = null;
                Quelle = null;
                _gebaeude.Clear();
                _meldungen.Add(new PruefMeldung(PruefStufe.Fehler, profil.Meldung("LESEFEHLER"), ex.Message));
            }

            melder?.Report(new ImportFortschritt(1.0, MELDUNG + "GELESEN",
                _gebaeude.Count.ToString(CultureInfo.InvariantCulture)));
            return _gebaeude.Count;
        }

        /// <summary>
        /// Liest den Strom vollständig in einen Puffer — höchstens <c>MaxBytes</c> + 1 Byte, damit
        /// auch ein Strom ohne Längenangabe die Grenze nicht unterläuft. <c>null</c> = zu groß.
        /// </summary>
        /// <summary>
        /// <b>Was der Dateiname einer IFC-Datei sagt</b> — je Gebäude als Hinweis, übernommen wird nichts davon: Trägt
        /// das Gebäude einen Platzhalternamen (<see cref="GebaeudeZuordnungsModell.PLATZHALTERNAMEN"/>), schlägt der
        /// Dialog den Dateinamen vor (<c>IMP_IFC_PROT_NAME_PLATZHALTER</c>, I); nennt der Dateiname genau ein Jahr
        /// (<see cref="Baujahrregel.JahrImDateinamen"/>) und führt die Datei kein Baujahr, nennt das Protokoll das Jahr
        /// (<c>IMP_IFC_PROT_BAUJAHR_DATEINAME</c>, I) — es wird NIE als Baujahr übernommen. Führt die Datei ein Baujahr und
        /// nennt der Dateiname genau ein anderes Jahr, benennt das Protokoll den Widerspruch einmal
        /// (<c>IMP_IFC_PROT_BAUJAHR_WIDERSPRUCH</c>, I); es gilt das Baujahr der Datei.
        /// </summary>
        private static void Dateihinweise(GebaeudeAbbild abbild, string dateiname)
        {
            if (!string.Equals(abbild.Format, GebaeudeQuelle.FORMAT_IFC, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(dateiname)) return;
            string ohneEndung = Path.GetFileNameWithoutExtension(dateiname.Trim()).Trim();
            int? jahr = Baujahrregel.JahrImDateinamen(dateiname);
            foreach (AbbildGebaeude g in abbild.Gebaeude)
            {
                if (ohneEndung.Length > 0 && GebaeudeZuordnungsModell.IstPlatzhaltername(g.Name))
                    g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, IfcImportProfil.MELDUNGSPRAEFIX + "NAME_PLATZHALTER", g.Name.Trim(), ohneEndung));
                if (jahr.HasValue && !g.Baujahr.HasValue)
                    g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, IfcImportProfil.MELDUNGSPRAEFIX + "BAUJAHR_DATEINAME",
                        jahr.Value.ToString(CultureInfo.InvariantCulture)));
                else if (jahr.HasValue && g.Baujahr.Value != jahr.Value)
                    g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, IfcImportProfil.MELDUNGSPRAEFIX + "BAUJAHR_WIDERSPRUCH",
                        g.Baujahr.Value.ToString(CultureInfo.InvariantCulture), jahr.Value.ToString(CultureInfo.InvariantCulture)));
            }
        }

        private byte[] Einlesen(Stream quelle, GebaeudeImportProfil profil, CancellationToken abbruch)
        {
            long grenze = profil.MaxBytes > 0 ? profil.MaxBytes : long.MaxValue;
            var ziel = new MemoryStream();
            var block = new byte[81920];
            int n;
            while ((n = quelle.Read(block, 0, block.Length)) > 0)
            {
                abbruch.ThrowIfCancellationRequested();
                ziel.Write(block, 0, n);
                if (ziel.Length > grenze)
                {
                    ZuGross(ziel.Length, profil);
                    return null;
                }
            }
            return ziel.ToArray();
        }

        private void ZuGross(long bytes, GebaeudeImportProfil profil)
        {
            _meldungen.Add(new PruefMeldung(PruefStufe.Fehler, profil.Meldung("ZU_GROSS"),
                bytes.ToString(CultureInfo.InvariantCulture),
                profil.MaxBytes.ToString(CultureInfo.InvariantCulture)));
        }

        private static bool HatFehler(IEnumerable<PruefMeldung> meldungen)
        {
            foreach (PruefMeldung m in meldungen)
                if (m.Stufe == PruefStufe.Fehler) return true;
            return false;
        }

        private static string Programmfassung()
        {
            try { return DeckblattBaustein.ProduktFassung(); }
            catch (Exception) { return null; }
        }

        // ==================================================================
        // 1b — Projektdatei dazuladen (HottCAD, Stufe SQ-1)
        // ==================================================================

        /// <summary>
        /// <b>Ist das IFC-Abbild ein HottCAD-Export?</b> Format IFC und ein <c>ObjectType</c> der Klasse <c>TModel…</c> am
        /// Gebäude oder an einem Raum (Befund Kapitel 4 und 5) — nur dann gibt es eine Projektdatei dazu.
        /// </summary>
        internal static bool IstHottcad(GebaeudeAbbild abbild, int gebaeudeIndex)
        {
            if (abbild == null || !string.Equals(abbild.Format, GebaeudeQuelle.FORMAT_IFC, StringComparison.Ordinal)) return false;
            if (gebaeudeIndex < 0 || gebaeudeIndex >= abbild.Gebaeude.Count) return false;
            AbbildGebaeude g = abbild.Gebaeude[gebaeudeIndex];
            static bool TModel(string s) => s != null && s.Trim().StartsWith("TModel", StringComparison.Ordinal);
            return TModel(g.Art) || g.Raeume.Exists(r => TModel(r.Raumtyp));
        }

        /// <summary><b>Lädt die Projektdatei von einem Pfad dazu</b> — die Datei des Anwenders wird nur gelesen (siehe Stromfassung).</summary>
        internal SqprojStand ProjektdateiLesen(string pfad, int gebaeudeIndex, CancellationToken abbruch = default)
        {
            if (string.IsNullOrWhiteSpace(pfad) || !File.Exists(pfad))
                return Projektdatei = new SqprojStand(GebaeudeQuelle.NurName(pfad ?? ""), null, 0, null, null, null,
                    new PruefMeldung(PruefStufe.Fehler, SqprojProtokoll.KEINE_DATEI, GebaeudeQuelle.NurName(pfad ?? "")));
            using (FileStream f = new FileStream(pfad, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                return ProjektdateiLesen(f, pfad, gebaeudeIndex, abbruch);
        }

        /// <summary>
        /// <b>Lädt die HottCAD-Projektdatei dazu</b> (Datenaustauschkonzept 16.2, 16.4): nur nach einem gelesenen
        /// HottCAD-IFC (<see cref="IstHottcad"/>, sonst <c>IMP_SQ_PROT_KEIN_HOTTCAD</c>), Größe gegen die eigene Grenze der
        /// Projektdatei (<see cref="ProjektdateiMaxBytes"/>, <c>IMP_SQ_PROT_ZU_GROSS</c> mit Größe und Grenze in MB), dann der Strom in eine <b>Arbeitskopie</b> unter
        /// <see cref="Arbeitsordner"/> — gelesen wird allein die Kopie, nur lesend; sie wird danach gelöscht, an der Datei
        /// des Anwenders bleibt keine Spur. Danach der Raumabgleich gegen das Gebäude <paramref name="gebaeudeIndex"/>.
        /// Eine Ablehnung lässt die IFC-Daten unberührt. Wirft nur beim Abbruch.
        /// </summary>
        internal SqprojStand ProjektdateiLesen(Stream quelle, string dateiname, int gebaeudeIndex, CancellationToken abbruch = default)
        {
            string name = GebaeudeQuelle.NurName(dateiname ?? "");
            SqprojStand Ab(string schluessel, params string[] werte)
                => Projektdatei = new SqprojStand(name, null, 0, null, null, null, new PruefMeldung(PruefStufe.Fehler, schluessel, werte));
            if (Abbild == null) return Ab(SqprojProtokoll.NICHT_GELESEN);
            if (!IstHottcad(Abbild, gebaeudeIndex)) return Ab(SqprojProtokoll.KEIN_HOTTCAD, name);
            if (quelle == null) return Ab(SqprojProtokoll.KEINE_DATEI, name);
            long grenze = ProjektdateiMaxBytes > 0 ? ProjektdateiMaxBytes : long.MaxValue;
            if (quelle.CanSeek && quelle.Length - quelle.Position > grenze)
                return Ab(SqprojProtokoll.ZU_GROSS, SqprojProtokoll.Mb(quelle.Length - quelle.Position), SqprojProtokoll.Mb(grenze));

            string ordner = Arbeitsordner();
            Directory.CreateDirectory(ordner);
            string kopie = Path.Combine(ordner, Path.GetRandomFileName() + ".sqproj");
            try
            {
                long bytes = 0;
                string hash;
                using (var ziel = new FileStream(kopie, FileMode.CreateNew, FileAccess.Write))
                using (var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
                {
                    var block = new byte[81920];
                    int n;
                    while ((n = quelle.Read(block, 0, block.Length)) > 0)
                    {
                        abbruch.ThrowIfCancellationRequested();
                        bytes += n;
                        if (bytes > grenze)
                            return Ab(SqprojProtokoll.ZU_GROSS, SqprojProtokoll.Mb(bytes), SqprojProtokoll.Mb(grenze));
                        ziel.Write(block, 0, n);
                        sha.AppendData(block, 0, n);
                    }
                    hash = Convert.ToHexStringLower(sha.GetHashAndReset());
                }
                SqprojAbbild a = SqprojLeser.Lesen(kopie);
                if (a.Abgelehnt)
                    return Projektdatei = new SqprojStand(name, hash, bytes, a, null, a.Meldungen, a.Ablehnung);
                SqprojRaumabgleich abgleich = SqprojRaumabgleich.Bilden(a, Abbild.Gebaeude[gebaeudeIndex]);
                var meldungen = new List<PruefMeldung>(a.Meldungen);
                meldungen.AddRange(abgleich.Meldungen);
                return Projektdatei = new SqprojStand(name, hash, bytes, a, abgleich, meldungen, null);
            }
            catch (IOException ex)
            {
                return Ab(SqprojProtokoll.LESEFEHLER, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Ab(SqprojProtokoll.LESEFEHLER, ex.Message);
            }
            finally
            {
                try
                {
                    if (File.Exists(kopie)) File.Delete(kopie);
                }
                catch (IOException) { /* bleibt im Temp-Ordner, der nächste Lauf stört sich nicht daran */ }
                catch (UnauthorizedAccessException) { }
            }
        }

        /// <summary><b>Entfernt die dazugeladene Projektdatei</b> (Knopf „Projektdatei entfernen“) — die IFC-Daten bleiben.</summary>
        internal void ProjektdateiEntfernen() => Projektdatei = null;

        /// <summary>
        /// <b>Übernimmt die Zonen der Projektdatei in den Plan</b> (<see cref="SqprojZonen.Uebernehmen"/>) und hängt die
        /// Zonierung (<c>IMP_SQ_PROT_ZONIERUNG</c>) und die Bilanz an (<c>IMP_SQ_PROT_BILANZ</c>: abgeglichen, nicht abgeglichen,
        /// IFC ohne Gegenstück, Zonen übernommen). <paramref name="zonierung"/> setzt die Wahl am Stand (E87, F1; der Wechsel
        /// liest nicht neu — das Abbild bleibt); <c>null</c> = die bisherige Wahl. Ohne gelesene Projektdatei ein leeres Ergebnis.
        /// </summary>
        internal SqprojZonenergebnis ProjektdateiUebernehmen(Zonenplan plan, SqprojZonierung? zonierung = null)
        {
            SqprojStand p = Projektdatei;
            if (p == null || p.Abgelehnt) return new SqprojZonenergebnis();
            if (zonierung is SqprojZonierung z) p.Gewaehlt = z;
            SqprojZonenergebnis e = SqprojZonen.Uebernehmen(plan, p.Abbild, p.Abgleich, p.Gewaehlt);
            if (p.Zonierungsmeldung() is PruefMeldung zm) e.Meldungen.Add(zm);
            e.Meldungen.Add(new PruefMeldung(PruefStufe.Info, SqprojProtokoll.BILANZ,
                SqprojProtokoll.Z(p.Abgeglichen), SqprojProtokoll.Z(p.NichtAbgeglichen), SqprojProtokoll.Z(p.IfcOhneGegenstueck),
                SqprojProtokoll.Z(e.Uebernommen)));
            return e;
        }

        /// <summary>
        /// <b>Die Konditionierung des Gebäudes im Einzonenweg</b> aus der dazugeladenen Projektdatei
        /// (<see cref="SqprojZonen.Gebaeudekonditionierung"/>) — Fläche und Volumen aus den beheizten Räumen des Gebäudes;
        /// <c>null</c> ohne gelesene Projektdatei oder wenn sie nichts liefert.
        /// </summary>
        /// <summary>
        /// Die DIN-V-18599-Nummer des Gebäudes im Einzonenweg aus der dazugeladenen Projektdatei
        /// (<see cref="SqprojZonen.Gebaeudeprofilnummer"/>) — nur der Vorschlag, gesetzt wird kein Profil; <c>null</c> ohne.
        /// </summary>
        internal int? Gebaeudeprofilnummer(int gebaeudeIndex)
        {
            SqprojStand p = Projektdatei;
            if (p == null || p.Abgelehnt || Abbild == null || gebaeudeIndex < 0 || gebaeudeIndex >= Abbild.Gebaeude.Count) return null;
            return SqprojZonen.Gebaeudeprofilnummer(p.Abbild, p.Abgleich, p.Gewaehlt);
        }

        internal Zonenkonditionierung Gebaeudekonditionierung(int gebaeudeIndex)
        {
            SqprojStand p = Projektdatei;
            if (p == null || p.Abgelehnt || Abbild == null || gebaeudeIndex < 0 || gebaeudeIndex >= Abbild.Gebaeude.Count) return null;
            List<AbbildRaum> warm = Abbild.Gebaeude[gebaeudeIndex].Raeume.Where(r => r.Beheizt).ToList();
            double flaeche = warm.Sum(r => r.FlaecheM2 ?? 0.0), volumen = warm.Sum(r => r.VolumenM3 ?? 0.0);
            Zonenkonditionierung k = SqprojZonen.Gebaeudekonditionierung(p.Abbild, p.Abgleich, flaeche > 0.0 ? flaeche : null, volumen > 0.0 ? volumen : null,
                                                                         p.Gewaehlt);
            return k != null && k.Liefert ? k : null;
        }

        // ==================================================================
        // 2 — Zuordnen
        // ==================================================================

        /// <summary>
        /// <b>Die gemeinsame Zuordnung (E2)</b> eines Gebäudes der Datei auf die Zielfelder des
        /// Einzonen-Wegs (Zonenregel X4): Gruppen bilden, U-Werte flächengewichtet, Fenster nach
        /// Himmelsrichtung, Fensterabzug (U14), Vorgaben der Baualtersklasse (U12, U15). Schreibt
        /// nichts. Die Regeln stehen in <see cref="GebaeudeAggregation"/>.
        /// </summary>
        /// <param name="gebaeudeIndex">Index in <see cref="Gebaeude"/>.</param>
        /// <param name="baualtersklasse">Die gewählte Klasse A…M; <c>null</c> = keine (dann keine Vorgaben).</param>
        /// <param name="beheiztUebersteuert">
        /// Die Haken der Raumliste, Raumkennung → beheizt; <c>null</c> = alles wie gelesen. Sie
        /// wirken nur auf diese Zuordnung — das Abbild bleibt, wie der Leser es gebaut hat.
        /// </param>
        /// <exception cref="InvalidOperationException">wenn nichts gelesen ist.</exception>
        /// <exception cref="ArgumentOutOfRangeException">bei einem Index außerhalb der Klappliste.</exception>
        /// <param name="raumtemperaturAlsSollwert">
        /// Der Schalter „Raumtemperatur der Datei als Heizsollwert übernehmen" (<see cref="GebaeudeCadSollwert"/>);
        /// Vorgabe aus — dann gilt die Normtemperatur.
        /// </param>
        public GebaeudeImportSatz Zuordnen(int gebaeudeIndex, char? baualtersklasse,
                                           IReadOnlyDictionary<string, bool> beheiztUebersteuert = null,
                                           bool raumtemperaturAlsSollwert = false)
        {
            GebaeudePruefen(gebaeudeIndex);
            GebaeudeImportSatz satz = GebaeudeAggregation.Bilden(Abbild, gebaeudeIndex, baualtersklasse, Quelle, Profil, beheiztUebersteuert,
                                                                 raumtemperaturAlsSollwert);
            if (satz != null) satz.Projektdatei = Projektdatei;
            return satz;
        }

        /// <summary>
        /// <b>Die Raumliste eines Gebäudes</b> mit dem wirksamen „beheizt" und dem Grund der
        /// Entscheidung (Attribut der Datei, Namensregel, keine Angabe) — in Dateireihenfolge, für
        /// die Liste mit dem Haken im Zuordnungsdialog (Umsetzungskonzept 3.5 Nr. 3).
        /// </summary>
        /// <param name="gebaeudeIndex">Index in <see cref="Gebaeude"/>.</param>
        /// <param name="beheiztUebersteuert">Dieselben Haken wie bei <see cref="Zuordnen"/>; <c>null</c> = keine.</param>
        /// <exception cref="InvalidOperationException">wenn nichts gelesen ist.</exception>
        /// <exception cref="ArgumentOutOfRangeException">bei einem Index außerhalb der Klappliste.</exception>
        public IReadOnlyList<GebaeudeRaumzeile> Raeume(int gebaeudeIndex, IReadOnlyDictionary<string, bool> beheiztUebersteuert = null)
        {
            GebaeudePruefen(gebaeudeIndex);
            var liste = new List<GebaeudeRaumzeile>();
            foreach (AbbildRaum r in Abbild.Gebaeude[gebaeudeIndex].Raeume)
                liste.Add(new GebaeudeRaumzeile(r, beheiztUebersteuert));
            return liste;
        }

        private void GebaeudePruefen(int gebaeudeIndex)
        {
            if (Abbild == null || Profil == null)
                throw new InvalidOperationException("Es ist keine Gebäudedatei gelesen.");
            if (gebaeudeIndex < 0 || gebaeudeIndex >= Abbild.Gebaeude.Count)
                throw new ArgumentOutOfRangeException(nameof(gebaeudeIndex));
        }

        // ==================================================================
        // 3 — Prüfen
        // ==================================================================

        /// <summary>
        /// Die Plausibilität der ÜBERNOMMENEN Zeilen — ohne zu schreiben (Vorprüfung,
        /// Softwarearchitektur 3.4): Nutzfläche und Raumhöhe sind Pflicht und größer null; ein
        /// U-Wert außerhalb 0,1 … 6 W/(m²K) und ein g-Wert außerhalb (0, 1] sind Warnungen; eine
        /// übernommene Zeile mit Fehlermarkierung blockiert. Jede Meldung der Stufe Fehler sperrt
        /// die Übernahme.
        ///
        /// <para>Dieselbe Prüfung gilt am OK des Zuordnungsdialogs: Die Hülle legt dessen
        /// Handänderungen und Haken auf den Satz (<see cref="GebaeudeImportSatz.ManuellSetzen"/>,
        /// <see cref="GebaeudeImportSatz.HakenSetzen"/>) und fragt hier.</para>
        /// </summary>
        /// <param name="satz">Der Satz samt Handänderungen.</param>
        /// <param name="katalogname">
        /// Der Name, unter dem das Gebäude angelegt würde; <c>null</c> = nicht zu prüfen, leer =
        /// Fehler (ein Katalogsatz ohne Namen ist nicht wiederzufinden).
        /// </param>
        public static IReadOnlyList<PruefMeldung> Pruefen(GebaeudeImportSatz satz, string katalogname = null)
        {
            var liste = new List<PruefMeldung>();
            if (satz == null) return liste;
            if (satz.Ablehnung != null) liste.Add(satz.Ablehnung);
            if (katalogname != null && string.IsNullOrWhiteSpace(katalogname))
                liste.Add(new PruefMeldung(PruefStufe.Fehler, MELDUNG + "NAME_FEHLT"));

            foreach (string pflicht in new[] { GebaeudeZielfelder.NUTZFLAECHE, GebaeudeZielfelder.RAUMHOEHE })
            {
                GebaeudeFeldzeile z = satz.Zeile(pflicht);
                if (z == null || !z.Uebernehmen || !(z.Wert > 0.0) || double.IsInfinity(z.Wert.Value))
                    liste.Add(new PruefMeldung(PruefStufe.Fehler, MELDUNG + "PFLICHT_FEHLT", pflicht));
            }

            foreach (GebaeudeFeldzeile z in satz.Zeilen)
            {
                if (!z.Uebernehmen) continue;

                if (z.Markierung == PruefStufe.Fehler)
                    liste.Add(new PruefMeldung(PruefStufe.Fehler, MELDUNG + "ZEILE_FEHLER", z.Zielfeld));

                if (z.Wert is double u && IstUWert(z.Zielfeld)
                    && !(u >= GebaeudeFestwerte.U_MIN && u <= GebaeudeFestwerte.U_MAX))
                    liste.Add(new PruefMeldung(PruefStufe.Warnung, MELDUNG + "U_AUSSERHALB", z.Zielfeld, Zahl(u),
                        Zahl(GebaeudeFestwerte.U_MIN), Zahl(GebaeudeFestwerte.U_MAX)));

                if (z.Wert is double g && z.Zielfeld == GebaeudeZielfelder.G_WERT && !(g > 0.0 && g <= 1.0))
                    liste.Add(new PruefMeldung(PruefStufe.Warnung, MELDUNG + "G_AUSSERHALB", Zahl(g)));

                // Das Baujahr ist eine ganze Jahreszahl im Bereich der Spalte (CHECK 1500 … 2100) - eine
                // Handänderung mit Nachkommastellen oder außerhalb des Bereichs sperrt die Übernahme.
                if (z.Wert is double jahr && z.Zielfeld == GebaeudeZielfelder.BAUJAHR
                    && !(jahr == Math.Floor(jahr) && jahr >= GebaeudeSchema.BAUJAHR_MIN && jahr <= GebaeudeSchema.BAUJAHR_MAX))
                    liste.Add(new PruefMeldung(PruefStufe.Fehler, MELDUNG + "BAUJAHR_UNGUELTIG", Zahl(jahr),
                        GebaeudeSchema.BAUJAHR_MIN.ToString(CultureInfo.InvariantCulture),
                        GebaeudeSchema.BAUJAHR_MAX.ToString(CultureInfo.InvariantCulture)));

                // Beginn und Ende der Nachtzeit sind volle Stunden im Bereich der Spalte (CHECK 0 … 23, E43);
                // das Paar prüft der vorbelegte Editor mit derselben Regel wie das Stundenmodell.
                if (z.Wert is double stunde
                    && (z.Zielfeld == GebaeudeZielfelder.NACHT_BEGINN || z.Zielfeld == GebaeudeZielfelder.NACHT_ENDE)
                    && !(stunde == Math.Floor(stunde) && stunde >= Nachtzeit.STUNDE_MIN && stunde <= Nachtzeit.STUNDE_MAX))
                    liste.Add(new PruefMeldung(PruefStufe.Fehler, MELDUNG + "NACHTZEIT_UNGUELTIG", z.Zielfeld, Zahl(stunde),
                        Nachtzeit.STUNDE_MIN.ToString(CultureInfo.InvariantCulture),
                        Nachtzeit.STUNDE_MAX.ToString(CultureInfo.InvariantCulture)));
            }

            // Prüfgröße: Volumen gegen Nutzfläche × Raumhöhe (Umsetzungskonzept 3.4, 20 %).
            double? a = satz.Zeile(GebaeudeZielfelder.NUTZFLAECHE)?.Wert;
            double? h = satz.Zeile(GebaeudeZielfelder.RAUMHOEHE)?.Wert;
            double? v = satz.Zeile(GebaeudeZielfelder.VOLUMEN)?.Wert;
            if (a > 0.0 && h > 0.0 && v > 0.0 && Math.Abs(v.Value - a.Value * h.Value) > VOLUMEN_TOLERANZ * v.Value)
                liste.Add(new PruefMeldung(PruefStufe.Warnung, MELDUNG + "VOLUMEN_ABWEICHUNG",
                    Zahl(v.Value), Zahl(a.Value * h.Value)));

            return liste;
        }

        /// <summary>Relative Abweichung, ab der das Volumen gegen Nutzfläche × Raumhöhe warnt.</summary>
        internal const double VOLUMEN_TOLERANZ = 0.2;

        /// <summary>Sperrt eine dieser Meldungen die Übernahme?</summary>
        public static bool Blockiert(IReadOnlyList<PruefMeldung> meldungen) => meldungen != null && HatFehler(meldungen);

        private static bool IstUWert(string zielfeld)
            => zielfeld == GebaeudeZielfelder.U_AUSSENWAND || zielfeld == GebaeudeZielfelder.U_FENSTER
               || zielfeld == GebaeudeZielfelder.U_DACH || zielfeld == GebaeudeZielfelder.U_GRUND
               || zielfeld == GebaeudeZielfelder.U_SONSTIGE;

        internal static string Zahl(double w) => w.ToString("0.######", CultureInfo.InvariantCulture);
    }
}
