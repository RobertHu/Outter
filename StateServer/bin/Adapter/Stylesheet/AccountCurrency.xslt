<?xml version="1.0" encoding="UTF-8" ?>
<stylesheet version="1.0" xmlns="http://www.w3.org/1999/XSL/Transform">
	<output method="xml" indent="yes" />
	<param name="currencyID" />
	<template match="Account">
		<copy>
			<copy-of select="@*" />
			<copy-of select="Currency[@ID=$currencyID]" />
		</copy>
	</template>
</stylesheet>
