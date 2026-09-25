using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Katalogimport der Bedarfstage und Parameter</b> (Umsetzungskonzept
    /// Zapfprofilgenerator, Kapitel 9 Zeilen ZU30 bis ZU33; N13 Folge (s), N18 (c)): der zweite
    /// Teil von <see cref="TwwNutzungsartCtrl"/>s Katalogimport — <c>Tab_TwwBedarfstag_STAMM</c>
    /// samt <c>Tab_TwwBedarfstagEreignis_STAMM</c> und <c>Tab_TwwParameter_STAMM</c>.
    ///
    /// <para><b>Die Regel ist ERSETZEN, nicht Versionsbildung</b> (Anwenderentscheide 25.09.2026,
    /// ZU30 und ZU31). Ein Bedarfstag ist über <c>Bezeichner</c> und <c>Katalogversion</c>
    /// bestimmt, ein Parameter über <c>Schluessel</c> und <c>Katalogversion</c>. Führt der Katalog
    /// die Zeile schon:
    /// <list type="bullet">
    /// <item>gleicher Inhalt — übersprungen (<c>KATALOGIMPORT_GLEICH_VORHANDEN</c>);</item>
    /// <item>abweichender Inhalt — die vorhandene Zeile trägt danach die Werte des Pakets, <b>am
    /// Platz und mit derselben <c>ID</c></b>, damit ein Projekt, das den Bedarfstag gewählt hat,
    /// weiter darauf zeigt. Auch eine Zeile der Auslieferung. Die Zeile ist danach eine
    /// Anwenderzeile: <c>Status = 'IMPORT'</c>, <c>ReadOnly = 0</c>, ohne <c>Beleg</c>, mit der
    /// Provenienz des Pakets (Herkunftsart <c>IMPORT</c>, <c>FREI</c> und <c>FIKTIV</c> bleiben —
    /// <see cref="TwwNutzungsartCtrl.ImportHerkunft"/>). Die Ereignisse eines Bedarfstags werden
    /// vollständig ersetzt.</item>
    /// </list>
    /// Beides steht im Bericht (<c>KATALOGIMPORT_BEDARFSTAG_ERSETZT</c>,
    /// <c>KATALOGIMPORT_PARAMETER_ERSETZT</c>), dazu zwei Hinweise: die Wirkung ersetzter Parameter
    /// auf jede künftige Auslegung (<c>KATALOGIMPORT_PARAMETER_WIRKUNG</c>) und die betroffenen
    /// Auslieferungszeilen (<c>KATALOGIMPORT_AUSLIEFERUNG_ERSETZT</c>).</para>
    ///
    /// <para><b>Warum die gelieferte Fassung nicht zurückkommt.</b> Eine ersetzte Zeile trägt den
    /// Stand <c>IMPORT</c>. Ein Programmupdate legt die Datenbank des Anwenders nicht neu an
    /// (<c>Erstbereitstellung</c> kopiert die Vorlage nur, wenn keine Datei liegt), und die
    /// Auslieferungsvorlage nimmt in eine neue Vorlage allein auf, was <c>Status = 'AUSLIEFERUNG'</c>
    /// trägt (<c>Werkzeuge/Auslieferungsvorlage/TwwKataloge.Bereinigen</c> löscht jede andere
    /// Zeile). Die gelieferte Fassung kommt deshalb nur mit einer neuen Installation oder einem
    /// Katalogpaket zurück, das sie führt — genau das sagt der Hinweis.</para>
    ///
    /// <para><b>Prüfung (ZU33).</b> Bedarfstag: <c>Quelle_Art</c> aus {2, 3, 4, 5} (die 1 ist das
    /// Stundenprofil der Zonen und steht in keinem Katalog), <c>Bezugsmenge</c> leer oder positiv,
    /// <c>Bezugsart</c> 1 bis 7 oder leer, mindestens ein Ereignis, jedes Ereignis im Tagesfenster
    /// (<c>Minute_Beginn</c> 0 bis 1439, <c>Dauer_min</c> ab 1, Summe höchstens 1440),
    /// <c>Energie_Kwh</c> nicht negativ und in der Summe positiv, <c>Reihenfolge</c> lückenlos ab 1.
    /// Ein Fehler lehnt NUR diesen Bedarfstag ab — wie bei den Nutzungsarten. Ein Ereignis, dessen
    /// <c>ID_Bedarfstag</c> auf keinen Bedarfstag des Pakets zeigt, ist ein Formfehler und lehnt das
    /// Paket als Ganzes ab: Es gibt keine Zeile, der er zufallen könnte.</para>
    ///
    /// <para><b>Parameter (ZU31).</b> Ein Schlüssel, den kein Rechenweg liest, wird benannt
    /// abgelehnt (<c>KATALOGIMPORT_PARAMETER_UNBEKANNT</c>) — die Liste der gelesenen Schlüssel
    /// steht in <see cref="TwwParameterkatalog"/>. Ebenso eine abweichende Einheit
    /// (<c>KATALOGIMPORT_PARAMETER_EINHEIT</c>) und ein Wert außerhalb des Bereichs
    /// (<c>KATALOGIMPORT_PARAMETER_BEREICH</c>).</para>
    /// </summary>
    internal static partial class TwwNutzungsartCtrl
    {
        // =================================================================================
        // Das Paket lesen: Bedarfstage samt Ereignissen
        // =================================================================================

        /// <summary>Ein Bedarfstag des Pakets samt Ereignissen und — wenn er nicht taugt — dem Grund.</summary>
        private sealed class PaketBedarfstag
        {
            /// <summary>Die <c>ID</c> des Pakets — nur Schlüssel für die Ereignisse, nie die der Datenbank.</summary>
            internal long Id { get; set; }

            internal int Zeile { get; set; }
            internal string Bezeichner { get; set; } = "";
            internal string Katalogversion { get; set; } = "";
            internal ZapfBedarfstagquelle QuelleArt { get; set; }
            internal double? Bezugsmenge { get; set; }
            internal int? Bezugsart { get; set; }
            internal Provenienz Herkunft { get; set; }

            /// <summary>Die Ereignisse in der Reihenfolge der Spalte <c>Reihenfolge</c>.</summary>
            internal List<Zapfereignis> Ereignisse { get; } = new List<Zapfereignis>();

            /// <summary>Die gelesenen Werte der Spalte <c>Reihenfolge</c>, in Dateireihenfolge.</summary>
            internal List<long> Reihenfolgen { get; } = new List<long>();

            internal ZapfSatz Fehler { get; set; }
        }

        /// <summary>Eine Parameterzeile des Pakets und — wenn sie nicht taugt — der Grund.</summary>
        private sealed class PaketParameterzeile
        {
            internal int Zeile { get; set; }
            internal string Schluessel { get; set; } = "";
            internal string Katalogversion { get; set; } = "";
            internal double Wert { get; set; }
            internal string Einheit { get; set; }
            internal Provenienz Herkunft { get; set; }
            internal ZapfSatz Fehler { get; set; }
        }

        /// <summary>
        /// Die Bedarfstage des Pakets samt Ereignissen. Ohne die Kopfdatei bleibt die Liste leer —
        /// liegen dann Ereignisse, ist das Paket abgelehnt (sie hängen an nichts).
        /// </summary>
        private static List<PaketBedarfstag> Bedarfstage(Dictionary<string, PaketTabelle> tabellen, Paket paket,
                                                         TwwKatalogimportBericht bericht)
        {
            var liste = new List<PaketBedarfstag>();
            tabellen.TryGetValue(TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM, out PaketTabelle te);
            if (!tabellen.TryGetValue(TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, out PaketTabelle tb))
            {
                if (te != null && te.Zeilen.Count > 0)
                    throw new PaketFehler(ZapfSatz.Neu("KATALOGIMPORT_KEINE_DATEI",
                                                       TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + ".csv"));
                return liste;
            }

            Pflicht(tb, "ID", "Bezeichner", "Katalogversion", "Quelle_Art", "Quelle", "Version", "Herkunftsart");
            bool bezugsartImPaket = tb.Hat(TwwSchema.SPALTE_BEZUGSART);
            if (bezugsartImPaket && !paket.MitBezugsart)
                bericht.Hinweise.Add(ZapfSatz.Neu("KATALOGIMPORT_BEZUGSART_OHNE_SPALTE"));

            var ids = new HashSet<long>();
            var schluessel = new HashSet<string>(StringComparer.Ordinal);
            foreach (var z in tb.Zeilen)
            {
                long id = tb.Ganz(z, "ID")
                          ?? throw new PaketFehler(ZapfSatz.Neu("KATALOGIMPORT_ID_FEHLT", tb.Datei, z.Zeile, "ID"));
                if (!ids.Add(id))
                    throw new PaketFehler(ZapfSatz.Neu("KATALOGIMPORT_ID_DOPPELT", tb.Datei, z.Zeile, id));

                var p = new PaketBedarfstag
                {
                    Id = id,
                    Zeile = z.Zeile,
                    Bezeichner = tb.Text(z, "Bezeichner"),
                    Katalogversion = tb.Text(z, "Katalogversion")
                };
                if (p.Bezeichner.Length == 0) p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_PFLICHT_FEHLT", "Bezeichner");
                else if (p.Katalogversion.Length == 0) p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_PFLICHT_FEHLT", "Katalogversion");

                long? art = tb.Ganz(z, "Quelle_Art");
                if (!art.HasValue) p.Fehler ??= ZapfSatz.Neu("KATALOGIMPORT_PFLICHT_FEHLT", "Quelle_Art");
                else if (!KatalogQuelle(art.Value))
                    p.Fehler ??= ZapfSatz.Neu("KATALOGIMPORT_WERT_UNGUELTIG", "Quelle_Art", tb.Text(z, "Quelle_Art"));
                else p.QuelleArt = (ZapfBedarfstagquelle)(int)art.Value;

                // Eine Bezugsmenge ist wahlfrei (ohne sie wird der Tag nie skaliert); steht eine da,
                // muss sie positiv sein - eine Null oder ein negativer Wert skaliert nichts.
                double? menge = tb.Zahl(z, "Bezugsmenge");
                if (menge.HasValue && !(menge.Value > 0))
                    p.Fehler ??= ZapfSatz.Neu("KATALOGIMPORT_BEDARFSTAG_BEZUGSMENGE", p.Bezeichner, menge.Value);
                p.Bezugsmenge = menge;

                long? bezugsart = bezugsartImPaket ? tb.Ganz(z, TwwSchema.SPALTE_BEZUGSART) : null;
                if (bezugsart.HasValue)
                {
                    if (bezugsart.Value < int.MinValue || bezugsart.Value > int.MaxValue
                        || !Enum.IsDefined(typeof(ZapfBezugsart), (int)bezugsart.Value))
                        p.Fehler ??= ZapfSatz.Neu("KATALOGIMPORT_WERT_UNGUELTIG", TwwSchema.SPALTE_BEZUGSART,
                                                  tb.Text(z, TwwSchema.SPALTE_BEZUGSART));
                    else p.Bezugsart = (int)bezugsart.Value;
                }

                Provenienz h = Herkunft(tb, z, "", out ZapfSatz fh);
                p.Herkunft = h;
                if (fh != null) p.Fehler ??= fh;

                if (p.Fehler == null && !schluessel.Add(p.Bezeichner + "\u0001" + p.Katalogversion))
                    p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_DOPPELT_IM_PAKET");
                liste.Add(p);
            }

            EreignisseLesen(te, liste);
            foreach (PaketBedarfstag p in liste) BedarfstagPruefen(p);
            return liste;
        }

        /// <summary>Gehört die Quelle in einen Katalog? Das Stundenprofil (1) entsteht im Lauf, nie im Paket.</summary>
        private static bool KatalogQuelle(long art)
            => art >= 2 && art <= 5 && Enum.IsDefined(typeof(ZapfBedarfstagquelle), (int)art);

        /// <summary>
        /// Die Ereignisse des Pakets an ihre Bedarfstage. Ein Ereignis ohne seinen Bedarfstag ist ein
        /// Formfehler: Es gibt keine Zeile, der er zufallen könnte — das Paket ist als Ganzes abgelehnt.
        /// </summary>
        private static void EreignisseLesen(PaketTabelle te, List<PaketBedarfstag> liste)
        {
            if (te == null) return;
            Pflicht(te, "ID_Bedarfstag", "Minute_Beginn", "Dauer_min", "Energie_Kwh", "Reihenfolge");
            foreach (var z in te.Zeilen)
            {
                long tagId = te.Ganz(z, "ID_Bedarfstag")
                             ?? throw new PaketFehler(ZapfSatz.Neu("KATALOGIMPORT_ID_FEHLT", te.Datei, z.Zeile, "ID_Bedarfstag"));
                PaketBedarfstag tag = liste.FirstOrDefault(x => x.Id == tagId);
                if (tag == null)
                    throw new PaketFehler(ZapfSatz.Neu("KATALOGIMPORT_EREIGNIS_OHNE_TAG", te.Datei, z.Zeile, tagId));

                long beginn = te.Ganz(z, "Minute_Beginn") ?? -1;
                long dauer = te.Ganz(z, "Dauer_min") ?? 0;
                double? energie = te.Zahl(z, "Energie_Kwh");
                long reihenfolge = te.Ganz(z, "Reihenfolge") ?? 0;
                if (!energie.HasValue) tag.Fehler ??= ZapfSatz.Neu("KATALOGIMPORT_PFLICHT_FEHLT", "Energie_Kwh");

                tag.Reihenfolgen.Add(reihenfolge);
                tag.Ereignisse.Add(new Zapfereignis(Klein(beginn), Klein(dauer), energie ?? 0.0));
            }
            // Die Reihenfolge der Spalte gilt, nicht die der Datei; bei gleicher Zahl bleibt die Datei.
            foreach (PaketBedarfstag p in liste)
            {
                if (p.Reihenfolgen.Count != p.Ereignisse.Count) continue;
                List<int> rang = Enumerable.Range(0, p.Ereignisse.Count)
                                           .OrderBy(i => p.Reihenfolgen[i]).ThenBy(i => i).ToList();
                var sortiert = rang.Select(i => p.Ereignisse[i]).ToList();
                p.Ereignisse.Clear();
                p.Ereignisse.AddRange(sortiert);
            }
        }

        /// <summary>Eine ganze Zahl des Pakets auf <c>int</c> beschnitten — die Prüfung weist sie danach ab.</summary>
        private static int Klein(long wert) => (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, wert));

        /// <summary>Die Regeln eines Bedarfstags (ZU33); der erste Verstoß lehnt NUR ihn ab.</summary>
        private static void BedarfstagPruefen(PaketBedarfstag p)
        {
            if (p.Fehler != null) return;
            if (p.Ereignisse.Count == 0)
            {
                p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_BEDARFSTAG_OHNE_EREIGNIS", p.Bezeichner);
                return;
            }
            foreach (Zapfereignis e in p.Ereignisse)
            {
                if (e.MinuteBeginn < 0 || e.MinuteBeginn > Bedarfstag.MINUTEN - 1 || e.DauerMin < 1
                    || (long)e.MinuteBeginn + e.DauerMin > Bedarfstag.MINUTEN)
                {
                    p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_EREIGNIS_FENSTER", p.Bezeichner, e.MinuteBeginn, e.DauerMin);
                    return;
                }
                if (!(e.EnergieKwh >= 0) || double.IsInfinity(e.EnergieKwh))
                {
                    p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_EREIGNIS_ENERGIE", p.Bezeichner, e.EnergieKwh);
                    return;
                }
            }
            if (!(p.Ereignisse.Sum(e => e.EnergieKwh) > 0))
            {
                p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_BEDARFSTAG_ENERGIE_NULL", p.Bezeichner);
                return;
            }
            // Lueckenlos ab 1: n Ereignisse tragen genau die Zahlen 1 bis n.
            long[] sortiert = p.Reihenfolgen.OrderBy(x => x).ToArray();
            for (int i = 0; i < sortiert.Length; i++)
                if (sortiert[i] != i + 1)
                {
                    p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_EREIGNIS_REIHENFOLGE", p.Bezeichner);
                    return;
                }
        }

        // =================================================================================
        // Das Paket lesen: Parameter
        // =================================================================================

        /// <summary>Die Parameterzeilen des Pakets; je Zeile die Prüfung nach ZU31.</summary>
        private static List<PaketParameterzeile> Parameterzeilen(Dictionary<string, PaketTabelle> tabellen,
                                                                 TwwKatalogimportBericht bericht)
        {
            var liste = new List<PaketParameterzeile>();
            if (!tabellen.TryGetValue(TwwSchema.TAB_TWW_PARAMETER_STAMM, out PaketTabelle tp)) return liste;

            Pflicht(tp, "Schluessel", "Wert", "Katalogversion", "Quelle", "Version", "Herkunftsart");
            var schluessel = new HashSet<string>(StringComparer.Ordinal);
            foreach (var z in tp.Zeilen)
            {
                var p = new PaketParameterzeile
                {
                    Zeile = z.Zeile,
                    Schluessel = tp.Text(z, "Schluessel"),
                    Katalogversion = tp.Text(z, "Katalogversion"),
                    Einheit = tp.Hat("Einheit") ? tp.Text(z, "Einheit") : ""
                };
                if (p.Schluessel.Length == 0) p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_PFLICHT_FEHLT", "Schluessel");
                else if (p.Katalogversion.Length == 0) p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_PFLICHT_FEHLT", "Katalogversion");

                double? wert = tp.Zahl(z, "Wert");
                if (!wert.HasValue) p.Fehler ??= ZapfSatz.Neu("KATALOGIMPORT_PFLICHT_FEHLT", "Wert");
                p.Wert = wert ?? 0.0;

                Provenienz h = Herkunft(tp, z, "", out ZapfSatz fh);
                p.Herkunft = h;
                if (fh != null) p.Fehler ??= fh;

                // Der Schluessel, die Einheit und der Bereich gegen die Liste der gelesenen Schluessel.
                TwwParameterschluessel bekannt = TwwParameterkatalog.Finden(p.Schluessel);
                if (p.Fehler == null && bekannt == null)
                    p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_PARAMETER_UNBEKANNT", p.Schluessel);
                else if (p.Fehler == null && !bekannt.EinheitPasst(p.Einheit))
                    p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_PARAMETER_EINHEIT", p.Schluessel, p.Einheit, bekannt.Einheit);
                else if (p.Fehler == null && !bekannt.ImBereich(p.Wert))
                    p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_PARAMETER_BEREICH", p.Schluessel, p.Wert,
                                            bekannt.Min, bekannt.Max);

                if (p.Fehler == null && !schluessel.Add(p.Schluessel + "\u0001" + p.Katalogversion))
                    p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_DOPPELT_IM_PAKET");
                liste.Add(p);
            }
            return liste;
        }

        // =================================================================================
        // Einspielen: Bedarfstag
        // =================================================================================

        /// <summary>
        /// Ein Bedarfstag des Pakets: anlegen, überspringen oder die vorhandene Zeile am Platz
        /// ersetzen (ZU30). <paramref name="auslieferung"/> sammelt die Bezeichner ersetzter
        /// Auslieferungszeilen für den Hinweis.
        /// </summary>
        private static TwwImportzeile BedarfstagEinspielen(DbVorgang v, PaketBedarfstag p, Paket paket,
                                                           List<string> auslieferung)
        {
            if (p.Fehler != null)
                return new TwwImportzeile(TwwImportausgang.Abgelehnt, p.Bezeichner, p.Katalogversion, "", 0, p.Zeile, p.Fehler)
                { Bereich = TwwImportbereich.Bedarfstag };

            string bezeichner = p.Bezeichner.Trim();
            string version = p.Katalogversion.Trim();
            Provenienz h = ImportHerkunft(p.Herkunft);

            int? ziel = BedarfstagId(v, bezeichner, version);
            if (ziel.HasValue)
            {
                if (BedarfstagGleich(v, ziel.Value, p, paket))
                    return new TwwImportzeile(TwwImportausgang.Uebersprungen, bezeichner, version, bezeichner, 0, p.Zeile,
                                              ZapfSatz.Neu("KATALOGIMPORT_GLEICH_VORHANDEN"))
                    { Bereich = TwwImportbereich.Bedarfstag };

                ZapfKatalogstatus alt = StatusVon(v, TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, ziel.Value);
                int ereignisseAlt = (int)Anzahl(v, "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM +
                                                   " WHERE ID_Bedarfstag = ?", ziel.Value);

                var werte = new List<DbParam>
                {
                    new DbParam("@art", (int)p.QuelleArt),
                    new DbParam("@menge", p.Bezugsmenge.HasValue ? (object)p.Bezugsmenge.Value : null),
                    new DbParam("@q", h.Quelle), new DbParam("@aus", h.Ausgabe), new DbParam("@ver", h.Version),
                    new DbParam("@h", TwwWertemengen.Text(h.Art)),
                    new DbParam("@st", TwwWertemengen.Text(ZapfKatalogstatus.Import)),
                    new DbParam("@id", ziel.Value)
                };
                v.Ausfuehren("UPDATE " + TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + " SET Quelle_Art = ?, Bezugsmenge = ?, " +
                             "Quelle = ?, Ausgabe = ?, Version = ?, Herkunftsart = ?, Status = ?, Beleg = NULL, " +
                             "ReadOnly = 0 WHERE ID = ?", werte.ToArray());
                BezugsartSchreiben(v, paket, ziel.Value, p.Bezugsart);
                EreignisseSchreiben(v, ziel.Value, p.Ereignisse, true);

                if (alt == ZapfKatalogstatus.Auslieferung) auslieferung.Add(bezeichner);
                return new TwwImportzeile(TwwImportausgang.Ersetzt, bezeichner, version, bezeichner, ziel.Value, p.Zeile,
                                          ZapfSatz.Neu("KATALOGIMPORT_BEDARFSTAG_ERSETZT", bezeichner,
                                                       TwwWertemengen.Text(alt), ereignisseAlt, p.Ereignisse.Count))
                { Bereich = TwwImportbereich.Bedarfstag, IdErsetzt = ziel.Value };
            }

            int neu = v.EinfuegenUndId(
                "INSERT INTO " + TwwSchema.TAB_TWW_BEDARFSTAG_STAMM +
                " (Bezeichner, Katalogversion, Quelle_Art, Bezugsmenge, Quelle, Ausgabe, Version, Herkunftsart, " +
                "Status, Beleg, ReadOnly) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, NULL, 0)",
                new[]
                {
                    new DbParam("@b", bezeichner), new DbParam("@k", version), new DbParam("@art", (int)p.QuelleArt),
                    new DbParam("@menge", p.Bezugsmenge.HasValue ? (object)p.Bezugsmenge.Value : null),
                    new DbParam("@q", h.Quelle), new DbParam("@aus", h.Ausgabe), new DbParam("@ver", h.Version),
                    new DbParam("@h", TwwWertemengen.Text(h.Art)),
                    new DbParam("@st", TwwWertemengen.Text(ZapfKatalogstatus.Import))
                });
            BezugsartSchreiben(v, paket, neu, p.Bezugsart);
            EreignisseSchreiben(v, neu, p.Ereignisse, false);
            return new TwwImportzeile(TwwImportausgang.Angelegt, bezeichner, version, bezeichner, neu, p.Zeile, null)
            { Bereich = TwwImportbereich.Bedarfstag };
        }

        /// <summary>Die Bezugsart — nur, wenn die Datenbank die Spalte führt (Schritt 124).</summary>
        private static void BezugsartSchreiben(DbVorgang v, Paket paket, int id, int? bezugsart)
        {
            if (!paket.MitBezugsart) return;
            v.Ausfuehren("UPDATE " + TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + " SET " + TwwSchema.SPALTE_BEZUGSART +
                         " = ? WHERE ID = ?",
                         new DbParam("@a", bezugsart.HasValue ? (object)bezugsart.Value : null), new DbParam("@id", id));
        }

        /// <summary>Die Ereignisse eines Bedarfstags; <paramref name="ersetzen"/> nimmt die vorhandenen vorher weg.</summary>
        private static void EreignisseSchreiben(DbVorgang v, int idBedarfstag, IReadOnlyList<Zapfereignis> ereignisse,
                                                bool ersetzen)
        {
            if (ersetzen)
                v.Ausfuehren("DELETE FROM " + TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM + " WHERE ID_Bedarfstag = ?",
                             new DbParam("@id", idBedarfstag));
            for (int i = 0; i < ereignisse.Count; i++)
                v.Ausfuehren("INSERT INTO " + TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM +
                             " (ID_Bedarfstag, Minute_Beginn, Dauer_min, Energie_Kwh, Reihenfolge) VALUES (?, ?, ?, ?, ?)",
                             new DbParam("@id", idBedarfstag), new DbParam("@m", ereignisse[i].MinuteBeginn),
                             new DbParam("@d", ereignisse[i].DauerMin), new DbParam("@e", ereignisse[i].EnergieKwh),
                             new DbParam("@r", i + 1));
        }

        private static int? BedarfstagId(DbVorgang v, string bezeichner, string version)
            => IdOderNull(v.Skalar("SELECT ID FROM " + TwwSchema.TAB_TWW_BEDARFSTAG_STAMM +
                                   " WHERE Bezeichner = ? AND Katalogversion = ?",
                                   new DbParam("@b", bezeichner), new DbParam("@k", version)));

        /// <summary>
        /// Trägt die Katalogzeile den Inhalt der Paketzeile? Quelle, Bezugsmenge, Bezugsart und die
        /// Ereignisse in ihrer Reihenfolge — die Provenienz bleibt außen vor (dieselbe Gruppenregel
        /// wie bei den Nutzungsarten), Status und <c>ReadOnly</c> ebenso.
        /// </summary>
        private static bool BedarfstagGleich(DbVorgang v, int ziel, PaketBedarfstag p, Paket paket)
        {
            DataTable dt = v.Lese("SELECT * FROM " + TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + " WHERE ID = ?",
                                  new DbParam("@id", ziel));
            if (dt == null || dt.Rows.Count != 1) return false;
            DataRow r = dt.Rows[0];
            if (Convert.ToInt32(r["Quelle_Art"], CultureInfo.InvariantCulture) != (int)p.QuelleArt) return false;

            double? menge = r["Bezugsmenge"] == DBNull.Value
                ? (double?)null : Convert.ToDouble(r["Bezugsmenge"], CultureInfo.InvariantCulture);
            if (menge.HasValue != p.Bezugsmenge.HasValue) return false;
            if (menge.HasValue && !menge.Value.Equals(p.Bezugsmenge.Value)) return false;

            if (paket.MitBezugsart)
            {
                object b = r[TwwSchema.SPALTE_BEZUGSART];
                int? alt = b == DBNull.Value ? (int?)null : Convert.ToInt32(b, CultureInfo.InvariantCulture);
                if (alt != p.Bezugsart) return false;
            }

            DataTable de = v.Lese("SELECT Minute_Beginn, Dauer_min, Energie_Kwh FROM " +
                                  TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM +
                                  " WHERE ID_Bedarfstag = ? ORDER BY Reihenfolge, ID", new DbParam("@id", ziel));
            int zahl = de?.Rows.Count ?? 0;
            if (zahl != p.Ereignisse.Count) return false;
            for (int i = 0; i < zahl; i++)
            {
                DataRow e = de.Rows[i];
                if (Convert.ToInt32(e["Minute_Beginn"], CultureInfo.InvariantCulture) != p.Ereignisse[i].MinuteBeginn) return false;
                if (Convert.ToInt32(e["Dauer_min"], CultureInfo.InvariantCulture) != p.Ereignisse[i].DauerMin) return false;
                if (!Convert.ToDouble(e["Energie_Kwh"], CultureInfo.InvariantCulture).Equals(p.Ereignisse[i].EnergieKwh)) return false;
            }
            return true;
        }

        // =================================================================================
        // Einspielen: Parameter
        // =================================================================================

        /// <summary>
        /// Eine Parameterzeile des Pakets: anlegen, überspringen oder den Wert am Platz ersetzen
        /// (ZU31). Keine Versionsbildung „(Import n)" — ein Parameter ist ein Wert, keine Version.
        /// </summary>
        private static TwwImportzeile ParameterEinspielen(DbVorgang v, PaketParameterzeile p, List<string> auslieferung)
        {
            if (p.Fehler != null)
                return new TwwImportzeile(TwwImportausgang.Abgelehnt, p.Schluessel, p.Katalogversion, "", 0, p.Zeile, p.Fehler)
                { Bereich = TwwImportbereich.Parameter };

            string schluessel = p.Schluessel.Trim();
            string version = p.Katalogversion.Trim();
            string einheit = (p.Einheit ?? "").Trim();
            Provenienz h = ImportHerkunft(p.Herkunft);

            DataTable dt = v.Lese("SELECT ID, Wert, Einheit, Status FROM " + TwwSchema.TAB_TWW_PARAMETER_STAMM +
                                  " WHERE Schluessel = ? AND Katalogversion = ?",
                                  new DbParam("@s", schluessel), new DbParam("@k", version));
            if (dt != null && dt.Rows.Count == 1)
            {
                DataRow r = dt.Rows[0];
                int id = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture);
                double altWert = Convert.ToDouble(r["Wert"], CultureInfo.InvariantCulture);
                string altEinheit = r["Einheit"] == DBNull.Value
                    ? "" : Convert.ToString(r["Einheit"], CultureInfo.InvariantCulture) ?? "";
                if (altWert.Equals(p.Wert) && string.Equals(altEinheit.Trim(), einheit, StringComparison.Ordinal))
                    return new TwwImportzeile(TwwImportausgang.Uebersprungen, schluessel, version, schluessel, 0, p.Zeile,
                                              ZapfSatz.Neu("KATALOGIMPORT_GLEICH_VORHANDEN"))
                    { Bereich = TwwImportbereich.Parameter };

                ZapfKatalogstatus alt = TwwWertemengen.Status(Convert.ToString(r["Status"], CultureInfo.InvariantCulture));
                v.Ausfuehren("UPDATE " + TwwSchema.TAB_TWW_PARAMETER_STAMM + " SET Wert = ?, Einheit = ?, Quelle = ?, " +
                             "Ausgabe = ?, Version = ?, Herkunftsart = ?, Status = ?, Beleg = NULL, ReadOnly = 0 " +
                             "WHERE ID = ?",
                             new DbParam("@w", p.Wert), new DbParam("@e", einheit.Length == 0 ? null : einheit),
                             new DbParam("@q", h.Quelle), new DbParam("@aus", h.Ausgabe), new DbParam("@ver", h.Version),
                             new DbParam("@h", TwwWertemengen.Text(h.Art)),
                             new DbParam("@st", TwwWertemengen.Text(ZapfKatalogstatus.Import)), new DbParam("@id", id));
                if (alt == ZapfKatalogstatus.Auslieferung) auslieferung.Add(schluessel);
                return new TwwImportzeile(TwwImportausgang.Ersetzt, schluessel, version, schluessel, id, p.Zeile,
                                          ZapfSatz.Neu("KATALOGIMPORT_PARAMETER_ERSETZT", schluessel, altWert, p.Wert,
                                                       einheit.Length == 0 ? "-" : einheit))
                { Bereich = TwwImportbereich.Parameter, IdErsetzt = id };
            }

            int neuId = v.EinfuegenUndId(
                "INSERT INTO " + TwwSchema.TAB_TWW_PARAMETER_STAMM +
                " (Schluessel, Wert, Einheit, Katalogversion, Quelle, Ausgabe, Version, Herkunftsart, Status, Beleg, ReadOnly) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, NULL, 0)",
                new[]
                {
                    new DbParam("@s", schluessel), new DbParam("@w", p.Wert),
                    new DbParam("@e", einheit.Length == 0 ? null : einheit), new DbParam("@k", version),
                    new DbParam("@q", h.Quelle), new DbParam("@aus", h.Ausgabe), new DbParam("@ver", h.Version),
                    new DbParam("@h", TwwWertemengen.Text(h.Art)),
                    new DbParam("@st", TwwWertemengen.Text(ZapfKatalogstatus.Import))
                });
            return new TwwImportzeile(TwwImportausgang.Angelegt, schluessel, version, schluessel, neuId, p.Zeile, null)
            { Bereich = TwwImportbereich.Parameter };
        }
    }
}
