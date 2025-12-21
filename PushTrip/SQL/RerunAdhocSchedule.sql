/*========================================================
  STEP 1: Extract successful TripNo + TripDate
========================================================*/
SELECT DISTINCT
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
 
  --select * from #TempSuccessTrip
 
/*========================================================
  STEP 2: Determine Date Range
========================================================*/
DECLARE @DateFromSQL DATE;
DECLARE @DateToSQL   DATE;
 
SELECT
    @DateFromSQL = MIN(TripDate),
    @DateToSQL   = MAX(TripDate)
FROM #TempSuccessTrip;
 
--SELECT @DateFromSQL AS DateFrom, @DateToSQL AS DateTo;
 
/*========================================================
  STEP 3: Variables
========================================================*/
DECLARE @AdhocArr INT = 30;
 
DECLARE @YearMonthFrom INT = YEAR(@DateFromSQL) * 100 + MONTH(@DateFromSQL);
DECLARE @YearMonthTo   INT = YEAR(@DateToSQL) * 100 + MONTH(@DateToSQL);
DECLARE @CurrentYM INT = @YearMonthFrom;
 
DECLARE @PartSQL NVARCHAR(MAX);
 
/*========================================================
  STEP 4: Prepare Final Result Table
========================================================*/
IF OBJECT_ID('tempdb..#FinalResult') IS NOT NULL DROP TABLE #FinalResult;
 
CREATE TABLE #FinalResult
(
    OperatorCode NVARCHAR(50),
    RouteNo NVARCHAR(50),
    TripNo NVARCHAR(50),
    [Type] NVARCHAR(10),
    TripDate DATE,
    [Date] DATE,
    [Time] NVARCHAR(10),
    PlateNo NVARCHAR(50),
    Position INT,
    Remark NVARCHAR(255),
    AdhocArr INT
);
 
/*========================================================
  STEP 5: Loop through each month and insert results
========================================================*/
WHILE @CurrentYM <= @YearMonthTo
BEGIN
    SET @PartSQL = N';
    ;WITH CTE AS (
        SELECT 
            c.SComp AS OperatorCode,
            b.RID AS RouteNo,
            a.TripN AS TripNo,
            CASE
                WHEN c.CoutN <> d.Posi THEN ''DEP''
                WHEN d.Posi <> 0 THEN ''ARR''
            END AS [Type],
            CAST(a.DDate AS DATE) AS TripDate,
            CAST(b.SDate AS DATE) AS [Date],
            b.TTime AS [Time],
            a.BusN AS PlateNo,
            d.Posi AS Position,
            b.Remk AS Remark,
            @AdhocArr AS AdhocArr,
            ROW_NUMBER() OVER (
                PARTITION BY 
                    a.TripN,
                    CAST(a.DDate AS DATE)
                ORDER BY 
                    CASE 
                        WHEN c.CoutN <> d.Posi THEN 1  -- DEP first
                        WHEN d.Posi <> 0 THEN 2        -- ARR second
                    END,
                    b.TTime
            ) AS rn
        FROM DerInfo_' + CAST(@CurrentYM AS NVARCHAR(6)) + ' a
        INNER JOIN DerTimer_' + CAST(@CurrentYM AS NVARCHAR(6)) + ' b
            ON a.TID = b.TID
        INNER JOIN TRoute c
            ON a.RID = c.RID
        INNER JOIN DerCout_' + CAST(@CurrentYM AS NVARCHAR(6)) + ' d
            ON b.TID = d.TID
           AND b.Cout = d.Cout
        INNER JOIN #TempSuccessTrip t
            ON t.TripNo = a.TripN
           AND t.TripDate = CAST(a.DDate AS DATE)
        WHERE a.DDate BETWEEN @DateFromSQL AND @DateToSQL
          AND b.Cout = ''TBS''
          AND b.TTime <> ''''
          AND b.sflg = ''1''
          AND a.stat = ''1''
          AND (
                (CASE WHEN c.CoutN <> d.Posi THEN ''DEP'' ELSE ''ARR'' END) <> ''DEP''
             OR DATEADD(HOUR, -1, CAST(CAST(a.DDate AS DATETIME) AS DATETIME) 
                         + CAST(STUFF(STUFF(b.TTime,3,0,'':''),6,0,'':'') AS DATETIME)) > GETDATE()
          )
    )
    INSERT INTO #FinalResult
    SELECT
        OperatorCode,
        RouteNo,
        TripNo,
        [Type],
        TripDate,
        [Date],
        [Time],
        PlateNo,
        Position,
        Remark,
        AdhocArr
    FROM CTE
    WHERE rn = 1;
    ';
 
    EXEC sp_executesql
        @PartSQL,
        N'@AdhocArr INT, @DateFromSQL DATE, @DateToSQL DATE',
        @AdhocArr, @DateFromSQL, @DateToSQL;
 
    -- increment month
    SET @CurrentYM = YEAR(DATEADD(MONTH, 1, CAST(CAST(@CurrentYM AS VARCHAR(6)) + '01' AS DATE))) * 100
                   + MONTH(DATEADD(MONTH, 1, CAST(CAST(@CurrentYM AS VARCHAR(6)) + '01' AS DATE)));
END
 
/*========================================================
  STEP 6: Return Final Results
========================================================*/
SELECT * FROM #FinalResult
ORDER BY TripNo;
 
/*========================================================
  STEP 7: Cleanup
========================================================*/
DROP TABLE #TempSuccessTrip;
DROP TABLE #FinalResult;