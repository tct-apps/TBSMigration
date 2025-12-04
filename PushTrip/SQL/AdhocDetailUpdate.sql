CREATE OR ALTER PROCEDURE UpdateAdhocID
(
    @TripDate DATE,
    @GateNo NVARCHAR(10),
    @GateNo2 NVARCHAR(10),
    @Position INT,
    @TripNo NVARCHAR(20),
    @CompanyCode NVARCHAR(10),
    @AdhocID NVARCHAR(50)
)
AS
BEGIN
    SET NOCOUNT ON;

	DECLARE @ResultCode INT,
			@ErrDescription VARCHAR(200)

    DECLARE @YrPr NVARCHAR(6),
            @TID NVARCHAR(20),
            @RID NVARCHAR(20),
            @BusN NVARCHAR(20),
            @Btyp NVARCHAR(10),
            @Cout NVARCHAR(10),
            @DefaultCounter NVARCHAR(10),
            @WID NVARCHAR(50);

    -- yyyyMM
    SET @YrPr = FORMAT(@TripDate,'yyyyMM');

    -- 1. Set DefaultCounter
    SELECT @DefaultCounter = 'TBS';

    -- 2. Check Gate
    IF NOT EXISTS (SELECT 1 FROM TGate WHERE GateN = @GateNo)
    BEGIN
        SET @ResultCode = 99;
        SET @ErrDescription = 'Invalid Gate No';
        RETURN;
    END

    -- 3. Get Trip info
    DECLARE @SQL NVARCHAR(MAX) = '
        SELECT TOP 1 @TID_OUT = a.TID,
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
        N'@Position INT, @TripNo NVARCHAR(20), @CompanyCode NVARCHAR(10),
          @TripDate DATE,
          @TID_OUT NVARCHAR(20) OUTPUT,
          @RID_OUT NVARCHAR(20) OUTPUT,
          @BusN_OUT NVARCHAR(20) OUTPUT,
          @Btyp_OUT NVARCHAR(10) OUTPUT,
          @Cout_OUT NVARCHAR(10) OUTPUT',
        @Position, @TripNo, @CompanyCode, @TripDate,
        @TID_OUT=@TID OUTPUT,
        @RID_OUT=@RID OUTPUT,
        @BusN_OUT=@BusN OUTPUT,
        @Btyp_OUT=@Btyp OUTPUT,
        @Cout_OUT=@Cout OUTPUT;

    IF @TID IS NULL
    BEGIN
        SET @ResultCode = 99;
        SET @ErrDescription = 'Record Not Found For Trip No ' + @TripNo;
        RETURN;
    END

    -- 4. Update DERTimer
    SET @SQL = '
        UPDATE DERTimer_' + @YrPr + '
        SET Adhoc = @AdhocID
        WHERE TID = @TID AND Cout = @Cout
    ';

    EXEC sp_executesql @SQL,
        N'@AdhocID NVARCHAR(50), @TID NVARCHAR(20), @Cout NVARCHAR(10)',
        @AdhocID, @TID, @Cout;

    -- 5. If Cout = DefaultCounter
    IF @Cout = @DefaultCounter
    BEGIN
        -- Check Waybill exists
        DECLARE @Exists INT = 0;
        SET @SQL = '
            SELECT @Exists_OUT = COUNT(*)
            FROM DerWaybill_' + @YrPr + '
            WHERE TID = @TID
        ';

        EXEC sp_executesql @SQL,
            N'@TID NVARCHAR(20), @Exists_OUT INT OUTPUT',
            @TID, @Exists_OUT=@Exists OUTPUT;

        IF @Exists = 1
        BEGIN
            SET @SQL = '
                UPDATE DerWaybill_' + @YrPr + '
                SET GateN=@GateNo, GateN2=@GateNo2
                WHERE TID=@TID
            ';

            EXEC sp_executesql @SQL,
                N'@GateNo NVARCHAR(10), @GateNo2 NVARCHAR(10), @TID NVARCHAR(20)',
                @GateNo, @GateNo2, @TID;
        END
        ELSE
        BEGIN
            SET @WID = NEWID(); -- random ID

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
                N'@WID NVARCHAR(50),@TID NVARCHAR(20),@RID NVARCHAR(20),
                  @TripDate DATE,@BusN NVARCHAR(20),@Btyp NVARCHAR(10),
                  @GateNo NVARCHAR(10),@GateNo2 NVARCHAR(10)',
                @WID,@TID,@RID,@TripDate,@BusN,@Btyp,@GateNo,@GateNo2;
        END

        -- Insert TripLog
        INSERT INTO TripLog (crid, crdt, TID, Modu, Acti, GateN, GateN2)
        VALUES ('TOS', GETDATE(), @TID, 'G', 'E', @GateNo, @GateNo2);
    END

    SET @ResultCode = 0;
    SET @ErrDescription = 'Success';
END