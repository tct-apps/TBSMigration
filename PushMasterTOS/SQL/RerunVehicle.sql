SELECT DISTINCT CustomData
INTO #temp
FROM dbo.LogMigrationProcess
WHERE Type = 'Vehicle'
AND IsSuccess = 0

--Vehicle
SELECT 
	B.Busn AS PlateNo,
	B.SComp AS OperatorCode
FROM BusInfo B
INNER JOIN #temp t ON t.CustomData = B.Busn

DROP TABLE IF EXISTS #temp