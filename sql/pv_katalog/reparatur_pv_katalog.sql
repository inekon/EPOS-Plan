-- ===========================================================================
-- Reparatur PV-Modulkatalog EPOS-Plan  (Tab_PV_STAMM / Tab_PV)
-- Datei:  reparatur_pv_katalog.sql
-- Datum:  02.09.2026
-- Ziel-DB: Kenndaten.sqlite (Tabellen Tab_PV_STAMM und Tab_PV)
--
-- ANLASS
--   Die Temperaturkoeffizienten des PV-Modulkatalogs sind in mehreren Zeilen
--   unbrauchbar:
--     a) Historischer Kopierfehler im Speicherweg (Commit 5d8122a, 17.03.2026):
--        alpha_SC, beta_OC und T_NOCT wurden mit dem Wert von I_Kurzschluss
--        beschrieben  (Tab_PV_STAMM 6, 7, 8 und die Projektkopien in Tab_PV).
--     b) Katalogdialog schrieb Nullen (Tab_PV_STAMM 5, Ablytek 6MN6A270).
--     c) PAN-Import ohne Koeffizienten (Tab_PV_STAMM 9, LG: NULL).
--   Die Simulation liest NULL wie 0; damit rechnet sie ohne Temperaturgang
--   bzw. mit absurd grossen Koeffizienten (z. B. alpha_SC = 9.42 A/K statt
--   0.0049 A/K).
--
-- ZIELSTAND
--   alpha_SC [A/K], beta_OC [V/K], gamma_PMP [%/K], T_NOCT [Grad C] je Zeile
--   auf den Wert der Quelle, aus der die Zeile stammt:
--     - Ablytek 6MN6A270/275/290 -> CEC (NREL SAM), Datei "CEC Modules_UTC.csv"
--       (Zuordnung belegt: I_sc_ref/V_oc_ref/I_mp_ref/V_mp_ref/Length/Width der
--       CEC-Zeile stimmen mit den DB-Werten ueberein)
--     - Jinkosolar JKM 260P-60 -> PAN "Jinko-Solar_JKM260P-60_Dec2019_CFV.PAN"
--       (Zuordnung belegt: Isc 9.014 / Voc 37.81 / Imp 8.461 / Vmp 30.73)
--     - LG 320 N1K-A5 -> PAN "LG_LG320N1K-A5_Dec2019_CFV.PAN"
--       (Zuordnung belegt: Isc 10.350 / Voc 40.11 / Imp 9.784 / Vmp 32.71)
--   Umrechnung PAN -> DB:
--     alpha_SC = muISC / 1000        [mA/Grad C -> A/K]
--     beta_OC  = muVocSpec / 1000    [mV/Grad C -> V/K]
--     gamma_PMP= muPmpReq            [%/Grad C  = %/K]
--     T_NOCT   = 0                   (im PAN-Format nicht enthalten)
--   Quellverzeichnis beider Quellen:
--     C:\Users\DirkEngelmann\Documents\WP-Plan\VDI-3805-Daten\PV\
--
-- NICHT ANGEFASST
--   Leistung, Wirkungsgrad, U_Mpp, U_Leerlauf, I_Mpp, I_Kurzschluss,
--   Laenge, Breite, Modulkosten, Firma, Beschreibung, ReadOnly.
--
-- WICHTIG
--   *** VORHER EINE SICHERUNG DER DATENBANK ANLEGEN ***
--   (z. B. Kenndaten.sqlite.vor-pv-reparatur-2026-09-02.bak)
--   EPOS-Plan vorher schliessen; es darf keine -wal/-shm-Datei offen sein.
--   Jedes UPDATE ist mit einem Guard auf die gemessenen Ist-Werte versehen und
--   greift nur, solange die Zeile noch im vermessenen Zustand ist. Ein zweiter
--   Lauf aendert daher 0 Zeilen (idempotent).
-- ===========================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- Tab_PV_STAMM ID 5 - "Ablytek 6MN6A270"
-- Quelle: CEC Modules_UTC.csv, Zeile 4 ("Ablytek 6MN6A270"),
--         Spalten alpha_sc = 0.00486614 [A/K], beta_oc = -0.121182 [V/K],
--         T_NOCT = 47.4 [C], gamma_pmp = -0.4509 [%/K]
-- Vorher:  alpha_SC = 0.0, beta_OC = 0.0, gamma_PMP = -0.4509, T_NOCT = 0.0
--          (I_Kurzschluss = 9.34) -> Nullen aus dem Katalogdialog
-- Aenderung: alpha_SC, beta_OC, T_NOCT; gamma_PMP bleibt (schon CEC-konform)
-- ---------------------------------------------------------------------------
UPDATE Tab_PV_STAMM
   SET alpha_SC = 0.00486614,
       beta_OC = -0.121182,
       gamma_PMP = -0.4509,
       T_NOCT = 47.4
 WHERE ID = 5
   AND Bezeichner = 'Ablytek 6MN6A270'
   AND alpha_SC = 0.0
   AND beta_OC = 0.0
   AND gamma_PMP = -0.4509
   AND T_NOCT = 0.0;

