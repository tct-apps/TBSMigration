
DECLARE @DateFrom VARCHAR(10) = '20251216';
DECLARE @DateTo VARCHAR(10) = '20261231';

DECLARE @AdhocArr INT = 0;
SELECT @AdhocArr = ISNULL(CAST(30 AS INT), 0) FROM sysPara;

DECLARE @DateFromSQL DATE = CONVERT(DATE, @DateFrom, 103);
DECLARE @DateToSQL   DATE = CONVERT(DATE, @DateTo, 103);

DECLARE @YearMonthFrom INT = YEAR(@DateFromSQL) * 100 + MONTH(@DateFromSQL);
DECLARE @YearMonthTo   INT = YEAR(@DateToSQL) * 100 + MONTH(@DateToSQL);
DECLARE @CurrentYM INT = @YearMonthFrom;

DECLARE @SQL NVARCHAR(MAX) = N'';
DECLARE @PartSQL NVARCHAR(MAX);

SET @DateFromSQL = CONVERT(DATE, @DateFrom, 103);
SET @DateToSQL = CONVERT(DATE, @DateTo, 103);

SET @YearMonthFrom = YEAR(@DateFromSQL) * 100 + MONTH(@DateFromSQL);
SET @YearMonthTo = YEAR(@DateToSQL) * 100 + MONTH(@DateToSQL);
SET @CurrentYM = @YearMonthFrom;

WHILE @CurrentYM <= @YearMonthTo
BEGIN
    SET @PartSQL = N'
    SELECT 
        c.SComp AS OperatorCode,
        b.RID AS RouteNo,
        a.TripN AS TripNo,
        CASE
            WHEN c.CoutN <> d.Posi THEN ''DEP''
            WHEN d.Posi <> 0 THEN ''ARR''
            ELSE NULL
        END AS [Type],
        CONVERT(VARCHAR(10), a.DDate, 120) AS TripDate,
        CONVERT(VARCHAR(10), b.SDate, 120) AS [Date],
        b.TTime AS [Time],
        a.BusN AS PlateNo,
        d.Posi AS Position,
        b.Remk AS Remark,
        @AdhocArr AS AdhocArr
    FROM DerInfo_' + CAST(@CurrentYM AS VARCHAR(6)) + ' a
    INNER JOIN DerTimer_' + CAST(@CurrentYM AS VARCHAR(6)) + ' b ON a.TID = b.TID
    INNER JOIN TRoute c ON a.RID = c.RID
    INNER JOIN DerCout_' + CAST(@CurrentYM AS VARCHAR(6)) + ' d ON b.TID = d.TID AND b.Cout = d.Cout
    WHERE a.DDate BETWEEN @DateFromSQL AND @DateToSQL
      AND b.Cout = ''TBS''
      AND b.TTime <> ''''
      AND b.sflg = ''1''
      AND a.stat = ''1''
	';

    SET @SQL = CASE WHEN @SQL = '' THEN @PartSQL ELSE @SQL + ' UNION ALL ' + @PartSQL END;

    SET @CurrentYM = YEAR(DATEADD(MONTH, 1, CAST(CAST(@CurrentYM AS VARCHAR(6)) + '01' AS DATE))) * 100
                   + MONTH(DATEADD(MONTH, 1, CAST(CAST(@CurrentYM AS VARCHAR(6)) + '01' AS DATE)));
END

IF @SQL = '' 
    SET @SQL = 'SELECT * FROM sysPara WHERE dcout = ''ZZZZ''';

EXEC sp_executesql 
    @SQL,
    N'@AdhocArr INT, @DateFromSQL DATE, @DateToSQL DATE',
    @AdhocArr = @AdhocArr,
    @DateFromSQL = @DateFromSQL,
    @DateToSQL = @DateToSQL;