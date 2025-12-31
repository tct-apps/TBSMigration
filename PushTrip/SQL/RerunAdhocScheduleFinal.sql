SET NOCOUNT ON;

---------------------------------------------------------
-- Drop temp tables if exist
---------------------------------------------------------
DROP TABLE IF EXISTS #SuccessLogs;
DROP TABLE IF EXISTS #ErrorLogs;
DROP TABLE IF EXISTS #ParsedSuccessNodes;
DROP TABLE IF EXISTS #ParsedErrorNodes;
DROP TABLE IF EXISTS #BatchSuccess;
DROP TABLE IF EXISTS #BatchError;
DROP TABLE IF EXISTS #BackupSuccessLogs;
DROP TABLE IF EXISTS #RerunLogs;
DROP TABLE IF EXISTS #ParsedBackupSuccessNodes;
DROP TABLE IF EXISTS #ParsedRerunErrorNodes;
DROP TABLE IF EXISTS #FinalSuccess;
DROP TABLE IF EXISTS #TripMonths;
DROP TABLE IF EXISTS #ResultS;

DECLARE @BatchSize INT = 3000;

---------------------------------------------------------
-- 1. Load Success & Error logs into temp tables
---------------------------------------------------------
SELECT Id, RequestXML, ResponseXML
INTO #SuccessLogs
FROM LogMigrationProcess
WHERE Type IN ('Trip')
  AND Process = 'Insert'
  AND IsSuccess = 0
  AND ISJSON(CustomData) = 1;
  
SELECT Id, RequestXML, ResponseXML
INTO #BackupSuccessLogs
FROM LogMigrationProcess
WHERE Type IN ('RerunTrip','RerunTripBackup')
  AND Process = 'Insert'
  AND ISJSON(CustomData) = 1;

---------------------------------------------------------
-- 2. Create parsed node tables
---------------------------------------------------------
CREATE TABLE #ParsedSuccessNodes
(
    OperatorCode NVARCHAR(50),
    RouteNo      NVARCHAR(50),
    TripNo       NVARCHAR(50),
    Type         NVARCHAR(10),
    Date         NVARCHAR(10),
    Time         NVARCHAR(10),
    PlateNo      NVARCHAR(50),
    Remark       NVARCHAR(255),
    Position     INT,
    TripDate     DATE
);

CREATE TABLE #ParsedBackupSuccessNodes
(
    OperatorCode NVARCHAR(50),
    RouteNo      NVARCHAR(50),
    TripNo       NVARCHAR(50),
    Type         NVARCHAR(10),
    Date         NVARCHAR(10),
    Time         NVARCHAR(10),
    PlateNo      NVARCHAR(50),
    Remark       NVARCHAR(255),
    Position     INT,
    TripDate     DATE
);

