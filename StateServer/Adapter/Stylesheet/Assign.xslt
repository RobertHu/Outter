<?xml version="1.0" encoding="UTF-8" ?>
<stylesheet version="1.0" xmlns="http://www.w3.org/1999/XSL/Transform">
	<output method="xml" indent="yes" />
	<template match="Transaction">
		<copy-of select="." />
	</template>
</stylesheet>
