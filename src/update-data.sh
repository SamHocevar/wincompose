#!/bin/sh

set -e

STEPS=5

INSTALLER=installer/installer.iss

#
# Rebuild po/wincompose.pot from our master translation file Text.resx
# then update all .po files
#

echo "[1/${STEPS}] Rebuild potfiles…"
DEST=po/wincompose.pot
# Update POT-Creation-Date with: date +'%Y-%m-%d %R%z'
cat > ${DEST} << EOF
# SOME DESCRIPTIVE TITLE.
# Copyright (C) YEAR THE PACKAGE'S COPYRIGHT HOLDER
# This file is distributed under the same license as the PACKAGE package.
# FIRST AUTHOR <EMAIL@ADDRESS>, YEAR.
#
#, fuzzy
msgid ""
msgstr ""
"Project-Id-Version: WinCompose $(dotnet-gitversion -showvariable MajorMinorPatch)\n"
"Report-Msgid-Bugs-To: Sam Hocevar <sam@hocevar.net>\n"
"POT-Creation-Date: 2015-03-23 15:27+0100\n"
"PO-Revision-Date: YEAR-MO-DA HO:MI+ZONE\n"
"Last-Translator: FULL NAME <EMAIL@ADDRESS>\n"
"Language-Team: LANGUAGE <LL@li.org>\n"
"Language: \n"
"MIME-Version: 1.0\n"
"Content-Type: text/plain; charset=UTF-8\n"
"Content-Transfer-Encoding: 8bit\n"

EOF
for FILE in language/i18n/Text.resx language/unicode/Category.resx; do
    awk < ${FILE} '
    /<!--/      { off=1 }
    /-->/       { off=0 }
    /<data /    { split($0, a, "\""); id=a[2]; comment=""; obsolete=0 }
    /"Obsolete/ { obsolete=1 }
    /<value>/   { split ($0, a, /[<>]/); value=a[3]; line=NR; }
    /<comment>/ { split ($0, a, /[<>]/); comment=a[3]; }
    /<\/data>/  { if (!off) {
                      if (comment) { print "#. " comment }
                      if (obsolete) { print "#. This string is obsolete but might be reused in the future" }
                      print "#: '${FILE}':" line " ID:" id;
                      print "msgid \"" value "\"";
                      print "msgstr \"\""; print "";
                  } }' \
  | sed 's/&amp;/\&/g; s/&lt;/</g; s/&gt;/>/g' \
  >> ${DEST}
done
for FILE in installer/installer.pas; do
    awk < ${FILE} '
    /_\(/ { ok=1; value=""; line=NR; }
          { if(ok) { split($0, a, "'"'"'"); value=value a[2]; } }
    /\)/  { if (ok && value) {
                print "#. This string appears in the installer, not in WinCompose."
                print "#: '${FILE}':" line "";
                print "msgid \"" value "\"";
                print "msgstr \"\""; print ""; }
            ok=0; }' \
  | sed 's/&amp;/\&/g; s/&lt;/</g; s/&gt;/>/g' \
  >> ${DEST}
done

for POFILE in po/*.po; do
    printf %s ${POFILE}
    msgmerge -U ${POFILE} po/wincompose.pot
done
rm -f po/*~

#
# Update each Text.*.resx with contents from *.po, i.e. the
# work from Weblate translators
#

po2res()
{
    case "$1" in
        pt_BR) echo pt-BR ;;
        zh) echo zh-CHS ;;
        zh_Hant) echo zh-CHT ;;
        sc) echo it-CH ;;
        eo) echo de-CH ;;
        be@latin) echo be-BY ;;
        *@*) echo "" ;; # ignore these languages
        *) echo $polang ;;
    esac
}

echo "[2/${STEPS}] Rebuild resx files…"
for POFILE in po/*.po; do
    polang=$(basename ${POFILE} .po)
    reslang=$(po2res $polang)
    if [ "$reslang" = "" ]; then
        continue
    fi

    for FILE in language/i18n/Text.resx language/unicode/Category.resx; do
        DEST=${FILE%%.resx}.${reslang}.resx
        sed -e '/^  <data/,$d' < ${FILE} > ${DEST}
        cat ${POFILE} \
          | sed 's/&/\&amp;/g; s/</\&lt;/g; s/>/\&gt;/g; s/\\"/\&quot;/g' \
          | awk 'function f() {
                     if (good && id && msgstr) {
                         print "  <data name=\"" id "\" xml:space=\"preserve\">";
                         print "    <value>" msgstr "</value>";
                         if (0 && comment) { print "    <comment>" comment "</comment>"; }
                         print "  </data>";
                     }
                     reset();
                 }
                 function reset() { good=0; id=""; comment=""; }
                 /^$/        { f(); }
                 END         { f(); }
                 /^#[.] /    { split($0, a, "#[.] "); comment=a[2]; }
                 /^#:.*ID:/  { split($0, a, "ID:"); id=a[2]; }
                 /^#: .*\/'${FILE##*/}':/ { good=1 }
                 /^#, fuzzy/ { reset(); }
                 /^ *"/      { split($0, a, "\""); msgstr=msgstr a[2]; }
                 /^msgstr/   { split($0, a, "\""); msgstr=a[2]; }' \
          >> ${DEST}
        echo "</root>" >> ${DEST}
        touch ${DEST%%.resx}.Designer.cs
    done
done

#
# Use Unicode description files from the unicode translation project
# and create .resx translation files for our project
#

