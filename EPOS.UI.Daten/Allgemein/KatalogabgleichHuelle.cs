using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Admin;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die HÜLLE des Dialogs „Katalog aktualisieren…" (Entscheidungsvorlage Modellgrenzen KU1
    /// Stufe 1) — plattformfrei: Sie ruft <see cref="Katalogabgleich"/> im Kern und übersetzt
    /// dessen Ergebnis in die Anzeigeform der Komponente. Windows steuert nur das Fenster bei
    /// (<c>Views/Admin/KatalogabgleichFenster.cs</c>); auf iOS gibt es den Menüpunkt nicht — dort
    /// liegt kein Katalogpaket, und gelesen wird der Katalog, wie die Datenbank ihn führt.
    ///
    /// <para><b>Der Paketort</b> kommt über die Naht <c>Dienste.Pfade.Auslieferungsvorlage</c>:
    /// Das Paket liegt neben der Vorlagendatenbank (<see cref="Katalogpaket.Pfad"/>).</para>
    /// </summary>
    public sealed class KatalogabgleichHuelle
    {
        /// <summary>Breite des Windows-Fensters.</summary>
        public const int FENSTER_BREITE = 1100;

        /// <summary>Höhe des Windows-Fensters.</summary>
        public const int FENSTER_HOEHE = 720;

        private readonly string _paketpfad;
        private readonly Func<string> _sichern;

        /// <summary>Die Hülle mit dem Paketort der Auslieferung und der Sicherung des Kerns.</summary>
        public KatalogabgleichHuelle()
            : this(Katalogpaket.Pfad(Dienste.Pfade.Auslieferungsvorlage), Katalogabgleich.SicherungAnlegen)
        {
        }

        /// <summary>Die Hülle mit eigenem Paketort und eigener Sicherung (Tests).</summary>
        public KatalogabgleichHuelle(string paketpfad, Func<string> sichern)
        {
            _paketpfad = paketpfad ?? "";
            _sichern = sichern;
        }

        /// <summary>Hat der Dialog geschrieben?</summary>
        public bool Geaendert { get; private set; }

        /// <summary>Der Fenstertitel.</summary>
        public static string Titel() => MyResource.Resource.KABG_TITEL;

        /// <summary>Der PARAMETERSATZ der Komponente — ohne <c>Geschlossen</c>.</summary>
        public IReadOnlyDictionary<string, object> Gaben() => new Dictionary<string, object>
        {
            ["Texte"] = Texte(),
            ["Stand"] = Stand(),
            ["Pruefen"] = new Func<Task<KatalogabgleichAntwort>>(() => Task.FromResult(Lauf(nurPruefen: true))),
            ["Abgleichen"] = new Func<Task<KatalogabgleichAntwort>>(() => Task.FromResult(Lauf(nurPruefen: false))),
            ["Wiederherstellen"] = new Func<string, string, Task<KatalogabgleichAntwort>>(
                (tabelle, schluessel) => Task.FromResult(Wiederherstellen(tabelle, schluessel))),
            ["HilfeSchluessel"] = "Form_Katalogabgleich.btn_Help",
        };

        /// <summary>Das Textbündel aus dem Ressourcenkatalog.</summary>
        public static KatalogabgleichTexte Texte() => new KatalogabgleichTexte
        {
            Titel = MyResource.Resource.KABG_TITEL,
            Einleitung = MyResource.Resource.KABG_EINLEITUNG,
            FassungDatenbank = MyResource.Resource.KABG_FASSUNG_DB,
            FassungPaket = MyResource.Resource.KABG_FASSUNG_PAKET,
            Paketort = MyResource.Resource.KABG_PAKETORT,
            Pruefen = MyResource.Resource.KABG_BTN_PRUEFEN,
            Abgleichen = MyResource.Resource.KABG_BTN_ABGLEICHEN,
            Wiederherstellen = MyResource.Resource.KABG_BTN_WIEDERHERSTELLEN,
            Schliessen = MyResource.Resource.KABG_BTN_SCHLIESSEN,
            SpalteKatalog = MyResource.Resource.KABG_SPALTE_KATALOG,
            SpalteSatz = MyResource.Resource.KABG_SPALTE_SATZ,
            SpalteAktion = MyResource.Resource.KABG_SPALTE_AKTION,
            SpalteHinweis = MyResource.Resource.KABG_SPALTE_HINWEIS,
            SpalteHandlung = MyResource.Resource.KABG_SPALTE_HANDLUNG,
            Leer = MyResource.Resource.KABG_LEER,
            KeineAenderung = MyResource.Resource.KABG_KEINE_AENDERUNG,
            NurGeprueft = MyResource.Resource.KABG_NUR_GEPRUEFT,
            Protokoll = MyResource.Resource.KABG_PROTOKOLL,
            FrageAbgleichen = MyResource.Resource.KABG_FRAGE_ABGLEICHEN,
            FrageWiederherstellen = MyResource.Resource.KABG_FRAGE_WIEDERHERSTELLEN,
            Ja = MyResource.Resource.ALLG_BTN_JA,
            Nein = MyResource.Resource.ALLG_BTN_NEIN,
        };

        /// <summary>Der Stand: Fassungen, Paketort, Protokoll.</summary>
        public KatalogabgleichStand Stand()
        {
            int? db = Katalogabgleich.FassungDerDatenbank();
            string fassungDb = db.HasValue ? db.Value.ToString(CultureInfo.CurrentCulture) : MyResource.Resource.KABG_NIE;
            string fassungPaket;
            bool vorhanden = false;
            try
            {
                if (_paketpfad.Length > 0 && System.IO.File.Exists(_paketpfad))
                {
                    fassungPaket = Katalogpaket.Lesen(_paketpfad).Fassung.ToString(CultureInfo.CurrentCulture);
                    vorhanden = true;
                }
                else fassungPaket = MyResource.Resource.KABG_KEIN_PAKET_KURZ;
            }
            catch (Exception ex)
            {
                fassungPaket = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KABG_PAKET_FEHLER, ex.Message);
            }
            List<string> protokoll = Katalogabgleich.Protokoll(50)
                .Select(z => z.Zeitpunkt + " · " + z.Fassung.ToString(CultureInfo.CurrentCulture) + " · " +
                             Aktionstext(z.Aktion) + (z.Tabelle.Length > 0 ? " · " + Katalogname(z.Tabelle) : "") +
                             (z.Schluessel.Length > 0 ? " · " + z.Schluessel : "") +
                             (z.Hinweis.Length > 0 ? " — " + z.Hinweis : ""))
                .ToList();
            return new KatalogabgleichStand(fassungDb, fassungPaket, _paketpfad, vorhanden, protokoll);
        }

        /// <summary>„Nur prüfen" (ohne Schreiben) oder „Abgleichen" (nach Sicherung).</summary>
        public KatalogabgleichAntwort Lauf(bool nurPruefen)
        {
            string sicherung = "";
            try
            {
                if (!nurPruefen && _sichern != null) sicherung = _sichern() ?? "";
            }
            catch (Exception ex)
            {
                return new KatalogabgleichAntwort(false,
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KABG_SICHERUNG_FEHLER, ex.Message),
                    "", new List<KatalogabgleichZeile>());
            }

            KatalogabgleichErgebnis e;
            try { e = Katalogabgleich.AusDatei(_paketpfad, nurPruefen, erzwingen: true); }
            catch (Exception ex)
            {
                return new KatalogabgleichAntwort(false, ex.Message, "", new List<KatalogabgleichZeile>(), Stand());
            }
            if (!nurPruefen && e.Ausgefuehrt) Geaendert = true;

            string meldung = e.Meldung ?? "";
            if (!nurPruefen && e.Ausgefuehrt && sicherung.Length > 0)
                meldung = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KABG_SICHERUNG, sicherung);
            return new KatalogabgleichAntwort(e.Ausgefuehrt, meldung,
                                              e.Ausgefuehrt ? e.Zusammenfassung() : "",
                                              e.Eintraege.Select(Zeile).ToList(), Stand());
        }

        /// <summary>Stellt den Auslieferungsstand eines Satzes wieder her.</summary>
        public KatalogabgleichAntwort Wiederherstellen(string tabelle, string schluessel)
        {
            Katalogpaket paket;
            try { paket = Katalogpaket.Lesen(_paketpfad); }
            catch (Exception ex)
            {
                return new KatalogabgleichAntwort(false,
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KABG_PAKET_FEHLER, ex.Message),
                    "", new List<KatalogabgleichZeile>());
            }
            (bool ok, string meldung) = Katalogabgleich.Wiederherstellen(paket, tabelle, schluessel);
            if (ok) Geaendert = true;
            return new KatalogabgleichAntwort(ok, meldung, "", new List<KatalogabgleichZeile>(), Stand());
        }

        private static KatalogabgleichZeile Zeile(KatalogabgleichEintrag e) =>
            new KatalogabgleichZeile(e.Tabelle, Katalogname(e.Tabelle), e.Schluessel, e.Bezeichner,
                                     Aktionstext(e.Aktion), e.Hinweis, e.Wiederherstellbar);

        /// <summary>Die Aktion in Worten.</summary>
        public static string Aktionstext(string aktion)
        {
            switch (aktion)
            {
                case Katalogabgleich.AKTION_EINGEFUEGT: return MyResource.Resource.KABG_AKTION_EINGEFUEGT;
                case Katalogabgleich.AKTION_AKTUALISIERT: return MyResource.Resource.KABG_AKTION_AKTUALISIERT;
                case Katalogabgleich.AKTION_BEHALTEN: return MyResource.Resource.KABG_AKTION_BEHALTEN;
                case Katalogabgleich.AKTION_AUSGELAUFEN: return MyResource.Resource.KABG_AKTION_AUSGELAUFEN;
                case Katalogabgleich.AKTION_WIEDERHERGESTELLT: return MyResource.Resource.KABG_AKTION_WIEDERHERGESTELLT;
                case Katalogabgleich.AKTION_KEIN_PAKET: return MyResource.Resource.KABG_AKTION_KEIN_PAKET;
                case Katalogabgleich.AKTION_BERICHT: return MyResource.Resource.KABG_AKTION_BERICHT;
                default: return aktion ?? "";
            }
        }

        /// <summary>Der Katalog in Worten.</summary>
        public static string Katalogname(string tabelle)
        {
            switch (tabelle)
            {
                case "Tab_WP_STAMM": return MyResource.Resource.KABG_KATALOG_WP;
                case "Tab_Heizkessel_STAMM": return MyResource.Resource.KABG_KATALOG_KESSEL;
                case "Tab_BHKW_STAMM": return MyResource.Resource.KABG_KATALOG_BHKW;
                case "Tab_PV_STAMM": return MyResource.Resource.KABG_KATALOG_PV;
                case "Tab_Brauchwasser_STAMM": return MyResource.Resource.KABG_KATALOG_BW;
                case "Tab_Brauchwassertyp_STAMM": return MyResource.Resource.KABG_KATALOG_BWT;
                case "Tab_Prozesswaerme_STAMM": return MyResource.Resource.KABG_KATALOG_PW;
                case "Tab_Prozesstyp_STAMM": return MyResource.Resource.KABG_KATALOG_PWT;
                default: return tabelle ?? "";
            }
        }
    }
}
