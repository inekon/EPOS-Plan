-- ===================================================================
-- EPOS-Plan — Beispieldatenbank zum Erproben von SQLiteStudio
-- Erzeugt am 31.08.2026 zum Konzept_DB-Migration_SQLite_EPOS-Plan.md
--
-- Kein Produktivdatenbestand. Ein verkleinerter Nachbau des echten
-- Modells, der GENAU die Punkte zeigt, die in der Migration
-- entschieden werden muessen:
--   1. STRICT-Tabellen (Typtreue)                    -> Abschnitt 5.1
--   2. INTEGER PRIMARY KEY AUTOINCREMENT (80 Spalten)-> Abschnitt 5.2
--   3. Umlaute in Spaltennamen UND in Daten          -> Abschnitt 11 / R1
--   4. Boolean als INTEGER CHECK (0,1), Access -1->1 -> Abschnitt 5.5
--   5. Datum als ISO-8601-TEXT                       -> Abschnitt 5.1
--   6. Fremdschluessel mit CASCADE (90 Beziehungen)  -> Abschnitt 5.3
--   7. Views mit CASE WHEN statt Access-IIf          -> Abschnitt 3.3
--   8. Geklammerte Joins (aus Abfrage_Kostenfaktoren)-> Abschnitt 3.3
--   9. [eckige Klammern] als Bezeichnergrenze        -> Abschnitt 7.1
--  10. Die NOCASE-Umlautfalle zum Anfassen           -> Abschnitt 11 / R1
-- ===================================================================

PRAGMA foreign_keys = ON;

-- -------------------------------------------------------------------
-- 1) Projekt  — Text-Schluessel mit Umlauten (wie Tab_Projekt)
-- -------------------------------------------------------------------
CREATE TABLE Tab_Projekt (
    ID            INTEGER PRIMARY KEY AUTOINCREMENT,
    Projektname   TEXT    NOT NULL UNIQUE,
    Bearbeiter    TEXT,
    Angelegt_Am   TEXT,                                  -- ISO-8601 'YYYY-MM-DD HH:MM:SS'
    Freigegeben   INTEGER NOT NULL DEFAULT 0 CHECK (Freigegeben IN (0,1))
) STRICT;

-- -------------------------------------------------------------------
-- 2) Gebaeude — Umlaute IN DEN SPALTENNAMEN, FK mit Kaskade
-- -------------------------------------------------------------------
CREATE TABLE Tab_Gebaeude (
    ID                            INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_Projekt                    INTEGER NOT NULL,
    Gebaeudename                  TEXT    NOT NULL,
    Wohnflaeche_gesamt            REAL,
    "k_Wert_Außenwand"            REAL,                  -- 20 solche Spalten gibt es echt
    "Flaeche_Außenwand"           REAL,
    "WBVK_Anschluß_Fenster_Wand"  REAL,
    Baualtersklasse               TEXT,
    FOREIGN KEY (ID_Projekt) REFERENCES Tab_Projekt(ID)
        ON UPDATE CASCADE ON DELETE CASCADE               -- 61 x Update-, 79 x Delete-Kaskade
) STRICT;

-- -------------------------------------------------------------------
-- 3) Energieanlagen — Boolean, Datum, Umlautspalte "Rücklauf"
-- -------------------------------------------------------------------
CREATE TABLE Tab_Energieanlagen (
    ID                  INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_Projekt          INTEGER NOT NULL,
    Bezeichner          TEXT    NOT NULL,
    Vorlauf             REAL,
    "Rücklauf"          REAL,
    WQ_Unbegrenzt       INTEGER NOT NULL DEFAULT 0 CHECK (WQ_Unbegrenzt IN (0,1)),
    WQ_TemperaturModus  TEXT    NOT NULL DEFAULT 'Berechnet'
                                CHECK (WQ_TemperaturModus IN ('Berechnet','Fest')),
    Geaendert_Am        TEXT,
    FOREIGN KEY (ID_Projekt) REFERENCES Tab_Projekt(ID)
        ON UPDATE CASCADE ON DELETE CASCADE
) STRICT;

