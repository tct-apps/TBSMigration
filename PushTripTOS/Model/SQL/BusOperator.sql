SELECT
    S.SComp AS OperatorCode,
    S.Desn AS OperatorName,
    I.IMGBIN AS OperatorLogo,
    S.Cont AS ContactPerson,
    S.Add1 AS Address1,
    S.Add2 AS Address2,
    S.Add3 AS Address3,
    S.MTel AS ContactNumber1,
    S.OTel AS ContactNumber2,
    S.FTel AS FaxNumber,
    S.Emai AS EmailId,
    S.WSite AS Website,
    S.Remk AS Description,
    S.RegNo AS RegisterNo
FROM SysComp S
LEFT JOIN MImage I ON I.IMGID = S.IMGID