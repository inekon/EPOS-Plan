using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    // =====================================================================================
    //  DER ARBEITSSTAND DER KONDITIONIERUNG (Stufe KP2, Welle K2; Entwurf KP2 Abschnitt 2)
    //
    //  Der Editor hält die Konditionierung im Arbeitsstand; geschrieben wird allein im OK-Weg.
    //  Diese Datei trägt die unveränderlichen Werteträger — je Ebene ein Konditionierungsstand,
    //  für das Gebäude samt Zonen ein Konditionierungsarbeitsstand — und die Befunde der Schritte.
    //  Die Schritte selbst stehen in Konditionierungsarbeit; beides ohne Datenbank.
    // =====================================================================================

    /// <summary>
    /// <b>Herkunft und Vermerk eines angelegten Kalenders</b> (Entwurf KP2, Festlegung 5, Befund B8):
    /// die zuletzt übernommene Vorlage und der Vermerk des zuletzt angewandten Werkzeugs. Beide
    /// zusammen bilden die Spalte <c>Bemerkung</c> — „Herkunft · letzter Werkzeugvermerk"; ein
    /// Werkzeug ersetzt nur den Vermerk, die Herkunft „aus Vorlage …" bleibt.
    /// </summary>
    /// <remarks>
    /// Die Herkunft steht nur als <b>Text</b> in <c>Bemerkung</c>, nie als Id am Ziel (N1.63 Nr. 19);
    /// gelesen wird sie über den Satz <c>KOND_MSG_HERKUNFT_VORLAGE</c> in beiden Sprachen, damit ein
    /// Bestand aus der anderen Sprache ebenso erkannt wird. Ein Text, der keiner Herkunft gleicht,
    /// ist ein Vermerk.
    /// </remarks>
    public sealed class Kalenderherkunft : IEquatable<Kalenderherkunft>
    {
        /// <summary>Das Trennzeichen zwischen Herkunft und Vermerk in <c>Bemerkung</c>.</summary>
        public const string TRENNER = " · ";

        /// <summary>Keine Herkunft, kein Vermerk.</summary>
        public static Kalenderherkunft Keine { get; } = new Kalenderherkunft(null, null);

        /// <summary>
        /// Baut die Herkunft; leere Texte heißen „keine". <paramref name="istProfil"/> = die Herkunft ist ein
        /// Nutzungsprofil des Katalogs (NP3c), nicht eine Konditionierungsvorlage gleichen Namens.
        /// </summary>
        public Kalenderherkunft(string vorlage, string vermerk, bool istProfil = false)
        {
            Vorlage = string.IsNullOrWhiteSpace(vorlage) ? null : vorlage.Trim();
            Vermerk = string.IsNullOrWhiteSpace(vermerk) ? null : vermerk.Trim();
            IstProfil = istProfil && Vorlage != null;
        }

        /// <summary>
        /// Der Name der zuletzt übernommenen Vorlage — bei <see cref="IstProfil"/> der Name des übernommenen
        /// Nutzungsprofils; <c>null</c> = keine.
        /// </summary>
        public string Vorlage { get; }

        /// <summary>
        /// <b>Die Art der Herkunft</b> (NP3c): <c>true</c> = ein übernommenes Nutzungsprofil („aus Nutzungsprofil …"),
        /// <c>false</c> = eine Konditionierungsvorlage („aus Vorlage …"). Drei EPOS-Muster tragen mit Absicht den Namen
        /// einer Vorlage; erst die Art bezeichnet die Herkunft eindeutig — der OK-Weg nimmt danach die Nutzung des
        /// Profils, nicht die der gleichnamigen Vorlage.
        /// </summary>
        public bool IstProfil { get; }

        /// <summary>Der Vermerk des zuletzt angewandten Werkzeugs; <c>null</c> = keiner.</summary>
        public string Vermerk { get; }

        /// <summary>Weder Herkunft noch Vermerk?</summary>
        public bool IstLeer => Vorlage == null && Vermerk == null;

        /// <summary>Dieselbe Herkunft mit einem neuen Vermerk — der Weg der Werkzeuge (B8).</summary>
        public Kalenderherkunft MitVermerk(string vermerk) => new Kalenderherkunft(Vorlage, vermerk, IstProfil);

        /// <summary>
        /// <b>Die Spalte <c>Bemerkung</c></b>: „aus Vorlage Büro · Zeitfenster …", nur die Herkunft,
        /// nur der Vermerk oder <c>null</c>. Gekürzt auf <see cref="KonditionierungSchema.BEMERKUNG_MAX_ZEICHEN"/>.
        /// </summary>
        public string Bemerkung()
        {
            string herkunft = Vorlage == null
                ? null
                : string.Format(CultureInfo.CurrentCulture,
                                IstProfil ? MyResource.Resource.RNP_MSG_HERKUNFT_PROFIL : MyResource.Resource.KOND_MSG_HERKUNFT_VORLAGE,
                                Vorlage);
            string text = herkunft == null ? Vermerk : Vermerk == null ? herkunft : herkunft + TRENNER + Vermerk;
            if (text == null) return null;
            return text.Length <= KonditionierungSchema.BEMERKUNG_MAX_ZEICHEN
                ? text
                : text.Substring(0, KonditionierungSchema.BEMERKUNG_MAX_ZEICHEN);
        }

        /// <summary>
        /// <b>Liest Herkunft und Vermerk aus einer Bemerkung</b> — das Gegenstück zu
        /// <see cref="Bemerkung"/>. Beginnt der Text mit dem Satz der Herkunft (deutsch oder
        /// englisch), ist der Rest bis zum letzten <see cref="TRENNER"/> der Vorlagenname und das
        /// Übrige der Vermerk; sonst ist der ganze Text ein Vermerk.
        /// </summary>
        public static Kalenderherkunft AusBemerkung(string bemerkung)
        {
            if (string.IsNullOrWhiteSpace(bemerkung)) return Keine;
            foreach ((string satz, bool profil) in Herkunftssaetze())
            {
                int platz = satz.IndexOf("{0}", StringComparison.Ordinal);
                if (platz < 0) continue;
                string vor = satz.Substring(0, platz);
                string nach = satz.Substring(platz + 3);
                if (vor.Length == 0 || !bemerkung.StartsWith(vor, StringComparison.Ordinal)) continue;

                string rest = bemerkung.Substring(vor.Length);
                string vermerk = null;
                int trenner = rest.LastIndexOf(TRENNER, StringComparison.Ordinal);
                if (trenner >= 0)
                {
                    vermerk = rest.Substring(trenner + TRENNER.Length);
                    rest = rest.Substring(0, trenner);
                }
                if (nach.Length > 0 && rest.EndsWith(nach, StringComparison.Ordinal))
                    rest = rest.Substring(0, rest.Length - nach.Length);
                return new Kalenderherkunft(rest, vermerk, profil);
            }
            return new Kalenderherkunft(null, bemerkung);
        }

        /// <summary>
        /// Die Sätze der Herkunft in beiden Sprachen der Oberfläche — die aktuelle zuerst; je Sprache der Satz des
        /// Nutzungsprofils (NP3c) vor dem der Vorlage.
        /// </summary>
        private static IEnumerable<(string Satz, bool Profil)> Herkunftssaetze()
        {
            var gesehen = new HashSet<string>(StringComparer.Ordinal);
            foreach (CultureInfo k in new[]
                     {
                         CultureInfo.CurrentUICulture, CultureInfo.GetCultureInfo("de-DE"), CultureInfo.GetCultureInfo("en-US")
                     })
                foreach ((string schluessel, bool profil) in new[] { ("RNP_MSG_HERKUNFT_PROFIL", true), ("KOND_MSG_HERKUNFT_VORLAGE", false) })
                {
                    string s = null;
                    try { s = MyResource.Resource.ResourceManager.GetString(schluessel, k); }
                    catch (Exception) { s = null; }
                    if (!string.IsNullOrEmpty(s) && gesehen.Add(s)) yield return (s, profil);
                }
        }

        /// <inheritdoc/>
        public bool Equals(Kalenderherkunft other)
            => other != null && string.Equals(Vorlage, other.Vorlage, StringComparison.Ordinal)
               && string.Equals(Vermerk, other.Vermerk, StringComparison.Ordinal) && IstProfil == other.IstProfil;

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as Kalenderherkunft);

        /// <inheritdoc/>
        public override int GetHashCode()
            => StringComparer.Ordinal.GetHashCode(Vorlage ?? "") ^ StringComparer.Ordinal.GetHashCode(Vermerk ?? "")
               ^ (IstProfil ? 0x5f3759df : 0);

        /// <summary>Die Bemerkung als Kurzfassung.</summary>
        public override string ToString() => Bemerkung() ?? "—";
    }

    /// <summary>
    /// <b>Die Konditionierung EINER Ebene</b> — eines Projektgebäudes, einer Zone oder eines
    /// Katalogbaus, ebenso der Inhalt einer Vorlage (Entwurf KP2 Abschnitt 2, K2 Teilschritt 1):
    /// die Bestandszellen samt Nachtzeiten, Ferienzeiträumen und Merkern (<see cref="Bestand"/>),
    /// die Vorgabezellen (was <c>Tab_Konditionierungsvorgabe</c> für diesen Eigentümer trägt), je
    /// Größe der angelegte Kalender samt Perioden und seine <see cref="Kalenderherkunft"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>Unveränderlich.</b> Jede Änderung liefert einen neuen Stand; der alte bleibt, wie er
    /// war — so kann die Oberfläche „Zurücknehmen" (eine Stufe) über den vorigen Stand leisten.
    /// <see cref="Bestand"/> gibt eine Kopie heraus.</para>
    /// <para><b>Ein Ort je Zelle</b> (Konzept 5.6): Eine Bestandszelle trägt ihren Wert im
    /// <see cref="Bestand"/>, ihre Vorgabezelle nur „aus", Zeiten und ΔT; eine Zelle ohne
    /// Bestandsspalte trägt ihren Wert in der Vorgabezelle (<see cref="Matrixzellenort"/>).</para>
    /// </remarks>
    public sealed class Konditionierungsstand
    {
        private const int ZEILEN = 6;

        private readonly Matrixeingang _bestand;
        private readonly Matrixzelle[] _zellen;
        private readonly Konditionierungskalender[] _kalender;
        private readonly Kalenderherkunft[] _herkunft;
        private readonly Gemeinschaftsperiode[] _gemeinsam;
        private readonly Ferienzeile[] _ferien;
        private readonly BenannteWoche[] _wochen;
        private readonly string[] _feriennamen;

        private Konditionierungsstand(Kalendereigentuemer art, Matrixeingang bestand, Matrixzelle[] zellen,
                                      Konditionierungskalender[] kalender, Kalenderherkunft[] herkunft,
                                      Gemeinschaftsperiode[] gemeinsam, Ferienzeile[] ferien, BenannteWoche[] wochen,
                                      string[] feriennamen = null)
        {
            _feriennamen = feriennamen ?? new string[Matrixeingang.FERIENZEITRAEUME];
            Art = art;
            _bestand = bestand ?? new Matrixeingang();
            _zellen = zellen;
            _kalender = kalender;
            _herkunft = herkunft;
            _gemeinsam = gemeinsam ?? Array.Empty<Gemeinschaftsperiode>();
            _ferien = ferien ?? Array.Empty<Ferienzeile>();
            _wochen = wochen ?? Array.Empty<BenannteWoche>();
            // Die Ferienliste der Ebene geht mit in die Bestandsfelder, die der Generator liest (Matrixeingang.WeitereFerien).
            if (_ferien.Length > 0 && !ReferenceEquals(_bestand.WeitereFerien, _ferien))
            {
                _bestand = _bestand.Kopie();
                _bestand.WeitereFerien = _ferien;
            }
        }

        /// <summary>Wem die Ebene gehört — Gebäude, Zone, Katalogbau oder Vorlage.</summary>
        public Kalendereigentuemer Art { get; }

        /// <summary>Die Bestandsfelder der Ebene — eine KOPIE; eine Änderung geht über <see cref="MitBestand"/>.</summary>
        public Matrixeingang Bestand => _bestand.Kopie();

        /// <summary>Eine leere Ebene: keine Vorgabezelle, kein angelegter Kalender.</summary>
        public static Konditionierungsstand Leer(Kalendereigentuemer art, Matrixeingang bestand)
        {
            var zellen = new Matrixzelle[5 * ZEILEN];
            for (int i = 0; i < zellen.Length; i++) zellen[i] = Matrixzelle.Leer;
            var herkunft = new Kalenderherkunft[5];
            for (int i = 0; i < herkunft.Length; i++) herkunft[i] = Kalenderherkunft.Keine;
            return new Konditionierungsstand(art, (bestand ?? new Matrixeingang()).Kopie(), zellen,
                                             new Konditionierungskalender[5], herkunft, null, null, null);
        }

        /// <summary>
        /// <b>Die Ebene aus den Zeilen der Datenbank</b> — Vorgabezeilen (je Größe und Zeile gilt die
        /// erste, wie im Leser <see cref="Vorgabematrix.Bilden"/>), die gelesenen Kalender und ihre
        /// Bemerkungen.
        /// </summary>
        public static Konditionierungsstand Aus(Kalendereigentuemer art, Matrixeingang bestand,
                                                IEnumerable<Vorgabezeile> vorgaben,
                                                IReadOnlyDictionary<Konditionierungsgroesse, Konditionierungskalender> kalender,
                                                IReadOnlyDictionary<Konditionierungsgroesse, string> bemerkungen)
        {
            Konditionierungsstand s = Leer(art, bestand);
            var zellen = (Matrixzelle[])s._zellen.Clone();
            var gesetzt = new bool[zellen.Length];
            if (vorgaben != null)
                foreach (Vorgabezeile v in vorgaben)
                {
                    if (v == null || !Konditionierungsgroessen.AusKennwort(v.Groesse, out Konditionierungsgroesse g)) continue;
                    int z = Zeilenindex(v.Zeile);
                    if (z < 0) continue;
                    int i = Index(g, z);
                    if (gesetzt[i]) continue;
                    gesetzt[i] = true;
                    zellen[i] = Zelle(v);
                }
            var k = new Konditionierungskalender[5];
            var h = (Kalenderherkunft[])s._herkunft.Clone();
            if (kalender != null)
                foreach (KeyValuePair<Konditionierungsgroesse, Konditionierungskalender> p in kalender)
                {
                    if (p.Value == null) continue;
                    k[(int)p.Key] = p.Value;
                    string b = null;
                    if (bemerkungen != null) bemerkungen.TryGetValue(p.Key, out b);
                    h[(int)p.Key] = Kalenderherkunft.AusBemerkung(b);
                }
            return new Konditionierungsstand(art, s._bestand, zellen, k, h, null, null, null);
        }

        // -----------------------------------------------------------------------------
        //  Lesen
        // -----------------------------------------------------------------------------

        /// <summary>Die Vorgabezelle einer Größe und Zeile (Kennwort aus <see cref="DbWerte.KOND_ZEILEN"/>); leer = keine.</summary>
        public Matrixzelle Vorgabe(Konditionierungsgroesse g, string zeile)
        {
            int z = Zeilenindex(zeile);
            if (z < 0) throw new ArgumentException("Die Zeile „" + (zeile ?? "leer") + "“ ist keine der sechs.", nameof(zeile));
            return _zellen[Index(g, z)];
        }

        /// <summary>Der angelegte Kalender einer Größe; <c>null</c> = abgeleitet.</summary>
        public Konditionierungskalender Kalender(Konditionierungsgroesse g) => _kalender[(int)g];

        /// <summary>Herkunft und Vermerk des Kalenders einer Größe.</summary>
        public Kalenderherkunft Herkunft(Konditionierungsgroesse g) => _herkunft[(int)g];

        /// <summary>Die angelegten Kalender je Größe.</summary>
        public IReadOnlyDictionary<Konditionierungsgroesse, Konditionierungskalender> Angelegt()
        {
            var d = new Dictionary<Konditionierungsgroesse, Konditionierungskalender>();
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                if (_kalender[(int)g] != null) d[g] = _kalender[(int)g];
            return d;
        }

        /// <summary>Wie viele Kalender angelegt sind.</summary>
        public int KalenderAnzahl
        {
            get
            {
                int n = 0;
                foreach (Konditionierungskalender k in _kalender) if (k != null) n++;
                return n;
            }
        }

        /// <summary>Wie viele Vorgabezellen etwas tragen (Wert, „aus", Zeiten oder ΔT).</summary>
        public int VorgabenAnzahl
        {
            get
            {
                int n = 0;
                foreach (Matrixzelle z in _zellen) if (Traegt(z)) n++;
                return n;
            }
        }

        /// <summary>Trägt die Ebene gar nichts — keine Vorgabezelle, keinen Kalender?</summary>
        public bool TabellenLeer => VorgabenAnzahl == 0 && KalenderAnzahl == 0;

        /// <summary>
        /// <b>Die Vorgabezeilen dieser Ebene</b> — die Zeilen, die <c>Tab_Konditionierungsvorgabe</c>
        /// für sie trägt, ohne Eigentümerspalten; eine Zelle ohne jede Angabe hat keine Zeile.
        /// </summary>
        public List<Vorgabezeile> Vorgabezeilen()
        {
            var liste = new List<Vorgabezeile>();
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                for (int z = 0; z < ZEILEN; z++)
                {
                    Matrixzelle c = _zellen[Index(g, z)];
                    if (!Traegt(c)) continue;
                    liste.Add(new Vorgabezeile
                    {
                        Groesse = Konditionierungsgroessen.Kennwort(g),
                        Zeile = DbWerte.KOND_ZEILEN[z],
                        Wert = c.Belegt && !c.Aus ? c.Wert : (double?)null,
                        Aus = c.Aus,
                        Von = c.Von,
                        Bis = c.Bis,
                        BedingtK = c.BedingtK,
                    });
                }
            return liste;
        }

        /// <summary>Die Matrix dieser Ebene allein (ohne Kaskade): Bestand und Vorgabezellen, gebildet wie im Lauf.</summary>
        public Vorgabematrix Matrix() => Vorgabematrix.Bilden(_bestand.Kopie(), Vorgabezeilen(), Art);

        // -----------------------------------------------------------------------------
        //  Ändern — je ein neuer Stand
        // -----------------------------------------------------------------------------

        /// <summary>Derselbe Stand mit geändertem Bestand; <paramref name="aendern"/> bekommt eine Kopie.</summary>
        public Konditionierungsstand MitBestand(Action<Matrixeingang> aendern)
        {
            Matrixeingang b = _bestand.Kopie();
            aendern?.Invoke(b);
            return new Konditionierungsstand(Art, b, _zellen, _kalender, _herkunft, _gemeinsam, _ferien, _wochen, _feriennamen);
        }

        /// <summary>Derselbe Stand mit einem neuen Bestand (kopiert).</summary>
        public Konditionierungsstand MitBestand(Matrixeingang bestand)
            => new Konditionierungsstand(Art, (bestand ?? new Matrixeingang()).Kopie(), _zellen, _kalender, _herkunft, _gemeinsam, _ferien, _wochen, _feriennamen);

        /// <summary>Derselbe Stand mit einer anderen Vorgabezelle; <c>null</c> heißt leer.</summary>
        public Konditionierungsstand MitVorgabe(Konditionierungsgroesse g, string zeile, Matrixzelle zelle)
        {
            int z = Zeilenindex(zeile);
            if (z < 0) throw new ArgumentException("Die Zeile „" + (zeile ?? "leer") + "“ ist keine der sechs.", nameof(zeile));
            var zellen = (Matrixzelle[])_zellen.Clone();
            zellen[Index(g, z)] = zelle == null || !Traegt(zelle) ? Matrixzelle.Leer : zelle;
            return new Konditionierungsstand(Art, _bestand, zellen, _kalender, _herkunft, _gemeinsam, _ferien, _wochen, _feriennamen);
        }

        /// <summary>Derselbe Stand mit einem anderen Kalender der Größe (<c>null</c> = verworfen) samt Herkunft.</summary>
        /// <exception cref="ArgumentException">Der Kalender trägt eine andere Größe.</exception>
        public Konditionierungsstand MitKalender(Konditionierungsgroesse g, Konditionierungskalender kalender,
                                                 Kalenderherkunft herkunft)
        {
            if (kalender != null && kalender.Groesse != g)
                throw new ArgumentException("Der Kalender trägt die Größe " + Konditionierungsgroessen.Kennwort(kalender.Groesse) +
                                            ", abgelegt wird " + Konditionierungsgroessen.Kennwort(g) + ".", nameof(kalender));
            var k = (Konditionierungskalender[])_kalender.Clone();
            var h = (Kalenderherkunft[])_herkunft.Clone();
            Gemeinschaftsperiode[] gemeinsam = _gemeinsam;
            if (kalender != null && _gemeinsam.Length > 0)
            {
                if (_kalender[(int)g] == null)
                    kalender = Ausgebreitet(kalender, kalender, _gemeinsam);       // neu angelegt: die Gemeinschaft wirkt
                else
                    gemeinsam = _gemeinsam                                          // eine entfernte Kopie verlässt die Maske
                        .Select(p => !p.Gilt(g) || kalender.Perioden.Any(r => r.Rang == p.Rang) ? p : p.MitMaske(p.Maske & ~Gemeinschaftsperiode.Bit(g)))
                        .Where(p => p.Maske != 0).ToArray();
            }
            k[(int)g] = kalender;
            h[(int)g] = kalender == null ? Kalenderherkunft.Keine : herkunft ?? Kalenderherkunft.Keine;
            return new Konditionierungsstand(Art, _bestand, _zellen, k, h, gemeinsam, _ferien, _wochen, _feriennamen);
        }

        // -----------------------------------------------------------------------------
        //  Der gemeinsame Kalender, die Ferienliste und die benannten Wochen (Schemaschritt KalenderbedienungSchema)
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Die Perioden des gemeinsamen Kalenders mit Angabe. Die Größenkalender (<see cref="Kalender"/>) tragen sie
        /// AUSGEBREITET — je Größe der Maske mit angelegtem Kalender als Periode am selben Rang, wie der Leser des Laufs
        /// (<see cref="Kalendergemeinschaft.Ausbreiten"/>); <see cref="EigenerKalender"/> zieht sie wieder ab.
        /// </summary>
        public IReadOnlyList<Gemeinschaftsperiode> Gemeinsam => _gemeinsam;

        /// <summary>Die Ferienzeiträume ab dem fünften (Ferienperioden des gemeinsamen Kalenders ab Rang 204, ohne Angabe).</summary>
        public IReadOnlyList<Ferienzeile> Ferienliste => _ferien;

        /// <summary>Die benannten Wochen des Eigentümers.</summary>
        public IReadOnlyList<BenannteWoche> Wochen => _wochen;

        /// <summary>
        /// <b>Der eigene Kalender einer Größe</b> — der angelegte ohne die ausgebreiteten Gemeinschaftsperioden (eine
        /// eigene Periode am Rang einer Gemeinschaftsperiode, die von ihr abweicht, bleibt: sie geht im Lauf vor).
        /// </summary>
        public Konditionierungskalender EigenerKalender(Konditionierungsgroesse g)
        {
            Konditionierungskalender k = _kalender[(int)g];
            if (k == null || _gemeinsam.Length == 0) return k;
            var eigene = k.Perioden.Where(r => !_gemeinsam.Any(p => p.Gilt(g) && p.Rang == r.Rang && Kalendervergleich.RegelGleich(p.Regel, r)))
                          .ToList();
            return eigene.Count == k.Perioden.Count ? k : new Konditionierungskalender(k.Groesse, k.Grundangabe, k.Nennwert, eigene);
        }

        /// <summary>
        /// <b>Derselbe Stand mit einem anderen gemeinsamen Kalender</b>: Jeder angelegte Größenkalender verliert die
        /// Kopien der bisherigen Gemeinschaftsperioden und bekommt die der neuen (wo der Rang frei ist).
        /// </summary>
        public Konditionierungsstand MitGemeinsam(IEnumerable<Gemeinschaftsperiode> gemeinsam)
        {
            Gemeinschaftsperiode[] neu = (gemeinsam ?? Enumerable.Empty<Gemeinschaftsperiode>()).Where(p => p != null && p.Maske != 0)
                                         .OrderByDescending(p => p.Rang).ToArray();
            var k = (Konditionierungskalender[])_kalender.Clone();
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                if (k[(int)g] != null) k[(int)g] = Ausgebreitet(EigenerKalender(g), k[(int)g], neu);
            return new Konditionierungsstand(Art, _bestand, _zellen, k, _herkunft, neu, _ferien, _wochen, _feriennamen);
        }

        /// <summary>
        /// <b>Derselbe Stand mit gelesenen Gemeinschaftsperioden</b> (Datenbank, Oberfläche): Die Größenkalender tragen sie
        /// schon ausgebreitet und bleiben unberührt; eine Größe der Maske, deren angelegter Kalender am Rang der Periode
        /// nichts trägt, verlässt die Maske (dort ist die Kopie entfernt worden).
        /// </summary>
        public Konditionierungsstand MitGemeinsamAbgeglichen(IEnumerable<Gemeinschaftsperiode> gemeinsam)
        {
            var neu = new List<Gemeinschaftsperiode>();
            foreach (Gemeinschaftsperiode p in gemeinsam ?? Enumerable.Empty<Gemeinschaftsperiode>())
            {
                if (p == null) continue;
                int maske = p.Maske;
                foreach (Konditionierungsgroesse g in Gemeinschaftsperiode.Groessen(p.Maske))
                    if (_kalender[(int)g] != null && !_kalender[(int)g].Perioden.Any(r => r.Rang == p.Rang))
                        maske &= ~Gemeinschaftsperiode.Bit(g);
                if (maske != 0) neu.Add(p.MitMaske(maske));
            }
            return new Konditionierungsstand(Art, _bestand, _zellen, _kalender, _herkunft,
                                             neu.OrderByDescending(p => p.Rang).ToArray(), _ferien, _wochen, _feriennamen);
        }

        /// <summary>
        /// Derselbe Stand mit einer anderen Ferienliste (ab dem fünften Zeitraum). Die Bestandsfelder tragen sie mit
        /// (<see cref="Matrixeingang.WeitereFerien"/>) — so liest der Generator („Matrix erneut") dieselbe Liste wie der Lauf.
        /// </summary>
        public Konditionierungsstand MitFerienliste(IEnumerable<Ferienzeile> ferien)
        {
            Ferienzeile[] liste = (ferien ?? Enumerable.Empty<Ferienzeile>()).ToArray();
            Matrixeingang bestand = _bestand;
            if (liste.Length == 0 && _bestand.WeitereFerien.Count > 0)
            {
                bestand = _bestand.Kopie();
                bestand.WeitereFerien = liste;
            }
            return new Konditionierungsstand(Art, bestand, _zellen, _kalender, _herkunft, _gemeinsam, liste, _wochen, _feriennamen);
        }

        /// <summary>
        /// <b>Die Namen der Ferienzeiträume 1 bis 4</b> (Spalten <c>Ferienbeginn/-ende_1…4</c>): je Zeitraum der Bezeichner
        /// seiner Spiegelperiode im gemeinsamen Kalender (Rang 200 … 203); <c>null</c> heißt „unbekannt" — der Schreibweg
        /// lässt den Bezeichner dann stehen. Die Namen wirken nicht auf die Rechnung.
        /// </summary>
        public IReadOnlyList<string> Feriennamen => _feriennamen;

        /// <summary>
        /// Derselbe Stand mit anderen Namen der Ferienzeiträume 1 bis 4 (fehlende Stellen und Leertexte heißen
        /// „unbekannt").
        /// </summary>
        public Konditionierungsstand MitFeriennamen(IEnumerable<string> namen)
        {
            var neu = new string[Matrixeingang.FERIENZEITRAEUME];
            int i = 0;
            foreach (string n in namen ?? Enumerable.Empty<string>())
            {
                if (i >= neu.Length) break;
                neu[i++] = string.IsNullOrWhiteSpace(n) ? null : n.Trim();
            }
            return new Konditionierungsstand(Art, _bestand, _zellen, _kalender, _herkunft, _gemeinsam, _ferien, _wochen, neu);
        }

        /// <summary>Derselbe Stand mit anderen benannten Wochen.</summary>
        public Konditionierungsstand MitWochen(IEnumerable<BenannteWoche> wochen)
            => new Konditionierungsstand(Art, _bestand, _zellen, _kalender, _herkunft, _gemeinsam, _ferien,
                                         (wochen ?? Enumerable.Empty<BenannteWoche>()).ToArray(), _feriennamen);

        /// <summary>Der eigene Kalender <paramref name="eigen"/> samt den Kopien der Gemeinschaftsperioden seiner Größe.</summary>
        private static Konditionierungskalender Ausgebreitet(Konditionierungskalender eigen, Konditionierungskalender form,
                                                             IReadOnlyList<Gemeinschaftsperiode> gemeinsam)
        {
            var perioden = eigen.Perioden.ToList();
            bool zu = false;
            foreach (Gemeinschaftsperiode p in gemeinsam)
                if (p.Gilt(form.Groesse) && !perioden.Any(r => r.Rang == p.Rang)) { perioden.Add(p.Regel); zu = true; }
            if (!zu && ReferenceEquals(eigen, form)) return form;
            return new Konditionierungskalender(form.Groesse, form.Grundangabe, form.Nennwert,
                                                perioden.OrderByDescending(r => r.Rang).ToList());
        }

        /// <summary>Derselbe Inhalt mit anderer Eigentümerart (etwa ein Katalogbau als Gebäudeebene).</summary>
        public Konditionierungsstand AlsArt(Kalendereigentuemer art)
            => new Konditionierungsstand(art, _bestand, _zellen, _kalender, _herkunft, _gemeinsam, _ferien, _wochen, _feriennamen);

        // -----------------------------------------------------------------------------
        //  Vergleich
        // -----------------------------------------------------------------------------

        /// <summary>
        /// <b>Tragen beide dieselbe Konditionierung</b> — Vorgabezellen, Kalender (bitgleich) und
        /// Herkunft; mit <paramref name="mitBestand"/> auch die Bestandsfelder der Matrix.
        /// </summary>
        public bool Gleich(Konditionierungsstand andere, bool mitBestand = true)
        {
            if (andere == null) return false;
            for (int i = 0; i < _zellen.Length; i++)
                if (!Kalendervergleich.ZelleGleich(_zellen[i], andere._zellen[i])) return false;
            for (int g = 0; g < 5; g++)
            {
                if (!Kalendervergleich.KalenderGleich(_kalender[g], andere._kalender[g])) return false;
                if (_kalender[g] != null && !_herkunft[g].Equals(andere._herkunft[g])) return false;
            }
            if (_gemeinsam.Length != andere._gemeinsam.Length || _ferien.Length != andere._ferien.Length
                || _wochen.Length != andere._wochen.Length) return false;
            for (int i = 0; i < _gemeinsam.Length; i++)
                if (_gemeinsam[i].Maske != andere._gemeinsam[i].Maske
                    || !Kalendervergleich.RegelGleich(_gemeinsam[i].Regel, andere._gemeinsam[i].Regel)) return false;
            for (int i = 0; i < _ferien.Length; i++)
                if (_ferien[i] != andere._ferien[i]) return false;
            for (int i = 0; i < _wochen.Length; i++)
                if (!_wochen[i].Gleich(andere._wochen[i])) return false;
            // Ein unbekannter Name (null) gleicht jedem — er wird nicht geschrieben.
            for (int i = 0; i < _feriennamen.Length && i < andere._feriennamen.Length; i++)
                if (_feriennamen[i] != null && andere._feriennamen[i] != null
                    && !string.Equals(_feriennamen[i], andere._feriennamen[i], StringComparison.Ordinal)) return false;
            return !mitBestand || Kalendervergleich.BestandGleich(_bestand, andere._bestand);
        }

        /// <summary>Trägt die Zelle irgendetwas — Wert, „aus", Zeiten oder ΔT?</summary>
        public static bool Traegt(Matrixzelle z)
            => z != null && (z.Belegt || z.Von.HasValue || z.Bis.HasValue || z.BedingtK.HasValue);

        /// <summary>Die Matrixzelle einer Vorgabezeile — dieselbe Abbildung wie <see cref="Vorgabematrix.Bilden"/>.</summary>
        public static Matrixzelle Zelle(Vorgabezeile v)
        {
            if (v == null) return Matrixzelle.Leer;
            return v.Aus
                ? Matrixzelle.Abgeschaltet(v.Von, v.Bis, v.BedingtK)
                : v.Wert.HasValue
                    ? Matrixzelle.AusWert(v.Wert.Value, v.Von, v.Bis, v.BedingtK)
                    : Matrixzelle.NurZeiten(v.Von, v.Bis, v.BedingtK);
        }

        /// <summary>Der Index eines Zeilenkennworts in <see cref="DbWerte.KOND_ZEILEN"/>; −1 = keines.</summary>
        public static int Zeilenindex(string kennwort)
        {
            for (int i = 0; i < DbWerte.KOND_ZEILEN.Count; i++)
                if (string.Equals(kennwort, DbWerte.KOND_ZEILEN[i], StringComparison.Ordinal)) return i;
            return -1;
        }

        private static int Index(Konditionierungsgroesse g, int zeile) => (int)g * ZEILEN + zeile;

        /// <summary>Sprachunabhängige Kurzfassung.</summary>
        public override string ToString()
            => Art + ": " + VorgabenAnzahl.ToString(CultureInfo.InvariantCulture) + " Vorgabezelle(n), " +
               KalenderAnzahl.ToString(CultureInfo.InvariantCulture) + " Kalender";
    }

    /// <summary>
    /// <b>Eine Zone im Arbeitsstand</b> — ihre Id (≤ 0 = vorläufig), ihr Name für die Rückfragen,
    /// Nutzfläche (Flächenschlüssel der inneren Gewinne), „beheizt" und ihre Ebene. Der
    /// <see cref="Konditionierungsstand.Bestand"/> einer Zone trägt nur ihre EIGENEN Werte; leer heißt
    /// „wie das Gebäude" (Vorgabenkaskade, <see cref="Zonenvorgaben"/>).
    /// </summary>
    public sealed class Konditionierungszone
    {
        /// <summary>Baut die Zone.</summary>
        public Konditionierungszone(long id, string name, double? nutzflaeche, bool istBeheizt, Konditionierungsstand stand)
        {
            Id = id;
            Name = name ?? "";
            Nutzflaeche = nutzflaeche;
            IstBeheizt = istBeheizt;
            Stand = stand ?? Konditionierungsstand.Leer(Kalendereigentuemer.Zone, null);
        }

        /// <summary>Die Id der Zone (≤ 0 = vorläufig).</summary>
        public long Id { get; }

        /// <summary>Der Name — die Rückfragen nennen Zonen mit Namen.</summary>
        public string Name { get; }

        /// <summary>Die eigene Nutzfläche [m²]; <c>null</c> = die des Gebäudes.</summary>
        public double? Nutzflaeche { get; }

        /// <summary>Wird die Zone beheizt? Unbeheizte Zonen haben weder Heiz- noch Kühlkalender (N1.56 Festlegung 2).</summary>
        public bool IstBeheizt { get; }

        /// <summary>Die Ebene der Zone.</summary>
        public Konditionierungsstand Stand { get; }

        /// <summary>Dieselbe Zone mit einer anderen Ebene.</summary>
        public Konditionierungszone MitStand(Konditionierungsstand stand)
            => new Konditionierungszone(Id, Name, Nutzflaeche, IstBeheizt, stand);
    }

    /// <summary>Wo ein Schritt wirkt: die Größe und das Gebäude bzw. der Katalogbau (<c>null</c>) oder eine Zone.</summary>
    /// <param name="Groesse">Die Größe.</param>
    /// <param name="Zone">Die Id der Zone im Arbeitsstand; <c>null</c> = Gebäude bzw. Katalogbau.</param>
    public sealed record Konditionierungsort(Konditionierungsgroesse Groesse, long? Zone = null);

    /// <summary>
    /// <b>Der Arbeitsstand eines Gebäudes samt Zonen</b> (bzw. eines Katalogbaus ohne Zonen) — dazu
    /// Nutzfläche (Flächenschlüssel) und das Wochentagsraster, gegen das die Jahresmittel der Lasten
    /// gerechnet werden (P1). Unveränderlich; jede Änderung liefert einen neuen.
    /// </summary>
    public sealed class Konditionierungsarbeitsstand
    {
        private readonly Konditionierungszone[] _zonen;

        /// <summary>Baut den Arbeitsstand.</summary>
        /// <param name="gebaeude">Die Ebene des Gebäudes bzw. Katalogbaus.</param>
        /// <param name="zonen">Die Zonen in Listenfolge; <c>null</c> = keine.</param>
        /// <param name="nutzflaeche">Die Nutzfläche des Gebäudes [m²] für den Flächenschlüssel; <c>null</c> = 0.</param>
        /// <param name="raster">Das Wochentagsraster des Laufs (E115, <see cref="Konditionierungdatenweg.Raster(int)"/>):
        /// w₀ der Klimaregion und, nur mit Preisreihe, deren Jahr; <c>null</c> = ohne Projekt
        /// <see cref="Konditionierungdatenweg.Rueckfallraster"/>.</param>
        public Konditionierungsarbeitsstand(Konditionierungsstand gebaeude, IEnumerable<Konditionierungszone> zonen,
                                            double? nutzflaeche = null, Gemeinjahrkalender? raster = null)
        {
            Gebaeude = gebaeude ?? Konditionierungsstand.Leer(Kalendereigentuemer.Gebaeude, null);
            _zonen = zonen == null ? Array.Empty<Konditionierungszone>() : new List<Konditionierungszone>(zonen).ToArray();
            Nutzflaeche = nutzflaeche;
            Kalender = raster ?? Konditionierungdatenweg.Rueckfallraster;
        }

        /// <summary>Das Jahr der Preisreihe; <c>null</c> = Regelfall ohne Jahr.</summary>
        public int? Feiertagsjahr => Kalender.MitJahr ? Kalender.Jahr : null;

        /// <summary>Das Wochentagsraster (E115): w₀ und, nur mit Preisreihe, deren Jahr für die Feiertagslage.</summary>
        public Gemeinjahrkalender Kalender { get; }

        /// <summary>Die Ebene des Gebäudes bzw. Katalogbaus.</summary>
        public Konditionierungsstand Gebaeude { get; }

        /// <summary>Die Zonen in Listenfolge.</summary>
        public IReadOnlyList<Konditionierungszone> Zonen => _zonen;

        /// <summary>Die Nutzfläche des Gebäudes [m²] — der Nenner des Flächenschlüssels.</summary>
        public double? Nutzflaeche { get; }

        /// <summary>w₀ des Rasters: 0 = Montag … 6 = Sonntag für den 1. Januar.</summary>
        public int W0 => Kalender.W0;

        /// <summary>Die Zone mit dieser Id; <c>null</c> = keine.</summary>
        public Konditionierungszone Zone(long id)
        {
            foreach (Konditionierungszone z in _zonen) if (z.Id == id) return z;
            return null;
        }

        /// <summary>Die Ebene eines Orts: das Gebäude (<c>null</c>) oder die Zone; <c>null</c>, wenn es die Zone nicht gibt.</summary>
        public Konditionierungsstand Ebene(long? zone) => zone.HasValue ? Zone(zone.Value)?.Stand : Gebaeude;

        /// <summary>Derselbe Arbeitsstand mit einer anderen Ebene des Gebäudes.</summary>
        public Konditionierungsarbeitsstand MitGebaeude(Konditionierungsstand gebaeude)
            => new Konditionierungsarbeitsstand(gebaeude, _zonen, Nutzflaeche, Kalender);

        /// <summary>Derselbe Arbeitsstand mit einer anderen Ebene am Ort <paramref name="zone"/> (<c>null</c> = Gebäude).</summary>
        /// <exception cref="ArgumentException">Die Zone gibt es nicht.</exception>
        public Konditionierungsarbeitsstand MitEbene(long? zone, Konditionierungsstand stand)
        {
            if (!zone.HasValue) return MitGebaeude(stand);
            var liste = new List<Konditionierungszone>(_zonen.Length);
            bool gefunden = false;
            foreach (Konditionierungszone z in _zonen)
            {
                if (z.Id == zone.Value) { liste.Add(z.MitStand(stand)); gefunden = true; }
                else liste.Add(z);
            }
            if (!gefunden) throw new ArgumentException("Die Zone " + zone.Value.ToString(CultureInfo.InvariantCulture) +
                                                       " steht nicht im Arbeitsstand.", nameof(zone));
            return new Konditionierungsarbeitsstand(Gebaeude, liste, Nutzflaeche, Kalender);
        }

        /// <summary>
        /// <b>Die aufgelösten Bestandsfelder einer Zone</b> — die Vorgabenkaskade des Laufs
        /// (<see cref="Zonenvorgaben"/>): Sollwerte, Maximalraumtemperatur und Lüftung „Zone, sonst
        /// Gebäude", innere Gewinne und Bewohner „Zone, sonst Gebäude × Flächenanteil"; Nachtzeit,
        /// Ferienzeiträume, Merker, Kühlung und Gesamtangabe kommen vom Gebäude. Dieselben Werte, mit
        /// denen der Lauf die Zone rechnet.
        /// </summary>
        public Matrixeingang AufgeloesterBestand(Konditionierungszone zone) => AufgeloesterBestand(zone, out _);

        /// <summary>
        /// Der Flächenanteil einer Zone — A_Zone / A_Gebäude aus der Vorgabenkaskade
        /// (<see cref="Zonenvorgaben.Flaechenanteil"/>); 1 ohne eigene Nutzfläche.
        /// </summary>
        public double Flaechenanteil(Konditionierungszone zone)
        {
            AufgeloesterBestand(zone, out double anteil);
            return anteil;
        }

        /// <summary>
        /// Die Nennwerte einer Zone (Konzept 3.4; Stufe KP2, Welle U4): ihr eigener Geräte- und
        /// Personen-Nennwert und ihr Flächenanteil — was ein Anteilskalender an ihr multipliziert.
        /// </summary>
        public Zonennennwerte Nennwerte(Konditionierungszone zone)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            return Zonennennwerte.Aus(Flaechenanteil(zone), zone.Stand.Bestand, zone.Stand.Vorgabezeilen());
        }

        private Matrixeingang AufgeloesterBestand(Konditionierungszone zone, out double flaechenanteil)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            Matrixeingang g = Gebaeude.Bestand;
            Matrixeingang z = zone.Stand.Bestand;
            var gebaeude = new Gebaeudevorgaben(
                Nutzflaeche ?? 0.0, 0.0,
                g.SollTag ?? 0.0, g.SollNacht ?? 0.0, g.SollWochenende ?? 0.0, g.SollFerien ?? 0.0,
                g.Maximaleraumtemperatur ?? 0.0, null, null,
                g.Luftwechselrate ?? 0.0, g.LuftwechselInfiltration, g.LuftwechselNutzer,
                g.InterneWaermegewinne ?? 0.0, g.Bewohner ?? 0.0,
                g.KuehlungWirksam, g.KuehlSollwert, g.KuehlSollwertNacht, null);
            var eingaben = new Zoneneingaben(
                Nutzflaeche: zone.Nutzflaeche, IstBeheizt: zone.IstBeheizt,
                SollTag: z.SollTag, SollNacht: z.SollNacht, SollWochenende: z.SollWochenende, SollFerien: z.SollFerien,
                Maximaleraumtemperatur: z.Maximaleraumtemperatur,
                LuftwechselInfiltration: z.LuftwechselInfiltration, LuftwechselNutzer: z.LuftwechselNutzer,
                InterneWaermegewinne: z.InterneWaermegewinne, Bewohner: z.Bewohner,
                KuehlSollwert: z.KuehlSollwert, KuehlSollwertNacht: z.KuehlSollwertNacht);
            Zonenvorgaben v = Zonenvorgaben.Bilden(eingaben, gebaeude, _zonen.Length);
            flaechenanteil = v.Flaechenanteil;
            return Konditionierungseingang.ZonenBestand(g, eingaben, v);
        }

        /// <summary>
        /// <b>Die wirksame Matrix eines Orts</b> — am Gebäude seine eigene, an einer Zone die
        /// Kaskade wie im Lauf (<see cref="Konditionierungdatenweg"/>): die Zeilen der Zone über denen
        /// des Gebäudes, beide über den aufgelösten Bestandsfeldern der Zone, der Personen-Nennwert als
        /// Nennwert der Zone (<see cref="Konditionierungseingang.Zonenmatrix"/>, Konzept 3.4).
        /// </summary>
        /// <exception cref="ArgumentException">Die Zone gibt es nicht.</exception>
        public Vorgabematrix Matrix(long? zone)
        {
            if (!zone.HasValue) return Gebaeude.Matrix();
            Konditionierungszone z = Zone(zone.Value)
                ?? throw new ArgumentException("Die Zone " + zone.Value.ToString(CultureInfo.InvariantCulture) +
                                               " steht nicht im Arbeitsstand.", nameof(zone));
            Matrixeingang b = AufgeloesterBestand(z, out double anteil);
            Vorgabematrix gebaeudematrix = Vorgabematrix.Bilden(b, Gebaeude.Vorgabezeilen());
            return Konditionierungseingang.Zonenmatrix(b, z.Stand.Vorgabezeilen(), gebaeudematrix, anteil);
        }

        /// <summary>
        /// <b>Die Matrix, die eine Zone vom Gebäude erbt</b> (Konzept 3.4, 7.3; Stufe KP2, Welle U4) — die
        /// wirksame Matrix der Zone ohne ihre eigenen Zellen und Bestandswerte, mit ihrer Nutzfläche (der
        /// Flächenanteil schlüsselt innere Gewinne, Bewohner und den Personen-Nennwert). Aus ihr stehen die
        /// Platzhalter „Vorgabe …" der Zonenmatrix: Was eine leere Zelle der Zone gerade gälte.
        /// </summary>
        /// <exception cref="ArgumentException">Die Zone gibt es nicht.</exception>
        public Vorgabematrix Erbmatrix(long zone)
        {
            if (Zone(zone) == null)
                throw new ArgumentException("Die Zone " + zone.ToString(CultureInfo.InvariantCulture) +
                                            " steht nicht im Arbeitsstand.", nameof(zone));
            return MitEbene(zone, Konditionierungsstand.Leer(Kalendereigentuemer.Zone, null)).Matrix(zone);
        }

        /// <summary>
        /// <b>Der Kalender, der an einem Ort gilt</b> — die erste Quelle der Kette (Konzept 3.4):
        /// angelegt an der Zone, angelegt am Gebäude, sonst abgeleitet aus der wirksamen Matrix, wo sie
        /// eine Angabe trägt. Der Kalender des Gebäudes trägt an einer Zone ihren Nennwert
        /// (<see cref="Konditionierungseingang.ErsteQuelleDerZone"/>). <c>null</c> heißt: der Bestandszweig.
        /// </summary>
        public Konditionierungskalender GeltenderKalender(Konditionierungsgroesse g, long? zone)
        {
            Konditionierungszone kz = zone.HasValue ? Zone(zone.Value) : null;
            Konditionierungsstand z = kz?.Stand;
            if (kz == null)
                return Konditionierungseingang.ErsteQuelle(g, Matrix(zone), null, Gebaeude.Angelegt(), false, out _);
            return Konditionierungseingang.ErsteQuelleDerZone(g, Matrix(zone), z.Angelegt(), Gebaeude.Angelegt(),
                                                              Konditionierungseingang.EigeneNachtzeile(z.Vorgabezeilen()),
                                                              Nennwerte(kz), out _);
        }

        /// <summary>
        /// <b>Der Kalender, den der Dialog für die Größe zeigt</b>: der angelegte der Zone, sonst der des
        /// Gebäudes, sonst der Generator aus der wirksamen Matrix — auch dort, wo der Lauf im
        /// Bestandszweig rechnet (der Generator ist ihm bitgleich). <c>null</c>, wenn die Matrix keinen
        /// Kalender ergibt (etwa Personen ohne Anteil).
        /// </summary>
        public Konditionierungskalender Ansichtskalender(Konditionierungsgroesse g, long? zone)
        {
            Konditionierungskalender k = GeltenderKalender(g, zone);
            if (k != null) return k;
            Fahrplanlesung l = Standardfahrplan.Erzeugen(Matrix(zone), g, rundlaufPruefen: false);
            return l.Befund == Fahrplanbefund.Erzeugt ? l.Kalender : null;
        }

        /// <summary>Sprachunabhängige Kurzfassung.</summary>
        public override string ToString()
            => Gebaeude + " · " + _zonen.Length.ToString(CultureInfo.InvariantCulture) + " Zone(n), " + Kalender;
    }

    /// <summary>
    /// <b>Eine Vorlage als Eingabe</b> des Schritts „Vorlage übernehmen" (P11, P12): Name, Größe und
    /// ihr Inhalt als Ebene der Art <see cref="Kalendereigentuemer.Vorlage"/>.
    /// </summary>
    /// <param name="Id">Die Id der Vorlage (nur zur Nennung).</param>
    /// <param name="Name">Der Name — er wird Herkunft („aus Vorlage …").</param>
    /// <param name="Groesse">Die eine Größe der Vorlage.</param>
    /// <param name="Inhalt">Vorgabezellen und höchstens ein Kalender der Größe.</param>
    /// <param name="AusProfil">
    /// Die Vorlage hat der Generator aus einem Nutzungsprofil erzeugt (NP3c): Die Herkunft des übernommenen Kalenders
    /// nennt dann das Profil („aus Nutzungsprofil …", <see cref="Kalenderherkunft.IstProfil"/>), nicht eine Vorlage.
    /// </param>
    public sealed record Konditionierungsvorlage(long Id, string Name, Konditionierungsgroesse Groesse,
                                                 Konditionierungsstand Inhalt, bool AusProfil = false);

    /// <summary>Was ein Rückfragebefund zählt — dieselbe Reihenfolge wie <c>KonditionierungPostenart</c> der Oberfläche.</summary>
    public enum Konditionierungspostenart
    {
        /// <summary>Belegte Zellen der Matrix.</summary>
        Matrixzellen = 0,

        /// <summary>Angelegte Kalender.</summary>
        Kalender = 1,

        /// <summary>Grundangabe bzw. Standardwoche eines Kalenders.</summary>
        Standardwoche = 2,

        /// <summary>Perioden der Art Ferien (Matrixbereich).</summary>
        Ferienperioden = 3,

        /// <summary>Perioden der Art Betriebspause — die Saison (Matrixbereich).</summary>
        Saisonperioden = 4,

        /// <summary>Eigene Perioden der Art Zeitraum (Ausnahmetage).</summary>
        EigenePerioden = 5,

        /// <summary>Feiertagsregeln.</summary>
        Feiertage = 6,

        /// <summary>Die Nachtzeiten des Gebäudes.</summary>
        Nachtzeiten = 7,

        /// <summary>Die Ferienzeiträume des Gebäudes.</summary>
        Ferienzeitraeume = 8,

        /// <summary>Die Gesamtangabe des Luftwechsels, aufgeteilt in Infiltration und Nutzerlüftung (F5).</summary>
        Luftwechsel = 9,

        /// <summary>Kalender und Zellen der Zonen, die ein Schritt mit anlegt oder zurücklässt.</summary>
        Zonenkalender = 10,

        /// <summary>Bauteile der Zonen („Speichern unter" im Projekt).</summary>
        Bauteile = 11,
    }

    /// <summary>Ein Posten eines Befunds: was und wie viel.</summary>
    public sealed record Konditionierungsposten(Konditionierungspostenart Art, int Anzahl);

    /// <summary>
    /// <b>Die Handlungen mit Rückfrage</b> (Entwurf KP2, Festlegung 3): Sie ersetzen etwas, das der
    /// Anwender von Hand angelegt haben kann; der Befund entsteht VOR dem Schreiben
    /// (<see cref="Konditionierungsarbeit.Rueckfrage"/>). „Speichern unter" im Projekt fragt über
    /// <see cref="Konditionierungsarbeit.RueckfrageSpeichernUnter"/>.
    /// </summary>
    public enum Konditionierungshandlung
    {
        /// <summary>„Matrix erneut anwenden…" (P12).</summary>
        MatrixErneut = 0,

        /// <summary>„Übernehmen" einer Vorlage (P11, P12).</summary>
        VorlageUebernehmen = 1,

        /// <summary>„Verwerfen" — zurück zur Matrix.</summary>
        Verwerfen = 2,

        /// <summary>„Aus dem Katalog erneut übernehmen…" (Festlegung 4).</summary>
        KatalogErneut = 3,
    }

    /// <summary>
    /// <b>Was ein Schritt ersetzt und was bleibt</b> (Entwurf KP2, Festlegung 3) — die Zahlen je
    /// Postenart und die Namen der betroffenen Zonen. Dieselbe Bilanz dient als Rückfragebefund VOR
    /// dem Schreiben und als Befund NACH dem Schritt.
    /// </summary>
    public sealed class Konditionierungsbilanz
    {
        /// <summary>Die leere Bilanz.</summary>
        public static Konditionierungsbilanz Keine { get; } =
            new Konditionierungsbilanz(Array.Empty<Konditionierungsposten>(), Array.Empty<Konditionierungsposten>(),
                                       Array.Empty<string>());

        /// <summary>Baut die Bilanz; Posten mit Anzahl 0 fallen weg.</summary>
        public Konditionierungsbilanz(IEnumerable<Konditionierungsposten> ersetzt, IEnumerable<Konditionierungsposten> bleibt,
                                      IEnumerable<string> zonen)
        {
            Ersetzt = Ohne0(ersetzt);
            Bleibt = Ohne0(bleibt);
            Zonen = zonen == null ? Array.Empty<string>() : new List<string>(zonen).ToArray();
        }

        /// <summary>Was ersetzt wird.</summary>
        public IReadOnlyList<Konditionierungsposten> Ersetzt { get; }

        /// <summary>Was stehen bleibt.</summary>
        public IReadOnlyList<Konditionierungsposten> Bleibt { get; }

        /// <summary>Die Namen der betroffenen Zonen.</summary>
        public IReadOnlyList<string> Zonen { get; }

        /// <summary>Nennt die Bilanz nichts?</summary>
        public bool IstLeer => Ersetzt.Count == 0 && Bleibt.Count == 0 && Zonen.Count == 0;

        /// <summary>Die Anzahl eines Postens unter „ersetzt"; 0 = keiner.</summary>
        public int ErsetztAnzahl(Konditionierungspostenart art) => Anzahl(Ersetzt, art);

        /// <summary>Die Anzahl eines Postens unter „bleibt"; 0 = keiner.</summary>
        public int BleibtAnzahl(Konditionierungspostenart art) => Anzahl(Bleibt, art);

        private static int Anzahl(IReadOnlyList<Konditionierungsposten> liste, Konditionierungspostenart art)
        {
            int n = 0;
            foreach (Konditionierungsposten p in liste) if (p.Art == art) n += p.Anzahl;
            return n;
        }

        private static Konditionierungsposten[] Ohne0(IEnumerable<Konditionierungsposten> posten)
        {
            var liste = new List<Konditionierungsposten>();
            if (posten != null)
                foreach (Konditionierungsposten p in posten)
                    if (p != null && p.Anzahl > 0) liste.Add(p);
            return liste.ToArray();
        }

        /// <summary>Sprachunabhängige Kurzfassung für Protokoll und Probe.</summary>
        public override string ToString()
        {
            var sb = new StringBuilder("ersetzt:");
            foreach (Konditionierungsposten p in Ersetzt) sb.Append(' ').Append(p.Art).Append('=').Append(p.Anzahl);
            sb.Append(" · bleibt:");
            foreach (Konditionierungsposten p in Bleibt) sb.Append(' ').Append(p.Art).Append('=').Append(p.Anzahl);
            if (Zonen.Count > 0) sb.Append(" · Zonen: ").Append(string.Join(", ", Zonen));
            return sb.ToString();
        }
    }

    /// <summary>
    /// <b>Was ein Schritt des Arbeitsstands ergeben hat</b> — der NEUE Stand samt Bilanz, eine benannte
    /// Ablehnung (<see cref="Stand"/> <c>null</c>, nichts geändert) oder eine Rückfrage, ohne die der
    /// Schritt nicht weitergeht (etwa „aufteilen" nach F5).
    /// </summary>
    public sealed class Konditionierungsschritt
    {
        private Konditionierungsschritt(bool ok, string meldung, Konditionierungsarbeitsstand stand,
                                        Konditionierungsbilanz bilanz, bool rueckfrage)
        {
            Ok = ok;
            Meldung = meldung ?? "";
            Stand = stand;
            Bilanz = bilanz ?? Konditionierungsbilanz.Keine;
            Rueckfrage = rueckfrage;
        }

        /// <summary>Hat der Schritt gegriffen?</summary>
        public bool Ok { get; }

        /// <summary>Die benannte Ablehnung; leer im guten Fall und bei einer Rückfrage.</summary>
        public string Meldung { get; }

        /// <summary>Der neue Stand; <c>null</c> bei Ablehnung und Rückfrage.</summary>
        public Konditionierungsarbeitsstand Stand { get; }

        /// <summary>Was ersetzt wurde und was blieb — bei einer Rückfrage: was die Antwort „Ja" täte.</summary>
        public Konditionierungsbilanz Bilanz { get; }

        /// <summary>Verlangt der Schritt vorher eine Rückfrage (<see cref="Bilanz"/> nennt sie)?</summary>
        public bool Rueckfrage { get; }

        /// <summary>Der gute Fall.</summary>
        public static Konditionierungsschritt Gut(Konditionierungsarbeitsstand stand, Konditionierungsbilanz bilanz = null)
            => new Konditionierungsschritt(true, "", stand ?? throw new ArgumentNullException(nameof(stand)), bilanz, false);

        /// <summary>Die benannte Ablehnung — nichts geändert.</summary>
        public static Konditionierungsschritt Fehler(string meldung)
            => new Konditionierungsschritt(false, meldung, null, null, false);

        /// <summary>Die Rückfrage — nichts geändert; <paramref name="bilanz"/> nennt, was die Antwort täte.</summary>
        public static Konditionierungsschritt Frage(Konditionierungsbilanz bilanz)
            => new Konditionierungsschritt(false, "", null, bilanz, true);

        /// <summary>Sprachunabhängige Kurzfassung.</summary>
        public override string ToString()
            => Ok ? "gut · " + Bilanz : Rueckfrage ? "Rückfrage · " + Bilanz : "abgelehnt: " + Meldung;
    }
}