-- ---------------------------------------------------------------------------
-- Tab_PV_STAMM ID 6 - "Ablytek 6MN6A275"
-- Quelle: CEC Modules_UTC.csv, Zeile 5 ("Ablytek 6MN6A275"),
--         Spalten alpha_sc = 0.00490782 [A/K], beta_oc = -0.122249 [V/K],
--         T_NOCT = 47.4 [C], gamma_pmp = -0.4509 [%/K]
-- Vorher:  alpha_SC = 9.42, beta_OC = 9.42, gamma_PMP = -0.4509, T_NOCT = 9.42
--          (I_Kurzschluss = 9.42) -> Kopierfehler alpha/beta/T_NOCT = I_Kurzschluss
-- Aenderung: alpha_SC, beta_OC, T_NOCT; gamma_PMP bleibt
-- ---------------------------------------------------------------------------
UPDATE Tab_PV_STAMM
   SET alpha_SC = 0.00490782,
       beta_OC = -0.122249,
       gamma_PMP = -0.4509,
       T_NOCT = 47.4
 WHERE ID = 6
   AND Bezeichner = 'Ablytek 6MN6A275'
   AND alpha_SC = 9.42
   AND beta_OC = 9.42
   AND gamma_PMP = -0.4509
   AND T_NOCT = 9.42;

-- ---------------------------------------------------------------------------
-- Tab_PV_STAMM ID 7 - "Ablytek 6MN6A290"
-- Quelle: CEC Modules_UTC.csv, Zeile 8 ("Ablytek 6MN6A290"),
--         Spalten alpha_sc = 0.00503807 [A/K], beta_oc = -0.125449 [V/K],
--         T_NOCT = 47.4 [C], gamma_pmp = -0.4509 [%/K]
-- Vorher:  alpha_SC = 9.67, beta_OC = 9.67, gamma_PMP = -0.4509, T_NOCT = 9.67
--          (I_Kurzschluss = 9.67) -> Kopierfehler
-- Aenderung: alpha_SC, beta_OC, T_NOCT; gamma_PMP bleibt
-- ---------------------------------------------------------------------------
UPDATE Tab_PV_STAMM
   SET alpha_SC = 0.00503807,
       beta_OC = -0.125449,
       gamma_PMP = -0.4509,
       T_NOCT = 47.4
 WHERE ID = 7
   AND Bezeichner = 'Ablytek 6MN6A290'
   AND alpha_SC = 9.67
   AND beta_OC = 9.67
   AND gamma_PMP = -0.4509
   AND T_NOCT = 9.67;

-- ---------------------------------------------------------------------------
-- Tab_PV_STAMM ID 8 - "Jinkosolar JKM 260P-60"
-- Quelle: Jinko-Solar_JKM260P-60_Dec2019_CFV.PAN (PVsyst), Schluessel
--         muISC = 3.40 [mA/C]   -> alpha_SC  = 3.40/1000  =  0.0034 [A/K]
--         muVocSpec = -118.1 [mV/C] -> beta_OC = -118.1/1000 = -0.1181 [V/K]
--         muPmpReq = -0.418 [%/C]   -> gamma_PMP = -0.418 [%/K]
--         T_NOCT im PAN nicht vorhanden -> 0
-- Vorher:  alpha_SC = 9.014, beta_OC = 9.014, gamma_PMP = 0.0, T_NOCT = 9.014
--          (I_Kurzschluss = 9.014) -> Kopierfehler + gamma nie gefuellt
-- ---------------------------------------------------------------------------
UPDATE Tab_PV_STAMM
   SET alpha_SC = 0.0034,
       beta_OC = -0.1181,
       gamma_PMP = -0.418,
       T_NOCT = 0
 WHERE ID = 8
   AND Bezeichner = 'Jinkosolar JKM 260P-60'
   AND alpha_SC = 9.014
   AND beta_OC = 9.014
   AND gamma_PMP = 0.0
   AND T_NOCT = 9.014;

