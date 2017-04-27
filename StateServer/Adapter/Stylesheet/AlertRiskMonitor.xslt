<?xml version="1.0" encoding="UTF-8" ?>
<stylesheet version="1.0" xmlns="http://www.w3.org/1999/XSL/Transform">
	<output method="xml" indent="yes" />
	<template match="*">
		<copy>
			<copy-of select="@*" />
			<apply-templates />
		</copy>
	</template>
	<template match="Account/Transactions/Transaction" />
	<template match="Account/Transactions/Transaction[Order[@Phase=2 and @LotBalance>0]]">
		<copy>
			<copy-of select="@*" />
			<copy-of select="Order[@Phase=2 and @LotBalance>0]" />
		</copy>
	</template>
</stylesheet>
