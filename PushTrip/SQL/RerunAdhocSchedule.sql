/*========================================================
  STEP 1: Extract successful TripNo + TripDate
========================================================*/
SELECT 
    CAST(j.value AS VARCHAR(50)) AS TripNo,
    CAST(SUBSTRING(
            l.Message,
            CHARINDEX('Date:', l.Message) + 6,
            10
        ) AS DATE) AS TripDate
INTO #TempSuccessTrip
FROM LogMigrationProcess l
CROSS APPLY OPENJSON(l.CustomData, '$.success') j
WHERE l.Type = 'Trip'
  AND l.Process = 'Insert'
  AND l.IsSuccess = 0
  AND ISJSON(l.CustomData) = 1;


/*========================================================
  STEP 2: Determine Date Range
========================================================*/
DECLARE @DateFromSQL DATE;
DECLARE @DateToSQL   DATE;

SELECT
    @DateFromSQL = MIN(TripDate),
    @DateToSQL   = MAX(TripDate)
FROM #TempSuccessTrip;

/*========================================================
  STEP 3: Variables
========================================================*/
DECLARE @AdhocArr INT = 30;

DECLARE @YearMonth CHAR(6) =
    CONVERT(CHAR(6), YEAR(@DateFromSQL) * 100 + MONTH(@DateFromSQL));

DECLARE @SQL NVARCHAR(MAX);


/*========================================================
  STEP 4: Build Dynamic SQL
========================================================*/
SET @SQL = N'
SELECT 
    c.SComp AS OperatorCode,
    b.RID AS RouteNo,
    a.TripN AS TripNo,
    CASE
        WHEN c.CoutN <> d.Posi THEN ''DEP''
        WHEN d.Posi <> 0 THEN ''ARR''
        ELSE NULL
    END AS [Type],
    CAST(a.DDate AS DATE) AS TripDate,
    CAST(b.SDate AS DATE) AS [Date],
    b.TTime AS [Time],
    a.BusN AS PlateNo,
    d.Posi AS Position,
    b.Remk AS Remark,
    @AdhocArr AS AdhocArr
FROM DerInfo_' + @YearMonth + ' a
INNER JOIN DerTimer_' + @YearMonth + ' b
    ON a.TID = b.TID
INNER JOIN TRoute c
    ON a.RID = c.RID
INNER JOIN DerCout_' + @YearMonth + ' d
    ON b.TID = d.TID
   AND b.Cout = d.Cout
WHERE a.DDate BETWEEN @DateFromSQL AND @DateToSQL
  AND b.Cout = ''TBS''
  AND b.TTime <> ''''
  AND b.sflg = ''1''
  AND a.stat = ''1''
  AND EXISTS (
        SELECT 1
        FROM #TempSuccessTrip t
        WHERE t.TripNo = a.TripN
          AND t.TripDate = CAST(a.DDate AS DATE)
  )
ORDER BY a.TripN;
';


/*========================================================
  STEP 5: Execute
========================================================*/
EXEC sp_executesql
    @SQL,
    N'@AdhocArr INT, @DateFromSQL DATE, @DateToSQL DATE',
    @AdhocArr,
    @DateFromSQL,
    @DateToSQL;


/*========================================================
  STEP 6: Cleanup
========================================================*/
DROP TABLE #TempSuccessTrip;