-- VARIANTE (bewusst auskommentiert, Entscheidung des Fachbereichs):
--   T_NOCT = 45.1 aus der CEC-Schwesterzeile "Jinko Solar Co. Ltd JKM260P-60"
--   (CEC Modules_UTC.csv, Zeile 7353, Spalte T_NOCT = 45.1).
--   ACHTUNG: anderes Prueflabor als die PAN-Datei (CEC dort Isc 8.98 statt
--   9.014, Length 1.614 statt 1.65) - deshalb nur als Option, nicht aktiv.
--   Nach dem obigen UPDATE anzuwenden (Guard dann T_NOCT = 0):
-- UPDATE Tab_PV_STAMM
--    SET T_NOCT = 45.1
--  WHERE ID = 8
--    AND Bezeichner = 'Jinkosolar JKM 260P-60'
--    AND T_NOCT = 0;

-- ---------------------------------------------------------------------------
-- Tab_PV_STAMM ID 9 - "LG Electronics LG 320 N1K-A5"
-- Quelle: LG_LG320N1K-A5_Dec2019_CFV.PAN (PVsyst), Schluessel
--         muISC = 3.10 [mA/C]   -> alpha_SC  = 3.10/1000  =  0.0031 [A/K]
--         muVocSpec = -110.2 [mV/C] -> beta_OC = -110.2/1000 = -0.1102 [V/K]
--         muPmpReq = -0.394 [%/C]   -> gamma_PMP = -0.394 [%/K] (bereits korrekt)
--         T_NOCT im PAN nicht vorhanden -> 0
-- Vorher:  alpha_SC = NULL, beta_OC = NULL, gamma_PMP = -0.394, T_NOCT = NULL
--          -> PAN-Import ohne Koeffizienten
-- Hinweis: T_NOCT wird 0 (nicht NULL), weil der Import 0 schreibt und die
--          Dubletten-Registry exakt vergleicht - NULL und 0 gaelten als
--          verschiedene Module.
-- ---------------------------------------------------------------------------
UPDATE Tab_PV_STAMM
   SET alpha_SC = 0.0031,
       beta_OC = -0.1102,
       gamma_PMP = -0.394,
       T_NOCT = 0
 WHERE ID = 9
   AND Bezeichner = 'LG Electronics LG 320 N1K-A5'
   AND alpha_SC IS NULL
   AND beta_OC IS NULL
   AND gamma_PMP = -0.394
   AND T_NOCT IS NULL;

-- ---------------------------------------------------------------------------
-- Tab_PV_STAMM ID 21 - "Philadelphia Solar PS-M144(HCBF)-530W"
-- KEIN UPDATE. Verifikation gegen CEC Modules_UTC.csv, Zeile 10328:
--   alpha_sc  = 0.00272    == DB alpha_SC  = 0.00272     OK
--   beta_oc   = -0.128904  == DB beta_OC   = -0.128904   OK
--   gamma_pmp = -0.385     == DB gamma_PMP = -0.385      OK
--   T_NOCT    = 45.3       == DB T_NOCT    = 45.3        OK
-- Alle vier Koeffizienten sind bereits quellrichtig und plausibel.
-- (Nebenbefund, nicht Gegenstand dieser Reparatur: Laenge = 0 und Breite = 0;
--  die CEC-Zeile fuehrt Length/Width ebenfalls leer.)
-- ---------------------------------------------------------------------------

-- ---------------------------------------------------------------------------
-- Tab_PV ID 1007005 (Projekt 1007) - "Ablytek 6MN6A270"
-- Projektkopie der Stammzeile 5.
-- Quelle: CEC Modules_UTC.csv, Zeile 4, Spalten alpha_sc / beta_oc / T_NOCT /
--         gamma_pmp  (0.00486614 / -0.121182 / 47.4 / -0.4509)
-- Vorher:  alpha_SC = 9.34, beta_OC = 9.34, gamma_PMP = -0.4509, T_NOCT = 9.34
--          (I_Kurzschluss = 9.34) -> Kopierfehler
-- ---------------------------------------------------------------------------
UPDATE Tab_PV
   SET alpha_SC = 0.00486614,
       beta_OC = -0.121182,
       gamma_PMP = -0.4509,
       T_NOCT = 47.4
 WHERE ID = 1007005
   AND Bezeichner = 'Ablytek 6MN6A270'
   AND alpha_SC = 9.34
   AND beta_OC = 9.34
   AND gamma_PMP = -0.4509
   AND T_NOCT = 9.34;

-- ---------------------------------------------------------------------------
-- Tab_PV ID 1007006 (Projekt 1007) - "Ablytek 6MN6A275"
-- Projektkopie der Stammzeile 6.
-- Quelle: CEC Modules_UTC.csv, Zeile 5, Spalten alpha_sc / beta_oc / T_NOCT /
--         gamma_pmp  (0.00490782 / -0.122249 / 47.4 / -0.4509)
-- Vorher:  alpha_SC = 9.42, beta_OC = 9.42, gamma_PMP = -0.4509, T_NOCT = 9.42
--          (I_Kurzschluss = 9.42) -> Kopierfehler
-- ---------------------------------------------------------------------------
UPDATE Tab_PV
   SET alpha_SC = 0.00490782,
       beta_OC = -0.122249,
       gamma_PMP = -0.4509,
       T_NOCT = 47.4
 WHERE ID = 1007006
   AND Bezeichner = 'Ablytek 6MN6A275'
   AND alpha_SC = 9.42
   AND beta_OC = 9.42
   AND gamma_PMP = -0.4509
   AND T_NOCT = 9.42;

