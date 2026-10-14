using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle von „Normwerte einlesen…"</b> (Stufe NP7b; Konzept Nutzungsprofile 6.5, E131) — sie baut aus
    /// <see cref="RaumnutzungNormwerte"/> den Weg des Dialogs <c>NormwerteEinlesenDialog</c> und der Knöpfe am Blatt
    /// „Nutzungsprofile". Plattformfrei; die Dateiwahl gehört der Plattform (<c>Dienste.Datei</c>), gelesen wird die CSV
    /// über <see cref="RaumnutzungNormwerte.AusCsv"/>, die Projektdatei über <see cref="ProjektdateiProfileHuelle.LesenAsync"/>
    /// mit der Grenze der Plattform.
    ///
    /// <para><b>Geschrieben wird nur die zuletzt geladene Vorschau</b>, erst mit „Übernehmen"; der Schalter „vorhandene Werte
    /// überschreiben" kommt mit. Ablehnungen kommen benannt als Text des Kerns.</para>
    /// </summary>
    internal static class RaumnutzungNormwerteHuelle
    {
        /// <summary>Der Dateivorschlag der Vorlage.</summary>
        internal const string VORLAGE_DATEI = "Normwerte_Vorlage.csv";

        /// <summary>
        /// Der Weg; <c>null</c> ohne Katalog oder ohne Schritt 220 (dann bietet das Blatt keinen Knopf an).
        /// <paramref name="ios"/> = <c>null</c> heißt <see cref="OperatingSystem.IsIOS"/>.
        /// </summary>
        internal static NormwerteWeg Weg(bool? ios = null)
        {
            if (!RaumnutzungCtrl.Lesbar() || !RaumnutzungNormwerteSchema.Lesbar()) return null;
            long grenze = SqprojProfil.GrenzeFuerPlattform(ios ?? OperatingSystem.IsIOS());
            RaumnutzungNormwerte.Vorschau letzte = null;
            return new NormwerteWeg
            {
                Texte = Texte(),
                Laden = async quelle =>
                {
                    RaumnutzungNormwerte.Vorschau v = await Lesen(quelle, grenze);
                    if (v == null) return null;
                    letzte = v;
                    return Vorschau(v);
                },
                Einlesen = ueberschreiben => Einlesen(letzte, ueberschreiben),
                VorlageSpeichern = VorlageSpeichern,
                Entfernen = () => Ergebnis(RaumnutzungNormwerte.Entfernen()),
                Eingelesene = RaumnutzungNormwerte.Eingelesene,
            };
        }

        /// <summary>Dateiwahl der Plattform und Lesen der Quelle; abgebrochen = <c>null</c>.</summary>
        private static async Task<RaumnutzungNormwerte.Vorschau> Lesen(NormwerteQuelle quelle, long grenze)
        {
            if (quelle == NormwerteQuelle.Projektdatei)
            {
                string sq = await Dienste.Datei.DateiOeffnenAsync(MyResource.Resource.GIMP_DLG_SQ_TITEL,
                                                                  MyResource.Resource.GIMP_DLG_SQ_FILTER, null);
                if (string.IsNullOrWhiteSpace(sq)) return null;
                Projektdateiprofile satz = await ProjektdateiProfileHuelle.LesenAsync(sq, grenze);
                return RaumnutzungNormwerte.AusProjektdatei(satz);
            }
            string pfad = await Dienste.Datei.DateiOeffnenAsync(MyResource.Resource.RNP_NW_DLG_CSV,
                                                                MyResource.Resource.RNP_CSV_DATEIFILTER, "") ?? "";
            if (string.IsNullOrWhiteSpace(pfad)) return null;
            byte[] inhalt;
            try
            {
                inhalt = File.ReadAllBytes(pfad);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException)
            {
                // Die Anzeige bleibt der Satz mit Grund; der Vermerk im Ausnahmeprotokoll kommt über die Fangstelle der Importe.
                Importfang.Vermerken(pfad, ex);
                throw new InvalidOperationException(Format(MyResource.Resource.RNP_CSV_MSG_LESEFEHLER, ex.Message), ex);
            }
            return RaumnutzungNormwerte.AusCsv(inhalt, Path.GetFileName(pfad));
        }

        /// <summary>Die Vorschau des Kerns als DTO; die übrigen Meldungen ohne die schon eigens geführten.</summary>
        internal static NormwerteVorschau Vorschau(RaumnutzungNormwerte.Vorschau v)
        {
            if (v == null) throw new ArgumentNullException(nameof(v));
            var eigens = new HashSet<string>(v.OhneWerte.Concat(v.Doppelt), StringComparer.Ordinal);
            return new NormwerteVorschau(
                v.Ok, v.Meldung ?? "", v.Quelle ?? "",
                v.Treffer.Select(t => new NormwerteTreffer(t.Nummer ?? "", t.Bezeichner ?? "", t.HatWerte, t.Eingelesen,
                                                           t.Abweichungen.ToList())).ToList(),
                v.Unbekannt.ToList(), v.OhneWerte.ToList(), v.Doppelt.ToList(),
                v.Meldungen.Where(m => !eigens.Contains(m)).ToList());
        }

        /// <summary>Liest die zuletzt geladene Vorschau ein; ohne sie benannt abgelehnt.</summary>
        internal static RaumnutzungErgebnis Einlesen(RaumnutzungNormwerte.Vorschau v, bool ueberschreiben)
        {
            if (v == null) return RaumnutzungErgebnis.Fehler(Texte().SperreDatei);
            return Ergebnis(RaumnutzungNormwerte.Einlesen(v, ueberschreiben));
        }

        /// <summary>
        /// Die Vorlage ohne Werte: Dateiwahl der Plattform, dann die Datei; auf iOS danach das Teilen. Abgebrochen = <c>Ok</c>
        /// mit leerer Meldung.
        /// </summary>
        internal static async Task<RaumnutzungErgebnis> VorlageSpeichern() => await VorlageSpeichern(OperatingSystem.IsIOS());

        /// <summary>Die Vorlage ohne Werte mit ausdrücklicher Plattform (<paramref name="teilen"/> = iOS).</summary>
        internal static async Task<RaumnutzungErgebnis> VorlageSpeichern(bool teilen)
        {
            byte[] inhalt = RaumnutzungNormwerte.Vorlage();
            string pfad = await Dienste.Datei.DateiSpeichernAsync(MyResource.Resource.RNP_NW_DLG_VORLAGE,
                                                                   MyResource.Resource.RNP_CSV_DATEIFILTER, VORLAGE_DATEI) ?? "";
            if (string.IsNullOrWhiteSpace(pfad)) return new RaumnutzungErgebnis(true, "");
            try
            {
                File.WriteAllBytes(pfad, inhalt);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException)
            {
                return RaumnutzungErgebnis.Fehler(Format(MyResource.Resource.RNP_CSV_MSG_SCHREIBFEHLER, ex.Message));
            }
            string text = Format(MyResource.Resource.RNP_NW_MSG_VORLAGE, pfad);
            if (teilen)
            {
                bool geteilt;
                try { geteilt = Dienste.Datei.MitSystemOeffnen(pfad); }
                catch (Exception) { geteilt = false; }
                if (!geteilt) text = Format(MyResource.Resource.RNP_CSV_MSG_TEILEN_FEHLER, pfad);
            }
            return new RaumnutzungErgebnis(true, text);
        }

        private static RaumnutzungErgebnis Ergebnis(RaumnutzungNormwerte.Bilanz b)
            => new RaumnutzungErgebnis(b.Ok, b.Meldung ?? "", b.Geschrieben);

        /// <summary>Das Textbündel aus <c>MyResource</c> — Rückfall ist der deutsche Vorgabewert.</summary>
        internal static NormwerteTexte Texte()
        {
            var t = new NormwerteTexte();
            t.KnopfEinlesen = Text_("RNP_NW_BTN_EINLESEN", t.KnopfEinlesen);
            t.KnopfEntfernen = Text_("RNP_NW_BTN_ENTFERNEN", t.KnopfEntfernen);
            t.Titel = Text_("RNP_NW_LBL_TITEL", t.Titel);
            t.Hinweis = Text_("RNP_NW_TXT_HINWEIS", t.Hinweis);
            t.KnopfCsv = Text_("RNP_NW_BTN_CSV", t.KnopfCsv);
            t.KnopfProjekt = Text_("RNP_NW_BTN_PROJEKT", t.KnopfProjekt);
            t.KnopfVorlage = Text_("RNP_NW_BTN_VORLAGE", t.KnopfVorlage);
            t.HinweisVorlage = Text_("RNP_NW_TXT_VORLAGE", t.HinweisVorlage);
            t.Datei = Text_("RNP_NW_LBL_DATEI", t.Datei);
            t.OhneDatei = Text_("RNP_NW_TXT_OHNE_DATEI", t.OhneDatei);
            t.TitelTreffer = Text_("RNP_NW_LBL_TREFFER", t.TitelTreffer);
            t.Nummer = Text_("RNP_NW_LBL_NUMMER", t.Nummer);
            t.Profil = Text_("RNP_NW_LBL_PROFIL", t.Profil);
            t.Stand = Text_("RNP_NW_LBL_STAND", t.Stand);
            t.Abweichungen = Text_("RNP_NW_LBL_ABWEICHUNG", t.Abweichungen);
            t.StandLeer = Text_("RNP_NW_STAND_LEER", t.StandLeer);
            t.StandVorhanden = Text_("RNP_NW_STAND_VORHANDEN", t.StandVorhanden);
            t.StandEingelesen = Text_("RNP_NW_STAND_EINGELESEN", t.StandEingelesen);
            t.Zaehlung = Text_("RNP_NW_TXT_ZAEHLUNG", t.Zaehlung);
            t.Spalten = Text_("RNP_NW_TXT_SPALTEN", t.Spalten);
            t.TitelHinweise = Text_("RNP_NW_LBL_HINWEISE", t.TitelHinweise);
            t.Art = Text_("RNP_NW_LBL_ART", t.Art);
            t.Grund = Text_("RNP_NW_LBL_GRUND", t.Grund);
            t.ArtUnbekannt = Text_("RNP_NW_ART_UNBEKANNT", t.ArtUnbekannt);
            t.ArtOhneWert = Text_("RNP_NW_ART_OHNE", t.ArtOhneWert);
            t.ArtDoppelt = Text_("RNP_NW_ART_DOPPELT", t.ArtDoppelt);
            t.ArtSonstige = Text_("RNP_NW_ART_SONST", t.ArtSonstige);
            t.KeineHinweise = Text_("RNP_NW_TXT_KEINE_HINWEISE", t.KeineHinweise);
            t.Ueberschreiben = Text_("RNP_NW_LBL_UEBERSCHREIBEN", t.Ueberschreiben);
            t.Ueberspringen = Text_("RNP_NW_TXT_UEBERSPRINGEN", t.Ueberspringen);
            t.KnopfUebernehmen = Text_("RNP_NW_BTN_UEBERNEHMEN", t.KnopfUebernehmen);
            t.KnopfAbbrechen = Text_("RNP_NW_BTN_ABBRECHEN", t.KnopfAbbrechen);
            t.SperreDatei = Text_("RNP_NW_SPERRE_DATEI", t.SperreDatei);
            t.SperreKeine = Text_("RNP_NW_SPERRE_KEINE", t.SperreKeine);
            t.FrageEntfernen = Text_("RNP_NW_FRAGE_ENTFERNEN", t.FrageEntfernen);
            t.Ja = Text_("RNP_NW_BTN_JA", t.Ja);
            t.Nein = Text_("RNP_NW_BTN_NEIN", t.Nein);
            return t;
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }

        private static string Format(string muster, params object[] werte) => string.Format(CultureInfo.CurrentCulture, muster, werte);
    }
}