-- -------------------------------------------------------------------
-- 4) Heizkessel — traegt bewusst die bekannte U+FFFD-Altlast
-- -------------------------------------------------------------------
CREATE TABLE Tab_Heizkessel (
    ID           INTEGER PRIMARY KEY AUTOINCREMENT,
    Bezeichner   TEXT    NOT NULL,
    Leistung_kW  REAL,
    ReadOnly     INTEGER NOT NULL DEFAULT 0 CHECK (ReadOnly IN (0,1))
) STRICT;

-- -------------------------------------------------------------------
-- 5) Energietraeger — Vorlage fuer die IIf->CASE-WHEN-Uebersetzung
--    Spaltenname mit Umlaut: ID_Energieträger (existiert echt)
-- -------------------------------------------------------------------
CREATE TABLE energy_carrier (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    name             TEXT NOT NULL,
    code             TEXT NOT NULL UNIQUE,
    billing_unit     TEXT,
    hi_kwh_per_unit  REAL,
    hs_kwh_per_unit  REAL,
    is_active        INTEGER NOT NULL DEFAULT 1 CHECK (is_active IN (0,1))
) STRICT;

CREATE TABLE energy_project_settings (
    ID                   INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_Projekt           INTEGER NOT NULL,
    "ID_Energieträger"   INTEGER NOT NULL,
    custom_hi            REAL,
    custom_hs            REAL,
    FOREIGN KEY (ID_Projekt)          REFERENCES Tab_Projekt(ID)   ON DELETE CASCADE,
    FOREIGN KEY ("ID_Energieträger")  REFERENCES energy_carrier(id) ON UPDATE CASCADE
) STRICT;

-- -------------------------------------------------------------------
-- 6) Senkenzuordnung — Rangliste je Anlage (wie Z_AnlageSenke)
-- -------------------------------------------------------------------
CREATE TABLE Z_AnlageSenke (
    ID           INTEGER PRIMARY KEY AUTOINCREMENT,
    ID_Anlage    INTEGER NOT NULL,
    Rang         INTEGER NOT NULL,
    Ziel         TEXT    NOT NULL,
    Bedarfsart   TEXT    NOT NULL CHECK (Bedarfsart IN ('Heizung','Brauchwasser','Prozess')),
    FOREIGN KEY (ID_Anlage) REFERENCES Tab_Energieanlagen(ID) ON DELETE CASCADE
) STRICT;

-- -------------------------------------------------------------------
-- 7) Kollationsprobe — macht Risiko R1 sichtbar und anfassbar
-- -------------------------------------------------------------------
CREATE TABLE Pruefung_Kollation (
    ID      INTEGER PRIMARY KEY AUTOINCREMENT,
    Fall    TEXT NOT NULL,
    LinksA  TEXT NOT NULL,
    RechtsB TEXT NOT NULL
) STRICT;

-- ===================================================================
-- Daten
-- ===================================================================

INSERT INTO Tab_Projekt (Projektname, Bearbeiter, Angelegt_Am, Freigegeben) VALUES
 ('Beispiel WP WG mit Erdwärme',   'Engelmann', '2026-08-14 09:12:00', 1),
 ('Beispiel WP WG 1 - Erdwärme',   'Engelmann', '2026-08-17 11:42:00', 1),
 ('Nahwärmenetz Süd',              'Götz',      '2026-08-21 08:30:00', 0),
 ('Quartier Grünstraße',           'Engelmann', '2026-08-29 16:05:00', 0),
 ('Testprojekt ohne Umlaute',      'Test',      '2026-08-31 10:00:00', 0);

INSERT INTO Tab_Gebaeude
 (ID_Projekt, Gebaeudename, Wohnflaeche_gesamt,
  "k_Wert_Außenwand", "Flaeche_Außenwand", "WBVK_Anschluß_Fenster_Wand", Baualtersklasse) VALUES
 (1, 'Mehrfamilienhaus Nord',  1240.0, 0.28,  980.0, 0.12, '1984-1994'),
 (1, 'Anbau Süd',               310.0, 0.22,  240.0, 0.10, 'ab 2016'),
 (2, 'Wohngebäude 1',           890.0, 0.35,  720.0, 0.15, '1969-1983'),
 (3, 'Schule Grünstraße',      3400.0, 0.45, 2100.0, 0.18, '1958-1968'),
 (4, 'Kita Löwenzahn',          620.0, 0.24,  480.0, 0.11, 'ab 2016');

