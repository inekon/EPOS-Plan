using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Sprache eines Berichtslaufs</b> (Konzept Berichtsvorlagen 4.9, BV-Q7 b; Etappe BV-E9): Trägt die
    /// gewählte Vorlage eine Sprache (<c>EPOS.Sprache</c> in <c>custom.xml</c>), entsteht der Bericht in ihr —
    /// Texte, Kapitel, Zahlen- und Datumsformate, Diagrammbeschriftungen —, unabhängig von der Oberflächensprache.
    /// Ohne Angabe gilt die Oberflächensprache; die Standardvorlage ist sprachneutral.
    ///
    /// <para><b>Word und Excel in einer Sprache.</b> Word-Bericht und Excel-Mappe eines Laufs entstehen in derselben
    /// Sprache. Tragen beide gewählten Vorlagen eine Sprache und widersprechen sich, gewinnt die Word-Vorlage
    /// (<see cref="Widerspruch"/>), und die Startrückfrage nennt es
    /// (<see cref="BerichtCtrl.SpracheAbgleichen(Startbefund, Excelstartbefund)"/>).</para>
    ///
    /// <para>Den Lauf in dieser Sprache hält <see cref="BerichtTexte.ImLauf"/>.</para>
    /// </summary>
    public sealed class Berichtssprache
    {
        private Berichtssprache(bool englisch, bool oberflaecheEnglisch, string vorlage, bool? excelEnglisch,
                                string excelVorlage, bool widerspruch)
        {
            Englisch = englisch;
            OberflaecheEnglisch = oberflaecheEnglisch;
            Vorlage = vorlage;
            ExcelEnglisch = excelEnglisch;
            ExcelVorlage = excelVorlage;
            Widerspruch = widerspruch;
        }

        /// <summary>Entsteht der Bericht auf Englisch?</summary>
        public bool Englisch { get; }

        /// <summary>Die Oberflächensprache, gegen die abgewogen wurde.</summary>
        public bool OberflaecheEnglisch { get; }

        /// <summary>Der Name der Vorlage, aus der die Sprache kommt; <c>null</c> = Oberflächensprache.</summary>
        public string Vorlage { get; }

        /// <summary>Kommt die Sprache aus einer Vorlage?</summary>
        public bool AusVorlage { get { return Vorlage != null; } }

        /// <summary>Weicht die Sprache des Laufs von der Oberflächensprache ab?</summary>
        public bool Abweichend { get { return Englisch != OberflaecheEnglisch; } }

        /// <summary>Die Sprache der Excel-Vorlage; <c>null</c> = keine Angabe oder keine Excel-Vorlage.</summary>
        public bool? ExcelEnglisch { get; }

        /// <summary>Der Name der Excel-Vorlage mit Sprache; <c>null</c> ohne.</summary>
        public string ExcelVorlage { get; }

        /// <summary>Tragen Word- und Excel-Vorlage verschiedene Sprachen? Dann gilt die der Word-Vorlage.</summary>
        public bool Widerspruch { get; }

        /// <summary>Die Oberflächensprache als Sprache des Laufs — ohne Vorlage mit Sprache.</summary>
        public static Berichtssprache Oberflaeche(bool oberflaecheEnglisch)
        {
            return new Berichtssprache(oberflaecheEnglisch, oberflaecheEnglisch, null, null, null, false);
        }

        /// <summary>
        /// Die Sprache des Laufs aus den Vorprüfungen: die der Word-Vorlage, die der Lauf füllt (<paramref name="weg"/>
        /// „Mit Standardvorlage“ oder eine unlesbare Vorlage zählen nicht — die Standardvorlage ist sprachneutral), sonst die
        /// der Excel-Vorlage (nicht bei <paramref name="excelOhneVorlage"/>), sonst die Oberflächensprache.
        /// </summary>
        /// <param name="word">Der Befund der Word-Vorlage; <c>null</c> = kein Word-Bericht.</param>
        /// <param name="weg">Die Antwort der Rückfrage für die Word-Vorlage.</param>
        /// <param name="excel">Der Befund der Excel-Vorlage; <c>null</c> = keine Mappe.</param>
        /// <param name="excelOhneVorlage">Entsteht die Mappe für diesen Lauf ohne Vorlage?</param>
        /// <param name="oberflaecheEnglisch">Die Oberflächensprache.</param>
        public static Berichtssprache Fuer(Startbefund word, Startweg weg, Excelstartbefund excel, bool excelOhneVorlage,
                                           bool oberflaecheEnglisch)
        {
            bool? wordSprache = null;
            string wordName = null;
            if (word?.Wahl?.Eintrag != null && word.KannGewaehlteFuellen
                && (weg == Startweg.Gewaehlt || word.Wahl.Eintrag.IstStandard))
            {
                wordSprache = AusKuerzel(word.Pruefbefund?.Sprache);
                if (wordSprache.HasValue) wordName = word.Wahl.Eintrag.Name;
            }

            bool? excelSprache = null;
            string excelName = null;
            if (!excelOhneVorlage && excel?.Wahl?.Eintrag != null && excel.KannGewaehlteFuellen)
            {
                excelSprache = AusKuerzel(excel.Pruefbefund?.Sprache);
                if (excelSprache.HasValue) excelName = excel.Wahl.Eintrag.Name;
            }

            if (wordSprache.HasValue)
                return new Berichtssprache(wordSprache.Value, oberflaecheEnglisch, wordName, excelSprache, excelName,
                                           excelSprache.HasValue && excelSprache.Value != wordSprache.Value);
            if (excelSprache.HasValue)
                return new Berichtssprache(excelSprache.Value, oberflaecheEnglisch, excelName, excelSprache, excelName, false);
            return Oberflaeche(oberflaecheEnglisch);
        }

        /// <summary>
        /// Die Information vor dem Start (ohne Halt): „Der Bericht wird auf Englisch erstellt – in der Sprache der
        /// Vorlage „…“.“ in der Oberflächensprache <paramref name="englisch"/>; leer, wenn der Bericht in der
        /// Oberflächensprache entsteht.
        /// </summary>
        public string Hinweis(bool englisch)
        {
            if (!Abweichend || !AusVorlage) return "";
            return Berichtslauftexte.T(englisch, nameof(R.BV_SPRACHE_HINWEIS), Sprachname(Englisch, englisch), Vorlage);
        }

        /// <summary>„Deutsch“ oder „Englisch“ in der Sprache <paramref name="inEnglisch"/>.</summary>
        public static string Sprachname(bool sprache, bool inEnglisch)
        {
            return Berichtslauftexte.T(inEnglisch, sprache ? nameof(R.VF_PRUEF_SPRACHE_EN) : nameof(R.VF_PRUEF_SPRACHE_DE));
        }

        /// <summary>
        /// Das Kürzel <c>EPOS.Sprache</c> als Sprache: beginnt mit „en“ = Englisch, mit „de“ = Deutsch; alles andere
        /// (auch leer) = keine Angabe.
        /// </summary>
        public static bool? AusKuerzel(string kuerzel)
        {
            if (string.IsNullOrWhiteSpace(kuerzel)) return null;
            string s = kuerzel.Trim();
            if (s.StartsWith("en", StringComparison.OrdinalIgnoreCase)) return true;
            if (s.StartsWith("de", StringComparison.OrdinalIgnoreCase)) return false;
            return null;
        }

        /// <summary>
        /// Die Sprache eines Vorlagenpakets (Word oder Excel) aus <c>custom.xml</c> — für einen Lauf ohne Vorprüfung.
        /// <c>null</c> = keine Angabe oder nicht lesbar.
        /// </summary>
        public static bool? AusPaket(byte[] paket)
        {
            if (paket == null || paket.Length == 0) return null;
            try
            {
                using (var strom = new MemoryStream(paket, false))
                using (var zip = new ZipArchive(strom, ZipArchiveMode.Read))
                {
                    string ziel = "docProps/custom.xml";
                    ZipArchiveEntry rels = zip.GetEntry("_rels/.rels");
                    if (rels != null)
                    {
                        using (Stream s = rels.Open())
                        {
                            XElement bezug = XDocument.Load(s).Root?.Elements()
                                .FirstOrDefault(e => ((string)e.Attribute("Type") ?? "").EndsWith("/custom-properties", StringComparison.Ordinal));
                            string target = (string)bezug?.Attribute("Target");
                            if (!string.IsNullOrWhiteSpace(target)) ziel = target.TrimStart('/');
                        }
                    }
                    ZipArchiveEntry eintrag = zip.GetEntry(ziel);
                    if (eintrag == null) return null;
                    using (Stream s = eintrag.Open())
                    {
                        XElement eigenschaft = XDocument.Load(s).Root?.Elements()
                            .FirstOrDefault(e => string.Equals(((string)e.Attribute("name") ?? "").Trim(), Vorlagenpruefer.EIGENSCHAFT_SPRACHE,
                                                               StringComparison.OrdinalIgnoreCase));
                        return AusKuerzel(eigenschaft?.Value);
                    }
                }
            }
            catch (Exception)
            {
                return null;   // ein unlesbares Paket trägt keine Sprache — der Lauf benennt es selbst
            }
        }
    }
}