---------------------------------------------------------
-- 3. Parse Success logs in batches
---------------------------------------------------------
WHILE EXISTS (SELECT 1 FROM #SuccessLogs)
BEGIN
    -- Create batch table with fixed columns
    CREATE TABLE #BatchSuccess
    (
        Id INT,
        RequestXML NVARCHAR(MAX)
    );

    INSERT INTO #BatchSuccess
    SELECT TOP (@BatchSize) Id, RequestXML
    FROM #SuccessLogs
    ORDER BY Id;

    ;WITH XMLNAMESPACES
    (
        'http://schemas.xmlsoap.org/soap/envelope/' AS soap,
        'http://tos.org/' AS tos
    )
    INSERT INTO #ParsedSuccessNodes
    SELECT
        a.value('@operator_code','nvarchar(50)'),
    a.value('@route_no','nvarchar(50)'),
    a.value('@trip_no','nvarchar(50)'),
    a.value('@type','nvarchar(10)'),
    a.value('@date','nvarchar(10)'),
    a.value('@time','nvarchar(10)'),
    a.value('@plate_no','nvarchar(50)'),
    a.value('@remark','nvarchar(255)'),
    a.value('@position','int'),
    a.value('@trip_date','date')
    FROM #BatchSuccess b
    CROSS APPLY (SELECT CAST(REPLACE(b.RequestXML,'<?xml version="1.0" encoding="utf-8"?>','') AS XML) AS ReqXML) f
    CROSS APPLY f.ReqXML.nodes(
        '/soap:Envelope/soap:Body/tos:adhocScheduleInsert/tos:insert_list/tos:schedule/tos:adhoc'
    ) x(a)
	WHERE a.value('@trip_date','nvarchar(10)') > '2025-12-31';

    -- Remove processed batch
    DELETE s
    FROM #SuccessLogs s
    JOIN #BatchSuccess b ON s.Id = b.Id;

    DROP TABLE #BatchSuccess;
END

---------------------------------------------------------
-- 4. Parse Error logs in batches
---------------------------------------------------------
WHILE EXISTS (SELECT 1 FROM #BackupSuccessLogs)
BEGIN
    CREATE TABLE #BatchError
    (
        Id INT,
        RequestXML NVARCHAR(MAX),
        ResponseXML NVARCHAR(MAX)
    );

    INSERT INTO #BatchError
    SELECT TOP (@BatchSize) Id, RequestXML, ResponseXML
    FROM #BackupSuccessLogs
    ORDER BY Id;

    ;WITH XMLNAMESPACES
    (
        'http://schemas.xmlsoap.org/soap/envelope/' AS soap,
        'http://tos.org/' AS tos
    )
    INSERT INTO #ParsedBackupSuccessNodes
    SELECT 
		a.value('@operator_code','nvarchar(50)'),
    a.value('@route_no','nvarchar(50)'),
    a.value('@trip_no','nvarchar(50)'),
    a.value('@type','nvarchar(10)'),
    a.value('@date','nvarchar(10)'),
    a.value('@time','nvarchar(10)'),
    a.value('@plate_no','nvarchar(50)'),
    a.value('@remark','nvarchar(255)'),
    a.value('@position','int'),
    a.value('@trip_date','date')
    FROM #BatchError b
    CROSS APPLY (SELECT CAST(REPLACE(b.RequestXML,'<?xml version="1.0" encoding="utf-8"?>','') AS XML) AS ReqXML) f
    CROSS APPLY f.ReqXML.nodes(
        '/soap:Envelope/soap:Body/tos:adhocScheduleInsert/tos:insert_list/tos:schedule/tos:adhoc'
    ) x(a)
	WHERE a.value('@trip_date','nvarchar(10)') > '2025-12-31';

    DELETE e
    FROM #BackupSuccessLogs e
    JOIN #BatchError b ON e.Id = b.Id;

    DROP TABLE #BatchError;
END


---------------------------------------------------------
-- 5. Final list: success nodes NOT in error nodes
---------------------------------------------------------

SELECT s.*
INTO #FinalSuccess
FROM #ParsedSuccessNodes s
WHERE NOT EXISTS
(
    SELECT 1
    FROM #ParsedBackupSuccessNodes e
    WHERE e.OperatorCode = s.OperatorCode
      AND e.RouteNo      = s.RouteNo
      AND e.TripNo       = s.TripNo
      AND ISNULL(e.Type,'')    = ISNULL(s.Type,'')
      AND ISNULL(e.Date,'')    = ISNULL(s.Date,'')
      AND ISNULL(e.Time,'')    = ISNULL(s.Time,'')
      AND ISNULL(e.PlateNo,'') = ISNULL(s.PlateNo,'')
      AND ISNULL(e.Remark,'')  = ISNULL(s.Remark,'')
      AND e.Position = s.Position
      AND e.TripDate = s.TripDate
);

SELECT DISTINCT FORMAT(TripDate,'yyyyMM') AS YrPr
INTO #TripMonths
FROM #FinalSuccess;

CREATE TABLE #ResultS
(
    OperatorCode NVARCHAR(50),
    RouteNo      NVARCHAR(50),
    TripNo       NVARCHAR(50),
    Type         NVARCHAR(10),
    Date         DATE,
    Time         NVARCHAR(10),
    PlateNo      NVARCHAR(50),
    Remark       NVARCHAR(255),
    Position     INT,
    TripDate     DATE
);

DECLARE @AdhocArr INT = 30;
DECLARE @SQL NVARCHAR(MAX) = '';

SELECT @SQL = @SQL + '
INSERT INTO #ResultS
SELECT 
    f.OperatorCode,
    f.RouteNo,
    f.TripNo,
    f.Type,
	CAST(f.Date AS DATE) AS [Date],
    f.Time,
    f.PlateNo,
    f.Remark,
    f.Position,
    CAST(f.TripDate AS DATE) AS TripDate
FROM #FinalSuccess f
LEFT JOIN DerWaybill_' + YrPr + ' w
    ON w.TID = f.TripNo
WHERE FORMAT(f.TripDate,''yyyyMM'') = ''' + YrPr + '''
  AND (w.GateN2 IS NULL OR LTRIM(RTRIM(w.GateN2)) = '''')
;'
FROM #TripMonths;

EXEC sp_executesql @SQL;

SELECT *, @AdhocArr AS AdhocArr
FROM #ResultS
ORDER BY TripDate DESC;