INSERT INTO Tab_Energieanlagen
 (ID_Projekt, Bezeichner, Vorlauf, "Rücklauf", WQ_Unbegrenzt, WQ_TemperaturModus, Geaendert_Am) VALUES
 (1, 'Wärmepumpe Sole/Wasser',    45.0, 35.0, 0, 'Berechnet', '2026-08-29 14:03:00'),
 (1, 'Spitzenlastkessel Gas',     70.0, 50.0, 1, 'Fest',      '2026-08-29 14:05:00'),
 (2, 'Erdwärmesonde + Booster',   55.0, 40.0, 0, 'Berechnet', '2026-08-30 09:20:00'),
 (3, 'BHKW Erdgas',               80.0, 60.0, 1, 'Fest',      '2026-08-30 17:44:00'),
 (4, 'Luft/Wasser-Wärmepumpe',    50.0, 38.0, 0, 'Berechnet', '2026-08-31 08:15:00');

-- Bezeichner 3 traegt absichtlich das Ersatzzeichen U+FFFD aus dem
-- ANSI-Importfehler (siehe KONTEXT_Importkodierung_ANSI.md).
INSERT INTO Tab_Heizkessel (Bezeichner, Leistung_kW, ReadOnly) VALUES
 ('Vitocrossal 200 CM2 raumluftabhängig',  120.0, 1),
 ('Vitodens 300-W B3HF',                    35.0, 1),
 ('Vitocrossal 200 CM2 raumluftabh�ngig',  120.0, 1),
 ('Logano plus GB402',                     200.0, 0);

INSERT INTO energy_carrier (name, code, billing_unit, hi_kwh_per_unit, hs_kwh_per_unit, is_active) VALUES
 ('Erdgas H',      'ERDGAS_H',  'm³',  10.00, 11.10, 1),
 ('Heizöl EL',     'HEIZOEL_EL','l',    9.90, 10.57, 1),
 ('Strom',         'STROM',     'kWh',  1.00,  1.00, 1),
 ('Holzpellets',   'PELLETS',   'kg',   4.80,  5.20, 1),
 ('Fernwärme',     'FERNWAERME','kWh',  1.00,  1.00, 0);

-- custom_hi teils gesetzt, teils NULL, teils 0 — genau die drei Faelle,
-- die das Access-IIf in Abfrage_Energietraeger_Effektiv unterscheidet.
INSERT INTO energy_project_settings (ID_Projekt, "ID_Energieträger", custom_hi, custom_hs) VALUES
 (1, 3, NULL, NULL),
 (1, 1, 10.35, NULL),
 (2, 3, 0.0,  0.0),
 (3, 1, NULL, 11.25),
 (4, 4, 4.95, NULL);

INSERT INTO Z_AnlageSenke (ID_Anlage, Rang, Ziel, Bedarfsart) VALUES
 (1, 1, 'Pufferspeicher 1', 'Heizung'),
 (1, 2, 'Pufferspeicher 2', 'Brauchwasser'),
 (2, 1, 'Pufferspeicher 1', 'Heizung'),
 (3, 1, 'Pufferspeicher 3', 'Heizung'),
 (4, 1, 'Netz',             'Prozess'),
 (5, 1, 'Pufferspeicher 4', 'Brauchwasser');

INSERT INTO Pruefung_Kollation (Fall, LinksA, RechtsB) VALUES
 ('ASCII, gleiche Schreibung',      'Erdgas',        'Erdgas'),
 ('ASCII, andere Schreibung',       'Erdgas',        'ERDGAS'),
 ('Umlaut, gleiche Schreibung',     'Erdwärme',      'Erdwärme'),
 ('Umlaut, andere Schreibung',      'Erdwärme',      'ERDWÄRME'),
 ('Umlaut, nur ae anders',          'Wärmepumpe',    'wärmepumpe'),
 ('Scharfes S',                     'Grünstraße',    'GRÜNSTRASSE');

