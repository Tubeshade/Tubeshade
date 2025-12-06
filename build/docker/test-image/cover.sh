#!/bin/sh
set -e

dotnet tool restore

dotnet dotCover cover \
	--TargetExecutable /tubeshade/source/Tubeshade.Server/bin/Debug/net"$DOTNET_CHANNEL"/"$DOTNET_RUNTIME"/publish/Tubeshade.Server \
	--Output /reports/report.xml \
	--ReportType DetailedXML

dotnet reportgenerator \
	-reports:/reports/report.xml \
	-reporttypes:Cobertura \
	-targetdir:/reports/ \
	"-assemblyfilters:-dbup-core;-dbup-postgresql;-Dapper;-YoutubeDLSharp" \
	"-filefilters:-*.g.cs"

rm /reports/report.xml
mv /reports/Cobertura.xml /reports/coverage.cobertura.xml