-- ---------------------------------------------------------------------------
-- Tab_PV ID 1011008 (Projekt 1011) - "Jinkosolar JKM 260P-60"
-- Projektkopie der Stammzeile 8.
-- Quelle: Jinko-Solar_JKM260P-60_Dec2019_CFV.PAN
--         muISC = 3.40 -> 0.0034 [A/K]; muVocSpec = -118.1 -> -0.1181 [V/K];
--         muPmpReq = -0.418 -> gamma_PMP [%/K]; T_NOCT nicht im PAN -> 0
-- Vorher:  alpha_SC = 9.014, beta_OC = 9.014, gamma_PMP = 0.0, T_NOCT = 9.014
--          (I_Kurzschluss = 9.014) -> Kopierfehler
-- ---------------------------------------------------------------------------
UPDATE Tab_PV
   SET alpha_SC = 0.0034,
       beta_OC = -0.1181,
       gamma_PMP = -0.418,
       T_NOCT = 0
 WHERE ID = 1011008
   AND Bezeichner = 'Jinkosolar JKM 260P-60'
   AND alpha_SC = 9.014
   AND beta_OC = 9.014
   AND gamma_PMP = 0.0
   AND T_NOCT = 9.014;

-- ---------------------------------------------------------------------------
-- Tab_PV ID 1015244 (Projekt 1026) - "Jinkosolar JKM 260P-60"
-- Projektkopie der Stammzeile 8. Quelle und Umrechnung wie ID 1011008.
-- Vorher:  alpha_SC = 9.014, beta_OC = 9.014, gamma_PMP = 0.0, T_NOCT = 9.014
-- ---------------------------------------------------------------------------
UPDATE Tab_PV
   SET alpha_SC = 0.0034,
       beta_OC = -0.1181,
       gamma_PMP = -0.418,
       T_NOCT = 0
 WHERE ID = 1015244
   AND Bezeichner = 'Jinkosolar JKM 260P-60'
   AND alpha_SC = 9.014
   AND beta_OC = 9.014
   AND gamma_PMP = 0.0
   AND T_NOCT = 9.014;

-- ---------------------------------------------------------------------------
-- Tab_PV ID 1015245 (Projekt 1028) - "Jinkosolar JKM 260P-60"
-- Projektkopie der Stammzeile 8. Quelle und Umrechnung wie ID 1011008.
-- Vorher:  alpha_SC = 9.014, beta_OC = 9.014, gamma_PMP = 0.0, T_NOCT = 9.014
-- ---------------------------------------------------------------------------
UPDATE Tab_PV
   SET alpha_SC = 0.0034,
       beta_OC = -0.1181,
       gamma_PMP = -0.418,
       T_NOCT = 0
 WHERE ID = 1015245
   AND Bezeichner = 'Jinkosolar JKM 260P-60'
   AND alpha_SC = 9.014
   AND beta_OC = 9.014
   AND gamma_PMP = 0.0
   AND T_NOCT = 9.014;

-- ---------------------------------------------------------------------------
-- Tab_PV ID 1015246 (Projekt 1029) - "Jinkosolar JKM 260P-60"
-- Projektkopie der Stammzeile 8. Quelle und Umrechnung wie ID 1011008.
-- Vorher:  alpha_SC = 9.014, beta_OC = 9.014, gamma_PMP = 0.0, T_NOCT = 9.014
-- ---------------------------------------------------------------------------
UPDATE Tab_PV
   SET alpha_SC = 0.0034,
       beta_OC = -0.1181,
       gamma_PMP = -0.418,
       T_NOCT = 0
 WHERE ID = 1015246
   AND Bezeichner = 'Jinkosolar JKM 260P-60'
   AND alpha_SC = 9.014
   AND beta_OC = 9.014
   AND gamma_PMP = 0.0
   AND T_NOCT = 9.014;

COMMIT;

-- ===========================================================================
-- Erwartetes Ergebnis: 11 UPDATEs, je 1 geaenderte Zeile (Tab_PV_STAMM 5, 6, 7,
-- 8, 9 und Tab_PV 1007005, 1007006, 1011008, 1015244, 1015245, 1015246).
-- Tab_PV_STAMM 21 bleibt unveraendert. Zweiter Lauf: 0 geaenderte Zeilen.
-- ===========================================================================
