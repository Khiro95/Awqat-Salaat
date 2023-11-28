<?xml version="1.0" encoding="utf-8"?>
<!--
    Copied from: https://stackoverflow.com/a/44766600/4644774
    Changes made:
        - Update wix namesapce to "http://wixtoolset.org/schemas/v4/wxs" to work with v4
        - Remove parts used to exclude .exe files, we only want to exclude .pdb files
-->
<xsl:stylesheet
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:wix="http://wixtoolset.org/schemas/v4/wxs"
    xmlns="http://schemas.microsoft.com/wix/2006/wi"

    version="1.0"
    exclude-result-prefixes="xsl wix"
>

    <xsl:output method="xml" indent="yes" omit-xml-declaration="yes" />

    <xsl:strip-space elements="*" />

    <!--
    Because WiX's Heat.exe only supports XSLT 1.0 and not XSLT 2.0 we cannot use `ends-with( haystack, needle )` (e.g. `ends-with( wix:File/@Source, '.exe' )`...
    ...but we can use this longer `substring` expression instead (see https://github.com/wixtoolset/issues/issues/5609 )
    -->
    
    <!-- Get the last 4 characters of a string using `substring( s, len(s) - 3 )`, it uses -3 and not -4 because XSLT uses 1-based indexes, not 0-based indexes. -->

    <xsl:key
        name="PdbToRemove"
        match="wix:Component[ substring( wix:File/@Source, string-length( wix:File/@Source ) - 3 ) = '.pdb' ]"
        use="@Id"
    />

    <!-- By default, copy all elements and nodes into the output... -->
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()" />
        </xsl:copy>
    </xsl:template>

    <!-- ...but if the element has the "PdbToRemove" key then don't render anything (i.e. removing it from the output) -->
    <xsl:template match="*[ self::wix:Component or self::wix:ComponentRef ][ key( 'PdbToRemove', @Id ) ]" />

</xsl:stylesheet>