-- ===================================================================
-- Indizes
-- ===================================================================
CREATE INDEX        IX_Gebaeude_Projekt   ON Tab_Gebaeude       (ID_Projekt);
CREATE INDEX        IX_Anlagen_Projekt    ON Tab_Energieanlagen (ID_Projekt);
CREATE INDEX        IX_Senke_Anlage       ON Z_AnlageSenke      (ID_Anlage, Rang);
CREATE UNIQUE INDEX UX_Carrier_Code       ON energy_carrier     (code);

-- ===================================================================
-- Views
-- ===================================================================

-- (a) Die echte Uebersetzung des Access-IIf nach CASE WHEN.
--     Original (Access):
--       IIf(s.custom_hi Is Null Or s.custom_hi=0, ec.hi_kwh_per_unit, s.custom_hi)
CREATE VIEW Abfrage_Energietraeger_Effektiv AS
SELECT s.ID_Projekt,
       s."ID_Energieträger" AS carrier_id,
       ec.code,
       ec.name,
       ec.billing_unit,
       CASE WHEN s.custom_hi IS NULL OR s.custom_hi = 0
            THEN ec.hi_kwh_per_unit ELSE s.custom_hi END AS eff_hi,
       CASE WHEN s.custom_hs IS NULL OR s.custom_hs = 0
            THEN ec.hs_kwh_per_unit ELSE s.custom_hs END AS eff_hs
FROM energy_project_settings AS s
JOIN energy_carrier AS ec ON s."ID_Energieträger" = ec.id;

-- (b) Geklammerter Join, wie ihn Abfrage_Kostenfaktoren benutzt.
--     Beweist, dass SQLite die Access-Schreibweise annimmt.
CREATE VIEW Abfrage_Projektgebaeude AS
SELECT p.Projektname, g.Gebaeudename, g.Wohnflaeche_gesamt,
       g."k_Wert_Außenwand", a.Bezeichner AS Anlage
FROM Tab_Projekt AS p
JOIN (Tab_Gebaeude AS g JOIN Tab_Energieanlagen AS a ON a.ID_Projekt = g.ID_Projekt)
     ON g.ID_Projekt = p.ID;

-- (c) [Eckige Klammern] als Bezeichnergrenze — der bestehende SQL-Stil
--     der Anwendung laeuft damit unveraendert.
CREATE VIEW Pruefung_EckigeKlammern AS
SELECT [ID], [Bezeichner], [Vorlauf], [Rücklauf], [WQ_Unbegrenzt]
FROM   [Tab_Energieanlagen]
WHERE  [WQ_Unbegrenzt] = 0;

-- (d) Die NOCASE-Umlautfalle. In der Spalte "NOCASE_gleich" steht 1,
--     wenn SQLite die beiden Werte fuer gleich haelt.
--     Erwartung: ASCII wird gefaltet, Umlaute NICHT.
CREATE VIEW Pruefung_Kollation_Ergebnis AS
SELECT Fall,
       LinksA,
       RechtsB,
       (LinksA = RechtsB)                       AS BINARY_gleich,
       (LinksA = RechtsB COLLATE NOCASE)        AS NOCASE_gleich,
       CASE WHEN (LinksA = RechtsB COLLATE NOCASE)
            THEN 'ok'
            ELSE 'ABWEICHUNG zu Access' END     AS Bewertung
FROM Pruefung_Kollation;

-- (e) Umgebungsauskunft — zeigt, welche SQLite-Fassung das Werkzeug
--     benutzt, mit dem diese Datei gerade geoeffnet ist.
CREATE VIEW Pruefung_Umgebung AS
SELECT sqlite_version()                                   AS SQLite_Version,
       (SELECT COUNT(*) FROM sqlite_master WHERE type='table') AS Tabellen,
       (SELECT COUNT(*) FROM sqlite_master WHERE type='view')  AS Views,
       (SELECT COUNT(*) FROM sqlite_master WHERE type='index') AS Indizes;
