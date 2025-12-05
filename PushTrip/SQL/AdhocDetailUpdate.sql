---------------------------------------------------------
-- Parameters passed from C# via Dapper:
-- @TripDate (DATE)
-- @GateNo (NVARCHAR)
-- @GateNo2 (NVARCHAR)
-- @Position (INT)
-- @TripNo (NVARCHAR)
-- @CompanyCode (NVARCHAR)
-- @AdhocID (NVARCHAR)
---------------------------------------------------------

SET NOCOUNT ON;

---------------------------------------------------------
-- Internal working variables
---------------------------------------------------------
DECLARE
    @ResultCode INT,
    @ErrDescription VARCHAR(200),
    @YrPr NVARCHAR(6),
    @TID NVARCHAR(20),
    @RID NVARCHAR(20),
    @BusN NVARCHAR(20),
    @Btyp NVARCHAR(10),
    @Cout NVARCHAR(10),
    @DefaultCounter NVARCHAR(10),
    @WID NVARCHAR(50);

---------------------------------------------------------
-- 1. Generate yyyyMM suffix for table names
---------------------------------------------------------
SET @YrPr = FORMAT(@TripDate,'yyyyMM');

---------------------------------------------------------
-- 2. Set default counter
---------------------------------------------------------
SET @DefaultCounter = 'TBS';

---------------------------------------------------------
-- 4. Retrieve trip information (Dynamic SQL)
---------------------------------------------------------
DECLARE @SQL NVARCHAR(MAX);

SET @SQL = '
    SELECT TOP 1 
        @TID_OUT = a.TID,
        @RID_OUT = a.RID,
        @BusN_OUT = a.BusN,
        @Btyp_OUT = a.Btyp,
        @Cout_OUT = c.Cout
    FROM DerInfo_' + @YrPr + ' a
    JOIN TRoute b ON a.RID = b.RID
    JOIN DerCout_' + @YrPr + ' c ON a.TID = c.TID
    WHERE c.Posi = @Position
      AND a.TripN = @TripNo
      AND b.SComp = @CompanyCode
      AND a.DDate = @TripDate
';

EXEC sp_executesql @SQL,
    N'@Position INT, @TripNo NVARCHAR(20), @CompanyCode NVARCHAR(10), @TripDate DATE,
      @TID_OUT NVARCHAR(20) OUTPUT, @RID_OUT NVARCHAR(20) OUTPUT, @BusN_OUT NVARCHAR(20) OUTPUT,
      @Btyp_OUT NVARCHAR(10) OUTPUT, @Cout_OUT NVARCHAR(10) OUTPUT',
    @Position, @TripNo, @CompanyCode, @TripDate,
    @TID_OUT=@TID OUTPUT,
    @RID_OUT=@RID OUTPUT,
    @BusN_OUT=@BusN OUTPUT,
    @Btyp_OUT=@Btyp OUTPUT,
    @Cout_OUT=@Cout OUTPUT;

IF @TID IS NULL
BEGIN
    RAISERROR('Record Not Found For Trip No', 16, 1);
    RETURN;
END

IF @Cout IS NULL
BEGIN
    RAISERROR('Record Not Found For Cout', 16, 1);
    RETURN;
END

---------------------------------------------------------
-- 5. Update DERTimer table
---------------------------------------------------------
SET @SQL = '
    UPDATE DERTimer_' + @YrPr + '
    SET Adhoc = @AdhocID
    WHERE TID = @TID AND Cout = @Cout
';

EXEC sp_executesql @SQL,
    N'@AdhocID NVARCHAR(50), @TID NVARCHAR(20), @Cout NVARCHAR(10)',
    @AdhocID, @TID, @Cout;

---------------------------------------------------------
-- 6. Handle DefaultCounter (TBS)
---------------------------------------------------------
IF @Cout = @DefaultCounter
BEGIN
    DECLARE @Exists INT;

    -- Check if waybill exists
    SET @SQL = '
        SELECT @Exists_OUT = COUNT(*)
        FROM DerWaybill_' + @YrPr + '
        WHERE TID = @TID
    ';

    EXEC sp_executesql @SQL,
        N'@TID NVARCHAR(20), @Exists_OUT INT OUTPUT',
        @TID, @Exists_OUT=@Exists OUTPUT;

    ---------------------------------------------------------
    -- If exists → UPDATE
    ---------------------------------------------------------
    IF @Exists = 1
    BEGIN
        SET @SQL = '
            UPDATE DerWaybill_' + @YrPr + '
            SET GateN = @GateNo, GateN2 = @GateNo2
            WHERE TID = @TID
        ';

        EXEC sp_executesql @SQL,
            N'@GateNo NVARCHAR(10), @GateNo2 NVARCHAR(10), @TID NVARCHAR(20)',
            @GateNo, @GateNo2, @TID;
    END
    ---------------------------------------------------------
    -- Else → INSERT
    ---------------------------------------------------------
    ELSE
    BEGIN
        SET @WID = FORMAT(GETDATE(), 'yyMMddHHmmss') + SUBSTRING(REPLACE(NEWID(), '-', ''), 1, 8);

        SET @SQL = '
            INSERT INTO DerWaybill_' + @YrPr + '
            (crid,crdt,lmid,lmdt,WID,TID,RID,DDate,BusN,Btyp,
             DrID1,DrID2,DrID3,Stat,TAllow,OAllow,SMill,EMill,TNG,TRate,
             B1,B2,B3,GateN,GateN2,DNam1,DNam2,DNam3)
            VALUES
            (''SYS'',GETDATE(),''SYS'',GETDATE(),
             @WID,@TID,@RID,@TripDate,@BusN,@Btyp,
             '' '','' '','' '',1,0,0,0,0,'' '',0,
             '' '','' '','' '',@GateNo,@GateNo2,'' '','' '','' '')
        ';

        EXEC sp_executesql @SQL,
            N'@WID NVARCHAR(50), @TID NVARCHAR(20), @RID NVARCHAR(20),
              @TripDate DATE, @BusN NVARCHAR(20), @Btyp NVARCHAR(10),
              @GateNo NVARCHAR(10), @GateNo2 NVARCHAR(10)',
            @WID, @TID, @RID, @TripDate, @BusN, @Btyp, @GateNo, @GateNo2;
    END

    ---------------------------------------------------------
    -- Insert into TripLog
    ---------------------------------------------------------
    INSERT INTO TripLog (crid, crdt, TID, Modu, Acti, GateN, GateN2)
    VALUES ('TOS', GETDATE(), @TID, 'G', 'E', @GateNo, @GateNo2);
END
