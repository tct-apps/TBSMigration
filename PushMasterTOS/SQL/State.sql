SELECT 
	S.State AS StateCode,
	S.desn AS StateName
FROM SysState S
WHERE S.acti = 1