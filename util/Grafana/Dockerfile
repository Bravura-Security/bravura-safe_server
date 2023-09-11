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


COPY entrypoint.sh /
RUN chmod +x /entrypoint.sh

# Expose the Grafana port
EXPOSE 3000

ENTRYPOINT ["sh", "/entrypoint.sh"]

# Start Grafana
#CMD ["grafana-server", "--config=/etc/grafana/grafana.ini"]
