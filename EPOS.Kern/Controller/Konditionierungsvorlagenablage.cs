using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Vorlagen der Konditionierung als Weg</b> (Stufe KP2, Welle U2; Teilkonzept
    /// Konditionierungsprofile 3.5, 7.4) — was die Kalenderkarte und die Vorlagenverwaltung von den
    /// Vorlagen brauchen: die Liste je Größe, ein Kopf, der Inhalt für „Übernehmen", „Als Vorlage
    /// speichern…", Umbenennen, Löschen, Duplizieren und die Namensregel.
    /// </summary>
    /// <remarks>
    /// Zwei Träger mit DENSELBEN Regeln: die Datenbank (<see cref="KonditionierungsvorlageCtrl"/>) und
    /// die Ablage ohne Datenbank (<see cref="Konditionierungsvorlagenablage"/>) für Prüfstände — die
    /// Wirtseite <c>/konditionierungsprobe</c> und die bunit-Proben der Oberfläche. Die Hülle
    /// (<c>KonditionierungHuelle</c>) baut aus einem von beiden die Delegaten des Wegs.
    /// </remarks>
    public interface IKonditionierungsvorlagen
    {
        /// <summary>Die Vorlagen einer Größe in der Reihenfolge der Auswahlliste (<see cref="KonditionierungsvorlageCtrl.Vergleichen"/>).</summary>
        List<KonditionierungsvorlageCtrl.Vorlage> Liste(Konditionierungsgroesse groesse);

        /// <summary>Ein Kopf über seine Id; <c>null</c>, wenn es ihn nicht gibt.</summary>
        KonditionierungsvorlageCtrl.Vorlage Lesen(long id);

        /// <summary>Kopf und Inhalt — die Eingabe von „Vorlage übernehmen"; <c>null</c> mit Meldung.</summary>
        Konditionierungsvorlage Inhalt(long id, out string meldung);

        /// <summary>„Als Vorlage speichern…" mit dem Inhalt aus dem Arbeitsstand (E54); schreibt sofort.</summary>
        KonditionierungCtrl.Ergebnis SpeichernAus(Konditionierungsstand inhalt, Konditionierungsgroesse groesse,
                                                  string bezeichner, string beschreibung, string nutzung, out long id);

        /// <summary>Eine eigene Vorlage umbenennen; eine ausgelieferte ist benannt gesperrt.</summary>
        KonditionierungCtrl.Ergebnis Umbenennen(long id, string bezeichner);

        /// <summary>Eine eigene Vorlage löschen; eine ausgelieferte ist benannt gesperrt.</summary>
        KonditionierungCtrl.Ergebnis Loeschen(long id);

        /// <summary>Eine Vorlage duplizieren — auch eine ausgelieferte; ohne Namen „Name (Kopie)", eindeutig gemacht.</summary>
        KonditionierungCtrl.Ergebnis Duplizieren(long id, string bezeichner, out long neueId);

        /// <summary>
        /// Die Namensregel (getrimmt, 1 … 80 Zeichen, je Größe eindeutig ohne Unterschied der
        /// Schreibweise) ohne zu schreiben; <paramref name="ausser"/> = die eigene Id beim Umbenennen.
        /// <c>null</c> = der Name ist frei, sonst die benannte Ablehnung.
        /// </summary>
        string NamePruefen(Konditionierungsgroesse groesse, string bezeichner, long ausser);
    }

    /// <summary>
    /// <b>Die Vorlagen ohne Datenbank</b> (Stufe KP2, Welle U2) — eine Ablage im Speicher mit denselben
    /// Regeln wie <see cref="KonditionierungsvorlageCtrl"/>: Reihenfolge der Liste, Namensregel,
    /// Nutzung, Schloss der ausgelieferten, „Name (Kopie)" beim Duplizieren. Sie schreibt nichts;
    /// ihr Stand lebt so lange wie die Instanz.
    /// </summary>
    /// <remarks>
    /// <para><b>Wozu.</b> Die Wirtseite <c>/konditionierungsprobe</c> zeigt den echten Editor ohne
    /// Datenbank, und die Proben der Oberfläche laufen ohne Testdatenbank. Beide brauchen die 14
    /// ausgelieferten Vorlagen: <see cref="AusSaat"/> baut sie aus der Saattabelle
    /// (<see cref="KonditionierungsvorlagenSaattabelle"/>) in derselben Form, in der der Schemaschritt
    /// sie sät — die Wache <c>KonditionierungsvorlagenablageTests</c> hält Ablage und gesäte
    /// Testdatenbank gleich.</para>
    /// <para><b>Ids.</b> Die Saat bekommt 1 … 14 in Schemareihenfolge; neue Vorlagen zählen weiter.</para>
    /// </remarks>
    public sealed class Konditionierungsvorlagenablage : IKonditionierungsvorlagen
    {
        private sealed class Eintrag
        {
            public KonditionierungsvorlageCtrl.Vorlage Kopf;
            public Konditionierungsstand Inhalt;
        }

        private readonly List<Eintrag> _eintraege = new List<Eintrag>();
        private long _letzteId;

        /// <summary>Eine leere Ablage.</summary>
        public Konditionierungsvorlagenablage()
        {
        }

        /// <summary>
        /// <b>Die 14 ausgelieferten Vorlagen</b> aus der Saattabelle, gesperrt (<c>ReadOnly</c>), mit den
        /// Ids 1 … 14 — Vorgabezeilen und bei Büro und Schule der Kalender ohne Woche mit den neun
        /// Feiertagen „wie Sonntag", wie <see cref="KonditionierungsvorlagenSaatSchema"/> sie schreibt.
        /// </summary>
        public static Konditionierungsvorlagenablage AusSaat()
        {
            var a = new Konditionierungsvorlagenablage();
            foreach (KonditionierungsvorlagenSaat s in KonditionierungsvorlagenSaattabelle.Alle)
                a.Hinzufuegen(s.Groesse, s.Bezeichner, s.Beschreibung, s.Nutzung, ausgeliefert: true, Inhalt(s));
            return a;
        }

        /// <summary>Der Inhalt einer gesäten Vorlage als Ebene der Art <see cref="Kalendereigentuemer.Vorlage"/>.</summary>
        public static Konditionierungsstand Inhalt(KonditionierungsvorlagenSaat s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            Konditionierungsstand v = Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null);
            foreach (KonditionierungsvorlagenSaatzeile z in s.Zeilen)
                v = v.MitVorgabe(s.Groesse, z.Zeile, z.AlsZelle());
            if (!s.Feiertage) return v;

            KonditionierungsvorlagenSaatzeile grund = s.Kalendergrund;
            Kalenderangabe angabe = grund == null || grund.Aus ? Kalenderangabe.Abgeschaltet
                                                               : Kalenderangabe.AusWert(grund.Wert ?? 0.0);
            var perioden = new List<Kalenderregel>();
            for (int k = 0; k < DbWerte.KOND_FEIERTAGE.Count; k++)
                perioden.Add(Kalenderregel.Feiertag(Standardfahrplan.RANG_FEIERTAG + k,
                                                    KonditionierungsvorlagenSaattabelle.FEIERTAGSNAMEN[k],
                                                    DbWerte.KOND_FEIERTAGE[k],
                                                    Kalenderangabe.AlsWochentag(KonditionierungsvorlagenSaattabelle.WIE_WOCHENTAG)));
            return v.MitKalender(s.Groesse, new Konditionierungskalender(s.Groesse, angabe, null, perioden),
                                 Kalenderherkunft.Keine);
        }

        /// <summary>Legt eine Vorlage an (ohne Prüfung) und gibt ihre Id zurück — der Aufbau eines Prüfstands.</summary>
        public long Hinzufuegen(Konditionierungsgroesse groesse, string bezeichner, string beschreibung, string nutzung,
                                bool ausgeliefert, Konditionierungsstand inhalt)
        {
            long id = ++_letzteId;
            _eintraege.Add(new Eintrag
            {
                Kopf = new KonditionierungsvorlageCtrl.Vorlage(id, groesse, bezeichner, Leer(beschreibung), Leer(nutzung),
                                                               ausgeliefert),
                Inhalt = (inhalt ?? Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null)).AlsArt(Kalendereigentuemer.Vorlage),
            });
            return id;
        }

        /// <summary>Wie viele Vorlagen die Ablage führt.</summary>
        public int Anzahl => _eintraege.Count;

        /// <inheritdoc/>
        public List<KonditionierungsvorlageCtrl.Vorlage> Liste(Konditionierungsgroesse groesse)
        {
            var liste = _eintraege.Where(e => e.Kopf.Groesse == groesse).Select(e => e.Kopf).ToList();
            liste.Sort(KonditionierungsvorlageCtrl.Vergleichen);
            return liste;
        }

        /// <inheritdoc/>
        public KonditionierungsvorlageCtrl.Vorlage Lesen(long id) => Finde(id)?.Kopf;

        /// <inheritdoc/>
        public Konditionierungsvorlage Inhalt(long id, out string meldung)
        {
            Eintrag e = Finde(id);
            if (e == null)
            {
                meldung = Fehlt(id);
                return null;
            }
            meldung = null;
            return new Konditionierungsvorlage(id, e.Kopf.Bezeichner, e.Kopf.Groesse, e.Inhalt);
        }

        /// <inheritdoc/>
        public KonditionierungCtrl.Ergebnis SpeichernAus(Konditionierungsstand inhalt, Konditionierungsgroesse groesse,
                                                         string bezeichner, string beschreibung, string nutzung, out long id)
        {
            id = 0;
            if (inhalt == null) throw new ArgumentNullException(nameof(inhalt));
            string name = KonditionierungsvorlageCtrl.Namensregel(groesse, bezeichner, Namen(groesse, 0), out string meldung);
            if (meldung != null) return KonditionierungCtrl.Ergebnis.Fehler(meldung);
            string wert = KonditionierungsvorlageCtrl.Nutzungspruefung(nutzung, out meldung);
            if (meldung != null) return KonditionierungCtrl.Ergebnis.Fehler(meldung);
            id = Hinzufuegen(groesse, name, beschreibung, wert, ausgeliefert: false, inhalt);
            return KonditionierungCtrl.Ergebnis.Gut;
        }

        /// <inheritdoc/>
        public KonditionierungCtrl.Ergebnis Umbenennen(long id, string bezeichner)
        {
            Eintrag e = Finde(id);
            if (e == null) return KonditionierungCtrl.Ergebnis.Fehler(Fehlt(id));
            if (e.Kopf.Ausgeliefert) return KonditionierungCtrl.Ergebnis.Fehler(Gesperrt(id));
            string name = KonditionierungsvorlageCtrl.Namensregel(e.Kopf.Groesse, bezeichner, Namen(e.Kopf.Groesse, id),
                                                                  out string meldung);
            if (meldung != null) return KonditionierungCtrl.Ergebnis.Fehler(meldung);
            e.Kopf = e.Kopf with { Bezeichner = name };
            return KonditionierungCtrl.Ergebnis.Gut;
        }

        /// <inheritdoc/>
        public KonditionierungCtrl.Ergebnis Loeschen(long id)
        {
            Eintrag e = Finde(id);
            if (e == null) return KonditionierungCtrl.Ergebnis.Fehler(Fehlt(id));
            if (e.Kopf.Ausgeliefert) return KonditionierungCtrl.Ergebnis.Fehler(Gesperrt(id));
            _eintraege.Remove(e);
            return KonditionierungCtrl.Ergebnis.Gut;
        }

        /// <inheritdoc/>
        public KonditionierungCtrl.Ergebnis Duplizieren(long id, string bezeichner, out long neueId)
        {
            neueId = 0;
            Eintrag e = Finde(id);
            if (e == null) return KonditionierungCtrl.Ergebnis.Fehler(Fehlt(id));
            string wunsch = (bezeichner ?? "").Trim();
            if (wunsch.Length == 0) wunsch = KonditionierungsvorlageCtrl.Kopiename(e.Kopf.Bezeichner);
            string name = KonditionierungsvorlageCtrl.EindeutigerName(Namen(e.Kopf.Groesse, 0), wunsch);
            name = KonditionierungsvorlageCtrl.Namensregel(e.Kopf.Groesse, name, Namen(e.Kopf.Groesse, 0), out string meldung);
            if (meldung != null) return KonditionierungCtrl.Ergebnis.Fehler(meldung);
            neueId = Hinzufuegen(e.Kopf.Groesse, name, e.Kopf.Beschreibung, e.Kopf.Nutzung, ausgeliefert: false, e.Inhalt);
            return KonditionierungCtrl.Ergebnis.Gut;
        }

        /// <inheritdoc/>
        public string NamePruefen(Konditionierungsgroesse groesse, string bezeichner, long ausser)
        {
            KonditionierungsvorlageCtrl.Namensregel(groesse, bezeichner, Namen(groesse, ausser), out string meldung);
            return meldung;
        }

        private Eintrag Finde(long id) => _eintraege.FirstOrDefault(e => e.Kopf.Id == id);

        private List<string> Namen(Konditionierungsgroesse groesse, long ausser)
            => _eintraege.Where(e => e.Kopf.Groesse == groesse && e.Kopf.Id != ausser).Select(e => e.Kopf.Bezeichner).ToList();

        private static string Fehlt(long id)
            => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_VORLAGE_FEHLT,
                             id.ToString(CultureInfo.InvariantCulture));

        private static string Gesperrt(long id)
            => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_VORLAGE_GESPERRT,
                             id.ToString(CultureInfo.InvariantCulture));

        private static string Leer(string t) => string.IsNullOrWhiteSpace(t) ? null : t.Trim();
    }
}
