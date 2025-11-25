SELECT
    S.SComp AS operator_code,
    S.Desn AS operator_name,
    I.IMGBIN AS operator_logo,
    S.Cont AS contact_person,
    S.Add1 AS address1,
    S.Add2 AS address2,
    S.Add3 AS address3,
    S.MTel AS contact_number1,
    S.OTel AS contact_number2,
    S.FTel AS fax_number,
    S.Emai AS email_id,
    S.WSite AS website,
    S.Remk AS description,
    S.RegNo AS register_no
FROM SysComp S
LEFT JOIN MImage I ON I.IMGID = S.IMGID