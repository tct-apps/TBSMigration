--Route
SELECT 
    R.SComp AS operator_code,
    R.RID AS route_no,
    R.Desn AS route_name,
    (SELECT TOP 1 Cout FROM TRouteCout WHERE RID = R.RID ORDER BY Posi ASC) AS origin_city,
    (SELECT TOP 1 Cout FROM TRouteCout WHERE RID = R.RID ORDER BY Posi DESC) AS destination_city
INTO #TempR
FROM TRoute R

SELECT * FROM #TempR

--Route Details
SELECT 
	R.operator_code,
	R.route_no,
	C.TOSDis AS display,
	C.Cout AS via_city,
	C.Posi AS stage_no
FROM TRouteCout C
INNER JOIN #TempR R ON C.RID = R.route_no

DROP TABLE IF EXISTS #TempR 