SET NOCOUNT ON;

SELECT DISTINCT Id, RequestXML, ResponseXML
INTO #SuccessLogs
FROM LogMigrationProcess l
CROSS APPLY OPENJSON(l.CustomData, '$.success') j
WHERE l.Type = 'RerunTripBackupFinal'
  AND l.Process = 'Insert'
  AND l.IsSuccess = 0
  AND ISJSON(l.CustomData) = 1;

DECLARE @BatchSize INT = 3000;

CREATE TABLE #ParsedBackupSuccessNodes
(
    OperatorCode NVARCHAR(50),
    RouteNo      NVARCHAR(50),
    TripNo       NVARCHAR(50),
    Type         NVARCHAR(10),
    Date         DATETIME,
    Time         NVARCHAR(10),
    PlateNo      NVARCHAR(50),
    Remark       NVARCHAR(255),
    Position     INT,
    TripDate     DATETIME
);


CREATE TABLE #BatchSuccess
(
    Id INT,
	RequestXML NVARCHAR(MAX),
    ResponseXML NVARCHAR(MAX)
);

INSERT INTO #BatchSuccess
SELECT TOP (@BatchSize) Id, RequestXML, ResponseXML
FROM #SuccessLogs
ORDER BY Id;

;WITH XMLNAMESPACES
(
    'http://schemas.xmlsoap.org/soap/envelope/' AS soap,
    'http://tos.org/' AS tos
)
INSERT INTO #ParsedBackupSuccessNodes
SELECT
    Req.a.value('@operator_code','nvarchar(50)'),
    Req.a.value('@route_no','nvarchar(50)'),
    Req.a.value('@trip_no','nvarchar(50)'),
    Req.a.value('@type','nvarchar(10)'),
    Req.a.value('@date','DATETIME'),
    Req.a.value('@time','nvarchar(10)'),
    Req.a.value('@plate_no','nvarchar(50)'),
    Req.a.value('@remark','nvarchar(255)'),
    Req.a.value('@position','int'),
    Req.a.value('@trip_date','DATETIME')
FROM #BatchSuccess b
CROSS APPLY
(
    SELECT CAST(
        REPLACE(b.ResponseXML,'<?xml version="1.0" encoding="utf-8"?>','')
        AS XML
    )
) AS RX(XmlData)
CROSS APPLY RX.XmlData.nodes(
    '/soap:Envelope/soap:Body
     /tos:adhocScheduleInsertResponse
     /tos:adhocScheduleInsertResult
     /tos:insert_status
     /tos:adhoc'
) Resp(a)
CROSS APPLY
(
    SELECT CAST(
        REPLACE(b.RequestXML,'<?xml version="1.0" encoding="utf-8"?>','')
        AS XML
    )
) AS RQ(XmlData)
CROSS APPLY RQ.XmlData.nodes(
    '/soap:Envelope/soap:Body
     /tos:adhocScheduleInsert
     /tos:insert_list
     /tos:schedule
     /tos:adhoc'
) Req(a)
WHERE Resp.a.value('@code','nvarchar(10)') = '1'
  AND Req.a.value('@trip_no','nvarchar(50)') =
      Resp.a.value('@trip_no','nvarchar(50)')
  AND Req.a.value('@route_no','nvarchar(50)') =
      Resp.a.value('@route_no','nvarchar(50)')
  AND Req.a.value('@trip_date','datetime') =
      Resp.a.value('@trip_date','datetime')
order by Req.a.value('@operator_code','nvarchar(50)');

SELECT s.*
INTO #FinalSuccess
FROM #ParsedBackupSuccessNodes s;

SELECT DISTINCT FORMAT(TripDate,'yyyyMM') AS YrPr
INTO #TripMonths
FROM #FinalSuccess;

CREATE TABLE #ResultS
(
    OperatorCode NVARCHAR(50),
    RouteNo      NVARCHAR(50),
    TripNo       NVARCHAR(50),
    Type         NVARCHAR(10),
    Date         DATETIME,
    Time         NVARCHAR(10),
    PlateNo      NVARCHAR(50),
    Remark       NVARCHAR(255),
    Position     INT,
    TripDate     DATETIME
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
	[Date],
    f.Time,
    f.PlateNo,
    f.Remark,
    f.Position,
	TripDate
FROM #FinalSuccess f
LEFT JOIN DerWaybill_' + YrPr + ' w
    ON w.TID = f.TripNo
WHERE FORMAT(f.TripDate,''yyyyMM'') = ''' + YrPr + '''
  AND (w.GateN2 IS NULL OR LTRIM(RTRIM(w.GateN2)) = '''')
;'
FROM #TripMonths;

EXEC sp_executesql @SQL;

SELECT 
	OperatorCode,
    RouteNo,
    TripNo,
    Type,
	CONVERT(VARCHAR(10), s.Date, 120) AS [Date],
    Time,
    PlateNo,
    Remark,
    Position,
	CONVERT(VARCHAR(10), s.TripDate, 120) AS TripDate, 
	@AdhocArr AS AdhocArr
FROM #ResultS s
ORDER BY s.TripDate DESC;

-- Cleanup
DROP TABLE #BatchSuccess;
DROP TABLE #SuccessLogs;
DROP TABLE #ParsedBackupSuccessNodes;
DROP TABLE #ResultS;
DROP TABLE #FinalSuccess;
DROP TABLE #TripMonths;
