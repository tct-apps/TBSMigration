SELECT 
	C.Cout AS CityCode,
	C.Desn AS CityName,
	C.State AS StateCode
FROM TCounter C
WHERE C.acti = 1