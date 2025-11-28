DECLARE
    @DateFrom VARCHAR(10) = '01/08/2025',
    @DateTo   VARCHAR(10) = '01/12/2025'

SET NOCOUNT ON;

DECLARE @AdhocArr INT = 0;
SELECT @AdhocArr = ISNULL(CAST(30 AS INT), 0) FROM sysPara;

DECLARE @DateFromSQL DATE = CONVERT(DATE, @DateFrom, 103);
DECLARE @DateToSQL   DATE = CONVERT(DATE, @DateTo, 103);

DECLARE @YearMonthFrom INT = YEAR(@DateFromSQL) * 100 + MONTH(@DateFromSQL);
DECLARE @YearMonthTo   INT = YEAR(@DateToSQL) * 100 + MONTH(@DateToSQL);
DECLARE @CurrentYM INT = @YearMonthFrom;

DECLARE @SQL NVARCHAR(MAX) = N'';
DECLARE @PartSQL NVARCHAR(MAX);

WHILE @CurrentYM <= @YearMonthTo
BEGIN
    SET @PartSQL = '
    SELECT 
        c.SComp      AS OperatorCode,
        b.RID        AS RouteNo,
        a.TripN      AS TripNo,
        CASE
            WHEN ISNULL(c.CoutN, 0) <> ISNULL(d.Posi, 0) THEN ''DEP''                  
            WHEN ISNULL(d.Posi, 0) <> 0 THEN ''ARR''                                    
            ELSE NULL
        END AS [Type],
        CONVERT(VARCHAR(10), a.DDate, 120) AS TripDate,
        CONVERT(VARCHAR(10), b.SDate, 120) AS [Date],
        b.TTime AS [Time],
        REPLACE(a.BusN, ''.'', '''') AS PlateNo,
        d.Posi AS Position,
        b.Remk AS Remark,
		CAST(' + CAST(@AdhocArr AS VARCHAR(10)) + ' AS INT) AS AdhocArr

    FROM DerInfo_' + CAST(@CurrentYM AS VARCHAR(6)) + ' a
    INNER JOIN DerTimer_' + CAST(@CurrentYM AS VARCHAR(6)) + ' b ON a.TID = b.TID
    INNER JOIN TRoute c ON a.RID = c.RID
    INNER JOIN DerCout_' + CAST(@CurrentYM AS VARCHAR(6)) + ' d ON b.TID = d.TID AND b.Cout = d.Cout
    WHERE a.DDate BETWEEN ''' + CONVERT(VARCHAR(10), @DateFromSQL, 120) + ''' AND ''' + CONVERT(VARCHAR(10), @DateToSQL, 120) + '''
      AND b.Cout = ''' + 'TBS' + '''
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

EXEC sp_executesql @SQL;