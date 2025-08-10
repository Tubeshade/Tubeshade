#!/bin/bash
set -e

archive_path="$1"
version=$(tr -d '\r' <version)
full_version="$version.$2"
changelog_path="tubeshade/usr/share/doc/tubeshade/changelog.gz"
maintainer_email="valters.melnalksnis@tubeshade.org"
maintainer="Valters Melnalksnis <$maintainer_email>"

mkdir -p tubeshade/opt/tubeshade
rm -rf tubeshade/opt/tubeshade/*
unzip "$archive_path" -d tubeshade/opt/tubeshade

chmod +x tubeshade/opt/tubeshade/Tubeshade.Server

mkdir -p tubeshade/DEBIAN
cp build/debian/debian/postinst tubeshade/DEBIAN/postinst
cp build/debian/debian/prerm tubeshade/DEBIAN/prerm

export FULL_VERSION=$full_version
export MAINTAINER=$maintainer
envsubst <build/debian/debian/control >tubeshade/DEBIAN/control
cat tubeshade/DEBIAN/control

mkdir -p tubeshade/etc/opt/tubeshade
mv tubeshade/opt/tubeshade/appsettings.json tubeshade/etc/opt/tubeshade/appsettings.json
echo "/etc/opt/tubeshade/appsettings.json" >>tubeshade/DEBIAN/conffiles

mkdir -p tubeshade/usr/share/doc/tubeshade
export MAINTAINER_EMAIL=$maintainer_email
envsubst <build/debian/debian/copyright >tubeshade/usr/share/doc/tubeshade/copyright
cat tubeshade/usr/share/doc/tubeshade/copyright

envsubst <build/debian/debian/changelog >changelog
cat changelog

gzip -n --best changelog
mv changelog.gz $changelog_path

mkdir -p tubeshade/lib/systemd/system
cp build/debian/debian/tubeshade.service tubeshade/lib/systemd/system/tubeshade.service

tree tubeshade

dpkg-deb --root-owner-group -Zxz --build tubeshade

# unstripped-binary-or-object suppressed because tubeshade/opt/tubeshade/Tubeshade.Server cannot be stripped without corrupting the application
# embedded-library suppressed because in .NET 9, the Runtime contains a statically linked version of zlib-ng.
lintian \
	--suppress-tags dir-or-file-in-opt,dir-or-file-in-etc-opt \
	--suppress-tags unstripped-binary-or-object \
	--suppress-tags embedded-library \
	tubeshade.deb
