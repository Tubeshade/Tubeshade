#!/bin/sh
set -e

# dotnet-coverage starts the server in the current directory, which needs to be the publish directory for wwwroot
cd /tubeshade/source/Tubeshade.Server/bin/Debug/net"$DOTNET_CHANNEL"/"$DOTNET_RUNTIME"/publish

dotnet dotnet-coverage collect \
	--output /reports/report.cobertura.xml \
	--output-format cobertura \
	-- ./Tubeshade.Server

dotnet reportgenerator \
	-reports:/reports/report.cobertura.xml \
	-reporttypes:Cobertura \
	-targetdir:/reports/ \
	"-assemblyfilters:-dbup-core;-dbup-postgresql;-Dapper;-YoutubeDLSharp;-Cronos" \
	"-filefilters:-*.g.cs"

rm /reports/report.cobertura.xml
mv /reports/Cobertura.xml /reports/coverage.cobertura.xml