echo "[3/${STEPS}] Rebuild Unicode translation files…"
for POFILE in 3rdparty/unicode-translation/po/*.po; do
    polang=$(basename ${POFILE} .po)
    reslang=$(po2res $polang)
    printf "${polang}... "

    # Parse data and put it in the Char.*.resx and Block.*.resx files
    for FILE in Char Block; do
        # This combination seemingly has problems
        if [ "${polang} ${FILE}" = "de Char" ]; then
            continue
        fi
        case ${FILE} in
            #. CHARACTER NAME for U+007B
            Char) CODE='/^#[.] CHARACTER NAME for / { split($0, a, "+"); c="U" a[2]; }' ;;
            #. UNICODE BLOCK: U+0000..U+007F
            Block) CODE='/^#[.] UNICODE BLOCK: / { split($0, a, /[+.]/); c="U" a[3] "_U" a[6]; }' ;;
        esac
        DEST=language/unicode/${FILE}.${reslang}.resx
        sed -e '/^  <data/,$d' < language/unicode/${FILE}.resx > ${DEST}
        if uname | grep -qi mingw; then unix2dos; else cat; fi < ${POFILE} \
          | sed 's/&/\&amp;/g; s/</\&lt;/g; s/>/\&gt;/g' \
          | awk 'function f() {
                     if (c && msgstr) {
                         print "  <data name=\"" c "\" xml:space=\"preserve\">";
                         print "    <value>" msgstr "</value>";
                         print "  </data>";
                     }
                     c=""; msgstr="";
                 }
                 '"${CODE}"'
                 /^msgstr/ { split($0, a, "\""); msgstr=a[2]; f(); }' \
          >> ${DEST}
        echo "</root>" >> ${DEST}
        touch ${DEST%%.resx}.Designer.cs
    done
done
echo "done."

#
# Check some language.csproj consistency
#

echo "[4/${STEPS}] Check consistency…"
for x in language/unicode/*.*.resx language/i18n/*.*.resx; do
    reslang="$(echo $x | cut -f2 -d.)"
    if ! grep -q '"'$(echo $x | cut -f2- -d/ | tr / .)'"' language/language.csproj; then
        echo "WARNING: $x not found in language.csproj"
    fi
    if ! grep -q '^Source: "{#BINDIR}[\\]'$reslang'[\\][*][.]dll";' ${INSTALLER}; then
        echo "WARNING: $reslang DLL not found in ${INSTALLER}"
    fi
    if grep -q '^; Name: "'$reslang'";' ${INSTALLER}; then
        echo "WARNING: $reslang is commented out in ${INSTALLER}"
    fi
done

if [ -d "/c/Program Files (x86)/Inno Setup 6/Languages" ]; then
    for f in "/c/Program Files (x86)/Inno Setup 6/Languages/"*.isl*; do
        f="$(basename "$f")"
        if ! grep -q "$f" ${INSTALLER}; then
            echo "WARNING: $f exists in Inno Setup but is not mentioned in ${INSTALLER}"
        fi
    done
fi

for po in po/*.po; do
    l="$(sed -ne 's/Language-Team: *//p' $po | tr -d '"' | cut -f1 -d' ')"
    if [ -f "3rdparty/innosetup/Files/Languages/Unofficial/$l.isl" ]; then
        if ! grep -q "$l[.]isl" ${INSTALLER}; then
            echo "WARNING: $po exists and $l.isl exists in Unofficial Inno Setup but they are not mentioned in ${INSTALLER}"
        fi
    fi
done

#
# Build translator list
#

echo "[5/${STEPS}] Update contributor list…"
printf '﻿' > wincompose/res/.contributors.html
cat >> wincompose/res/.contributors.html << EOF
<html>
<body style="font-family: verdana, sans-serif; font-size: .7em;">
<h3>Programming</h3>
<ul>
  <li>Sam Hocevar &lt;sam@hocevar.net&gt;</li>
  <li>Benlitz &lt;dev@benlitz.net&gt;</li>
  <li>gdow &lt;gdow@divroet.net&gt;</li>
</ul>
<h3>Translation</h3>
<ul>
EOF
git log --stat po/*.po | \
  awk 'BEGIN { FS="[/.@ ]" }
       /^Author:/ { n=substr($0, 9) }
       /^ src\/po.*[.]po\>/ { lut[n][$4]=1 }
       END { for(n in lut) {
                 s="";
                 for(l in lut[n]) {
                     if(s)s=s", ";
                     s=s l;
                 }
                 print n" ("s")"
           } }' \
  | grep -v '\(Daniele <daniele.viglietti\|Kastuś.Kaszenia\|Michael Robert Lawrence\)' \
  | sed 's/\(.*Sam Hocevar.*\) (.*)/\1 (de, fr, es)/' \
  | LANG=C sort | uniq \
  | sed 's/</\&lt;/g' | sed 's/>/\&gt;/g' | sed 's,.*,<li>&</li>,' \
  >> wincompose/res/.contributors.html
cat >> wincompose/res/.contributors.html << EOF
</ul>
<h3>Donations</h3>
<ul>
<li>Aaron Mayzes</li>
<li>André Rauscher</li>
<li>Casper Clausen</li>
<li>Anonyme</li>
<li>Ben Babcock</li>
<li>Francois Conil</li>
<li>Edward Palmer</li>
<li>Bryan Mills</li>
<li>Grigory Rechistov</li>
<li>Matt Proud</li>
<li>Pascal Heitz</li>
<li>Jacob Dreesen</li>
<li>Anonymous</li>
<li>Fabio Ferreira Cachapa</li>
<li>Tim Weber</li>
<li>Andreu Cabré Chacón</li>
<li>Anonymous</li>
<li>Marc Dirix</li>
<li>Jeffrey Miller</li>
<li>Anonymous</li>
<li>Jason Baietto</li>
<li>Stephen Friedrich</li>
</ul>
</body>
</html>
EOF
mv wincompose/res/.contributors.html wincompose/res/contributors.html

