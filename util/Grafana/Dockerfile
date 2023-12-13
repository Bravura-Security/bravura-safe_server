FROM grafana/grafana

LABEL com.hitachi.product="bravura_vault"

ENV USERNAME="bitwarden"
ENV GROUPNAME="bitwarden"

USER root:root

ENV GOSU_VERSION 1.12
RUN set -eux; \
        \
        apk add --no-cache --virtual .gosu-deps \
                ca-certificates \
                dpkg \
                gnupg \
                wget \
        ; \
        \
        dpkgArch="$(dpkg --print-architecture | awk -F- '{ print $NF }')"; \
        wget -O /usr/local/bin/gosu "https://github.com/tianon/gosu/releases/download/$GOSU_VERSION/gosu-$dpkgArch"; \
        wget -O /usr/local/bin/gosu.asc "https://github.com/tianon/gosu/releases/download/$GOSU_VERSION/gosu-$dpkgArch.asc"; \
        \
# verify the signature
        export GNUPGHOME="$(mktemp -d)"; \
        if [ -n "${http_proxy+set}" ]; then \
            gpg --batch --keyserver hkps://keys.openpgp.org --keyserver-options "http-proxy=$http_proxy" --recv-keys B42F6819007F00F88E364FD4036A9C25BF357DD4; \
        else \
            gpg --batch --keyserver hkps://keys.openpgp.org --recv-keys B42F6819007F00F88E364FD4036A9C25BF357DD4; \
        fi; \
        gpg --batch --verify /usr/local/bin/gosu.asc /usr/local/bin/gosu; \
        command -v gpgconf && gpgconf --kill all || :; \
        rm -rf "$GNUPGHOME" /usr/local/bin/gosu.asc; \
        \
# clean up fetch dependencies
        apk del --no-network .gosu-deps; \
        \
        chmod +x /usr/local/bin/gosu; \
# verify that the binary works
        gosu --version; \
        gosu nobody true; \
# add packages for alpine container
        apk add --upgrade --no-cache \
                linux-pam \
                shadow \
                tzdata \
                openssl \
                icu-libs


# COPY custom.ini /etc/grafana/custom.ini
ENV GF_INSTALL_PLUGINS="grafana-clock-panel,grafana-simple-json-datasource,grafana-piechart-panel"
ENV GF_SERVER_SERVE_FROM_SUB_PATH=true
ENV GF_SERVER_ROOT_URL="%(protocol)s://%(domain)s/grafana"


# customize grafana sections
ENV GF_ENABLE_GZIP=true
ENV GF_USERS_DEFAULT_THEME=dark

## Replace Favicon and Apple Touch
COPY favicon-32x32.png /usr/share/grafana/public/img/fav32.png
COPY favicon-32x32.png /usr/share/grafana/public/img/apple-touch-icon.png

## Replace Logo
COPY grafana_bravura-safe-logo-white.svg /usr/share/grafana/public/img/grafana_icon.svg

## Update Background
COPY g8_login_dark.svg /usr/share/grafana/public/img/g8_login_dark.svg
COPY g8_login_light.svg /usr/share/grafana/public/img/g8_login_light.svg

## Update Title
RUN find /usr/share/grafana/public/build/ -name *.js -exec sed -i 's|AppTitle="Grafana"|AppTitle="Bravura Safe"|g' {} \;

## Update Login Title
RUN find /usr/share/grafana/public/build/ -name *.js -exec sed -i 's|LoginTitle="Welcome to Grafana"|LoginTitle="Welcome to Bravura Safe"|g' {} \;

## Remove Documentation, Support, Community in the Footer
RUN find /usr/share/grafana/public/build/ -name *.js -exec sed -i 's|\[{target:"_blank",id:"documentation".*grafana_footer"}\]|\[\]|g' {} \;

## Remove Edition in the Footer
#RUN find /usr/share/grafana/public/build/ -name *.js -exec sed -i 's|({target:"_blank",id:"license",.*licenseUrl})|()|g' {} \;

## Remove Version in the Footer
#RUN find /usr/share/grafana/public/build/ -name *.js -exec sed -i 's|({target:"_blank",id:"version",.*CHANGELOG.md":void 0})|()|g' {} \;

## Remove New Version is available in the Footer
## this next line will cause crash on grafana 10.2.x do not uncomment
##RUN find /usr/share/grafana/public/build/ -name *.js -exec sed -i 's|({target:"_blank",id:"updateVersion",.*grafana_footer"})|()|g' {} \;

## Remove News icon
RUN find /usr/share/grafana/public/build/ -name *.js -exec sed -i 's|..createElement(....,{className:.,onClick:.,iconOnly:!0,icon:"rss","aria-label":"News"})|null|g' {} \;

# end grafana customizations

COPY entrypoint.sh /
RUN chmod +x /entrypoint.sh

# Expose the Grafana port
EXPOSE 3000
EXPOSE 3001

HEALTHCHECK --interval=1m --timeout=10s CMD wget --no-verbose --tries=3 --spider http://localhost:3000/api/health || exit 1

ENTRYPOINT ["sh", "/entrypoint.sh"]

# Start Grafana
#CMD ["grafana-server", "--config=/etc/grafana/grafana.ini"]
# ENTRYPOINT ["tail", "-f", "/dev/null"]
