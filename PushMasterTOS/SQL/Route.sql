--Route
SELECT 
    R.SComp AS OperatorCode,
    R.RID AS RouteNo,
    R.Desn AS RouteName,
    (SELECT TOP 1 Cout FROM TRouteCout WHERE RID = R.RID ORDER BY Posi ASC) AS OriginCity,
    (SELECT TOP 1 Cout FROM TRouteCout WHERE RID = R.RID ORDER BY Posi DESC) AS DestinationCity
INTO #TempR
FROM TRoute R

SELECT * FROM #TempR

--Route Details
SELECT 
	R.OperatorCode,
	R.RouteNo,
	C.TOSDis AS Display,
	C.Cout AS ViaCity,
	C.Posi AS StageNo
FROM TRouteCout C
INNER JOIN #TempR R ON C.RID = R.RouteNo

DROP TABLE IF EXISTS #TempR 