using System;
using System.Collections.Generic;
using System.Globalization;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die acht Auswahllisten des Lastgang-Importdialogs — Steuerwerte und
    /// Beschriftungen getrennt (iU9-W12.0e).
    ///
    /// <para><b>Warum im Kern.</b> Die Listen standen als fuenf
    /// <c>static readonly</c>-Felder in
    /// <c>Views/Stromverbraucher/Form_GanglinieImportOptionen.cs</c>, zusammen mit
    /// den drei Hilfsmethoden <c>Index</c>, <c>Wert</c> und <c>Grenzen</c>. Blazor
    /// und iOS brauchen genau dieselben Listen in genau derselben Reihenfolge —
    /// eine zweite Aufzaehlung waere eine zweite Wahrheit ueber die Bedeutung eines
    /// Listenplatzes.</para>
    ///
    /// <para><b>Die Drei-Schichten-Regel steht im Mittelpunkt.</b> Ein Listenplatz
    /// traegt einen WERT (<c>';'</c>, <see cref="GanglinienRaster.Viertelstunde"/>);
    /// der Anzeigetext steht daneben und kommt aus <c>MyResource</c>. Wer sie
    /// vertauscht, baut einen Dialog, den ein Sprachwechsel zerreisst.</para>
    /// </summary>
    public static class GanglinienOptionenModell
    {
        // ==================================================================
        // Steuerwerte — Reihenfolge WOERTLICH wie im Vorlaeufer (:36-63)
        // ==================================================================

        /// <summary>Steuerwerte der Trennzeichenliste; <c>'\0'</c> = einspaltig.</summary>
        public static readonly char[] Trennzeichenwerte = { ';', ',', '\t', '|', '\0' };

        /// <summary>Steuerwerte der Dezimaltrennerliste.</summary>
        public static readonly char[] Dezimalwerte = { ',', '.' };

        /// <summary>Steuerwerte der Einheitenliste.</summary>
        public static readonly GanglinienEinheit[] Einheitswerte =
        {
            GanglinienEinheit.Kilowatt,
            GanglinienEinheit.KilowattstundeJeIntervall
        };

        /// <summary>Steuerwerte der Rasterliste des Optionendialogs (vier Eintraege).</summary>
        public static readonly GanglinienRaster[] Rasterwerte =
        {
            GanglinienRaster.Unbekannt,
            GanglinienRaster.Stunde,
            GanglinienRaster.Viertelstunde,
            GanglinienRaster.Minute
        };

        /// <summary>Steuerwerte der Konventionsliste.</summary>
        public static readonly IntervallKonvention[] Konventionswerte =
        {
            IntervallKonvention.Automatisch,
            IntervallKonvention.Anfang,
            IntervallKonvention.Ende
        };

        // ==================================================================
        // Rueckfallindizes — ebenfalls woertlich (:203-213)
        // ==================================================================

        /// <summary>Rueckfall der Trennzeichenliste: <c>'\0'</c> (einspaltig).</summary>
        public const int RueckfallTrennzeichen = 4;

        /// <summary>Rueckfall der Dezimaltrennerliste: Punkt.</summary>
        public const int RueckfallDezimal = 1;

        // ==================================================================
        // Beschriftungen
        // ==================================================================

        /// <summary>Die fuenf Trennzeichen-Beschriftungen in der Reihenfolge der Werte.</summary>
        public static List<string> TrennzeichenTexte() => new List<string>
        {
            MyResource.Resource.IMPORT_TRENN_SEMIKOLON,
            MyResource.Resource.IMPORT_TRENN_KOMMA,
            MyResource.Resource.IMPORT_TRENN_TABULATOR,
            MyResource.Resource.IMPORT_TRENN_PIPE,
            MyResource.Resource.IMPORT_TRENN_KEINES
        };

        /// <summary>Die zwei Dezimaltrenner-Beschriftungen.</summary>
        public static List<string> DezimalTexte() => new List<string>
        {
            MyResource.Resource.IMPORT_DEZ_KOMMA,
            MyResource.Resource.IMPORT_DEZ_PUNKT
        };

        /// <summary>Die zwei Einheiten-Beschriftungen.</summary>
        public static List<string> EinheitTexte() => new List<string>
        {
            MyResource.Resource.IMPORT_EINHEIT_KW,
            MyResource.Resource.IMPORT_EINHEIT_KWH
        };

        /// <summary>Die vier Raster-Beschriftungen des Optionendialogs.</summary>
        public static List<string> RasterTexte() => new List<string>
        {
            MyResource.Resource.IMPORT_RASTER_AUTO,
            MyResource.Resource.IMPORT_RASTER_STUNDE,
            MyResource.Resource.IMPORT_RASTER_VIERTEL,
            MyResource.Resource.IMPORT_RASTER_MINUTE
        };

        /// <summary>Die drei Konventions-Beschriftungen.</summary>
        public static List<string> KonventionTexte() => new List<string>
        {
            MyResource.Resource.IMPORT_KONV_AUTO,
            MyResource.Resource.IMPORT_KONV_ANFANG,
            MyResource.Resource.IMPORT_KONV_ENDE
        };

        /// <summary>„Spalte {0}" fuer 1 … <paramref name="spalten"/>.</summary>
        public static List<string> SpaltenTexte(int spalten)
        {
            List<string> liste = new List<string>();
            int n = Math.Max(spalten, 1);
            for (int i = 1; i <= n; i++)
                liste.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.IMPORT_SPALTE_N, i));
            return liste;
        }

        /// <summary>
        /// Die Zeitspaltenliste: „(keine)" vorweg, danach die Spalten. Der Index der
        /// Liste ist deshalb um EINS gegenueber <c>GanglinienImportOptionen.ZeitSpalte</c>
        /// verschoben (dort ist <c>-1</c> „keine").
        /// </summary>
        public static List<string> ZeitspaltenTexte(int spalten)
        {
            List<string> liste = new List<string> { MyResource.Resource.IMPORT_SPALTE_KEINE };
            liste.AddRange(SpaltenTexte(spalten));
            return liste;
        }

        // ==================================================================
        // Index und Wert
        // ==================================================================

        /// <summary>Platz eines Wertes in seiner Liste; <paramref name="vorgabe"/>, wenn er fehlt.</summary>
        public static int Index<T>(T[] werte, T gesucht, int vorgabe)
        {
            for (int i = 0; i < werte.Length; i++)
                if (Equals(werte[i], gesucht)) return i;
            return vorgabe;
        }

        /// <summary>Wert eines Listenplatzes; <paramref name="vorgabe"/> ausserhalb der Liste.</summary>
        public static T Wert<T>(T[] werte, int index, T vorgabe)
            => index >= 0 && index < werte.Length ? werte[index] : vorgabe;

        /// <summary>Haelt einen Index in den Grenzen einer Liste; <c>-1</c> bei leerer Liste.</summary>
        public static int Grenzen(int index, int anzahl)
        {
            if (anzahl <= 0) return -1;
            if (index < 0) return 0;
            return index < anzahl ? index : anzahl - 1;
        }

        // ==================================================================
        // Die ZWEI-Eintraege-Liste der Stammdatenverwaltung
        // ==================================================================

        /// <summary>
        /// Das Raster aus dem Platz in der Auswahlliste der
        /// STAMMDATENVERWALTUNG — nicht der des Optionendialogs.
        ///
        /// <para><b>Woertlich uebernommen, samt totem Zweig</b> (Befund W12-B15).
        /// <c>Form_Stromganglinie_Admin.RasterAusAuswahl</c> kannte vier Faelle
        /// (0 = Stunde, 1 = Viertelstunde, 2 = Minute, sonst Unbekannt), die
        /// Auswahlliste der Maske hat aber nur ZWEI Eintraege — „Stundenwerte" und
        /// „1/4 Stundenwerte". <c>case 2</c> war damit nie erreichbar. Er bleibt
        /// stehen: Die Zuordnung „Platz 2 waere Minute" ist die Aussage des
        /// Vorlaeufers, und ein dritter Listeneintrag koennte sie morgen brauchen.
        /// Der Blazor-Dialog fuellt die Liste weiterhin mit zwei Eintraegen.</para>
        /// </summary>
        /// <param name="index">Platz in der Auswahlliste der Stammdatenverwaltung.</param>
        public static GanglinienRaster RasterAusIndex(int index)
        {
            switch (index)
            {
                case 0: return GanglinienRaster.Stunde;
                case 1: return GanglinienRaster.Viertelstunde;
                case 2: return GanglinienRaster.Minute;
                default: return GanglinienRaster.Unbekannt;
            }
        }

        /// <summary>
        /// Die ZWEI Beschriftungen der Rasterliste der Stammdatenverwaltung
        /// (<c>Form_Stromganglinie_Admin.comboBox_Zeitinterval</c>). Sie kamen dort
        /// aus der <c>.resx</c> der Maske; mit dem Port stehen sie im
        /// Ressourcenkatalog.
        /// </summary>
        public static List<string> AdminRasterTexte() => new List<string>
        {
            MyResource.Resource.IMPORT_RASTER_STUNDE,
            MyResource.Resource.IMPORT_RASTER_VIERTEL
        };
    }
}
