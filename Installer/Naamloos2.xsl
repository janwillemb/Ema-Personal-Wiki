<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
    <xsl:template match="/Questionnaire">
        <div class="questionnaire" id="{@name}">
            <xsl:apply-templates select="@title"/>
            <xsl:apply-templates />
        </div>
    </xsl:template>
    
    <xsl:template match="Page">
        <div class="page" id="{@name}">
            <xsl:apply-templates select="@title"/>
            <xsl:apply-templates />
        </div>
    </xsl:template>
    
    <xsl:template match="Questionnaire/@title">
        <h1><xsl:value-of select="."/></h1>
    </xsl:template>
    
    <xsl:template match="Page/@title">
        <h2><xsl:value-of select="."/></h2>
    </xsl:template>
    
</xsl:stylesheet>
