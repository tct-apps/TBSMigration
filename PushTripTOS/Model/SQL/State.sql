SELECT 
	S.State AS state_code,
	S.desn AS state_name
FROM SysState S
WHERE S.acti = 1