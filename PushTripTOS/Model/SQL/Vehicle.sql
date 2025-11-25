SELECT 
	B.Busn AS plate_no,
	B.SComp AS operator_code
FROM BusInfo B
WHERE B.acti = 1