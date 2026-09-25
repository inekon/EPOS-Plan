using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Wirtschaftlichkeit eines Berichtslaufs als reiner Wertesatz</b> (Konzept Berichtsvorlagen 5.1,
    /// Etappe BV-E3) — <see cref="BerichtsDaten.Wirtschaft"/>. Was der Wirtschaftlichkeitsbaustein, die
    /// Anhang-E-Checkliste, der Tabellenbericht und seine Formelmappe beim Schreiben aus der Datenbank laden oder
    /// daraus rechnen — Ergebnisse, Parameter, Tarif, Bewertung, Nachweiszeilen, Bilanzkonvention, Aktualität,
    /// Zeilen der Kennzahltafel, KWKG-Lage, Kapitalwertverlauf, Strommatrix, Referenzkessel, Emissionsbilanz,
    /// Trägerpreise und Trägernamen —, steht hier. Beim Schreiben wird die Datenbank nicht berührt.
    ///
    /// <para><b>Eine Auskunft ruft den Rechenweg, sie schreibt ihn nicht ab</b> (<c>EPOS.Kern/CLAUDE.md</c>).
    /// Jeder Teil ist GENAU der Aufruf, den die Schreiber bis BV-E3 selbst taten — dieselbe Methode, dieselben
    /// Eingaben, dieselbe Fehlerbehandlung (wo der Schreiber einen Fehler fing, fängt ihn der Teil; wo nicht,
    /// nicht). Der einzige neue Rechenweg-Anteil, die Trägerpreise der Formelmappe, ist aus dem Schreiber in
    /// <see cref="Traegerpreissatz.Lies"/> ausgegliedert und wird von beiden Seiten gerufen.</para>
    ///
    /// <para><b>Zwei Arten, ihn zu bekommen.</b> Der <see cref="BerichtsDatenSammler"/> ruft EINMAL
    /// <see cref="Ermittle"/> nach der Wirtschaftlichkeitsrechnung — alle Teile werden dann dort gerechnet, der
    /// Verlauf und die Emissionsbilanz nur nach <see cref="Berichtsbedarf"/>; Word und Excel lesen denselben
    /// Satz. Ein Baum, der nicht durch den Sammler ging (Proben, Prüfstände), bekommt über <see cref="Von"/> einen
    /// Satz, der jeden Teil erst beim ersten Lesen rechnet — Zahl für Zahl und Zugriff für Zugriff der Weg der
    /// Schreiber vor BV-E3, einmal je Schreiber.</para>
    ///
    /// <para><b>Nachgeholt</b> heißt: Ein Schreiber las einen Teil, den der Sammler nicht gerechnet hat — etwa den
    /// Verlauf, obwohl der Bedarf ihn ausschloss, weil statt der geprüften Vorlage die Standardvorlage einsprang.
    /// Dann rechnet der Teil nach (derselbe Aufruf), und <see cref="Nachgeholt"/> nennt ihn. Der Bericht bleibt
    /// vollständig; im Regelfall ist die Liste leer.</para>
    /// </summary>
    public sealed class WirtschaftsBerichtswerte
    {
        private readonly BerichtsDaten _daten;

        /// <summary>
        /// EIN Controller für alle Teile — wie bis BV-E3 ein Controller je Schreiber: Die Zwischenspeicher, die
        /// er zwischen zwei Aufrufen führt (Referenzkessel, Tarif), sehen dieselbe Folge wie dort.
        /// </summary>
        private readonly WirtschaftlichkeitCtrl _provider = new WirtschaftlichkeitCtrl();

        private readonly List<string> _nachgeholt = new List<string>();
        private bool _abgeschlossen;

        private readonly Teil<List<WirtschaftlichkeitErgebnis>> _ergebnisse;
        private readonly Teil<WirtschaftlichkeitParameter> _parameter;
        private readonly Teil<TarifParameter> _tarif;
        private readonly Teil<WirtschaftlichkeitBewertung> _bewertungErsatz;
        private readonly Teil<List<ProjektWirkung>> _wirkungen;
        private readonly Teil<BilanzKonvention> _bilanzkonvention;
        private readonly Teil<WirtschaftlichkeitCtrl.ErzeugerFlags> _erzeuger;
        private readonly Teil<bool> _kwkgAktiv;
        private readonly Teil<Dictionary<int, StromMatrix>> _strommatrizen;
        private readonly Teil<ReferenzkesselInfo> _referenzkessel;
        private readonly Teil<WirtschaftlichkeitVerlaufSzenarien> _verlauf;

        private readonly Dictionary<string, string> _parameternachweis = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _traegerpreiszeile = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<WirtschaftlichkeitErgebnis, bool> _aktuell =
            new Dictionary<WirtschaftlichkeitErgebnis, bool>(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<int, List<WirtZeile>> _zeilen = new Dictionary<int, List<WirtZeile>>();
        private readonly Dictionary<int, EmissionsBilanz> _emissionsbilanz = new Dictionary<int, EmissionsBilanz>();
        private readonly Dictionary<int, List<Traegerpreissatz>> _traegerpreise = new Dictionary<int, List<Traegerpreissatz>>();
        private readonly Dictionary<int, string> _traegernamen = new Dictionary<int, string>();

        private WirtschaftsBerichtswerte(BerichtsDaten daten)
        {
            _daten = daten ?? throw new ArgumentNullException(nameof(daten));

            _ergebnisse = new Teil<List<WirtschaftlichkeitErgebnis>>(this, "Ergebnisse",
                () => AusDiesemLauf ? _daten.Wirtschaftlichkeit : _provider.LadeErgebnisse(Ids()));
            _parameter = new Teil<WirtschaftlichkeitParameter>(this, "Parameter",
                () => _provider.LadeParameter(_daten.IdStamm));
            _tarif = new Teil<TarifParameter>(this, "Tarif", () => _provider.LadeTarif(_daten.IdStamm));
            _bewertungErsatz = new Teil<WirtschaftlichkeitBewertung>(this, "Bewertung", () =>
            {
                List<WirtschaftlichkeitErgebnis> alle = Ergebnisse;
                WirtschaftlichkeitParameter p = Parameter;
                return WirtschaftlichkeitBewertung.FuerBericht(_daten, alle, p, BerichtTexte.Kultur);
            });
            _wirkungen = new Teil<List<ProjektWirkung>>(this, "Wirkungen",
                () => new ProjektWirkungCtrl().Laden(_daten.IdStamm));
            _bilanzkonvention = new Teil<BilanzKonvention>(this, "Bilanzkonvention", () =>
            {
                WirtschaftlichkeitParameter p = Parameter;
                return BilanzKonvention.Bestimme(p, new GesetzKatalog());
            });
            _erzeuger = new Teil<WirtschaftlichkeitCtrl.ErzeugerFlags>(this, "Erzeuger", () =>
            {
                // Der Schreiber fing jeden Fehler: Die Zeile ist Beiwerk, kein Ergebnis.
                try { return _provider.ErzeugerDerGruppe(_daten.IdStamm); }
                catch { return null; }
            });
            _kwkgAktiv = new Teil<bool>(this, "KwkgAktiv", () => KwkgAktivierung.IstAktiv(_daten.IdStamm, Ids()));
            _strommatrizen = new Teil<Dictionary<int, StromMatrix>>(this, "Strommatrix",
                () => _provider.LadeStromMatrix(Ids()));
            _referenzkessel = new Teil<ReferenzkesselInfo>(this, "Referenzkessel",
                () => _provider.LiesReferenzkessel(_daten.IdStamm));
            _verlauf = new Teil<WirtschaftlichkeitVerlaufSzenarien>(this, "Verlauf", () =>
            {
                WirtschaftlichkeitParameter p = Parameter;
                VerlaufRechnungen++;
                // ETAPPE E9a (Schritt B): jedes Szenario über SEINEN Betrachtungszeitraum. Ein Fehler kostet
                // den Verlauf (Bild, Brücke, Mehrjahrestafeln), nie den Bericht — wie im Baustein bisher.
                try { return _provider.BerechneVerlaufSzenarienJeZeitraum(_daten, p); }
                catch { return null; }
            });
        }

        // =====================================================================
        //  Zugang
        // =====================================================================

        /// <summary>
        /// Der Wertesatz eines Berichtsbaums: der des Sammlers (<see cref="BerichtsDaten.Wirtschaft"/>), sonst ein
        /// neuer, der jeden Teil erst beim ersten Lesen rechnet — der Rückfall für Bäume ohne Sammler (Proben,
        /// Prüfstände). Er wird nicht am Baum abgelegt: Jeder Schreiber rechnet dann für sich, wie bisher.
        /// </summary>
        public static WirtschaftsBerichtswerte Von(BerichtsDaten daten)
        {
            if (daten == null) throw new ArgumentNullException(nameof(daten));
            return daten.Wirtschaft ?? new WirtschaftsBerichtswerte(daten);
        }

        /// <summary>
        /// <b>Das Ermitteln im Sammler</b> — EINMAL je Berichtslauf, nach der Wirtschaftlichkeitsrechnung
        /// (<see cref="BerichtsDatenSammler.SammleFuerBericht(int, string, List{int}, Berichtsbedarf, IProgress{BerichtsDatenSammler.Fortschritt}, System.Threading.CancellationToken, Vergleichssicht)"/>):
        /// rechnet jeden Teil in der Folge, in der der Wortbericht ihn las; den Kapitalwertverlauf nur mit
        /// <see cref="Berichtsbedarf.Verlauf"/>, Referenzkessel und Emissionsbilanz nur mit
        /// <see cref="Berichtsbedarf.Emissionsbilanz"/> und einem Kraftwerkspark. Ein Teil, der hier scheitert,
        /// bleibt offen und rechnet beim Lesen wie bisher im Schreiber — samt dessen Fehlerbild.
        /// </summary>
        /// <param name="bedarf">Was der Bericht zeigt; <c>null</c> = <see cref="Berichtsbedarf.Alles"/>.</param>
        public static WirtschaftsBerichtswerte Ermittle(BerichtsDaten daten, Berichtsbedarf bedarf)
        {
            var w = new WirtschaftsBerichtswerte(daten) { Bedarf = bedarf ?? Berichtsbedarf.Alles };
            CultureInfo kultur = BerichtTexte.Kultur;

            // Folge des Wortberichts: Ergebnisse, Parameter, Bewertung, Tarif, Nachweiszeile, Konvention,
            // Erzeuger, Aktualität, Kennzahltafel, KWKG, Verlauf, Szenarioannahmen, Strommatrix, Emissionsbilanz;
            // danach, was Mappe, Formelmappe, Anhang E und die Kälteerzeugertafel zusätzlich lesen.
            Versuche(() => _ = w.Ergebnisse);
            Versuche(() => _ = w.Parameter);
            Versuche(() => { if (daten.Bewertung == null && w.Ergebnisse.Count > 0) _ = w.Bewertung; });
            Versuche(() => _ = w.Tarif);
            Versuche(() => _ = w.Parameternachweis(kultur));
            Versuche(() => _ = w.Bilanzkonvention);
            Versuche(() => _ = w.Erzeuger);
            foreach (VariantenDaten v in daten.Varianten)
            {
                VariantenDaten stand = v;
                Versuche(() =>
                {
                    WirtschaftlichkeitErgebnis e = w.Erwartet(stand.IdProjekt);
                    if (e != null) _ = w.ErgebnisAktuell(e);
                });
            }
            Versuche(() => _ = w.Zeilen(w.IdReferenzTafel));
            Versuche(() => _ = w.KwkgAktiv);
            if (w.Bedarf.Verlauf)
                Versuche(() => { if (!w.VerlaufEntfaellt) _ = w.Verlauf; });
            foreach (string szenario in new[] { WirtschaftlichkeitSzenario.WORST, WirtschaftlichkeitSzenario.BEST })
            {
                string sz = szenario;
                Versuche(() => _ = w.Traegerpreiszeile(sz, kultur));
            }
            Versuche(() => _ = w.Strommatrizen);
            if (w.Bedarf.Emissionsbilanz)
                Versuche(() =>
                {
                    if (w.Parameter == null || w.Parameter.IdKraftwerkspark <= 0) return;
                    _ = w.Referenzkessel;
                    foreach (VariantenDaten v in daten.Varianten)
                    {
                        VariantenDaten stand = v;
                        Versuche(() =>
                        {
                            WirtschaftlichkeitErgebnis erw = w.Erwartet(stand.IdProjekt);
                            if (erw != null && w.ErgebnisAktuell(erw)) _ = w.Emissionsbilanz(stand.IdProjekt);
                        });
                    }
                });
            foreach (VariantenDaten v in daten.Varianten)
            {
                VariantenDaten stand = v;
                Versuche(() => _ = w.Traegerpreise(stand));
            }
            Versuche(() =>
            {
                WirtschaftlichkeitBewertung b = daten.Bewertung ?? (w.Ergebnisse.Count > 0 ? w.Bewertung : null);
                if (b == null || b.Wirkungen == null) _ = w.Wirkungen;
            });
            foreach (int traeger in Kuehltraeger(daten))
            {
                int id = traeger;
                Versuche(() => _ = w.Traegername(id));
            }

            w._abgeschlossen = true;
            return w;
        }

        /// <summary>Ein Teil im Sammler: gelingt er nicht, bleibt er offen — der Lauf geht weiter.</summary>
        private static void Versuche(Action teil)
        {
            try { teil(); }
            catch (OperationCanceledException) { throw; }
            catch { /* bleibt offen; der Schreiber rechnet ihn wie bisher */ }
        }

        /// <summary>Die Kühlträger der Kälteerzeugertafel (Stromträger des Kältestroms, E34) aller Stände.</summary>
        private static IEnumerable<int> Kuehltraeger(BerichtsDaten daten)
        {
            var ids = new SortedSet<int>();
            foreach (VariantenDaten v in daten.Varianten)
            {
                ErgebnisWaermepumpeModel wp = v?.Ergebnis?.Waermepumpe;
                if (wp?.Module == null) continue;
                foreach (ErgebnisWaermepumpeModulModel m in wp.Module)
                    if (m != null && m.Kuehl_CarrierId.HasValue && m.Kuehl_CarrierId.Value > 0)
                        ids.Add(m.Kuehl_CarrierId.Value);
            }
            return ids;
        }

        // =====================================================================
        //  Der Bestand des Satzes
        // =====================================================================

        /// <summary>Der Bedarf, mit dem der Sammler ermittelt hat; <c>null</c> ohne Sammler.</summary>
        public Berichtsbedarf Bedarf { get; private set; }

        /// <summary>Hat der Sammler den Satz ermittelt (sonst rechnet jeder Teil beim ersten Lesen)?</summary>
        public bool Gesammelt { get { return _abgeschlossen; } }

        /// <summary>Die Teile, die ein Schreiber NACH dem Sammeln las und die deshalb nachgerechnet wurden.</summary>
        public IReadOnlyList<string> Nachgeholt { get { return _nachgeholt; } }

        /// <summary>Wie oft dieser Satz den Kapitalwertverlauf gerechnet hat (Nachweis des Bedarfs).</summary>
        internal int VerlaufRechnungen { get; private set; }

        /// <summary>Wie oft dieser Satz eine Emissionsbilanz gerechnet hat (Nachweis des Bedarfs).</summary>
        internal int EmissionsbilanzRechnungen { get; private set; }

        // =====================================================================
        //  Die Teile
        // =====================================================================

        /// <summary>Stammen die Zahlen aus DIESEM Lauf? Sonst ist <see cref="Ergebnisse"/> der gespeicherte Stand.</summary>
        public bool AusDiesemLauf { get { return _daten.Wirtschaftlichkeit.Count > 0; } }

        /// <summary>
        /// Die Ergebnisse, die der Bericht zeigt: die DIESES Laufs, ersatzweise der gespeicherte Stand
        /// (<see cref="WirtschaftlichkeitCtrl.LadeErgebnisse"/>) — das Rückfallnetz, falls die Rechnung scheiterte.
        /// </summary>
        public List<WirtschaftlichkeitErgebnis> Ergebnisse { get { return _ergebnisse.Wert; } }

        /// <summary>Der Parametersatz des Stammprojekts (<see cref="WirtschaftlichkeitCtrl.LadeParameter"/>).</summary>
        public WirtschaftlichkeitParameter Parameter { get { return _parameter.Wert; } }

        /// <summary>Der Tarifsatz des Stammprojekts (<see cref="WirtschaftlichkeitCtrl.LadeTarif"/>).</summary>
        public TarifParameter Tarif { get { return _tarif.Wert; } }

        /// <summary>
        /// Die Bewertung des Laufs: die des Sammlers (<see cref="BerichtsDaten.Bewertung"/>), ohne sie die aus
        /// denselben Kernmethoden (<see cref="WirtschaftlichkeitBewertung.FuerBericht(BerichtsDaten, IEnumerable{WirtschaftlichkeitErgebnis}, WirtschaftlichkeitParameter, CultureInfo)"/>).
        /// Nur lesen, wenn <see cref="Ergebnisse"/> etwas führt — so taten es die Schreiber.
        /// </summary>
        public WirtschaftlichkeitBewertung Bewertung { get { return _daten.Bewertung ?? _bewertungErsatz.Wert; } }

        /// <summary>Die nicht monetarisierbaren Wirkungen des Stammprojekts — der Rückfall der Checkliste ohne Bewertung.</summary>
        public List<ProjektWirkung> Wirkungen { get { return _wirkungen.Wert; } }

        /// <summary>Die Bilanzierungskonvention des Laufs (L12/L13) — ihr <see cref="BilanzKonvention.Ausweis"/> steht in der Nachweiszeile.</summary>
        public BilanzKonvention Bilanzkonvention { get { return _bilanzkonvention.Wert; } }

        /// <summary>Die Erzeugertypen der Vergleichsgruppe; <c>null</c>, wenn sie sich nicht lesen ließen.</summary>
        public WirtschaftlichkeitCtrl.ErzeugerFlags Erzeuger { get { return _erzeuger.Wert; } }

        /// <summary>Rechnet ein Stand der Gruppe KWKG (<see cref="KwkgAktivierung.IstAktiv(int, IEnumerable{int})"/>)?</summary>
        public bool KwkgAktiv { get { return _kwkgAktiv.Wert; } }

        /// <summary>Die Strommengen-Matrix je Projekt (<see cref="WirtschaftlichkeitCtrl.LadeStromMatrix"/>).</summary>
        public Dictionary<int, StromMatrix> Strommatrizen { get { return _strommatrizen.Wert; } }

        /// <summary>Der Referenzkessel der getrennten Erzeugung (<see cref="WirtschaftlichkeitCtrl.LiesReferenzkessel"/>).</summary>
        public ReferenzkesselInfo Referenzkessel { get { return _referenzkessel.Wert; } }

        /// <summary>
        /// Der Kapitalwertverlauf aller drei Szenarien je Betrachtungszeitraum
        /// (<see cref="WirtschaftlichkeitCtrl.BerechneVerlaufSzenarienJeZeitraum"/>); <c>null</c>, wenn die Rechnung
        /// scheiterte. Vorher <see cref="VerlaufEntfaellt"/> fragen — dann zeigt der Bericht keinen Verlauf.
        /// </summary>
        public WirtschaftlichkeitVerlaufSzenarien Verlauf { get { return _verlauf.Wert; } }

        /// <summary>
        /// Hängen die Zahlungsreihen an den Stundenreihen (Review 11, BK1, Q11)? Ein WIRKSAMER Tarifsatz
        /// (Rollentarif) oder ein Stand mit KWKG — dieselbe Regel wie im Rechenkern.
        /// </summary>
        public bool ZeitreihenNoetig { get { return (Tarif != null && Tarif.Wirksam) || KwkgAktiv; } }

        /// <summary>
        /// Das Konsistenz-Gate des Verlaufs (Review 11): Brauchen die Zahlungsreihen Stundenreihen, fehlen sie
        /// aber einem Stand ohne Fehler, entfällt der Verlauf mit Hinweis — sonst zeigte das Diagramm andere
        /// Zahlen als die Tafeln darüber.
        /// </summary>
        public bool VerlaufEntfaellt
        {
            get { return ZeitreihenNoetig && _daten.Varianten.Any(v => v.Fehler == null && v.Zeitreihen == null); }
        }

        /// <summary>
        /// Die Referenz der Kennzahltafel (Konzept § 2.9, § 2.15): in Sicht 2 der Stand A, sonst die Referenz der
        /// Gruppe (0 = Stamm) — dieselbe Regel für Wort- und Tabellenbericht.
        /// </summary>
        public int IdReferenzTafel
        {
            get { return _daten.Sicht != null && _daten.Sicht.IstPaar ? _daten.Sicht.IdA : _daten.IdGruppenreferenz; }
        }

        /// <summary>Das Ergebnis „Erwartet“ eines Stands in <see cref="Ergebnisse"/>; <c>null</c> = keins.</summary>
        public WirtschaftlichkeitErgebnis Erwartet(int idProjekt)
        {
            return Ergebnisse.FirstOrDefault(x => x.IdProjekt == idProjekt && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
        }

        /// <summary>Die Nachweiszeile des Parametersatzes in einer Kultur (<see cref="WirtschaftlichkeitParameter.Nachweis"/>).</summary>
        public string Parameternachweis(CultureInfo kultur)
        {
            string schluessel = kultur?.Name ?? "";
            if (_parameternachweis.TryGetValue(schluessel, out string text)) return text;
            Merke("Parameternachweis " + schluessel);
            WirtschaftlichkeitParameter p = Parameter;
            text = p.Nachweis(kultur);
            _parameternachweis[schluessel] = text;
            return text;
        }

        /// <summary>
        /// Passt das gespeicherte Ergebnis zum Simulationslauf (<see cref="WirtschaftlichkeitCtrl.ErgebnisAktuell"/>)?
        /// Je Ergebnis einmal gefragt.
        /// </summary>
        public bool ErgebnisAktuell(WirtschaftlichkeitErgebnis e)
        {
            if (e == null) return false;
            if (_aktuell.TryGetValue(e, out bool aktuell)) return aktuell;
            Merke("ErgebnisAktuell " + e.IdProjekt);
            aktuell = _provider.ErgebnisAktuell(e);
            _aktuell[e] = aktuell;
            return aktuell;
        }

        /// <summary>
        /// Die Zeilen der Kennzahltafel gegen eine Referenz (<see cref="WirtschaftlichkeitZeilen.Kennzahlen(IList{WirtschaftlichkeitErgebnis}, TarifParameter, int)"/>)
        /// — noch ungefiltert; die Sichtbarkeit entscheidet der Schreiber mit <see cref="WirtschaftlichkeitZeilen.Sichtbare"/>.
        /// </summary>
        public List<WirtZeile> Zeilen(int idReferenz)
        {
            if (_zeilen.TryGetValue(idReferenz, out List<WirtZeile> zeilen)) return zeilen;
            Merke("Zeilen " + idReferenz.ToString(CultureInfo.InvariantCulture));
            List<WirtschaftlichkeitErgebnis> alle = Ergebnisse;
            TarifParameter tarif = Tarif;
            zeilen = WirtschaftlichkeitZeilen.Kennzahlen(alle, tarif, idReferenz);
            _zeilen[idReferenz] = zeilen;
            return zeilen;
        }

        /// <summary>
        /// Die Zeile der gepflegten Trägerpreise eines Szenarios über alle Stände
        /// (<see cref="TraegerpreisSzenario.Nachweiszeile"/>); <c>null</c>, wo keiner gepflegt ist.
        /// </summary>
        public string Traegerpreiszeile(string szenario, CultureInfo kultur)
        {
            string schluessel = (szenario ?? "") + "|" + (kultur?.Name ?? "");
            if (_traegerpreiszeile.TryGetValue(schluessel, out string text)) return text;
            Merke("Traegerpreiszeile " + schluessel);
            text = TraegerpreisSzenario.Nachweiszeile(_daten.Varianten, szenario, kultur);
            _traegerpreiszeile[schluessel] = text;
            return text;
        }

        /// <summary>
        /// Die Emissionsbilanz eines Stands gekoppelt gegen getrennt (<see cref="EmissionsBilanzRechner.Berechne"/>);
        /// <c>null</c> = mangels Faktoren nicht bestimmbar. Nur für Stände mit aktuellem Ergebnis gefragt.
        /// </summary>
        public EmissionsBilanz Emissionsbilanz(int idProjekt)
        {
            if (_emissionsbilanz.TryGetValue(idProjekt, out EmissionsBilanz bilanz)) return bilanz;
            Merke("Emissionsbilanz " + idProjekt.ToString(CultureInfo.InvariantCulture));
            WirtschaftlichkeitParameter p = Parameter;
            EmissionsbilanzRechnungen++;
            bilanz = EmissionsBilanzRechner.Berechne(idProjekt, p);
            _emissionsbilanz[idProjekt] = bilanz;
            return bilanz;
        }

        /// <summary>Die gepflegten Trägerpreise eines Stands für den Parameterblock der Formelmappe (<see cref="Traegerpreissatz.Lies"/>).</summary>
        public IReadOnlyList<Traegerpreissatz> Traegerpreise(VariantenDaten stand)
        {
            if (stand == null) return Array.Empty<Traegerpreissatz>();
            if (_traegerpreise.TryGetValue(stand.IdProjekt, out List<Traegerpreissatz> saetze)) return saetze;
            Merke("Traegerpreise " + stand.IdProjekt.ToString(CultureInfo.InvariantCulture));
            saetze = Traegerpreissatz.Lies(stand.IdProjekt);
            _traegerpreise[stand.IdProjekt] = saetze;
            return saetze;
        }

        /// <summary>Der Name eines Energieträgers (<see cref="Emissionsquelle.TraegerName"/>).</summary>
        public string Traegername(int idTraeger)
        {
            if (_traegernamen.TryGetValue(idTraeger, out string name)) return name;
            Merke("Traegername " + idTraeger.ToString(CultureInfo.InvariantCulture));
            name = Emissionsquelle.TraegerName(idTraeger);
            _traegernamen[idTraeger] = name;
            return name;
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private List<int> Ids()
        {
            return _daten.Varianten.Select(v => v.IdProjekt).ToList();
        }

        /// <summary>Nach dem Sammeln ist jede neue Rechnung nachgeholt.</summary>
        private void Merke(string teil)
        {
            if (_abgeschlossen) _nachgeholt.Add(teil);
        }

        /// <summary>Ein Teil des Satzes: beim ersten Lesen gerechnet, danach gelesen; ein Fehler bleibt offen.</summary>
        private sealed class Teil<T>
        {
            private readonly WirtschaftsBerichtswerte _satz;
            private readonly string _name;
            private readonly Func<T> _rechne;
            private bool _da;
            private T _wert;

            internal Teil(WirtschaftsBerichtswerte satz, string name, Func<T> rechne)
            {
                _satz = satz;
                _name = name;
                _rechne = rechne;
            }

            internal T Wert
            {
                get
                {
                    if (_da) return _wert;
                    _satz.Merke(_name);
                    _wert = _rechne();
                    _da = true;
                    return _wert;
                }
            }
        }
    }

    /// <summary>
    /// <b>Die gepflegten Trägerpreise eines Stands</b> (Etappe E9a Schritt C; ausgegliedert in BV-E3) — je
    /// Träger mit Szenariopreis sein Name, der gepflegte Szenariosatz und die WIRKSAMEN Preise der drei
    /// Szenarien, mit denen die Energiekosten rechnen (<see cref="KostenEmissionRechner.PreisSatz"/>). Der
    /// Parameterblock der Formelmappe schreibt daraus je Träger und Preisart eine Zeile.
    /// </summary>
    public sealed class Traegerpreissatz
    {
        /// <summary><c>energy_carrier.id</c> des Trägers.</summary>
        public int IdTraeger;

        /// <summary>Der Name des Trägers.</summary>
        public string Name = "";

        /// <summary>Der gepflegte Szenariosatz (Günstig, Ungünstig).</summary>
        public TraegerpreisSzenario Szenario = new TraegerpreisSzenario();

        /// <summary>Wirksame Preise im Szenario „Erwartet“.</summary>
        public double? ArbeitErwartet, GrundErwartet, LeistungErwartet;

        /// <summary>Wirksame Preise im Szenario „Günstig“.</summary>
        public double? ArbeitGuenstig, GrundGuenstig, LeistungGuenstig;

        /// <summary>Wirksame Preise im Szenario „Ungünstig“.</summary>
        public double? ArbeitUnguenstig, GrundUnguenstig, LeistungUnguenstig;

        /// <summary>
        /// Die Trägerpreise eines Projekts — die Leseschritte, die bis BV-E3 im Parameterblock der Formelmappe
        /// standen, in derselben Folge: die Träger mit Szenariopreis
        /// (<see cref="EnergietraegerPreisCtrl.SzenarioJeTraeger"/>), je Träger sein Name und seine wirksamen
        /// Preise in Erwartet, Günstig und Ungünstig.
        /// </summary>
        internal static List<Traegerpreissatz> Lies(int idProjekt)
        {
            var liste = new List<Traegerpreissatz>();
            foreach (KeyValuePair<int, TraegerpreisSzenario> kv in EnergietraegerPreisCtrl.SzenarioJeTraeger(idProjekt))
            {
                var satz = new Traegerpreissatz
                {
                    IdTraeger = kv.Key,
                    Name = Emissionsquelle.TraegerName(kv.Key),
                    Szenario = kv.Value
                };
                KostenEmissionRechner.PreisSatz(idProjekt, kv.Key, WirtschaftlichkeitSzenario.ERWARTET,
                                                out satz.ArbeitErwartet, out satz.GrundErwartet, out satz.LeistungErwartet);
                KostenEmissionRechner.PreisSatz(idProjekt, kv.Key, WirtschaftlichkeitSzenario.BEST,
                                                out satz.ArbeitGuenstig, out satz.GrundGuenstig, out satz.LeistungGuenstig);
                KostenEmissionRechner.PreisSatz(idProjekt, kv.Key, WirtschaftlichkeitSzenario.WORST,
                                                out satz.ArbeitUnguenstig, out satz.GrundUnguenstig, out satz.LeistungUnguenstig);
                liste.Add(satz);
            }
            return liste;
        }
    }
}
