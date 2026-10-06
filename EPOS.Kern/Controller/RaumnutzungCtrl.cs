using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Controller der Nutzungsprofile</b> (Konzept Nutzungsprofile 4.4, NP-F3, NP-F12, NP-F14, NP-F17–NP-F19): liest
    /// Kategorien, Profile samt Zeilenbild und Stundenprofilen und die Zuordnung; legt eigene Kategorien, Profile und
    /// Zuordnungen an, ändert, dupliziert und löscht sie — ausgelieferte Zeilen (<c>ReadOnly = 1</c>) nur duplizieren,
    /// eine Zuordnung darf aber auf ein ausgeliefertes Profil zeigen und eine ausgelieferte Zuordnung umgestellt oder
    /// auf „keine" gesetzt werden. Alle Zugriffe über <see cref="DataRepository"/> mit <c>?</c>-Parametern.
    ///
    /// <para><b>Profil übernehmen</b> (<see cref="ProfilUebernehmen"/>) erzeugt je Größe über den
    /// <see cref="Raumnutzungsgenerator"/> eine Vorlage und schreibt sie mit <see cref="Konditionierungsarbeit.VorlageUebernehmen"/>
    /// — demselben reinen Schritt wie „Vorlage übernehmen" — als Kopie an Gebäude oder Zone: Ferien des Ziels, P12 bei
    /// vorhandenem Kalender (nur der Matrixbereich wird ersetzt, eigene Perioden bleiben), Herkunft und Nutzung als Text
    /// (Profilname, NP-F14), bei einer Zone dazu <c>Tab_Zone.Nutzungsprofil</c>. Ein Vorgang; die Rückgabe nennt je Größe
    /// Weg, Ersetzung, Nennwert samt Herleitung und Hinweis.</para>
    /// </summary>
    public sealed class RaumnutzungCtrl
    {
        // =================================================================
        //  Datenklassen
        // =================================================================

        /// <summary>Ein Ergebnis: <c>Ok</c> oder die benannte Ablehnung.</summary>
        public sealed record Ergebnis(bool Ok, string Meldung, long Id = 0)
        {
            /// <summary>Gelungen, ohne neue Id.</summary>
            public static readonly Ergebnis Gut = new Ergebnis(true, null);

            /// <summary>Gelungen mit der Id der neuen Zeile.</summary>
            public static Ergebnis MitId(long id) => new Ergebnis(true, null, id);

            /// <summary>Benannt abgelehnt.</summary>
            public static Ergebnis Fehler(string meldung) => new Ergebnis(false, meldung ?? "");
        }

        /// <summary>Eine Kategorie (<c>Tab_Raumnutzungskatalog</c>).</summary>
        public sealed record Kategorie(long Id, string Bezeichner, string Art, string Beschreibung, string Quellenhinweis,
                                       bool Ausgeliefert, int? Reihenfolge);

        /// <summary>Eine Zeile der Zuordnung (<c>Tab_Raumnutzungszuordnung</c>); <c>IdProfil = null</c> heißt „keine".</summary>
        public sealed record Zuordnung(long Id, string Art, string Schluessel, long? IdProfil, bool Ausgeliefert);

        /// <summary>Was „Profil übernehmen" an einer Größe getan hat.</summary>
        /// <param name="Groesse">Die Größe.</param>
        /// <param name="Weg">Woher die Zeilen kamen; <see cref="Raumnutzungsweg.Keiner"/> = nicht belegt, das Ziel behält seinen Kalender.</param>
        /// <param name="Uebernommen">Ein Kalender wurde geschrieben.</param>
        /// <param name="Ersetzt">Das Ziel trug schon einen Kalender dieser Größe; ersetzt wurde nur sein Matrixbereich (P12).</param>
        /// <param name="Unbeheizt">Heizen bzw. Kühlen an einer unbeheizten Zone: übersprungen (Zonenregel).</param>
        /// <param name="Nennwert">Der gesetzte Nennwert [W] oder <c>null</c>.</param>
        /// <param name="Nennwertherleitung">Die Formel des Nennwerts oder <c>null</c>.</param>
        /// <param name="Hinweis">Was der Generator benennt.</param>
        public sealed record Uebernahmeposten(Konditionierungsgroesse Groesse, Raumnutzungsweg Weg, bool Uebernommen, bool Ersetzt,
                                              bool Unbeheizt, double? Nennwert, string Nennwertherleitung, Raumnutzungshinweis Hinweis);

        /// <summary>Das Ergebnis von „Profil übernehmen".</summary>
        public sealed record Uebernahme(bool Ok, string Meldung, string Profilname, IReadOnlyList<Uebernahmeposten> Posten)
        {
            /// <summary>Benannt abgelehnt; nichts geschrieben.</summary>
            public static Uebernahme Fehler(string meldung) => new Uebernahme(false, meldung ?? "", null, Array.Empty<Uebernahmeposten>());
        }

        // =================================================================
        //  Lesen
        // =================================================================

        /// <summary>Stehen die fünf Tabellen des Katalogs?</summary>
        public static bool Lesbar() => RaumnutzungSchema.TABELLEN.All(DataRepository.TabelleVorhanden);

        /// <summary>Die Kategorien: nach Reihenfolge, dann Name; leer ohne Katalog.</summary>
        public List<Kategorie> Kategorien()
        {
            var liste = new List<Kategorie>();
            if (!Lesbar()) return liste;
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"ID\", \"Bezeichner\", \"Art\", \"Beschreibung\", \"Quellenhinweis\", \"ReadOnly\", \"Reihenfolge\" FROM \"" +
                RaumnutzungSchema.TAB_KATALOG + "\" ORDER BY IIF(\"Reihenfolge\" IS NULL, 1, 0), \"Reihenfolge\", " +
                "\"Bezeichner\" COLLATE NOCASE, \"ID\"");
            if (t != null)
                foreach (DataRow r in t.Rows) liste.Add(KategorieAus(r));
            return liste;
        }

        /// <summary>Eine Kategorie über ihre Id; <c>null</c>, wenn es sie nicht gibt.</summary>
        public Kategorie KategorieLesen(long id)
        {
            if (!Lesbar()) return null;
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"ID\", \"Bezeichner\", \"Art\", \"Beschreibung\", \"Quellenhinweis\", \"ReadOnly\", \"Reihenfolge\" FROM \"" +
                RaumnutzungSchema.TAB_KATALOG + "\" WHERE \"ID\" = ?", new DbParam("@id", id));
            return t == null || t.Rows.Count == 0 ? null : KategorieAus(t.Rows[0]);
        }

        /// <summary>
        /// Die Profile samt Zeilenbild und Stundenprofilen — einer Kategorie oder (<paramref name="idKatalog"/> = <c>null</c>)
        /// aller; je Kategorie nach Nummer und Name.
        /// </summary>
        public List<Raumnutzungsprofil> Profile(long? idKatalog = null)
        {
            var liste = new List<Raumnutzungsprofil>();
            if (!Lesbar()) return liste;
            DataTable t = idKatalog.HasValue
                ? DataRepository.GetDataTable(SQL_PROFIL + " WHERE \"ID_Katalog\" = ? " + SQL_PROFIL_ORDNUNG, new DbParam("@k", idKatalog.Value))
                : DataRepository.GetDataTable(SQL_PROFIL + " " + SQL_PROFIL_ORDNUNG);
            if (t == null) return liste;
            foreach (DataRow r in t.Rows) liste.Add(ProfilAus(r));
            Inhalt(liste);
            return liste;
        }

        /// <summary>Ein Profil samt Zeilenbild und Stundenprofilen; <c>null</c>, wenn es es nicht gibt.</summary>
        public Raumnutzungsprofil ProfilLesen(long id)
        {
            if (!Lesbar()) return null;
            DataTable t = DataRepository.GetDataTable(SQL_PROFIL + " WHERE \"ID\" = ?", new DbParam("@id", id));
            if (t == null || t.Rows.Count == 0) return null;
            var p = ProfilAus(t.Rows[0]);
            Inhalt(new List<Raumnutzungsprofil> { p });
            return p;
        }

        /// <summary>Die Zuordnung — einer Art oder (<paramref name="art"/> = <c>null</c>) aller — nach Art und Schlüssel.</summary>
        public List<Zuordnung> Zuordnungen(string art = null)
        {
            var liste = new List<Zuordnung>();
            if (!Lesbar()) return liste;
            const string SPALTEN = "SELECT \"ID\", \"Art\", \"Schluessel\", \"ID_Profil\", \"ReadOnly\" FROM \"" + RaumnutzungSchema.TAB_ZUORDNUNG + "\"";
            const string ORDNUNG = " ORDER BY \"Art\", \"Schluessel\" COLLATE NOCASE, \"ID\"";
            DataTable t = art == null
                ? DataRepository.GetDataTable(SPALTEN + ORDNUNG)
                : DataRepository.GetDataTable(SPALTEN + " WHERE \"Art\" = ?" + ORDNUNG, new DbParam("@a", art));
            if (t != null)
                foreach (DataRow r in t.Rows) liste.Add(ZuordnungAus(r));
            return liste;
        }

        /// <summary>
        /// <b>Die Zuordnung auflösen</b> (NP-F12): Art und Schlüssel (getrimmt, ganzer Vergleich ohne Unterschied der
        /// Schreibung) → das Profil samt Inhalt; <c>null</c> ohne Zeile, bei „keine" (<c>ID_Profil</c> leer) oder ohne Katalog.
        /// Der Rückfall auf die Vorgabe im Code gehört dem Aufrufer.
        /// </summary>
        public Raumnutzungsprofil ZuordnungAufloesen(string art, string schluessel)
        {
            string s = schluessel?.Trim();
            if (!Lesbar() || string.IsNullOrEmpty(art) || string.IsNullOrEmpty(s)) return null;
            object id = DataRepository.ExecuteScalar(
                "SELECT \"ID_Profil\" FROM \"" + RaumnutzungSchema.TAB_ZUORDNUNG +
                "\" WHERE \"Art\" = ? AND \"Schluessel\" = ? COLLATE NOCASE ORDER BY \"ID\" LIMIT 1",
                new DbParam("@a", art), new DbParam("@s", s));
            long? idProfil = Lang(id);
            return idProfil.HasValue ? ProfilLesen(idProfil.Value) : null;
        }

        // =================================================================
        //  Kategorien
        // =================================================================

        /// <summary>Legt eine eigene Kategorie an (Art <c>EIGEN</c>, ans Ende der Reihenfolge).</summary>
        public Ergebnis KategorieAnlegen(string bezeichner, string beschreibung, string quellenhinweis)
        {
            string bereit = Bereit();
            if (bereit != null) return Ergebnis.Fehler(bereit);
            string m = KategorieTexte(ref bezeichner, ref beschreibung, ref quellenhinweis, 0);
            if (m != null) return Ergebnis.Fehler(m);
            return Schreibe(v => Ergebnis.MitId(KategorieEinfuegen(v, bezeichner, beschreibung, quellenhinweis)));
        }

        /// <summary>Ändert Name, Beschreibung und Quellenhinweis einer eigenen Kategorie.</summary>
        public Ergebnis KategorieAendern(long id, string bezeichner, string beschreibung, string quellenhinweis)
        {
            string bereit = Bereit();
            if (bereit != null) return Ergebnis.Fehler(bereit);
            Kategorie k = KategorieLesen(id);
            if (k == null) return Ergebnis.Fehler(Fehlt(MyResource.Resource.RAUMNUTZUNG_MSG_KATEGORIE_FEHLT, id));
            if (k.Ausgeliefert) return Ergebnis.Fehler(Ausgeliefert(k.Bezeichner));
            string m = KategorieTexte(ref bezeichner, ref beschreibung, ref quellenhinweis, id);
            if (m != null) return Ergebnis.Fehler(m);
            return Schreibe(v =>
            {
                v.Ausfuehren("UPDATE \"" + RaumnutzungSchema.TAB_KATALOG + "\" SET \"Bezeichner\" = ?, \"Beschreibung\" = ?, " +
                             "\"Quellenhinweis\" = ? WHERE \"ID\" = ?",
                             new DbParam("@b", bezeichner), new DbParam("@be", (object)beschreibung),
                             new DbParam("@q", (object)quellenhinweis), new DbParam("@id", id));
                return Ergebnis.Gut;
            });
        }

        /// <summary>
        /// Dupliziert eine Kategorie — auch eine ausgelieferte — samt aller Profile als eigene Kategorie (Art <c>EIGEN</c>,
        /// <c>ReadOnly = 0</c>); ohne Namen „Name (Kopie)", eindeutig gemacht. Die Zuordnung bleibt, wie sie ist.
        /// </summary>
        public Ergebnis KategorieDuplizieren(long id, string bezeichner)
        {
            string bereit = Bereit();
            if (bereit != null) return Ergebnis.Fehler(bereit);
            Kategorie k = KategorieLesen(id);
            if (k == null) return Ergebnis.Fehler(Fehlt(MyResource.Resource.RAUMNUTZUNG_MSG_KATEGORIE_FEHLT, id));
            string name = Wunschname(bezeichner, k.Bezeichner, Kategorien().Select(x => x.Bezeichner).ToList());
            string beschreibung = k.Beschreibung, quelle = k.Quellenhinweis;
            string m = KategorieTexte(ref name, ref beschreibung, ref quelle, 0);
            if (m != null) return Ergebnis.Fehler(m);
            List<Raumnutzungsprofil> profile = Profile(id);
            return Schreibe(v =>
            {
                long neu = KategorieEinfuegen(v, name, beschreibung, quelle);
                foreach (Raumnutzungsprofil p in profile)
                {
                    Raumnutzungsprofil kopie = p.Kopie();
                    kopie.IdKatalog = neu;
                    kopie.Ausgeliefert = false;
                    ProfilEinfuegen(v, kopie);
                }
                return Ergebnis.MitId(neu);
            });
        }

        /// <summary>
        /// Löscht eine eigene Kategorie samt ihrer Profile (Kaskade); Zuordnungen auf diese Profile werden „keine"
        /// (NP-F19 — die Rückfrage führt der Aufrufer, <see cref="ZuordnungenAuf"/>).
        /// </summary>
        public Ergebnis KategorieLoeschen(long id)
        {
            string bereit = Bereit();
            if (bereit != null) return Ergebnis.Fehler(bereit);
            Kategorie k = KategorieLesen(id);
            if (k == null) return Ergebnis.Fehler(Fehlt(MyResource.Resource.RAUMNUTZUNG_MSG_KATEGORIE_FEHLT, id));
            if (k.Ausgeliefert) return Ergebnis.Fehler(Ausgeliefert(k.Bezeichner));
            return Schreibe(v =>
            {
                v.Ausfuehren("UPDATE \"" + RaumnutzungSchema.TAB_ZUORDNUNG + "\" SET \"ID_Profil\" = NULL WHERE \"ID_Profil\" IN " +
                             "(SELECT \"ID\" FROM \"" + RaumnutzungSchema.TAB_PROFIL + "\" WHERE \"ID_Katalog\" = ?)", new DbParam("@k", id));
                v.Ausfuehren("DELETE FROM \"" + RaumnutzungSchema.TAB_PROFIL + "\" WHERE \"ID_Katalog\" = ?", new DbParam("@k", id));
                v.Ausfuehren("DELETE FROM \"" + RaumnutzungSchema.TAB_KATALOG + "\" WHERE \"ID\" = ?", new DbParam("@id", id));
                return Ergebnis.Gut;
            });
        }

        // =================================================================
        //  Profile
        // =================================================================

        /// <summary>Legt ein eigenes Profil in einer eigenen Kategorie an — Kopf, Kennwerte, Zeilenbild und Stunden in einem Vorgang.</summary>
        public Ergebnis ProfilAnlegen(Raumnutzungsprofil profil)
        {
            if (profil == null) throw new ArgumentNullException(nameof(profil));
            string bereit = Bereit();
            if (bereit != null) return Ergebnis.Fehler(bereit);
            Raumnutzungsprofil p = profil.Kopie();
            p.Id = 0;
            p.Ausgeliefert = false;
            string m = ZielkategoriePruefen(p.IdKatalog) ?? Profilpruefung(p) ?? Eindeutigkeit(p);
            if (m != null) return Ergebnis.Fehler(m);
            return Schreibe(v => Ergebnis.MitId(ProfilEinfuegen(v, p)));
        }

        /// <summary>Ändert ein eigenes Profil im Ganzen (Kopf, Kennwerte, Zeilenbild, Stunden); auch der Wechsel in eine andere eigene Kategorie.</summary>
        public Ergebnis ProfilAendern(Raumnutzungsprofil profil)
        {
            if (profil == null) throw new ArgumentNullException(nameof(profil));
            string bereit = Bereit();
            if (bereit != null) return Ergebnis.Fehler(bereit);
            Raumnutzungsprofil alt = ProfilLesen(profil.Id);
            if (alt == null) return Ergebnis.Fehler(Fehlt(MyResource.Resource.RAUMNUTZUNG_MSG_PROFIL_FEHLT, profil.Id));
            if (alt.Ausgeliefert) return Ergebnis.Fehler(Ausgeliefert(alt.Bezeichner));
            Raumnutzungsprofil p = profil.Kopie();
            p.Ausgeliefert = false;
            string m = ZielkategoriePruefen(p.IdKatalog) ?? Profilpruefung(p) ?? Eindeutigkeit(p);
            if (m != null) return Ergebnis.Fehler(m);
            return Schreibe(v =>
            {
                // Kopf und Kennwerte an Ort und Stelle — die Zuordnungen zeigen weiter auf dieselbe Id —, Zeilenbild und Stunden neu.
                var par = KopfParameter(p);
                par.Add(new DbParam("@id", p.Id));
                v.Ausfuehren("UPDATE \"" + RaumnutzungSchema.TAB_PROFIL + "\" SET " +
                             string.Join(", ", SPALTEN_ALLE.Select(c => "\"" + c + "\" = ?")) + " WHERE \"ID\" = ?", par.ToArray());
                v.Ausfuehren("DELETE FROM \"" + RaumnutzungSchema.TAB_ZEILE + "\" WHERE \"ID_Profil\" = ?", new DbParam("@id", p.Id));
                v.Ausfuehren("DELETE FROM \"" + RaumnutzungSchema.TAB_STUNDEN + "\" WHERE \"ID_Profil\" = ?", new DbParam("@id", p.Id));
                InhaltEinfuegen(v, p, p.Id);
                return Ergebnis.MitId(p.Id);
            });
        }

        /// <summary>
        /// Dupliziert ein Profil — auch ein ausgeliefertes — als eigenes (NP-F19), in die Kategorie
        /// <paramref name="idZielKatalog"/> oder, ohne Angabe, in die eigene des Profils; eine ausgelieferte Kategorie ist
        /// kein Ziel. Ohne Namen „Name (Kopie)", eindeutig gemacht.
        /// </summary>
        public Ergebnis ProfilDuplizieren(long id, long? idZielKatalog, string bezeichner)
        {
            string bereit = Bereit();
            if (bereit != null) return Ergebnis.Fehler(bereit);
            Raumnutzungsprofil q = ProfilLesen(id);
            if (q == null) return Ergebnis.Fehler(Fehlt(MyResource.Resource.RAUMNUTZUNG_MSG_PROFIL_FEHLT, id));
            Raumnutzungsprofil p = q.Kopie();
            p.Id = 0;
            p.Ausgeliefert = false;
            p.IdKatalog = idZielKatalog ?? q.IdKatalog;
            string m = ZielkategoriePruefen(p.IdKatalog);
            if (m != null) return Ergebnis.Fehler(m);
            List<Raumnutzungsprofil> nachbarn = Profile(p.IdKatalog);
            p.Bezeichner = Wunschname(bezeichner, q.Bezeichner, nachbarn.Select(x => x.Bezeichner).ToList());
            if (p.Nummer != null && nachbarn.Any(x => string.Equals(x.Nummer, p.Nummer, StringComparison.OrdinalIgnoreCase)))
                p.Nummer = null;
            m = Profilpruefung(p) ?? Eindeutigkeit(p);
            if (m != null) return Ergebnis.Fehler(m);
            return Schreibe(v => Ergebnis.MitId(ProfilEinfuegen(v, p)));
        }

        /// <summary>Wie viele Zuordnungen zeigen auf das Profil? (Die Rückfrage vor dem Löschen, NP-F19.)</summary>
        public int ZuordnungenAuf(long idProfil)
            => Lesbar()
                ? (int)(Lang(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + RaumnutzungSchema.TAB_ZUORDNUNG +
                                                          "\" WHERE \"ID_Profil\" = ?", new DbParam("@p", idProfil))) ?? 0)
                : 0;

        /// <summary>Löscht ein eigenes Profil samt Zeilenbild und Stunden; Zuordnungen darauf werden „keine" (NP-F19).</summary>
        public Ergebnis ProfilLoeschen(long id)
        {
            string bereit = Bereit();
            if (bereit != null) return Ergebnis.Fehler(bereit);
            Raumnutzungsprofil p = ProfilLesen(id);
            if (p == null) return Ergebnis.Fehler(Fehlt(MyResource.Resource.RAUMNUTZUNG_MSG_PROFIL_FEHLT, id));
            if (p.Ausgeliefert) return Ergebnis.Fehler(Ausgeliefert(p.Bezeichner));
            return Schreibe(v =>
            {
                v.Ausfuehren("UPDATE \"" + RaumnutzungSchema.TAB_ZUORDNUNG + "\" SET \"ID_Profil\" = NULL WHERE \"ID_Profil\" = ?",
                             new DbParam("@p", id));
                v.Ausfuehren("DELETE FROM \"" + RaumnutzungSchema.TAB_PROFIL + "\" WHERE \"ID\" = ?", new DbParam("@id", id));
                return Ergebnis.Gut;
            });
        }

        // =================================================================
        //  Zuordnung
        // =================================================================

        /// <summary>
        /// Setzt die Zuordnung von (Art, Schlüssel) auf ein Profil oder „keine" (<paramref name="idProfil"/> = <c>null</c>):
        /// eine vorhandene Zeile — auch eine ausgelieferte — wird umgestellt, sonst eine eigene angelegt. Das Profil darf
        /// ausgeliefert sein.
        /// </summary>
        public Ergebnis ZuordnungSetzen(string art, string schluessel, long? idProfil)
        {
            string bereit = Bereit();
            if (bereit != null) return Ergebnis.Fehler(bereit);
            if (art == null || !RaumnutzungSchema.ARTEN_ZUORDNUNG.Contains(art))
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RAUMNUTZUNG_MSG_ZUORDNUNG_ART, art ?? ""));
            string s = schluessel?.Trim();
            if (string.IsNullOrEmpty(s) || s.Length > RaumnutzungSchema.BEZEICHNER_MAX_ZEICHEN)
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RAUMNUTZUNG_MSG_SCHLUESSEL_LAENGE,
                                                     RaumnutzungSchema.BEZEICHNER_MAX_ZEICHEN));
            if (idProfil.HasValue && ProfilKopf(idProfil.Value) == null)
                return Ergebnis.Fehler(Fehlt(MyResource.Resource.RAUMNUTZUNG_MSG_PROFIL_FEHLT, idProfil.Value));
            return Schreibe(v =>
            {
                long? vorhanden = Lang(v.Skalar("SELECT \"ID\" FROM \"" + RaumnutzungSchema.TAB_ZUORDNUNG +
                                                "\" WHERE \"Art\" = ? AND \"Schluessel\" = ? COLLATE NOCASE ORDER BY \"ID\" LIMIT 1",
                                                new DbParam("@a", art), new DbParam("@s", s)));
                if (vorhanden.HasValue)
                {
                    v.Ausfuehren("UPDATE \"" + RaumnutzungSchema.TAB_ZUORDNUNG + "\" SET \"ID_Profil\" = ? WHERE \"ID\" = ?",
                                 new DbParam("@p", (object)idProfil), new DbParam("@id", vorhanden.Value));
                    return Ergebnis.MitId(vorhanden.Value);
                }
                v.Ausfuehren("INSERT INTO \"" + RaumnutzungSchema.TAB_ZUORDNUNG + "\" (\"Art\", \"Schluessel\", \"ID_Profil\", \"ReadOnly\") " +
                             "VALUES (?, ?, ?, 0)", new DbParam("@a", art), new DbParam("@s", s), new DbParam("@p", (object)idProfil));
                return Ergebnis.MitId(Lang(v.Skalar("SELECT last_insert_rowid()")) ?? 0);
            });
        }

        /// <summary>Löscht eine eigene Zuordnung; eine ausgelieferte lässt sich nur auf „keine" stellen.</summary>
        public Ergebnis ZuordnungLoeschen(long id)
        {
            string bereit = Bereit();
            if (bereit != null) return Ergebnis.Fehler(bereit);
            Zuordnung z = Zuordnungen().FirstOrDefault(x => x.Id == id);
            if (z == null) return Ergebnis.Fehler(Fehlt(MyResource.Resource.RAUMNUTZUNG_MSG_ZUORDNUNG_FEHLT, id));
            if (z.Ausgeliefert) return Ergebnis.Fehler(Ausgeliefert(z.Art + ":" + z.Schluessel));
            return Schreibe(v =>
            {
                v.Ausfuehren("DELETE FROM \"" + RaumnutzungSchema.TAB_ZUORDNUNG + "\" WHERE \"ID\" = ?", new DbParam("@id", id));
                return Ergebnis.Gut;
            });
        }

        // =================================================================
        //  Profil übernehmen
        // =================================================================

        /// <summary>
        /// <b>Profil übernehmen</b> an ein Gebäude (<paramref name="idZone"/> = <c>null</c>) oder eine seiner Zonen: je Größe
        /// erzeugt der <see cref="Raumnutzungsgenerator"/> die Vorlage, <see cref="Konditionierungsarbeit.VorlageUebernehmen"/>
        /// schreibt sie als Kopie (P12 bei vorhandenem Kalender; die Rückfrage „Luftwechsel aufteilen" wird wie im Zonenplan
        /// bejaht — der wirksame Luftwechsel bleibt). Eine unbeheizte Zone bekommt weder Heiz- noch Kühlkalender, eine
        /// nicht belegte Größe keinen (NP-F6). Herkunft und <c>Nutzung</c> der Kopien ist der Profilname, bei einer Zone
        /// auch <c>Tab_Zone.Nutzungsprofil</c> — auch ohne einen Kalender (NP-F13). Alles in einem Vorgang.
        /// </summary>
        /// <param name="idProfil">Das Profil.</param>
        /// <param name="idGebaeude">Das Gebäude (<c>Tab_Gebaeude.ID</c>).</param>
        /// <param name="idZone">Die Zone oder <c>null</c> für das Gebäude.</param>
        /// <param name="flaeche">Die Fläche des Ziels für die Nennwerte (Q39, Q40); <c>null</c> = der Nennwert des Ziels bleibt.</param>
        /// <param name="lichteHoehe">Die lichte Höhe des Ziels für Außenluft in m³/(h·m²); <c>null</c> = ohne.</param>
        public Uebernahme ProfilUebernehmen(long idProfil, long idGebaeude, long? idZone, double? flaeche, double? lichteHoehe = null)
        {
            return Uebernehmen(idProfil, idGebaeude, idZone, flaeche, lichteHoehe);
        }

        /// <summary>
        /// <b>Profil übernehmen mit den Maßen des Ziels</b> (Stufe NP3b, NP-F10, Q39, Q40): Fläche und lichte Höhe liest
        /// der Weg selbst — an der Zone ihre Nutzfläche und Raumhöhe (leer = die des Gebäudes), am Gebäude dessen
        /// Nutzfläche und Raumhöhe (<see cref="Zielmasse"/>); sonst wie
        /// <see cref="ProfilUebernehmen(long, long, long?, double?, double?)"/>.
        /// </summary>
        public Uebernahme ProfilUebernehmen(long idProfil, long idGebaeude, long? idZone)
        {
            (double? flaeche, double? hoehe) = Zielmasse(idGebaeude, idZone);
            return Uebernehmen(idProfil, idGebaeude, idZone, flaeche, hoehe);
        }

        /// <summary>
        /// <b>Fläche und lichte Höhe eines Ziels</b> aus der Datenbank (NP-F10): an der Zone <c>Tab_Zone.Nutzflaeche</c>
        /// und <c>Raumhoehe</c> (leer = die Raumhöhe des Gebäudes), am Gebäude <c>Tab_Gebaeude.Nutzflaeche</c> und
        /// <c>Raumhoehe</c>; ein Wert ≤ 0 heißt „ohne".
        /// </summary>
        public static (double? Flaeche, double? LichteHoehe) Zielmasse(long idGebaeude, long? idZone)
        {
            System.Data.DataTable g = DataRepository.GetDataTable(
                "SELECT \"Nutzflaeche\", \"Raumhoehe\" FROM \"Tab_Gebaeude\" WHERE \"ID\" = ?", new DbParam("@g", idGebaeude));
            double? flaeche = g.Rows.Count > 0 ? Zahl(g.Rows[0]["Nutzflaeche"]) : null;
            double? hoehe = g.Rows.Count > 0 ? Zahl(g.Rows[0]["Raumhoehe"]) : null;
            if (idZone.HasValue)
            {
                System.Data.DataTable z = DataRepository.GetDataTable(
                    "SELECT \"Nutzflaeche\", \"Raumhoehe\" FROM \"Tab_Zone\" WHERE \"ID\" = ? AND \"ID_Gebaeude\" = ?",
                    new DbParam("@z", idZone.Value), new DbParam("@g", idGebaeude));
                flaeche = z.Rows.Count > 0 ? Zahl(z.Rows[0]["Nutzflaeche"]) : null;
                if (z.Rows.Count > 0 && Zahl(z.Rows[0]["Raumhoehe"]) is double eigene) hoehe = eigene;
            }
            return (Masz(flaeche), Masz(hoehe));
        }

        private static double? Zahl(object wert)
            => wert == null || wert == DBNull.Value ? null : Convert.ToDouble(wert, CultureInfo.InvariantCulture);

        private Uebernahme Uebernehmen(long idProfil, long idGebaeude, long? idZone, double? flaeche, double? lichteHoehe)
        {
            string bereit = Bereit();
            if (bereit != null) return Uebernahme.Fehler(bereit);
            Raumnutzungsprofil profil = ProfilLesen(idProfil);
            if (profil == null) return Uebernahme.Fehler(Fehlt(MyResource.Resource.RAUMNUTZUNG_MSG_PROFIL_FEHLT, idProfil));

            var kond = new KonditionierungCtrl();
            Konditionierungsarbeitsstand stand = kond.ArbeitsstandLesen(idGebaeude, null, out string gelesen);
            if (stand == null) return Uebernahme.Fehler(gelesen ?? "");
            Konditionierungszone zone = idZone.HasValue ? stand.Zone(idZone.Value) : null;
            if (idZone.HasValue && zone == null)
                return Uebernahme.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RAUMNUTZUNG_MSG_ZONE_FEHLT,
                                                       idGebaeude.ToString(CultureInfo.InvariantCulture),
                                                       idZone.Value.ToString(CultureInfo.InvariantCulture)));

            Anwendung anwendung = ProfilAnwenden(stand, profil, idZone, flaeche, lichteHoehe);
            if (!anwendung.Ok) return Uebernahme.Fehler(anwendung.Meldung);
            stand = anwendung.Stand;
            IReadOnlyList<Uebernahmeposten> posten = anwendung.Posten;
            List<Konditionierungsgroesse> uebernommen = posten.Where(x => x.Uebernommen).Select(x => x.Groesse).ToList();

            string schloss = KonditionierungCtrl.Schloss(KonditionierungCtrl.Eigner.Gebaeude(idGebaeude));
            if (schloss != null) return Uebernahme.Fehler(schloss);
            string nutzung = KonditionierungNutzungSchema.Nutzungstext(profil.Bezeichner);
            using (DbVorgang v = DataRepository.Vorgang())
            using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(v))
            {
                try
                {
                    if (uebernommen.Count > 0)
                    {
                        KonditionierungCtrl.Ergebnis e = kond.StandSchreiben(v, KonditionierungCtrl.Eigner.Gebaeude(idGebaeude), stand.Gebaeude, true, out _);
                        foreach (Konditionierungszone z in stand.Zonen)
                        {
                            if (!e.Ok) break;
                            e = kond.StandSchreiben(v, KonditionierungCtrl.Eigner.Zone(idGebaeude, z.Id), z.Stand, true, out _);
                        }
                        if (!e.Ok)
                        {
                            v.Rollback();
                            return Uebernahme.Fehler(e.Meldung);
                        }
                        KonditionierungCtrl.Eigner ziel = idZone.HasValue
                            ? KonditionierungCtrl.Eigner.Zone(idGebaeude, idZone.Value)
                            : KonditionierungCtrl.Eigner.Gebaeude(idGebaeude);
                        foreach (Konditionierungsgroesse g in uebernommen) KonditionierungCtrl.NutzungSetzen(ziel, g, nutzung);
                    }
                    if (idZone.HasValue && RaumnutzungSchema.ZonenspalteVorhanden())
                        v.Ausfuehren("UPDATE \"" + RaumnutzungSchema.TAB_ZONE + "\" SET \"" + RaumnutzungSchema.SPALTE_ZONE_NUTZUNGSPROFIL +
                                     "\" = ? WHERE \"ID\" = ?", new DbParam("@n", (object)nutzung), new DbParam("@z", idZone.Value));
                    v.Commit();
                }
                catch (Exception ex)
                {
                    v.Rollback();
                    return Uebernahme.Fehler(ex.Message);
                }
            }
            return new Uebernahme(true, null, profil.Bezeichner, posten);
        }

        /// <summary>
        /// Das Ergebnis der reinen Anwendung eines Profils auf einen Arbeitsstand (<see cref="ProfilAnwenden"/>): der neue
        /// Stand, je Größe ein Posten und ob die Gesamtangabe der Lüftung dafür aufgeteilt wurde (E56 F5 (a)).
        /// </summary>
        public sealed record Anwendung(bool Ok, string Meldung, Konditionierungsarbeitsstand Stand,
                                       IReadOnlyList<Uebernahmeposten> Posten, bool Aufgeteilt)
        {
            /// <summary>Benannt abgelehnt; der Stand bleibt.</summary>
            public static Anwendung Fehler(string meldung)
                => new Anwendung(false, meldung ?? "", null, Array.Empty<Uebernahmeposten>(), false);
        }

        /// <summary>
        /// <b>Ein Profil auf einen Arbeitsstand anwenden</b> — rein, ohne Datenbank (Konzept Nutzungsprofile 4.4, 6.2;
        /// NP-F6, NP-F13, NP-F17, NP-F18): je Größe erzeugt der <see cref="Raumnutzungsgenerator"/> die Vorlage,
        /// <see cref="Konditionierungsarbeit.VorlageUebernehmen"/> trägt sie ein (am angelegten Kalender ersetzt sie nur den
        /// Matrixbereich, P12). Eine unbeheizte Zone bekommt weder Heiz- noch Kühlkalender, eine nicht belegte Größe
        /// keinen; die Rückfrage „aufteilen" der Lüftung wird bejaht — der wirksame Luftwechsel bleibt. Derselbe Schritt
        /// trägt <see cref="ProfilUebernehmen(long, long, long?, double?, double?)"/> und den OK-Weg der Editoren (die
        /// Hülle der Konditionierung), damit Vorschau, Rückfrage und Schreiben dasselbe Ergebnis sehen.
        /// </summary>
        /// <param name="stand">Der Arbeitsstand des Gebäudes samt Zonen.</param>
        /// <param name="profil">Das Profil.</param>
        /// <param name="idZone">Die Zone im Arbeitsstand oder <c>null</c> für das Gebäude.</param>
        /// <param name="flaeche">Die Fläche des Ziels für die Nennwerte (Q39, Q40); <c>null</c> = der Nennwert des Ziels bleibt.</param>
        /// <param name="lichteHoehe">Die lichte Höhe des Ziels für Außenluft in m³/(h·m²); <c>null</c> = ohne (NP-F10).</param>
        public static Anwendung ProfilAnwenden(Konditionierungsarbeitsstand stand, Raumnutzungsprofil profil, long? idZone,
                                               double? flaeche, double? lichteHoehe = null)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (profil == null) throw new ArgumentNullException(nameof(profil));
            long? zonenId = idZone;
            Konditionierungszone zone = zonenId.HasValue ? stand.Zone(zonenId.Value) : null;
            if (zonenId.HasValue && zone == null)
                return Anwendung.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RAUMNUTZUNG_MSG_ZONE_FEHLT,
                                                      "—", idZone.Value.ToString(CultureInfo.InvariantCulture)));

            var posten = new List<Uebernahmeposten>();
            bool aufgeteilt = false;
            foreach (Raumnutzungsgroesse r in Raumnutzungsgenerator.Erzeugen(profil, Masz(flaeche), Masz(lichteHoehe)))
            {
                bool unbeheizt = zone != null && !zone.IstBeheizt &&
                                 (r.Groesse == Konditionierungsgroesse.Heizsoll || r.Groesse == Konditionierungsgroesse.Kuehlsoll);
                if (r.Vorlage == null || unbeheizt)
                {
                    posten.Add(new Uebernahmeposten(r.Groesse, r.Weg, false, false, unbeheizt && r.Vorlage != null, null, null, r.Hinweis));
                    continue;
                }
                bool ersetzt = stand.Ebene(zonenId)?.Kalender(r.Groesse) != null;
                var ort = new Konditionierungsort(r.Groesse, zonenId);
                Konditionierungsschritt schritt = Konditionierungsarbeit.VorlageUebernehmen(stand, ort, r.Vorlage);
                if (schritt.Rueckfrage)
                {
                    Konditionierungsschritt geteilt = Konditionierungsarbeit.LuftwechselAufteilen(stand);
                    if (!geteilt.Ok) return Anwendung.Fehler(geteilt.Meldung);
                    schritt = Konditionierungsarbeit.VorlageUebernehmen(geteilt.Stand, ort, r.Vorlage);
                    aufgeteilt = true;
                }
                if (!schritt.Ok || schritt.Rueckfrage) return Anwendung.Fehler(schritt.Meldung ?? "");
                stand = schritt.Stand;
                posten.Add(new Uebernahmeposten(r.Groesse, r.Weg, true, ersetzt, false, r.Nennwert, r.Nennwertherleitung, r.Hinweis));
            }
            return new Anwendung(true, null, stand, posten, aufgeteilt);
        }

        /// <summary>Ein Maß des Ziels (Fläche, Höhe): nur endlich und größer null, sonst „ohne".</summary>
        private static double? Masz(double? wert)
            => wert.HasValue && double.IsFinite(wert.Value) && wert.Value > 0.0 ? wert : null;

        /// <summary>
        /// <b>Die Nutzung der Kalender einer Zone</b> (Konzept Nutzungsprofile 6.2, Kopfzeile des Zonendialogs): der Text
        /// der Spalte <c>Nutzung</c> am ersten Kalender der Zone, der einen trägt — der Profilname oder eine der vier
        /// alten Kennungen; <c>null</c> ohne Kalender mit Nutzung oder ohne die Spalte.
        /// </summary>
        public static string Kalendernutzung(long idGebaeude, long idZone)
        {
            if (!KonditionierungNutzungSchema.SchemaVollstaendig()) return null;
            KonditionierungCtrl.Eigner eigner = KonditionierungCtrl.Eigner.Zone(idGebaeude, idZone);
            object n = DataRepository.ExecuteScalar(
                "SELECT \"Nutzung\" FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE " + eigner.Bedingung() +
                " AND \"Nutzung\" IS NOT NULL ORDER BY \"ID\" LIMIT 1", eigner.Parameter());
            return n == null || n == DBNull.Value ? null : KonditionierungNutzungSchema.Nutzungstext(Convert.ToString(n, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <b>Die Nutzung eines Kalenders, dessen Herkunft ein Profil nennt</b> (NP-F14, NP-F15): Der OK-Weg der Editoren
        /// schreibt die Kalender eines übernommenen Profils über den Arbeitsstand; ihre Herkunft ist der Profilname. Nennt
        /// sie keine Vorlage der Größe, aber ein Profil des Katalogs, ist die Nutzung dieser Name — dasselbe, was
        /// <see cref="ProfilUebernehmen(long, long, long?, double?, double?)"/> setzt. <c>null</c> ohne Herkunft, ohne
        /// Katalog oder ohne ein Profil dieses Namens.
        /// </summary>
        internal static string NutzungDesProfils(DbVorgang v, string bemerkung)
        {
            string name = Kalenderherkunft.AusBemerkung(bemerkung).Vorlage;
            if (string.IsNullOrEmpty(name)) return null;
            object da = v.Skalar("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?",
                                 new DbParam("@t", RaumnutzungSchema.TAB_PROFIL));
            if (da == null || da == DBNull.Value || Convert.ToInt64(da, CultureInfo.InvariantCulture) == 0) return null;
            object n = v.Skalar("SELECT COUNT(*) FROM \"" + RaumnutzungSchema.TAB_PROFIL + "\" WHERE \"Bezeichner\" = ?",
                                new DbParam("@b", name.Trim()));
            return n == null || n == DBNull.Value || Convert.ToInt64(n, CultureInfo.InvariantCulture) == 0
                ? null
                : KonditionierungNutzungSchema.Nutzungstext(name);
        }

        // =================================================================
        //  Prüfungen ohne Datenbank
        // =================================================================

        /// <summary>
        /// <b>Die Prüfung eines Profils</b> ohne Datenbank (NP-F11): Name, Nummer, Beschreibung, jeder Kennwert in den
        /// Grenzen der Größe bzw. der Spalte, jede Zeile des Zeilenbilds (<see cref="Konditionierungsarbeit.Zellenpruefung"/>,
        /// je Größe und Zeile höchstens einmal, ohne <c>NENNWERT</c>/<c>SAISON</c>) und jedes Stundenprofil (24 Werte).
        /// </summary>
        /// <returns><c>null</c> oder die erste benannte Ablehnung.</returns>
        public static string Profilpruefung(Raumnutzungsprofil p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            p.Bezeichner = p.Bezeichner?.Trim();
            if (string.IsNullOrEmpty(p.Bezeichner) || p.Bezeichner.Length > RaumnutzungSchema.BEZEICHNER_MAX_ZEICHEN)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RAUMNUTZUNG_MSG_NAME_LAENGE, RaumnutzungSchema.BEZEICHNER_MAX_ZEICHEN);
            p.Nummer = Leer(p.Nummer);
            if (p.Nummer != null && p.Nummer.Length > RaumnutzungSchema.NUMMER_MAX_ZEICHEN)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RAUMNUTZUNG_MSG_NUMMER_LAENGE, RaumnutzungSchema.NUMMER_MAX_ZEICHEN);
            p.Beschreibung = Leer(p.Beschreibung);
            if (p.Beschreibung != null && p.Beschreibung.Length > RaumnutzungSchema.BESCHREIBUNG_MAX_ZEICHEN)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RAUMNUTZUNG_MSG_TEXT_LAENGE, RaumnutzungSchema.BESCHREIBUNG_MAX_ZEICHEN);

            string m = Stunde("Nutzung_Von", p.Nutzung_Von) ?? Stunde("Nutzung_Bis", p.Nutzung_Bis)
                       ?? Stunde("Betrieb_Von", p.Betrieb_Von) ?? Stunde("Betrieb_Bis", p.Betrieb_Bis);
            if (m != null) return m;
            if (p.Nutzungstage_Woche != null && (p.Nutzungstage_Woche.Length != 7 || p.Nutzungstage_Woche.Any(c => c != '0' && c != '1')))
                return Kennwert("Nutzungstage_Woche", p.Nutzungstage_Woche);
            m = Groesse("Heiz_Soll", p.Heiz_Soll, Konditionierungsgroesse.Heizsoll)
                ?? Groesse("Heiz_Soll_Ausserhalb", p.Heiz_Soll_Ausserhalb, Konditionierungsgroesse.Heizsoll)
                ?? Groesse("Kuehl_Soll", p.Kuehl_Soll, Konditionierungsgroesse.Kuehlsoll)
                ?? Groesse("Kuehl_Soll_Ausserhalb", p.Kuehl_Soll_Ausserhalb, Konditionierungsgroesse.Kuehlsoll)
                ?? Bereich("Aussenluft", p.Aussenluft, 0.0, double.MaxValue)
                ?? Bereich("Aussenluft_Ausserhalb", p.Aussenluft_Ausserhalb, 0.0, double.MaxValue)
                ?? Bereich("Personen_Waerme", p.Personen_Waerme, 0.0, double.MaxValue)
                ?? Bereich("Personen_Anteil", p.Personen_Anteil, 0.0, 1.0)
                ?? Bereich("Personen_Anteil_Ausserhalb", p.Personen_Anteil_Ausserhalb, 0.0, 1.0)
                ?? Bereich("Geraete_Leistung", p.Geraete_Leistung, 0.0, double.MaxValue)
                ?? Bereich("Geraete_Anteil", p.Geraete_Anteil, 0.0, 1.0)
                ?? Bereich("Geraete_Anteil_Ausserhalb", p.Geraete_Anteil_Ausserhalb, 0.0, 1.0)
                ?? Bereich("Beleuchtung_Leistung", p.Beleuchtung_Leistung, 0.0, double.MaxValue)
                ?? Bereich("Beleuchtung_Anteil", p.Beleuchtung_Anteil, 0.0, 1.0);
            if (m != null) return m;
            if (p.Personen_Flaeche.HasValue && !(p.Personen_Flaeche.Value > 0.0 && double.IsFinite(p.Personen_Flaeche.Value)))
                return Kennwert("Personen_Flaeche", p.Personen_Flaeche);
            p.Aussenluft_Einheit = Leer(p.Aussenluft_Einheit);
            if (p.Aussenluft_Einheit != null && p.Aussenluft_Einheit != RaumnutzungSchema.EINHEIT_JE_STUNDE &&
                p.Aussenluft_Einheit != RaumnutzungSchema.EINHEIT_JE_FLAECHE)
                return Kennwert("Aussenluft_Einheit", p.Aussenluft_Einheit);
            if ((p.Aussenluft.HasValue || p.Aussenluft_Ausserhalb.HasValue) && p.Aussenluft_Einheit == null)
                return Kennwert("Aussenluft_Einheit", null);

            var zeilen = new HashSet<string>(StringComparer.Ordinal);
            foreach (Vorgabezeile z in p.Zeilen ?? new List<Vorgabezeile>())
            {
                if (z == null || !Konditionierungsgroessen.AusKennwort(z.Groesse, out Konditionierungsgroesse g) ||
                    !RaumnutzungSchema.ZEILEN.Contains(z.Zeile) || !zeilen.Add(z.Groesse + "/" + z.Zeile))
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RAUMNUTZUNG_MSG_ZEILE, z?.Groesse, z?.Zeile, "");
                string f = Konditionierungsarbeit.Zellenpruefung(g, z.Zeile, Konditionierungsstand.Zelle(z));
                if (f != null) return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RAUMNUTZUNG_MSG_ZEILE, z.Groesse, z.Zeile, f);
            }
            var stunden = new HashSet<string>(StringComparer.Ordinal);
            foreach (Raumnutzungsstunden s in p.Stunden ?? new List<Raumnutzungsstunden>())
            {
                bool gut = s != null && Konditionierungsgroessen.AusKennwort(s.Groesse, out Konditionierungsgroesse g) &&
                           (s.Tagesart == RaumnutzungSchema.TAGESART_WERKTAG || s.Tagesart == RaumnutzungSchema.TAGESART_FREI) &&
                           stunden.Add(s.Groesse + "/" + s.Tagesart) &&
                           (s.Werte?.Length ?? 0) <= RaumnutzungSchema.STUNDENWERTE_MAX_ZEICHEN &&
                           Raumnutzungsgenerator.Stundenwerte(s.Werte, g) != null;
                if (!gut) return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RAUMNUTZUNG_MSG_STUNDEN, s?.Groesse, s?.Tagesart);
            }
            return null;
        }

        // =================================================================
        //  Schreiben
        // =================================================================

        private static readonly string SQL_PROFIL =
            "SELECT \"ID\", \"ID_Katalog\", \"Nummer\", \"Bezeichner\", \"Beschreibung\", \"ReadOnly\", " +
            string.Join(", ", RaumnutzungSchema.SPALTEN_KENNWERTE.Select(s => "\"" + s.Spalte + "\"")) +
            " FROM \"" + RaumnutzungSchema.TAB_PROFIL + "\"";

        private const string SQL_PROFIL_ORDNUNG = "ORDER BY \"ID_Katalog\", IIF(\"Nummer\" IS NULL, 1, 0), \"Nummer\" COLLATE NOCASE, \"Bezeichner\" COLLATE NOCASE, \"ID\"";

        private static long KategorieEinfuegen(DbVorgang v, string bezeichner, string beschreibung, string quellenhinweis)
        {
            long reihe = Lang(v.Skalar("SELECT MAX(\"Reihenfolge\") FROM \"" + RaumnutzungSchema.TAB_KATALOG + "\"")) ?? 0;
            v.Ausfuehren("INSERT INTO \"" + RaumnutzungSchema.TAB_KATALOG + "\" (\"Bezeichner\", \"Art\", \"Beschreibung\", " +
                         "\"Quellenhinweis\", \"ReadOnly\", \"Reihenfolge\") VALUES (?, ?, ?, ?, 0, ?)",
                         new DbParam("@b", bezeichner), new DbParam("@a", RaumnutzungSchema.ART_EIGEN),
                         new DbParam("@be", (object)beschreibung), new DbParam("@q", (object)quellenhinweis),
                         new DbParam("@r", reihe + 1));
            return Lang(v.Skalar("SELECT last_insert_rowid()")) ?? 0;
        }

        /// <summary>Die Spalten von Kopf und Kennwerten in der Ordnung von <see cref="KopfParameter"/>.</summary>
        private static readonly IReadOnlyList<string> SPALTEN_ALLE =
            new[] { "ID_Katalog", "Nummer", "Bezeichner", "Beschreibung", "ReadOnly" }
                .Concat(RaumnutzungSchema.SPALTEN_KENNWERTE.Select(s => s.Spalte)).ToList();

        private static List<DbParam> KopfParameter(Raumnutzungsprofil p)
        {
            var par = new List<DbParam>
            {
                new DbParam("@k", p.IdKatalog), new DbParam("@n", (object)p.Nummer), new DbParam("@b", p.Bezeichner),
                new DbParam("@be", (object)p.Beschreibung), new DbParam("@ro", p.Ausgeliefert ? 1 : 0),
            };
            IReadOnlyList<object> kennwerte = p.Kennwerte();
            for (int i = 0; i < kennwerte.Count; i++)
            {
                object w = kennwerte[i] is bool b ? (b ? 1 : 0) : kennwerte[i];
                par.Add(new DbParam("@w" + i.ToString(CultureInfo.InvariantCulture), w));
            }
            return par;
        }

        /// <summary>Schreibt Kopf, Kennwerte, Zeilenbild und Stunden als neues Profil.</summary>
        private static long ProfilEinfuegen(DbVorgang v, Raumnutzungsprofil p)
        {
            List<DbParam> par = KopfParameter(p);
            v.Ausfuehren("INSERT INTO \"" + RaumnutzungSchema.TAB_PROFIL + "\" (" + string.Join(", ", SPALTEN_ALLE.Select(c => "\"" + c + "\"")) +
                         ") VALUES (" + string.Join(", ", Enumerable.Repeat("?", par.Count)) + ")", par.ToArray());
            long neu = Lang(v.Skalar("SELECT last_insert_rowid()")) ?? 0;
            InhaltEinfuegen(v, p, neu);
            return neu;
        }

        private static void InhaltEinfuegen(DbVorgang v, Raumnutzungsprofil p, long neu)
        {
            foreach (Vorgabezeile z in p.Zeilen ?? new List<Vorgabezeile>())
                v.Ausfuehren("INSERT INTO \"" + RaumnutzungSchema.TAB_ZEILE + "\" (\"ID_Profil\", \"Groesse\", \"Zeile\", \"Wert\", \"Aus\", " +
                             "\"Von\", \"Bis\", \"Bedingt_K\") VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                             new DbParam("@p", neu), new DbParam("@g", z.Groesse), new DbParam("@z", z.Zeile),
                             new DbParam("@w", (object)(z.Aus ? null : z.Wert)), new DbParam("@a", z.Aus ? 1 : 0),
                             new DbParam("@v", (object)z.Von), new DbParam("@bi", (object)z.Bis), new DbParam("@dk", (object)z.BedingtK));
            foreach (Raumnutzungsstunden s in p.Stunden ?? new List<Raumnutzungsstunden>())
                v.Ausfuehren("INSERT INTO \"" + RaumnutzungSchema.TAB_STUNDEN + "\" (\"ID_Profil\", \"Groesse\", \"Tagesart\", \"Werte\") " +
                             "VALUES (?, ?, ?, ?)",
                             new DbParam("@p", neu), new DbParam("@g", s.Groesse), new DbParam("@t", s.Tagesart), new DbParam("@w", s.Werte));
        }

        /// <summary>Ein Schreibweg in einem Vorgang; eine Ausnahme wird benannt, nichts bleibt halb stehen.</summary>
        private static Ergebnis Schreibe(Func<DbVorgang, Ergebnis> arbeit)
        {
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    Ergebnis e = arbeit(v);
                    if (e.Ok) v.Commit();
                    else v.Rollback();
                    return e;
                }
                catch (Exception ex)
                {
                    v.Rollback();
                    return Ergebnis.Fehler(ex.Message);
                }
            }
        }

        // =================================================================
        //  Lesen (Helfer)
        // =================================================================

        private static void Inhalt(List<Raumnutzungsprofil> profile)
        {
            if (profile.Count == 0) return;
            Dictionary<long, Raumnutzungsprofil> je = profile.ToDictionary(p => p.Id);
            DataTable z = DataRepository.GetDataTable(
                "SELECT \"ID_Profil\", \"Groesse\", \"Zeile\", \"Wert\", \"Aus\", \"Von\", \"Bis\", \"Bedingt_K\" FROM \"" +
                RaumnutzungSchema.TAB_ZEILE + "\" ORDER BY \"ID_Profil\", \"ID\"");
            if (z != null)
                foreach (DataRow r in z.Rows)
                    if (je.TryGetValue(Lang(r["ID_Profil"]) ?? 0, out Raumnutzungsprofil p))
                        p.Zeilen.Add(new Vorgabezeile
                        {
                            Groesse = Text(r, "Groesse"), Zeile = Text(r, "Zeile"), Wert = Zahl(r, "Wert"),
                            Aus = (Lang(r["Aus"]) ?? 0) == 1, Von = Ganz(r, "Von"), Bis = Ganz(r, "Bis"), BedingtK = Zahl(r, "Bedingt_K"),
                        });
            DataTable s = DataRepository.GetDataTable(
                "SELECT \"ID_Profil\", \"Groesse\", \"Tagesart\", \"Werte\" FROM \"" + RaumnutzungSchema.TAB_STUNDEN +
                "\" ORDER BY \"ID_Profil\", \"ID\"");
            if (s != null)
                foreach (DataRow r in s.Rows)
                    if (je.TryGetValue(Lang(r["ID_Profil"]) ?? 0, out Raumnutzungsprofil p))
                        p.Stunden.Add(new Raumnutzungsstunden(Text(r, "Groesse"), Text(r, "Tagesart"), Text(r, "Werte")));
        }

        private static Raumnutzungsprofil ProfilAus(DataRow r) => new Raumnutzungsprofil
        {
            Id = Lang(r["ID"]) ?? 0,
            IdKatalog = Lang(r["ID_Katalog"]) ?? 0,
            Nummer = Text(r, "Nummer"),
            Bezeichner = Text(r, "Bezeichner"),
            Beschreibung = Text(r, "Beschreibung"),
            Ausgeliefert = (Lang(r["ReadOnly"]) ?? 0) == 1,
            Nutzung_Von = Ganz(r, "Nutzung_Von"),
            Nutzung_Bis = Ganz(r, "Nutzung_Bis"),
            Betrieb_Von = Ganz(r, "Betrieb_Von"),
            Betrieb_Bis = Ganz(r, "Betrieb_Bis"),
            Nutzungstage_Woche = Text(r, "Nutzungstage_Woche"),
            Nutzungstage_Jahr = Ganz(r, "Nutzungstage_Jahr"),
            Feiertage_Wie_Sonntag = Bit(r, "Feiertage_Wie_Sonntag"),
            Heiz_Soll = Zahl(r, "Heiz_Soll"),
            Heiz_Soll_Ausserhalb = Zahl(r, "Heiz_Soll_Ausserhalb"),
            Heiz_Aus_Ausserhalb = Bit(r, "Heiz_Aus_Ausserhalb"),
            Kuehl_Soll = Zahl(r, "Kuehl_Soll"),
            Kuehl_Soll_Ausserhalb = Zahl(r, "Kuehl_Soll_Ausserhalb"),
            Kuehl_Aus_Ausserhalb = Bit(r, "Kuehl_Aus_Ausserhalb"),
            Aussenluft = Zahl(r, "Aussenluft"),
            Aussenluft_Einheit = Text(r, "Aussenluft_Einheit"),
            Aussenluft_Ausserhalb = Zahl(r, "Aussenluft_Ausserhalb"),
            Personen_Flaeche = Zahl(r, "Personen_Flaeche"),
            Personen_Waerme = Zahl(r, "Personen_Waerme"),
            Personen_Anteil = Zahl(r, "Personen_Anteil"),
            Personen_Anteil_Ausserhalb = Zahl(r, "Personen_Anteil_Ausserhalb"),
            Geraete_Leistung = Zahl(r, "Geraete_Leistung"),
            Geraete_Anteil = Zahl(r, "Geraete_Anteil"),
            Geraete_Anteil_Ausserhalb = Zahl(r, "Geraete_Anteil_Ausserhalb"),
            Beleuchtung_Leistung = Zahl(r, "Beleuchtung_Leistung"),
            Beleuchtung_Anteil = Zahl(r, "Beleuchtung_Anteil"),
        };

        /// <summary>Kopf eines Profils ohne Inhalt (Prüfungen).</summary>
        private static Raumnutzungsprofil ProfilKopf(long id)
        {
            DataTable t = DataRepository.GetDataTable(SQL_PROFIL + " WHERE \"ID\" = ?", new DbParam("@id", id));
            return t == null || t.Rows.Count == 0 ? null : ProfilAus(t.Rows[0]);
        }

        private static Kategorie KategorieAus(DataRow r)
            => new Kategorie(Lang(r["ID"]) ?? 0, Text(r, "Bezeichner"), Text(r, "Art"), Text(r, "Beschreibung"),
                             Text(r, "Quellenhinweis"), (Lang(r["ReadOnly"]) ?? 0) == 1, Ganz(r, "Reihenfolge"));

        private static Zuordnung ZuordnungAus(DataRow r)
            => new Zuordnung(Lang(r["ID"]) ?? 0, Text(r, "Art"), Text(r, "Schluessel"), Lang(r["ID_Profil"]),
                             (Lang(r["ReadOnly"]) ?? 0) == 1);

        // =================================================================
        //  Regeln (Helfer)
        // =================================================================

        private static string Bereit()
            => Lesbar() ? null : string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RAUMNUTZUNG_MSG_SCHEMA_FEHLT,
                                               RaumnutzungSchema.SCHRITT.ToString(CultureInfo.InvariantCulture));

        private string ZielkategoriePruefen(long idKatalog)
        {
            Kategorie k = KategorieLesen(idKatalog);
            if (k == null) return Fehlt(MyResource.Resource.RAUMNUTZUNG_MSG_KATEGORIE_FEHLT, idKatalog);
            return k.Ausgeliefert ? Ausgeliefert(k.Bezeichner) : null;
        }

        /// <summary>Name und Nummer je Kategorie eindeutig ohne Unterschied der Schreibung (NP-F5, NP-F20).</summary>
        private string Eindeutigkeit(Raumnutzungsprofil p)
        {
            foreach (Raumnutzungsprofil n in Profile(p.IdKatalog))
            {
                if (n.Id == p.Id) continue;
                if (string.Equals(n.Bezeichner, p.Bezeichner, StringComparison.OrdinalIgnoreCase))
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RAUMNUTZUNG_MSG_NAME_DOPPELT, p.Bezeichner);
                if (p.Nummer != null && string.Equals(n.Nummer, p.Nummer, StringComparison.OrdinalIgnoreCase))
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RAUMNUTZUNG_MSG_NUMMER_DOPPELT, p.Nummer);
            }
            return null;
        }

        private string KategorieTexte(ref string bezeichner, ref string beschreibung, ref string quellenhinweis, long ausser)
        {
            bezeichner = bezeichner?.Trim();
            beschreibung = Leer(beschreibung);
            quellenhinweis = Leer(quellenhinweis);
            if (string.IsNullOrEmpty(bezeichner) || bezeichner.Length > RaumnutzungSchema.BEZEICHNER_MAX_ZEICHEN)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RAUMNUTZUNG_MSG_NAME_LAENGE, RaumnutzungSchema.BEZEICHNER_MAX_ZEICHEN);
            if ((beschreibung?.Length ?? 0) > RaumnutzungSchema.BESCHREIBUNG_MAX_ZEICHEN ||
                (quellenhinweis?.Length ?? 0) > RaumnutzungSchema.BESCHREIBUNG_MAX_ZEICHEN)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RAUMNUTZUNG_MSG_TEXT_LAENGE, RaumnutzungSchema.BESCHREIBUNG_MAX_ZEICHEN);
            string name = bezeichner;
            if (Kategorien().Any(k => k.Id != ausser && string.Equals(k.Bezeichner, name, StringComparison.OrdinalIgnoreCase)))
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RAUMNUTZUNG_MSG_NAME_DOPPELT, bezeichner);
            return null;
        }

        /// <summary>Der Wunschname oder „Quelle (Kopie)", eindeutig gemacht (dieselbe Regel wie bei den Vorlagen).</summary>
        private static string Wunschname(string wunsch, string quelle, IReadOnlyCollection<string> namen)
        {
            string w = (wunsch ?? "").Trim();
            if (w.Length == 0) w = KonditionierungsvorlageCtrl.Kopiename(quelle);
            return KonditionierungsvorlageCtrl.EindeutigerName(namen, w);
        }

        private static string Stunde(string spalte, int? s) => s.HasValue && (s < 0 || s > 24) ? Kennwert(spalte, s) : null;

        private static string Groesse(string spalte, double? w, Konditionierungsgroesse g)
            => w.HasValue && !Konditionierungsgroessen.ImBereich(g, w.Value) ? Kennwert(spalte, w) : null;

        private static string Bereich(string spalte, double? w, double min, double max)
            => w.HasValue && !(double.IsFinite(w.Value) && w.Value >= min && w.Value <= max) ? Kennwert(spalte, w) : null;

        private static string Kennwert(string spalte, object wert)
            => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RAUMNUTZUNG_MSG_KENNWERT, spalte,
                             Convert.ToString(wert, CultureInfo.InvariantCulture) ?? "—");

        private static string Fehlt(string muster, long id)
            => string.Format(CultureInfo.CurrentCulture, muster, id.ToString(CultureInfo.InvariantCulture));

        private static string Ausgeliefert(string name)
            => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RAUMNUTZUNG_MSG_AUSGELIEFERT, name);

        private static string Leer(string t) => string.IsNullOrWhiteSpace(t) ? null : t.Trim();

        private static long? Lang(object w)
            => w == null || w == DBNull.Value ? null : Convert.ToInt64(w, CultureInfo.InvariantCulture);

        private static int? Ganz(DataRow r, string spalte)
            => r[spalte] == DBNull.Value ? null : Convert.ToInt32(r[spalte], CultureInfo.InvariantCulture);

        private static double? Zahl(DataRow r, string spalte)
            => r[spalte] == DBNull.Value ? null : Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture);

        private static bool? Bit(DataRow r, string spalte)
            => r[spalte] == DBNull.Value ? null : Convert.ToInt64(r[spalte], CultureInfo.InvariantCulture) == 1;

        private static string Text(DataRow r, string spalte)
            => r[spalte] == DBNull.Value ? null : Convert.ToString(r[spalte], CultureInfo.InvariantCulture);

        #region CSV-Import und -Export (Stufe NP4a; Konzept Nutzungsprofile 6.1, 6.4, NP-F11, NP-F20)

        /// <summary>Der Export einer Kategorie: die Bytes des CSV-Formats (<see cref="RaumnutzungCsv"/>) oder die benannte Ablehnung.</summary>
        public sealed record CsvAusgabe(bool Ok, string Meldung, string Kategorie, int Profile, byte[] Inhalt);

        /// <summary>Was „Übernehmen" geschrieben hat; <c>IdKatalog</c> ist die Zielkategorie (auch eine neu angelegte).</summary>
        public sealed record CsvBilanz(bool Ok, string Meldung, long IdKatalog, int Angelegt, int Ersetzt, int Uebersprungen, int Abgelehnt);

        /// <summary>
        /// Schreibt eine Kategorie — auch eine ausgelieferte — im CSV-Format 6.4: alle Profile mit Kennwerten, Stundenprofilen
        /// und Zeilenbild; <c>Nutzungstage_Jahr</c> nie (E93). Eine Kategorie ohne Profile ergibt die Kopfzeile allein.
        /// </summary>
        public CsvAusgabe CsvExportieren(long idKatalog)
        {
            string bereit = Bereit();
            if (bereit != null) return new CsvAusgabe(false, bereit, null, 0, null);
            Kategorie k = KategorieLesen(idKatalog);
            if (k == null) return new CsvAusgabe(false, Fehlt(MyResource.Resource.RAUMNUTZUNG_MSG_KATEGORIE_FEHLT, idKatalog), null, 0, null);
            List<Raumnutzungsprofil> profile = Profile(idKatalog);
            return new CsvAusgabe(true, null, k.Bezeichner, profile.Count, RaumnutzungCsv.SchreibenBytes(profile));
        }

        /// <summary>Liest die Bytes einer Datei und gleicht sie mit der Zielkategorie ab (<c>null</c> = neue Kategorie); schreibt nichts.</summary>
        public RaumnutzungCsvLesung CsvPruefen(byte[] inhalt, long? idKatalog)
        {
            RaumnutzungCsvLesung lesung = RaumnutzungCsv.Lesen(inhalt);
            CsvAbgleichen(lesung, idKatalog);
            return lesung;
        }

        /// <summary>
        /// Gleicht die Zeilen mit der Zielkategorie ab: ein gleichnamiges Profil (ohne Unterschied der Schreibung) heißt
        /// „vorhanden" — die Rückfrage ersetzen/überspringen führt der Aufrufer; eine Nummer, die dort ein anderes Profil trägt,
        /// lehnt die Zeile benannt ab (NP-F5). Jeder Abgleich ersetzt den vorigen.
        /// </summary>
        public void CsvAbgleichen(RaumnutzungCsvLesung lesung, long? idKatalog)
        {
            if (lesung == null) throw new ArgumentNullException(nameof(lesung));
            lesung.Zielmeldungen.Clear();
            foreach (RaumnutzungCsvZeile z in lesung.Zeilen)
            {
                z.Zielablehnung = null;
                z.Vorhanden = null;
            }
            if (lesung.Abbruch != null || idKatalog is not long id || !Lesbar()) return;
            List<Raumnutzungsprofil> ziel = Profile(id);
            foreach (RaumnutzungCsvZeile z in lesung.Zeilen.Where(x => x.Ablehnung == null))
            {
                Raumnutzungsprofil gleich = ziel.FirstOrDefault(p => string.Equals(p.Bezeichner, z.Profil.Bezeichner, StringComparison.OrdinalIgnoreCase));
                z.Vorhanden = gleich?.Id;
                if (z.Profil.Nummer == null) continue;
                Raumnutzungsprofil andere = ziel.FirstOrDefault(p => p.Id != (gleich?.Id ?? 0) &&
                                                                     string.Equals(p.Nummer, z.Profil.Nummer, StringComparison.OrdinalIgnoreCase));
                if (andere == null) continue;
                z.Zielablehnung = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RNP_CSV_GRUND_NUMMER_VORHANDEN,
                                                z.Profil.Nummer, andere.Bezeichner);
                lesung.Zielmeldungen.Add(new RaumnutzungCsvMeldung(z.Zeile, RaumnutzungCsv.SPALTE_NUMMER, RaumnutzungCsvArt.Fehler, z.Zielablehnung));
            }
        }

        /// <summary>
        /// <b>Übernehmen</b>: schreibt die übernehmbaren Zeilen in eine eigene Zielkategorie (<paramref name="idKatalog"/>) oder in
        /// eine neue (<paramref name="neueKategorie"/>, Art <c>EIGEN</c>) — nie in eine ausgelieferte. Ein vorhandenes Profil gleichen
        /// Namens wird nach <paramref name="ersetzen"/> an Ort und Stelle ersetzt (die Zuordnungen zeigen weiter darauf) oder
        /// übersprungen. Ein Vorgang: Gelingt eine Zeile nicht, bleibt nichts geschrieben.
        /// </summary>
        public CsvBilanz CsvUebernehmen(RaumnutzungCsvLesung lesung, long? idKatalog, string neueKategorie, bool ersetzen)
        {
            if (lesung == null) throw new ArgumentNullException(nameof(lesung));
            string bereit = Bereit();
            if (bereit != null) return CsvAbgelehnt(bereit);
            if (lesung.Abbruch != null) return CsvAbgelehnt(lesung.Abbruch);
            string name = neueKategorie, beschreibung = null, quelle = null;
            string m = idKatalog is long id ? ZielkategoriePruefen(id) : KategorieTexte(ref name, ref beschreibung, ref quelle, 0);
            if (m != null) return CsvAbgelehnt(m);
            CsvAbgleichen(lesung, idKatalog);
            List<RaumnutzungCsvZeile> zeilen = lesung.Uebernehmbare.ToList();
            int abgelehnt = lesung.Zeilen.Count - zeilen.Count;
            if (zeilen.Count == 0) return CsvAbgelehnt(MyResource.Resource.RNP_CSV_MSG_NICHTS);

            int angelegt = 0, ersetzt = 0, uebersprungen = 0;
            Ergebnis e = Schreibe(v =>
            {
                long ziel = idKatalog ?? KategorieEinfuegen(v, name, beschreibung, quelle);
                foreach (RaumnutzungCsvZeile z in zeilen)
                {
                    Raumnutzungsprofil p = z.Profil.Kopie();
                    p.IdKatalog = ziel;
                    p.Ausgeliefert = false;
                    p.Id = z.Vorhanden ?? 0;
                    string f = Profilpruefung(p);
                    if (f != null) return Ergebnis.Fehler(f);
                    if (z.Vorhanden.HasValue)
                    {
                        if (!ersetzen)
                        {
                            uebersprungen++;
                            continue;
                        }
                        CsvErsetzen(v, p);
                        ersetzt++;
                        continue;
                    }
                    ProfilEinfuegen(v, p);
                    angelegt++;
                }
                return Ergebnis.MitId(ziel);
            });
            if (!e.Ok) return CsvAbgelehnt(e.Meldung);
            return new CsvBilanz(true, string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RNP_CSV_MSG_ERGEBNIS,
                                                     angelegt, ersetzt, uebersprungen, abgelehnt),
                                 e.Id, angelegt, ersetzt, uebersprungen, abgelehnt);
        }

        private static CsvBilanz CsvAbgelehnt(string meldung) => new CsvBilanz(false, meldung ?? "", 0, 0, 0, 0, 0);

        /// <summary>Ersetzt Kopf, Kennwerte, Zeilenbild und Stunden eines eigenen Profils an Ort und Stelle (wie <see cref="ProfilAendern"/>).</summary>
        private static void CsvErsetzen(DbVorgang v, Raumnutzungsprofil p)
        {
            List<DbParam> par = KopfParameter(p);
            par.Add(new DbParam("@id", p.Id));
            v.Ausfuehren("UPDATE \"" + RaumnutzungSchema.TAB_PROFIL + "\" SET " +
                         string.Join(", ", SPALTEN_ALLE.Select(c => "\"" + c + "\" = ?")) + " WHERE \"ID\" = ? AND \"ReadOnly\" = 0", par.ToArray());
            v.Ausfuehren("DELETE FROM \"" + RaumnutzungSchema.TAB_ZEILE + "\" WHERE \"ID_Profil\" = ?", new DbParam("@id", p.Id));
            v.Ausfuehren("DELETE FROM \"" + RaumnutzungSchema.TAB_STUNDEN + "\" WHERE \"ID_Profil\" = ?", new DbParam("@id", p.Id));
            InhaltEinfuegen(v, p, p.Id);
        }

        #endregion

        #region Profile aus einer Projektdatei (Stufe NP4b, Q46, NP-F21)

        /// <summary>
        /// Was die Übernahme der Profile einer Projektdatei ergab: die Kategorie, je Profil neu, ersetzt, unverändert
        /// (vorhanden, „Ergänzen") oder übersprungen (benannt), beim „Ersetzen" entfernte Profile und die Zuordnungen, die
        /// dadurch auf „keine" stehen.
        /// </summary>
        public sealed record Projektdateiuebernahme(bool Ok, string Meldung, long IdKategorie, int Neu, int Ersetzt, int Unveraendert,
                                                    int Entfernt, int ZuordnungenGeloest, IReadOnlyList<string> Meldungen)
        {
            /// <summary>Die benannte Ablehnung; nichts geschrieben.</summary>
            public static Projektdateiuebernahme Fehler(string meldung)
                => new Projektdateiuebernahme(false, meldung ?? "", 0, 0, 0, 0, 0, 0, Array.Empty<string>());
        }

        /// <summary>Die Kategorie mit diesem Namen (ohne Unterschied der Schreibung); <c>null</c> = keine.</summary>
        public Kategorie KategorieMitNamen(string bezeichner)
        {
            string name = (bezeichner ?? "").Trim();
            return name.Length == 0 ? null
                : Kategorien().FirstOrDefault(k => string.Equals(k.Bezeichner, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Steht ein Profil der Projektdatei schon in der Kategorie? Schlüssel ist die Nummer (ohne Unterschied der Schreibung),
        /// ohne Nummer der Name.
        /// </summary>
        public static bool GleichesProjektdateiprofil(Raumnutzungsprofil vorhanden, Raumnutzungsprofil neu)
        {
            if (vorhanden == null || neu == null) return false;
            return neu.Nummer != null
                ? string.Equals(vorhanden.Nummer, neu.Nummer, StringComparison.OrdinalIgnoreCase)
                : vorhanden.Nummer == null && string.Equals(vorhanden.Bezeichner, neu.Bezeichner, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// <b>Übernimmt die Profile einer Projektdatei in ihre eigene Kategorie</b> (Q46, NP4b) — in einem Vorgang. Gibt es die
        /// Kategorie nicht, entsteht sie (Art <c>EIGEN</c>) mit allen Profilen. Gibt es sie, ergänzt die Übernahme nur die
        /// Nummern, die noch fehlen (<paramref name="ersetzen"/> = <c>false</c>), oder schreibt die Werte der Datei an Ort und
        /// Stelle (dieselben Ids, die Zuordnungen zeigen weiter darauf), legt neue an und entfernt Profile, die die Datei nicht
        /// mehr führt — Zuordnungen darauf werden „keine" (NP-F19). Wiederholte Übernahme doppelt nichts. Die Zuordnung der
        /// DIN-Nummern stellt die Übernahme nicht um.
        /// </summary>
        public Projektdateiuebernahme ProjektdateiUebernehmen(Projektdateiprofile satz, bool ersetzen)
        {
            if (satz == null) throw new ArgumentNullException(nameof(satz));
            if (satz.Abgelehnt) return Projektdateiuebernahme.Fehler(satz.Ablehnung);
            string bereit = Bereit();
            if (bereit != null) return Projektdateiuebernahme.Fehler(bereit);
            if (satz.Profile.Count == 0) return Projektdateiuebernahme.Fehler(MyResource.Resource.RNP_PD_KEINE);

            Kategorie k = KategorieMitNamen(satz.Kategorie);
            if (k != null && k.Ausgeliefert) return Projektdateiuebernahme.Fehler(Ausgeliefert(k.Bezeichner));
            string bezeichner = satz.Kategorie, beschreibung = null, quelle = satz.Quellenhinweis;
            if (k == null)
            {
                string m = KategorieTexte(ref bezeichner, ref beschreibung, ref quelle, 0);
                if (m != null) return Projektdateiuebernahme.Fehler(m);
            }

            var meldungen = new List<string>();
            var neue = new List<Raumnutzungsprofil>();
            foreach (Projektdateiprofil q in satz.Profile)
            {
                Raumnutzungsprofil p = q?.Profil?.Kopie();
                if (p == null) continue;
                p.Id = 0;
                p.Ausgeliefert = false;
                string m = Profilpruefung(p);
                if (m != null) meldungen.Add((p.Nummer ?? p.Bezeichner) + ": " + m);
                else neue.Add(p);
            }
            List<Raumnutzungsprofil> vorhanden = k == null ? new List<Raumnutzungsprofil>() : Profile(k.Id);
            int neu = 0, ersetzt = 0, gleich = 0, entfernt = 0, geloest = 0;
            long idKategorie = k?.Id ?? 0;
            Ergebnis e = Schreibe(v =>
            {
                if (idKategorie == 0) idKategorie = KategorieEinfuegen(v, bezeichner, beschreibung, quelle);
                var namen = vorhanden.Select(x => x.Bezeichner).ToList();
                var getroffen = new HashSet<long>();
                foreach (Raumnutzungsprofil p in neue)
                {
                    p.IdKatalog = idKategorie;
                    Raumnutzungsprofil alt = vorhanden.FirstOrDefault(x => !getroffen.Contains(x.Id) && GleichesProjektdateiprofil(x, p));
                    if (alt == null)
                    {
                        p.Bezeichner = KonditionierungsvorlageCtrl.EindeutigerName(namen, p.Bezeichner);
                        namen.Add(p.Bezeichner);
                        ProfilEinfuegen(v, p);
                        neu++;
                        continue;
                    }
                    getroffen.Add(alt.Id);
                    if (!ersetzen || alt.Ausgeliefert)
                    {
                        gleich++;
                        continue;
                    }
                    namen.Remove(alt.Bezeichner);
                    p.Id = alt.Id;
                    p.Bezeichner = KonditionierungsvorlageCtrl.EindeutigerName(namen, p.Bezeichner);
                    namen.Add(p.Bezeichner);
                    ProjektdateiprofilErsetzen(v, p);
                    ersetzt++;
                }
                if (ersetzen)
                    foreach (Raumnutzungsprofil x in vorhanden.Where(x => !getroffen.Contains(x.Id) && !x.Ausgeliefert))
                    {
                        geloest += v.Ausfuehren("UPDATE \"" + RaumnutzungSchema.TAB_ZUORDNUNG + "\" SET \"ID_Profil\" = NULL WHERE \"ID_Profil\" = ?",
                                                new DbParam("@p", x.Id));
                        v.Ausfuehren("DELETE FROM \"" + RaumnutzungSchema.TAB_PROFIL + "\" WHERE \"ID\" = ?", new DbParam("@id", x.Id));
                        entfernt++;
                    }
                return Ergebnis.MitId(idKategorie);
            });
            if (!e.Ok) return Projektdateiuebernahme.Fehler(e.Meldung);
            if (geloest > 0)
                meldungen.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RNP_PD_MSG_ZUORDNUNG_GELOEST,
                                            geloest.ToString(CultureInfo.CurrentCulture)));
            return new Projektdateiuebernahme(true, null, e.Id, neu, ersetzt, gleich, entfernt, geloest, meldungen);
        }

        /// <summary>Schreibt Kopf, Kennwerte, Zeilenbild und Stunden eines vorhandenen Profils an Ort und Stelle neu (dieselbe Id).</summary>
        private static void ProjektdateiprofilErsetzen(DbVorgang v, Raumnutzungsprofil p)
        {
            List<DbParam> par = KopfParameter(p);
            par.Add(new DbParam("@id", p.Id));
            v.Ausfuehren("UPDATE \"" + RaumnutzungSchema.TAB_PROFIL + "\" SET " +
                         string.Join(", ", SPALTEN_ALLE.Select(c => "\"" + c + "\" = ?")) + " WHERE \"ID\" = ?", par.ToArray());
            v.Ausfuehren("DELETE FROM \"" + RaumnutzungSchema.TAB_ZEILE + "\" WHERE \"ID_Profil\" = ?", new DbParam("@id", p.Id));
            v.Ausfuehren("DELETE FROM \"" + RaumnutzungSchema.TAB_STUNDEN + "\" WHERE \"ID_Profil\" = ?", new DbParam("@id", p.Id));
            InhaltEinfuegen(v, p, p.Id);
        }

        #endregion

        #region NP4c — Vorschau und Vorschläge des Profileditors (Konzept Nutzungsprofile 6.1, NP-F7, NP-F9, 4.3)

        // Rein, ohne Datenbank: Was der Editor „Zeitverlauf je Größe" zeigt und vorschlägt, rechnet derselbe Schritt wie die
        // Übernahme (ProfilAnwenden) an einem leeren Gebäude mit einer NEUTRALEN Ferienlage — so sieht die Vorschau, was ein
        // Ziel ohne eigene Kalender bekäme. Die Ferienlage ist ein Phantasiewert (zwei Wochen im August), keine Normangabe.

        /// <summary>Erster Ferientag der neutralen Ferienlage der Vorschau (Jahrestag, 1. August im Gemeinjahr).</summary>
        public const int VORSCHAU_FERIEN_BEGINN = 213;

        /// <summary>Letzter Ferientag der neutralen Ferienlage der Vorschau (Jahrestag, 14. August im Gemeinjahr).</summary>
        public const int VORSCHAU_FERIEN_ENDE = 226;

        /// <summary>Heizsollwert des neutralen Vorschauziels [°C] — ein runder Phantasiewert, keine Normangabe.</summary>
        public const double VORSCHAU_SOLL_TAG = 20.0;

        /// <summary>Kühlsollwert des neutralen Vorschauziels [°C] — ein runder Phantasiewert, keine Normangabe.</summary>
        public const double VORSCHAU_KUEHL_SOLL = 26.0;

        /// <summary>Gerätelast des neutralen Vorschauziels [W] — ein runder Phantasiewert, keine Normangabe.</summary>
        public const double VORSCHAU_GERAETE_W = 100.0;

        /// <summary>Der Tag (ab 0), ab dem die Vorschau ihre Woche sucht: der erste Montag danach liegt Mitte Januar ohne Feiertag.</summary>
        private const int VORSCHAU_WOCHE_AB_TAG = 14;

        /// <summary>
        /// Das Ergebnis der Vorschau einer Größe: der Kalender, den ein leeres Ziel bekäme, das Bezugsjahr und eine typische
        /// Woche (Montag 0 Uhr bis Sonntag 23 Uhr, ohne Ferien und Feiertage) samt dem Weg des Generators.
        /// </summary>
        /// <param name="Weg">Woher die Zeilen kamen; <see cref="Raumnutzungsweg.Keiner"/> = nicht belegt (dann ohne Kalender).</param>
        /// <param name="Hinweis">Was der Generator benennt.</param>
        /// <param name="Kalender">Der Kalender am Ziel oder <c>null</c>.</param>
        /// <param name="Referenzjahr">Das Bezugsjahr der Reihe.</param>
        /// <param name="Woche">Die 168 Werte der typischen Woche (Rohwerte, „aus" = NaN) oder <c>null</c>.</param>
        /// <param name="Meldung">Warum der Schritt am leeren Ziel abgelehnt hat; <c>null</c> = nichts.</param>
        public sealed record Profilvorschau(Raumnutzungsweg Weg, Raumnutzungshinweis Hinweis, Konditionierungskalender Kalender,
                                            int Referenzjahr, double[] Woche, string Meldung = null);

        /// <summary>
        /// Der Bestand des neutralen Vorschauziels: was ein Gebäude ohne eigene Konditionierung mitbringt — Heiz- und
        /// Kühlsollwert, eine Person und eine Gerätelast (runde Phantasiewerte), der Luftwechsel nach den Vorgaben des
        /// Gebäudemodells und die neutrale Ferienlage. Eine Größe, die das Profil nur teilweise belegt (etwa nur die Nacht der Lüftung), nimmt den Rest
        /// von hier, wie am Ziel vom Bestand.
        /// </summary>
        public static Matrixeingang Vorschaubestand()
        {
            var b = new Matrixeingang
            {
                Ferienmerker = 1.0,
                SollTag = VORSCHAU_SOLL_TAG,
                KuehlungWirksam = true,
                KuehlSollwert = VORSCHAU_KUEHL_SOLL,
                LuftwechselInfiltration = Gebaeudemodellvorgaben.LuftwechselInfiltration,
                LuftwechselNutzer = Gebaeudemodellvorgaben.LuftwechselNutzer,
                Bewohner = 1.0,
                InterneWaermegewinne = VORSCHAU_GERAETE_W,
            };
            b.Ferienbeginn[0] = VORSCHAU_FERIEN_BEGINN;
            b.Ferienende[0] = VORSCHAU_FERIEN_ENDE;
            return b;
        }

        /// <summary>
        /// <b>Die Vorschau einer Größe</b> (6.1 Punkt 2): die Vorlage, die der Generator für die Größe erzeugt, über denselben
        /// Schritt wie die Übernahme (<see cref="ProfilAnwenden"/>: <see cref="Konditionierungsarbeit.VorlageUebernehmen"/>,
        /// die Rückfrage „aufteilen" der Lüftung bejaht) an einem leeren Gebäude mit der neutralen Ferienlage, ohne Fläche und
        /// Höhe — NUR diese Größe, damit eine andere Größe die Vorschau nicht verstellt; daraus der Kalender des Ziels und eine
        /// typische Woche. Eine nicht belegte Größe liefert keinen Kalender (NP-F6).
        /// </summary>
        public static Profilvorschau Vorschau(Raumnutzungsprofil profil, Konditionierungsgroesse groesse)
        {
            if (profil == null) throw new ArgumentNullException(nameof(profil));
            var leer = new Konditionierungsarbeitsstand(Konditionierungsstand.Leer(Kalendereigentuemer.Gebaeude, Vorschaubestand()), null);
            Raumnutzungsgroesse r = Raumnutzungsgenerator.Erzeugen(profil, groesse, null);
            if (r.Weg == Raumnutzungsweg.Keiner || r.Vorlage == null)
                return new Profilvorschau(r.Weg, r.Hinweis, null, leer.Referenzjahr, null);

            var ort = new Konditionierungsort(groesse, null);
            Konditionierungsschritt schritt = Konditionierungsarbeit.VorlageUebernehmen(leer, ort, r.Vorlage);
            if (schritt.Rueckfrage)
            {
                Konditionierungsschritt geteilt = Konditionierungsarbeit.LuftwechselAufteilen(leer);
                schritt = geteilt.Ok ? Konditionierungsarbeit.VorlageUebernehmen(geteilt.Stand, ort, r.Vorlage) : geteilt;
            }
            Konditionierungskalender k = schritt.Ok && !schritt.Rueckfrage ? schritt.Stand.Ansichtskalender(groesse, null) : null;
            if (k == null) return new Profilvorschau(r.Weg, r.Hinweis, null, leer.Referenzjahr, null, schritt.Meldung);

            double[] jahr = k.Auswerten(leer.W0, leer.Referenzjahr);
            int start = VORSCHAU_WOCHE_AB_TAG + (7 - (leer.W0 + VORSCHAU_WOCHE_AB_TAG) % 7) % 7;
            var woche = new double[Kalenderwoche.WOCHENWERTE];
            Array.Copy(jahr, start * 24, woche, 0, woche.Length);
            return new Profilvorschau(r.Weg, r.Hinweis, k, leer.Referenzjahr, woche);
        }

        /// <summary>
        /// <b>Der Vorschlag für ein neues Zeilenbild</b> (NP-F7): die Vorgabezeilen, die der Generator aus den KENNWERTEN der
        /// Größe erzeugt (Zeilenbild und Stundenprofil der Größe bleiben dafür außen vor) — Tag, Nacht, Wochenende, Ferien.
        /// Leer, wenn die Kennwerte die Größe nicht belegen.
        /// </summary>
        public static List<Vorgabezeile> Zeilenbildvorschlag(Raumnutzungsprofil profil, Konditionierungsgroesse groesse)
        {
            if (profil == null) throw new ArgumentNullException(nameof(profil));
            Raumnutzungsprofil k = OhneBildDerGroesse(profil, groesse);
            if (k.IstLeer) return new List<Vorgabezeile>();
            Raumnutzungsgroesse r = Raumnutzungsgenerator.Erzeugen(k, groesse, null);
            if (r.Weg != Raumnutzungsweg.Kennwerte || r.Vorlage == null) return new List<Vorgabezeile>();
            string kennwort = Konditionierungsgroessen.Kennwort(groesse);
            return r.Vorlage.Inhalt.Vorgabezeilen()
                    .Where(z => string.Equals(z.Groesse, kennwort, StringComparison.Ordinal) && RaumnutzungSchema.ZEILEN.Contains(z.Zeile))
                    .Select(z => new Vorgabezeile { Groesse = z.Groesse, Zeile = z.Zeile, Wert = z.Wert, Aus = z.Aus, Von = z.Von, Bis = z.Bis, BedingtK = z.BedingtK })
                    .ToList();
        }

        /// <summary>
        /// <b>Der Vorschlag für ein neues Stundenprofil</b> (NP-F9): aus der typischen Woche, die das Profil OHNE Stundenprofil
        /// der Größe ergäbe, der erste Nutzungstag als Werktag und der erste nutzungsfreie Tag als freier Tag (ohne freien Tag
        /// derselbe). An der Lüftung mit Kennwert Außenluft in 1/h sind die Stundenwerte Anteile davon (der Generator
        /// multipliziert). <c>null</c>, wenn die Größe dann keinen Kalender ergibt.
        /// </summary>
        public static List<Raumnutzungsstunden> Stundenvorschlag(Raumnutzungsprofil profil, Konditionierungsgroesse groesse)
        {
            if (profil == null) throw new ArgumentNullException(nameof(profil));
            Raumnutzungsprofil ohne = profil.Kopie();
            string kennwort = Konditionierungsgroessen.Kennwort(groesse);
            ohne.Stunden = (ohne.Stunden ?? new List<Raumnutzungsstunden>())
                .Where(s => !string.Equals(s.Groesse, kennwort, StringComparison.Ordinal)).ToList();
            double[] woche = ohne.IstLeer ? null : Vorschau(ohne, groesse).Woche;
            if (woche == null) return null;

            double teiler = 1.0;
            if (groesse == Konditionierungsgroesse.Lueftung && profil.Aussenluft is double luft && luft > 0.0 &&
                string.Equals(profil.Aussenluft_Einheit, RaumnutzungSchema.EINHEIT_JE_STUNDE, StringComparison.Ordinal))
                teiler = luft;

            string muster = string.IsNullOrEmpty(profil.Nutzungstage_Woche) || profil.Nutzungstage_Woche.Length != 7
                ? "1111111" : profil.Nutzungstage_Woche;
            int werktag = Math.Max(0, muster.IndexOf('1'));
            int frei = muster.IndexOf('0');
            if (frei < 0) frei = werktag;
            return new List<Raumnutzungsstunden>
            {
                new Raumnutzungsstunden(kennwort, RaumnutzungSchema.TAGESART_WERKTAG, Tageswerte(woche, werktag, teiler)),
                new Raumnutzungsstunden(kennwort, RaumnutzungSchema.TAGESART_FREI, Tageswerte(woche, frei, teiler)),
            };
        }

        /// <summary>Die Kopie eines Profils ohne Zeilenbild und Stundenprofil einer Größe.</summary>
        private static Raumnutzungsprofil OhneBildDerGroesse(Raumnutzungsprofil profil, Konditionierungsgroesse groesse)
        {
            Raumnutzungsprofil k = profil.Kopie();
            string kennwort = Konditionierungsgroessen.Kennwort(groesse);
            k.Zeilen = (k.Zeilen ?? new List<Vorgabezeile>()).Where(z => !string.Equals(z.Groesse, kennwort, StringComparison.Ordinal)).ToList();
            k.Stunden = (k.Stunden ?? new List<Raumnutzungsstunden>()).Where(s => !string.Equals(s.Groesse, kennwort, StringComparison.Ordinal)).ToList();
            return k;
        }

        /// <summary>Die 24 Werte eines Wochentags als Text des Stundenprofils (Semikolon, invariant, „aus" bei NaN).</summary>
        private static string Tageswerte(double[] woche, int tag, double teiler)
            => string.Join(";", Enumerable.Range(0, 24).Select(h =>
            {
                double w = woche[Kalenderwoche.Stelle(tag, h)];
                return double.IsNaN(w)
                    ? DbWerte.KOND_WOCHE_AUS
                    : Math.Round(w / teiler, 4, MidpointRounding.AwayFromZero).ToString("0.####", CultureInfo.InvariantCulture);
            }));

        #endregion
    }
}
