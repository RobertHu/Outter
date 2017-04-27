<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
	<xsl:output method="xml" indent="yes" />
	<xsl:template match="Transaction">
		<Transaction>
			<xsl:attribute name="ID">
				<xsl:value-of select="@ID" />
			</xsl:attribute>
			<xsl:if test="@Code">
				<xsl:attribute name="Code">
					<xsl:value-of select="@Code" />
				</xsl:attribute>
			</xsl:if>
			<xsl:attribute name="InstrumentID">
				<xsl:value-of select="@InstrumentID" />
			</xsl:attribute>
      <xsl:attribute name="InstrumentCategory">
        <xsl:value-of select="@InstrumentCategory" />
      </xsl:attribute>
			<xsl:attribute name="AccountID">
				<xsl:value-of select="@AccountID" />
			</xsl:attribute>
			<xsl:attribute name="Type">
				<xsl:value-of select="@Type" />
			</xsl:attribute>
			<xsl:attribute name="SubType">
				<xsl:value-of select="@SubType" />
			</xsl:attribute>
			<xsl:attribute name="Phase">
				<xsl:value-of select="@Phase" />
			</xsl:attribute>
			<xsl:attribute name="OrderType">
				<xsl:value-of select="@OrderType" />
			</xsl:attribute>
			<xsl:attribute name="BeginTime">
				<xsl:value-of select="@BeginTime" />
			</xsl:attribute>
			<xsl:attribute name="EndTime">
				<xsl:value-of select="@EndTime" />
			</xsl:attribute>
			<xsl:attribute name="ExpireType">
				<xsl:value-of select="@ExpireType" />
			</xsl:attribute>
			<xsl:attribute name="SubmitTime">
				<xsl:value-of select="@SubmitTime" />
			</xsl:attribute>
			<xsl:if test="@ExecuteTime">
				<xsl:attribute name="ExecuteTime">
					<xsl:value-of select="@ExecuteTime" />
				</xsl:attribute>
			</xsl:if>
			<xsl:attribute name="SubmitorID">
				<xsl:value-of select="@SubmitorID" />
			</xsl:attribute>
			<xsl:if test="@AssigningOrderID">
				<xsl:attribute name="AssigningOrderID">
					<xsl:value-of select="@AssigningOrderID" />
				</xsl:attribute>
			</xsl:if>
			<xsl:apply-templates />
		</Transaction>
	</xsl:template>
	<xsl:template match="Transaction/Order">
		<Order>
			<xsl:attribute name="ID">
				<xsl:value-of select="@ID" />
			</xsl:attribute>
			<xsl:if test="@Code">
				<xsl:attribute name="Code">
					<xsl:value-of select="@Code" />
				</xsl:attribute>
			</xsl:if>
			<xsl:attribute name="TradeOption">
				<xsl:value-of select="@TradeOption" />
			</xsl:attribute>
			<xsl:attribute name="IsOpen">
				<xsl:value-of select="@IsOpen" />
			</xsl:attribute>
			<xsl:attribute name="IsBuy">
				<xsl:value-of select="@IsBuy" />
			</xsl:attribute>
			<xsl:if test="@SetPrice">
				<xsl:attribute name="SetPrice">
					<xsl:value-of select="@SetPrice" />
				</xsl:attribute>
			</xsl:if>
      <xsl:attribute name="PhysicalTradeSide">
        <xsl:value-of select="@PhysicalTradeSide" />
      </xsl:attribute>
      <xsl:if test="@PhysicalOriginValue">
        <xsl:attribute name="PhysicalOriginValue">
          <xsl:value-of select="@PhysicalOriginValue" />
        </xsl:attribute>
      </xsl:if>
      <xsl:if test="@PhysicalPaidAmount">
        <xsl:attribute name="PhysicalPaidAmount">
          <xsl:value-of select="@PhysicalPaidAmount" />
        </xsl:attribute>
      </xsl:if>
      <xsl:if test="@PaidPledge">
        <xsl:attribute name="PaidPledge">
          <xsl:value-of select="@PaidPledge" />
        </xsl:attribute>
      </xsl:if>
      <xsl:if test="@PaidPledgeBalance">
        <xsl:attribute name="PaidPledgeBalance">
          <xsl:value-of select="@PaidPledgeBalance" />
        </xsl:attribute>
      </xsl:if>
      <xsl:if test="@PhysicalRequestId">
        <xsl:attribute name="PhysicalRequestId">
          <xsl:value-of select="@PhysicalRequestId" />
        </xsl:attribute>
      </xsl:if>
      <xsl:if test="@InstalmentPolicyId">
        <xsl:attribute name="InstalmentPolicyId">
          <xsl:value-of select="@InstalmentPolicyId" />
        </xsl:attribute>
      </xsl:if>
      <xsl:if test="@InstalmentPolicyId">
        <xsl:attribute name="Period">
          <xsl:value-of select="@Period" />
        </xsl:attribute>
      </xsl:if>
      <xsl:if test="@InstalmentPolicyId">
        <xsl:attribute name="DownPayment">
          <xsl:value-of select="@DownPayment" />
        </xsl:attribute>
      </xsl:if>
      <xsl:if test="@InstalmentPolicyId">
        <xsl:attribute name="DownPaymentBasis">
          <xsl:value-of select="@DownPaymentBasis" />
        </xsl:attribute>
      </xsl:if>
      <xsl:if test="@InstalmentPolicyId">
        <xsl:attribute name="PhysicalInstalmentType">
          <xsl:value-of select="@PhysicalInstalmentType" />
        </xsl:attribute>
      </xsl:if>
      <xsl:if test="@InstalmentPolicyId">
        <xsl:attribute name="RecalculateRateType">
          <xsl:value-of select="@RecalculateRateType" />
        </xsl:attribute>
      </xsl:if>
      <xsl:if test="@InstalmentPolicyId">
        <xsl:attribute name="InstalmentAdministrationFee">
          <xsl:value-of select="@InstalmentAdministrationFee" />
        </xsl:attribute>
      </xsl:if>
      <xsl:if test="@InstalmentPolicyId">
        <xsl:attribute name="InstalmentFrequence">
          <xsl:value-of select="@InstalmentFrequence" />
        </xsl:attribute>
      </xsl:if>
      <xsl:attribute name="InterestValueDate">
        <xsl:value-of select="@InterestValueDate" />
      </xsl:attribute>      
			<xsl:if test="@SetPrice2">
				<xsl:attribute name="SetPrice2">
					<xsl:value-of select="@SetPrice2" />
				</xsl:attribute>
			</xsl:if>
			<xsl:if test="@SetPriceMaxMovePips">
				<xsl:attribute name="SetPriceMaxMovePips">
					<xsl:value-of select="@SetPriceMaxMovePips" />
				</xsl:attribute>
			</xsl:if>
			<xsl:if test="@DQMaxMove">
				<xsl:attribute name="DQMaxMove">
					<xsl:value-of select="@DQMaxMove" />
				</xsl:attribute>
			</xsl:if>
			<xsl:attribute name="Lot">
				<xsl:value-of select="@Lot" />
			</xsl:attribute>
      <xsl:attribute name="TotalDeposit">
        <xsl:value-of select="@TotalDeposit" />
      </xsl:attribute>
      <xsl:attribute name="Equity">
        <xsl:value-of select="@Equity" />
      </xsl:attribute>      
      <xsl:if test="@MinLot">
        <xsl:attribute name="MinLot">
          <xsl:value-of select="@MinLot" />
        </xsl:attribute>
      </xsl:if>
      <xsl:if test="@MaxShow">
        <xsl:attribute name="MaxShow">
          <xsl:value-of select="@MaxShow" />
        </xsl:attribute>
      </xsl:if>
      <xsl:attribute name="OriginalLot">
				<xsl:value-of select="@OriginalLot" />
			</xsl:attribute>
			<xsl:attribute name="LotBalance">
				<xsl:value-of select="@LotBalance" />
			</xsl:attribute>
      <xsl:attribute name="PlacedByRiskMonitor">
        <xsl:value-of select="@PlacedByRiskMonitor" />
      </xsl:attribute>      
      <xsl:if test="@BOBetTypeID">
        <xsl:attribute name="BOBetTypeID">
          <xsl:value-of select="@BOBetTypeID" />
        </xsl:attribute>
      </xsl:if>
      <xsl:if test="@BOFrequency">
        <xsl:attribute name="BOFrequency">
          <xsl:value-of select="@BOFrequency" />
        </xsl:attribute>
      </xsl:if>      
      <xsl:if test="@BOOdds">
        <xsl:attribute name="BOOdds">
          <xsl:value-of select="@BOOdds" />
        </xsl:attribute>
      </xsl:if>
      <xsl:if test="@BOBetOption">
        <xsl:attribute name="BOBetOption">
          <xsl:value-of select="@BOBetOption" />
        </xsl:attribute>
      </xsl:if>      
      <xsl:apply-templates />
		</Order>
	</xsl:template>
	<xsl:template match="Transaction/Order/OrderRelation">
		<OrderRelation>
			<xsl:attribute name="OpenOrderID">
				<xsl:value-of select="@OpenOrderID" />
			</xsl:attribute>
			<xsl:attribute name="ClosedLot">
				<xsl:value-of select="@ClosedLot" />
			</xsl:attribute>
      <xsl:if test="@PhysicalValue">
        <xsl:attribute name="PhysicalValue">
          <xsl:value-of select="@PhysicalValue" />
        </xsl:attribute>
      </xsl:if>      
      <xsl:if test="@PhysicalValueMatureDate">
        <xsl:attribute name="PhysicalValueMatureDate">
          <xsl:value-of select="@PhysicalValueMatureDate" />
        </xsl:attribute>
      </xsl:if>
      <xsl:if test="@PayBackPledge">
        <xsl:attribute name="PayBackPledge">
          <xsl:value-of select="@PayBackPledge" />
        </xsl:attribute>
      </xsl:if>
		</OrderRelation>
	</xsl:template>
</xsl:stylesheet>
