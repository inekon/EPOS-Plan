using NReco.Csv;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class Attrribute_hk
    {
        public string m_szName;
        public string m_szFirma;
        public string m_szBauart;
        public string m_szBrennstoff;
        public string m_szBrennstoffIndex;
        public string szBrennstoffart;
        public string m_szThLeistung;

        /// <summary>
        /// Der Wirkungsgrad bei Nennlast (η₁₀₀), wie er in die Uebernahme geht — seit
        /// Anwenderentscheid F3 (29.09.2026) aus Satz <c>710.01</c> Spalte 6, ersatzweise aus
        /// Satz 700 Spalte 26 (<see cref="m_szWirkungsgradSatz700"/>). In Prozent, wie in der Datei.
        /// </summary>
        public string m_szWirkungsgrad;

        /// <summary>
        /// Satz 700 Spalte 26, unveraendert. Liegt in den Herstellerdateien meist um 87–91 % und
        /// ist vermutlich der brennwertbezogene Jahresnutzungsgrad (Konzept Kesselkennlinie, N1) —
        /// deshalb nur noch der Rueckfall, wenn <c>710.01</c> keinen Nennlastwert fuehrt.
        /// </summary>
        public string m_szWirkungsgradSatz700;

        /// <summary>
        /// Stammt <see cref="m_szWirkungsgrad"/> aus Satz <c>710.01</c> (und nicht aus dem Rueckfall
        /// Satz 700)? Die Katalognachpflege uebernimmt eta100 nur dann (Entscheid F3).
        /// </summary>
        public bool m_bNennlastAus710;

        /// <summary>
        /// Der Wirkungsgrad bei 30 % Teillast (η₃₀) aus Satz <c>710.01</c> Spalte 7, heizwertbezogen,
        /// in Prozent wie in der Datei; leer, wenn die Datei keinen fuehrt.
        /// </summary>
        public string m_szWirkungsgrad30;

        /// <summary>
        /// Die kleinste Waermeleistung [kW] aus Satz <c>710.01</c> — der kleinere der Werte der
        /// Spalten 3 und 4 (einige Dateien fuehren beide vertauscht); leer, wenn keiner da ist.
        /// </summary>
        public string m_szMindestleistung;

        /// <summary>Das Temperaturpaar des <c>710.01</c>-Satzes, aus dem die Werte stammen (z. B. „40/30").</summary>
        public string m_szTemperaturpaar;

        public string m_szVerluste;
        public string m_szCO;
        public string m_szCO2;
        public string m_szNOX;

        public Attrribute_hk()
        {
            m_szName = ""; ;
            m_szFirma = "";
            m_szBauart = "";
            m_szBrennstoff = "";
            m_szBrennstoffIndex = "";
            szBrennstoffart = "";
            m_szThLeistung = "";
            m_szVerluste = "";
            m_szWirkungsgrad = "";
            m_szWirkungsgradSatz700 = "";
            m_szWirkungsgrad30 = "";
            m_szMindestleistung = "";
            m_szTemperaturpaar = "";
            m_szCO = "";
            m_szCO2 = "";
            m_szNOX = "";
        }
    }

    public class HeizkesselImport
    {
        public List<Attrribute_hk> _list = new List<Attrribute_hk>();

        /// <summary>
        /// Eine Zeile des Satzes <c>710.01</c> („Leistungsdaten je Temperaturpaar"), wie sie der
        /// Import braucht. Belegung nach Stellung und Groesse, an allen Herstellerdateien unter
        /// <c>VDI-3805-Daten/SPK-Daten/</c> geprueft (Konzept Kesselkennlinie 1.3):
        /// Spalte 2 Temperaturpaar „Vorlauf/Ruecklauf", 3 und 4 kleinste und groesste
        /// Waermeleistung [kW], 5 Normnutzungsgrad, 6 Wirkungsgrad bei Nennlast, 7 Wirkungsgrad
        /// bei 30 % Teillast (je Prozent, heizwertbezogen; die Pruefpunkte der
        /// Wirkungsgradrichtlinie 92/42/EWG). Spalte 7 bleibt in jeder Datei unter Hs/Hi des
        /// Brennstoffs, Spalte 6 liegt um 96–98 %.
        /// </summary>
        private sealed class Leistungszeile
        {
            internal string Paar = "";
            internal string LeistungKlein = "";
            internal string LeistungGross = "";
            internal string Nennlast = "";
            internal string Teillast30 = "";
            internal int Reihenfolge;

            /// <summary>Der Ruecklauf des Paars; ohne lesbares Paar ganz hinten.</summary>
            internal double Ruecklauf
            {
                get
                {
                    string[] teile = (Paar ?? "").Split('/');
                    double rl;
                    if (teile.Length == 2 && ZahlText.Parsen(teile[1].Trim(), out rl)) return rl;
                    return double.MaxValue;
                }
            }
        }

        public void Import(string filename)
        {
            // ANSI (Windows-1252) explizit: deterministisch fuer deutsche Umlaute (ae, oe, ue, ss),
            // unabhaengig von der System-Locale. (Encoding.Default waere locale-/runtime-abhaengig.)
            ImportText(File.ReadAllText(filename, AnsiEncoding.Get()));
        }

        /// <summary>
        /// Liest einen Dateiinhalt, der schon als Text vorliegt — fuer die Nachpflege des Katalogs,
        /// die ihre Dateien auch aus einem ZIP-Archiv nimmt (<c>KesselkatalogNachpflege</c>).
        /// </summary>
        public void ImportText(string inhalt)
        {
            TextReader sr = new StringReader(inhalt ?? "");
            var csvReader = new CsvReader(sr, ";");

            csvReader.BufferSize = 32768;

            string szFirma = "";
            string szBrennstoff = "";
            string szBrennstoffIndex = "";
            string szBrennstoffart = "";
            string szCO = "";
            string szCO2 = "";
            string szNOX = "";
            bool bBeginn = false;
            var leistung = new List<Leistungszeile>();

            Attrribute_hk temp = null;
            _list.Clear();

            while (csvReader.Read())
            {
                if (Col(csvReader, 0) == "700" && bBeginn)
                {
                    // Ende
                    temp.m_szFirma = szFirma;
                    temp.m_szBrennstoff = szBrennstoff;
                    temp.m_szBrennstoffIndex = szBrennstoffIndex;
                    temp.szBrennstoffart = szBrennstoffart;
                    temp.m_szCO = szCO;
                    temp.m_szCO2 = szCO2;
                    temp.m_szNOX = szNOX;
                    Leistungsdaten(temp, leistung);
                    _list.Add(temp);
                    szBrennstoff = "";
                    szBrennstoffIndex = "";
                    szBrennstoffart = "";
                    szCO = "";
                    szCO2 = "";
                    szNOX = "";
                    leistung.Clear();
                    bBeginn = false;
                }

                if (Col(csvReader, 0) == "010")
                {
                    szFirma = Col(csvReader, 3);
                }
                else if (Col(csvReader, 0) == "100")
                {

                }
                else if (Col(csvReader, 0) == "710.01")
                {
                    // Die Leistungsdaten je Temperaturpaar (Belegung bei Leistungszeile). Gesammelt
                    // wird je Kesselblock; ausgewertet am Blockende (Leistungsdaten).
                    if (bBeginn && temp != null)
                    {
                        leistung.Add(new Leistungszeile
                        {
                            Paar = Col(csvReader, 2),
                            LeistungKlein = Col(csvReader, 3),
                            LeistungGross = Col(csvReader, 4),
                            Nennlast = Col(csvReader, 6),
                            Teillast30 = Col(csvReader, 7),
                            Reihenfolge = leistung.Count
                        });
                    }
                }
                else if (Col(csvReader, 0) == "710.05")
                {
                    szCO2 = Col(csvReader, 10);
                    szCO = Col(csvReader, 13);
                    szNOX = Col(csvReader, 12);
                }
                else if (Col(csvReader, 0) == "710.11")
                {
                    if (szBrennstoffIndex == "")
                    {
                        // noch nicht gesetzt
                        szBrennstoffart = Col(csvReader, 2);
                        szBrennstoffIndex = Col(csvReader, 3); // _Aufstellung[Int32.Parse(Col(csvReader, 1))-1];
                        szBrennstoff = Col(csvReader, 4);
                        // Hinweis: frueherer ASCII-Roundtrip entfernt - er haette Umlaute in "?" verwandelt.
                    }
                }
                else if (Col(csvReader, 0) == "700")
                {
                    temp = new Attrribute_hk();
                    temp.m_szName = Col(csvReader, 4);
                    temp.m_szBauart = Col(csvReader, 14);
                    temp.m_szThLeistung = Col(csvReader, 5);
                    temp.m_szWirkungsgradSatz700 = Col(csvReader, 26);
                    temp.m_szVerluste = Col(csvReader, 28);
                    bBeginn = true;
                }
            }

            // Letzten offenen Datensatz uebernehmen: das in-loop-Finalisieren passiert erst beim
            // naechsten "700"; ohne diesen Block fiele der LETZTE Heizkessel der Datei weg.
            if (bBeginn && temp != null)
            {
                temp.m_szFirma = szFirma;
                temp.m_szBrennstoff = szBrennstoff;
                temp.m_szBrennstoffIndex = szBrennstoffIndex;
                temp.szBrennstoffart = szBrennstoffart;
                temp.m_szCO = szCO;
                temp.m_szCO2 = szCO2;
                temp.m_szNOX = szNOX;
                Leistungsdaten(temp, leistung);
                _list.Add(temp);
                bBeginn = false;
            }
        }

        /// <summary>
        /// Wertet die <c>710.01</c>-Zeilen eines Kesselblocks aus (Konzept Kesselkennlinie 3.4,
        /// Anwenderentscheid F3 vom 29.09.2026).
        /// </summary>
        /// <remarks>
        /// <para><b>Das Paar mit dem niedrigsten Ruecklauf zuerst</b> (Konzept 3.4); ohne lesbares Paar
        /// gilt die Reihenfolge der Datei. Jede Groesse nimmt den ersten belegten Wert in dieser
        /// Reihenfolge — die Werte sind in den Dateien je Paar ohnehin gleich.</para>
        /// <para><b>η₁₀₀ aus Spalte 6</b> (F3); fuehrt keine Zeile einen, gilt Satz 700 Spalte 26 —
        /// dieselbe Rangfolge wie bisher, nur umgekehrt (Nebenbefund N1).</para>
        /// <para><b>η₃₀ aus Spalte 7</b>, die <b>Mindestleistung</b> als kleinerer der Werte der
        /// Spalten 3 und 4.</para>
        /// </remarks>
        private static void Leistungsdaten(Attrribute_hk satz, List<Leistungszeile> zeilen)
        {
            List<Leistungszeile> geordnet = zeilen
                .OrderBy(z => z.Ruecklauf)
                .ThenBy(z => z.Reihenfolge)
                .ToList();

            Leistungszeile nenn = geordnet.FirstOrDefault(z => Belegt(z.Nennlast));
            satz.m_szWirkungsgrad = nenn != null ? nenn.Nennlast.Trim() : satz.m_szWirkungsgradSatz700;
            satz.m_bNennlastAus710 = nenn != null;

            Leistungszeile teil = geordnet.FirstOrDefault(z => Belegt(z.Teillast30));
            satz.m_szWirkungsgrad30 = teil != null ? teil.Teillast30.Trim() : "";

            foreach (Leistungszeile z in geordnet)
            {
                double? klein = Zahl(z.LeistungKlein);
                double? gross = Zahl(z.LeistungGross);
                double? mindest = klein.HasValue && gross.HasValue ? Math.Min(klein.Value, gross.Value) : klein;
                if (!mindest.HasValue || mindest.Value <= 0) continue;
                satz.m_szMindestleistung = mindest.Value.ToString("0.######", CultureInfo.InvariantCulture);
                break;
            }

            Leistungszeile erste = geordnet.FirstOrDefault();
            satz.m_szTemperaturpaar = erste != null ? (erste.Paar ?? "").Trim() : "";
        }

        private static bool Belegt(string text)
        {
            double wert;
            return ZahlText.Parsen((text ?? "").Trim(), out wert) && wert > 0;
        }

        private static double? Zahl(string text)
        {
            double wert;
            return ZahlText.Parsen((text ?? "").Trim(), out wert) ? wert : (double?)null;
        }

        // Sicherer Feldzugriff: leerer String statt IndexOutOfRange, falls die Zeile
        // weniger Spalten hat als erwartet.
        private static string Col(CsvReader r, int i)
        {
            return (i >= 0 && i < r.FieldsCount) ? r[i] : "";
        }
    }
}
