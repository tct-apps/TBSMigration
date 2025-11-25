SELECT 
	C.Cout AS city_code,
	C.Desn AS city_name,
	C.State AS state_code
FROM TCounter C
WHERE C.acti = 1